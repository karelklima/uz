using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UZ.PDF.Objects
{
    public class PdfObject : Pdf
    {
        public enum ObjectType
        {
            NULL,
            BOOLEAN,
            STRING,
            NAME,
            NUMBER,
            ARRAY,
            DICTIONARY,
            REFERENCE,
            OBJECT,
            LITERAL,
            STREAM,
            COMMAND
        }

        protected ObjectType type;
        protected byte[] bytes;

        protected PdfObject(ObjectType type, byte[] bytes)
        {
            this.type = type;
            this.bytes = new byte[bytes.Length];
            Array.Copy(bytes, 0, this.bytes, 0, bytes.Length);
        }

        protected PdfObject(ObjectType type, string text)
        {
            this.type = type;
            this.bytes = EncodingTools.StringToBytes(text);
        }

        public ObjectType Type
        {
            get { return type; }
        }

        public string Value
        {
            get { return ToString(); }
        }

        public virtual byte[] Bytes
        {
            get { return bytes; }
        }

        override public string ToString()
        {
            return EncodingTools.BytesToString(bytes);
        }

        public bool IsStream()
        {
            return type == ObjectType.STREAM;
        }

        public bool IsDictionary()
        {
            return type == ObjectType.DICTIONARY;
        }

        public bool IsArray()
        {
            return type == ObjectType.ARRAY;
        }

        public bool IsString()
        {
            return type == ObjectType.STRING || type == ObjectType.LITERAL;
        }

        public bool IsName()
        {
            return type == ObjectType.NAME;
        }

        public bool IsNumber()
        {
            return type == ObjectType.NUMBER;
        }

        public bool IsBoolean()
        {
            return type == ObjectType.BOOLEAN;
        }

        public bool IsNull()
        {
            return type == ObjectType.NULL;
        }

        public bool IsReference()
        {
            return type == ObjectType.REFERENCE;
        }

        public PdfObject GetTarget(PdfObject obj)
        {
            return obj == null ? null : obj.GetTarget();
        }

        public virtual PdfObject GetTarget()
        {
            return this;
        }

        public PdfObject Target
        {
            get { return GetTarget(); }
        }
    }
}
