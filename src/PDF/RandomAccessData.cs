using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace UZ.PDF
{
    public class RandomAccessData
    {
        private int pointer = 0;
        private byte[] content;
        private int startOffset = 0;

        public RandomAccessData(string filename)
        {
            FileStream FS = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            content = StreamToArray(FS);
            FS.Close();
        }

        public RandomAccessData(byte[] content)
        {
            this.content = content;
        }

        public RandomAccessData(Stream stream)
        {
            this.content = StreamToArray(stream);
        }

        public byte[] StreamToArray(Stream Input)
        {
            byte[] Buffer = new byte[8192];
            MemoryStream Output = new MemoryStream();
            while (true)
            {
                int Read = Input.Read(Buffer, 0, Buffer.Length);
                if (Read < 1)
                    break;
                Output.Write(Buffer, 0, Read);
            }
            return Output.ToArray();
        }

        public byte[] Copy(int start, int length)
        {
            int begin = start + startOffset;
            if (begin > content.Length)
                throw new ArgumentOutOfRangeException();
            if (begin + length > content.Length)
                throw new ArgumentOutOfRangeException();
            byte[] output = new byte[length];
            Array.Copy(content, startOffset + start, output, 0, length);
            return output;
        }

        public int Length
        {
            get
            {
                return content.Length - startOffset;
            }
        }

        public int Pointer
        {
            get
            {
                return pointer - startOffset;
            }
        }

        public int StartOffset
        {
            get {
                return startOffset;
            }
            set {
                startOffset = value;
            }
        }

        public int Read()
        {
            if (pointer >= content.Length)
                return -1;
            return content[pointer++] & 0xff;
        }

        public void Seek(int Position)
        {
            pointer = Position + startOffset;
        }

        public void Back()
        {
            if (pointer > 0) pointer--;
        }
    }
}
