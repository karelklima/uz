using System;
using System.Collections.Generic;
using System.Text;

namespace UZ
{
    class UZException : Exception
    {
        private string p;

        public UZException(string message)
            : base(message)
        { }

        public UZException(string message, params object[] args)
            : this(String.Format(message, args))
        { }

    }
}
