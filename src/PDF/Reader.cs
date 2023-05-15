using System;
using System.Collections.Generic;
using System.Text;
using UZ.PDF.Objects;
using System.Collections;

namespace UZ.PDF
{
    class Reader : Pdf
    {
        private Tokenizer tokens;
        private CrossReferenceTable xref;
        private PdfDictionary trailer;

        public Reader(String filename)
        {
            tokens = new Tokenizer(filename);
            xref = new CrossReferenceTable();
            ReadPdf();
        }

        public CrossReferenceTable XRef
        {
            get { return xref; }
        }

        public PdfDictionary Trailer
        {
            get { return trailer; }
        }

        private void ReadPdf()
        {
            tokens.CheckHeader();
            ReadXRef();
            ReadDocument();
        }

        private void ReadXRef()
        {
            int xrefpos = tokens.Startxref;
            tokens.Seek(xrefpos);
            tokens.Next();
            Assert(tokens.Value.Equals("startxref"), "Invalid PDF file, token startxref not found");
            tokens.Next();
            Assert(tokens.Type == Tokenizer.TokenType.NUMBER, "Invalid PDF file, invalid startxref spec");
            int startxref = tokens.IntValue;
       
            //try
            //{
                if (ReadXRefStream(startxref))
                {
                    xref.SetNewXRefType(true);
                    return;
                }
            //}
            //catch { }

            tokens.Seek(startxref);
            trailer = ReadXRefSection();
            PdfDictionary trailer_ = trailer;
            while (true)
            {
                if (!trailer_.ContainsKey("Prev"))
                    break;
                PdfNumber prevXref = (PdfNumber)trailer_.Get("Prev");
                tokens.Seek(prevXref.IntValue);
                trailer_ = ReadXRefSection();
            }


            int x = 0;
        }

        private bool ReadXRefStream(int pointer)
        {
            tokens.Seek(pointer);
            int xrefStream = 0;
            if (!tokens.Next())
                return false;
            if (tokens.Type != Tokenizer.TokenType.NUMBER)
                return false;
            xrefStream = tokens.IntValue;
            if (!tokens.Next() || tokens.Type != Tokenizer.TokenType.NUMBER)
                return false;
            if (!tokens.Next() || !tokens.Value.Equals("obj"))
                return false;
            PdfObject streamObject = ReadObject();
            PdfStream stream = null;
            if (streamObject.IsStream())
            {
                stream = (PdfStream)streamObject;
                if (!"XRef".Equals(stream.Get("Type").Value))
                    return false;
            }
            else
                return false;

            // Valid XRef stream found, start processing
            if (trailer == null)
            {
                trailer = new PdfDictionary();
                trailer.Merge(stream);
            }

            stream.Load();
            byte[] bytes = stream.Stream;
            int size = ((PdfNumber)stream.Get("Size")).IntValue;

            // Get /INDEX
            PdfArray index = null;
            if (stream.ContainsKey("Index")) //
                index = (PdfArray)stream.Get("Index");
            else
            {
                index = new PdfArray();
                index.Objects.Add(new PdfNumber("0"));
                index.Objects.Add(new PdfNumber(size.ToString(EncodingTools.NumberFormat)));
            }

            // Get w and prev pointer
            PdfArray w = (PdfArray)stream.Get("W");
            int prev = -1;
            if (stream.ContainsKey("Prev"))
                prev = ((PdfNumber)stream.Get("Prev")).IntValue;
            
            // Process XRef stream data
            xref.EnsureLength(size * 2);

            int bptr = 0;
            int[] wc = new int[3];
            for (int k = 0; k < 3; ++k)
                wc[k] = ((PdfNumber)w.Objects[k]).IntValue;
            for (int idx = 0; idx < index.Objects.Count; idx += 2) {
                int start = ((PdfNumber)index.Objects[idx]).IntValue;
                int length = ((PdfNumber)index.Objects[idx + 1]).IntValue;
                xref.EnsureLength((start + length) * 2);
                while (length-- > 0) {
                    int type = 1;
                    if (wc[0] > 0) {
                        type = 0;
                        for (int k = 0; k < wc[0]; ++k)
                            type = (type << 8) + (bytes[bptr++] & 0xff);
                    }
                    int field2 = 0;
                    for (int k = 0; k < wc[1]; ++k)
                        field2 = (field2 << 8) + (bytes[bptr++] & 0xff);
                    int field3 = 0;
                    for (int k = 0; k < wc[2]; ++k)
                        field3 = (field3 << 8) + (bytes[bptr++] & 0xff);
                    int baseb = start * 2;
                    if (xref.Pointer[baseb] == 0 && xref.Pointer[baseb + 1] == 0)
                    {
                        switch (type) {
                            case 0:
                                xref.Pointer[baseb] = -1;
                                break;
                            case 1:
                                xref.Pointer[baseb] = field2;
                                break;
                            case 2:
                                xref.Pointer[baseb] = field3;
                                xref.Pointer[baseb + 1] = field2; // generation numbers deprecated
                                Hashtable sequence;
                                if (!xref.ObjectStreams.TryGetValue(field2, out sequence)) {
                                    sequence = new Hashtable();
                                    sequence[field3] = 1;
                                    xref.ObjectStreams[field2] = sequence;
                                }
                                else
                                    sequence[field3] = 1;
                                break;
                        }
                    }
                    ++start;
                }
            }
            xrefStream *= 2;
            if (xrefStream < xref.Pointer.Length)
                xref.Pointer[xrefStream] = -1;
                
            if (prev == -1)
                return true;
            return ReadXRefStream(prev);

        }

