using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace UZ.Sbirka
{
    class Sazec
    {
        public const int DELKA_RADKU = 60;
        public const string PREDSAZENI = "   ";

        public static void VysazejTextDoSouboru(string soubor, Text text)
        {
            File.WriteAllText(soubor, VysazejText(text));
        }

        public static string VysazejText(Text text)
        {
            Sazec sazec = new Sazec(text.Kopie());
            return sazec.builder.ToString();
        }

        public static string VysazejText(Sekce.ISekce sekce)
        {
            Sazec sazec = new Sazec(sekce.GetKopie());
            return sazec.builder.ToString();
        }

        private StringBuilder builder = new StringBuilder();

        private Sazec(Text text)
        {
            builder = new StringBuilder();
            VysazejUvod(text.Uvod);
            VysazejObsah(text.Obsah);
            VysazejZaver(text.Zaver);
            VysazejPoznamky(text.Obsah.Poznamky);
        }

        private Sazec(Sekce.ISekce sekce)
        {
            builder = new StringBuilder();
            VysazejSekci(sekce);
        }

        private void VysazejUvod(Sekce.Zasobnik zasobnik)
        {
            foreach (Sekce.ISekce sekce in zasobnik.Subsekce)
                VysazejOdstavecNaStred(sekce.UvodniUstanoveni, 2);
        }

        private void VysazejObsah(Sekce.Zasobnik zasobnik)
        {
            foreach (Sekce.ISekce sekce in zasobnik.Subsekce)
                VysazejSekci(sekce);
        }

        private void VysazejZaver(Sekce.Zasobnik zasobnik)
        {
            foreach (Sekce.ISekce sekce in zasobnik.Subsekce)
                VysazejOdstavecNaStred(sekce.UvodniUstanoveni, 2);
        }

        private void VysazejSekci(Sekce.ISekce sekce)
        {
            if (sekce.Typ == Sekce.ZASOBNIK) // specialni pripad - vysazeny text musi byt zabaleny do uvozovek
            {
                if (sekce.Subsekce.Count < 1)
                    return;
                Sekce.ISekce prvni = sekce.Subsekce[0];
                if (prvni.Oznaceni.Length > 0)
                    prvni.Oznaceni = Interpret.UVOZOVKY_DOLE + prvni.Oznaceni;
                else
                    prvni.UvodniUstanoveni = Interpret.UVOZOVKY_DOLE + prvni.UvodniUstanoveni;

                Sekce.ISekce posledni = sekce.Subsekce[sekce.Subsekce.Count - 1];
                while (posledni.Subsekce.Count > 0)
                    posledni = posledni.Subsekce[posledni.Subsekce.Count - 1];

                if (posledni.ZaverecneUstanoveni.Length > 0)
                    posledni.ZaverecneUstanoveni = posledni.ZaverecneUstanoveni + Interpret.UVOZOVKY_NAHORE;
                else if (posledni.UvodniUstanoveni.Length > 0)
                    posledni.UvodniUstanoveni = posledni.UvodniUstanoveni + Interpret.UVOZOVKY_NAHORE;
                else
                    posledni.Oznaceni = posledni.Oznaceni + Interpret.UVOZOVKY_NAHORE;
            }
            else if (sekce.Typ == Sekce.ODSTAVEC || sekce.Typ == Sekce.PISMENO || sekce.Typ == Sekce.BOD)
            {
                // sekce, kde text zacina na stejnem radku jako oznaceni
                string predsazeni = sekce.Typ == Sekce.ODSTAVEC ? "" : PREDSAZENI + PREDSAZENI;
                VysazejOdstavec(sekce.Oznaceni + " " + sekce.UvodniUstanoveni, 1, PREDSAZENI, predsazeni);
            }
            else
            {
                if (sekce.Oznaceni.Length > 0)
                    VysazejOdstavecNaStred(sekce.Oznaceni, 2);
                if (sekce.Nadpis.Length > 0)
                    VysazejOdstavecNaStred(sekce.Nadpis, 2);
                if (sekce.UvodniUstanoveni.Length > 0)
                    VysazejOdstavec(sekce.UvodniUstanoveni, 1);
            }

            foreach (Sekce.ISekce subsekce in sekce.Subsekce)
            {
                VysazejSekci(subsekce);
            }

            if (sekce.ZaverecneUstanoveni.Length > 0)
            {
                VysazejOdstavec(sekce.ZaverecneUstanoveni);
                NovyRadek();
            }
            else if (sekce.Typ == Sekce.CLANEK || sekce.Typ == Sekce.PARAGRAF || sekce.Typ == Sekce.PREAMBULE)
                NovyRadek();
        }

        private void VysazejOdstavec(string text, int odradkovani = 1, string predsazeniPrvniRadek = PREDSAZENI, string predsazeniDalsiRadky = "")
        {
            bool prvni = true;
            List<string> radky = new List<string>();
            foreach (string radek in RozradkujText(text))
            {
                radky.Add((prvni ? predsazeniPrvniRadek : predsazeniDalsiRadky) + radek);
                prvni = false;
            }
            builder.Append(String.Join("\n", radky));
            for (int i = 0; i < odradkovani; i++)
                NovyRadek();
        }

        private void VysazejOdstavecNaStred(string text, int odradkovani)
        {
            List<string> radky = new List<string>();
            foreach (string radek in RozradkujText(text))
            {
                radky.Add(radek.PadLeft((DELKA_RADKU - radek.Length) / 2 + radek.Length, ' '));
            }
            builder.Append(String.Join("\n", radky));
            for (int i = 0; i < odradkovani; i++)
                NovyRadek();
        }

        private List<string> RozradkujText(string text)
        {
            text = text.Replace("<>)", ")"); // odebrani markeru pro poznamku pod carou

            List<string> radky = new List<string>();

            StringBuilder radek = new StringBuilder();
            string[] slova = text.Split(' ');

            if (slova.Length < 1)
                return radky;

            radek.Append(slova[0]);
            int delka = slova[0].Length;
            for (int i = 1; i < slova.Length; i++)
            {
                if (delka + 1 + slova[i].Length > DELKA_RADKU)
                {
                    radky.Add(radek.ToString());
                    radek.Clear();
                    delka = slova[i].Length;
                }
                else
                {
                    radek.Append(" ");
                    delka = delka + slova[i].Length + 1;
                }
                radek.Append(slova[i]);
            }
            if (radek.Length > 0)
                radky.Add(radek.ToString());

            return radky;
        }

        private void NovyRadek()
        {
            builder.Append("\n");
        }

        private void VysazejPoznamky(List<Sekce.ISekce> sekce)
        {
            if (sekce.Count < 1)
                return;

            NovyRadek();
            NovyRadek();
            builder.Append("---------------");
            NovyRadek();

            foreach (Sekce.ISekce poznamka in sekce)
            {
                string text = poznamka.Cislo + ") " + poznamka.UvodniUstanoveni;
                VysazejOdstavec(text, 1, "", "   ");
            }
        }
    }
}
