using System.Configuration;

namespace NServiceBus.Tibco.Satellite.Configuration
{
    public sealed class TibcoEmsConfig : ConfigurationSection
    {
        private static readonly ConfigurationProperty _propMyServices;

        private static readonly ConfigurationPropertyCollection _properties;

        private static TibcoEmsConfig _settings = ConfigurationManager.GetSection("TibcoEmsConfig") as TibcoEmsConfig;
        public static TibcoEmsConfig Settings
        {
            get
            {
                return _settings;
            }
        }

        static TibcoEmsConfig()
        {
            _propMyServices = new ConfigurationProperty(
                null, typeof(global::TibcoConnectionElementCollection), null,
                ConfigurationPropertyOptions.IsDefaultCollection);
            _properties = new ConfigurationPropertyCollection { _propMyServices };
        }

        [ConfigurationProperty("", Options = ConfigurationPropertyOptions.IsDefaultCollection)]
        [ConfigurationCollection(typeof(TibcoConnectionElementCollection), AddItemName = "connection")]
        public global::TibcoConnectionElementCollection MyServices
        {
            get { return (global::TibcoConnectionElementCollection)base[_propMyServices]; }
            set { base[_propMyServices] = value; }
        }

        protected override ConfigurationPropertyCollection Properties
        { get { return _properties; } }
    }
}

[ConfigurationCollection(typeof(DestinationElement), CollectionType = ConfigurationElementCollectionType.AddRemoveClearMap)]
public sealed class TibcoConnectionElementCollection : ConfigurationElementCollection
{
    public DestinationElement this[int index]
    {
        get { return (DestinationElement)BaseGet(index); }
        set
        {
            if (BaseGet(index) != null) { BaseRemoveAt(index); }
            BaseAdd(index, value);
        }
    }

    public new DestinationElement this[string key]
    {
        get { return (DestinationElement)BaseGet(key); }
    }

    protected override ConfigurationElement CreateNewElement()
    {
        return new DestinationElement();
    }

    protected override object GetElementKey(ConfigurationElement element)
    {
        return ((DestinationElement)element).Name;
    }
}

public class DestinationElement : ConfigurationElement
{
    private static readonly ConfigurationProperty _propName;

    private static readonly ConfigurationPropertyCollection properties;

    static DestinationElement()
    {
        _propName = new ConfigurationProperty("name", typeof(string), null, null,
                                              new StringValidator(1),
                                              ConfigurationPropertyOptions.IsRequired |
                                              ConfigurationPropertyOptions.IsKey);
        properties = new ConfigurationPropertyCollection { _propName };
    }

    [ConfigurationProperty("name", DefaultValue = "",
          Options = ConfigurationPropertyOptions.IsRequired |
                    ConfigurationPropertyOptions.IsKey)]
    public string Name
    {
        get { return (string)base["name"]; }
        set { base["name"] = value; }
    }

    protected override ConfigurationPropertyCollection Properties
    { get { return properties; } }
}