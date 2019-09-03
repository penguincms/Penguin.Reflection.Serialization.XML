using Penguin.Reflection.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Penguin.Reflection.Serialization.XML
{
    public class XMLSerializer
    {
        XMLDeserializerOptions Options { get; set; }
        public XMLSerializer()
        {
            Options = new XMLDeserializerOptions();
        }
        public XMLSerializer(XMLDeserializerOptions options)
        {
            Options = options;
        }

        public string SerializeObject(object Source, StringBuilder sb = null)
        {

            if (sb is null)
            {
                sb = new StringBuilder();
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

        public  (string propName, char lastChar) GetNextPropertyName(StringReader reader)
        {
            StringBuilder s = new StringBuilder();

            char c = (char)reader.Read();

            
            while(!char.IsWhiteSpace(c) && c != '>')
            {
                s.Append(c);
                c = (char)reader.Read();
            }


            return (s.ToString(), c);
        }

        //XML Serializers suck. 
        public T DeserializeObject<T>(string Xml)
        {
            StringReader reader = new StringReader(Xml);


            T o = Activator.CreateInstance<T>();

            PropertyInfo[] props = o.GetType().GetProperties();


            if (Options.StartNode != null)
            {
                reader.AdvancePast(Options.StartNode);
            }

            reader.AdvancePast(">");


            int i = reader.Read();

            while (i != -1)
            {

                char c = (char)i;

                while (c != '<')
                {
                    c = (char)reader.Read();
                }

                (string propName, char lastChar) = GetNextPropertyName(reader);

                if(propName[0] == '/')
                {
                    return o;
                }

                PropertyInfo pInfo = props.FirstOrDefault(p => isNameEqual(propName, p.Name));

                if (lastChar != '>')
                {
                    reader.AdvancePast('>');
                }


                if (pInfo != null)
                {
                    pInfo.SetValue(o, GetValue(pInfo.PropertyType, reader));
                }
            }

            return o;
        }

        private bool isNameEqual(string XEName, string PName)
        {
            return string.Equals(XEName.Replace("-", "_"), PName, Options.NameComparisonType);
        }

        private object GetValue(Type pType, StringReader reader, char lastChar = ' ')
        {
            char c = lastChar;

            while (char.IsWhiteSpace(c))
            {
                c = (char)reader.Read();
            }

            if (c == '<')
            {
                if(pType == typeof(string))
                {
                    reader.AdvancePast('>');

                    c = (char)reader.Read();

                    StringBuilder toReturn = new StringBuilder();

                    while(c != '<')
                    {
                        toReturn.Append(c);
                        c = (char)reader.Read();
                    }

                    reader.AdvancePast('>');

                    return toReturn.ToString();

                }

                object o = Activator.CreateInstance(pType);


                if (o.GetType().ImplementsInterface<IList>())
                {
                    Type collectionType = pType.GetCollectionType();

                    do
                    {
                        (o as IList).Add(GetValue(collectionType, reader, c));

                        do
                        {
                            c = (char)reader.Read();
                        } while (char.IsWhiteSpace(c));

                    } while ((char)reader.Peek() != '/');

                    reader.AdvancePast('>');
                }
                else
                {


                    PropertyInfo[] props = o.GetType().GetProperties();

                    string propName;

                    (propName, lastChar) = GetNextPropertyName(reader);

                    if (Options.AttributesAsProperties)
                    {
                        
                        while (lastChar != '>')
                        {
                            StringBuilder prop = new StringBuilder();

                            StringBuilder val = new StringBuilder();

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

                            do
                            {
                                val.Append(c);
                                c = (char)reader.Read();
                            } while (c != '"');

                            PropertyInfo pInfo = props.FirstOrDefault(p => isNameEqual(prop.ToString(), p.Name));

                            if (pInfo != null)
                            {
                                pInfo.SetValue(o, val.ToString().Convert(pInfo.PropertyType));
                            }

                            do
                            {
                                c = (char)reader.Read();
                            } while (char.IsWhiteSpace(c));

                            if(c == '/')
                            {
                                reader.AdvancePast('>');
                                return o;
                            }

                            lastChar = c;
                        }
                    }

                    if (lastChar != '>')
                    {
                        reader.AdvancePast('>');
                    }

                    do
                    {

                        PropertyInfo pInfo = props.FirstOrDefault(p => isNameEqual(propName, p.Name));

                        if (pInfo != null)
                        {
                            pInfo.SetValue(o, GetValue(pInfo.PropertyType, reader));
                        }


                        do
                        {
                            c = (char)reader.Read();
                        } while (char.IsWhiteSpace(c));

                        (propName, lastChar) = GetNextPropertyName(reader);


                    } while (propName[0] != '/');

                }

                return o;

            } else
            {
                StringBuilder propValue = new StringBuilder();

                while(c != '<')
                {
                    propValue.Append(c);
                    c = (char)reader.Read();

                }

                reader.AdvancePast('>');

                return propValue.ToString().Convert(pType); 
            }

            return null;

        }




        //private object DeserializeProperty(XmlElement oxe, Type t)
        //{

        //    object o = Activator.CreateInstance(t);

        //    if (t.ImplementsInterface<IList>())
        //    {
        //        foreach (XmlElement xe in oxe.ChildNodes)
        //        {
        //            (o as IList).Add(GetValue(t.GetCollectionType(), xe));
        //        }
        //    }
        //    else
        //    {
        //        PropertyInfo[] props = o.GetType().GetProperties();

        //        foreach (XmlElement xe in oxe.ChildNodes)
        //        {

        //            PropertyInfo pInfo = props.FirstOrDefault(p => isNameEqual(xe.Name, p.Name));

        //            if (!(pInfo is null))
        //            {
        //                pInfo.SetValue(o, GetValue(pInfo.PropertyType, xe));
        //            }
        //        }

        //        if (Options.AttributesAsProperties)
        //        {
        //            foreach (XmlAttribute xa in oxe.Attributes)
        //            {
        //                PropertyInfo pInfo = props.FirstOrDefault(p => isNameEqual(xa.Name, p.Name));

        //                if (!(pInfo is null))
        //                {
        //                    pInfo.SetValue(o, xa.Value.Convert(pInfo.PropertyType));
        //                }
        //            }
        //        }
        //    }

        //    return o;
        //}

        public bool IsNullable(object obj)
        {
            Type t = obj.GetType();
            return t.IsGenericType
                && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private void SerializeProperty(PropertyInfo PInfo, object Source, StringBuilder sb)
        {
            sb.Append($"<{PInfo.Name}>");
            foreach (PropertyInfo pInfo in Source.GetType().GetProperties())
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
