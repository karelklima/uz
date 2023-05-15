using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UZ.PDF.Font
{
    class TextRenderInfo : IRenderInfo
    {

        public const float textLeadingTreshold = 1.5f;

        
        private string text;
        private char spaceCharacter;
        private GraphicsState graphicsState;
        private Matrix textMatrix;

        public TextRenderInfo(string text, GraphicsState graphicsState, Matrix textMatrix, char wordSpaceCharacter)
        {
            this.text = text;
            this.spaceCharacter = wordSpaceCharacter;

            this.graphicsState = graphicsState;
            this.textMatrix = textMatrix.Multiply(graphicsState.CTM); // text to user space matrix

            /*this.fullUnscaledWidth = GetUnscaledRangeWidth(0, text.Length, false);
            this.unscaledWidth = GetUnscaledRangeWidth();

            // counts unscaled width of space
            this.unscaledSpaceWidth = ((graphicsState.TextFont.SpaceWidth / 1000f) * graphicsState.TextFontSize
                + graphicsState.CharacterSpacing + graphicsState.WordSpacing) * graphicsState.HorizontalScaling;*/

            //this.unscaledSpaceWidth = ((graphicsState.TextFont.SpaceWidth / 1000f) * graphicsState.TextFontSize
            //    + graphicsState.CharacterSpacing + graphicsState.WordSpacing) * graphicsState.HorizontalScaling;

            /*this.unscaledSpaceWidth = ((graphicsState.TextFont.GetWidth(' ') / 1000f) * graphicsState.TextFontSize
                + graphicsState.CharacterSpacing + graphicsState.WordSpacing) * graphicsState.HorizontalScaling;

            this.scaledCharacterSpacing = Scale(graphicsState.CharacterSpacing).Length;
            this.scaledWordSpacing = Scale(graphicsState.WordSpacing).Length;*/
        }

        public string Text
        {
            get { return text; }
        }

        public char SpaceCharacter
        {
            get { return spaceCharacter; }
        }

        public Matrix TextMatrix
        {
            get { return textMatrix; }
        }

        public GraphicsState GraphicsState
        {
            get { return graphicsState; }
        }

        public float SpaceWidth
        {
            get
            {
                return Scale(GetUnscaledSpaceWidth()).Length;
            }
        }

        public float CharacterSpacing
        {
            get { return Scale(graphicsState.CharacterSpacing).Length; }
        }

        public float WordSpacing
        {
            get { return Scale(graphicsState.WordSpacing).Length; }
        }

        public float Ascender
        {
            get
            {
                return graphicsState.TextFont.Ascender * graphicsState.TextFontSize / 1000;
            }
        }

        public float Descender
        {
            get
            {
                return graphicsState.TextFont.Descender * graphicsState.TextFontSize / 1000;
            }
        }

        public Line AscentLine
        {
            get
            {
                return GetUnscaledBaselineWithOffset(Ascender + graphicsState.TextRise).TransformBy(textMatrix);
            }
        }

        public Line DescentLine
        {
            get
            {
                return GetUnscaledBaselineWithOffset(Descender + graphicsState.TextRise).TransformBy(textMatrix);
            }
        }

        public Line BaseLine
        {
            get
            {
                return Scale(GetUnscaledRangeWidth());
            }
        }

        public float FullUnscaledWidth
        {
            get
            {
                return GetUnscaledRangeWidth(0, text.Length, false);
            }
        }

        private Line Scale(float toScale)
        {
            return new Line(new Vector(), new Vector(toScale, 0)).TransformBy(textMatrix);
        }

        private float GetUnscaledRangeWidth(int start = -1, int length = -1, bool skipLastSpace = true)
        {
            if (start < 0)
                start = 0;
            if (length < 0)
                length = text.Length - start; // rest

            if (start + length > text.Length)
                throw new PdfException("Invalid range specified");

            char[] Chars = text.ToCharArray();
            float width = 0f;
            for (int i = start; i < start + length; i++)
            {
                float cw = graphicsState.TextFont.GetWidth(Chars[i]) / 1000.0f;
                float wordSpace = Chars[i] == spaceCharacter ? graphicsState.WordSpacing : 0f;
                if (i == start + length - 1 && skipLastSpace) // last
                    width += (cw * graphicsState.TextFontSize) * graphicsState.HorizontalScaling;
                else
                    width += (cw * graphicsState.TextFontSize + graphicsState.CharacterSpacing + wordSpace) * graphicsState.HorizontalScaling;
            }
            return width;
        }

        public float GetRangeWidth(int start = -1, int length = -1)
        {
            return Scale(GetUnscaledRangeWidth(start, length)).Length;
        }

        private float GetUnscaledSpaceWidth()
        {
            return ((graphicsState.TextFont.SpaceWidth / 1000f) * graphicsState.TextFontSize
                + graphicsState.CharacterSpacing + graphicsState.WordSpacing) * graphicsState.HorizontalScaling;
        }

        private Line GetUnscaledBaselineWithOffset(float yOffset)
        {
            return new Line(new Vector(0, yOffset), new Vector(GetUnscaledRangeWidth(), yOffset));
        }
    }
}
