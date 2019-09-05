using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Penguin.Reflection.Serialization.XML
{
    public static class TextReaderExtensions
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
            while(reader.Read() != c)
            {

            }
        }
    }
}
