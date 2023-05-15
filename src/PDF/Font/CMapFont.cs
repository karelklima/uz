using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UZ.PDF.Objects;
using System.IO;
using System.Text.RegularExpressions;
using UZ.System.util.collections;
using UZ.PDF.Font.Cmaps;

namespace UZ.PDF.Font
{
    class CMapFont : Pdf
    {

        private static Regex boldRegex = new Regex("Bold|Adv[A-Z_]+B");
        private static Regex italicRegex = new Regex("Italic|Adv[A-Z_]+I");

        private static int[] standardEncoding = {
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            32,33,34,35,36,37,38,8217,40,41,42,43,44,45,46,47,
            48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,
            64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,
            80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,
            8216,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,
            112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,0,
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            0,161,162,163,8260,165,402,167,164,39,8220,171,8249,8250,64257,64258,
            0,8211,8224,8225,183,0,182,8226,8218,8222,8221,187,8230,8240,0,191,
            0,96,180,710,732,175,728,729,168,0,730,184,0,733,731,711,
            8212,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            0,198,0,170,0,0,0,0,321,216,338,186,0,0,0,0,
            0,230,0,0,0,305,0,0,322,248,339,223,0,0,0,0};

        private PdfDictionary dictionary;

        private string font;
        private string encoding;
        private string subtype;

        private CMapToUnicode toUnicodeCMap;
        private CMapByteCid byteCid;
        private CMapCidUni cidUni;
        private IDictionary<int, int> unicodeToCid;

        private IntHashtable unicodeToByte = new IntHashtable();
        private IntHashtable byteToUnicode = new IntHashtable();
        private IntHashtable differencesMap = new IntHashtable();
        private char[] cidByteToUnicode;

        //private Dictionary<string, int> charSet = new Dictionary<string, int>();

        
        private bool isType0 = false;

        private int[] widths = new int[256];
        private int averageWidth;
        private int spaceWidth;
        private float ascender = 800;
        private float descender = -200;

        private int flags = 0;
        private int italicAngle = 0;
        private int stemV = 0;

        private DefaultFont defaultFont = null;
        private bool useDefaultFont = false;

        public float Ascender { get { return ascender; } }

        public float Descender { get { return descender; } }

        public string Name { get { return font; } }

        public bool IsBold { get { return boldRegex.IsMatch(font) || StemV > 0; } }

        public bool IsItalic { get { return italicRegex.IsMatch(font) || ItalicAngle > 0; } }

        public bool IsHighlighted { get { return IsBold || IsItalic; } }

        public PdfDictionary Dictionary { get { return dictionary; } }

        public int Flags { get { return flags; } }

        public int ItalicAngle { get { return italicAngle; } }

        public int StemV { get { return stemV; } }

        public CMapFont(PdfDictionary dictionary)
        {
            this.dictionary = dictionary;
            this.encoding = "";
            InitBase();
            InitCMap();

            int a = 1;
        }

        private void InitBase()
        {
            this.font = "Unnamed font";
            if (dictionary.ContainsKey("BaseFont"))
                this.font = ((PdfName)dictionary.Get("BaseFont").GetTarget()).Value;

            this.subtype = "Unknown subtype";
            if (dictionary.ContainsKey("Subtype"))
                this.subtype = ((PdfName)dictionary.Get("Subtype").GetTarget()).Value;

            
            if ("Type1".Equals(subtype) || "TrueType".Equals(subtype))
            {
                DoType1TT();
            }
            else if ("Type0".Equals(subtype) && dictionary.ContainsKey("Encoding"))
            {
                isType0 = true;
                string enc = ((PdfName)dictionary.Get("Encoding").Target).Value;

                PdfArray descendantFonts = (PdfArray)dictionary.Get("DescendantFonts").Target;
                PdfDictionary cidFont = (PdfDictionary)descendantFonts.Objects[0].Target;

                LoadFontDescriptor(cidFont);

                int x = 1;

                
            }
            else
            {
                int f = 1;

                throw new PdfException("Unknown font");
            }
        }

        private void InitCMap()
        {

            ProcessToUnicode();
            ProcessUnicodeToByte();

            averageWidth = GetAverageWidth();
            spaceWidth = GetWidth(' ');
            string space = DecodeCID(new byte[] { 32 }, 0, 1);
            if (spaceWidth < 0.01f || (space != null && space.Equals(" ")))
                spaceWidth = averageWidth;
            
        }


