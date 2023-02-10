using System;
using System.Collections.Generic;

namespace Penguin.Reflection.Serialization.XML.Interfaces
{
    internal class ExpandoPropertyInfo : IPropertyInfo
    {
        public string Name { get; private set; }

        public Type PropertyType { get; }

        public ExpandoPropertyInfo(KeyValuePair<string, object> kvp)
        {
            this.Name = kvp.Key;
            this.PropertyType = kvp.Value?.GetType() ?? typeof(object);
        }

        public TAttribute GetCustomAttribute<TAttribute>() where TAttribute : Attribute => null;

        public object GetValue(object source)
        {
            IDictionary<string, object> toGet = (IDictionary<string, object>)source;
            if (toGet.ContainsKey(this.Name))
            {
                return toGet[this.Name];
            }

            return null;
        }

        public void SetValue(object o, object convert)
        {
            IDictionary<string, object> toSet = (IDictionary<string, object>)o;

            if (toSet.ContainsKey(this.Name))
            {
                toSet[this.Name] = convert;
            }
            else
            {
                toSet.Add(this.Name, convert);
            }
        }
    }
}