using System;
using System.IO;
using System.Xml.Serialization;

namespace NServiceBus.Tibco.Satellite.Utlities
{
    public static class ObjectExtensions
    {
        /// <summary>Serializes an object of type T in to an xml string</summary>
        /// <typeparam name="T">Any class type</typeparam>
        /// <param name="obj">Object to serialize</param>
        /// <returns>A string that represents Xml, empty otherwise</returns>
        public static string ToXml<T>(this T obj) where T : class
        {
            if (obj == null) throw new ArgumentNullException("obj");

            var serializer = new XmlSerializer(typeof (T));
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

            var serializer = new XmlSerializer(typeof (T));
            using (var reader = new StringReader(xml))
            {
                try
                {
                    return (T) serializer.Deserialize(reader);
                }
                catch
                {
                    return null;
                } // Could not be deserialized to this type.
            }
        }
    }
}