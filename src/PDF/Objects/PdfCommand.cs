using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UZ.PDF.Objects
{
    class PdfCommand : PdfObject
    {
        public PdfCommand(string text) : base(ObjectType.COMMAND, text) { }
    }
}
