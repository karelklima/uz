using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UZ.PDF;

namespace UZ.Sbirka
{
    class Novelizator
    {
        const string CITACE_SEPARATOR = "_";
        const int VETA = 100;
        const int POSLEDNI = 100;
        const string CESTA = "<cesta>";
        const string UKAZATEL = "<ukazatel>";
        const string UKAZATEL_VETA = "<ukazatel:veta>";
        const string SLOVO = "<slovo>";
        const string SLOVEM = "<slovem>";
        const string CITACE = "<citace>";
        const string OTAZNIK = "?";
        const string ZRUSUJICI_TEXT = "(zrušen)";
        const string VELKA_PISMENA = "AÁBCČDĎEÉFGHIÍJKLMNŇOÓPQRŘSŠTŤUÚŮVWXYÝZ";

        static Regex cilenyUkazatel = new Regex("^<ukazatel:(.*)>$");
        static Regex operaceRegex = new Regex("^<.*>$");
        static Regex slovoRegex = new Regex("^(slovo|slova|věta|věty|částka|číslo|číslice|písmeno|písmena)$");
        static Regex slovemRegex = new Regex("^(slovem|slovy|větou|větami|částkou|číslem|číslicí|písmenem|písmeny)$");

        public const string ZMENA = "ZMENA";
        public const string DOPLNENI = "DOPLNENI";
        public const string ZRUSENI = "ZRUSENI";

        public static string aktualniNovelaID = string.Empty;
        public static string aktualniNovelaSekceID = string.Empty;
        public static string aktualniNovelaBodID = string.Empty;

        public static Text UplneZneni(Predpis predpis, Text original, Text novela, string novelaID)
        {
            statistika = null;
            Novelizator.aktualniNovelaID = novelaID;
            Novelizator n = new Novelizator(predpis.Cislo, predpis.Rocnik, predpis.Adapter, original, novela);
            return n.originalText;
        }

        public static Statistika UplneZneniTest(int cislo, int rocnik, IAdapter adapter, Text novela)
        {
            statistika = new Statistika();
            Novelizator n = new Novelizator(cislo, rocnik, adapter, new Text(), novela);
            return statistika;
        }

        public static List<string> ZmenyPredpisu(IAdapter adapter, Text text)
        {
            Dictionary<string, Sekce.ISekce> zmeny = new Dictionary<string,Sekce.ISekce>();
            NajdiNovelizacniSekce(adapter, text.Obsah, zmeny);
            List<string> predpisy = new List<string>();
            foreach (KeyValuePair<string, Sekce.ISekce> zmena in zmeny)
                predpisy.Add(zmena.Key);
            return predpisy;
        }

        public class Statistika
        {
            public List<Novelizator.IOperace> Operace = new List<IOperace>();
            public List<Sekce.ISekce> NerozpoznaneBody = new List<Sekce.ISekce>();
        }

        public class Ukazatel
        {
            private bool validni = false;
            private int typ;
            private List<string> data;
            private List<string> cisla;
            private List<string> oznaceni;
            private bool interval = false;

            public int Typ { get { return typ; } }
            public List<string> Data { get { return data; } }
            public List<string> Cisla { get { return cisla; } }
            public string PrvniCislo { get { return cisla[0]; } }
            public List<string> Oznaceni { get { return oznaceni; } }
            public string PrvniOznaceni { get { return oznaceni[0]; } }
            public bool Interval { get { return interval; } }
            public bool JeValidni { get { return validni; } }
            
            public Ukazatel(int typ, string cislo)
            {
                this.typ = typ;
                data = new List<string>();
                data.Add(cislo);
                cisla = NormalizujSeznam(typ, data);
                oznaceni = GenerujOznaceni(typ, cisla);

                this.validni = true;
            }

            public Ukazatel(List<string> vstup, bool smazatData = true)
            {
                if (vstup.Count < 2 || !Novelizator.klice.ContainsKey(vstup[0]))
                    return;

                string typData = vstup[0];
                typ = Novelizator.klice[typData];

                vstup.RemoveAt(0); // odebereme kvuli parsovani seznamu a intervalu

                data = NactiInterval(vstup, smazatData);
                interval = true;
                if (data.Count < 1)
                {
                    data = NactiSeznam(vstup, smazatData);
                    interval = false;
                }
                if (!smazatData)
                    vstup.Insert(0, typData);

                cisla = NormalizujSeznam(typ, data);
                if (interval)
                    cisla = GenerujInterval(typ, cisla[0], cisla[1]);
                oznaceni = GenerujOznaceni(typ, cisla);

                this.validni = true;
            }
        }

        public class OperaceUzel
        {
            private IOperace operace;
            private Dictionary<string, OperaceUzel> uzly = new Dictionary<string, OperaceUzel>();

            public IOperace Operace { get { return operace; } }
            public Dictionary<string, OperaceUzel> Uzly { get { return uzly; } }

            public void Add(List<string> maska, IOperace operace)
            {
                if (maska.Count < 1)
                    this.operace = operace;
                else
                {
                    string klic = maska[0];
                    if (klic.StartsWith(OTAZNIK)) // fakultativni cast
                    {
                        klic = klic.Substring(OTAZNIK.Length);
                        List<string> kopie = new List<string>(maska);
                        kopie.RemoveAt(0);
                        this.Add(kopie, operace);
                    }
                    if (!uzly.ContainsKey(klic))
                        uzly.Add(klic, new OperaceUzel());

                    maska.RemoveAt(0);
                    uzly[klic].Add(maska, operace);
                }
            }
        }

        private static OperaceUzel operace = new OperaceUzel();

        private static Dictionary<string, int> slovniCislovani = new Dictionary<string, int>();
        private static Dictionary<string, int> klice = new Dictionary<string, int>();
        public static Dictionary<string, int> Klice { get { return klice; }}

        private static Regex cisloRegex = new Regex("^[0-9]{1,}$");
        private static Regex pismenoRegex = new Regex("^[a-z]{1,}$");

