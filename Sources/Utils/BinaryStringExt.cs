using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Qiil.Binary.Utils
{
    public static class BinaryStringExt
    {
        public static void WriteStringNull(this BinaryWriter output, string text, Encoding encoding)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            if (text == null)
                throw new ArgumentNullException("text");

            if (encoding == null)
                throw new ArgumentNullException("encoding");

            output.Write(encoding.GetBytes(text));
            output.Write(encoding.GetBytes("\0"));
        }

        public static string ReadStringNull(this BinaryReader input, Encoding encoding)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            if (encoding == null)
                throw new ArgumentNullException("encoding");

            byte[] terminator = encoding.GetBytes("\0"); // Problem: The encoding may not have a NULL character
            int charSize = terminator.Length; // Problem: The character size may be variable
            List<byte> strBytes = new List<byte>();
            byte[] chr;
            while (!(chr = input.ReadBytes(charSize)).SequenceEqual(terminator))
            {
                if (chr.Length != charSize)
                    return null; //throw new EndOfStreamException();

                strBytes.AddRange(chr);
            }

            return encoding.GetString(strBytes.ToArray());
        }
    }
}

