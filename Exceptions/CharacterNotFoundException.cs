using System;

namespace Penguin.Reflection.Serialization.XML.Exceptions
{
    public class CharacterNotFoundException : Exception
    {
        public char SearchedChar { get; internal set; }
        public string XmlString { get; internal set; }

        public CharacterNotFoundException(string message, char c) : base(message) => this.SearchedChar = c;

        public CharacterNotFoundException(string message, char c, Exception innerException) : base(message, innerException) => this.SearchedChar = c;
    }
}