        private PdfDictionary ReadXRefSection()
        {
            tokens.NextValid();
            Assert(tokens.Value.Equals("xref"), "Invalid PDF file, xref not found");

            while (true)
            {
                tokens.NextValid();

                if (tokens.Value.Equals("trailer"))
                    break; // ok, end of xref table

                Assert(tokens.Type == Tokenizer.TokenType.NUMBER, "Invalid PDF file, invalid xref spec #1");
                int Start = tokens.IntValue;
                tokens.NextValid();
                Assert(tokens.Type == Tokenizer.TokenType.NUMBER, "Invalid PDF file, invalid xref spec #2");
                int End = Start + tokens.IntValue;

                xref.EnsureLength(End * 2);
                int Position, Generation;
                string Flag;
                for (int i = Start; i < End; i++)
                {
                    Assert(tokens.Next() && tokens.Type == Tokenizer.TokenType.NUMBER, "Invalid PDF file, invalid xref spec #3");
                    Position = tokens.IntValue;
                    Assert(tokens.Next() && tokens.Type == Tokenizer.TokenType.NUMBER, "Invalid PDF file, invalid xref spec #4");
                    Generation = tokens.IntValue;
                    Assert(tokens.Next() && tokens.Type == Tokenizer.TokenType.OTHER, "Invalid PDF file, invalid xref spec #5");
                    Flag = tokens.Value;
                    int k = 2 * i;
                    int newValue = Flag == "n" ? Position : -1;
                    if (xref.Pointer[k] == 0 && xref.Pointer[k + 1] == 0)
                        xref.Pointer[k] = newValue;
                    
                    //xref.Pointer[k + 1] = Generation;
                }
            }
            PdfObject trailer = ReadObject();
            Assert(trailer != null && trailer.Type == PdfObject.ObjectType.DICTIONARY, "Invalid PDF file, invalid trailer spec");
            return (PdfDictionary)trailer;
        }

        private void ReadDocument()
        {
            List<PdfStream> streams = new List<PdfStream>();
            for (int i = 2; i < xref.Pointer.Length; i += 2)
            {
                if (xref.Pointer[i] <= 0 || xref.Pointer[i + 1] > 0)
                    continue;
                PdfObject obj = GetObjectByNumber(i / 2);
                if (obj != null && obj.IsStream())
                    streams.Add((PdfStream)obj);
            }
            foreach (PdfStream stream in streams)
                stream.Load();

            if (xref.ObjectStreams.Count > 0)
            {
                foreach (KeyValuePair<int, Hashtable> entry in xref.ObjectStreams)
                {
                    int key = entry.Key;
                    Hashtable table = entry.Value;
                    ReadObjectStream((PdfStream)xref.Reference[key], table);
                }
            }

        }

