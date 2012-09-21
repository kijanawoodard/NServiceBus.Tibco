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
            var session = GetSession();
            _destinations.ForEach(x => x.Subscribe(session, listener));
        }

        public void Publish(string key, string data)
        {
            _destinations.ForEach(x => x.Publish(GetSession(), key, data));
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
    }
}