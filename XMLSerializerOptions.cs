using System;
using System.Collections.Generic;
using System.Text;

namespace Penguin.Reflection.Serialization.XML
{
    public class XMLDeserializerOptions
    {
        public bool CaseSensitive { get; set; } = true;

        public bool AttributesAsProperties { get; set; } = true;

        public string StartNode { get; set; }

        internal StringComparison NameComparisonType
        {
            get
            {
                return CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            }
        }
    }
}
