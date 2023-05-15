using System;
using System.Collections.Generic;
using System.Text;
using UZ.PDF;
using UZ.Sbirka.Adaptery;
using System.IO;
using System.Xml;
using System.Threading;

namespace UZ.Sbirka
{
    class Predpis
    {
        private static List<IAdapter> adaptery = new List<IAdapter>();
        private IAdapter adapter;
        private string nazev;
        private int cislo;
        private int rocnik;
        private Castka castka;
        private StructuredDocument obsah;
        private Text text;

        private List<string> zmeny;
        private SortedSet<Novela> novely;

        public string Adresar
        {
            get
            {
                return Index.AdresarPredpisu + '/' + Rocnik.ToString(EncodingTools.NumberFormat)
                    + '/' + cislo.ToString(EncodingTools.NumberFormat).PadLeft(4, '0');
            }
        }

        public string Soubor
        {
            get
            {
                return "pr" + Index.GetId(cislo, Rocnik);
            }
        }

        public string SouborInfo
        {
            get { return Adresar + '/' + Soubor + "_info.xml"; }
        }

        public string SouborZmeny
        {
            get { return Adresar + '/' + Soubor + "_zmeny.xml"; }
        }

        public string SouborNovely
        {
            get { return Adresar + '/' + Soubor + "_novely.xml"; }
        }

        public string SouborText
        {
            get { return Adresar + '/' + Soubor + "_original.xml"; }
        }

        public IAdapter Adapter { get { return adapter; } }

        public Castka Castka { get { return castka; } }

        public string Typ { get { return adapter.Typ; } }

        public int Cislo
        {
            get { return cislo; }
        }

        public int Rocnik
        {
            get { return rocnik; }
        }

        public string Nazev { get { return nazev; } }

        public string Oznaceni
        {
            get
            {
                return String.Format("{0} č. {1}/{2}", Typ, Cislo, Rocnik);
            }
        }

        public bool JeNeznamy
        {
            get
            {
                return this.adapter.Typ.Equals(Index.NEZNAMY_PREDPIS);
            }
        }

        public StructuredDocument Obsah
        {
            get
            {
                if (obsah == null)
                    obsah = Extraktor.ExtrahujPredpis(this, this.castka);
                return obsah;
            }
        }

        public Text Text
        {
            get
            {
                if (text == null)
                {
                    if (File.Exists(SouborText))
                        text = Text.FromXmlFile(SouborText);
                    else
                    {
                                 
                        text = Interpret.InterpretujPredpis(this);
                        text = adapter.PostInterpret(text);
                        text.ToXmlFile(SouborText);
                    
                    }
                }
                return text;
            }
        }

        public List<string> Zmeny
        {
            get
            {
                if (zmeny == null)
                {
                    if (File.Exists(SouborZmeny))
                        NactiZmeny();
                    else
                    {
                        zmeny = Novelizator.ZmenyPredpisu(adapter, Text);
                        UlozZmeny();
                    }
                }
                return zmeny;
            }
        }

        public SortedSet<Novela> Novely
        {
            get
            {
                if (novely == null)
                {
                    if (File.Exists(SouborNovely))
                        NactiNovely();
                    else
                    {
                        novely = new SortedSet<Novela>();
                        UlozNovely();
                    }
                }
                return novely;
            }
        }

        public Text UplneZneni
        {
            get
            {
                if (Novely.Count < 1)
                    return Text;
                return Novely.Max.UplneZneni;
            }
        }

        static Predpis()
        {
            adaptery.Add(new UstavniZakon());
            adaptery.Add(new Zakon());
            adaptery.Add(new NeznamyPredpis());
        }

        public static Predpis ZObsahuCastky(string odstavec, Castka castka)
        {
            if (adaptery.Count < 1)
                throw new UZException("Nebyly specifikovany zadne pravni predpisy");

            
            string cisloPredpisuRaw = odstavec.Remove(odstavec.IndexOf('.')).Trim();
            int cisloPredpisu = int.Parse(cisloPredpisuRaw, EncodingTools.NumberFormat);
            string nazevPredpisu = odstavec.Substring(odstavec.IndexOf('.') + 1).Trim();

            foreach (IAdapter adapter in adaptery)
            {
                if (adapter.NazevRegex.IsMatch(nazevPredpisu))
                    return new Predpis(adapter, nazevPredpisu, cisloPredpisu, castka);
            }

            return new Predpis(new NeznamyPredpis(), nazevPredpisu, cisloPredpisu, castka);
        }