        static Novelizator()
        {
            klice.Add("§", Sekce.PARAGRAF);
            klice.Add("čl.", Sekce.CLANEK);
            klice.Add("Čl.", Sekce.CLANEK);
            klice.Add("článek", Sekce.CLANEK);
            klice.Add("články", Sekce.CLANEK);
            klice.Add("odst.", Sekce.ODSTAVEC);
            klice.Add("odstavec", Sekce.ODSTAVEC);
            klice.Add("odstavce", Sekce.ODSTAVEC);
            klice.Add("písm.", Sekce.PISMENO);
            klice.Add("písmeno", Sekce.PISMENO);
            klice.Add("písmene", Sekce.PISMENO);
            klice.Add("písmena", Sekce.PISMENO);
            klice.Add("bod", Sekce.BOD);
            klice.Add("bodě", Sekce.BOD);
            klice.Add("bodu", Sekce.BOD);
            klice.Add("body", Sekce.BOD);

            klice.Add("věta", VETA);
            klice.Add("věty", VETA);
            klice.Add("větou", VETA);
            klice.Add("větami", VETA);
            klice.Add("větě", VETA);
            klice.Add("větách", VETA);

            PridejSlovniCislovani(1, "první");
            PridejSlovniCislovani(2, "druh");
            PridejSlovniCislovani(3, "třetí");
            PridejSlovniCislovani(4, "čtvrt");
            PridejSlovniCislovani(5, "pát");
            PridejSlovniCislovani(6, "šest");
            PridejSlovniCislovani(7, "sedm");
            PridejSlovniCislovani(8, "osm");
            PridejSlovniCislovani(9, "devát");
            PridejSlovniCislovani(10, "desát");
            PridejSlovniCislovani(11, "jedenáct");
            PridejSlovniCislovani(12, "dvanáct");
            PridejSlovniCislovani(13, "třináct");
            PridejSlovniCislovani(14, "čtrnáct");
            PridejSlovniCislovani(15, "patnáct");
            PridejSlovniCislovani(POSLEDNI, "poslední");

            PridejOperaci(new Prejmenovani());
            PridejOperaci(new NahrazeniA());
            PridejOperaci(new NahrazeniB());
            PridejOperaci(new NahrazeniC());
            PridejOperaci(new DoplneniA());
            PridejOperaci(new DoplneniB());
            PridejOperaci(new DoplneniC());
            PridejOperaci(new DoplneniD());
            PridejOperaci(new DoplneniD2());
            PridejOperaci(new DoplneniE());
            PridejOperaci(new DoplneniF());
            PridejOperaci(new DoplneniG());
            PridejOperaci(new DoplneniH());
            PridejOperaci(new DoplneniI());
            PridejOperaci(new DoplneniJ());
            PridejOperaci(new DoplneniK());
            PridejOperaci(new DoplneniL());
            PridejOperaci(new ZruseniA());
            PridejOperaci(new ZruseniB());
            PridejOperaci(new ZruseniC());
            PridejOperaci(new ZruseniD());
            PridejOperaci(new ZruseniE());
            PridejOperaci(new ZruseniF());
        }

        private int cislo;
        private int rocnik;
        private IAdapter adapter;
        private Text originalText;
        private Sekce.ISekce original;
        private Sekce.ISekce novela;

        private static Statistika statistika;
        
        private Novelizator(int cislo, int rocnik, IAdapter adapter, Text originalText, Text novelaText)
        {
            
            this.cislo = cislo;
            this.rocnik = rocnik;
            this.adapter = adapter;
            this.originalText = originalText.Kopie();
            this.original = this.originalText.Obsah;
            this.novela = NajdiNovelizacniSekci(novelaText.Obsah.Subsekce);
            Novelizator.aktualniNovelaSekceID = String.Format("SEKCE:{0},CISLO:{1}", this.novela.Typ, this.novela.Cislo);
            if (this.novela == null)
                throw new UZException("Nepodarilo se najit novelizacni sekci");

            ZpracujNovelizaci();
        }

        private static void PridejOperaci(IOperace op)
        {
            //foreach (string maska in op.GetPattern())
            //    operace.Add(Rozdel(maska), op);
            operace.Add(Rozdel(op.GetPattern()), op);
        }

        private static IOperace NajdiOperaci(List<string> data, OperaceUzel op, List<object> parametry)
        {
            if (op == null)
                op = operace; // koren

            if (data.Count < 1)
                return op.Operace;

            foreach (KeyValuePair<string, OperaceUzel> sub in op.Uzly)
            {
                if (operaceRegex.IsMatch(sub.Key))
                    continue; // zpracuje se extra

                if (sub.Key.Equals(data[0])) // match cesty case-sensitive
                {
                    List<string> kopie = new List<string>(data);
                    kopie.RemoveAt(0);
                    IOperace vystup = NajdiOperaci(kopie, sub.Value, parametry);
                    if (vystup != null)
                        return vystup;
                }
            }

            foreach (KeyValuePair<string, OperaceUzel> sub in op.Uzly)
            {
                if (!operaceRegex.IsMatch(sub.Key))
                    continue; // uz je zpracovano

                List<string> kopie = new List<string>(data);
                object parametr = null;
                
                if (sub.Key == CESTA)
                {
                    List<Ukazatel> cesta = NactiCestu(kopie);
                    if (cesta.Count > 0)
                        parametr = cesta;
                }
                else if (sub.Key == UKAZATEL)
                {
                    Ukazatel uk = new Ukazatel(kopie);
                    if (uk.JeValidni && uk.Typ != VETA)
                        parametr = uk;
                }
                else if (sub.Key == UKAZATEL_VETA)
                {
                    Ukazatel uk = new Ukazatel(kopie);
                    if (uk.JeValidni && uk.Typ == VETA)
                        parametr = uk;
                }
                else if (sub.Key == SLOVO)
                {
                    kopie.RemoveAt(0);
                    Match match = slovoRegex.Match(data[0]);
                    if (match.Success)
                        parametr = match.Value;
                }
                else if (sub.Key == SLOVEM)
                {
                    kopie.RemoveAt(0);
                    Match match = slovemRegex.Match(data[0]);
                    if (match.Success)
                        parametr = match.Value;
                }
                else if (sub.Key == CITACE)
                {
                    kopie.RemoveAt(0);
                    if (JeCitace(data[0]))
                        parametr = RozbalCitace(data[0]).Substring(1, data[0].Length - 2);
                }
                else
                {
                    kopie.RemoveAt(0);
                    Regex regex = new Regex("^(" + sub.Key.Substring(1, sub.Key.Length - 2) + ")$");
                    Match match = regex.Match(data[0]);
                    if (match.Success)
                        parametr = match.Value;
                }

                IOperace vystup = NajdiOperaci(kopie, sub.Value, parametry);

                if (vystup != null && parametr != null)
                {
                    parametry.Insert(0, parametr);
                    return vystup;
                }
            }

            return null;
        }

        private static void NajdiNovelizacniSekce(IAdapter adapter, Sekce.ISekce pocatek, Dictionary<string, Sekce.ISekce> zmeny)
        {
            Regex regex = new Regex("(^V|^" + adapter.Typ + ")([^/]*)č. ([0-9]{1,4}/[0-9]{4})");
            foreach (Sekce.ISekce sekce in pocatek.Subsekce)
            {
                if ((sekce.Typ == adapter.HlavniSekce || sekce.Typ == Sekce.CLANEK) && sekce.UvodniUstanoveni != null && regex.IsMatch(sekce.UvodniUstanoveni))
                {
                    Match m = regex.Match(sekce.UvodniUstanoveni);
                    string oznaceni = m.Groups[3].Value; // napr.: "1/1993"
                    if (oznaceni.Equals("1/1993"))
                    {
                        int x = 1;
                    }
                    if (!zmeny.ContainsKey(oznaceni))
                        zmeny.Add(oznaceni, sekce);
                }
                NajdiNovelizacniSekce(adapter, sekce, zmeny);
            }
        }

