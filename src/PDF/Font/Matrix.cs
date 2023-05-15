using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UZ.PDF.Font
{
    class Matrix
    {
        public const int
            i11 = 0,
            i12 = 1,
            i13 = 2,
            i21 = 3,
            i22 = 4,
            i23 = 5,
            i31 = 6,
            i32 = 7,
            i33 = 8;

        private float[] data;

        public Matrix()
        {
            data = new float[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 }; // identity matrix
        }

        public Matrix(Matrix other)
        {
            this.data = other.data;
        }

        public Matrix(float a, float b, float c, float d, float e, float f)
        {
            data = new float[] { a, b, 0, c, d, 0, e, f, 1 };
        }

        public Matrix(float x, float y)
        {
            data = new float[] { 1, 0, 0, 0, 1, 0, x, y, 1 };
        }

        public float this[int index]
        {
            get { return data[index]; }
            set { data[index] = value; }
        }

        public Matrix Multiply(Matrix Other)
        {
            Matrix Result = new Matrix();

            for (int i = 0; i < 9; i++)
            {
                Result[i] = 0;
                for (int x = 0; x < 3; x++)
                {
                    Result[i] += this[i - (i % 3) + x] * Other[3 * x + (i % 3)];
                }
            }

            return Result;
        }
    }
}
