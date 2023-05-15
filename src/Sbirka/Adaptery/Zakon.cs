using System;
using System.Collections.Generic;
using System.Text;
using UZ.PDF;
using System.Text.RegularExpressions;

namespace UZ.Sbirka.Adaptery
{
    class Zakon : IAdapter
    {
        public string Typ { get { return "Zákon"; } }
        

        public int HlavniSekce
        {
            get { return Sekce.PARAGRAF; }
        }

        public Regex NazevRegex
        {
            get
            {
                return new Regex("^Zákon");
            }
        }

        public Regex UvodRegex
        {
            get
            {
                return new Regex("(^Česká[ ]?národní[ ]?rada[ ]?se[ ]?usnesla)|(^Parlament se usnesl)|(^Federální shromáždění České a Slovenské Federativní Republiky se usneslo)(.*)zákoně( České republiky)?(:?)$");
            }
        }

        public Regex ZaverRegex
        {
            get
            {
                return new Regex("v\\.[ ]*r\\.$");
            }
        }


        public Text PostInterpret(Text text)
        {
            return text;
        }
    }
}
