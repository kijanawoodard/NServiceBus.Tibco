using System;
using System.Collections.Generic;
using System.Linq;
using TIBCO.EMS;

namespace NServiceBus.Tibco.Satellite
{
    public class TibcoConnection : IDisposable
    {
        private readonly string _url;
        private readonly string _username;
        private readonly string _password;
        private readonly List<TibcoDestination> _destinations;

        private Connection _connection;
        private Session _session;

        public TibcoConnection(string url, string username, string password, IEnumerable<TibcoDestination> destinations)
        {
            _url = url;
            _username = username;
            _password = password;
            _destinations = destinations.ToList();

            _session = GetSession(); //TODO: this needs a bit more thought; do we need to handle dropped sessions to tibco or does the tibco dll take care of that for us

            _destinations.ForEach(x => x.SetSession(_session)); //go ahead and connect now; if something is wrong with the connection info, fail fast; doesn't take into account temporary failure
        }

        public void Subscribe(string key, IMessageListener listener)
        {
            _destinations.ForEach(x => x.Subscribe(key, listener));
        }

        public void Publish(string key, string data)
        {
            _destinations.ForEach(x => x.Publish(key, data));
        }

        public int InterestedDestinationCount(string key)
        {
            return _destinations.Count(x => x.IsInterested(key));
        }

        public void LookForDestinationsThatHaveNotShownInterestInAnyKey()
        {
            _destinations.ForEach(x => x.WarnIfNoInterestExpressed());
        }

        public void Dispose()
        {
            _connection.Stop();
        }

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
    }
}