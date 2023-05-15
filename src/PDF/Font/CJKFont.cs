using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.util;
using UZ.PDF.Font.Cmaps;
using UZ.PDF.Objects;
using UZ.System.util.collections;

namespace UZ.PDF.Font
{
    class CJKFont : BaseFont
    {
        /** The encoding used in the PDF document for CJK fonts
         */
        internal const string CJK_ENCODING = "UNICODEBIGUNMARKED";
        private const int FIRST = 0;
        private const int BRACKET = 1;
        private const int SERIAL = 2;
        private const int V1Y = 880;
        
        internal static Settings cjkFonts = new Settings();
        internal static Settings cjkEncodings = new Settings();
        private static Dictionary<String, Dictionary<String, object>> allFonts = new Dictionary<string, Dictionary<string, object>>();
        private static bool SettingsLoaded = false;

        /** The path to the font resources. */
        public const String RESOURCE_PATH_CMAP = RESOURCE_PATH + "Cmaps.";
        private static Dictionary<String, Dictionary<String, object>> registryNames = new Dictionary<string, Dictionary<string, object>>();
        private CMapCidByte cidByte;
        private CMapUniCid uniCid;
        private CMapCidUni cidUni;
        private String uniMap;

        /** The font name */
        private string fontName;
        /** The style modifier */
        private string style = "";
        /** The CMap name associated with this font */
        private string CMap;

        private bool cidDirect = false;

        private IntHashtable vMetrics;
        private IntHashtable hMetrics;
        private Dictionary<String, object> fontDesc;

        private static void LoadSettings()
        {
            if (SettingsLoaded)
                return;
            lock (allFonts)
            {
                if (SettingsLoaded)
                    return;
                try
                {
                    LoadRegistry();
                    foreach (String font in registryNames["fonts"].Keys)
                    {
                        allFonts[font] = ReadFontSettings(font);
                    }
                }
                catch
                {
                }
                SettingsLoaded = true;
            }
        }

        private static readonly char[] cspace = { ' ' };
        private static void LoadRegistry()
        {
            Stream isp = StreamUtil.GetResourceStream(RESOURCE_PATH_CMAP + "cjk_registry.Settings");
            Settings p = new Settings();
            p.Load(isp);
            isp.Close();
            foreach (string key in p.Keys)
            {
                String value = p[key];
                String[] sp = value.Split(cspace, StringSplitOptions.RemoveEmptyEntries);
                Dictionary<String, object> hs = new Dictionary<string, object>();
                foreach (String s in sp)
                {
                    hs[s] = null;
                }
                registryNames[key] = hs;
            }
        }

        /** Creates a CJK font.
         * @param fontName the name of the font
         * @param enc the encoding of the font
         * @param emb always <CODE>false</CODE>. CJK font and not embedded
         * @throws DocumentException on error
         * @throws IOException on error
         */
        internal CJKFont(string fontName, string enc, bool emb)
        {
            LoadSettings();
            this.FontType = FONT_TYPE_CJK;
            string nameBase = GetBaseName(fontName);
            if (!IsCJKFont(nameBase, enc))
                throw new PdfException("This font is not a CJK font");
            if (nameBase.Length < fontName.Length)
            {
                style = fontName.Substring(nameBase.Length);
                fontName = nameBase;
            }
            this.fontName = fontName;
            encoding = CJK_ENCODING;
            vertical = enc.EndsWith("V");
            CMap = enc;
            if (enc.Equals(IDENTITY_H) || enc.Equals(IDENTITY_V))
                cidDirect = true;
            LoadCMaps();
        }

        internal String UniMap
        {
            get
            {
                return uniMap;
            }
        }

        private void LoadCMaps()
        {
            try
            {
                fontDesc = allFonts[fontName];
                hMetrics = (IntHashtable)fontDesc["W"];
                vMetrics = (IntHashtable)fontDesc["W2"];
                String registry = (String)fontDesc["Registry"];
                uniMap = "";
                foreach (String name in registryNames[registry + "_Uni"].Keys)
                {
                    uniMap = name;
                    if (name.EndsWith("V") && vertical)
                        break;
                    if (!name.EndsWith("V") && !vertical)
                        break;
                }
                if (cidDirect)
                {
                    cidUni = CMapCache.GetCachedCMapCidUni(uniMap);
                }
                else
                {
                    uniCid = CMapCache.GetCachedCMapUniCid(uniMap);
                    cidByte = CMapCache.GetCachedCMapCidByte(CMap);
                }
            }
            catch (Exception ex)
            {
                throw new PdfException(ex.Message);
            }
        }

