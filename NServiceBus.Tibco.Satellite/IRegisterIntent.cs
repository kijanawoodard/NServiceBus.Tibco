namespace NServiceBus.Tibco.Satellite
{
    public interface IRegisterIntent
    {
        void Subscribe<T>(string key) where T : class, new();
        void Publish<T>(string key) where T : class, new();
    }
}