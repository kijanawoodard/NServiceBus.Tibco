using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using NServiceBus;
using NServiceBus.Config;
using NServiceBus.MessageInterfaces;
using NServiceBus.ObjectBuilder;
using NServiceBus.Satellites;
using NServiceBus.Serializers.XML;
using NServiceBus.Tibco.Satellite.Configuration;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Transport;
using TIBCO.EMS;

namespace NServiceBus.Tibco.Satellite
{
    internal class TibcoSatellite : ISatellite, IRegisterIntent
    {
        public IBus Bus { get; set; }
        public IMessageMapper Mapper { get; set; }
        public XmlMessageSerializer Serializer { get; set; }
        private List<TibcoConnection> _connections;
        public IBuilder Builder { get; set; }

        public void Handle(TransportMessage message)
        {

        }

        public void Start()
        {
            var settings = TibcoSettings.Settings;
            settings.Validate();

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


            return;
//            var type = Type.GetType("GI.Content.Articles.Events.IArticleWasPublished, GI.Content.Articles.Events");
//            Mapper.Initialize(new Type[] {type});
//            Serializer = new XmlMessageSerializer(Mapper);
//            Serializer.Initialize(new Type[] {type});
            //var l = TibcoEmsConfig.Settings;
            
            

            var unicast = Bus as UnicastBus;
            if (unicast != null)
            {
                unicast.MessagesSent += new EventHandler<MessagesEventArgs>(MessagesSent);
            }
        }

        void MessagesSent(object sender, MessagesEventArgs e)
        {
            foreach (var m in e.Messages)
            {
                var ok = m.GetType().FullName.Replace("__impl", "") == "GI.Content.Articles.Events.IArticleWasPublished";
                if (!ok) continue;

                using (var stream = new MemoryStream())
                {
                    Serializer.Serialize(new object[] { m }, stream);
                    stream.Position = 0;
                    var sr = new StreamReader(stream);
                    var xml = sr.ReadToEnd();
                }
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

        internal static readonly Address TibcoAddress = Address.Local.SubScope("EMS");
        
        void IRegisterIntent.Subscribe<T>(string key)  
        {
            var listener = new MessageListener<T>(Bus, key);
            var connections = _connections.Where(x => x.IsInterested(key)).ToList();
            //TODO: Warn if zero or more than one

            connections.ForEach(x => x.Subscribe(key, listener));
        }

        void IRegisterIntent.Publish<T>(string key)
        {
            throw new NotImplementedException();
        }
    }

    public interface IWantToRegisterIntentForTibco
    {
        void Init(IRegisterIntent register);
    }

    public interface IRegisterIntent
    {
        void Subscribe<T>(string key) where T : class, new();
        void Publish<T>(string key) where T : class, new();
    }

    public class TibcoConnection : IDisposable
    {
        private readonly string _url;
        private readonly string _username;
        private readonly string _password;
        private readonly List<TibcoDestination> _destinations;

        private Connection _connection;
        private Session _session;

        private Connection CreateConnection()
        {
            var factory = new ConnectionFactory(_url);
            var connection = factory.CreateConnection(_username, _password);
            connection.Start();
            return connection;
        }

        private Session GetSession()
        {
            _connection = _connection ?? CreateConnection();

            _session = _session ?? _connection.CreateSession(false, Session.CLIENT_ACKNOWLEDGE);
            return _session;
        }

        public TibcoConnection(string url, string username, string password, IEnumerable<TibcoDestination> destinations)
        {
            _url = url;
            _username = username;
            _password = password;
            _destinations = destinations.ToList();
        }

        public void Subscribe(string key, IMessageListener listener)
        {
            var destination = _destinations.First(x => x.Key == key);
            var session = GetSession();
            destination.Subscribe(session, listener);
        }

        public bool IsInterested(string key)
        {
            return _destinations.Any(x => x.Key == key);
        }

        public void Dispose()
        {
            _connection.Stop();
        }
    }

    public class TibcoDestination
    {
        public string Key { get; private set; }
        private readonly string _type;
        private readonly string _name;

        public TibcoDestination(string key, string type, string name)
        {
            Key = key;
            var ok = type == "topic" || type == "queue";
            if (!ok)
                throw new ArgumentException(string.Format("Did not understand type configuration '{3}' for tibco destination '{0}'. Valid values are {1} or {2}.", key, "topic", "queue", type));

            _type = type;
            _name = name;
        }

        public void Subscribe(Session session, IMessageListener listener)
        {
            if (_type == "topic")
                SubscribeTopic(session, listener);
            else
                SubscribeQueue(session, listener);
        }

        public void SubscribeTopic(Session session, IMessageListener listener)
        {
            var destination = session.CreateTopic(_name);

            var sub = session.CreateDurableSubscriber(destination, TibcoSatellite.TibcoAddress.ToString());
            sub.MessageListener = listener;
        }

        public void SubscribeQueue(Session session, IMessageListener listener)
        {
            var destination = session.CreateQueue(_name);

            var messageConsumer = session.CreateConsumer(destination);
            messageConsumer.MessageListener = listener;
        }
    }

    public class MessageListener<T> : IMessageListener where T : class, new()
    {
        public string Key { get; set; }
        private IBus _bus;

        public MessageListener(IBus bus, string key)
        {
            Key = key;
            _bus = bus;
        }

        private void Process(string message)
        {
            var result = message.FromXml<T>();
            _bus.SendLocal(result);
        }

        public void OnMessage(Message message)
        {
            var m = (TextMessage)message;
            Process(m.Text);
            message.Acknowledge();
        }
    }

    

    public static class ObjectExtensions
    {
        /// <summary>Serializes an object of type T in to an xml string</summary>
        /// <typeparam name="T">Any class type</typeparam>
        /// <param name="obj">Object to serialize</param>
        /// <returns>A string that represents Xml, empty otherwise</returns>
        public static string ToXml<T>(this T obj) where T : class
        {
            if (obj == null) throw new ArgumentNullException("obj");

            var serializer = new XmlSerializer(typeof (T));
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, obj);
                return writer.ToString();
            }
        }

        /// <summary>Deserializes an xml string in to an object of Type T</summary>
        /// <typeparam name="T">Any class type</typeparam>
        /// <param name="xml">Xml as string to deserialize from</param>
        /// <returns>A new object of type T is successful, null if failed</returns>
        public static T FromXml<T>(this string xml) where T : class, new()
        {
            if (xml == null) throw new ArgumentNullException("xml");

            var serializer = new XmlSerializer(typeof (T));
            using (var reader = new StringReader(xml))
            {
                try
                {
                    return (T) serializer.Deserialize(reader);
                }
                catch
                {
                    return null;
                } // Could not be deserialized to this type.
            }
        }
    }
}