        private Sekce.ISekce NajdiNovelizacniSekci(List<Sekce.ISekce> subsekce)
        {
            Regex regex = new Regex(String.Format("(^V|^{0})(.*)č. {1}/{2}", adapter.Typ, cislo, rocnik));
            foreach (Sekce.ISekce sekce in subsekce)
            {
                if ((sekce.Typ == adapter.HlavniSekce || sekce.Typ == Sekce.CLANEK) && sekce.UvodniUstanoveni != null && regex.IsMatch(sekce.UvodniUstanoveni))
                    return sekce;
                Sekce.ISekce sub = NajdiNovelizacniSekci(sekce.Subsekce);
                if (sub != null)
                    return sub;
            }
            return null;
        }

        private static void PridejSlovniCislovani(int cislo, string slovo)
        {
            if (slovo.EndsWith("í")) // vzor jarni - nesklonuje se
            {
                slovniCislovani.Add(slovo, cislo);
                return;
            }

            // sklonovani pro zensky tvar
            slovniCislovani.Add(slovo + "á", cislo);
            slovniCislovani.Add(slovo + "é", cislo);
            slovniCislovani.Add(slovo + "ou", cislo);
        }

        private static int PrelozSlovniCislovani(string poradi)
        {
            if (slovniCislovani.ContainsKey(poradi))
                return slovniCislovani[poradi];
            throw new UZException("Nezname slovni poradi: " + poradi);
        }

        private static string SbalCitace(string text)
        {
            StringBuilder vystup = new StringBuilder();
            List<string> casti = Rozdel(text);
            bool citace = false;
            for (int i = 0; i < casti.Count; i++)
            {
                if (i > 0)
                    vystup.Append(citace ? CITACE_SEPARATOR : " ");
                string cast = casti[i];
                if (!citace && cast.StartsWith(Interpret.UVOZOVKY_DOLE))
                    citace = true;
                if (citace && cast.EndsWith(Interpret.UVOZOVKY_NAHORE)) // muze nastat "slovo", proto ne else if
                    citace = false;
                vystup.Append(cast);
            }
            return vystup.ToString();
        }

        private static string RozbalCitace(string text)
        {
            if (!JeCitace(text))
                throw new UZException("Text neni citace: " + text);

            return text.Replace(CITACE_SEPARATOR, " ");
        }

        private static bool JeCitace(string text)
        {
            return text.StartsWith(Interpret.UVOZOVKY_DOLE) && text.EndsWith(Interpret.UVOZOVKY_NAHORE);
        }

        private static List<string> Rozdel(string text, string separator = " ")
        {
            return new List<string>(text.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries));
        }

        private static string Spoj(List<string> data, string separator = " ")
        {
            return String.Join(separator, data);
        }

        private static List<string> GenerujOznaceni(int typ, List<string> cisla)
        {
            List<string> vystup = new List<string>();
            foreach (string cislo in cisla)
            {
                if (typ == Sekce.ODSTAVEC)
                    vystup.Add("(" + cislo + ")");
                else if (typ == Sekce.PISMENO)
                    vystup.Add(cislo + ")");
                else
                    vystup.Add(cislo);
            }
            return vystup;
        }

        private static List<string> GenerujInterval(int typ, string start, string end)
        {
            List<string> vystup = new List<string>();
            if (cisloRegex.IsMatch(start) && cisloRegex.IsMatch(end))
            {
                for (int i = int.Parse(start); i <= int.Parse(end); i++)
                {
                    vystup.Add(i.ToString(EncodingTools.NumberFormat));
                }
                return vystup;
            }

            if (cisloRegex.IsMatch(start) && cisloRegex.IsMatch(end))
            {
                for (int i = (int)start[0]; i <= (int)end[0]; i++)
                {
                    
                    vystup.Add(new String(new char[] { Convert.ToChar(i) }));
                }
                return vystup;
            }
            
            // ZMENA
            //throw new UZException("Nevalidni interval: {0} az {1}", start, end);

            vystup.Add(start);
            vystup.Add(end);
            return vystup;
        }

        private static List<string> NactiInterval(List<string> data, bool smazData = true)
        {
            List<string> vystup = new List<string>();
            if (data.Count > 2 && data[1].Equals("až"))
            {
                vystup.Add(data[0]);
                vystup.Add(data[2]);
                if (smazData)
                    data.RemoveRange(0, 3);
            }
            return vystup;
        }

        private static List<string> NactiSeznam(List<string> data, bool smazData = true)
        {
            List<string> vystup = new List<string>();
            int counter = 0;
            int stav = 0;
            bool konec = false;
            while (!konec && counter < data.Count)
            {
                string d = data[counter];
                switch (stav)
                {
                    case 0:
                        if (d.EndsWith(","))
                            vystup.Add(d.Substring(0, d.Length - 1));
                        else
                        {
                            vystup.Add(d);
                            stav = 1;
                        }
                        break;
                    case 1:
                        if (d.Equals("a"))
                            stav = 2;
                        else
                        {
                            konec = true;
                            counter--;
                        }
                        break;
                    case 2:
                        vystup.Add(d);
                        konec = true;
                        break;
                }
                counter++;
            }
            if (smazData)
                data.RemoveRange(0, counter);
            return vystup;
        }

        private static List<string> NormalizujSeznam(int typ, List<string> data)
        {
            if (typ == Sekce.PISMENO)
            {
                List<string> vystup = new List<string>();
                foreach (string d in data)
                {
                    if (d.EndsWith(")"))
                        vystup.Add(d.Substring(0, d.Length - 1));
                    else
                        vystup.Add(d);
                }
                return vystup;
            }
            else if (typ == VETA)
            {
                List<string> vystup = new List<string>();
                foreach (string d in data)
                {
                    vystup.Add(PrelozSlovniCislovani(d).ToString(EncodingTools.NumberFormat));
                }
                return vystup;
            }
            return data;
        }

        private static List<Ukazatel> NactiCestu(List<string> data, bool smazData = true)
        {
            List<string> kopie = data;
            if (!smazData)
                kopie = new List<string>(data);

            List<Ukazatel> ukazatele = new List<Ukazatel>();
            Ukazatel ukazatel = new Ukazatel(kopie); // umaze data z kopie
            while (ukazatel.JeValidni && ukazatel.Typ != VETA)
            {
                ukazatele.Add(ukazatel);
                ukazatel = new Ukazatel(kopie);
            }
            return ukazatele;
        }

        private static List<string> NactiPoUkazatel(List<string> data)
        {
            List<string> vystup = new List<string>();
            while (data.Count > 0)
            {
                Ukazatel uk = new Ukazatel(data, false);
                if (uk.JeValidni)
                    break;
                vystup.Add(data[0]);
                data.RemoveAt(0);
            }
            return vystup;
        }

        private static string Zacatek(List<string> data)
        {
            if (data.Count < 1)
                return string.Empty;
            return data[0];
        }

