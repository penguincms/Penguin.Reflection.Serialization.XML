using System;
using System.Collections.Generic;
using System.Text;

namespace Penguin.Reflection.Serialization.XML.Attributes
{
    public class XmlPropertyAttribute : Attribute
    {
        public string Name { get; private set; }
        public XmlPropertyAttribute(string name)
        {
            this.Name = name;
        }
    }
}
