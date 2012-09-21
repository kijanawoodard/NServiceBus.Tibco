using System;

namespace NServiceBus.Tibco.Satellite
{
    public class TibcoEventPackage : ICommand
    {
        public string Type { get; set; }
        public string Data { get; set; }
    }
}