        private static string Konec(List<string> data)
        {
            if (data.Count < 1)
                return string.Empty;
            return data[data.Count - 1];
        }

        private static int Pozice(List<string> jehla, List<string> kupka)
        {
            string j = Spoj(jehla);
            string k = Spoj(kupka);
            int index = k.IndexOf(j);
            if (index > 1)
            {
                string prefix = k.Substring(0, index - 1);
                return Rozdel(prefix).Count;
            }
            return index;
        }

        private static int Pozice(string jehla, List<string> kupka)
        {
            return Pozice(Rozdel(jehla), kupka);
        }

        private static bool Shoda(List<string> jehla, List<string> kupka)
        {
            return Pozice(jehla, kupka) == 0;
        }

        private static int OdstranText(string text, List<string> data)
        {
            int pozice = Pozice(text, data);
            if (pozice >= 0)
            {
                data.RemoveRange(pozice, Rozdel(text).Count);
            }
            return pozice;
        }

        private static List<string> PripravData(string text)
        {
            if (text.EndsWith(".") || text.EndsWith(":")) // odebereme posledni znak
                text = text.Substring(0, text.Length - 1);
            text = SbalCitace(text);

            text = text.Replace(", který", " který");
            text = text.Replace(", která", " která");
            text = text.Replace(", které", " které");
            text = text.Replace("a doplňuje", " doplňuje");
            text = text.Replace("a doplňují", " doplňují");
            text = text.Replace("a zároveň", " zároveň");
            text = text.Replace("a v ", "v ");

            List<string> data = Rozdel(text);

            OdstranText("včetně nadpisu", data);
            OdstranText("včetně nadpisů", data);
            OdstranText("úvodní část ustanovení", data);
            OdstranText("úvodní části ustanovení", data);
            
            int p1 = OdstranText("včetně poznámky pod čarou č.", data);
            if (p1 >= 0)
                data.RemoveRange(p1, 1); // odebereme i cislo
            int p2 = OdstranText("včetně poznámek pod čarou č.", data);
            if (p2 >= 0)
            {
                List<string> kopie = new List<string>(data);
                kopie.RemoveRange(0, p2);
                int c1 = kopie.Count;
                List<string> pozn = NactiSeznam(kopie);
                int c2 = kopie.Count;
                data.RemoveRange(p2, (c1 - c2)); // odebereme seznam cisel
            }

            return data;
        }

        private void ZpracujNovelizaci()
        {
            if (this.novela.Subsekce.Count < 1 || this.novela.Subsekce[0].Typ != Sekce.BOD)
                ZpracujSoloBod(this.original, this.novela);
            else
            {

                List<Sekce.ISekce> opraveneBody = new List<Sekce.ISekce>();
                foreach (Sekce.ISekce bod in this.novela.Subsekce)
                {
                    if (bod.Typ != Sekce.BOD && opraveneBody.Count > 0)
                        opraveneBody[opraveneBody.Count - 1].AddSubsekce(bod);
                    else
                        opraveneBody.Add(bod);
                }

                foreach (Sekce.ISekce bod in opraveneBody)
                {
                    if (bod.Typ != Sekce.BOD)
                        throw new UZException("Nevalidni cast v novelizaci: " + bod.Typ.ToString());
                    bool vysledek = ZpracujBod(this.original, bod);
                    if (!vysledek && statistika != null)
                    {
                        statistika.NerozpoznaneBody.Add(bod);
                    }
                }
            }
        }

        private static void ZpracujSoloBod(Sekce.ISekce original, Sekce.ISekce bod)
        {
            string obsah = bod.UvodniUstanoveni;
            List<string> data = PripravData(obsah);
            List<string> zacatekData = NactiPoUkazatel(data);

            List<string> kopieBezZacatku = new List<string>(data);
            List<Ukazatel> cesta = NactiCestu(data, true);
            List<string> cestaData = kopieBezZacatku.GetRange(0, kopieBezZacatku.Count - data.Count);

            List<string> prefixData = new List<string>();
            prefixData.AddRange(zacatekData);
            prefixData.AddRange(cestaData);

            string prefix = Spoj(prefixData);
            string zbytek = Spoj(data);

            int index;
            while ((index = zbytek.IndexOf(",")) >= 0)
            {
                zbytek = zbytek.Remove(0, index + 1);
                string moznyObsah = prefix + zbytek;
                bod.UvodniUstanoveni = moznyObsah;
                if (ZpracujBod(original, bod, false))
                    break; // konec - operace nalezena
            }

        }

        private static bool ZpracujBod(Sekce.ISekce original, Sekce.ISekce bod, bool zaverecneUstanoveni = false)
        {
            string obsah;

            Novelizator.aktualniNovelaBodID = String.Format("SEKCE:{0},CISLO:{1}", bod.Typ, bod.Cislo);
            
            try
            {

                if (!zaverecneUstanoveni)
                {
                    if (bod.UvodniUstanoveni.Length < 1)
                        throw new UZException("Bod neobsahuje zadny popis");
                    obsah = bod.UvodniUstanoveni;
                }
                else
                {
                    if (bod.ZaverecneUstanoveni.Length < 1)
                        throw new UZException("Bod neobsahuje zadny popis");
                    obsah = bod.ZaverecneUstanoveni;
                }

                List<string> data = PripravData(obsah);

                if (data.Count < 3)
                    throw new UZException("Nevalidne zadana novelizace: " + bod.UvodniUstanoveni);

                List<object> parametry = new List<object>();
                IOperace op = NajdiOperaci(data, operace, parametry);

                if (op != null)
                {
                    if (statistika != null)
                        statistika.Operace.Add(op);
                    else
                        op.Invoke(original, bod, parametry);
                    return true;
                }

            }
            catch (UZException e) { }
            catch (NullReferenceException e) { }
            

            return false;
        }

        private static void AplikujNovelizaci(Sekce.ISekce original, Sekce.ISekce zmeny)
        {
            if (zmeny.Typ == Sekce.ZASOBNIK)
                AplikujNovelizaci(original, zmeny.Subsekce);
            else
            {
                List<Sekce.ISekce> seznam = new List<Sekce.ISekce>();
                seznam.Add(zmeny);
                AplikujNovelizaci(original, seznam);
            }
        }

        private static void AplikujNovelizaci(Sekce.ISekce original, List<Sekce.ISekce> zmeny)
        {
            foreach (Sekce.ISekce zmena in zmeny)
            {
                Sekce.ISekce mirror = NajdiSekci(original, new Ukazatel(zmena.Typ, zmena.Cislo));
                if (mirror == null)
                    continue;
                mirror.Nadpis = zmena.Nadpis;
                mirror.UvodniUstanoveni = zmena.UvodniUstanoveni;
                mirror.ZaverecneUstanoveni = zmena.ZaverecneUstanoveni;
                mirror.Subsekce.Clear();
                foreach (Sekce.ISekce sub in zmena.Subsekce)
                    mirror.AddSubsekce(sub);
                mirror.Poznamky.Clear();
                foreach (Sekce.ISekce pozn in zmena.Poznamky)
                    mirror.AddPoznamka(pozn);
            }
        }

