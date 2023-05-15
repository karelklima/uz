using System;
using System.Text;
using UZ.PDF.Font;
using UZ.PDF.Objects;
using System.Collections.Generic;
using System.Drawing;

namespace UZ.PDF
{
    class ContentRenderer : IContentRenderer
    {

        private StructuredDocument document = new StructuredDocument();
        private const float textLeadingTreshold = 5f;

        private CharacterRenderInfo last;

        private float spaceWidthFactor = 0.5f;
        private float lineBreakFactor = 1.0f;


        class CharacterRenderInfo
        {
            char character;
            CMapFont font;
            Line baseline;
            Line ascentline;
            Line descentline;
            float spaceWidth;

            public CharacterRenderInfo(char character, CMapFont font, Line baseline, Line ascentline, Line descentline, float spaceWidth)
            {
                this.character = character;
                this.font = font;
                this.baseline = baseline;
                this.ascentline = ascentline;
                this.descentline = descentline;
                this.spaceWidth = spaceWidth;
            }

            public char Character { get { return character; } }
            public CMapFont Font { get { return font; } }
            public Line BaseLine { get { return baseline; } }
            public Line AscentLine { get { return ascentline; } }
            public Line DescentLine { get { return descentline; } }
            public float LineHeight { get { return ascentline.Start.Y - descentline.Start.Y; } }
            public float SpaceWidth { get { return spaceWidth; } }
        }


        public ContentRenderer(float spaceWidthFactor = 0f, float lineBreakFactor = 0f)
        {
            if (spaceWidthFactor > 0f)
                this.spaceWidthFactor = spaceWidthFactor;
            if (lineBreakFactor > 0f)
                this.lineBreakFactor = lineBreakFactor;
        }
        
        public StructuredDocument Document
        {
            get { return document; }
        }

        public void BeginPage(PdfDictionary resources)
        {
            document.AddPage();
        }

        public void EndPage(PdfDictionary resources) { }

        public void BeginTextObject() { }
        public void EndTextObject() { }

        public void RenderText(TextRenderInfo renderInfo)
        {
            StructuredDocument.Page page = document.LastPage;

            string text = renderInfo.Text;
            float charSpacing = renderInfo.CharacterSpacing;
            float spaceWid = renderInfo.SpaceWidth;

            CMapFont font = renderInfo.GraphicsState.TextFont;
            string fontName = renderInfo.GraphicsState.TextFont.Name;
            PdfDictionary fontDescriptor = (PdfDictionary)renderInfo.GraphicsState.TextFont.Dictionary.Get("FontDescriptor").Target;

            string desc = font.Flags.ToString() + " " + font.ItalicAngle.ToString() + " " + font.StemV.ToString();

            bool bold = font.IsBold;
            bool italic = font.IsItalic;
            bool highlighted = font.IsHighlighted;

            Line baseline = renderInfo.BaseLine;
            Line ascentline = renderInfo.AscentLine;
            Line descentline = renderInfo.DescentLine;
            for (int i = 0; i < renderInfo.Text.Length; i++)
            {
                float charWidth = renderInfo.GetRangeWidth(i, 1);
                float rangeWidth = renderInfo.GetRangeWidth(0, i + 1);
                float distFromStart = rangeWidth - charWidth;
                Vector charStart = new Vector(baseline.Start.X + distFromStart, baseline.Start.Y);
                Vector charEnd = new Vector(baseline.Start.X + distFromStart + charWidth, baseline.Start.Y);

                CharacterRenderInfo charInfo = new CharacterRenderInfo(renderInfo.Text[i],
                    renderInfo.GraphicsState.TextFont,
                    GetLineSegment(renderInfo.BaseLine, distFromStart, charWidth),
                    GetLineSegment(renderInfo.AscentLine, distFromStart, charWidth),
                    GetLineSegment(renderInfo.DescentLine, distFromStart, charWidth),
                    renderInfo.SpaceWidth);

                RenderCharacter(charInfo);
            }
        }

        private Line GetLineSegment(Line line, float start, float length)
        {
            return new Line(
                new Vector(line.Start.X + start, line.Start.Y),
                new Vector(line.Start.X + start + length, line.Start.Y)
                );
        }

