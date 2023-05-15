using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.IO;
using UZ.PDF;

namespace UZ.Sbirka
{
    class Sekce
    {
        public const int PREAMBULE = 0;
        public const int CAST = 1;
        public const int HLAVA = 2;
        public const int DIL = 3;
        public const int ODDIL = 4;
        public const int PODODDIL = 5;
        public const int NADPIS = 6;
        public const int CLANEK = 7;
        public const int PARAGRAF = 8;
        public const int ODSTAVEC = 9;
        public const int PISMENO = 10;
        public const int BOD = 11;
        public const int POZNAMKA = 12;
        public const int TEXT = 13;
        public const int ZASOBNIK = 14;

        private static Dictionary<int, Sekce.ISekce> seznam;

        public static Dictionary<int, Sekce.ISekce> Seznam
        {
            get
            {
                if (seznam == null)
                {
                    seznam = new Dictionary<int, ISekce>();
                    seznam.Add(PREAMBULE, new Sekce.Preambule());
                    seznam.Add(CAST, new Sekce.Cast());
                    seznam.Add(HLAVA, new Sekce.Hlava());
                    seznam.Add(DIL, new Sekce.Dil());
                    seznam.Add(ODDIL, new Sekce.Oddil());
                    seznam.Add(PODODDIL, new Sekce.Pododdil());
                    seznam.Add(NADPIS, new Sekce.Nadpis());
                    seznam.Add(CLANEK, new Sekce.Clanek());
                    seznam.Add(PARAGRAF, new Sekce.Paragraf());
                    seznam.Add(ODSTAVEC, new Sekce.Odstavec());
                    seznam.Add(PISMENO, new Sekce.Pismeno());
                    seznam.Add(BOD, new Sekce.Bod());
                    seznam.Add(POZNAMKA, new Sekce.Poznamka());
                    seznam.Add(TEXT, new Sekce.Text());
                }
                return seznam;
            }
        }

        public abstract class ISekce
        {
            protected int typ;
            protected Regex regex;

            private string oznaceni = string.Empty;
            private string cislo = string.Empty;
            private string nadpis = string.Empty;
            private string uvodniUstanoveni = string.Empty;
            private string zaverecneUstanoveni = string.Empty;
            private string posledniZmena = string.Empty;
            private ISekce rodic;
            private List<ISekce> subsekce = new List<ISekce>();
            private List<ISekce> poznamky = new List<ISekce>();

            protected abstract void Init();

            public ISekce()
            {
                Init();
            }

            public ISekce(string oznaceni, string cislo, string nadpis = "") : this()
            {
                this.oznaceni = oznaceni;
                this.cislo = cislo;
                this.nadpis = nadpis;
            }

            public ISekce Factory()
            {
                return (ISekce)Activator.CreateInstance(this.GetType());
            }

            public void AddNadpis(string text)
            {
                if (nadpis != null && nadpis.Length > 0)
                    text = nadpis + '\n' + text;
                nadpis = text;
            }

            public void AddUvodniUstanoveni(string text)
            {
                if (uvodniUstanoveni != null && uvodniUstanoveni.Length > 0)
                    text = uvodniUstanoveni + '\n' + text;
                uvodniUstanoveni = text;
            }

            public void AddUvodniUstanoveni(List<string> textList)
            {
                foreach (string text in textList)
                    AddUvodniUstanoveni(text);
            }

            public void AddZaverecneUstanoveni(string text)
            {
                if (zaverecneUstanoveni != null && zaverecneUstanoveni.Length > 0)
                    text = zaverecneUstanoveni + '\n' + text;
                zaverecneUstanoveni = text;
            }

            public void AddSubsekce(ISekce s)
            {
                s.Rodic = this;
                subsekce.Add(s);
            }

            public void AddSubsekce(ISekce s, int index)
            {
                s.Rodic = this;
                subsekce.Insert(index, s);
            }

