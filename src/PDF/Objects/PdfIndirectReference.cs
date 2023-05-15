using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UZ.PDF.Objects
{
    class PdfIndirectReference : PdfObject
    {
        private int number;
        private int generation;
        private CrossReferenceTable xref;

        public PdfIndirectReference(CrossReferenceTable xref, int number, int generation)
            : base(ObjectType.REFERENCE, new StringBuilder().Append(number).Append(" ").Append(generation).Append(" R").ToString())
        {
            this.xref = xref;
            this.number = number;
            this.generation = generation;
        }

        public int Number
        {
            get { return number; }
        }

        public int Generation
        {
            get { return generation; }
        }

        public override PdfObject GetTarget()
        {
            PdfObject target = xref.Reference[number];
            if (target == null || !target.IsReference())
                return target;
            else
                return target.GetTarget();
        }

        new public string ToString()
        {
            return new StringBuilder().Append(number).Append(" ").Append(generation).Append(" R").ToString();
        }
    }
}
