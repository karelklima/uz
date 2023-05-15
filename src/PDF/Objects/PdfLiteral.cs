using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UZ.PDF.Objects
{
    class PdfLiteral : PdfObject
    {
        public PdfLiteral(string text) : base(ObjectType.LITERAL, text) { }
    }
}
