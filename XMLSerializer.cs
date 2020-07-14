using Penguin.Reflection.Extensions;
using Penguin.Reflection.Serialization.XML.Exceptions;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Penguin.Reflection.Serialization.XML
{
    /// <summary>
    /// Serializes/Deserializes XML
    /// </summary>
    public class XMLSerializer
    {
        private XMLDeserializerOptions Options { get; set; }
        private Dictionary<Type, Dictionary<string, PropertyInfo>> PropertyCache { get; set; } = new Dictionary<Type, Dictionary<string, PropertyInfo>>();

        /// <summary>
        /// Constructs a new instance of the XML serializer with the default options
        /// </summary>
        public XMLSerializer() : this(new XMLDeserializerOptions())
        {
        }

        /// <summary>
        /// Constructs a new instance of the XML serializer with the given options
        /// </summary>
        /// <param name="options"></param>
        public XMLSerializer(XMLDeserializerOptions options)
        {
            Options = options;
        }

        /// <summary>
        /// Deserializes an XML stream to the requested type
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="reader">And TextReader stream</param>
        /// <returns>The deserialized object</returns>
        public T DeserializeObject<T>(TextReader reader) where T : class
        {
            if (reader is null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            long startPos = -1;
            StreamReader startReader = null;

            if (reader is StreamReader sReader)
            {
                startReader = sReader;
                if (sReader.BaseStream.Position == sReader.BaseStream.Length)
                {
                    return null;
                }

                startPos = sReader.BaseStream.Position;
            }

            try
            {
                if (Options.StartNode != null)
                {
                    reader.AdvancePast("<" + Options.StartNode);
                }

                reader.AdvancePast(">");

                return GetValue(typeof(T), reader, '>') as T;
            }
            catch (CharacterNotFoundException cex) when (startPos != -1 && startReader != null && startReader.BaseStream.CanSeek)
            {
                startReader.BaseStream.Seek(startPos, SeekOrigin.Begin);

                StringBuilder exceptionBuilder = new StringBuilder();
                char c = (char)startReader.Read();

                while ((c = (char)startReader.Read()) != -1 && exceptionBuilder.Length < 5000)
                {
                    exceptionBuilder.Append(c);
                }
                cex.XmlString = exceptionBuilder.ToString();

                throw;
            }
        }

        /// <summary>
        /// Deserializes an XML string to the given object type
        /// </summary>
        /// <typeparam name="T">The type to return</typeparam>
        /// <param name="Xml">The XML to deserialize</param>
        /// <returns>The Deserialized object</returns>
        public T DeserializeObject<T>(string Xml) where T : class
        {
            StringReader reader = new StringReader(Xml);

            return DeserializeObject<T>(reader);
        }

        /// <summary>
        /// Serializes the object to an XML string.
        /// </summary>
        /// <param name="Source">The object to serialize</param>
        /// <returns>The serialized object</returns>
        public string SerializeObject(object Source) => SerializeObject(new StringBuilder());

        /// <summary>
        /// Defines the PropertyInfo for a given type used to map during deserialization. Should allow the Deserializer to skip properties by
        /// specifying only the properties you want deserialized
        /// </summary>
        /// <param name="t">The type being targeted</param>
        /// <param name="properties">The properties to deserialize</param>
        public void SetProperties(Type t, IEnumerable<PropertyInfo> properties)
        {
            if (PropertyCache.ContainsKey(t))
            {
                PropertyCache.Remove(t);
            }

            if (properties is null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            _SetProperties(t, properties);
        }

        internal (string propName, char lastChar) GetNextPropertyName(TextReader reader)
        {
            StringBuilder s = new StringBuilder(20);

            char c = (char)reader.Read();

            while (!char.IsWhiteSpace(c) && c != '>')
            {
                s.Append(c);
                c = (char)reader.Read();
            }

            return (s.ToString(), c);
        }

        internal string SerializeObject(object Source, StringBuilder sb)
        {
            if (sb is null)
            {
                sb = new StringBuilder(200);
            }

            foreach (PropertyInfo pInfo in Source.GetType().GetProperties())
            {
                object pValue = pInfo.GetValue(Source);
                if (pValue is null)
                {
                    continue;
                }
                SerializeProperty(pInfo, pValue, sb);
            }

            return sb.ToString();
        }

        private void _SetProperties(Type t, IEnumerable<PropertyInfo> properties)
        {
            Dictionary<string, PropertyInfo> props;

            if (Options.CaseSensitive)
            {
                props = new Dictionary<string, PropertyInfo>();
            }
            else
            {
                props = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            }

            foreach (PropertyInfo p in properties)
            {
                props.Add(p.Name, p);
            }

            PropertyCache.Add(t, props);
        }

        private Dictionary<string, PropertyInfo> GetProperties(Type t)
        {
            if (PropertyCache.TryGetValue(t, out Dictionary<string, PropertyInfo> props))
            {
                return props;
            }
            else
            {
                _SetProperties(t, t.GetProperties());
                return PropertyCache[t];
            }
        }

        private object GetValue(Type pType, TextReader reader, char lastChar = ' ')
        {
            char c = lastChar;

            if (pType.IsPrimitive || pType == typeof(string) || pType == typeof(decimal) || pType == typeof(DateTime))
            {
                if (lastChar != '>')
                {
                    reader.AdvancePast('>');
                }

                c = (char)reader.Read();

                StringBuilder toReturn = new StringBuilder(25);

                while (c != '<')
                {
                    toReturn.Append(c);
                    c = (char)reader.Read();
                }

                reader.AdvancePast('>');

                return toReturn.ToString().Convert(pType);
            }

            while (char.IsWhiteSpace(c))
            {
                c = (char)reader.Read();
            }

            lastChar = c;

            object o = Activator.CreateInstance(pType);

            if (o is IList)
            {
                Type collectionType = pType.GetCollectionType();
                string propName;
                do
                {
                    if (c != '<')
                    {
                        reader.AdvancePast('<');
                    }

                    (propName, lastChar) = GetNextPropertyName(reader);

                    (o as IList).Add(GetValue(collectionType, reader, lastChar));

                    do
                    {
                        c = (char)reader.Read();
                    } while (char.IsWhiteSpace(c));
                } while ((char)reader.Peek() != '/');

                reader.AdvancePast('>');
            }
            else
            {
                Dictionary<string, PropertyInfo> props = GetProperties(pType);

                if (Options.AttributesAsProperties)
                {
                    while (lastChar != '>')
                    {
                        StringBuilder prop = new StringBuilder(20);

                        StringBuilder val = new StringBuilder(20);

                        c = lastChar;

                        while (char.IsWhiteSpace(c))
                        {
                            c = (char)reader.Read();
                        }

                        do
                        {
                            prop.Append(c);
                            c = (char)reader.Read();
                        } while (c != '=');

                        reader.AdvancePast('"');

                        c = (char)reader.Read();

                        string pName = prop.ToString();

                        if (props.TryGetValue(pName, out PropertyInfo pInfo))
                        {
                            do
                            {
                                val.Append(c);
                                c = (char)reader.Read();
                            } while (c != '"');

                            pInfo.SetValue(o, val.ToString().Convert(pInfo.PropertyType));
                        }
                        else
                        {
                            do
                            {
                                c = (char)reader.Read();
                            } while (c != '"');
                        }

                        do
                        {
                            c = (char)reader.Read();
                        } while (char.IsWhiteSpace(c));

                        if (c == '/')
                        {
                            reader.AdvancePast('>');
                            return o;
                        }

                        lastChar = c;
                    }
                }

                while (lastChar != '<')
                {
                    lastChar = (char)reader.Read();
                }

                string propName;

                (propName, lastChar) = GetNextPropertyName(reader);

                do
                {
                    bool selfClosing = propName[propName.Length - 1] == '/';

                    while (lastChar != '>')
                    {
                        lastChar = (char)reader.Read();

                        if (lastChar == '/')
                        {
                            selfClosing = true;
                        }
                    }

                    if (selfClosing)
                    {
                        reader.AdvancePast("<");

                        (propName, lastChar) = GetNextPropertyName(reader);

                        continue;
                    }

                    if (props.TryGetValue(propName.Replace("-", "_"), out PropertyInfo pInfo))
                    {
                        pInfo.SetValue(o, GetValue(pInfo.PropertyType, reader, lastChar));
                    }
                    else
                    {
                        reader.AdvancePast(propName + ">");
                    }

                    do
                    {
                        c = (char)reader.Read();
                    } while (char.IsWhiteSpace(c));

                    if (c == '/')
                    {
                        c = (char)reader.Read();
                    }

                    (propName, lastChar) = GetNextPropertyName(reader);
                } while (propName[0] != '/');
            }

            return o;
        }

        private void SerializeProperty(PropertyInfo PInfo, object Source, StringBuilder sb)
        {
            sb.Append($"<{PInfo.Name}>");
            foreach (PropertyInfo pInfo in GetProperties(Source.GetType()).Values)
            {
                object pValue = pInfo.GetValue(Source);

                if (pValue is null)
                {
                    continue;
                }
                else if (pInfo.PropertyType.IsGenericType && pInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    bool hasValue = (bool)pInfo.PropertyType.GetProperty("HasValue").GetValue(pValue);

                    if (!hasValue)
                    {
                        continue;
                    }
                }

                if (pValue is Enum)
                {
                    sb.Append($"<{pInfo.Name}>");
                    sb.Append(((int)pValue).ToString());
                    sb.Append($"</{pInfo.Name}>");
                }
                else if (pInfo.PropertyType == typeof(string))
                {
                    sb.Append($"<{pInfo.Name}>");
                    sb.Append(pValue);
                    sb.Append($"</{pInfo.Name}>");
                }
                else
                {
                    SerializeProperty(pInfo, pValue, sb);
                }
            }
            sb.Append($"</{PInfo.Name}>");
        }
    }
}