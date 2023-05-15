using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UZ.PDF.Objects
{
    class PdfArray : PdfObject
    {
        protected List<PdfObject> objects;
        public PdfArray()
            : base(ObjectType.ARRAY, "Array")
        {
            objects = new List<PdfObject>();
        }

        public List<PdfObject> Objects
        {
            get { return objects; }
        }
    }
}
