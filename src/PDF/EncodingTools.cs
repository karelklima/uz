using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
//using ComponentAce.Compression.Libs.zlib;
using System.util.zlib;
using System.Globalization;
using UZ.System.util.collections;
using UZ.PDF.Font;

namespace UZ.PDF
{
    class EncodingTools
    {

        static char[] winansiEncoding = {
        (char)0, (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10, (char)11, (char)12, (char)13, (char)14, (char)15,
        (char)16, (char)17, (char)18, (char)19, (char)20, (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30, (char)31,
        (char)32, (char)33, (char)34, (char)35, (char)36, (char)37, (char)38, (char)39, (char)40, (char)41, (char)42, (char)43, (char)44, (char)45, (char)46, (char)47,
        (char)48, (char)49, (char)50, (char)51, (char)52, (char)53, (char)54, (char)55, (char)56, (char)57, (char)58, (char)59, (char)60, (char)61, (char)62, (char)63,
        (char)64, (char)65, (char)66, (char)67, (char)68, (char)69, (char)70, (char)71, (char)72, (char)73, (char)74, (char)75, (char)76, (char)77, (char)78, (char)79,
        (char)80, (char)81, (char)82, (char)83, (char)84, (char)85, (char)86, (char)87, (char)88, (char)89, (char)90, (char)91, (char)92, (char)93, (char)94, (char)95,
        (char)96, (char)97, (char)98, (char)99, (char)100, (char)101, (char)102, (char)103, (char)104, (char)105, (char)106, (char)107, (char)108, (char)109, (char)110, (char)111,
        (char)112, (char)113, (char)114, (char)115, (char)116, (char)117, (char)118, (char)119, (char)120, (char)121, (char)122, (char)123, (char)124, (char)125, (char)126, (char)127,
        (char)8364, (char)65533, (char)8218, (char)402, (char)8222, (char)8230, (char)8224, (char)8225, (char)710, (char)8240, (char)352, (char)8249, (char)338, (char)65533, (char)381, (char)65533,
        (char)65533, (char)8216, (char)8217, (char)8220, (char)8221, (char)8226, (char)8211, (char)8212, (char)732, (char)8482, (char)353, (char)8250, (char)339, (char)65533, (char)382, (char)376,
        (char)160, (char)161, (char)162, (char)163, (char)164, (char)165, (char)166, (char)167, (char)168, (char)169, (char)170, (char)171, (char)172, (char)173, (char)174, (char)175,
        (char)176, (char)177, (char)178, (char)179, (char)180, (char)181, (char)182, (char)183, (char)184, (char)185, (char)186, (char)187, (char)188, (char)189, (char)190, (char)191,
        (char)192, (char)193, (char)194, (char)195, (char)196, (char)197, (char)198, (char)199, (char)200, (char)201, (char)202, (char)203, (char)204, (char)205, (char)206, (char)207,
        (char)208, (char)209, (char)210, (char)211, (char)212, (char)213, (char)214, (char)215, (char)216, (char)217, (char)218, (char)219, (char)220, (char)221, (char)222, (char)223,
        (char)224, (char)225, (char)226, (char)227, (char)228, (char)229, (char)230, (char)231, (char)232, (char)233, (char)234, (char)235, (char)236, (char)237, (char)238, (char)239,
        (char)240, (char)241, (char)242, (char)243, (char)244, (char)245, (char)246, (char)247, (char)248, (char)249, (char)250, (char)251, (char)252, (char)253, (char)254, (char)255};

        internal static char[] winansiByteToChar = {
        (char)0, (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10, (char)11, (char)12, (char)13, (char)14, (char)15,
        (char)16, (char)17, (char)18, (char)19, (char)20, (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30, (char)31,
        (char)32, (char)33, (char)34, (char)35, (char)36, (char)37, (char)38, (char)39, (char)40, (char)41, (char)42, (char)43, (char)44, (char)45, (char)46, (char)47,
        (char)48, (char)49, (char)50, (char)51, (char)52, (char)53, (char)54, (char)55, (char)56, (char)57, (char)58, (char)59, (char)60, (char)61, (char)62, (char)63,
        (char)64, (char)65, (char)66, (char)67, (char)68, (char)69, (char)70, (char)71, (char)72, (char)73, (char)74, (char)75, (char)76, (char)77, (char)78, (char)79,
        (char)80, (char)81, (char)82, (char)83, (char)84, (char)85, (char)86, (char)87, (char)88, (char)89, (char)90, (char)91, (char)92, (char)93, (char)94, (char)95,
        (char)96, (char)97, (char)98, (char)99, (char)100, (char)101, (char)102, (char)103, (char)104, (char)105, (char)106, (char)107, (char)108, (char)109, (char)110, (char)111,
        (char)112, (char)113, (char)114, (char)115, (char)116, (char)117, (char)118, (char)119, (char)120, (char)121, (char)122, (char)123, (char)124, (char)125, (char)126, (char)127,
        (char)8364, (char)65533, (char)8218, (char)402, (char)8222, (char)8230, (char)8224, (char)8225, (char)710, (char)8240, (char)352, (char)8249, (char)338, (char)65533, (char)381, (char)65533,
        (char)65533, (char)8216, (char)8217, (char)8220, (char)8221, (char)8226, (char)8211, (char)8212, (char)732, (char)8482, (char)353, (char)8250, (char)339, (char)65533, (char)382, (char)376,
        (char)160, (char)161, (char)162, (char)163, (char)164, (char)165, (char)166, (char)167, (char)168, (char)169, (char)170, (char)171, (char)172, (char)173, (char)174, (char)175,
        (char)176, (char)177, (char)178, (char)179, (char)180, (char)181, (char)182, (char)183, (char)184, (char)185, (char)186, (char)187, (char)188, (char)189, (char)190, (char)191,
        (char)192, (char)193, (char)194, (char)195, (char)196, (char)197, (char)198, (char)199, (char)200, (char)201, (char)202, (char)203, (char)204, (char)205, (char)206, (char)207,
        (char)208, (char)209, (char)210, (char)211, (char)212, (char)213, (char)214, (char)215, (char)216, (char)217, (char)218, (char)219, (char)220, (char)221, (char)222, (char)223,
        (char)224, (char)225, (char)226, (char)227, (char)228, (char)229, (char)230, (char)231, (char)232, (char)233, (char)234, (char)235, (char)236, (char)237, (char)238, (char)239,
        (char)240, (char)241, (char)242, (char)243, (char)244, (char)245, (char)246, (char)247, (char)248, (char)249, (char)250, (char)251, (char)252, (char)253, (char)254, (char)255};

        internal static char[] pdfEncodingByteToChar = {
        (char)0, (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10, (char)11, (char)12, (char)13, (char)14, (char)15,
        (char)16, (char)17, (char)18, (char)19, (char)20, (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30, (char)31,
        (char)32, (char)33, (char)34, (char)35, (char)36, (char)37, (char)38, (char)39, (char)40, (char)41, (char)42, (char)43, (char)44, (char)45, (char)46, (char)47,
        (char)48, (char)49, (char)50, (char)51, (char)52, (char)53, (char)54, (char)55, (char)56, (char)57, (char)58, (char)59, (char)60, (char)61, (char)62, (char)63,
        (char)64, (char)65, (char)66, (char)67, (char)68, (char)69, (char)70, (char)71, (char)72, (char)73, (char)74, (char)75, (char)76, (char)77, (char)78, (char)79,
        (char)80, (char)81, (char)82, (char)83, (char)84, (char)85, (char)86, (char)87, (char)88, (char)89, (char)90, (char)91, (char)92, (char)93, (char)94, (char)95,
        (char)96, (char)97, (char)98, (char)99, (char)100, (char)101, (char)102, (char)103, (char)104, (char)105, (char)106, (char)107, (char)108, (char)109, (char)110, (char)111,
        (char)112, (char)113, (char)114, (char)115, (char)116, (char)117, (char)118, (char)119, (char)120, (char)121, (char)122, (char)123, (char)124, (char)125, (char)126, (char)127,
        (char)0x2022, (char)0x2020, (char)0x2021, (char)0x2026, (char)0x2014, (char)0x2013, (char)0x0192, (char)0x2044, (char)0x2039, (char)0x203a, (char)0x2212, (char)0x2030, (char)0x201e, (char)0x201c, (char)0x201d, (char)0x2018,
        (char)0x2019, (char)0x201a, (char)0x2122, (char)0xfb01, (char)0xfb02, (char)0x0141, (char)0x0152, (char)0x0160, (char)0x0178, (char)0x017d, (char)0x0131, (char)0x0142, (char)0x0153, (char)0x0161, (char)0x017e, (char)65533,
        (char)0x20ac, (char)161, (char)162, (char)163, (char)164, (char)165, (char)166, (char)167, (char)168, (char)169, (char)170, (char)171, (char)172, (char)173, (char)174, (char)175,
        (char)176, (char)177, (char)178, (char)179, (char)180, (char)181, (char)182, (char)183, (char)184, (char)185, (char)186, (char)187, (char)188, (char)189, (char)190, (char)191,
        (char)192, (char)193, (char)194, (char)195, (char)196, (char)197, (char)198, (char)199, (char)200, (char)201, (char)202, (char)203, (char)204, (char)205, (char)206, (char)207,
        (char)208, (char)209, (char)210, (char)211, (char)212, (char)213, (char)214, (char)215, (char)216, (char)217, (char)218, (char)219, (char)220, (char)221, (char)222, (char)223,
        (char)224, (char)225, (char)226, (char)227, (char)228, (char)229, (char)230, (char)231, (char)232, (char)233, (char)234, (char)235, (char)236, (char)237, (char)238, (char)239,
        (char)240, (char)241, (char)242, (char)243, (char)244, (char)245, (char)246, (char)247, (char)248, (char)249, (char)250, (char)251, (char)252, (char)253, (char)254, (char)255};

        internal static IntHashtable winansi = new IntHashtable();
        internal static IntHashtable pdfEncoding = new IntHashtable();

        public const char ACCENT_ACUTE = '´';
        public const char ACCENT_CARON = 'ˇ';
        public const char ACCENT_CARON2 = '’';
        public const char ACCENT_RING = '˚';

        private static Dictionary<string, int> IANAEncodingMap;

        static EncodingTools()
        {
            for (int k = 128; k < 161; ++k)
            {
                char c = winansiByteToChar[k];
                if (c != 65533)
                    winansi[c] = k;
            }

            for (int k = 128; k < 161; ++k)
            {
                char c = pdfEncodingByteToChar[k];
                if (c != 65533)
                    pdfEncoding[c] = k;
            }

            IANAEncodingMap = new Dictionary<string, int>();
            IANAEncodingMap.Add("MACROMANENCODING", 10000);
        }

        public static NumberFormatInfo NumberFormat
        {
            get { return NumberFormatInfo.InvariantInfo; }
        }

        public static CultureInfo CultureFormat
        {
            get { return CultureInfo.InvariantCulture; }
        }

        public static byte[] FlateDecode(byte[] input)
        {
            byte[] b = FlateDecode(input, true);
            //if (b.Length < 1 && input.Length > 0)
            //    return FlateDecode(input, false);
            return b;
        }

        public static byte[] FlateDecode(byte[] input, bool skipHeader)
        {
            byte[] b = FlateDecode(input, true, skipHeader);
            if (b == null)
                return FlateDecode(input, false, skipHeader);
            return b;
        }

        public static byte[] FlateDecode(byte[] input, bool strict, bool skipHeader)
        {
            MemoryStream inputStream = new MemoryStream(input);

                
            if (skipHeader)
            {
                inputStream.ReadByte();
                inputStream.ReadByte(); // skip ZLIB header
            }

            DeflateStream decoder = new DeflateStream(inputStream, CompressionMode.Decompress);

            //ZInputStream decoder = new ZInputStream(inputStream);

            //ZInflaterInputStream decoder = new ZInflaterInputStream(inputStream);
           

            MemoryStream decoded = new MemoryStream();
            try
            {
                byte[] buffer = new byte[strict ? 4092 : 1];
                while (true)
                {
                    int read = decoder.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        break;
                    decoded.Write(buffer, 0, read);
                }
                
            }
            catch
            {
                if (strict)
                    return null;
            }

            return decoded.ToArray();

        }

        public static byte[] LZWDecode(byte[] input)
        {
            MemoryStream output = new MemoryStream();
            LZWDecoder lzw = new LZWDecoder();
            lzw.Decode(input, output);
            return output.ToArray();
        }

        public static bool IsWhitespace(int Char)
        {
            return (Char == 0 || Char == 9 || Char == 10 || Char == 12 || Char == 13 || Char == 32);
        }

        public static byte[] ASCII85Decode(byte[] input)
        {
            MemoryStream output = new MemoryStream();
            int state = 0;
            int[] ChN = new int[5];
            for (int k = 0; k < input.Length; ++k)
            {
                int Char = input[k] & 0xff;
                if (Char == '~')
                    break;
                if (IsWhitespace(Char))
                    continue;
                if (Char == 'z' && state == 0)
                {
                    output.WriteByte(0);
                    output.WriteByte(0);
                    output.WriteByte(0);
                    output.WriteByte(0);
                    continue;
                }
                if (Char < '!' || Char > 'u')
                    throw new ArgumentException("ASCII85decode: illegal character found");
                ChN[state] = Char - '!';
                ++state;
                if (state == 5)
                {
                    state = 0;
                    int rx = 0;
                    for (int j = 0; j < 5; ++j)
                        rx = rx * 85 + ChN[j];
                    output.WriteByte((byte)(rx >> 24));
                    output.WriteByte((byte)(rx >> 16));
                    output.WriteByte((byte)(rx >> 8));
                    output.WriteByte((byte)rx);
                }
            }
            int r = 0;
            if (state == 2)
            {
                r = ChN[0] * 85 * 85 * 85 * 85 + ChN[1] * 85 * 85 * 85 + 85 * 85 * 85 + 85 * 85 + 85;
                output.WriteByte((byte)(r >> 24));
            }
            else if (state == 3)
            {
                r = ChN[0] * 85 * 85 * 85 * 85 + ChN[1] * 85 * 85 * 85 + ChN[2] * 85 * 85 + 85 * 85 + 85;
                output.WriteByte((byte)(r >> 24));
                output.WriteByte((byte)(r >> 16));
            }
            else if (state == 4)
            {
                r = ChN[0] * 85 * 85 * 85 * 85 + ChN[1] * 85 * 85 * 85 + ChN[2] * 85 * 85 + ChN[3] * 85 + 85;
                output.WriteByte((byte)(r >> 24));
                output.WriteByte((byte)(r >> 16));
                output.WriteByte((byte)(r >> 8));
            }
            return output.ToArray();
        }

        public static byte[] DecodePredictor(byte[] input, int predictor, int columns = 1, int colors = 1, int bpc = 8)
        {
            if (predictor < 10)
                return input;
            
            MemoryStream dataStream = new MemoryStream(input);
            MemoryStream fout = new MemoryStream(input.Length);

            int bytesPerPixel = colors * bpc / 8;
            int bytesPerRow = (colors * columns * bpc + 7) / 8;
            byte[] curr = new byte[bytesPerRow];
            byte[] prior = new byte[bytesPerRow];

            // Decode rows
            while (true)
            {
                int filter = 0;
                try
                {
                    filter = dataStream.ReadByte();
                    if (filter < 0)
                        return fout.ToArray();

                    int total = 0;
                    while (total < bytesPerRow)
                    {
                        int n = dataStream.Read(curr, total, bytesPerRow - total);
                        if (n <= 0)
                            return fout.ToArray();
                        total += n;
                    }
                }
                catch
                {
                    return fout.ToArray();
                }

                switch (filter)
                {
                    case 0: //PNG_FILTER_NONE
                        break;
                    case 1: //PNG_FILTER_SUB
                        for (int i = bytesPerPixel; i < bytesPerRow; i++)
                            curr[i] += curr[i - bytesPerPixel];
                        break;
                    case 2: //PNG_FILTER_UP
                        for (int i = 0; i < bytesPerRow; i++)
                            curr[i] += prior[i];
                        break;
                    case 3: //PNG_FILTER_AVERAGE
                        for (int i = 0; i < bytesPerPixel; i++)
                            curr[i] += (byte)(prior[i] / 2);
                        for (int i = bytesPerPixel; i < bytesPerRow; i++)
                            curr[i] += (byte)(((curr[i - bytesPerPixel] & 0xff) + (prior[i] & 0xff)) / 2);
                        break;
                    case 4: //PNG_FILTER_PAETH
                        for (int i = 0; i < bytesPerPixel; i++)
                            curr[i] += prior[i];

                        for (int i = bytesPerPixel; i < bytesPerRow; i++)
                        {
                            int a = curr[i - bytesPerPixel] & 0xff;
                            int b = prior[i] & 0xff;
                            int c = prior[i - bytesPerPixel] & 0xff;

                            int p = a + b - c;
                            int pa = Math.Abs(p - a);
                            int pb = Math.Abs(p - b);
                            int pc = Math.Abs(p - c);

                            int ret;

                            if ((pa <= pb) && (pa <= pc))
                            {
                                ret = a;
                            }
                            else if (pb <= pc)
                            {
                                ret = b;
                            }
                            else
                            {
                                ret = c;
                            }
                            curr[i] += (byte)(ret);
                        }
                        break;
                    default:
                        throw new PdfException("Unknown filter type");
                }
                fout.Write(curr, 0, curr.Length);

                // Swap
                byte[] tmp = prior;
                prior = curr;
                curr = tmp;
            }
        }

        public static byte[] StringToBytes(string Input)
        {
            if (Input == null)
                return new byte[0];

            byte[] Output = new byte[Input.Length];
            for (int k = 0; k < Input.Length; ++k)
                Output[k] = (byte)Input[k];
            return Output;
        }

        public static string BytesToString(byte[] Input) {
            if (Input == null)
                return null;
            
            char[] Chars = new char[Input.Length];
            for (int k = 0; k < Input.Length; ++k)
                Chars[k] = (char)(Input[k] & 0xff);
            return new String(Chars);
        }

        public static byte[] ConvertToBytes(char char1, String encoding)
        {
            if (encoding == null || encoding.Length == 0)
                return new byte[] { (byte)char1 };
            IntHashtable hash = null;
            if (encoding.Equals(BaseFont.WINANSI))
                hash = winansi;
            else if (encoding.Equals("PDF"))
                hash = pdfEncoding;
            if (hash != null)
            {
                int c = 0;
                if (char1 < 128 || (char1 > 160 && char1 <= 255))
                    c = char1;
                else
                    c = hash[char1];
                if (c != 0)
                    return new byte[] { (byte)c };
                else
                    return new byte[0];
            }
            Encoding encw = IanaEncodings.GetEncodingEncoding(encoding);
            byte[] preamble = encw.GetPreamble();
            char[] text = new char[] { char1 };
            if (preamble.Length == 0)
                return encw.GetBytes(text);
            byte[] encoded = encw.GetBytes(text);
            byte[] total = new byte[encoded.Length + preamble.Length];
            Array.Copy(preamble, 0, total, 0, preamble.Length);
            Array.Copy(encoded, 0, total, preamble.Length, encoded.Length);
            return total;
        }

        public static byte[] ConvertToBytes(string text, string encoding)
        {
            if (text == null)
                return new byte[0];
            if (encoding == null || encoding.Length == 0)
            {
                int len = text.Length;
                byte[] b = new byte[len];
                for (int k = 0; k < len; ++k)
                    b[k] = (byte)text[k];
                return b;
            }
            
            IntHashtable hash = null;
            if (encoding.Equals(BaseFont.CP1252))
                hash = winansi;
            else if (encoding.Equals("PDF"))
                hash = pdfEncoding;
            if (hash != null)
            {
                char[] cc = text.ToCharArray();
                int len = cc.Length;
                int ptr = 0;
                byte[] b = new byte[len];
                int c = 0;
                for (int k = 0; k < len; ++k)
                {
                    char char1 = cc[k];
                    if (char1 < 128 || (char1 > 160 && char1 <= 255))
                        c = char1;
                    else
                        c = hash[char1];
                    if (c != 0)
                        b[ptr++] = (byte)c;
                }
                if (ptr == len)
                    return b;
                byte[] b2 = new byte[ptr];
                Array.Copy(b, 0, b2, 0, ptr);
                return b2;
            }
            Encoding encw = IanaEncodings.GetEncodingEncoding(encoding);
            byte[] preamble = encw.GetPreamble();
            if (preamble.Length == 0)
                return encw.GetBytes(text);
            byte[] encoded = encw.GetBytes(text);
            byte[] total = new byte[encoded.Length + preamble.Length];
            Array.Copy(preamble, 0, total, 0, preamble.Length);
            Array.Copy(encoded, 0, total, preamble.Length, encoded.Length);
            return total;
        }

        public static byte[] UnicodeToBytes(string input)
        {
            return Encoding.BigEndianUnicode.GetBytes(input);
        }

        public static string BytesToUnicode(byte[] bytes)
        {
            if (bytes.Length == 1)
                return Convert.ToString((char)bytes[0]);
            else
                return Encoding.BigEndianUnicode.GetString(bytes);
        }

        public static bool IsAccent(char input)
        {
            return input == ACCENT_ACUTE || input == ACCENT_CARON || input == ACCENT_CARON2 || input == ACCENT_RING;
        }

        public static string FixDiacritics(string input)
        {
            if (input.Length < 2)
                return input;

            string diaCommaIn = "aeıouyAEIOUY";
            string diaCommaOut = "áéíóúýÁÉÍÓÚÝ";
            string diaHookIn  = "escrznESCRZDTN";
            string diaHookOut = "ěščřžňĚŠČŘŽĎŤŇ";
            string diaHook2In = "dt";
            string diaHook2Out = "ďť";
            string diaCircleIn = "uU";
            string diaCircleOut = "ůŮ";
            StringBuilder output = new StringBuilder();
            int p1 = 0;
            int p2 = 1;
            while (p2 < input.Length)
            {
                int pos = -1;
                char fix = input[p1];
                switch (input[p2])
                {
                    case ACCENT_ACUTE:
                        pos = diaCommaIn.IndexOf(input[p1]);
                        fix = pos >= 0 ? diaCommaOut[pos] : fix;
                        break;
                    case ACCENT_CARON:
                        pos = diaHookIn.IndexOf(input[p1]);
                        fix = pos >= 0 ? diaHookOut[pos] : fix;
                        break;
                    case ACCENT_CARON2:
                        pos = diaHook2In.IndexOf(input[p1]);
                        fix = pos >= 0 ? diaHook2Out[pos] : fix;
                        break;
                    case ACCENT_RING:
                        pos = diaCircleIn.IndexOf(input[p1]);
                        fix = pos >= 0 ? diaCircleOut[pos] : fix;
                        break;
                }
                if (input[p1] != fix)
                {
                    output.Append(fix);
                    p1 = p2 + 1;
                    p2 = p1 + 1;
                }
                else
                {
                    output.Append(input[p1]);
                    p1++;
                    p2++;
                }

            }

            if (p1 < input.Length)
                output.Append(input[input.Length - 1]);

            return output.ToString();
            
        }

        public static int GetHexValue(int value)
        {
            if (value >= '0' && value <= '9')
                return value - '0';
            if (value >= 'A' && value <= 'F')
                return value - 'A' + 10;
            if (value >= 'a' && value <= 'f')
                return value - 'a' + 10;
            return -1;
        }

        public static string DecodeName(string name)
        {
            /*StringBuilder buffer = new StringBuilder();
            int length = name.Length;
            for (int k = 1; k < length; ++k)
            {
                char Char = name[k];
                if (Char == '#')
                {
                    Char = (char)((GetHexValue(name[k + 1]) << 4) + GetHexValue(name[k + 2]));
                    k += 2;
                }
                buffer.Append(Char);
            }
            return buffer.ToString();*/
            StringBuilder buf = new StringBuilder();
            int len = name.Length;
            for (int k = 1; k < len; ++k)
            {
                char c = name[k];
                if (c == '#')
                {
                    c = (char)((Tokenizer.GetHex(name[k + 1]) << 4) + Tokenizer.GetHex(name[k + 2]));
                    k += 2;
                }
                buf.Append(c);
            }
            return buf.ToString();
        }

        /*public static string ConvertToString(byte[] bytes, string encoding)
        {
            if (encoding == null || encoding.Length == 0)
            {
                char[] Chars = new char[bytes.Length];
                for (int k = 0; k < bytes.Length; ++k)
                    Chars[k] = (char)(bytes[k] & 0xff);
                return new String(Chars);
            }
            
            if ("WinAnsiEncoding".Equals(encoding) || "Cp1252".Equals(encoding))
            {
                //throw new PdfException("WinAnsi and PDF encodings not supported");
                
                int length = bytes.Length;
                char[] CharArray = new char[length];
                for (int k = 0; k < length; ++k)
                {
                    CharArray[k] = winansiEncoding[bytes[k] & 0xff];
                }
                return new String(CharArray);
            }
            String encodingUppercase = encoding.ToUpper(CultureFormat);
            Encoding encodingObject = null;
            if (encodingUppercase.Equals("UNICODEBIGUNMARKED"))
                encodingObject = new UnicodeEncoding(true, false);
            else if (encodingUppercase.Equals("UNICODELITTLEUNMARKED"))
                encodingObject = new UnicodeEncoding(false, false);
            if (encodingObject != null)
                return encodingObject.GetString(bytes);
            bool marker = false;
            bool big = false;
            int offset = 0;
            if (bytes.Length >= 2)
            {
                if (bytes[0] == (byte)254 && bytes[1] == (byte)255)
                {
                    marker = true;
                    big = true;
                    offset = 2;
                }
                else if (bytes[0] == (byte)255 && bytes[1] == (byte)254)
                {
                    marker = true;
                    big = false;
                    offset = 2;
                }
            }
            if (encodingUppercase.Equals("UNICODEBIG"))
                encodingObject = new UnicodeEncoding(marker ? big : true, false);
            else if (encodingUppercase.Equals("UNICODELITTLE"))
                encodingObject = new UnicodeEncoding(marker ? big : false, false);
            if (encodingObject != null)
                return encodingObject.GetString(bytes, offset, bytes.Length - offset);

            //throw new PdfException("Unknown encoding found");
            return GetIANAEncoding(encoding).GetString(bytes);
        }*/

        public static string ConvertToString(byte[] bytes, string encoding)
        {
            if (bytes == null)
                return "";
            if (encoding == null || encoding.Length == 0)
            {
                char[] c = new char[bytes.Length];
                for (int k = 0; k < bytes.Length; ++k)
                    c[k] = (char)(bytes[k] & 0xff);
                return new String(c);
            }
            char[] ch = null;
            if (encoding.Equals("Cp1252")) // winansi
                ch = winansiByteToChar;
            else if (encoding.Equals("PDF"))
                ch = pdfEncodingByteToChar;
            if (ch != null)
            {
                int len = bytes.Length;
                char[] c = new char[len];
                for (int k = 0; k < len; ++k)
                {
                    c[k] = ch[bytes[k] & 0xff];
                }
                return new String(c);
            }
            String nameU = encoding.ToUpper(CultureInfo.InvariantCulture);
            Encoding enc = null;
            if (nameU.Equals("UNICODEBIGUNMARKED"))
                enc = new UnicodeEncoding(true, false);
            else if (nameU.Equals("UNICODELITTLEUNMARKED"))
                enc = new UnicodeEncoding(false, false);
            if (enc != null)
                return enc.GetString(bytes);
            bool marker = false;
            bool big = false;
            int offset = 0;
            if (bytes.Length >= 2)
            {
                if (bytes[0] == (byte)254 && bytes[1] == (byte)255)
                {
                    marker = true;
                    big = true;
                    offset = 2;
                }
                else if (bytes[0] == (byte)255 && bytes[1] == (byte)254)
                {
                    marker = true;
                    big = false;
                    offset = 2;
                }
            }
            if (nameU.Equals("UNICODEBIG"))
                enc = new UnicodeEncoding(marker ? big : true, false);
            else if (nameU.Equals("UNICODELITTLE"))
                enc = new UnicodeEncoding(marker ? big : false, false);
            if (enc != null)
                return enc.GetString(bytes, offset, bytes.Length - offset);
            return IanaEncodings.GetEncodingEncoding(encoding).GetString(bytes);
        }

        public static Encoding GetIANAEncoding(string name)
        {
            return IanaEncodings.GetEncodingEncoding(name);
            /*String nameUppercase = name.ToUpper(CultureInfo.InvariantCulture);
            if (nameUppercase.Equals("UNICODEBIGUNMARKED"))
                return new UnicodeEncoding(true, false);
            if (nameUppercase.Equals("UNICODEBIG"))
                return new UnicodeEncoding(true, true);
            if (nameUppercase.Equals("UNICODELITTLEUNMARKED"))
                return new UnicodeEncoding(false, false);
            if (nameUppercase.Equals("UNICODELITTLE"))
                return new UnicodeEncoding(false, true);
            Encoding enc;
            if (IANAEncodingMap.ContainsKey(nameUppercase))
                enc = Encoding.GetEncoding(IANAEncodingMap[nameUppercase], new EncoderReplacementFallback(""), new DecoderReplacementFallback());
            else 
                enc = Encoding.GetEncoding(name, new EncoderReplacementFallback(""), new DecoderReplacementFallback());
            return enc;*/
        }

        public static bool IsPdfDocEncoding(String text)
        {
            if (text == null)
                return true;
            int len = text.Length;
            for (int k = 0; k < len; ++k)
            {
                char char1 = text[k];
                if (char1 < 128 || (char1 > 160 && char1 <= 255))
                    continue;
                if (!pdfEncoding.ContainsKey(char1))
                    return false;
            }
            return true;
        }

    }
}
