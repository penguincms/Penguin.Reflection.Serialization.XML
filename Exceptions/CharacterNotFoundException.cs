using System;
using System.Collections.Generic;
using System.Text;

namespace Penguin.Reflection.Serialization.XML.Exceptions
{
    public class CharacterNotFoundException : Exception
    {
        public string XmlString { get; internal set; }
        public char SearchedChar { get; internal set; }
        public CharacterNotFoundException(string message, char c) : base(message)
        {
        }

        public CharacterNotFoundException(string message, char c, Exception innerException) : base(message, innerException)
        {
        }
    }
}
