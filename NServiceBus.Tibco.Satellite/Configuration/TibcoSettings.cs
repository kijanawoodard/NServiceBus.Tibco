using System.Collections.Generic;
using System.Configuration;

namespace NServiceBus.Tibco.Satellite.Configuration
{
    public class TibcoSettings : ConfigurationSection
    {
        public static readonly TibcoSettings Settings = ConfigurationManager.GetSection("TibcoSettings") as TibcoSettings;
        
        [ConfigurationProperty("", Options = ConfigurationPropertyOptions.IsDefaultCollection)]
        [ConfigurationCollection(typeof(TibcoConnectionElementCollection), AddItemName = "connection")]
        public TibcoConnectionElementCollection Connections
        {
            get
            {
                return (TibcoConnectionElementCollection)base[""];
            }
        }
    }

    public class TibcoConnectionElementCollection : ConfigurationElementCollection, IEnumerable<TibcoConnectionElement>
    {
        //TODO: flatten out the configuration: http://stackoverflow.com/questions/2002715/how-to-specify-a-collection-in-a-custom-configsection
        protected override ConfigurationElement CreateNewElement()
        {
            return new TibcoConnectionElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((TibcoConnectionElement)element).Url;
        }

        public TibcoConnectionElement this[int index]
        {
            get
            {
                return BaseGet(index) as TibcoConnectionElement;
            }
        }

        public IEnumerator<TibcoConnectionElement> GetEnumerator()
        {
            int count = base.Count;
            for (int i = 0; i < count; i++)
            {
                yield return base.BaseGet(i) as TibcoConnectionElement;
            }
        }
    }

    public class TibcoConnectionElement : ConfigurationElement
    {
        [ConfigurationProperty("url", IsRequired = true, IsKey = true)]
        public string Url
        {
            get { return (string)this["url"]; }
            set { this["url"] = value; }
        }

        [ConfigurationProperty("username", IsRequired = true)]
        public string UserName
        {
            get { return (string)this["username"]; }
            set { this["username"] = value; }
        }

        [ConfigurationProperty("password", IsRequired = true)]
        public string Password
        {
            get { return (string)this["password"]; }
            set { this["password"] = value; }
        }

        [ConfigurationProperty("", Options = ConfigurationPropertyOptions.IsDefaultCollection)]
        [ConfigurationCollection(typeof(TibcoElementCollection), AddItemName = "destination")]
        public TibcoElementCollection Destinations
        {
            get
            {
                return (TibcoElementCollection)base[""];
            }
        }
    }

    public class TibcoElementCollection : ConfigurationElementCollection, IEnumerable<TibcoElement>
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new TibcoElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((TibcoElement)element).Key;
        }

        public TibcoElement this[int index]
        {
            get
            {
                return BaseGet(index) as TibcoElement;
            }
        }

        public IEnumerator<TibcoElement> GetEnumerator()
        {
            int count = base.Count;
            for (int i = 0; i < count; i++)
            {
                yield return base.BaseGet(i) as TibcoElement;
            }
        }
    }

    public class TibcoElement : ConfigurationElement
    {
        [ConfigurationProperty("key", IsRequired = true, IsKey = true)]
        public string Key
        {
            get { return (string)this["key"]; }
            set { this["key"] = value; }
        }

        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }

        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }
    }
}