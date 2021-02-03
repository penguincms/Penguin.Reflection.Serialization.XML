using Penguin.Reflection.Serialization.XML.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Penguin.Reflection.Serialization.XML.Extensions
{
    static class PropertyInfoExtensions
    {
        public static string GetPropertyName(this PropertyInfo pi) => pi.GetCustomAttribute<XmlPropertyAttribute>()?.Name ?? pi.Name;
    }
}
