using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UZ.PDF.Objects;

namespace UZ.PDF.Font
{
    class CMapParser : Pdf
    {
        private Tokenizer tokens;
        private CMap cmap;

        public CMapParser(byte[] Input)
        {
            cmap = new CMap();
            tokens = new Tokenizer(Input);
            ParseCMapStream();
        }

        public CMap CMap
        {
            get { return cmap; }
        }

        private void ParseCMapStream()
        {
            PdfObject obj = null;
            PdfObject lastObj = null;
            while (true)
            {
                lastObj = obj;
                obj = ReadObject();

                if (obj == null)
                    break;

                if (obj.Type != PdfObject.ObjectType.LITERAL)
                    continue;
                switch (obj.Value)
                {
                    case "begincodespacerange":
                        Assert(lastObj.Type == PdfObject.ObjectType.NUMBER, "begincodespacerange expects a number of items");
                        int na = ((PdfNumber)lastObj).IntValue;
                        for (int i = 0; i < na; i++)
                        {
                            PdfObject start = ReadObject();
                            PdfObject end = ReadObject();
                            Assert(start.Type == PdfObject.ObjectType.STRING && end.Type == PdfObject.ObjectType.STRING,
                                "codespacestart expects two hex-string parameters");
                            cmap.AddCodespaceRange(new CodespaceRange(start.Bytes, end.Bytes));
                        }
                        break;
                    case "beginbfrange":
                        Assert(lastObj.Type == PdfObject.ObjectType.NUMBER, "beginbfrange expects a number of items");
                        int nb = ((PdfNumber)lastObj).IntValue;
                        for (int i = 0; i < nb; i++)
                        {
                            PdfObject start = ReadObject();
                            PdfObject end = ReadObject();
                            Assert(start.Type == PdfObject.ObjectType.STRING && end.Type == PdfObject.ObjectType.STRING,
                                "beginbfrange expects two hex-string parameters as a range");

                            String value = null;
                            byte[] pointer = start.Bytes;
                            byte[] map;

                            PdfObject mapObj = ReadObject();
                            if (mapObj.IsArray())
                            {
                                foreach (PdfObject item in ((PdfArray)mapObj).Objects)
                                {
                                    Assert(item.Type == PdfObject.ObjectType.STRING,
                                        "beginbfrange third param must be a hex-string or array of hex-strings");
                                    value = EncodingTools.BytesToUnicode(item.Bytes);
                                    cmap.AddMapping(pointer, value);
                                    Increment(pointer);
                                }
                            }
                            else
                            {
                                Assert(mapObj.Type == PdfObject.ObjectType.STRING,
                                    "beginbfrange third param must be a hex-string or array of hex-strings");
                                map = mapObj.Bytes;
                                do
                                {
                                    value = EncodingTools.BytesToUnicode(map);
                                    cmap.AddMapping(pointer, value);
                                    Increment(pointer);
                                    Increment(map);
                                } while (LessThan(pointer, end.Bytes)) ;
                            }
                        }
                        break;
                    case "beginbfchar":
                        Assert(lastObj.Type == PdfObject.ObjectType.NUMBER, "beginbfchar expects a number of items");
                        int nc = ((PdfNumber)lastObj).IntValue;
                        for (int i = 0; i < nc; i++)
                        {
                            PdfObject key = ReadObject();
                            PdfObject value = ReadObject();
                            Assert(key.Type == PdfObject.ObjectType.STRING && value.Type == PdfObject.ObjectType.STRING,
                                "beginbfchar expects two hex-string parameters");
                            cmap.AddMapping(key.Bytes, EncodingTools.BytesToUnicode(value.Bytes));
                        }
                        break;
                }
            }
        }

        private PdfDictionary ReadDictionary()
        {
            PdfDictionary dictionary = new PdfDictionary();
            while (true)
            {
                tokens.NextValid();
                if (tokens.Type == Tokenizer.TokenType.DICTIONARY_END)
                    break;
                Assert(tokens.Type == Tokenizer.TokenType.NAME, "Name token expected inside dictionary");
                PdfName name = new PdfName(tokens.Value);
                PdfObject obj = ReadObject();
                dictionary.Set(name, obj);
            }
            return dictionary;
        }

        private PdfArray ReadArray()
        {
            PdfArray array = new PdfArray();
            while (true)
            {
                PdfObject obj = ReadObject();
                if (tokens.Type == Tokenizer.TokenType.ARRAY_END)
                    break;
                array.Objects.Add(obj);
            }
            return array;
        }

        protected PdfObject ReadObject()
        {
            tokens.NextValid();
            switch (tokens.Type)
            {
                case Tokenizer.TokenType.DICTIONARY_START:
                    {
                        PdfDictionary dictionary = ReadDictionary();
                        int pos = tokens.Pointer;
                        return dictionary;
                    }
                case Tokenizer.TokenType.ARRAY_START:
                    {
                        PdfArray arr = ReadArray();
                        return arr;
                    }
                case Tokenizer.TokenType.NUMBER:
                    return new PdfNumber(tokens.Value);
                case Tokenizer.TokenType.STRING:
                    return new PdfString(tokens.Value);
                case Tokenizer.TokenType.NAME:
                    return new PdfName(tokens.Value);
                case Tokenizer.TokenType.EOF:
                    return null;
                default:
                    String value = tokens.Value;
                    if ("null".Equals(value))
                    {
                        return new PdfNull(value);
                    }
                    else if ("true".Equals(value) || "false".Equals(value))
                    {
                        return new PdfBoolean(value);
                    }
                    return new PdfLiteral(value);
            }
        }

        private bool LessThan(byte[] first, byte[] second)
        {
            for (int i = 0; i < first.Length; i++)
            {
                if (first[i] == second[i])
                    continue;
                return (first[i] + 256) % 256 < (second[i] + 256) % 256;
            }
            return false;
        }

        private void Increment(byte[] data)
        {
            Increment(data, data.Length - 1);
        }

        private void Increment(byte[] data, int position)
        {
            if (position > 0 && (data[position] + 256) % 256 == 255)
            {
                data[position] = 0;
                Increment(data, position - 1);
            }
            else
            {
                data[position] = (byte)(data[position] + 1);
            }
        }
    }
}