        private static Sekce.ISekce NajdiSekci(Sekce.ISekce pocatek, Ukazatel ukazatel)
        {
            if (pocatek.Typ == ukazatel.Typ && pocatek.Cislo.Equals(ukazatel.PrvniCislo))
                return pocatek;

            foreach (Sekce.ISekce sub in pocatek.Subsekce)
            {
                Sekce.ISekce vystup = NajdiSekci(sub, ukazatel);
                if (vystup != null)
                    return vystup;
            }
            return null;
        }

        private static Sekce.ISekce NajdiSekci(Sekce.ISekce pocatek, List<Ukazatel> cesta)
        {
            Sekce.ISekce vystup = pocatek;
            foreach (Ukazatel uk in cesta)
            {
                vystup = NajdiSekci(vystup, uk);
                if (vystup == null)
                    throw new UZException("Cesta nebyla nalezena");
            }
            return vystup;
        }

        private static List<Sekce.ISekce> NajdiSekce(Sekce.ISekce pocatek, Ukazatel ukazatel)
        {
            List<Ukazatel> cesta = new List<Ukazatel>();
            cesta.Add(ukazatel);
            return NajdiSekce(pocatek, cesta);
        }

        private static List<Sekce.ISekce> NajdiSekce(Sekce.ISekce pocatek, List<Ukazatel> cesta)
        {
            Ukazatel ukazatel = cesta[cesta.Count - 1];
            if (cesta.Count > 0)
            {
                List<Ukazatel> kopie = new List<Ukazatel>(cesta);
                kopie.RemoveAt(kopie.Count - 1);
                pocatek = NajdiSekci(pocatek, kopie);
            }

            List<Sekce.ISekce> vystup = new List<Sekce.ISekce>();
            foreach (string cislo in ukazatel.Cisla)
            {
                Sekce.ISekce v = NajdiSekci(pocatek, new Ukazatel(ukazatel.Typ, cislo));
                if (v == null)
                    throw new UZException("Sekce nebyla nalezena: {0}, {1}", ukazatel.Typ, cislo);
                vystup.Add(v);
            }

            return vystup;
        }

        private static string SpojSlova(string slovo1, string slovo2)
        {
            slovo1 = slovo1.Trim();
            slovo2 = slovo2.Trim();
            if (slovo2.StartsWith(",") || slovo2.StartsWith(";") || slovo2.StartsWith(" "))
                return slovo1 + slovo2;
            return slovo1 + " " + slovo2;
        }

        private static List<string> RozdelNaVety(string text)
        {
            List<char> velkaPismena = new List<char>(VELKA_PISMENA.ToCharArray());
            // hledame posloupnost tecky, mezery a velkeho pismene
            List<string> vystup = new List<string>();
            StringBuilder veta = new StringBuilder();
            int stav = 0;
            foreach (char c in text.ToCharArray())
            {
                switch (stav)
                {
                    case 0:
                        veta.Append(c);
                        if (c == '.')
                            stav = 1;
                        break;
                    case 1:
                        veta.Append(c);
                        if (c == ' ')
                            stav = 2;
                        else
                            stav = 0;
                        break;
                    case 2:
                        if (velkaPismena.Contains(c))
                        {
                            vystup.Add(veta.ToString().Trim());
                            veta.Clear();
                        }
                        veta.Append(c);
                        stav = 0;
                        break;
                }
            }
            if (veta.Length > 0)
                vystup.Add(veta.ToString());
            return vystup;
        }

        private static List<int> NajdiPoziceVet(Ukazatel ukazatel, List<string> vety)
        {
            List<int> vystup = new List<int>();
            foreach (string cislo in ukazatel.Cisla)
            {
                int konvertovane = int.Parse(cislo, EncodingTools.NumberFormat);
                if (konvertovane == POSLEDNI)
                    vystup.Add(vety.Count - 1);
                else
                    vystup.Add(konvertovane - 1); // napr. "prvni" ma index 0
            }
            return vystup;
        }

        private static List<string> NajdiVety(Ukazatel ukazatel, string text)
        {
            List<string> vystup = new List<string>();
            List<string> vety = RozdelNaVety(text);
            List<int> pozice = NajdiPoziceVet(ukazatel, vety);

            foreach (int index in pozice)
            {
                if (index < vety.Count)
                    vystup.Add(vety[index]);
                else
                    throw new UZException("Neocekavany puvodni text");
            }

            return vystup;
        }

        private static string NormalizujMezery(string text)
        {
            return String.Join(" ", Rozdel(text));
        }

        private static void NahradText(Sekce.ISekce sekce, string puvodni, string novy)
        {
            sekce.UvodniUstanoveni = NormalizujMezery(sekce.UvodniUstanoveni.Replace(puvodni, novy));
            sekce.ZaverecneUstanoveni = NormalizujMezery(sekce.ZaverecneUstanoveni.Replace(puvodni, novy));
        }

        private static void NahradTextVeVete(Sekce.ISekce sekce, Ukazatel vetaUkazatel, string puvodni, string novy, string predZaStrednikem = null)
        {
            List<string> vety = RozdelNaVety(sekce.UvodniUstanoveni);
            List<int> pozice = NajdiPoziceVet(vetaUkazatel, vety);
            for (int i = 0; i < vety.Count; i++)
            {
                if (!pozice.Contains(i))
                    continue;
                string veta = vety[i];
                List<string> casti = Rozdel(veta, "; ");
                int cast = (predZaStrednikem == null) ? 0 : (predZaStrednikem.Equals("za") ? 1 : 0);
                casti[cast] = NormalizujMezery(casti[cast].Replace(puvodni, novy));
                vety[i] = String.Join("; ", casti);
            }
            sekce.UvodniUstanoveni = String.Join(" ", vety);
        }

        private static void NahradTextNaKonci(Sekce.ISekce sekce, string puvodni, string novy)
        {
            string text = sekce.ZaverecneUstanoveni.Length > 0 ? sekce.ZaverecneUstanoveni : sekce.UvodniUstanoveni;

            if (text.EndsWith(puvodni))
                text = text.Substring(0, text.Length - puvodni.Length);

            text = text + novy;

            if (sekce.ZaverecneUstanoveni.Length > 0)
                sekce.ZaverecneUstanoveni = text;
            else
                sekce.UvodniUstanoveni = text;
        }

        private static void VlozTextZaText(Sekce.ISekce sekce, string kotva, string text)
        {
            NahradText(sekce, kotva, kotva + " " + text);
        }

        private static void VlozTextNaZacatek(Sekce.ISekce sekce, string text)
        {
            sekce.UvodniUstanoveni = NormalizujMezery(text + " " + sekce.UvodniUstanoveni);
        }

