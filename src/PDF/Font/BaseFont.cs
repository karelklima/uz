using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UZ.System.util.collections;

namespace UZ.PDF.Font
{
    abstract class BaseFont
    {

        public const string COURIER = "Courier";
        public const string COURIER_BOLD = "Courier-Bold";
        public const string COURIER_OBLIQUE = "Courier-Oblique";
        public const string COURIER_BOLDOBLIQUE = "Courier-BoldOblique";
        public const string HELVETICA = "Helvetica";
        public const string HELVETICA_BOLD = "Helvetica-Bold";
        public const string HELVETICA_OBLIQUE = "Helvetica-Oblique";
        public const string HELVETICA_BOLDOBLIQUE = "Helvetica-BoldOblique";
        public const string SYMBOL = "Symbol";
        public const string TIMES_ROMAN = "Times-Roman";
        public const string TIMES_BOLD = "Times-Bold";
        public const string TIMES_ITALIC = "Times-Italic";
        public const string TIMES_BOLDITALIC = "Times-BoldItalic";
        public const string ZAPFDINGBATS = "ZapfDingbats";

        public const string IDENTITY_H = "Identity-H";
        public const string IDENTITY_V = "Identity-V";
        
        public const string CP1250 = "Cp1250";
        public const string CP1252 = "Cp1252";
        public const string CP1257 = "Cp1257";
        public const string WINANSI = "Cp1252";
        public const string MACROMAN = "MacRoman";

        public const char CID_NEWLINE = '\u7fff';

        public const string RESOURCE_PATH = "UZ.PDF.Font.";

        public const int FONT_TYPE_T1 = 0;
        public const int FONT_TYPE_TT = 1;
        public const int FONT_TYPE_CJK = 2;
        public const int FONT_TYPE_TTUNI = 3;
        public const int FONT_TYPE_DOCUMENT = 4;
        public const int FONT_TYPE_T3 = 5;

        protected bool directTextToByte = false;
        protected bool fastWinansi = true;
        protected bool forceWidthsOutput = false;
        protected bool fontSpecific = true;
        protected bool vertical = false;

        protected int fontType = 1;
        
        protected int[] widths = new int[256];
        protected string[] differences = new string[256];
        protected char[] unicodeDifferences = new char[256];
        protected int[][] charBBoxes = new int[256][];
        
        protected string encoding;
        protected bool embedded;

        protected static Dictionary<String, BaseFont> fontCache = new Dictionary<string, BaseFont>();
        protected static Dictionary<string, String> BuiltinFonts14 = new Dictionary<string, string>();

        protected IntHashtable specialMap;

        static BaseFont()
        {
            foreach (KeyValuePair<string, DefaultFont> pair in DefaultFont.Library)
            {
                BuiltinFonts14.Add(pair.Key, pair.Key);
            }
        }

        protected BaseFont()
        {
        }

        protected static string GetBaseName(string name)
        {
            if (name.EndsWith(",Bold"))
                return name.Substring(0, name.Length - 5);
            else if (name.EndsWith(",Italic"))
                return name.Substring(0, name.Length - 7);
            else if (name.EndsWith(",BoldItalic"))
                return name.Substring(0, name.Length - 11);
            else
                return name;
        }

        public static BaseFont CreateFont(String name, String encoding, bool embedded)
        {
            return CreateFont(name, encoding, embedded, true, null, null, false, false);
        }