        /**
         * Returns a font compatible with a CJK encoding or null if not found.
         * @param enc
         * @return 
         */
        public static String GetCompatibleFont(String enc)
        {
            LoadSettings();
            String registry = null;
            foreach (KeyValuePair<String, Dictionary<String, object>> e in registryNames)
            {
                if (e.Value.ContainsKey(enc))
                {
                    registry = e.Key;
                    break;
                }
            }
            if (registry == null)
                return null;
            foreach (KeyValuePair<String, Dictionary<String, object>> e in allFonts)
            {
                if (registry.Equals(e.Value["Registry"]))
                    return e.Key;
            }
            return null;
        }

        /** Checks if its a valid CJK font.
         * @param fontName the font name
         * @param enc the encoding
         * @return <CODE>true</CODE> if it is CJK font
         */
        public static bool IsCJKFont(string fontName, string enc)
        {
            LoadSettings();
            if (!registryNames.ContainsKey("fonts"))
                return false;
            if (!registryNames["fonts"].ContainsKey(fontName))
                return false;
            if (enc.Equals(IDENTITY_H) || enc.Equals(IDENTITY_V))
                return true;
            String registry = allFonts[fontName]["Registry"] as string;
            Dictionary<String, object> encodings;
            registryNames.TryGetValue(registry, out encodings);
            return encodings != null && encodings.ContainsKey(enc);
        }

        /**
         * Gets the width of a <CODE>char</CODE> in normalized 1000 units.
         * @param char1 the unicode <CODE>char</CODE> to get the width of
         * @return the width in normalized 1000 units
         */
        public override int GetWidth(int char1)
        {
            int c = (int)char1;
            if (!cidDirect)
                c = uniCid.Lookup(char1);
            int v;
            if (vertical)
                v = vMetrics[c];
            else
                v = hMetrics[c];
            if (v > 0)
                return v;
            else
                return 1000;
        }

