using AISCast.Model.Message;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace AISCast.Decoding
{
    public static class Decoder
    {
        private static readonly List<char> Mapping;
        private static readonly List<char> ASCIIEncoding;

        public static IDictionary<string, int> Test = new Dictionary<string, int>();

        static Decoder()
        {
            ASCIIEncoding = new List<char>();
            Mapping = new List<char>();

            CreateASCIIEncoding();
            CreateMapping();
        }

        #region Pre-built maps
        private static void CreateASCIIEncoding()
        {
            ASCIIEncoding.Add('@');
            for (char i = 'A'; i <= 'Z'; i++)
            {
                ASCIIEncoding.Add(i);
            }
            ASCIIEncoding.Add('[');
            ASCIIEncoding.Add('\\');
            ASCIIEncoding.Add(']');
            ASCIIEncoding.Add('^');
            ASCIIEncoding.Add('_');
            ASCIIEncoding.Add(' ');
            ASCIIEncoding.Add('!');
            ASCIIEncoding.Add('"');
            ASCIIEncoding.Add('#');
            ASCIIEncoding.Add('$');
            ASCIIEncoding.Add('%');
            ASCIIEncoding.Add('&');
            ASCIIEncoding.Add('\'');
            ASCIIEncoding.Add('*');
            ASCIIEncoding.Add('+');
            ASCIIEncoding.Add(',');
            ASCIIEncoding.Add('-');
            ASCIIEncoding.Add('.');
            ASCIIEncoding.Add('/');
            for (char i = '0'; i <= '9'; i++)
            {
                ASCIIEncoding.Add(i);
            }
            ASCIIEncoding.Add(':');
            ASCIIEncoding.Add(';');
            ASCIIEncoding.Add('<');
            ASCIIEncoding.Add('=');
            ASCIIEncoding.Add('>');
            ASCIIEncoding.Add('?');
        }

        private static void CreateMapping()
        {
            for(char i = '0'; i <= '9'; i++)
            {
                Mapping.Add(i);
            }
            Mapping.Add(':');
            Mapping.Add(';');
            Mapping.Add('<');
            Mapping.Add('=');
            Mapping.Add('>');
            Mapping.Add('?');
            Mapping.Add('@');
            for(char i = 'A'; i <= 'W'; i++)
            {
                Mapping.Add(i);
            }
            Mapping.Add('`');
            for (char i = 'a'; i <= 'w'; i++)
            {
                Mapping.Add(i);
            }
        }
        #endregion

        public static IMessage Decode(string input)
        {
            var bits = DecodeBits(input);

            var messageTypeBits = SubArray(bits, 0, 6);

            var messageType = BitsToInt(messageTypeBits);

            switch(messageType[0])
            {
                case 5:
                    return ExtractMessage5(bits);
                default:
                    return null;
            }
        }

        private static IMessage ExtractMessage5(bool[] bits)
        {
            var callSignBits = SubArray(bits, 70, 42);
            var callSignInts = BitsToInt(callSignBits);
            var callSign = IntsToString(callSignInts);

            var vesselNameBits = SubArray(bits, 112, 120);
            var vesselNameInts = BitsToInt(vesselNameBits);
            var vesselName = IntsToString(vesselNameInts);

            var mmsiBits = SubArray(bits, 8, 30);
            var mmsi = BitsToLong(mmsiBits).ToString();

            var message = new Message5
            {
                CallSign = callSign.TrimEnd(),
                VesselName = vesselName.TrimEnd(),
                MMSI = mmsi
            };

            return message;
        }

        private static bool[] DecodeBits(string input)
        {
            var rawBinary = new bool[input.Length * 6];
            var newDataOffset = rawBinary.Length - 6;

            foreach(var chr in input)
            {
                rawBinary = ShiftLeft(rawBinary);

                var bits = CharToBits(chr);

                for (var i = 0; i < 6; i++)
                    rawBinary[newDataOffset + i] = bits[i];
            }

            return rawBinary;
        }

        private static bool[] ShiftLeft(bool[] bits)
        {
            var maxLength = bits.Length - 6;
            for (int i = 0; i < maxLength; i++)
            {
                bits[i] = bits[i + 6];
            }

            for (int i = maxLength; i < bits.Length; i++)
                bits[i] = false;

            return bits;
        }

        private static bool[] SubArray(bool[] bits, int offset, int length)
        {
            var result = new bool[length];

            for (var i = 0; i < length; i++)
                result[i] = bits[offset + i];

            return result;
        }

        private static bool[] CharToBits(char chr)
        {
            var intValue = Mapping.IndexOf(chr);
            var bits = new BitVector32(intValue);

            var result = new bool[6];

            result[5] = bits[1];
            result[4] = bits[2];
            result[3] = bits[4];
            result[2] = bits[8];
            result[1] = bits[16];
            result[0] = bits[32];

            return result;
        }

        private static int[] BitsToInt(bool[] bits)
        {
            var maxLength = bits.Length / 6;

            var result = new int[maxLength];

            for(var i = 0; i < maxLength; i++)
            {
                var offset = i * 6;

                var tempBits = new bool[8];

                for (var j = 0; j < 6; j++)
                    tempBits[5 - j] = bits[offset + j];

                var tempInt = new int[1];
                var bitArray = new BitArray(tempBits);
                bitArray.CopyTo(tempInt, 0);

                result[i] = tempInt[0];
            }

            return result;
        }

        private static long BitsToLong(bool[] bits)
        {
            var maxLength = bits.Length;
            long result = 0;

            for(var i = 0; i < maxLength; i++)
            {
                result <<= 1;
                if (bits[i])
                    result += 1;
            }

            return result;
        }

        private static string IntsToString(int[] ints)
        {
            var result = new StringBuilder();
            foreach (var num in ints)
                result.Append(ASCIIEncoding[num]);

            return result.ToString();
        }
    }
}