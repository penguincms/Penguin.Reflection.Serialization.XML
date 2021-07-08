using Penguin.Reflection.Extensions;
using Penguin.Reflection.Serialization.XML.Exceptions;
using Penguin.Reflection.Serialization.XML.Extensions;
using Penguin.Reflection.Serialization.XML.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Penguin.Reflection.Serialization.XML
{
    /// <summary>
    /// Serializes/Deserializes XML
    /// </summary>
    public class XMLSerializer
    {
        private XMLDeserializerOptions Options { get; set; }
        private Dictionary<Type, Dictionary<string, IPropertyInfo>> PropertyCache { get; set; } = new Dictionary<Type, Dictionary<string, IPropertyInfo>>();

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
        public XMLSerializer(XMLDeserializerOptions options) => this.Options = options;

        /// <summary>
        /// Static serialization method
        /// </summary>
        /// <param name="o">Object to serialize</param>
        /// <returns>Serialized object</returns>
        public static string Serialize(object o) => new XMLSerializer().SerializeObject(o);

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
                if (this.Options.StartNode != null)
                {
                    reader.AdvancePast("<" + this.Options.StartNode);
                }

                reader.AdvancePast(">");

                return this.GetValue(typeof(T), reader, '>') as T;
            }
            catch (CharacterNotFoundException cex) when (startPos != -1 && startReader != null && startReader.BaseStream.CanSeek)
            {
                startReader.BaseStream.Seek(startPos, SeekOrigin.Begin);

                StringBuilder exceptionBuilder = new StringBuilder();
                _ = (char)startReader.Read();
                char c;
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

            return this.DeserializeObject<T>(reader);
        }

        /// <summary>
        /// Serializes the object to an XML string.
        /// </summary>
        /// <param name="source">The object to serialize</param>
        /// <returns>The serialized object</returns>
        public string SerializeObject(object source) => source is null ? throw new ArgumentNullException(nameof(source)) : this.SerializeObject(source, new StringBuilder());

        /// <summary>
        /// Defines the PropertyInfo for a given type used to map during deserialization. Should allow the Deserializer to skip properties by
        /// specifying only the properties you want deserialized
        /// </summary>
        /// <param name="t">The type being targeted</param>
        /// <param name="properties">The properties to deserialize</param>
        public void SetProperties(Type t, IEnumerable<PropertyInfo> properties)
        {
            if (this.PropertyCache.ContainsKey(t))
            {
                this.PropertyCache.Remove(t);
            }

            if (properties is null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            this._SetProperties(t, properties);
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

            foreach (IPropertyInfo pInfo in this.GetProperties(Source).Values)
            {
                object pValue = pInfo.GetValue(Source);

                if (pValue is null)
                {
                    continue;
                }

                this.SerializeProperty(pInfo, pValue, sb);
            }

            return sb.ToString();
        }

        private void _SetProperties(Type t, IEnumerable<PropertyInfo> properties)
        {
            Dictionary<string, IPropertyInfo> props = this.Options.CaseSensitive
                ? new Dictionary<string, IPropertyInfo>()
                : new Dictionary<string, IPropertyInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (PropertyInfo p in properties)
            {
                props.Add(p.GetPropertyName(), new SystemPropertyInfo(p));
            }

            this.PropertyCache.Add(t, props);
        }

        private Dictionary<string, IPropertyInfo> GetProperties(object propertySource)
        {
            if (propertySource is null)
            {
                throw new ArgumentNullException(nameof(propertySource));
            }

            if (propertySource is ExpandoObject eo)
            {
                Dictionary<string, IPropertyInfo> toReturn = new Dictionary<string, IPropertyInfo>();

                IDictionary<string, object> expandoDict = (IDictionary<string, object>)eo;

                foreach (KeyValuePair<string, object> kvp in expandoDict)
                {
                    toReturn.Add(kvp.Key, new ExpandoPropertyInfo(kvp));
                }

                return toReturn;
            }
            else
            {
                Type t = propertySource.GetType();

                if (this.PropertyCache.TryGetValue(t, out Dictionary<string, IPropertyInfo> props))
                {
                    return props;
                }
                else
                {
                    this._SetProperties(t, t.GetProperties());
                    return this.PropertyCache[t];
                }
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
                do
                {
                    if (c != '<')
                    {
                        reader.AdvancePast('<');
                    }

                    (_, lastChar) = this.GetNextPropertyName(reader);

                    (o as IList).Add(this.GetValue(collectionType, reader, lastChar));

                    do
                    {
                        c = (char)reader.Read();
                    } while (char.IsWhiteSpace(c));
                } while ((char)reader.Peek() != '/');

                reader.AdvancePast('>');
            }
            else
            {
                Dictionary<string, IPropertyInfo> props = this.GetProperties(o);

                if (this.Options.AttributesAsProperties)
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

                        if (props.TryGetValue(pName, out IPropertyInfo pInfo))
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

                (propName, lastChar) = this.GetNextPropertyName(reader);

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

                        (propName, lastChar) = this.GetNextPropertyName(reader);

                        continue;
                    }

                    if (props.TryGetValue(propName.Replace("-", "_"), out IPropertyInfo pInfo))
                    {
                        pInfo.SetValue(o, this.GetValue(pInfo.PropertyType, reader, lastChar));
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
                        reader.Read();
                    }

                    (propName, lastChar) = this.GetNextPropertyName(reader);
                } while (propName[0] != '/');
            }

            return o;
        }

        private void SerializeProperty(IPropertyInfo PInfo, object Source, StringBuilder sb)
        {
            sb.Append($"<{PInfo.GetPropertyName()}>");
            foreach (IPropertyInfo pInfo in this.GetProperties(Source).Values)
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
                    sb.Append($"<{pInfo.GetPropertyName()}>");
                    sb.Append((int)pValue);
                    sb.Append($"</{pInfo.GetPropertyName()}>");
                }
                else if (pInfo.PropertyType == typeof(string))
                {
                    sb.Append($"<{pInfo.GetPropertyName()}>");
                    sb.Append(pValue);
                    sb.Append($"</{pInfo.GetPropertyName()}>");
                }
                else
                {
                    this.SerializeProperty(pInfo, pValue, sb);
                }
            }
            sb.Append($"</{PInfo.GetPropertyName()}>");
        }
    }
}