        private static void VlozTextNaKonec(Sekce.ISekce sekce, string text)
        {
            if (sekce.ZaverecneUstanoveni.Length > 0)
                sekce.ZaverecneUstanoveni = NormalizujMezery(sekce.ZaverecneUstanoveni + " " + text);
            else
                sekce.UvodniUstanoveni = NormalizujMezery(sekce.UvodniUstanoveni + " " + text);
        }

        private static Sekce.ISekce NajdiKonec(Sekce.ISekce sekce)
        {
            while (sekce.Typ < Sekce.BOD && sekce.Subsekce.Count > 0)
                sekce = sekce.Subsekce[sekce.Subsekce.Count - 1];
            return sekce;
        }

        private static void NastavPosledniZmenu(Sekce.ISekce sekce, string typ)
        {
            sekce.SetPosledniZmena(String.Format("{0}|{1}|{2}|{3}", Novelizator.aktualniNovelaID, Novelizator.aktualniNovelaSekceID, Novelizator.aktualniNovelaBodID, typ));

        }

        private static List<object> PostavParametry(params object[] parametry)
        {
            return new List<object>(parametry);
        }

        public interface IOperace
        {
            string GetId();
            string GetPattern();
            void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry);
        }

        class Prejmenovani : IOperace
        {
            public string GetId()
            {
                return "Prejmenovani";
            }

            public string GetPattern()
            {
                return "Dosavadní <cesta> se <označuj(e|í)> jako <ukazatel>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                Ukazatel typy = (Ukazatel)parametry[2]; // automaticky z citace

                List<Sekce.ISekce> kPrejmenovani = NajdiSekce(original, cesta);

                for (int i = 0; i < kPrejmenovani.Count; i++)
                {
                    Sekce.ISekce sekce = kPrejmenovani[i];
                    sekce.Cislo = typy.Cisla[i];
                    sekce.Oznaceni = typy.Oznaceni[i];
                }
            }
        }

        class NahrazeniA : IOperace
        {
            public string GetId()
            {
                return "NahrazeniA";
            }

            public string GetPattern()
            {
                return "?V <cesta> <zn(í|ějí)>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                Sekce.ISekce sekce = NajdiSekci(original, cesta);
                if (bod.Subsekce.Count < 1)
                {
                    throw new UZException("Chybi zasobnik s textem");
                }
                AplikujNovelizaci(sekce.Rodic, bod.Subsekce[0]);
                NastavPosledniZmenu(sekce, ZMENA);
            }
        }

        class NahrazeniB : IOperace
        {
            public string GetId()
            {
                return "NahrazeniB";
            }

            public string GetPattern()
            {
                return "V <cesta> se <ukazatel:veta> <nahrazuj(e|í)> <vět(ou|ami)> <citace>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                Ukazatel ukazatelVeta = (Ukazatel)parametry[1];
                string novyText = (string)parametry[4];
                List<Sekce.ISekce> sekce = NajdiSekce(original, cesta);
                
                foreach (Sekce.ISekce s in sekce)
                {
                    List<string> vety = NajdiVety(ukazatelVeta, s.UvodniUstanoveni);
                    NahradText(s, String.Join(" ", vety), novyText);
                    NastavPosledniZmenu(s, ZMENA);
                }
            }
        }

        class NahrazeniC : IOperace
        {
            public string GetId()
            {
                return "NahrazeniC";
            }

            public string GetPattern()
            {
                return "V <cesta> ?v ?<cesta> se <slovo> <citace> <nahrazuj(e|í)> <slovem> <citace>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                List<Ukazatel> cesta2 = null;

                if (parametry.Count > 6)
                {
                    try
                    {
                        cesta2 = (List<Ukazatel>)parametry[1];
                    }
                    catch (InvalidCastException e)
                    { throw new UZException("Velkej problem s ukazatelem"); }
                    parametry.RemoveAt(1);
                }

                string puvodni = (string)parametry[2];
                string nove = (string)parametry[5];
                List<Sekce.ISekce> sekce = NajdiSekce(original, cesta);
                foreach (Sekce.ISekce sub in sekce)
                {
                    NahradText(sub, puvodni, nove);
                    NastavPosledniZmenu(sub, ZMENA);
                }

                if (cesta2 != null)
                {
                    parametry[0] = cesta2;
                    new NahrazeniC().Invoke(original, bod, parametry);
                }
            }
        }

        class DoplneniA : IOperace
        {
            public string GetId()
            {
                return "DoplneniA";
            }

            public string GetPattern()
            {
                return "Za <cesta> se <vklád(á|ají)> <nov(ý|á|é)> <ukazatel> <kter(ý|é|á)> <zn(í|ějí)>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                Ukazatel typy = (Ukazatel)parametry[3]; // automaticky z citace
                Sekce.ISekce pointer = NajdiSekci(original, cesta);
                Sekce.ISekce rodic = pointer.Rodic;

                // Zpracovani prejmenovani sekci
                if (bod.ZaverecneUstanoveni.Length > 0)
                    ZpracujBod(rodic, bod, true);

                int index = rodic.Subsekce.IndexOf(pointer);

                if (bod.Subsekce.Count < 1)
                    throw new UZException("Bod neobsahuje novelizacni ustanoveni");

                foreach (Sekce.ISekce sub in bod.Subsekce[0].Subsekce)
                {
                    index++;
                    Sekce.ISekce kopie = sub.GetKopie();
                    NastavPosledniZmenu(kopie, DOPLNENI);
                    rodic.AddSubsekce(kopie, index);
                }
            }
        }

        class DoplneniB : IOperace
        {
            public string GetId()
            {
                return "DoplneniB";
            }

            public string GetPattern()
            {
                return "V <cesta> se za <ukazatel> <vklád(á|ají)> <nov(ý|á|é)> <ukazatel> <kter(ý|á|é)> <zn(í|ějí)>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                Ukazatel zarazka = (Ukazatel)parametry[1]; 
                Ukazatel vkladane = (Ukazatel)parametry[4];

                cesta.Add(zarazka);

                new DoplneniA().Invoke(original, bod, PostavParametry(cesta, parametry[2], parametry[3], vkladane));
            }
        }

        class DoplneniC : IOperace
        {
            public string GetId()
            {
                return "DoplneniC";
            }

            public string GetPattern()
            {
                return "V <cesta> se <doplňuj(e|í)> ?nový <ukazatel> <kter(ý|é|á)> <zn(í|ějí)>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                Ukazatel typy = (Ukazatel)parametry[2]; // automaticky z citace
                Sekce.ISekce rodic = NajdiSekci(original, cesta);
                if (bod.Subsekce.Count < 1)
                    throw new UZException("Bod neobsahuje novelizacni ustanoveni");
                foreach (Sekce.ISekce sub in bod.Subsekce[0].Subsekce)
                {
                    Sekce.ISekce kopie = sub.GetKopie();
                    NastavPosledniZmenu(kopie, DOPLNENI);
                    rodic.AddSubsekce(kopie);
                }
            }
        }

        class DoplneniD : IOperace
        {
            public string GetId()
            {
                return "DoplneniD";
            }

            public string GetPattern()
            {
                return "V <cesta> se na konci <ukazatel> tečka nahrazuje <čárkou|středníkem|slovem> ?<citace>"
                    + " <doplňuj(e|í)> se <ukazatel> <kter(ý|é|á)> <zn(í|ějí)>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                string citace = "";
                if (parametry.Count > 7) // obsahuje citaci
                {
                    citace = (string)parametry[3];
                    parametry.RemoveAt(3);
                }
                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                Ukazatel posledni = (Ukazatel)parametry[1];
                string typNahrady = (string)parametry[2];
                Ukazatel novy = (Ukazatel)parametry[4];


                Sekce.ISekce rodic = NajdiSekci(original, cesta);
                Sekce.ISekce posledniPotomek = NajdiSekci(rodic, posledni);
                posledniPotomek = NajdiKonec(posledniPotomek);
                string nahrada = citace;
                if (typNahrady.Equals("čárkou"))
                    nahrada = ",";
                else if (typNahrady.Equals("středníkem"))
                    nahrada = ";";
                NahradTextNaKonci(posledniPotomek, ".", nahrada);

                if (posledni.Typ != novy.Typ)
                    cesta.Add(posledni);

                new DoplneniC().Invoke(original, bod, PostavParametry(cesta, parametry[3], parametry[4], parametry[5], parametry[6]));
            }
        }

        class DoplneniD2 : IOperace
        {
            public string GetId()
            {
                return "DoplneniD2";
            }

            public string GetPattern()
            {
                return "V <cesta> se tečka na konci nahrazuje <čárkou|středníkem|slovem> ?<citace>"
                    + " <doplňuj(e|í)> se <ukazatel> <kter(ý|é|á)> <zn(í|ějí)>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                List<object> parametry2 = new List<object>(parametry); // kopie

                string citace = "";
                if (parametry.Count > 6) // obsahuje citaci
                {
                    citace = (string)parametry[2];
                    parametry.RemoveAt(2);
                }
                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];

                Sekce.ISekce konec = NajdiSekci(original, cesta);
                while (konec.Typ < Sekce.BOD && konec.Subsekce.Count > 0)
                    konec = konec.Subsekce[konec.Subsekce.Count - 1];

                Ukazatel konecUkazatel = new Ukazatel(konec.Typ, konec.Cislo);

                parametry2.Insert(1, konecUkazatel);
                new DoplneniD().Invoke(original, bod, parametry2);
            }
        }

        class DoplneniE : IOperace
        {
            public string GetId()
            {
                return "DoplneniE";
            }

            public string GetPattern()
            {
                return "V <cesta> se na konci <ukazatel> tečka zrušuje"
                    + " <doplňuj(e|í)> se <ukazatel> <kter(ý|é|á)> <zn(í|ějí)>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                new DoplneniD().Invoke(original, bod,
                    PostavParametry(parametry[0], parametry[1], "slovem", "", parametry[2], parametry[3], parametry[4], parametry[5]));
            }
        }

        class DoplneniF : IOperace
        {
            public string GetId()
            {
                return "DoplneniF";
            }

            public string GetPattern()
            {
                return "V <cesta> se dosavadní text označuje jako <ukazatel> <doplňuj(e|í)> se <ukazatel> <kter(ý|á|é)> <zn(í|ějí)>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                Ukazatel premena = (Ukazatel)parametry[1];
                Ukazatel doplneni = (Ukazatel)parametry[3];

                Sekce.ISekce sekce = NajdiSekci(original, cesta);

                string text = sekce.UvodniUstanoveni;
                sekce.UvodniUstanoveni = string.Empty;
                Sekce.ISekce novaSekce = Sekce.Seznam[premena.Typ].Factory();
                novaSekce.Cislo = premena.PrvniCislo;
                novaSekce.Oznaceni = premena.PrvniOznaceni;
                novaSekce.UvodniUstanoveni = text;
                NastavPosledniZmenu(novaSekce, DOPLNENI);
                sekce.AddSubsekce(novaSekce);

                new DoplneniC().Invoke(original, bod, PostavParametry(cesta, parametry[2], doplneni));
            }
        }

        class DoplneniG : IOperace
        {
            public string GetId()
            {
                return "DoplneniG";
            }

            public string GetPattern()
            {
                return "V <cesta> se na začátek ?<ukazatel> <vklád(á|ají)> <slovo> <citace>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                Ukazatel ukazatel = null;
                string citace = "";
                if (parametry.Count > 4)
                {
                    ukazatel = (Ukazatel)parametry[1];
                    citace = (string)parametry[4];
                }
                else
                    citace = (string)parametry[3];

                Sekce.ISekce sekce = NajdiSekci(original, cesta);
                if (ukazatel != null)
                    sekce = NajdiSekci(sekce, ukazatel);

                VlozTextNaZacatek(sekce, citace);
                NastavPosledniZmenu(sekce, DOPLNENI);
            }
        }

        class DoplneniH : IOperace
        {
            public string GetId()
            {
                return "DoplneniH";
            }

            public string GetPattern()
            {
                return "V <cesta> se za <slovo> <citace> <vklád(á|ají)> <slovo> <citace>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                string kotva = (string)parametry[2];
                string doplneni = (string)parametry[5];
                    
                Sekce.ISekce sekce = NajdiSekci(original, cesta);
                VlozTextZaText(sekce, kotva, doplneni);
                NastavPosledniZmenu(sekce, DOPLNENI);
            }
        }

        class DoplneniI : IOperace
        {
            public string GetId()
            {
                return "DoplneniI";
            }

            public string GetPattern()
            {
                return "V <cesta> se za <ukazatel:veta> <vklád(á|ají)> <vět(a|y)> <citace>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                Ukazatel vetaUkazatel = (Ukazatel)parametry[1];
                string doplneni = (string)parametry[4];

                Sekce.ISekce sekce = NajdiSekci(original, cesta);
                List<string> kotvaVeta = NajdiVety(vetaUkazatel, sekce.UvodniUstanoveni);
                VlozTextZaText(sekce, String.Join(" ", kotvaVeta), doplneni);
                NastavPosledniZmenu(sekce, ZMENA);
            }
        }

        class DoplneniJ : IOperace
        {
            public string GetId()
            {
                return "DoplneniJ";
            }

            public string GetPattern()
            {
                return "Na konci ?<textu> <cesta> se <doplňuj(e|í)> <slovo> <citace>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                bool najdiKonec = parametry.Count > 4;
                if (najdiKonec)
                    parametry.RemoveAt(0);

                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                string doplneni = (string)parametry[3];

                Sekce.ISekce sekce = NajdiSekci(original, cesta);

                if (najdiKonec) // najdeme nejspodnejsi cast dane sekce
                    sekce = NajdiKonec(sekce);

                VlozTextNaKonec(sekce, doplneni);
                NastavPosledniZmenu(sekce, ZMENA);
            }
        }

        class DoplneniK : IOperace
        {
            public string GetId()
            {
                return "DoplneniK";
            }

            public string GetPattern()
            {
                return "V <cesta> se na konci ?<textu> <ukazatel> <doplňuj(e|í)> <slovo> <citace>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                bool konecTextu = parametry.Count > 5;
                if (konecTextu)
                    parametry.RemoveAt(1);
                Ukazatel ukazatel = (Ukazatel)parametry[1];

                cesta.Add(ukazatel);
                List<object> parametry2 = PostavParametry(cesta, parametry[2], parametry[3], parametry[4]);
                if (konecTextu)
                    parametry2.Insert(0, "textu");

                new DoplneniJ().Invoke(original, bod, parametry2);
            }
        }

        class DoplneniL : IOperace
        {
            public string GetId()
            {
                return "DoplneniL";
            }

            public string GetPattern()
            {
                return "V <cesta> ?v ?části <ukazatel:veta> ?<před|za> ?středníkem se za <slovo> <citace> <vkládá|vkládají|doplňuje|doplňují> <slovo> <citace>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                Ukazatel vetaUkazatel = (Ukazatel)parametry[1];
                string predZaStrednikem = null;
                string puvodni;
                string novy;
                if (parametry.Count > 8) // cast vety
                {
                    predZaStrednikem = (string)parametry[2];
                    puvodni = (string)parametry[4];
                    novy = (string)parametry[7];
                }
                else
                {
                    puvodni = (string)parametry[3];
                    novy = (string)parametry[6];
                }

                
                Sekce.ISekce sekce = NajdiSekci(original, cesta);

                NahradTextVeVete(sekce, vetaUkazatel, puvodni, SpojSlova(puvodni, novy), predZaStrednikem);
                NastavPosledniZmenu(sekce, ZMENA);
            }
        }

        class ZruseniA : IOperace
        {
            public string GetId()
            {
                return "ZruseniA";
            }

            public string GetPattern()
            {
                return "<cesta> se <vypouští|zrušuje|zrušují>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                List<Sekce.ISekce> sekce = NajdiSekce(original, cesta);
                foreach (Sekce.ISekce s in sekce)
                {
                    s.Nadpis = "";
                    s.ZaverecneUstanoveni = "";
                    s.UvodniUstanoveni = ZRUSUJICI_TEXT;
                    s.Subsekce.Clear();
                    NastavPosledniZmenu(s, ZRUSENI);
                }
            }
        }

        class ZruseniB : IOperace
        {
            public string GetId()
            {
                return "ZruseniB";
            }

            public string GetPattern()
            {
                return "V <cesta> se <ukazatel> <zrušuj(e|í)>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                Ukazatel ukazatel = (Ukazatel)parametry[1];

                if (ukazatel.Typ <= Sekce.CAST && ukazatel.Typ >= Sekce.PODODDIL)
                {
                    cesta.Add(ukazatel);
                    new ZruseniA().Invoke(original, bod, PostavParametry(cesta));
                    return; // zruseni pomoci textu (zrusen)
                }

                // uplne vyjmuti sekce
                Sekce.ISekce rodic = NajdiSekci(original, cesta);

                List<Sekce.ISekce> keZruseni = NajdiSekce(original, ukazatel);
                foreach (Sekce.ISekce sekce in keZruseni)
                    rodic.Subsekce.Remove(sekce);

                NastavPosledniZmenu(rodic, ZMENA);

                // Zpracovani zbylych prejmenovani sekci
                if (bod.ZaverecneUstanoveni.Length > 0)
                    ZpracujBod(rodic, bod, true);
            }
        }

        class ZruseniC : IOperace
        {
            public string GetId()
            {
                return "ZruseniC";
            }

            public string GetPattern()
            {
                return "V <cesta> se <ukazatel> se <zrušuj(e|í)> a zároveň se zrušuje označení <ukazatel>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                new ZruseniB().Invoke(original, bod, parametry);

                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                Ukazatel ukazatel = (Ukazatel)parametry[3];

                Sekce.ISekce rodic = NajdiSekci(original, cesta);
                Sekce.ISekce potomek = NajdiSekci(rodic, ukazatel);

                rodic.UvodniUstanoveni = potomek.UvodniUstanoveni;
                rodic.Subsekce.Remove(potomek);
                NastavPosledniZmenu(rodic, ZMENA);
            }
        }

        class ZruseniD : IOperace
        {
            public string GetId()
            {
                return "ZruseniD";
            }

            public string GetPattern()
            {
                return "V <cesta> se na konci <ukazatel> <čárka|středník> nahrazuje tečkou a <ukazatel> se <zrušuj(e|í)>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                Ukazatel keZruseni = (Ukazatel)parametry[3];

                new ZruseniB().Invoke(original, bod, PostavParametry(cesta, keZruseni, parametry[4]));

                Ukazatel ukazatel = (Ukazatel)parametry[1];
                string nahrada = (string)parametry[2];

                Sekce.ISekce rodic = NajdiSekci(original, cesta);
                Sekce.ISekce potomek = NajdiSekci(rodic, ukazatel);

                NahradText(potomek, nahrada.Equals("čárka") ? "," : ";", ".");

                rodic.UvodniUstanoveni = potomek.UvodniUstanoveni;
                rodic.Subsekce.Remove(potomek);
                NastavPosledniZmenu(rodic, ZMENA);
            }
        }

        class ZruseniE : IOperace
        {
            public string GetId()
            {
                return "ZruseniE";
            }

            public string GetPattern()
            {
                return "V <cesta> se <ukazatel:veta> <zrušuj(e|í)>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                Ukazatel ukazatelVeta = (Ukazatel)parametry[1];
                //string novyText = (string)parametry[4];
                string novyText = "";
                List<Sekce.ISekce> sekce = NajdiSekce(original, cesta);

                foreach (Sekce.ISekce s in sekce)
                {
                    List<string> vetyKeZruseni = NajdiVety(ukazatelVeta, s.UvodniUstanoveni);
                    NahradText(s, String.Join(" ", vetyKeZruseni), novyText);
                    NastavPosledniZmenu(s, ZMENA);
                }
            }
        }

        class ZruseniF : IOperace
        {
            public string GetId()
            {
                return "ZruseniF";
            }

            public string GetPattern()
            {
                return "V <cesta> se <slov(o|a)> <citace> <zrušuj(e|í)>";
            }
            public void Invoke(Sekce.ISekce original, Sekce.ISekce bod, List<object> parametry)
            {
                List<Ukazatel> cesta = (List<Ukazatel>)parametry[0];
                string keZruseni = (string)parametry[2];
                List<Sekce.ISekce> sekce = NajdiSekce(original, cesta);

                foreach (Sekce.ISekce s in sekce)
                {
                    NahradText(s, keZruseni, "");
                    NastavPosledniZmenu(s, ZMENA);
                }
            }
        }
        
    }
}
