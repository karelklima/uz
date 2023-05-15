using System;
using System.Collections.Generic;
using System.Text;

namespace UZ.PDF.Font
{
    class LineRenderInfo : IRenderInfo
    {
        private float thickness;
        private Line line;

        private GraphicsState graphicsState;

        public GraphicsState GraphicsState
        {
            get { return graphicsState; }
        }

        public Line Line
        {
            get { return line; }
        }

        public float Thickness
        {
            get { return thickness; }
        }

        public LineRenderInfo(float posX, float posY, float width, float height, GraphicsState graphicsState)
        {
            float vertical = (posY + posY + height) / 2f;
            this.line = new Line(new Vector(posX, vertical), new Vector(posX + width, vertical));
            this.thickness = Math.Abs(height);
            this.graphicsState = graphicsState;
        }
    }
}
