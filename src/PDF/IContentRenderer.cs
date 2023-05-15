using System;
using UZ.PDF.Objects;
using UZ.PDF.Font;

namespace UZ.PDF
{
    interface IContentRenderer
    {
        void BeginPage(PdfDictionary resources);
        void BeginTextObject();
        void EndPage(PdfDictionary resources);
        void EndTextObject();
        void RenderText(TextRenderInfo renderInfo);
        void RenderLine(LineRenderInfo renderInfo);
        void RenderImage(ImageRenderInfo renderInfo);
        void RenderPath(PathRenderInfo renderInfo);
        void RenderXObject(PdfObject xobject);
    }
}