        public static BaseFont CreateFont(String name, String encoding, bool embedded, bool cached, byte[] ttfAfm, byte[] pfb, bool noThrow, bool forceRead)
        {
            string nameBase = GetBaseName(name);
            encoding = NormalizeEncoding(encoding);
            bool isBuiltinFonts14 = BuiltinFonts14.ContainsKey(name);
            bool isCJKFont = isBuiltinFonts14 ? false : CJKFont.IsCJKFont(nameBase, encoding);
            if (isBuiltinFonts14 || isCJKFont)
                embedded = false;
            else if (encoding.Equals(IDENTITY_H) || encoding.Equals(IDENTITY_V))
                embedded = true;
            BaseFont fontFound = null;
            BaseFont fontBuilt = null;
            string key = name + "\n" + encoding + "\n" + embedded;
            if (cached)
            {
                lock (fontCache)
                {
                    fontCache.TryGetValue(key, out fontFound);
                }
                if (fontFound != null)
                    return fontFound;
            }
            if (isBuiltinFonts14 || name.ToLower(CultureInfo.InvariantCulture).EndsWith(".afm") || name.ToLower(CultureInfo.InvariantCulture).EndsWith(".pfm"))
            {
                throw new NotImplementedException();
                //fontBuilt = new Type1Font(name, encoding, embedded, ttfAfm, pfb, forceRead);
                //fontBuilt.fastWinansi = encoding.Equals(CP1252);
            }
            else if (nameBase.ToLower(CultureInfo.InvariantCulture).EndsWith(".ttf") || nameBase.ToLower(CultureInfo.InvariantCulture).EndsWith(".otf") || nameBase.ToLower(CultureInfo.InvariantCulture).IndexOf(".ttc,") > 0)
            {
                throw new NotImplementedException();
                /*if (encoding.Equals(IDENTITY_H) || encoding.Equals(IDENTITY_V))
                    fontBuilt = new TrueTypeFontUnicode(name, encoding, embedded, ttfAfm, forceRead);
                else
                {
                    fontBuilt = new TrueTypeFont(name, encoding, embedded, ttfAfm, false, forceRead);
                    fontBuilt.fastWinansi = encoding.Equals(CP1252);
                }*/
            }
            else if (isCJKFont)
                fontBuilt = new CJKFont(name, encoding, embedded);
            else if (noThrow)
                return null;
            else
                throw new PdfException("Font is not recognized");
            if (cached)
            {
                lock (fontCache)
                {
                    fontCache.TryGetValue(key, out fontFound);
                    if (fontFound != null)
                        return fontFound;
                    fontCache[key] = fontBuilt;
                }
            }
            return fontBuilt;
        }

        protected static string NormalizeEncoding(string enc)
        {
            if (enc.Equals("winansi") || enc.Equals(""))
                return CP1252;
            else if (enc.Equals("macroman"))
                return MACROMAN;
            int n = IanaEncodings.GetEncodingNumber(enc);
            if (n == 1252)
                return CP1252;
            if (n == 10000)
                return MACROMAN;
            return enc;
        }

        protected void CreateEncoding()
        {
            throw new NotImplementedException();
        }

        internal abstract int GetRawWidth(int c, string name);

        public abstract int GetKerning(int char1, int char2);

        public abstract bool SetKerning(int char1, int char2, int kern);

        public virtual int GetWidth(int char1)
        {
            if (fastWinansi)
            {
                if (char1 < 128 || (char1 >= 160 && char1 <= 255))
                    return widths[char1];
                else
                    return widths[EncodingTools.winansi[char1]];
            }
            else
            {
                int total = 0;
                byte[] mbytes = ConvertToBytes((char)char1);
                for (int k = 0; k < mbytes.Length; ++k)
                    total += widths[0xff & mbytes[k]];
                return total;
            }
        }

        public virtual int GetWidth(string text)
        {
            int total = 0;
            if (fastWinansi)
            {
                int len = text.Length;
                for (int k = 0; k < len; ++k)
                {
                    char char1 = text[k];
                    if (char1 < 128 || (char1 >= 160 && char1 <= 255))
                        total += widths[char1];
                    else
                        total += widths[EncodingTools.winansi[char1]];
                }
                return total;
            }
            else
            {
                byte[] mbytes = ConvertToBytes(text);
                for (int k = 0; k < mbytes.Length; ++k)
                    total += widths[0xff & mbytes[k]];
            }
            return total;
        }

        public int GetDescent(String text)
        {
            int min = 0;
            char[] chars = text.ToCharArray();
            for (int k = 0; k < chars.Length; ++k)
            {
                int[] bbox = GetCharBBox(chars[k]);
                if (bbox != null && bbox[1] < min)
                    min = bbox[1];
            }
            return min;
        }

        public int GetAscent(String text)
        {
            int max = 0;
            char[] chars = text.ToCharArray();
            for (int k = 0; k < chars.Length; ++k)
            {
                int[] bbox = GetCharBBox(chars[k]);
                if (bbox != null && bbox[3] > max)
                    max = bbox[3];
            }
            return max;
        }

        public float GetDescentPoint(String text, float fontSize)
        {
            return (float)GetDescent(text) * 0.001f * fontSize;
        }

        public float GetAscentPoint(String text, float fontSize)
        {
            return (float)GetAscent(text) * 0.001f * fontSize;
        }

