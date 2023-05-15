using System;
using System.Collections.Generic;
using System.Text;
using UZ.PDF;
using System.Text.RegularExpressions;

namespace UZ.Sbirka.Adaptery
{
    class UstavniZakon : IAdapter
    {
        public string Typ { get { return "Ústavní zákon"; } }
        

        public int HlavniSekce
        {
            get { return Sekce.CLANEK; }
        }

        public Regex NazevRegex
        {
            get
            {
                return new Regex("(^Ústavní zákon)|(^Ústava České republiky$)");
            }
        }

        public Regex UvodRegex
        {
            get
            {
                return new Regex("(^Česká[ ]?národní[ ]?rada[ ]?se[ ]?usnesla)|(^Parlament se usnesl)(.*)zákoně( České republiky)?(:?)$");
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
            Sekce.ISekce zasobnik = text.Uvod;
            if (zasobnik.Subsekce.Count > 6)
            {
                List<string> uvod = new List<string>();
                uvod.Add("1");
                uvod.Add("ÚSTAVNÍ ZÁKON");
                uvod.Add("České národní rady");
                uvod.Add("ze dne 16. prosince 1992");

                bool ustava = true;

                for (int i = 0; i < uvod.Count; i++)
                {
                    if (!zasobnik.Subsekce[i].UvodniUstanoveni.Equals(uvod[i]))
                    {
                        ustava = false;
                        break;
                    }
                }

                if (ustava)
                {
                    zasobnik.Subsekce.RemoveRange(6, zasobnik.Subsekce.Count - 6);
                    zasobnik.Subsekce[4].UvodniUstanoveni = "ÚSTAVA ČESKÉ REPUBLIKY";
                    zasobnik.Subsekce[5].UvodniUstanoveni = "Česká národní rada se usnesla na tomto ústavním zákoně:";
                }
            }
            return text;
        }
    }
}