        public override int GetWidth(string text)
        {
            int total = 0;
            if (cidDirect)
            {
                foreach (char c in text)
                {
                    total += GetWidth(c);
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
            return total;
        }

        internal override int GetRawWidth(int c, string name)
        {
            return 0;
        }
        public override int GetKerning(int char1, int char2)
        {
            return 0;
        }

        private PdfDictionary GetFontDescriptor()
        {
            throw new NotImplementedException();
            /*PdfDictionary dic = new PdfDictionary(PdfName.FONTDESCRIPTOR);
            dic.Put(PdfName.ASCENT, new PdfLiteral((String)fontDesc["Ascent"]));
            dic.Put(PdfName.CAPHEIGHT, new PdfLiteral((String)fontDesc["CapHeight"]));
            dic.Put(PdfName.DESCENT, new PdfLiteral((String)fontDesc["Descent"]));
            dic.Put(PdfName.FLAGS, new PdfLiteral((String)fontDesc["Flags"]));
            dic.Put(PdfName.FONTBBOX, new PdfLiteral((String)fontDesc["FontBBox"]));
            dic.Put(PdfName.FONTNAME, new PdfName(fontName + style));
            dic.Put(PdfName.ITALICANGLE, new PdfLiteral((String)fontDesc["ItalicAngle"]));
            dic.Put(PdfName.STEMV, new PdfLiteral((String)fontDesc["StemV"]));
            PdfDictionary pdic = new PdfDictionary();
            pdic.Put(PdfName.PANOSE, new PdfString((String)fontDesc["Panose"], null));
            dic.Put(PdfName.STYLE, pdic);
            return dic;*/
        }

        private PdfDictionary GetCIDFont(PdfIndirectReference fontDescriptor, IntHashtable cjkTag)
        {
            throw new NotImplementedException();
            
            /*PdfDictionary dic = new PdfDictionary(PdfName.FONT);
            dic.Put(PdfName.SUBTYPE, PdfName.CIDFONTTYPE0);
            dic.Put(PdfName.BASEFONT, new PdfName(fontName + style));
            dic.Put(PdfName.FONTDESCRIPTOR, fontDescriptor);
            int[] keys = cjkTag.ToOrderedKeys();
            string w = ConvertToHCIDMetrics(keys, hMetrics);
            if (w != null)
                dic.Put(PdfName.W, new PdfLiteral(w));
            if (vertical)
            {
                w = ConvertToVCIDMetrics(keys, vMetrics, hMetrics);
                if (w != null)
                    dic.Put(PdfName.W2, new PdfLiteral(w));
            }
            else
                dic.Put(PdfName.DW, new PdfNumber(1000));
            PdfDictionary cdic = new PdfDictionary();
            if (cidDirect)
            {
                cdic.Put(PdfName.REGISTRY, new PdfString(cidUni.Registry, null));
                cdic.Put(PdfName.ORDERING, new PdfString(cidUni.Ordering, null));
                cdic.Put(PdfName.SUPPLEMENT, new PdfNumber(cidUni.Supplement));
            }
            else
            {
                cdic.Put(PdfName.REGISTRY, new PdfString(cidByte.Registry, null));
                cdic.Put(PdfName.ORDERING, new PdfString(cidByte.Ordering, null));
                cdic.Put(PdfName.SUPPLEMENT, new PdfNumber(cidByte.Supplement));
            }
            dic.Put(PdfName.CIDSYSTEMINFO, cdic);
            return dic;*/
        }

        private PdfDictionary GetFontBaseType(PdfIndirectReference CIDFont)
        {
            throw new NotImplementedException();
            /*
            PdfDictionary dic = new PdfDictionary(PdfName.FONT);
            dic.Put(PdfName.SUBTYPE, PdfName.TYPE0);
            string name = fontName;
            if (style.Length > 0)
                name += "-" + style.Substring(1);
            name += "-" + CMap;
            dic.Put(PdfName.BASEFONT, new PdfName(name));
            dic.Put(PdfName.ENCODING, new PdfName(CMap));
            dic.Put(PdfName.DESCENDANTFONTS, new PdfArray(CIDFont));
            return dic;*/
        }

        
        /**
         * You can't get the FontStream of a CJK font (CJK fonts are never embedded),
         * so this method always returns null.
         * @return  null
         * @since   2.1.3
         */
        

        private float GetBBox(int idx)
        {
            string s = (string)fontDesc["FontBBox"];
            StringTokenizer tk = new StringTokenizer(s, " []\r\n\t\f");
            string ret = tk.NextToken();
            for (int k = 0; k < idx; ++k)
                ret = tk.NextToken();
            return int.Parse(ret);
        }

        /** Gets the font parameter identified by <CODE>key</CODE>. Valid values
         * for <CODE>key</CODE> are <CODE>ASCENT</CODE>, <CODE>CAPHEIGHT</CODE>, <CODE>DESCENT</CODE>
         * and <CODE>ITALICANGLE</CODE>.
         * @param key the parameter to be extracted
         * @param fontSize the font size in points
         * @return the parameter in points
         */
        public override float GetFontDescriptor(int key, float fontSize)
        {
            throw new NotImplementedException();
            /*
            switch (key)
            {
                case AWT_ASCENT:
                case ASCENT:
                    return GetDescNumber("Ascent") * fontSize / 1000;
                case CAPHEIGHT:
                    return GetDescNumber("CapHeight") * fontSize / 1000;
                case AWT_DESCENT:
                case DESCENT:
                    return GetDescNumber("Descent") * fontSize / 1000;
                case ITALICANGLE:
                    return GetDescNumber("ItalicAngle");
                case BBOXLLX:
                    return fontSize * GetBBox(0) / 1000;
                case BBOXLLY:
                    return fontSize * GetBBox(1) / 1000;
                case BBOXURX:
                    return fontSize * GetBBox(2) / 1000;
                case BBOXURY:
                    return fontSize * GetBBox(3) / 1000;
                case AWT_LEADING:
                    return 0;
                case AWT_MAXADVANCE:
                    return fontSize * (GetBBox(2) - GetBBox(0)) / 1000;
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
         */
        
        /** Gets all the entries of the names-table. If it is a True Type font
         * each array element will have {Name ID, Platform ID, Platform Encoding ID,
         * Language ID, font name}. The interpretation of this values can be
         * found in the Open Type specification, chapter 2, in the 'name' table.<br>
         * For the other fonts the array has a single element with {"4", "", "", "",
         * font name}.
         * @return the full name of the font
         */
        
        /** Gets the family name of the font. If it is a True Type font
         * each array element will have {Platform ID, Platform Encoding ID,
         * Language ID, font name}. The interpretation of this values can be
         * found in the Open Type specification, chapter 2, in the 'name' table.<br>
         * For the other fonts the array has a single element with {"", "", "",
         * font name}.
         * @return the family name of the font
         */
        

        internal static IntHashtable CreateMetric(string s)
        {
            IntHashtable h = new IntHashtable();
            StringTokenizer tk = new StringTokenizer(s);
            while (tk.HasMoreTokens())
            {
                int n1 = int.Parse(tk.NextToken());
                h[n1] = int.Parse(tk.NextToken());
            }
            return h;
        }

        internal static string ConvertToHCIDMetrics(int[] keys, IntHashtable h)
        {
            if (keys.Length == 0)
                return null;
            int lastCid = 0;
            int lastValue = 0;
            int start;
            for (start = 0; start < keys.Length; ++start)
            {
                lastCid = keys[start];
                lastValue = h[lastCid];
                if (lastValue != 0)
                {
                    ++start;
                    break;
                }
            }
            if (lastValue == 0)
                return null;
            StringBuilder buf = new StringBuilder();
            buf.Append('[');
            buf.Append(lastCid);
            int state = FIRST;
            for (int k = start; k < keys.Length; ++k)
            {
                int cid = keys[k];
                int value = h[cid];
                if (value == 0)
                    continue;
                switch (state)
                {
                    case FIRST:
                        {
                            if (cid == lastCid + 1 && value == lastValue)
                            {
                                state = SERIAL;
                            }
                            else if (cid == lastCid + 1)
                            {
                                state = BRACKET;
                                buf.Append('[').Append(lastValue);
                            }
                            else
                            {
                                buf.Append('[').Append(lastValue).Append(']').Append(cid);
                            }
                            break;
                        }
                    case BRACKET:
                        {
                            if (cid == lastCid + 1 && value == lastValue)
                            {
                                state = SERIAL;
                                buf.Append(']').Append(lastCid);
                            }
                            else if (cid == lastCid + 1)
                            {
                                buf.Append(' ').Append(lastValue);
                            }
                            else
                            {
                                state = FIRST;
                                buf.Append(' ').Append(lastValue).Append(']').Append(cid);
                            }
                            break;
                        }
                    case SERIAL:
                        {
                            if (cid != lastCid + 1 || value != lastValue)
                            {
                                buf.Append(' ').Append(lastCid).Append(' ').Append(lastValue).Append(' ').Append(cid);
                                state = FIRST;
                            }
                            break;
                        }
                }
                lastValue = value;
                lastCid = cid;
            }
            switch (state)
            {
                case FIRST:
                    {
                        buf.Append('[').Append(lastValue).Append("]]");
                        break;
                    }
                case BRACKET:
                    {
                        buf.Append(' ').Append(lastValue).Append("]]");
                        break;
                    }
                case SERIAL:
                    {
                        buf.Append(' ').Append(lastCid).Append(' ').Append(lastValue).Append(']');
                        break;
                    }
            }
            return buf.ToString();
        }

        internal static string ConvertToVCIDMetrics(int[] keys, IntHashtable v, IntHashtable h)
        {
            if (keys.Length == 0)
                return null;
            int lastCid = 0;
            int lastValue = 0;
            int lastHValue = 0;
            int start;
            for (start = 0; start < keys.Length; ++start)
            {
                lastCid = keys[start];
                lastValue = v[lastCid];
                if (lastValue != 0)
                {
                    ++start;
                    break;
                }
                else
                    lastHValue = h[lastCid];
            }
            if (lastValue == 0)
                return null;
            if (lastHValue == 0)
                lastHValue = 1000;
            StringBuilder buf = new StringBuilder();
            buf.Append('[');
            buf.Append(lastCid);
            int state = FIRST;
            for (int k = start; k < keys.Length; ++k)
            {
                int cid = keys[k];
                int value = v[cid];
                if (value == 0)
                    continue;
                int hValue = h[lastCid];
                if (hValue == 0)
                    hValue = 1000;
                switch (state)
                {
                    case FIRST:
                        {
                            if (cid == lastCid + 1 && value == lastValue && hValue == lastHValue)
                            {
                                state = SERIAL;
                            }
                            else
                            {
                                buf.Append(' ').Append(lastCid).Append(' ').Append(-lastValue).Append(' ').Append(lastHValue / 2).Append(' ').Append(V1Y).Append(' ').Append(cid);
                            }
                            break;
                        }
                    case SERIAL:
                        {
                            if (cid != lastCid + 1 || value != lastValue || hValue != lastHValue)
                            {
                                buf.Append(' ').Append(lastCid).Append(' ').Append(-lastValue).Append(' ').Append(lastHValue / 2).Append(' ').Append(V1Y).Append(' ').Append(cid);
                                state = FIRST;
                            }
                            break;
                        }
                }
                lastValue = value;
                lastCid = cid;
                lastHValue = hValue;
            }
            buf.Append(' ').Append(lastCid).Append(' ').Append(-lastValue).Append(' ').Append(lastHValue / 2).Append(' ').Append(V1Y).Append(" ]");
            return buf.ToString();
        }

        internal static Dictionary<String, Object> ReadFontSettings(String name)
        {
            name += ".Settings";
            Stream isp = StreamUtil.GetResourceStream(RESOURCE_PATH_CMAP + name);
            Settings p = new Settings();
            p.Load(isp);
            isp.Close();
            IntHashtable W = CreateMetric(p["W"]);
            p.Remove("W");
            IntHashtable W2 = CreateMetric(p["W2"]);
            p.Remove("W2");
            Dictionary<String, Object> map = new Dictionary<string, object>();
            foreach (string key in p.Keys)
            {
                map[key] = p[key];
            }
            map["W"] = W;
            map["W2"] = W2;
            return map;
        }

        public override int GetUnicodeEquivalent(int c)
        {
            if (cidDirect)
            {
                if (c == CID_NEWLINE)
                    return '\n';
                return cidUni.Lookup(c);
            }
            return c;
        }

        public override int GetCidCode(int c)
        {
            if (cidDirect)
                return c;
            return uniCid.Lookup(c);
        }

        public override bool HasKernPairs()
        {
            return false;
        }

        public override bool CharExists(int c)
        {
            if (cidDirect)
                return true;
            return cidByte.Lookup(uniCid.Lookup(c)).Length > 0;
        }

        public override bool SetCharAdvance(int c, int advance)
        {
            return false;
        }

        public override bool SetKerning(int char1, int char2, int kern)
        {
            return false;
        }

        public override int[] GetCharBBox(int c)
        {
            return null;
        }

        protected override int[] GetRawCharBBox(int c, String name)
        {
            return null;
        }
        /**
         * Converts a <CODE>String</CODE> to a </CODE>byte</CODE> array according
         * to the font's encoding.
         * @param text the <CODE>String</CODE> to be converted
         * @return an array of <CODE>byte</CODE> representing the conversion according to the font's encoding
         */
        public override byte[] ConvertToBytes(String text)
        {
            if (cidDirect)
                return base.ConvertToBytes(text);
            if (text.Length == 1)
                return ConvertToBytes((int)text[0]);
            MemoryStream bout = new MemoryStream();
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
                byte[] b = ConvertToBytes(val);
                bout.Write(b, 0, b.Length);
            }
            return bout.ToArray();
        }

        /**
         * Converts a <CODE>char</CODE> to a </CODE>byte</CODE> array according
         * to the font's encoding.
         * @param char1 the <CODE>char</CODE> to be converted
         * @return an array of <CODE>byte</CODE> representing the conversion according to the font's encoding
         */
        internal override byte[] ConvertToBytes(int char1)
        {
            if (cidDirect)
                return base.ConvertToBytes(char1);
            return cidByte.Lookup(uniCid.Lookup(char1));
        }

        public bool IsIdentity()
        {
            return cidDirect;
        }
    }
}