            public void AddPoznamka(ISekce poznamka)
            {
                poznamka.Rodic = this;
                poznamky.Add(poznamka);
            }

            public void SetPosledniZmena(string zmena)
            {
                this.posledniZmena = zmena;
            }

            public ISekce GetKopie()
            {
                ISekce kopie = this.Factory();
                kopie.oznaceni = this.oznaceni;
                kopie.cislo = this.cislo;
                kopie.nadpis = this.nadpis;
                kopie.AddUvodniUstanoveni(this.uvodniUstanoveni);
                kopie.AddZaverecneUstanoveni(this.zaverecneUstanoveni);
                // rodic se nekopiruje - pointer

                foreach (ISekce pozn in this.poznamky)
                    kopie.AddPoznamka(pozn.GetKopie());

                foreach (ISekce sub in this.subsekce)
                    kopie.AddSubsekce(sub.GetKopie());

                kopie.SetPosledniZmena(this.posledniZmena);

                return kopie;
            }

            public string Oznaceni { get { return oznaceni; } set { oznaceni = value; } }

            public string Cislo { get { return cislo; } set { cislo = value; } }

            public string Nadpis { get { return nadpis; } set { nadpis = value; } }

            public string UvodniUstanoveni { get { return uvodniUstanoveni; } set { uvodniUstanoveni = value; } }

            public string ZaverecneUstanoveni { get { return zaverecneUstanoveni; } set { zaverecneUstanoveni = value; } }

            public Regex Regex { get { return regex; } }

            public ISekce Rodic { get { return rodic; } set { rodic = value; } }

            public List<ISekce> Subsekce { get { return subsekce; } }

            public List<ISekce> Poznamky { get { return poznamky; } }

            public int Typ { get { return typ; } }

            public void ToXmlFile(string filename)
            {
                XmlTextWriter writer = Xml.GetXmlTextWriter(filename);
                this.ToXML(writer);
                Xml.CloseXmlTextWriter(writer);
            }

            public void ToXML(XmlTextWriter writer)
            {
                writer.WriteStartElement(Xml.SEKCE);
                writer.WriteElementString(Xml.TYP, typ.ToString(EncodingTools.NumberFormat));
                if (oznaceni.Length > 0)
                    writer.WriteElementString(Xml.OZNACENI, oznaceni);
                if (cislo.Length > 0)
                    writer.WriteElementString(Xml.CISLO, cislo);
                if (nadpis.Length > 0)
                    writer.WriteElementString(Xml.NADPIS, nadpis);
                if (uvodniUstanoveni.Length > 0)
                    writer.WriteElementString(Xml.UVODNIUSTANOVENI, uvodniUstanoveni);
                if (zaverecneUstanoveni.Length > 0)
                    writer.WriteElementString(Xml.ZAVERECNEUSTANOVENI, zaverecneUstanoveni);

                if (subsekce.Count > 0)
                {
                    writer.WriteStartElement(Xml.OBSAH);
                    foreach (ISekce sub in subsekce)
                        sub.ToXML(writer);
                    writer.WriteEndElement(); // obsah
                }
                if (poznamky.Count > 0)
                {
                    writer.WriteStartElement(Xml.POZNAMKY);
                    foreach (ISekce pozn in poznamky)
                        pozn.ToXML(writer);
                    writer.WriteEndElement(); // poznamky
                }

                if (posledniZmena.Length > 0)
                    writer.WriteElementString(Xml.POSLEDNIZMENA, posledniZmena);

                writer.WriteEndElement(); // sekce
            }

            public static ISekce FromXmlFile(string filename)
            {
                XmlTextReader reader = Xml.GetXmlTextReader(filename);
                ISekce output = FromXml(reader);
                Xml.CloseXmlTextReader(reader);
                return output;
            }

