using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UZ.PDF.Font
{
    class CodespaceRange
    {
        private byte[] start;
        private byte[] end;

        public CodespaceRange(byte[] start, byte[] end)
        {
            this.start = start;
            this.end = end;
        }

        public byte[] Start
        {
            get { return start; }
        }

        public byte[] End
        {
            get { return End; }
        }
    }
}
