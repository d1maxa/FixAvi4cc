using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace FixAvi4cc
{
    [XmlRoot("Dictionary")]
    public class SerializableDictionary
    {
        [XmlElement("Item")]
        public List<SerializableKeyValue> Items { get; set; } = new List<SerializableKeyValue>();

        public SerializableDictionary() { }

        public SerializableDictionary(Dictionary<string, string> dict)
        {
            foreach (var kv in dict)
                Items.Add(new SerializableKeyValue { Key = kv.Key, Value = kv.Value });
        }

        public Dictionary<string, string> ToDictionary()
        {
            var dict = new Dictionary<string, string>();
            foreach (var item in Items)
                dict[item.Key] = item.Value;
            return dict;
        }
    }

    public class SerializableKeyValue
    {
        [XmlAttribute("Key")]
        public string Key { get; set; }

        [XmlText]
        public string Value { get; set; }
    }

    public static class XmlDictionaryHelper
    {
        public static void Save(string path, Dictionary<string, string> dict)
        {
            var wrapper = new SerializableDictionary(dict);
            var serializer = new XmlSerializer(typeof(SerializableDictionary));
            using (var writer = new StreamWriter(path))
                serializer.Serialize(writer, wrapper);
        }

        public static Dictionary<string, string> Load(string path)
        {
            var serializer = new XmlSerializer(typeof(SerializableDictionary));
            using (var reader = new StreamReader(path))
            {
                var wrapper = (SerializableDictionary)serializer.Deserialize(reader);
                return wrapper.ToDictionary();
            }
        }
    }
}