using System;
using TIBCO.EMS;

namespace NServiceBus.Tibco.Satellite
{
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

        private void SubscribeTopic(Session session, IMessageListener listener)
        {
            var destination = session.CreateTopic(_name);

            var sub = session.CreateDurableSubscriber(destination, TibcoSatellite.TibcoAddress.ToString());
            sub.MessageListener = listener;
        }

        private void SubscribeQueue(Session session, IMessageListener listener)
        {
            var destination = session.CreateQueue(_name);

            var messageConsumer = session.CreateConsumer(destination);
            messageConsumer.MessageListener = listener;
        }

        public void Publish(Session session, string key, string data)
        {
            if (key != Key) return;

            var destination = _type == "topic" ? session.CreateTopic(_name) as Destination : session.CreateQueue(_name);
            var producer = session.CreateProducer(destination);
            var message = session.CreateTextMessage(data);
            producer.Send(message);
        }
    }
}