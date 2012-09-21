using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using NServiceBus;
using NServiceBus.Tibco.Satellite;
using NServiceBus.Utils;

namespace NServiceBus.Tibco.Endpoint
{
    class EndpointConfig : IConfigureThisEndpoint, AsA_Publisher { }

    public class Startup : IWantToRunAtStartup
    {
        public IBus Bus { get; set; }

        public void Run()
        {
            TibcoSatellite.InstallIfNeccessary(); //TODO: remove this; something is wrong with the satellite creating the queue
            Console.WriteLine("Press 'Enter' to send a message");

            int i = 0;
            while (Console.ReadLine() != null)
            {
                Bus.Publish<Foo>(foo => foo.Id = string.Format("Message {0}", i));
            }
        }

        public void Stop()
        {
            
        }
    }

    public class TibcoRegistration : IWantToRegisterIntentForTibco
    {
        public TibcoRegistration()
        {
            
        }

        public void Init(IRegisterIntent register)
        {
            register.Publish<Foo>("hook");

        }
    }

    [Serializable]
    public class Foo : IEvent
    {
        public string Id { get; set; }
    }
}
