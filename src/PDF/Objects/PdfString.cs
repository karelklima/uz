using System;
using System.Text;

namespace UZ.PDF.Objects
{
    public class PdfString : PdfObject
    {
        protected string value = "";
        protected string encoding = "PDF";
        protected bool hexWriting = false;

        public PdfString(string text) : base(ObjectType.STRING, text)
        {
            this.value = text;
        }

        public PdfString(string text, string encoding) : base(ObjectType.STRING, text)
        {
            this.value = text;
            this.encoding = encoding;
        }

        public PdfString(byte[] bytes) : base(ObjectType.STRING, bytes)
        {
            this.value = EncodingTools.ConvertToString(bytes, null);
            this.encoding = "";
        }

        /*public string ToUnicodeString ()
        {
            return new UnicodeEncoding(true, false).GetString(bytes);
        }*/

        public override string ToString()
        {
            return ToUnicodeString();
        }

        public String ToUnicodeString()
        {
            if (encoding != null && encoding.Length != 0)
            {
                //throw new NotImplementedException();
                return value;
            }
            //GetBytes();
            if (bytes.Length >= 2 && bytes[0] == (byte)254 && bytes[1] == (byte)255)
                return EncodingTools.ConvertToString(bytes, "UnicodeBig");
            else
                return EncodingTools.ConvertToString(bytes, "PDF");
        }

        public string Unicode
        {
            get { return ToUnicodeString(); }
        }

        public PdfString SetHexWriting(bool hexWriting)
        {
            this.hexWriting = hexWriting;
            return this;
        }

        public bool IsHexWriting()
        {
            return hexWriting;
        }

        public virtual byte[] StringBytes
        {
            get
            {
                if (bytes != null)
                    return bytes;
                if (encoding != null && encoding.Equals("UnicodeBig") && EncodingTools.IsPdfDocEncoding(value))
                    return EncodingTools.ConvertToBytes(value, "PDF");
                else
                    return EncodingTools.ConvertToBytes(value, encoding);
            }
        }
    }
}