        public float GetWidthPointKerned(String text, float fontSize)
        {
            float size = (float)GetWidth(text) * 0.001f * fontSize;
            if (!HasKernPairs())
                return size;
            int len = text.Length - 1;
            int kern = 0;
            char[] c = text.ToCharArray();
            for (int k = 0; k < len; ++k)
            {
                kern += GetKerning(c[k], c[k + 1]);
            }
            return size + kern * 0.001f * fontSize;
        }

        public float GetWidthPoint(string text, float fontSize)
        {
            return (float)GetWidth(text) * 0.001f * fontSize;
        }

        public float GetWidthPoint(int char1, float fontSize)
        {
            return GetWidth(char1) * 0.001f * fontSize;
        }

        public virtual byte[] ConvertToBytes(string text)
        {
            if (directTextToByte)
                return EncodingTools.ConvertToBytes(text, null);
            if (specialMap != null)
            {
                byte[] b = new byte[text.Length];
                int ptr = 0;
                int length = text.Length;
                for (int k = 0; k < length; ++k)
                {
                    char c = text[k];
                    if (specialMap.ContainsKey((int)c))
                        b[ptr++] = (byte)specialMap[(int)c];
                }
                if (ptr < length)
                {
                    byte[] b2 = new byte[ptr];
                    Array.Copy(b, 0, b2, 0, ptr);
                    return b2;
                }
                else
                    return b;
            }
            return EncodingTools.ConvertToBytes(text, encoding);
        }

        internal virtual byte[] ConvertToBytes(int char1)
        {
            if (directTextToByte)
                return EncodingTools.ConvertToBytes((char)char1, null);
            if (specialMap != null)
            {
                if (specialMap.ContainsKey(char1))
                    return new byte[] { (byte)specialMap[(int)char1] };
                else
                    return new byte[0];
            }
            return EncodingTools.ConvertToBytes((char)char1, encoding);
        }

        public string Encoding
        {
            get
            {
                return encoding;
            }
        }

        public abstract float GetFontDescriptor(int key, float fontSize);
        public virtual void SetFontDescriptor(int key, float value) { }

        public int FontType
        {
            get
            {
                return fontType;
            }

            set
            {
                fontType = value;
            }
        }

        internal char GetUnicodeDifferences(int index)
        {
            return unicodeDifferences[index];
        }

        public int[] Widths
        {
            get
            {
                return widths;
            }
        }

        public string[] Differences
        {
            get
            {
                return differences;
            }
        }

        public char[] UnicodeDifferences
        {
            get
            {
                return unicodeDifferences;
            }
        }

        public bool ForceWidthsOutput
        {
            set
            {
                this.forceWidthsOutput = value;
            }
            get
            {
                return forceWidthsOutput;
            }
        }

        public bool DirectTextToByte
        {
            set
            {
                this.directTextToByte = value;
            }
            get
            {
                return directTextToByte;
            }
        }

        public virtual int GetUnicodeEquivalent(int c)
        {
            return c;
        }

        public virtual int GetCidCode(int c)
        {
            return c;
        }

        public abstract bool HasKernPairs();

        public virtual bool CharExists(int c)
        {
            byte[] b = ConvertToBytes(c);
            return b.Length > 0;
        }

        public virtual bool SetCharAdvance(int c, int advance)
        {
            byte[] b = ConvertToBytes(c);
            if (b.Length == 0)
                return false;
            widths[0xff & b[0]] = advance;
            return true;
        }

        public virtual int[] GetCharBBox(int c)
        {
            byte[] b = ConvertToBytes(c);
            if (b.Length == 0)
                return null;
            else
                return charBBoxes[b[0] & 0xff];
        }

        protected abstract int[] GetRawCharBBox(int c, String name);

        public void CorrectArabicAdvance()
        {
            for (char c = '\u064b'; c <= '\u0658'; ++c)
                SetCharAdvance(c, 0);
            SetCharAdvance('\u0670', 0);
            for (char c = '\u06d6'; c <= '\u06dc'; ++c)
                SetCharAdvance(c, 0);
            for (char c = '\u06df'; c <= '\u06e4'; ++c)
                SetCharAdvance(c, 0);
            for (char c = '\u06e7'; c <= '\u06e8'; ++c)
                SetCharAdvance(c, 0);
            for (char c = '\u06ea'; c <= '\u06ed'; ++c)
                SetCharAdvance(c, 0);
        }

        public virtual bool IsVertical()
        {
            return vertical;
        }
    }
}