        private void ReadObjectStream(PdfStream stream, Hashtable table)
        {
            int first = ((PdfNumber)stream.Get("First")).IntValue;
            int count = ((PdfNumber)stream.Get("N")).IntValue;
            byte[] bytes = stream.Stream;
            Tokenizer savedTokenizer = tokens;
            tokens = new Tokenizer(bytes);
            try
            {
                int[] address = new int[count];
                int[] objNumber = new int[count];
                bool ok = true;
                for (int k = 0; k < count; ++k)
                {
                    ok = tokens.Next();
                    if (!ok)
                        break;
                    if (tokens.Type != Tokenizer.TokenType.NUMBER)
                    {
                        ok = false;
                        break;
                    }
                    objNumber[k] = tokens.IntValue;
                    ok = tokens.Next();
                    if (!ok)
                        break;
                    if (tokens.Type != Tokenizer.TokenType.NUMBER)
                    {
                        ok = false;
                        break;
                    }
                    address[k] = tokens.IntValue + first;
                }
                if (!ok)
                    throw new PdfException("Reading object stream failed");
                for (int k = 0; k < count; ++k)
                {
                    if (table.ContainsKey(k))
                    {
                        tokens.Seek(address[k]);
                        tokens.Next();
                        PdfObject obj;
                        if (tokens.Type == Tokenizer.TokenType.NUMBER)
                        {
                            obj = new PdfNumber(tokens.Value);
                        }
                        else
                        {
                            tokens.Seek(address[k]);
                            obj = ReadObject();
                        }
                        xref.Reference[objNumber[k]] = obj;
                    }
                }
            }
            finally
            {
                tokens = savedTokenizer;
            }
        }

        private PdfObject GetObjectByNumber(int number)
        {
            Assert(number >= 0 && number < xref.Reference.Length, "Object number out of bounds");
            if (xref.Pointer[number * 2] <= 0)
                return null; // object deleted
            if (xref.Reference[number] != null)
                return xref.Reference[number];

            tokens.Seek(xref.Pointer[number * 2]);
            tokens.NextValid();
            Assert(tokens.Type == Tokenizer.TokenType.NUMBER && tokens.IntValue == number, "Invalid object number found");
            tokens.NextValid();
            Assert(tokens.Type == Tokenizer.TokenType.NUMBER, "Invalid object generation found");
            tokens.NextValid();
            Assert(tokens.Value == "obj", "Token obj expected");

            PdfObject obj = ReadObject();
            xref.Reference[number] = obj;
            return obj;
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
                if (tokens.Type == Tokenizer.TokenType.ARRAY_END && !obj.IsArray())
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
                        
                        bool hasNext;
                        do
                        {
                            hasNext = tokens.Next();
                        } while (hasNext && tokens.Type == Tokenizer.TokenType.COMMENT);

                        if (hasNext && tokens.Value.Equals("stream"))
                        {
                            //skip whitespaces
                            /*int Char;
                            do
                            {
                                Char = tokens.File.Read();
                            } while (tokens.IsWhitespace(Char));
                            tokens.File.Back();*/
                            

                            int Char;
                            do
                            {
                                Char = tokens.Read();
                            } while (Char == 32 || Char == 9 || Char == 0 || Char == 12);
                            if (Char != '\n')
                                Char = tokens.Read();
                            if (Char != '\n')
                                tokens.File.Back();

                            return new PdfStream(tokens.File, tokens.File.Pointer, dictionary);
                        }
                        else
                        {
                            tokens.Seek(pos);
                            return dictionary;
                        }
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
                case Tokenizer.TokenType.REFERENCE:
                    return new PdfIndirectReference(xref, tokens.Reference, tokens.Generation);
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
    }
}