        private void DoType1TT()
        {
            CMapToUnicode toUnicode = null;
            if (!dictionary.ContainsKey("Encoding"))
            {
                FillEncoding(null);
                toUnicode = GetToUnicodeCMap();
                if (toUnicode != null)
                {
                    IDictionary<int, int> reverse = toUnicode.CreateReverseMapping();
                    foreach (KeyValuePair<int, int> entry in reverse)
                    {
                        unicodeToByte[entry.Key] = entry.Value;
                        byteToUnicode[entry.Value] = entry.Key;
                    }
                }
            }
            else
            {
                PdfObject encodingObject = dictionary.Get("Encoding").GetTarget();
                if (encodingObject.IsName())
                {
                    FillEncoding(((PdfName)encodingObject).Value);
                }
                else if (encodingObject.IsDictionary())
                {
                    PdfDictionary encodingDictionary = (PdfDictionary)encodingObject;
                    if (encodingDictionary.ContainsKey("BaseEncoding"))
                        FillEncoding(((PdfName)encodingDictionary.Get("BaseEncoding")).Value);
                    else
                        FillEncoding(null);

                    if (encodingDictionary.ContainsKey("Differences"))
                    {
                        PdfArray diffs = (PdfArray)encodingDictionary.Get("Differences");

                        differencesMap = new IntHashtable();
                        int currentNumber = 0;
                        for (int k = 0; k < diffs.Objects.Count; ++k)
                        {
                            PdfObject obj = diffs.Objects[k];
                            if (obj.IsNumber())
                                currentNumber = ((PdfNumber)obj).IntValue;
                            else
                            {
                                string charName = ((PdfName)obj).Value;
                                string name = EncodingTools.DecodeName("/" + charName);
                                int[] c = GlyphList.NameToUnicode(name);

                                if (c != null && c.Length > 0)
                                {
                                    unicodeToByte[c[0]] = currentNumber;
                                    byteToUnicode[currentNumber] = c[0];
                                    differencesMap[c[0]] = currentNumber;
                                }
                                else
                                {
                                    if (toUnicode == null)
                                    {
                                        toUnicode = GetToUnicodeCMap();
                                        if (toUnicode == null)
                                            toUnicode = new CMapToUnicode();
                                    }
                                    string unicode = toUnicode.Lookup(new byte[] { (byte)currentNumber }, 0, 1);
                                    if (unicode != null && unicode.Length == 1)
                                    {
                                        unicodeToByte[unicode[0]] = currentNumber;
                                        byteToUnicode[currentNumber] = unicode[0];
                                        differencesMap[unicode[0]] = currentNumber;
                                    }
                                }
                                ++currentNumber;
                            }
                        }
                    }
                }
            }

            if (DefaultFont.Library.ContainsKey(font))
            {
                //throw new PdfException("Builtin fonts not supported");

                defaultFont = DefaultFont.Library[font];
                useDefaultFont = true;

                int[] keys = unicodeToByte.ToOrderedKeys();
                for (int k = 0; k < keys.Length; ++k)
                {
                    int n = unicodeToByte[keys[k]];
                    widths[n] = defaultFont.GetCharacterWidth(n, GlyphList.UnicodeToName(keys[k]));
                }
                if (differencesMap != null)
                { //widths for differences must override existing ones
                    keys = differencesMap.ToOrderedKeys();
                    for (int k = 0; k < keys.Length; ++k)
                    {
                        int n = differencesMap[keys[k]];
                        widths[n] = defaultFont.GetCharacterWidth(n, GlyphList.UnicodeToName(keys[k]));
                    }
                    differencesMap = null;
                }
            }

            
            if (dictionary.ContainsKey("Widths") && dictionary.ContainsKey("FirstChar") && dictionary.ContainsKey("LastChar"))
            {
                PdfArray widthsSpec = (PdfArray)dictionary.Get("Widths").GetTarget();
                PdfNumber first = (PdfNumber)dictionary.Get("FirstChar").GetTarget();
                PdfNumber last = (PdfNumber)dictionary.Get("LastChar").GetTarget();
                
                int f = first.IntValue;
                int nSize = f + widthsSpec.Objects.Count;
                if (widths.Length < nSize)
                {
                    int[] tmp = new int[nSize];
                    Array.Copy(widths, 0, tmp, 0, f);
                    widths = tmp;
                }
                for (int k = 0; k < widthsSpec.Objects.Count; ++k)
                {
                    widths[f + k] = ((PdfNumber)widthsSpec.Objects[k]).IntValue;
                }
            }

            PdfDictionary fontDesc = null;
            if (dictionary.ContainsKey("FontDescriptor"))
                fontDesc = (PdfDictionary)dictionary.Get("FontDescriptor").Target;

            LoadFontDescriptor(fontDesc);

        }