            public static ISekce FromXml(XmlTextReader reader)
            {
                Sekce.ISekce output = new Zasobnik();
                bool cteniObsahu = false;
                bool cteniPoznamek = false;
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name == Xml.SEKCE && !cteniObsahu && !cteniPoznamek)
                                    continue; // zacatek sekce
                            if (reader.Name == Xml.TYP)
                            {
                                int t = reader.ReadElementContentAsInt();
                                if (Sekce.Seznam.ContainsKey(t))
                                    output = Sekce.Seznam[t].Factory();
                            }
                            else if (reader.Name == Xml.OZNACENI)
                                output.Oznaceni = reader.ReadElementContentAsString();
                            else if (reader.Name == Xml.CISLO)
                                output.Cislo = reader.ReadElementContentAsString();
                            else if (reader.Name == Xml.NADPIS)
                                output.Nadpis = reader.ReadElementContentAsString();
                            else if (reader.Name == Xml.UVODNIUSTANOVENI)
                                output.UvodniUstanoveni = reader.ReadElementContentAsString();
                            else if (reader.Name == Xml.ZAVERECNEUSTANOVENI)
                                output.ZaverecneUstanoveni = reader.ReadElementContentAsString();
                            else if (reader.Name == Xml.OBSAH)
                                cteniObsahu = true;
                            else if (reader.Name == Xml.SEKCE && cteniObsahu)
                                output.AddSubsekce(ISekce.FromXml(reader));
                            else if (reader.Name == Xml.POZNAMKY)
                                cteniPoznamek = true;
                            else if (reader.Name == Xml.SEKCE && cteniPoznamek)
                                output.AddPoznamka(ISekce.FromXml(reader));
                            else if (reader.Name == Xml.POSLEDNIZMENA)
                                output.SetPosledniZmena(reader.ReadElementContentAsString());

