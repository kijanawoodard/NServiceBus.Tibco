using System;
using TIBCO.EMS;
using log4net;

namespace NServiceBus.Tibco.Satellite
{
    public class TibcoDestination
    {
        private readonly string _key;
        private readonly string _type;
        private readonly string _name;
        private bool _haveExpressedInterest;
        private Session _session;

        public TibcoDestination(string key, string type, string name)
        {
            _key = key;
            var ok = type == "topic" || type == "queue";
            if (!ok)
                throw new ArgumentException(string.Format("Did not understand type configuration '{3}' for tibco destination '{0}'. Valid values are {1} or {2}.", key, "topic", "queue", type));

            _type = type;
            _name = name;
        }

        public void SetSession(Session session)
        {
            _session = session;
        }

        public void Subscribe(string key, IMessageListener listener)
        {
            if (key != _key) return;

            if (_type == "topic")
                SubscribeTopic( listener);
            else
                SubscribeQueue(listener);
        }

        private void SubscribeTopic(IMessageListener listener)
        {
            var destination = _session.CreateTopic(_name);

            var sub = _session.CreateDurableSubscriber(destination, TibcoSatellite.TibcoAddress.ToString());
            sub.MessageListener = listener;
        }

        private void SubscribeQueue(IMessageListener listener)
        {
            var destination = _session.CreateQueue(_name);

            var messageConsumer = _session.CreateConsumer(destination);
            messageConsumer.MessageListener = listener;
        }

        public void Publish(string key, string data)
        {
            if (key != _key) return;

            var destination = _type == "topic" ? _session.CreateTopic(_name) as Destination : _session.CreateQueue(_name);
            var producer = _session.CreateProducer(destination);
            var message = _session.CreateTextMessage(data);
            producer.Send(message);

            Logger.Info("Published to Tibco: " + data);
        }

        //would get rid of this, but Publish doesn't happen at startup time; unfortunate naming for Publish; Subscribe makes sense, publish...not so much
        public bool IsInterested(string key)
        {
            var yes = key == _key;
            if (yes)
                _haveExpressedInterest = true;

            return yes;
        }

        public void WarnIfNoInterestExpressed()
        {
            if (!_haveExpressedInterest)
                Logger.Warn(string.Format("No interest for destination key '{0}'.", _key));
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(TibcoSatellite));
    }
}