using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UZ.PDF.Font
{
    class CMap : Pdf
    {
        private List<CodespaceRange> codespaceRangeList = new List<CodespaceRange>();
        private Dictionary<int, string> singleByteMap = new Dictionary<int, String>();
        private Dictionary<int, string> doubleByteMap = new Dictionary<int, String>();

        public bool IsEmpty
        {
            get
            {
                return singleByteMap.Count < 1 && doubleByteMap.Count < 1;
            }
        }

        public void AddCodespaceRange(CodespaceRange range)
        {
            codespaceRangeList.Add(range);
        }

        public void AddMapping(byte[] key, String value)
        {
            switch (key.Length)
            {
                case 1:
                    singleByteMap[key[0] & 0xff] = value;
                    break;
                case 2:
                    int iKey = key[0] & 0xFF;
                    iKey <<= 8;
                    iKey |= key[1] & 0xFF;
                    doubleByteMap[iKey] = value;
                    break;
                default:
                    Assert(false, "Only one or two byte character mappings supported");
                    break;
            }
        }

        public string Convert(byte[] Input, int offset, int length)
        {
            string Output = null;
            int key = 0;
            if (length == 1)
            {
                key = Input[offset] & 0xff;
                singleByteMap.TryGetValue(key, out Output);
            }
            else if (length == 2)
            {
                key = Input[offset] & 0xff;
                key <<= 8;
                key += Input[offset + 1] & 0xff;

                doubleByteMap.TryGetValue(key, out Output);
            }

            return Output;
        }

        public Dictionary<int, int> CreateReverseMapping()
        {
            Dictionary<int, int> output = new Dictionary<int, int>();
            foreach (KeyValuePair<int, string> entry in singleByteMap)
            {
                output[ConvertToInt(entry.Value)] = entry.Key;
            }
            foreach (KeyValuePair<int, string> entry in doubleByteMap)
            {
                output[ConvertToInt(entry.Value)] = entry.Key;
            }
            return output;
        }

        public Dictionary<int, int> CreateDirectMapping()
        {
            Dictionary<int, int> output = new Dictionary<int, int>();
            foreach (KeyValuePair<int, String> entry in singleByteMap)
            {
                output[entry.Key] = ConvertToInt(entry.Value);
            }
            foreach (KeyValuePair<int, String> entry in doubleByteMap)
            {
                output[entry.Key] = ConvertToInt(entry.Value);
            }
            return output;
        }

        private int ConvertToInt(string s)
        {
            UnicodeEncoding ue = new UnicodeEncoding(true, false);
            byte[] b = ue.GetBytes(s);
            int value = 0;
            for (int i = 0; i < b.Length - 1; i++)
            {
                value += b[i] & 0xff;
                value <<= 8;
            }
            value += b[b.Length - 1] & 0xff;
            return value;
        }
    }
}
