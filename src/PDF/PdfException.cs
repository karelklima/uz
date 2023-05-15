using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UZ.PDF
{
    class PdfException : Exception
    {
        private string p;

        public PdfException(string message):base(message)
        { }

    }
}