        public static Predpis ZObsahuCastky(string typ, int cislo, string nazev, Castka castka)
        {
            foreach (IAdapter adapter in adaptery)
            {
                if (typ.Equals(adapter.Typ))
                    return new Predpis(adapter, nazev, cislo, castka);
            }

            throw new UZException("Predpis nebyl identifikovan: " + nazev);
        }

        public Predpis(int cislo, int rocnik)
        {
            this.cislo = cislo;
            this.rocnik = rocnik;
            if (File.Exists(SouborInfo))
                NactiInfo();
            
            if (this.Typ == Index.NEZNAMY_PREDPIS)
                throw new UZException("Predpis nebyl identifikovan: {0}/{1}", cislo, rocnik);
        }

        public Predpis(string oznaceni) : this(Int32.Parse(oznaceni.Split('/')[0]), Int32.Parse(oznaceni.Split('/')[1])) 
        {
        }

        protected Predpis(IAdapter adapter, string nazev, int cislo, Castka castka)
        {
            this.adapter = adapter;
            this.nazev = nazev;
            this.cislo = cislo;
            this.rocnik = castka.Rocnik;
            this.castka = castka;
        }

        public void SetNazev(string nazev)
        {
            this.nazev = nazev;
        }

        public void Uloz()
        {
            UlozInfo();
        }

        private void UlozInfo()
        {

            if (!Directory.Exists(Adresar))
                Directory.CreateDirectory(Adresar);

            XmlTextWriter writer = Xml.GetXmlTextWriter(SouborInfo);
            
            writer.WriteStartDocument();
            writer.WriteStartElement(Xml.PREDPIS);

            writer.WriteElementString(Xml.TYP, adapter.Typ);
            writer.WriteElementString(Xml.CISLO, cislo.ToString(EncodingTools.NumberFormat));
            writer.WriteElementString(Xml.CASTKA, castka.Cislo.ToString(EncodingTools.NumberFormat));
            writer.WriteElementString(Xml.ROCNIK, castka.Rocnik.ToString(EncodingTools.NumberFormat));
            writer.WriteElementString(Xml.NAZEV, nazev);
            
            writer.WriteEndElement();
            writer.WriteEndDocument();

            Xml.CloseXmlTextWriter(writer);
        }

        private void NactiInfo()
        {
            XmlTextReader reader = Xml.GetXmlTextReader(SouborInfo);

            int castkaCislo = 0;
            int castkaRocnik = 0;
            int prCislo = 0;
            string prNazev = string.Empty;
            
            
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == Xml.PREDPIS)
                            continue; // zacatek sekce
                        if (reader.Name == Xml.TYP)
                        {
                            string ptyp = reader.ReadElementContentAsString();
                            bool found = false;
                            foreach (IAdapter ad in Predpis.adaptery)
                            {
                                if (ad.Typ.Equals(ptyp))
                                {
                                    adapter = ad;
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                                throw new UZException("Neznamy predpis: " + ptyp);
                        }
                        else if (reader.Name == Xml.CISLO)
                            prCislo = reader.ReadElementContentAsInt();
                        else if (reader.Name == Xml.CASTKA)
                            castkaCislo = reader.ReadElementContentAsInt();
                        else if (reader.Name == Xml.ROCNIK)
                            castkaRocnik = reader.ReadElementContentAsInt();
                        else if (reader.Name == Xml.NAZEV)
                            prNazev = reader.ReadElementContentAsString();
                        break;
                    default:
                        // do nothing
                        break;
                }
            }

            this.cislo = prCislo;
            this.nazev = prNazev;
            this.rocnik = castkaRocnik;
            this.castka = Index.NajdiCastku(castkaCislo, castkaRocnik);
            
            Xml.CloseXmlTextReader(reader);
        }

        private void UlozZmeny()
        {
            if (this.zmeny == null)
                return;
            if (!Directory.Exists(Adresar))
                Directory.CreateDirectory(Adresar);

            XmlTextWriter writer = Xml.GetXmlTextWriter(SouborZmeny);

            writer.WriteStartDocument();
            writer.WriteStartElement(Xml.ZMENY);

            foreach (string zmena in this.zmeny)
            {
                writer.WriteElementString(Xml.ZMENA, zmena);
            }
            
            writer.WriteEndElement();
            writer.WriteEndDocument();

            Xml.CloseXmlTextWriter(writer);

            //Thread.Sleep(1000);
        }

