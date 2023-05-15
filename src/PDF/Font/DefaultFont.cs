using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Resources;
using System.Globalization;

namespace UZ.PDF.Font
{
    class DefaultFont : Pdf
    {
        private static Dictionary<string, DefaultFont> library = new Dictionary<string, DefaultFont>();

        public static Dictionary<string, DefaultFont> Library
        {
            get { return library; }
        }

        static DefaultFont()
        {
            AddFont("Courier", Properties.Resources.Courier);
            AddFont("Courier-Bold", Properties.Resources.Courier_Bold);
            AddFont("Courier-Oblique", Properties.Resources.Courier_Oblique);
            AddFont("Courier-BoldOblique", Properties.Resources.Courier_BoldOblique);
            AddFont("Helvetica", Properties.Resources.Helvetica);
            AddFont("Helvetica-Bold", Properties.Resources.Helvetica_Bold);
            AddFont("Helvetica-Oblique", Properties.Resources.Helvetica_Oblique);
            AddFont("Helvetica-BoldOblique", Properties.Resources.Helvetica_BoldOblique);
            AddFont("Symbol", Properties.Resources.Symbol);
            AddFont("Times-Roman", Properties.Resources.Times_Roman);
            AddFont("Times-Bold", Properties.Resources.Times_Bold);
            AddFont("Times-Italic", Properties.Resources.Times_Italic);
            AddFont("Times-BoldItalic", Properties.Resources.Times_BoldItalic);
            AddFont("ZapfDingbats", Properties.Resources.ZapfDingbats);
        }

        private Dictionary<object, object[]> charMetrics = new Dictionary<object, object[]>();
        private Dictionary<string, object[]> kernPairs = new Dictionary<string, object[]>();

        private string encoding = "Cp1252";
        private string encodingScheme;
        private bool fontSpecificEncoding;

        protected int[] widths = new int[256];
        protected string[] differences = new string[256];
        protected char[] unicodeDifferences = new char[256];
        
        

        private static void AddFont(string name, byte[] fontData)
        {
            library.Add(name, new DefaultFont(name, fontData));
        }

        public int[] Widths { get { return widths; } }

        public string[] Differences { get { return differences; } }

        public char[] UnicodeDifferences { get { return unicodeDifferences; } }

        public DefaultFont(string name, byte[] fontData)
        {
            fontSpecificEncoding = true;

            try
            {
                /*FileStream fileHandle = file.OpenRead();
                StreamReader container = new StreamReader(fileHandle);
                String content = container.ReadToEnd();
                fileHandle.Close();

                object fontObj = ResourceManager.GetObject(name, EncodingTools.CultureFormat);
                byte[] data = ((byte[])(fontObj));*/

                string content = EncodingTools.BytesToString(fontData);
                
                StringReader reader = new StringReader(content);
                string line;
                string[] chunks;
                bool hasMetrics = false;
                while (true)
                {
                    line = reader.ReadLine();
                    if (line == null)
                        break;
                    if (line.Length < 1)
                        continue;
                    chunks = line.Split(' ');
                    if (chunks[0] == "EncodingScheme")
                        encodingScheme = chunks[1];
                    if (chunks[0] == "StartCharMetrics")
                    {
                        hasMetrics = true;
                        break;
                    }
                }

                if (!hasMetrics)
                    throw new PdfException("Invalid Default font definition #1");

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length < 1)
                        continue;
                    chunks = line.Split(' ');
                    if (chunks[0].Equals("EndCharMetrics"))
                    {
                        hasMetrics = false;
                        break;
                    }
                    int C = int.Parse(chunks[1], EncodingTools.NumberFormat);
                    int WX = (int)float.Parse(chunks[4], EncodingTools.NumberFormat);
                    string N = chunks[7];
                    int[] B = new int[4];
                    B[0] = int.Parse(chunks[10], EncodingTools.NumberFormat);
                    B[1] = int.Parse(chunks[11], EncodingTools.NumberFormat);
                    B[2] = int.Parse(chunks[12], EncodingTools.NumberFormat);
                    B[3] = int.Parse(chunks[13], EncodingTools.NumberFormat);
                    object[] metrics = new object[] { C, WX, N, B };
                    if (C >= 0)
                        charMetrics[C] = metrics;
                    charMetrics[N] = metrics;
                }

