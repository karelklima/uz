using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UZ.PDF
{
    public class Pdf
    {
        public const double SupportedPDFVersion = 1.7;

        public const string FontResourcesDirectory = "PDF/Font/Resources";

        protected void Assert(bool expression, string message)
        {
            if (!expression)
                throw new PdfException(message);
        }
    }
}