        private void NactiZmeny()
        {
            XmlTextReader reader = Xml.GetXmlTextReader(SouborZmeny);
            List<string> seznam = new List<string>();
            
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == Xml.ZMENA)
                            seznam.Add(reader.ReadElementContentAsString());
                        break;
                    default:
                        // do nothing
                        break;
                }
            }
            this.zmeny = seznam;
            
            Xml.CloseXmlTextReader(reader);
        }

        public void UlozNovely()
        {
            if (this.novely == null)
                return;
            if (!Directory.Exists(Adresar))
                Directory.CreateDirectory(Adresar);

            XmlTextWriter writer = Xml.GetXmlTextWriter(SouborNovely);

            writer.WriteStartDocument();
            writer.WriteStartElement(Xml.NOVELIZACE);

            foreach (Novela n in this.novely)
            {
                writer.WriteStartElement(Xml.NOVELA);
                writer.WriteElementString(Xml.CISLO, n.Cislo.ToString(EncodingTools.NumberFormat));
                writer.WriteElementString(Xml.CASTKA, n.Castka.Cislo.ToString(EncodingTools.NumberFormat));
                writer.WriteElementString(Xml.ROCNIK, n.Castka.Rocnik.ToString(EncodingTools.NumberFormat));
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();

            Xml.CloseXmlTextWriter(writer);
            //Thread.Sleep(1000);
        }

        private void NactiNovely()
        {
            XmlTextReader reader = Xml.GetXmlTextReader(SouborNovely);

            SortedSet<Novela> seznam = new SortedSet<Novela>();

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == Xml.NOVELA)
                        {
                            int novCislo = 0, novCastka = 0, novRocnik = 0;
                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == Xml.NOVELA)
                                {
                                    seznam.Add(new Novela(this, (Index.NajdiPredpis(novCislo, new Castka(novCastka, novRocnik)))));
                                    break; // konec definice
                                }

                                switch (reader.NodeType)
                                {
                                    case XmlNodeType.Element:
                                        if (reader.Name == Xml.CISLO)
                                            novCislo = reader.ReadElementContentAsInt();
                                        else if (reader.Name == Xml.CASTKA)
                                            novCastka = reader.ReadElementContentAsInt();
                                        else if (reader.Name == Xml.ROCNIK)
                                            novRocnik = reader.ReadElementContentAsInt();
                                        break;
                                }

                            }
                        }
                        break;
                    default:
                        // do nothing
                        break;
                }
            }

            this.novely = seznam;
            
            Xml.CloseXmlTextReader(reader);
        }

        public void PridejNovelu(Predpis novela)
        {
            foreach (Novela n in Novely)
            {
                if (n.Cislo == novela.Cislo && n.Castka == novela.Castka)
                    return; // novela uz existuje
            }
            novely.Add(new Novela(this, novela));
            UlozNovely();
        }

        public class Novela : IComparable
        {
            Predpis original;
            Predpis novela;
            Text uplneZneni;

            public string ID
            {
                get
                {
                    return Index.GetId(Cislo, Castka.Rocnik);
                }
            }

            public string Soubor
            {
                get { return original.Soubor + "_" + ID; }
            }

            public string SouborText
            {
                get { return original.Adresar + '/' + Soubor + ".xml"; }
            }

            public int Cislo { get { return novela.Cislo; } }

            public Castka Castka { get { return novela.Castka; } }

            public string Nazev { get { return novela.Nazev; } }

            public Novela NasledujiciNovela
            {
                get
                {
                    foreach (Novela n in original.Novely)
                    {
                        if (n.CompareTo(this) > 0)
                            return n; // vratime prvni prvek za sebou samym
                    }
                    return null;
                }
            }

            public Novela PredchoziNovela
            {
                get
                {
                    foreach (Novela n in original.Novely.Reverse())
                    {
                        if (n.CompareTo(this) < 0)
                            return n;
                    }
                    return null;
                }
            }

            public Text UplneZneni
            {
                get
                {
                    if (uplneZneni == null)
                    {
                        //if (File.Exists(SouborText))
                        //    uplneZneni = Text.FromXmlFile(SouborText);
                        //else
                        //{
                            if (PredchoziNovela != null)
                               uplneZneni = Novelizator.UplneZneni(original, PredchoziNovela.UplneZneni, novela.Text, novela.Soubor);
                            else
                                uplneZneni = Novelizator.UplneZneni(original, original.Text, novela.Text, novela.Soubor);

                            int x = 1;
                            uplneZneni.ToXmlFile(SouborText);
                            int a = 1;
                           
                        //}
                    }
                    return uplneZneni;
                }
            }

            public int CompareTo(object obj)
            {
                Novela other = (Novela)obj;
                int roky = this.Castka.Rocnik.CompareTo(other.Castka.Rocnik);
                return roky != 0 ? roky : this.Cislo.CompareTo(other.Cislo);
            }

            public Novela(Predpis original, Predpis novela)
            {
                this.original = original;
                this.novela = novela;
            }


        }


    }
}
