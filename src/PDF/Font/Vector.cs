using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UZ.PDF.Font
{
    class Vector
    {
        private float x;
        private float y;
        private float z;
        private float lengthSquared;
        private float length;

        public Vector() : this(0, 0, 1) { }

        public Vector(float x, float y) : this(x, y, 1) { }

        public Vector(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.lengthSquared = x * x + y * y + z * z;
            this.length = (float)Math.Sqrt(this.lengthSquared);
        }

        public float X
        {
            get { return x; }
        }

        public float Y
        {
            get { return y; }
        }

        public float Z
        {
            get { return z; }
        }

        public float LengthSquared
        {
            get { return lengthSquared; }
        }

        public float Length
        {
            get { return length; }
        }

        public Vector Subtract(Vector other)
        {
            return new Vector(x - other.X, y - other.Y, z - other.Z);
        }

        public Vector CrossProduct(Matrix matrix)
        {
            float a = x * matrix[Matrix.i11] + y * matrix[Matrix.i21] + z * matrix[Matrix.i31];
            float b = x * matrix[Matrix.i12] + y * matrix[Matrix.i22] + z * matrix[Matrix.i32];
            float c = x * matrix[Matrix.i13] + y * matrix[Matrix.i23] + z * matrix[Matrix.i33];

            return new Vector(a, b, c);
        }

        public Vector Cross(Vector vector)
        {
            float a = y * vector.Z - z * vector.Y;
            float b = z * vector.X - x * vector.Z;
            float c = x * vector.Y - y * vector.X;

            return new Vector(a, b, c);
        }

        public string ToString()
        {
            StringBuilder output = new StringBuilder();
            output.Append("(");
            output.Append(X.ToString());
            output.Append(", ");
            output.Append(Y.ToString());
            output.Append(", ");
            output.Append(Z.ToString());
            output.Append(")");
            
            return output.ToString();
        }
    }
}
