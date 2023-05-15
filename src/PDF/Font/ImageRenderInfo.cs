using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UZ.PDF.Objects;

namespace UZ.PDF.Font
{
    class ImageRenderInfo : IRenderInfo
    {
        private byte[] data;
        private PdfDictionary imageParams;
        private PdfDictionary colorParams;
        private bool inline;
        private GraphicsState graphicsState;

        public GraphicsState GraphicsState
        {
            get { return graphicsState; }
        }

        public bool Inline { get { return inline; } }
        

        public ImageRenderInfo(byte[] data, GraphicsState graphicsState, PdfDictionary imageParams, PdfDictionary colorParams, bool inline = false)
        {
            this.data = data;
            this.graphicsState = graphicsState;
            this.imageParams = imageParams;
            this.colorParams = colorParams;
            this.inline = inline;
        }
    }
}