                if (hasMetrics)
                    throw new PdfException("Invalid Default font definition #2");

                if (!charMetrics.ContainsKey("nonbreakingspace") && charMetrics.ContainsKey("space"))
                    charMetrics["nonbreakingspace"] = charMetrics["space"];

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length < 1)
                        continue;
                    chunks = line.Split(' ');
                    if (chunks[0].Equals("EndFontMetrics"))
                        return;
                    if (chunks[0].Equals("StartKernPairs"))
                    {
                        hasMetrics = true;
                        break;
                    }
                }

                if (!hasMetrics)
                    throw new PdfException("Invalid Default font definition #3");

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length < 1)
                        continue;
                    chunks = line.Split(' ');

                    if (!chunks[0].Equals("KPX"))
                        continue;

                    if (chunks[0].Equals("EndKernPairs"))
                    {
                        hasMetrics = false;
                        break;
                    }

                    string first = chunks[1];
                    string second = chunks[2];
                    int width = int.Parse(chunks[3], EncodingTools.NumberFormat);

                    if (!kernPairs.ContainsKey(first))
                        kernPairs[first] = new object[] { second, width };
                    else
                    {
                        object[] relations = kernPairs[first];
                        int n = relations.Length;
                        object[] newRelations = new object[n + 2];
                        Array.Copy(relations, 0, newRelations, 0, n);
                        newRelations[n] = second;
                        newRelations[n + 1] = width;
                        kernPairs[first] = newRelations;
                    }
                }
                if (!hasMetrics)
                    throw new PdfException("Invalid Default font definition #4");

            }
            catch (Exception e)
            {
                throw new PdfException("Unable to read default font file: " + e.Message);
            }
            encodingScheme = encodingScheme.Trim();
            if (encodingScheme.Equals("AdobeStandardEncoding") || encodingScheme.Equals("StandardEncoding"))
            {
                fontSpecificEncoding = false;
            }
            CreateEncoding();
        }

        public int GetCharacterWidth(int c, string name)
        {
            object[] metrics = null;
            if (name == null)
            { // font specific
                if (charMetrics.ContainsKey(c))
                    metrics = charMetrics[c];
            }
            else
            {
                if (name.Equals(".notdef"))
                    return 0;
                if (charMetrics.ContainsKey(name))
                    metrics = charMetrics[name];
            }
            if (metrics != null)
                return (int)metrics[1];
            return 0;
        }

        private void CreateEncoding()
        {
            if (fontSpecificEncoding)
            {
                for (int k = 0; k < 256; ++k)
                {
                    widths[k] = GetCharacterWidth(k, null);
                }
            }
            else
            {
                string s;
                string name;
                char c;
                byte[] b = new byte[1];
                for (int k = 0; k < 256; ++k)
                {
                    b[0] = (byte)k;
                    s = EncodingTools.ConvertToString(b, encoding);
                    if (s.Length > 0)
                    {
                        c = s[0];
                    }
                    else
                    {
                        c = '?';
                    }
                    name = GlyphList.UnicodeToName((int)c);
                    if (name == null)
                        name = ".notdef";
                    differences[k] = name;
                    unicodeDifferences[k] = c;
                    widths[k] = GetCharacterWidth((int)c, name);
                }
            }
        }

        public virtual int GetWidth(int char1)
        {
            int total = 0;
            byte[] mbytes = EncodingTools.ConvertToBytes((char)char1, encoding);
            for (int k = 0; k < mbytes.Length; ++k)
                total += widths[0xff & mbytes[k]];
            return total;
        }

    }
}
