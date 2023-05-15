using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UZ.PDF.Objects
{
    class PdfStream : PdfDictionary
    {
        private RandomAccessData source;
        private int offset;

        private byte[] original;

        private byte[] stream;

        public PdfStream(RandomAccessData source, int offset, PdfDictionary dictionary)
            :base(ObjectType.STREAM, "Stream")
        {
            this.source = source;
            this.offset = offset;
            Merge(dictionary);
        }

        public void Load()
        {
            int a = 0;
            if (source == null)
                return;
            PdfNumber lenObj = (PdfNumber)GetTarget(Get("Length"));
            Assert(lenObj != null && lenObj.IntValue > 0, "Invalid stream length value");

            int length = lenObj.IntValue;

            stream = source.Copy(offset, length);
            original = source.Copy(offset, length);

            if (ContainsKey("Filter"))
            {
                PdfArray filters;
                if (Get("Filter").IsArray())
                    filters = (PdfArray)Get("Filter");
                else
                {
                    filters = new PdfArray();
                    filters.Objects.Add(Get("Filter"));
                }

                foreach (PdfObject filter in filters.Objects)
                {
                    PdfName filterName = (PdfName)filter;
                    if (filterName.Value == "FlateDecode")
                        stream = EncodingTools.FlateDecode(stream);
                    if (filterName.Value == "LZWDecode")
                        stream = EncodingTools.LZWDecode(stream);
                    if (filterName.Value == "ASCII85Decode")
                        stream = EncodingTools.ASCII85Decode(stream);
                }

                if (original.Length > 0 && stream.Length < 1)
                    throw new PdfException("Unable to decode stream");
            }

            if (ContainsKey("DecodeParms"))
            {
                PdfDictionary decodeParms = (PdfDictionary)Get("DecodeParms");
                int predictor = 0;
                if (decodeParms.ContainsKey("Predictor"))
                    predictor = ((PdfNumber)decodeParms.Get("Predictor")).IntValue;
                int columns = 1;
                if (decodeParms.ContainsKey("Columns"))
                    columns = ((PdfNumber)decodeParms.Get("Columns")).IntValue;

                stream = EncodingTools.DecodePredictor(stream, predictor, columns);
            }
        }

        public byte[] Stream
        {
            get { return stream; }
        }

        public byte[] Original
        {
            get { return original; }
        }
    }
}