        private void ProcessToUnicode()
        {
            CMapToUnicode cm = GetToUnicodeCMap();
            if (cm != null)
            {
                toUnicodeCMap = cm;
                unicodeToCid = cm.CreateReverseMapping();
            }
        }

        private void ProcessUnicodeToByte()
        {
            //Dictionary<int, int> uni2byte = unicodeToByte;
            IntHashtable byte2uni = byteToUnicode;

            int[] keys = byteToUnicode.ToOrderedKeys();
            if (keys.Length == 0)
                return;
            cidByteToUnicode = new char[256];
            for (int k = 0; k < keys.Length; ++k)
            {
                int key = keys[k];
                cidByteToUnicode[key] = (char)byteToUnicode[key];
            }
            if (toUnicodeCMap != null)
            {
                IDictionary<int, int> dm = toUnicodeCMap.CreateDirectMapping();
                foreach (KeyValuePair<int, int> kv in dm)
                {
                    if (kv.Key < 256)
                        cidByteToUnicode[kv.Key] = (char)kv.Value;
                }
            }
            
            IntHashtable diffmap = differencesMap;
            if (diffmap != null)
            {
                keys = diffmap.ToOrderedKeys();
                for (int k = 0; k < keys.Length; ++k)
                {
                    int n = diffmap[keys[k]];
                    if (n < 256)
                        cidByteToUnicode[n] = (char)keys[k];
                }
            }
        }

        private CMapToUnicode GetToUnicodeCMap()
        {
            CMapToUnicode cmapRet = null;
            if (dictionary.ContainsKey("ToUnicode"))
            {
                PdfStream toUnicodeReference = (PdfStream)dictionary.Get("ToUnicode").Target;
                /*if (toUnicodeReference != null)
                {
                    CMapParser cmapParser = new CMapParser(((PdfStream)(toUnicodeReference.GetTarget())).Stream);
                    cmap = cmapParser.CMap;
                }*/
                if (toUnicodeReference is PdfStream)
                {
                    try
                    {
                        byte[] touni = toUnicodeReference.Stream;
                        CidLocationFromByte lb = new CidLocationFromByte(touni);
                        cmapRet = new CMapToUnicode();
                        CMapParserEx.ParseCid("", cmapRet, lb);
                        int x = 1;
                    }
                    catch
                    {
                        cmapRet = null;
                    }
                }
            }
            return cmapRet;
        }

        private IntHashtable ReadWidths(PdfArray ws)
        {
            IntHashtable hh = new IntHashtable();
            if (ws == null)
                return hh;
            for (int k = 0; k < ws.Objects.Count; ++k)
            {
                int c1 = ((PdfNumber)ws.Objects[k]).IntValue;
                PdfObject obj = ws.Objects[++k];
                if (obj.IsArray())
                {
                    PdfArray a2 = (PdfArray)obj;
                    for (int j = 0; j < a2.Objects.Count; ++j)
                    {
                        int c2 = ((PdfNumber)a2.Objects[j]).IntValue;
                        hh[c1++] = c2;
                    }
                }
                else
                {
                    int c2 = ((PdfNumber)obj).IntValue;
                    int w = ((PdfNumber)ws.Objects[++k]).IntValue;
                    for (; c1 <= c2; ++c1)
                        hh[c1] = w;
                }
            }
            return hh;
        }

        private void FillEncoding(string encodingName)
        {
            if (encodingName != null && ("MacRomanEncoding".Equals(encodingName) || "WinAnsiEncoding".Equals(encodingName)))
            {
                byte[] b = new byte[256];
                for (int k = 0; k < 256; ++k)
                    b[k] = (byte)k;
                String enc = "Cp1252"; // winansi
                if ("MacRomanEncoding".Equals(encodingName))
                    enc = "MacRoman";
                String converted = EncodingTools.ConvertToString(b, enc);
                char[] charArray = converted.ToCharArray();
                for (int k = 0; k < 256; ++k)
                {
                    unicodeToByte[charArray[k]] = k;
                    byteToUnicode[k] = charArray[k];
                }
            }
            else
            {
                for (int k = 0; k < 256; ++k)
                {
                    unicodeToByte[standardEncoding[k]] = k;
                    byteToUnicode[k] = standardEncoding[k];
                }
            }
        }

