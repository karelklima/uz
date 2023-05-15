using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UZ.PDF.Objects
{
    class PdfNumber : PdfObject
    {
        double value;
        public PdfNumber(string text)
            : base(ObjectType.NUMBER, text)
        {
            try
            {
                value = Double.Parse(text, EncodingTools.NumberFormat); // use standard non-locale-dependant format
            }
            catch (Exception e)
            {
                throw new PdfException("Invalid number format: " + text);
            }
        }
        
        public int IntValue
        {
            get { return (int)value; }
        }

        public double DoubleValue
        {
            get { return value; }
        }

        public float FloatValue
        {
            get { return (float)value; }
        }
    }
}
