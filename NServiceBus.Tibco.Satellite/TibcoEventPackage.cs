using System;

namespace NServiceBus.Tibco.Satellite
{
    //Why does this exist? when you try to bus.send an event to our satellite queue, the NSB infrastructure gets upset. 
    //Would like a way to say "i'm doing something werid, but let it go". 
    //In the meantime, wrap the data to go to Tibco.
    public class TibcoEventPackage : ICommand
    {
        public string Type { get; set; }
        public string Data { get; set; }
    }
}