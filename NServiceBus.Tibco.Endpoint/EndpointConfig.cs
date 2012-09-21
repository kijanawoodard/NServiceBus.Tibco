using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus;
using NServiceBus.Tibco.Satellite;

namespace NServiceBus.Tibco.Endpoint
{
    class EndpointConfig : IConfigureThisEndpoint, AsA_Publisher { }

    public class TibcoRegistration : IWantToRegisterIntentForTibco
    {
        public TibcoRegistration()
        {
            
        }

        public void Init(IRegisterIntent register)
        {
            register.Subscribe<Foo>("hook");
        }
    }

    public class Foo
    {
        public string Id { get; set; }
    }
}
