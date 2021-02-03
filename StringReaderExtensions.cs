using Penguin.Reflection.Serialization.XML.Exceptions;
using System;
using System.IO;
using System.Linq;

namespace Penguin.Reflection.Serialization.XML
{
    internal static class TextReaderExtensions
    {
        public static void AdvancePast(this TextReader reader, string s)
        {
            char[] cbuff = new char[s.Length];

            while (!cbuff.SequenceEqual(s) && cbuff[0] != -1)
            {
                for (int i = 1; i < cbuff.Length; i++)
                {
                    cbuff[i - 1] = cbuff[i];
                }

                cbuff[cbuff.Length - 1] = (char)reader.Read();
            }
        }

        public static void AdvancePast(this TextReader reader, char c)
        {
            int x;
            while ((x = reader.Read()) != c)
            {
                if (x == -1)
                {
                    throw new CharacterNotFoundException("End of stream reached without matching character", c);
                }
            }
        }
    }
}