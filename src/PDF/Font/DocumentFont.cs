using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UZ.PDF.Font.Cmaps;
using UZ.PDF.Objects;
using UZ.System.util.collections;

namespace UZ.PDF.Font
{
    class DocumentFont : BaseFont
    {

        // code, [glyph, width]
        private Dictionary<int, int[]> metrics = new Dictionary<int, int[]>();
        private String fontName;
        private PdfIndirectReference refFont;
        private PdfDictionary font;
        private IntHashtable uni2byte = new IntHashtable();
        private IntHashtable byte2uni = new IntHashtable();
        private IntHashtable diffmap;
        public float Ascender = 800;
        public float CapHeight = 700;
        public float Descender = -200;
        private float ItalicAngle = 0;
        private float fontWeight = 0;
        private float llx = -50;
        private float lly = -200;
        private float urx = 100;
        private float ury = 900;
        protected bool isType0 = false;
        protected int defaultWidth = 1000;
        private IntHashtable hMetrics;
        protected internal String cjkEncoding;
        protected internal String uniMap;

        private BaseFont cjkMirror;

        private static int[] stdEnc = {
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

        /** Creates a new instance of DocumentFont */
        internal DocumentFont(PdfDictionary font)
        {
            this.refFont = null;
            this.font = font;
            Init();
        }

        /** Creates a new instance of DocumentFont */
        internal DocumentFont(PdfIndirectReference refFont)
        {
            this.refFont = refFont;
            font = (PdfDictionary)refFont.Target;
            Init();
        }

        private void Init()
        {
            encoding = "";
            fontSpecific = false;
            fontType = FONT_TYPE_DOCUMENT;

            string fontName = "";
            if (font.ContainsKey("BaseFont"))
                fontName = ((PdfName)font.Get("BaseFont").GetTarget()).Value;

            string subType = "Unknown subtype";
            if (font.ContainsKey("Subtype"))
                subType = ((PdfName)font.Get("Subtype").GetTarget()).Value;

            if ("Type1".Equals(subType) || "TrueType".Equals(subType))
                DoType1TT();
            else
            {
                if (font.ContainsKey("Encoding"))
                {
                    String enc = ((PdfName)font.Get("Encoding").GetTarget()).Value;
                    String ffontname = CJKFont.GetCompatibleFont(enc);
                    if (ffontname != null)
                    {
                        cjkMirror = BaseFont.CreateFont(ffontname, enc, false);
                        cjkEncoding = enc;
                        uniMap = ((CJKFont)cjkMirror).UniMap;
                    }
                    if ("Type0".Equals(subType))
                    {
                        isType0 = true;
                        if (!enc.Equals("Identity-H") && cjkMirror != null)
                        {
                            PdfArray df = (PdfArray)font.Get("DescendantFonts").Target;
                            PdfDictionary cidft = (PdfDictionary)df.Objects[0].Target;
                            if (cidft.ContainsKey("DW"))
                                defaultWidth = int.Parse(cidft.Get("DW").Target.Value);
                            
                            hMetrics = ReadWidths((PdfArray)cidft.Get("W").Target);

                            PdfDictionary fontDesc = (PdfDictionary)cidft.Get("FontDescriptor").Target;
                            FillFontDesc(fontDesc);
                        }
                        else
                        {
                            ProcessType0(font);
                        }
                    }
                }
            }
        }

        public PdfDictionary FontDictionary
        {
            get { return font; }
        }

        private void ProcessType0(PdfDictionary font)
        {
            PdfArray df = (PdfArray)font.Get("DescendantFonts").Target;
            PdfDictionary cidft = (PdfDictionary)df.Objects[0].Target;
            
            int dw = 1000;
            if (cidft.ContainsKey("DW"))
                dw = int.Parse(cidft.Get("DW").Target.Value);
            
            IntHashtable widths = ReadWidths((PdfArray)cidft.Get("W").Target);
            PdfDictionary fontDesc = (PdfDictionary)cidft.Get("FontDescriptor").Target;
            FillFontDesc(fontDesc);
            PdfObject toUniObject = null;
            
            if (font.ContainsKey("ToUnicode"))
                toUniObject = font.Get("ToUnicode").Target;
            
            if (toUniObject is PdfStream)
            {
                FillMetrics(((PdfStream)toUniObject).Bytes, widths, dw);
            }
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

        private String DecodeString(PdfString ps)
        {
            if (ps.IsHexWriting())
                return EncodingTools.ConvertToString(ps.Bytes, "UnicodeBigUnmarked");
            else
                return ps.ToUnicodeString();
        }

        private void FillMetrics(byte[] touni, IntHashtable widths, int dw)
        {
            ContentParser ps = new ContentParser(touni);
            PdfObject ob = null;
            bool notFound = true;
            int nestLevel = 0;
            int maxExc = 50;
            while ((notFound || nestLevel > 0))
            {
                try
                {
                    ob = ps.ReadObject();
                    //ob = ps.ReadPRObject();
                }
                catch
                {
                    if (--maxExc < 0)
                        break;
                    continue;
                }
                if (ob == null)
                    break;
                if (ob.Type == PdfObject.ObjectType.COMMAND)
                {
                    if (ob.ToString().Equals("begin"))
                    {
                        notFound = false;
                        nestLevel++;
                    }
                    else if (ob.ToString().Equals("end"))
                    {
                        nestLevel--;
                    }
                    else if (ob.ToString().Equals("beginbfchar"))
                    {
                        while (true)
                        {
                            PdfObject nx = ps.ReadObject();
                            if (nx.ToString().Equals("endbfchar"))
                                break;
                            String cid = DecodeString((PdfString)nx);
                            String uni = DecodeString((PdfString)ps.ReadObject());
                            if (uni.Length == 1)
                            {
                                int cidc = (int)cid[0];
                                int unic = (int)uni[uni.Length - 1];
                                int w = dw;
                                if (widths.ContainsKey(cidc))
                                    w = widths[cidc];
                                metrics[unic] = new int[] { cidc, w };
                            }
                        }
                    }
                    else if (ob.ToString().Equals("beginbfrange"))
                    {
                        while (true)
                        {
                            PdfObject nx = ps.ReadObject();
                            if (nx.ToString().Equals("endbfrange"))
                                break;
                            String cid1 = DecodeString((PdfString)nx);
                            String cid2 = DecodeString((PdfString)ps.ReadObject());
                            int cid1c = (int)cid1[0];
                            int cid2c = (int)cid2[0];
                            PdfObject ob2 = ps.ReadObject();
                            if (ob2.IsString())
                            {
                                String uni = DecodeString((PdfString)ob2);
                                if (uni.Length == 1)
                                {
                                    int unic = (int)uni[uni.Length - 1];
                                    for (; cid1c <= cid2c; cid1c++, unic++)
                                    {
                                        int w = dw;
                                        if (widths.ContainsKey(cid1c))
                                            w = widths[cid1c];
                                        metrics[unic] = new int[] { cid1c, w };
                                    }
                                }
                            }
                            else
                            {
                                PdfArray a = (PdfArray)ob2;
                                for (int j = 0; j < a.Objects.Count; ++j, ++cid1c)
                                {
                                    String uni = DecodeString((PdfString)a.Objects[j]);
                                    if (uni.Length == 1)
                                    {
                                        int unic = (int)uni[uni.Length - 1];
                                        int w = dw;
                                        if (widths.ContainsKey(cid1c))
                                            w = widths[cid1c];
                                        metrics[unic] = new int[] { cid1c, w };
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DoType1TT()
        {
            CMapToUnicode toUnicode = null;
            if (!font.ContainsKey("Encoding"))
            {
                FillEncoding(null);
                toUnicode = ProcessToUnicode();
                if (toUnicode != null)
                {
                    IDictionary<int, int> rm = toUnicode.CreateReverseMapping();
                    foreach (KeyValuePair<int, int> kv in rm)
                    {
                        uni2byte[kv.Key] = kv.Value;
                        byte2uni[kv.Value] = kv.Key;
                    }
                }
            }
            else
            {
                PdfObject enc = font.Get("Encoding");
            
                if (enc.IsName())
                    FillEncoding((PdfName)enc);
                else if (enc.IsDictionary())
                {
                    PdfDictionary encDic = (PdfDictionary)enc;
                    if (!encDic.ContainsKey("BaseEncoding"))
                        FillEncoding(null);
                    else
                        FillEncoding((PdfName)encDic.Get("BaseEncoding"));

                    if (encDic.ContainsKey("Differences"))
                    {
                        PdfArray diffs = (PdfArray)encDic.Get("Differences").Target;
                        diffmap = new IntHashtable();
                        int currentNumber = 0;
                        for (int k = 0; k < diffs.Objects.Count; ++k)
                        {
                            PdfObject obj = diffs.Objects[k];
                            if (obj.IsNumber())
                                currentNumber = ((PdfNumber)obj).IntValue;
                            else
                            {
                                //int[] c = GlyphList.NameToUnicode(PdfName.DecodeName(((PdfName)obj).ToString()));
                                int[] c = GlyphList.NameToUnicode(((PdfName)obj).Value);
                                
                                if (c != null && c.Length > 0)
                                {
                                    uni2byte[c[0]] = currentNumber;
                                    byte2uni[currentNumber] = c[0];
                                    diffmap[c[0]] = currentNumber;
                                }
                                else
                                {
                                    if (toUnicode == null)
                                    {
                                        toUnicode = ProcessToUnicode();
                                        if (toUnicode == null)
                                        {
                                            toUnicode = new CMapToUnicode();
                                        }
                                    }
                                    string unicode = toUnicode.Lookup(new byte[] { (byte)currentNumber }, 0, 1);
                                    if ((unicode != null) && (unicode.Length == 1))
                                    {
                                        this.uni2byte[unicode[0]] = currentNumber;
                                        this.byte2uni[currentNumber] = unicode[0];
                                        this.diffmap[unicode[0]] = currentNumber;
                                    }
                                }
                                ++currentNumber;
                            }
                        }
                    }
                }
            }
            PdfArray newWidths = (PdfArray)font.Get("Widths").Target;
            
            if (fontName != null && BuiltinFonts14.ContainsKey(fontName))
            {
                BaseFont bf = BaseFont.CreateFont(fontName, WINANSI, false);
                int[] e = uni2byte.ToOrderedKeys();
                for (int k = 0; k < e.Length; ++k)
                {
                    int n = uni2byte[e[k]];
                    widths[n] = bf.GetRawWidth(n, GlyphList.UnicodeToName(e[k]));
                }
                if (diffmap != null)
                { //widths for differences must override existing ones
                    e = diffmap.ToOrderedKeys();
                    for (int k = 0; k < e.Length; ++k)
                    {
                        int n = diffmap[e[k]];
                        widths[n] = bf.GetRawWidth(n, GlyphList.UnicodeToName(e[k]));
                    }
                    diffmap = null;
                }
                throw new NotImplementedException();
                /*
                Ascender = bf.GetFontDescriptor(ASCENT, 1000);
                CapHeight = bf.GetFontDescriptor(CAPHEIGHT, 1000);
                Descender = bf.GetFontDescriptor(DESCENT, 1000);
                ItalicAngle = bf.GetFontDescriptor(ITALICANGLE, 1000);
                fontWeight = bf.GetFontDescriptor(FONT_WEIGHT, 1000);
                llx = bf.GetFontDescriptor(BBOXLLX, 1000);
                lly = bf.GetFontDescriptor(BBOXLLY, 1000);
                urx = bf.GetFontDescriptor(BBOXURX, 1000);
                ury = bf.GetFontDescriptor(BBOXURY, 1000);*/
            }
            PdfNumber first = null;
            if (font.ContainsKey("FirstChar"))
                first = (PdfNumber)font.Get("FirstChar").Target;
            PdfNumber last = null;
            if (font.ContainsKey("LastChar"))
                last = (PdfNumber)font.Get("LastChar").Target;

            if (first != null && last != null && newWidths != null)
            {
                int f = first.IntValue;
                int nSize = f + newWidths.Objects.Count;
                if (widths.Length < nSize)
                {
                    int[] tmp = new int[nSize];
                    Array.Copy(widths, 0, tmp, 0, f);
                    widths = tmp;
                }
                for (int k = 0; k < newWidths.Objects.Count; ++k)
                {
                    widths[f + k] = ((PdfNumber)newWidths.Objects[k]).IntValue;
                }
            }
            FillFontDesc((PdfDictionary)font.Get("FontDescriptor").Target);
        }

        private CMapToUnicode ProcessToUnicode()
        {
            CMapToUnicode cmapRet = null;
            PdfObject toUni = null;
            if (font.ContainsKey("ToUnicode"))
                toUni = font.Get("ToUnicode").Target;
            if (toUni != null && toUni is PdfStream)
            {
                try
                {
                    byte[] touni = ((PdfStream)toUni).Bytes;
                    CidLocationFromByte lb = new CidLocationFromByte(touni);
                    cmapRet = new CMapToUnicode();
                    CMapParserEx.ParseCid("", cmapRet, lb);
                }
                catch
                {
                    cmapRet = null;
                }
            }
            return cmapRet;
        }

        private void FillFontDesc(PdfDictionary fontDesc)
        {
            //throw new NotImplementedException();
            
            if (fontDesc == null)
                return;

            if (fontDesc.ContainsKey("Ascent"))
                Ascender = ((PdfNumber)fontDesc.Get("Ascent").Target).FloatValue;
            if (fontDesc.ContainsKey("CapHeight"))
                CapHeight = ((PdfNumber)fontDesc.Get("CapHeight").Target).FloatValue;
            if (fontDesc.ContainsKey("Descent"))
                Descender = ((PdfNumber)fontDesc.Get("Descent").Target).FloatValue;
            if (fontDesc.ContainsKey("ItalicAngle"))
                ItalicAngle = ((PdfNumber)fontDesc.Get("ItalicAngle").Target).FloatValue;
            if (fontDesc.ContainsKey("FontWeight"))
                fontWeight = ((PdfNumber)fontDesc.Get("FontWeight").Target).FloatValue;

            
            if (fontDesc.ContainsKey("FontBBox"))
            {
                PdfArray bbox = (PdfArray)fontDesc.Get("FontBBox").Target;
                llx = ((PdfNumber)bbox.Objects[0]).FloatValue;
                lly = ((PdfNumber)bbox.Objects[1]).FloatValue;
                urx = ((PdfNumber)bbox.Objects[2]).FloatValue;
                ury = ((PdfNumber)bbox.Objects[3]).FloatValue;
                if (llx > urx)
                {
                    float t = llx;
                    llx = urx;
                    urx = t;
                }
                if (lly > ury)
                {
                    float t = lly;
                    lly = ury;
                    ury = t;
                }
            }
            float maxAscent = Math.Max(ury, Ascender);
            float minDescent = Math.Min(lly, Descender);
            Ascender = maxAscent * 1000 / (maxAscent - minDescent);
            Descender = minDescent * 1000 / (maxAscent - minDescent);
        }

        private void FillEncoding(PdfName encoding)
        {
            if ("MacRomanEncoding".Equals(encoding.Value) || "WinAnsiEncoding".Equals(encoding.Value))
            {
                byte[] b = new byte[256];
                for (int k = 0; k < 256; ++k)
                    b[k] = (byte)k;
                String enc = WINANSI;
                if ("MacRomanEncoding".Equals(encoding.Value))
                    enc = MACROMAN;
                String cv = EncodingTools.ConvertToString(b, enc);
                char[] arr = cv.ToCharArray();
                for (int k = 0; k < 256; ++k)
                {
                    uni2byte[arr[k]] = k;
                    byte2uni[k] = arr[k];
                }
            }
            else
            {
                for (int k = 0; k < 256; ++k)
                {
                    uni2byte[stdEnc[k]] = k;
                    byte2uni[k] = stdEnc[k];
                }
            }
        }

        /** Gets the family name of the font. If it is a True Type font
        * each array element will have {Platform ID, Platform Encoding ID,
        * Language ID, font name}. The interpretation of this values can be
        * found in the Open Type specification, chapter 2, in the 'name' table.<br>
        * For the other fonts the array has a single element with {"", "", "",
        * font name}.
        * @return the family name of the font
        *
        */
        

        /** Gets the font parameter identified by <CODE>key</CODE>. Valid values
        * for <CODE>key</CODE> are <CODE>ASCENT</CODE>, <CODE>CAPHEIGHT</CODE>, <CODE>DESCENT</CODE>,
        * <CODE>ITALICANGLE</CODE>, <CODE>BBOXLLX</CODE>, <CODE>BBOXLLY</CODE>, <CODE>BBOXURX</CODE>
        * and <CODE>BBOXURY</CODE>.
        * @param key the parameter to be extracted
        * @param fontSize the font size in points
        * @return the parameter in points
        *
        */
        public override float GetFontDescriptor(int key, float fontSize)
        {
            throw new NotImplementedException();
            /*
            if (cjkMirror != null)
                return cjkMirror.GetFontDescriptor(key, fontSize);
            switch (key)
            {
                case AWT_ASCENT:
                case ASCENT:
                    return Ascender * fontSize / 1000;
                case CAPHEIGHT:
                    return CapHeight * fontSize / 1000;
                case AWT_DESCENT:
                case DESCENT:
                    return Descender * fontSize / 1000;
                case ITALICANGLE:
                    return ItalicAngle;
                case BBOXLLX:
                    return llx * fontSize / 1000;
                case BBOXLLY:
                    return lly * fontSize / 1000;
                case BBOXURX:
                    return urx * fontSize / 1000;
                case BBOXURY:
                    return ury * fontSize / 1000;
                case AWT_LEADING:
                    return 0;
                case AWT_MAXADVANCE:
                    return (urx - llx) * fontSize / 1000;
                case FONT_WEIGHT:
                    return fontWeight * fontSize / 1000;
            }
            return 0;*/
        }

        /** Gets the full name of the font. If it is a True Type font
        * each array element will have {Platform ID, Platform Encoding ID,
        * Language ID, font name}. The interpretation of this values can be
        * found in the Open Type specification, chapter 2, in the 'name' table.<br>
        * For the other fonts the array has a single element with {"", "", "",
        * font name}.
        * @return the full name of the font
        *
        */
        
        /** Gets all the entries of the names-table. If it is a True Type font
        * each array element will have {Name ID, Platform ID, Platform Encoding ID,
        * Language ID, font name}. The interpretation of this values can be
        * found in the Open Type specification, chapter 2, in the 'name' table.<br>
        * For the other fonts the array has a single element with {"4", "", "", "",
        * font name}.
        * @return the full name of the font
        */
        
        public override int GetKerning(int char1, int char2)
        {
            return 0;
        }

        /** Gets the postscript font name.
        * @return the postscript font name
        *
        */
        
        /** Gets the width from the font according to the Unicode char <CODE>c</CODE>
        * or the <CODE>name</CODE>. If the <CODE>name</CODE> is null it's a symbolic font.
        * @param c the unicode char
        * @param name the glyph name
        * @return the width of the char
        *
        */
        internal override int GetRawWidth(int c, String name)
        {
            return 0;
        }

        /** Checks if the font has any kerning pairs.
        * @return <CODE>true</CODE> if the font has any kerning pairs
        *
        */
        public override bool HasKernPairs()
        {
            return false;
        }

        /** Outputs to the writer the font dictionaries and streams.
        * @param writer the writer for this document
        * @param ref the font indirect reference
        * @param params several parameters that depend on the font type
        * @throws IOException on error
        * @throws DocumentException error in generating the object
        *
        */
        /*internal override void WriteFont(PdfWriter writer, PdfIndirectReference refi, Object[] param)
        {
        }*/

        /**
        * Always returns null.
        * @return  null
        * @since   2.1.3
        */
        
        /**
        * Gets the width of a <CODE>char</CODE> in normalized 1000 units.
        * @param char1 the unicode <CODE>char</CODE> to get the width of
        * @return the width in normalized 1000 units
        */
        public override int GetWidth(int char1)
        {
            if (isType0)
            {
                if (hMetrics != null && cjkMirror != null && !cjkMirror.IsVertical())
                {
                    int c = cjkMirror.GetCidCode(char1);
                    int v = hMetrics[c];
                    if (v > 0)
                        return v;
                    else
                        return defaultWidth;
                }
                else
                {
                    int[] ws = null;
                    metrics.TryGetValue(char1, out ws);
                    if (ws != null)
                        return ws[1];
                    else
                        return 0;
                }
            }
            if (cjkMirror != null)
                return cjkMirror.GetWidth(char1);
            return base.GetWidth(char1);
        }

        public override int GetWidth(String text)
        {
            if (isType0)
            {
                int total = 0;
                if (hMetrics != null && cjkMirror != null && !cjkMirror.IsVertical())
                {
                    if (((CJKFont)cjkMirror).IsIdentity())
                    {
                        for (int k = 0; k < text.Length; ++k)
                        {
                            total += GetWidth(text[k]);
                        }
                    }
                    else
                    {
                        for (int k = 0; k < text.Length; ++k)
                        {
                            int val;
                            if (Utilities.IsSurrogatePair(text, k))
                            {
                                val = Utilities.ConvertToUtf32(text, k);
                                k++;
                            }
                            else
                            {
                                val = text[k];
                            }
                            total += GetWidth(val);
                        }
                    }
                }
                else
                {
                    char[] chars = text.ToCharArray();
                    int len = chars.Length;
                    for (int k = 0; k < len; ++k)
                    {
                        int[] ws = null;
                        metrics.TryGetValue(chars[k], out ws);
                        if (ws != null)
                            total += ws[1];
                    }
                }
                return total;
            }
            if (cjkMirror != null)
                return cjkMirror.GetWidth(text);
            return base.GetWidth(text);
        }

        public override byte[] ConvertToBytes(String text)
        {
            if (cjkMirror != null)
                return cjkMirror.ConvertToBytes(text);
            else if (isType0)
            {
                char[] chars = text.ToCharArray();
                int len = chars.Length;
                byte[] b = new byte[len * 2];
                int bptr = 0;
                for (int k = 0; k < len; ++k)
                {
                    int[] ws;
                    metrics.TryGetValue((int)chars[k], out ws);
                    if (ws != null)
                    {
                        int g = ws[0];
                        b[bptr++] = (byte)(g / 256);
                        b[bptr++] = (byte)g;
                    }
                }
                if (bptr == b.Length)
                    return b;
                else
                {
                    byte[] nb = new byte[bptr];
                    Array.Copy(b, 0, nb, 0, bptr);
                    return nb;
                }
            }
            else
            {
                char[] cc = text.ToCharArray();
                byte[] b = new byte[cc.Length];
                int ptr = 0;
                for (int k = 0; k < cc.Length; ++k)
                {
                    if (uni2byte.ContainsKey(cc[k]))
                        b[ptr++] = (byte)uni2byte[cc[k]];
                }
                if (ptr == b.Length)
                    return b;
                else
                {
                    byte[] b2 = new byte[ptr];
                    Array.Copy(b, 0, b2, 0, ptr);
                    return b2;
                }
            }
        }

        internal override byte[] ConvertToBytes(int char1)
        {
            if (cjkMirror != null)
                return cjkMirror.ConvertToBytes(char1);
            else if (isType0)
            {
                int[] ws;
                metrics.TryGetValue((int)char1, out ws);
                if (ws != null)
                {
                    int g = ws[0];
                    return new byte[] { (byte)(g / 256), (byte)g };
                }
                else
                    return new byte[0];
            }
            else
            {
                if (uni2byte.ContainsKey(char1))
                    return new byte[] { (byte)uni2byte[char1] };
                else
                    return new byte[0];
            }
        }

        internal PdfIndirectReference IndirectReference
        {
            get
            {
                if (refFont == null)
                    throw new ArgumentException("Font reuse not allowed with direct font objects.");
                return refFont;
            }
        }

        public override bool CharExists(int c)
        {
            if (cjkMirror != null)
                return cjkMirror.CharExists(c);
            else if (isType0)
            {
                return metrics.ContainsKey((int)c);
            }
            else
                return base.CharExists(c);
        }

        public override bool SetKerning(int char1, int char2, int kern)
        {
            return false;
        }

        public override int[] GetCharBBox(int c)
        {
            return null;
        }

        public override bool IsVertical()
        {
            if (cjkMirror != null)
                return cjkMirror.IsVertical();
            else
                return base.IsVertical();
        }

        protected override int[] GetRawCharBBox(int c, String name)
        {
            return null;
        }

        /**
        * Exposes the unicode - > CID map that is constructed from the font's encoding
        * @return the unicode to CID map
        * @since 2.1.7
        */
        internal IntHashtable Uni2Byte
        {
            get
            {
                return uni2byte;
            }
        }

        /**
         * Exposes the CID - > unicode map that is constructed from the font's encoding
         * @return the CID to unicode map
         * @since 5.4.0
         */
        internal IntHashtable Byte2Uni
        {
            get
            {
                return byte2uni;
            }
        }

        /**
         * Gets the difference map
         * @return the difference map
         * @since 5.0.5
         */
        internal IntHashtable Diffmap
        {
            get
            {
                return diffmap;
            }
        }
    }
}
