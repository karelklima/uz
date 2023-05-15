using System;
using System.Collections.Generic;
using System.Text;
using UZ.PDF;
using System.Text.RegularExpressions;

namespace UZ.Sbirka.Adaptery
{
    class NeznamyPredpis : IAdapter
    {
        public string Typ { get { return Index.NEZNAMY_PREDPIS; } }

        public int HlavniSekce
        {
            get { throw new NotImplementedException(); }
        }

        public Regex NazevRegex
        {
            get
            {
                return new Regex("(?=a)b"); // never match
            }
        }

        public Regex UvodRegex
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Regex ZaverRegex
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Text PostInterpret(Text text)
        {
            return text;
        }
    }
}