        private void RenderCharacter(CharacterRenderInfo current)
        {
            StructuredDocument.Page page = document.LastPage;

            if (last != null)
            {
                Line baseline = current.BaseLine;
                Line lastBaseline = last.BaseLine;

                char character = current.Character;

                float verticalGap = baseline.GetVerticalDistanceFrom(lastBaseline);
                float verticalDistance = baseline.GetTrueVerticalDistanceFrom(lastBaseline);
                float horizontalDistance = baseline.GetTrueHorizontalDistanceFrom(lastBaseline);
                //float spaceWidth = current.LineHeight * 0.4f;
                float spaceWidth = current.SpaceWidth;
                if (spaceWidth == 0f)
                    spaceWidth = current.BaseLine.Length;
                float newLineTreshold = lineBreakFactor * 1.5f * current.LineHeight;

                float spaceTreshold = spaceWidth * spaceWidthFactor;

                if (EncodingTools.IsAccent(current.Character))
                {
                    page.LastParagraph.Append(current.Character.ToString(), current.BaseLine.Start, current.BaseLine.End);
                    current = last; // it gets swaped few lines below
                }
                else
                {
                    if (last.DescentLine.End.Y < current.AscentLine.Start.Y
                        && last.AscentLine.End.Y > current.DescentLine.Start.Y)
                    {
                        if (horizontalDistance > spaceTreshold * 0.3 && horizontalDistance < spaceTreshold * 2f)
                            page.LastParagraph.Append(" ");
                        else if (Math.Abs(horizontalDistance) > spaceTreshold * 2f)
                            page.LastParagraph.NewRow();
                        else if (last.BaseLine.End.Y > current.BaseLine.Start.Y)
                            page.LastParagraph.Append("<>"); // sup index
                    }
                    else
                    {
                        if (verticalDistance > 0 && verticalDistance < newLineTreshold)
                            page.LastParagraph.NewRow();
                        else
                            page.AddParagraph();
                    }
                    
                    page.LastParagraph.Append(current.Character.ToString(), baseline.Start, baseline.End);

                }
            }
            else
                page.LastParagraph.Append(current.Character.ToString(), current.BaseLine.Start, current.BaseLine.End);

            if (page.LastParagraph.FontStyle == StructuredDocument.Paragraph.FONT_NORMAL)
            {
                if (current.Font.IsBold)
                    page.LastParagraph.FontStyle = StructuredDocument.Paragraph.FONT_BOLD;
                else if (current.Font.IsItalic)
                    page.LastParagraph.FontStyle = StructuredDocument.Paragraph.FONT_ITALIC;
            }

            last = current;
        }

        public void RenderLine(LineRenderInfo renderInfo)
        {
            float briFill = 0f;
            if (renderInfo.GraphicsState.FillColor != null)
                briFill = renderInfo.GraphicsState.FillColor.GetBrightness();

            float briStroke = 0f;
            if (renderInfo.GraphicsState.StrokeColor != null)
                briStroke = renderInfo.GraphicsState.StrokeColor.GetBrightness();

            if (briFill > 0.9)
                return; // too bright to paint
            StructuredDocument.Page page = document.LastPage;
            page.AddLine(renderInfo.Line.Start, renderInfo.Line.End, renderInfo.Thickness);
        }

        public void RenderImage(ImageRenderInfo renderInfo)
        {
            // TODO
            if (renderInfo.Inline)
                document.LastPage.LastParagraph.Append("<image>");
            else
            {
                document.LastPage.AddParagraph();
                document.LastPage.LastParagraph.Append("<image>");
                document.LastPage.AddParagraph();
            }
        }

        public void RenderXObject(PdfObject xobject)
        {
            // TODO: process images - insert to paragraph
        }

        public void RenderPath(PathRenderInfo renderInfo)
        {
            PathRenderInfo ri = renderInfo;
            List<Line> lines = renderInfo.Lines;

            if (renderInfo.RenderMode == PathRenderInfo.Mode.Fill || renderInfo.RenderMode == PathRenderInfo.Mode.FillEvenOdd
                || renderInfo.RenderMode == PathRenderInfo.Mode.FillAndStroke || renderInfo.RenderMode == PathRenderInfo.Mode.FillAndStrokeEvenOdd)
            {
                StructuredDocument.Box box = new StructuredDocument.Box();
                foreach (Line line in renderInfo.Lines)
                {
                    box.AddPoint(line.Start.CrossProduct(renderInfo.GraphicsState.CTM));
                    box.AddPoint(line.End.CrossProduct(renderInfo.GraphicsState.CTM));
                }
                RenderLine(new LineRenderInfo(box.MinX, box.MinY, box.MaxX - box.MinX, box.MaxY - box.MinY, renderInfo.GraphicsState));
            }
            else if (renderInfo.RenderMode == PathRenderInfo.Mode.Stroke)
            {
                foreach (Line line in renderInfo.Lines)
                {
                    StructuredDocument.Box box = new StructuredDocument.Box();
                    box.AddPoint(line.Start.CrossProduct(renderInfo.GraphicsState.CTM));
                    box.AddPoint(line.End.CrossProduct(renderInfo.GraphicsState.CTM));
                    RenderLine(new LineRenderInfo(box.MinX, box.MinY, box.MaxX - box.MinX, box.MaxY - box.MinY, renderInfo.GraphicsState));
                }
            }
            else
            {
                throw new PdfException("Unknown path render mode");
            }
        }

    }
}
