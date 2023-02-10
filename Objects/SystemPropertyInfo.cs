using System;
using System.Reflection;

namespace Penguin.Reflection.Serialization.XML.Interfaces
{
    internal class SystemPropertyInfo : IPropertyInfo
    {
        private readonly PropertyInfo PropertyInfo;

        public string Name => this.PropertyInfo.Name;

        public Type PropertyType => this.PropertyInfo.PropertyType;

        public SystemPropertyInfo(PropertyInfo propertyInfo) => this.PropertyInfo = propertyInfo;

        public TAttribute GetCustomAttribute<TAttribute>() where TAttribute : Attribute => this.PropertyInfo.GetCustomAttribute<TAttribute>();

        public object GetValue(object source) => this.PropertyInfo.GetValue(source);

        public void SetValue(object o, object convert) => this.PropertyInfo.SetValue(o, convert);
    }
}