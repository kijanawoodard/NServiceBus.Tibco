using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NServiceBus.Satellites;
using NServiceBus.Serialization;
using NServiceBus.Tibco.Satellite.Configuration;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Tibco.Satellite
{
    internal class TibcoSatellite : ISatellite, IRegisterIntent
    {
        public IBus Bus { get; set; }
        private List<TibcoConnection> _connections;
        public IMessageSerializer MessageSerializer { get; set; }

        public void Handle(TransportMessage message)
        {
            TibcoEventPackage package;

            using (var stream = new MemoryStream(message.Body))
            {
                package = (TibcoEventPackage)MessageSerializer.Deserialize(stream).First();
            }

            foreach (var m in package.Messages)
            {
                using (var stream = new MemoryStream())
                {
                    MessageSerializer.Serialize(new object[] { m }, stream);
                    stream.Position = 0;
                    var sr = new StreamReader(stream);
                    var xml = sr.ReadToEnd();

                    //TODO: remove item from array wrapper
                    //TODO: handle for xml, json, and binary??? might be easier to handle xml first to unwrap array
                    //TODO: find destinations interested in type and publish
                }
            }
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
                                .Select(destination => new TibcoDestination(destination.Key, destination.Type, destination.Name));
                        
                        var tc = new TibcoConnection(connection.Url, connection.UserName, connection.Password, destinations);
                        return tc;
                    })
                    .ToList();

            Configure.Instance.ForAllTypes<IWantToRegisterIntentForTibco>(t =>
            {
                var ini = (IWantToRegisterIntentForTibco)Activator.CreateInstance(t);
                ini.Init(this);
            });            

            var unicast = Bus as UnicastBus;
            if (unicast != null)
            {
                unicast.MessagesSent += MessagesSent;
            }
        }

        void MessagesSent(object sender, MessagesEventArgs e)
        {
            if (e.Messages.Length == 1 && e.Messages[0] is TibcoEventPackage)
                return;
            //TODO: only send types we are interesting in
            Bus.Send(InputAddress, e.Messages); //Put them on the satellite queue to allow the main endpoint to complete
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

        internal static readonly Address TibcoAddress = Address.Local.SubScope("EMS");
        
        void IRegisterIntent.Subscribe<T>(string key)  
        {
            var listener = new MessageListener<T>(Bus, key);
            var connections = _connections.Where(x => x.IsInterested(key)).ToList();
            //TODO: Throw if zero; Warn if  more than one?

            connections.ForEach(x => x.Subscribe(key, listener));
        }

        void IRegisterIntent.Publish<T>(string key)
        {
            throw new NotImplementedException();
        }
    }
}
