using System;
using System.Collections.Generic;
using System.Text;

namespace Penguin.Reflection.Serialization.XML.Classes
{
    public class StringValue
    {
        public string Value { get; set; }

        public override string ToString()
        {
            return Value;
        }
    }
}
