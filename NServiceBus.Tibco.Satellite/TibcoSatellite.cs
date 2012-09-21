using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Xml;
using log4net;
using NServiceBus.MessageInterfaces;
using NServiceBus.Satellites;
using NServiceBus.Serializers.XML;
using NServiceBus.Tibco.Satellite.Configuration;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Transport;
using NServiceBus.Utils;

namespace NServiceBus.Tibco.Satellite
{
    public class TibcoSatellite : ISatellite, IRegisterIntent
    {
        public IBus Bus { get; set; }
        public IMessageMapper Mapper { get; set; }
        public XmlMessageSerializer MessageSerializer { get; set; } //explicitly go to xml; host process may have different serialization needs than for tibco
        
        private List<TibcoConnection> _connections;
        private readonly Dictionary<string, Type> _typesToPublish = new Dictionary<string, Type>();

        public void Handle(TransportMessage message)
        {
            TibcoEventPackage package;

            using (var stream = new MemoryStream(message.Body))
            {
                package = (TibcoEventPackage) MessageSerializer.Deserialize(stream).First();
            }

            var keys = _typesToPublish.Where(x => x.Value.ToString().Equals(package.Type)).Select(x => x.Key).ToList();
            keys.ForEach(key => _connections.ForEach(x => x.Publish(key, package.Data)));
        }

        public void Start()
        {
            ParseSettings();
            RegisterIntentsForTibco();
            InitializeSerializer();
            RegisterCallBackForWhenMessagesAreSentOnTheMainProcess();
            Logger.Info("Ready");
        }

        public void Stop()
        {
            _connections.ForEach(x => x.Dispose());
        }

        public Address InputAddress
        {
            get { return TibcoAddress; }
            set { }
        }

        public bool Disabled
        {
            get { return false; }
            set { }
        }

        private void ParseSettings()
        {
            var settings = TibcoSettings.Settings;

            _connections =
                settings
                    .Connections
                    .Select(connection =>
                    {
                        var destinations =
                            connection
                                .Destinations
                                .Select(
                                    destination =>
                                    new TibcoDestination(destination.Key, destination.Type, destination.Name));

                        var tc = new TibcoConnection(connection.Url, connection.UserName,
                                                     connection.Password, destinations);
                        return tc;
                    })
                    .ToList();
        }

        private void RegisterIntentsForTibco()
        {
            Configure
                .Instance
                .ForAllTypes<IWantToRegisterIntentForTibco>(t =>
                {
                    var ini = (IWantToRegisterIntentForTibco)Activator.CreateInstance(t);
                    ini.Init(this);
                });

            _connections.ForEach(x => x.LookForDestinationsThatHaveNotShownInterestInAnyKey());
        }

        private void InitializeSerializer()
        {
            Mapper.Initialize(_typesToPublish.Values.ToArray());
            MessageSerializer = new XmlMessageSerializer(Mapper);
            MessageSerializer.Initialize(_typesToPublish.Values.ToArray());
        }

        private void RegisterCallBackForWhenMessagesAreSentOnTheMainProcess()
        {
            var unicast = Bus as UnicastBus;
            if (unicast != null)
            {
                unicast.MessagesSent += MessagesSent;
            }
        }

        private void MessagesSent(object sender, MessagesEventArgs e)
        {
            foreach (var message in e.Messages)
            {
                var type = message.GetType();
                var keys = _typesToPublish.Where(x => x.Value.IsAssignableFrom(type)).Select(x => x.Key).ToList();
                if (keys.Count == 0) continue;

                var xml = GenerateXml(message);

                //send to tibco from satellite queue to disconnect "tibco unreachable" from host process
                Bus.Send<TibcoEventPackage>(TibcoAddress, x =>
                {
                    x.Type = _typesToPublish.First().Value.ToString(); //don't use type because it could be __impl
                    x.Data = xml;
                });
            }
        }

        private string GenerateXml(object message)
        {
            using (var stream = new MemoryStream())
            {
                MessageSerializer.Serialize(new object[] { message }, stream); //TODO: handle for json, and binary, objectMessage???
                stream.Position = 0;

                var doc = new XmlDocument();
                doc.Load(stream);
                return doc.InnerXml;
            }
        }

        //TODO: remove this; something is wrong with the satellite creating the queue
        public static void InstallIfNeccessary()
        {
            MsmqUtilities.CreateQueueIfNecessary(TibcoAddress, WindowsIdentity.GetCurrent().Name);
        }

        public static readonly Address TibcoAddress = Address.Local.SubScope("EMS");

        void IRegisterIntent.Subscribe<T>(string key)
        {
            DoWeHaveDestinationsForThisKey(key);

            var listener = new MessageListener<T>(Bus, key);
            _connections.ForEach(x => x.Subscribe(key, listener));
        }

        void IRegisterIntent.Publish<T>(string key)
        {
            DoWeHaveDestinationsForThisKey(key);
            _typesToPublish[key] = typeof(T);
        }

        void DoWeHaveDestinationsForThisKey(string key)
        {
            var count = _connections.Sum(x => x.InterestedDestinationCount(key));
            if (count == 0)
            {
                Logger.Warn(string.Format("There is no destination setup for key '{0}'.", key));
            }

            if (count > 1)
            {
                Logger.Warn(string.Format("You have {0} destinations configured for key '{1}'. Consider using a topic.", count, key));
            }
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(TibcoSatellite));
    }
}
