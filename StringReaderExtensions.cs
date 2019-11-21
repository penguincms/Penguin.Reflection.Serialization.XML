using Penguin.Reflection.Serialization.XML.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public static void AdvancePast(this TextReader reader, char c)
        {
            int x;
            while((x = reader.Read()) != c)
            {
                if(x == -1)
                {
                    throw new CharacterNotFoundException("End of stream reached without matching character", c);
                }
            }
        }
    }
}
