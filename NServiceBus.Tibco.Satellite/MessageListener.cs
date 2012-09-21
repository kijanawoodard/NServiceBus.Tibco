using NServiceBus.Tibco.Satellite.Utlities;
using TIBCO.EMS;

namespace NServiceBus.Tibco.Satellite
{
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
}