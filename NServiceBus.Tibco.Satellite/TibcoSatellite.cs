using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Xml;
using NServiceBus.MessageInterfaces;
using NServiceBus.Satellites;
using NServiceBus.Serialization;
using NServiceBus.Serializers.XML;
using NServiceBus.Tibco.Satellite.Configuration;
using NServiceBus.Tibco.Satellite.Utlities;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Transport;
using NServiceBus.Utils;

namespace NServiceBus.Tibco.Satellite
{
    public class TibcoSatellite : ISatellite, IRegisterIntent
    {
        public IBus Bus { get; set; }
        private List<TibcoConnection> _connections;
        public XmlMessageSerializer MessageSerializer { get; set; }
        public IMessageMapper Mapper { get; set; }

        public void Handle(TransportMessage message)
        {
            var doc = new XmlDocument();
            doc.Load(new MemoryStream(message.Body));
            var messageBodyXml = doc.InnerXml;

            

//            TibcoEventPackage package;
//
//            using (var stream = new MemoryStream(message.Body))
//            {
//                package = (TibcoEventPackage) MessageSerializer.Deserialize(stream).First();
//            }
//
//            using (var stream = new MemoryStream())
//            {
//                MessageSerializer.Serialize(new[] {package.Message}, stream);
//                stream.Position = 0;
//                var sr = new StreamReader(stream);
//                var xml = sr.ReadToEnd();
//
//                //TODO: remove item from array wrapper
//                //TODO: handle for xml, json, and binary??? might be easier to handle xml first to unwrap array
//                //TODO: find destinations interested in type and publish
//            }
        }

        public void Start()
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

            Configure.Instance.ForAllTypes<IWantToRegisterIntentForTibco>(t =>
                                                                              {
                                                                                  var ini =
                                                                                      (IWantToRegisterIntentForTibco)
                                                                                      Activator.CreateInstance(t);
                                                                                  ini.Init(this);
                                                                              });

            Mapper.Initialize(_typeToPublish.Values.ToArray());
            MessageSerializer = new XmlMessageSerializer(Mapper);
            MessageSerializer.Initialize(_typeToPublish.Values.ToArray());

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
                var publishers = _typeToPublish.Where(x => x.Value == type).ToList();
                if (publishers.Count == 0) continue;

                var xml = "";
                using (var stream = new MemoryStream())
                {
                    MessageSerializer.Serialize(new object[] { message }, stream);
                    stream.Position = 0;
                        
                    var doc = new XmlDocument();
                    doc.Load(stream);
                    xml = doc.InnerXml;
                }

                publishers.ForEach(pub => _connections.ForEach(x => x.Publish(pub.Key, xml)));
            }
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

        //TODO: remove this; something is wrong with the satellite creating the queue
        public static void InstallIfNeccessary()
        {
            MsmqUtilities.CreateQueueIfNecessary(TibcoAddress, WindowsIdentity.GetCurrent().Name);
        }

        public static readonly Address TibcoAddress = Address.Local.SubScope("EMS");

        void IRegisterIntent.Subscribe<T>(string key)
        {
            var listener = new MessageListener<T>(Bus, key);
            var connections = _connections.Where(x => x.IsInterested(key)).ToList();
            //TODO: Throw if zero; Warn if  more than one?

            connections.ForEach(x => x.Subscribe(key, listener));
        }

        void IRegisterIntent.Publish<T>(string key)
        {
            _typeToPublish[key] = typeof(T);
        }

        private readonly Dictionary<string, Type> _typeToPublish = new Dictionary<string, Type>();
    }
}
