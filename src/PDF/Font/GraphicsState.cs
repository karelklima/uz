using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace UZ.PDF.Font
{
    class GraphicsState
    {
        public Matrix CTM = new Matrix();

        public CMapFont TextFont;
        public float TextFontSize = 1f;
        public float CharacterSpacing = 0f;
        public float WordSpacing = 0f;
        public float HorizontalScaling = 1f;
        public float TextLeading = 0f;
        public float TextRise = 0f;
        public int TextRenderingMode = 0;

        public string ColorSpaceFill;
        public string ColorSpaceStroke;

        public Color FillColor;
        public Color StrokeColor;

        public float LineWidth;
        

        public GraphicsState()
        { }

        public GraphicsState(GraphicsState Other)
        {
            this.CTM = Other.CTM;
            this.TextFont = Other.TextFont;
            this.TextFontSize = Other.TextFontSize;
            this.CharacterSpacing = Other.CharacterSpacing;
            this.WordSpacing = Other.WordSpacing;
            this.HorizontalScaling = Other.HorizontalScaling;
            this.TextLeading = Other.TextLeading;
            this.TextRise = Other.TextRise;
            this.TextRenderingMode = Other.TextRenderingMode;

            this.ColorSpaceFill = Other.ColorSpaceFill;
            this.ColorSpaceStroke = Other.ColorSpaceStroke;
            this.FillColor = Other.FillColor;
            this.StrokeColor = Other.StrokeColor;

            this.LineWidth = Other.LineWidth;
        }
    }
}
