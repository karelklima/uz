using System;
using System.Collections.Generic;
using System.Text;
using UZ.PDF.Objects;

namespace UZ.PDF
{
    class DocumentSkeleton : Pdf
    {
        private CrossReferenceTable xref;
        private PdfDictionary trailer;

        private PdfDictionary catalogObject;
        private PdfDictionary pagesObject;
        private List<PdfDictionary> pages;

        public DocumentSkeleton(Reader reader)
        {
            this.xref = reader.XRef;
            this.trailer = reader.Trailer;
            BuildDocument();
        }

        public DocumentSkeleton(string filename) : this(new Reader(filename)) { }

        private void BuildDocument()
        {
            catalogObject = (PdfDictionary)trailer.Get("Root").GetTarget();
            pagesObject = (PdfDictionary)catalogObject.Get("Pages").GetTarget();
            pages = new List<PdfDictionary>(((PdfNumber)pagesObject.Get("Count").GetTarget()).IntValue);
            RegisterPage(pagesObject);
        }

        private void RegisterPage(PdfObject obj)
        {
            Assert(obj.IsDictionary(), "Page object must be a dictionary");
            PdfDictionary dictionary = (PdfDictionary)obj;
            Assert(dictionary.ContainsKey("Type"), "Pages information not found in dictionary #1");

            PdfName type = (PdfName)dictionary.Get("Type").GetTarget();
            switch (type.Value)
            {
                case "Pages":
                    {
                        PdfArray kids = (PdfArray)dictionary.Get("Kids").GetTarget();
                        foreach (PdfObject kid in kids.Objects)
                        {
                            RegisterPage(kid.GetTarget());
                        }
                        break;
                    }
                case "Page":
                    {
                        pages.Add(dictionary);
                        break;
                    }

                default:
                    {
                        Assert(false, "Pages information not found in dictionary #1");
                        break;
                    }
            }
        }

        public List<PdfDictionary> Pages
        {
            get { return pages; }
        }

        public CrossReferenceTable XRef
        {
            get { return xref; }
        }
    }
}
