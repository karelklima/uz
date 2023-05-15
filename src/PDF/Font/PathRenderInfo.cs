using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UZ.PDF.Font
{
    class PathRenderInfo : IRenderInfo
    {

        public enum Mode
        {
            Fill,
            FillEvenOdd,
            Stroke,
            FillAndStroke,
            FillAndStrokeEvenOdd
        }

        private Mode mode;
        private Vector start;
        private List<Line> lines = new List<Line>();
        private GraphicsState graphicsState;

        public PathRenderInfo(GraphicsState gs, Vector start)
        {
            this.graphicsState = gs;
            this.start = start;
        }

        public void Add(Vector point)
        {
            Vector st = start;
            if (lines.Count > 0)
                st = lines[lines.Count - 1].End;

            lines.Add(new Line(st, point));
        }

        public void SetMode(Mode mode)
        {
            this.mode = mode;
        }

        public Mode RenderMode
        {
            get
            {
                return mode;
            }
        }

        public Vector Start
        {
            get
            {
                return start;
            }
        }

        public List<Line> Lines
        {
            get
            {
                return lines;
            }
        }

        public GraphicsState GraphicsState
        {
            get { return graphicsState; }
        }
    }
}