        private void LoadFontDescriptor(PdfDictionary fontDescriptor)
        {
            if (fontDescriptor == null)
                return;

            /*if (fontDescriptor.ContainsKey("CharSet"))
            {
                PdfString charSetString = (PdfString)fontDescriptor.Get("CharSet").GetTarget();
                string[] chunks = charSetString.Value.Split('/');
                for (int k = 1; k < chunks.Length; k++)
                {
                    charSet[chunks[k]] = k;
                }
            }*/

            if (fontDescriptor.ContainsKey("Flags"))
                this.flags = ((PdfNumber)fontDescriptor.Get("Flags")).IntValue;

            if (fontDescriptor.ContainsKey("ItalicAngle"))
                this.italicAngle = ((PdfNumber)fontDescriptor.Get("ItalicAngle")).IntValue;

            if (fontDescriptor.ContainsKey("StemV"))
                this.stemV = ((PdfNumber)fontDescriptor.Get("StemV")).IntValue;

            if (fontDescriptor.ContainsKey("FontBBox"))
            {
                PdfArray boxAray = (PdfArray)fontDescriptor.Get("FontBBox");
                float xStart = ((PdfNumber)boxAray.Objects[0]).FloatValue;
                float yStart = ((PdfNumber)boxAray.Objects[1]).FloatValue;
                float xEnd = ((PdfNumber)boxAray.Objects[2]).FloatValue;
                float yEnd = ((PdfNumber)boxAray.Objects[3]).FloatValue;
                if (xStart > xEnd)
                {
                    float temp = xStart;
                    xStart = xEnd;
                    xEnd = temp;
                }
                if (yStart > yEnd)
                {
                    float temp = yStart;
                    yStart = yEnd;
                    yEnd = temp;
                }
                
                ascender = yEnd * 1000f / (yEnd - yStart);
                descender = yStart * 1000f / (yEnd - yStart);
            }
            
        }

        public int GetAverageWidth()
        {
            int count = 0;
            int total = 0;
            for (int i = 0; i < widths.Length; i++)
            {
                if (widths[i] != 0)
                {
                    total += widths[i];
                    count++;
                }
            }
            return count != 0 ? total / count : 0;
        }

        public int GetWidth(int Char)
        {
            if (Char == ' ')
                return spaceWidth;

            if (isType0 && false)
            {
                // TODO
            }

            
            int total = 0;
            byte[] mbytes = ConvertToBytes((char)Char);
            for (int k = 0; k < mbytes.Length; ++k)
                total += widths[0xff & mbytes[k]];
            return total;
        }

        private byte[] ConvertToBytes(int Char)
        {
            if (Char == 269)
            {
                int a = 1;
            }
            if (unicodeToByte.ContainsKey(Char))
                return new byte[] { (byte)unicodeToByte[Char] };
            else
                return new byte[0];
        }

        public int AverageWidth
        {
            get { return averageWidth; }
        }

        public int SpaceWidth
        {
            get { return spaceWidth; }
        }

        private String DecodeCID(byte[] bytes, int offset, int length)
        {
            if (toUnicodeCMap != null)
            {
                if (offset + length > bytes.Length)
                    throw new IndexOutOfRangeException("Invalid CID index");
                string s = toUnicodeCMap.Lookup(bytes, offset, length);
                if (s != null)
                    return s;
                if (length != 1 || cidByteToUnicode == null)
                    return null;
            }

            if (length == 1)
            {
                if (cidByteToUnicode == null)
                    return "";
                else
                    return new String(cidByteToUnicode, 0xff & bytes[offset], 1);
            }

            throw new PdfException("Cannot decode CID");
        }

        public String Decode(byte[] bytes, int offset, int length)
        {
            /*StringBuilder sb = new StringBuilder(); // it's a shame we can't make this StringBuilder
            for (int i = offset; i < offset + length; i++)
            {
                String result = DecodeCID(bytes, i, 1);
                if (result == null && i < offset + length - 1)
                {
                    result = DecodeCID(bytes, i, 2);
                    i++;
                }
                sb.Append(result);
            }

            return sb.ToString();*/
            StringBuilder sb = new StringBuilder();
            if (toUnicodeCMap == null && byteCid != null)
            {
                CMapSequence seq = new CMapSequence(bytes, offset, length);
                String cid = byteCid.DecodeSequence(seq);
                foreach (char ca in cid)
                {
                    int c = cidUni.Lookup(ca);
                    if (c > 0)
                        sb.Append(Utilities.ConvertFromUtf32(c));
                }
            }
            else
            {
                for (int i = offset; i < offset + length; i++)
                {
                    String rslt = DecodeCID(bytes, i, 1);
                    if (rslt == null && i < offset + length - 1)
                    {
                        rslt = DecodeCID(bytes, i, 2);
                        i++;
                    }
                    if (rslt != null)
                        sb.Append(rslt);
                }
            }
            return sb.ToString();
        }

        
    }
}
