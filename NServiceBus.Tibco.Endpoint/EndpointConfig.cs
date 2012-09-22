using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using NServiceBus;
using NServiceBus.Tibco.Satellite;
using NServiceBus.Unicast;
using NServiceBus.Utils;
using TIBCO.EMS;
using log4net;

namespace NServiceBus.Tibco.Endpoint
{
    class EndpointConfig : IConfigureThisEndpoint, AsA_Publisher { }

    public class Startup : IWantToRunAtStartup
    {
        private static int _index;
        public void Run()
        {
            TibcoSatellite.InstallIfNeccessary(); //TODO: remove this; something is wrong with the satellite creating the queue
            Console.WriteLine("Press 'Enter' to send a message");

            while (Console.ReadLine() != null)
            {
                var foo = new Foo {Id = string.Format("Command {0}", ++_index)};
                var xml = foo.ToXml();
                TibcoTesting.SendMessage("a.topic", xml);
            }
        }

        public void Stop()
        {
            
        }
    }

    class FooHandler : IHandleMessages<Foo>
    {
        public IBus Bus { get; set; }
        private static int _index;

        public void Handle(Foo message)
        {
            Interlocked.Increment(ref _index);
            Bus.Publish<IFoo>(foo => foo.Id = string.Format("Event {0}", _index));
            Logger.Info(message);
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(FooHandler));
    }

    public class TibcoRegistration : IWantToRegisterIntentForTibco
    {
        public TibcoRegistration()
        {
            
        }

        public void Init(IRegisterIntent register)
        {
            register.Subscribe<Foo>("foo.sub");
            register.Publish<IFoo>("foo.pub");
        }
    }

    public class Foo : ICommand
    {
        public string Id { get; set; }

        public override string ToString()
        {
            return string.Format("Id: {0}", Id);
        }
    }

    public interface IFoo : IEvent
    {
        string Id { get; set; }
    }

    public static class ObjectExtensions
    {
        /// <summary>Serializes an object of type T in to an xml string</summary>
        /// <typeparam name="T">Any class type</typeparam>
        /// <param name="obj">Object to serialize</param>
        /// <returns>A string that represents Xml, empty otherwise</returns>
        public static string ToXml<T>(this T obj) where T : class
        {
            if (obj == null) throw new ArgumentNullException("obj");

            var serializer = new XmlSerializer(typeof(T));
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, obj);
                return writer.ToString();
            }
        }

        /// <summary>Deserializes an xml string in to an object of Type T</summary>
        /// <typeparam name="T">Any class type</typeparam>
        /// <param name="xml">Xml as string to deserialize from</param>
        /// <returns>A new object of type T is successful, null if failed</returns>
        public static T FromXml<T>(this string xml) where T : class, new()
        {
            if (xml == null) throw new ArgumentNullException("xml");

            var serializer = new XmlSerializer(typeof(T));
            using (var reader = new StringReader(xml))
            {
                try
                {
                    return (T)serializer.Deserialize(reader);
                }
                catch
                {
                    return null;
                } // Could not be deserialized to this type.
            }
        }
    }
}
