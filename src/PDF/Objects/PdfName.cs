using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UZ.PDF.Objects
{
    class PdfName : PdfObject
    {
        public PdfName(string text):base(ObjectType.NAME, text) { }
    }
}
