namespace NServiceBus.Tibco.Satellite
{
    public class TibcoEventPackage : ICommand
    {
        public object[] Messages { get; set; } //need a wrapper to avoid the "events should be published" error
    }
}