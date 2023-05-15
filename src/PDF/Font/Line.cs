using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UZ.PDF.Font
{
    class Line
    {
        private Vector start;
        private Vector end;

        public Line(Vector start, Vector end)
        {
            this.start = start;
            this.end = end;
        }

        public Vector Start
        {
            get { return start; }
        }

        public Vector End
        {
            get { return end; }
        }

        public Line Vector
        {
            get { return new Line(new Vector(0, 0), new Vector(end.X - start.X, end.Y - start.Y)); }
        }

        public float Length
        {
            get { return (float)Math.Sqrt(Math.Pow(Vector.End.X, 2) + Math.Pow(Vector.End.Y, 2)); }
        }

        public Line NormalizedVector
        {
            get { return new Line(new Vector(0, 0), new Vector(Vector.End.X / Length, Vector.End.Y / Length)); }
        }

        public Line TransformBy(Matrix textMatrix)
        {
            return new Line(start.CrossProduct(textMatrix), end.CrossProduct(textMatrix));
        }

        public float GetVerticalDistanceFrom(Line other)
        {
            // counts distance of a point from this line
            return Math.Abs(start.Y - other.start.Y);
        }

        public float GetTrueVerticalDistanceFrom(Line other)
        {
            return other.start.Y - start.Y;
        }

        public float GetTrueHorizontalDistanceFrom(Line other)
        {
            return start.X - other.end.X;
        }
    }
}
