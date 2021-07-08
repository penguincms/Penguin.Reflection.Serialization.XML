using Penguin.Reflection.Serialization.XML.Attributes;
using Penguin.Reflection.Serialization.XML.Interfaces;
using System.Reflection;

namespace Penguin.Reflection.Serialization.XML.Extensions
{
    internal static class IPropertyInfoExtensions
    {
        public static string GetPropertyName(this IPropertyInfo pi) => pi.GetCustomAttribute<XmlPropertyAttribute>()?.Name ?? pi.Name;

        public static string GetPropertyName(this PropertyInfo pi) => pi.GetCustomAttribute<XmlPropertyAttribute>()?.Name ?? pi.Name;
    }
}