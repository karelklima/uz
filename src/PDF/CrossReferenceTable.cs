using System;
using System.Collections.Generic;
using System.Text;
using UZ.PDF.Objects;
using System.Collections;

namespace UZ.PDF
{
    class CrossReferenceTable
    {
        private bool newXRefType = false;
        private Dictionary<int, Hashtable> objectStreams = new Dictionary<int,Hashtable>();
        private int[] pointer;
        private PdfObject[] reference;
        
        public void EnsureLength(int length)
        {
            if (pointer == null)
            {
                pointer = new int[length];
                FillArray(pointer, 0, length); // Fill changed from -1 to 0
            }
            else if (pointer.Length < length)
            {
                int[] tmp = new int[length];
                FillArray(tmp, 0, length); // Fill changed from -1 to 0
                pointer.CopyTo(tmp, 0);
                pointer = tmp;
            }

            if (reference == null)
            {
                reference = new PdfObject[length];
                FillArray(reference, null, length);
            }
            else if (reference.Length < length)
            {
                PdfObject[] tmp = new PdfObject[length];
                FillArray(tmp, null, length);
                reference.CopyTo(tmp, 0);
                reference = tmp;
            }
        }

        public void SetNewXRefType(bool flag)
        {
            newXRefType = flag;
        }

        public int[] Pointer
        {
            get { return pointer; }
        }

        public PdfObject[] Reference
        {
            get { return reference; }
        }

        public Dictionary<int, Hashtable> ObjectStreams
        {
            get { return objectStreams; }
        }

        public bool NewXRefType
        {
            get { return newXRefType; }
        }

        private void FillArray(Array array, object toFill, int count)
        {
            for (int i = 0; i < count; array.SetValue(toFill, i), i++);
        }
    }
}
