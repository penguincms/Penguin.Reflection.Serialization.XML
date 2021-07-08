using System;

namespace Penguin.Reflection.Serialization.XML.Interfaces
{
    public interface IPropertyInfo
    {
        string Name { get; }

        Type PropertyType { get; }

        TAttribute GetCustomAttribute<TAttribute>() where TAttribute : Attribute;

        object GetValue(object source);

        void SetValue(object o, object convert);
    }
}