using System;
using System.Text;

namespace UZ.PDF.Objects
{
    class PdfNull : PdfObject
    {
        public const string NULL = "null";
        public PdfNull(string text)
            : base(ObjectType.NULL, text)
        {
            if (!text.Equals(NULL))
                throw new PdfException("Unexpected null value: " + text);
        }
    }
}