                            break;
                        case XmlNodeType.EndElement:
                            if (reader.Name == Xml.SEKCE)
                                return output;
                            if (reader.Name == Xml.OBSAH)
                                cteniObsahu = false;
                            if (reader.Name == Xml.POZNAMKY)
                                cteniPoznamek = false;
                            break;
                        default:
                            // do nothing
                            break;
                    }
                }
                throw new UZException("Nenalezen konec XML sekce");
            }
        }

        public class Preambule : ISekce
        {
            protected override void Init()
            {
                typ = PREAMBULE;
                regex = new Regex("^(PREAMBULE)$");
            }
        }

        public class Cast : ISekce
        {
            protected override void Init()
            {
                typ = CAST;
                //regex = new Regex("^ČÁST[ ]{0,}([IVXLDM]{1,8}|PRVNÍ|DRUHÁ|TŘETÍ|ČTVRTÁ|PÁTÁ|ŠESTÁ|SEDMÁ|OSMÁ|DEVÁTÁ|DESÁTÁ|JEDENÁCTÁ|DVANÁCTÁ|TŘINÁCTÁ|ČTRNÁCTÁ|PATNÁCTÁ|ŠESTNÁCTÁ|SEDMNÁCTÁ|OSMNÁCTÁ|DEVATENÁCTÁ|DVACÁTÁ|DVACÁTÁPRVNÍ|DVACÁTÁDRUHÁ|DVACÁTÁ PRVNÍ|DVACÁTÁ DRUHÁ|DVACÁTÁ TŘETÍ|DVACÁTÁ ČTVRTÁ|DVACÁTÁ PÁTÁ)$");
                regex = new Regex("^ČÁST[ ]{0,}([IVXLDM]{1,8}|(DVACÁTÁ|TŘICÁTÁ|ČTYŘICÁTÁ)?( )?(PRVNÍ|DRUHÁ|TŘETÍ|ČTVRTÁ|PÁTÁ|ŠESTÁ|SEDMÁ|OSMÁ|DEVÁTÁ)|DESÁTÁ|JEDENÁCTÁ|DVANÁCTÁ|TŘINÁCTÁ|ČTRNÁCTÁ|PATNÁCTÁ|ŠESTNÁCTÁ|SEDMNÁCTÁ|OSMNÁCTÁ|DEVATENÁCTÁ|DVACÁTÁ|TŘICÁTÁ|ČTYŘICÁTÁ)$");
            
            }
        }

        public class Hlava : ISekce
        {
            protected override void Init()
            {
                typ = HLAVA;
                regex = new Regex("^HLAVA[ ]{0,}(PRVNÍ|DRUHÁ|TŘETÍ|TŘETÍ[ ]A|ČTVRTÁ|PÁTÁ|ŠESTÁ|SEDMÁ|OSMÁ|DEVÁTÁ|DESÁTÁ|JEDENÁCTÁ|DVANÁCTÁ|TŘINÁCTÁ|ČTRNÁCTÁ|PATNÁCTÁ|[IVXLDM]{1,4})$");
            }
        }

        public class Dil : ISekce
        {
            protected override void Init()
            {
                typ = DIL;
                regex = new Regex("^Díl[ ]{0,}([1-9][0-9]{0,2}|[IVXLDM]{1,8}|první|druhý|třetí|čtvrtý|pátý|šestý|sedmý|osmý|devátý|desátý|jedenáctý|dvanáctý|třináctý|čtrnáctý)$");
            }
        }

        public class Oddil : ISekce
        {
            protected override void Init()
            {
                typ = ODDIL;
                regex = new Regex("^Oddíl[ ]{0,}([1-9][0-9]{0,2}|[IVXLDM]{1,8}|první|druhý|třetí|čtvrtý|pátý|šestý|sedmý|osmý|devátý|desátý)$");
            }
        }

        public class Pododdil : ISekce
        {
            protected override void Init()
            {
                typ = PODODDIL;
                regex = new Regex("^Pododdíl[ ]{0,}([1-9][0-9]{0,2}|[IVXLDM]{1,8})$");
            }
        }

        public class Nadpis : ISekce
        {
            protected override void Init()
            {
                typ = NADPIS;
                regex = new Regex("^(?![0-9]{1,3}\\.|[a-z]\\)|\\([0-9]|Čl\\.|§|ČÁST|HLAVA|Díl|Oddíl|Pododdíl|[0-9]{1,4}<>\\))");
            }
        }

        public class Clanek : ISekce
        {
            protected override void Init()
            {
                typ = CLANEK;
                regex = new Regex("^Čl.[ ]{0,}([1-9][0-9]{0,3}[a-z]?|[IVXLDM]{1,8})$");
            }
        }

        public class Paragraf : ISekce
        {
            protected override void Init()
            {
                typ = PARAGRAF;
                regex = new Regex("^§[ ]{0,}([1-9][0-9]{0,3}[ ]?[a-z]{0,2})$");
            }
        }

        public class Odstavec : ISekce
        {
            protected override void Init()
            {
                typ = ODSTAVEC;
                regex = new Regex("^\\(([1-9][0-9]{0,1})\\)");
            }
        }

        public class Pismeno : ISekce
        {
            protected override void Init()
            {
                typ = PISMENO;
                regex = new Regex("^([a-z])\\)");
            }
        }

        public class Bod : ISekce
        {
            protected override void Init()
            {
                typ = BOD;
                regex = new Regex("^([1-9][0-9]{0,2})\\.");
            }
        }

        public class Poznamka : ISekce
        {
            protected override void Init()
            {
                typ = POZNAMKA;
                regex = new Regex("^([0-9]{1,3}[a-z]{0,1})<>\\)");
            }
        }

        public class Text : ISekce
        {
            protected override void Init()
            {
                typ = TEXT;
                regex = new Regex("(?=a)b"); // never match
            }
        }

        public class Zasobnik : ISekce
        {
            protected override void Init()
            {
                typ = ZASOBNIK;
                regex = new Regex("(?=a)b"); // never match
            }
        }

    }
}
