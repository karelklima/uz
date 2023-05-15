using System;

namespace UZ.PDF.Objects
{
    class PdfBoolean : PdfObject
    {
        public const string TRUE = "true";
        public const string FALSE = "false";
        private bool value;
        public PdfBoolean(string text)
            : base(ObjectType.BOOLEAN, text)
        {
            switch (text)
            {
                case TRUE: value = true; break;
                case FALSE: value = false; break;
                default:
                    throw new PdfException("Unexpected boolean value: " + text);
            }
        }

        new public bool Value
        {
            get { return value; }
        }
    }
}
