using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UZ.Sbirka.Adaptery;

namespace UZ.Sbirka
{
    class TestNovel
    {

        public class Statistika
        {
            public int PocetPredpisu = 0;
            public int PocetVadnychPredpisu = 0;

            public int PocetNovel = 0;
            public int PocetPravidel = 0;
            public int PocetNerozpoznanychPravidel = 0;

            public Dictionary<string, int> Operace = new Dictionary<string,int>();

            public void ToXmlFile(string filename)
            {
                XmlTextWriter writer = Xml.GetXmlTextWriter(filename);
                this.ToXML(writer);
                Xml.CloseXmlTextWriter(writer);
            }

            public void ToXML(XmlTextWriter writer)
            {
                writer.WriteStartElement("statistika");
                writer.WriteElementString("pocetpredpisu", PocetPredpisu.ToString());
                writer.WriteElementString("pocetvadnychpredpisu", PocetVadnychPredpisu.ToString());
                writer.WriteElementString("pocetnovel", PocetNovel.ToString());
                writer.WriteElementString("pocetpravidel", PocetPravidel.ToString());
                writer.WriteElementString("pocetnerozpoznanychpravidel", PocetNerozpoznanychPravidel.ToString());
                
                writer.WriteStartElement("seznamoperaci");
                foreach (KeyValuePair<string, int> op in Operace)
                {
                    writer.WriteStartElement("operace");
                    writer.WriteAttributeString("typ", op.Key);
                    writer.WriteAttributeString("pocet", op.Value.ToString());
                    writer.WriteEndElement();
                }
                
                writer.WriteEndElement(); // statistika
            }

            public static Statistika FromXmlFile(string filename)
            {
                XmlTextReader reader = Xml.GetXmlTextReader(filename);
                Statistika output = FromXml(reader);
                Xml.CloseXmlTextReader(reader);
                return output;
            }

            public static Statistika FromXml(XmlTextReader reader)
            {
                Statistika output = new Statistika();
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name == "pocetpredpisu")
                                output.PocetPredpisu = reader.ReadElementContentAsInt();
                            else if (reader.Name == "pocetvadnychpredpisu")
                                output.PocetVadnychPredpisu = reader.ReadElementContentAsInt();
                            else if (reader.Name == "pocetnovel")
                                output.PocetNovel = reader.ReadElementContentAsInt();
                            else if (reader.Name == "pocetpravidel")
                                output.PocetPravidel = reader.ReadElementContentAsInt();
                            else if (reader.Name == "pocetnerozpoznanychpravidel")
                                output.PocetNerozpoznanychPravidel = reader.ReadElementContentAsInt();
                            else if (reader.Name == "pocetpredpisu")
                                output.PocetPredpisu = reader.ReadElementContentAsInt();
                            else if (reader.Name == "operace")
                            {
                                string typ = reader.GetAttribute("typ");
                                string pocet = reader.GetAttribute("pocet");
                                output.Operace[typ] = Int32.Parse(pocet);
                            }
                            break;
                        case XmlNodeType.EndElement:
                            if (reader.Name == "statistika")
                                return output;
                            break;
                        default:
                            // do nothing
                            break;
                    }
                }
                throw new UZException("Nenalezen konec XML sekce");
            }
        }

        public class NerozpoznanaPravidla
        {
            public List<string> Pravidla = new List<string>();

            public void ToXmlFile(string filename)
            {
                XmlTextWriter writer = Xml.GetXmlTextWriter(filename);
                this.ToXML(writer);
                Xml.CloseXmlTextWriter(writer);
            }

            public void ToXML(XmlTextWriter writer)
            {
                writer.WriteStartElement("nerozpoznanapravidla");
                foreach (string pr in Pravidla)
                {
                    writer.WriteElementString("pravidlo", pr);
                }
                writer.WriteEndElement(); // statistika
            }

            public static NerozpoznanaPravidla FromXmlFile(string filename)
            {
                XmlTextReader reader = Xml.GetXmlTextReader(filename);
                NerozpoznanaPravidla output = FromXml(reader);
                Xml.CloseXmlTextReader(reader);
                return output;
            }

            public static NerozpoznanaPravidla FromXml(XmlTextReader reader)
            {
                NerozpoznanaPravidla output = new NerozpoznanaPravidla();
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name == "pravidlo")
                                output.Pravidla.Add(reader.ReadElementContentAsString());
                            break;
                        case XmlNodeType.EndElement:
                            if (reader.Name == "nerozpoznanapravidla")
                                return output;
                            break;
                        default:
                            // do nothing
                            break;
                    }
                }
                throw new UZException("Nenalezen konec XML sekce");
            }
        }
        

        private Statistika statistika = new Statistika();
        private List<string> log = new List<string>();

        private DirectoryInfo adresar;

        public static Statistika ZpracujRocnik(int rocnik = 1993)
        {
            TestNovel test = new TestNovel(rocnik);
            return test.statistika;
        }

        private void Log(int cislo, int rocnik, string message)
        {
            Log(rocnik.ToString() + "/" + cislo.ToString() + "\t" + message);
        }

        private void Log(string message)
        {
            log.Add(message);
            Console.WriteLine(message);
        }

        private TestNovel(int rocnik = 1993)
        {
            adresar = new DirectoryInfo("testnovel/" + rocnik.ToString());
            if (!adresar.Exists)
                adresar.Create();

            Log("Rocnik: " + rocnik.ToString());
            List<Predpis> predpisy = NajdiPredpisy(rocnik);
            statistika.PocetPredpisu = predpisy.Count();
            Log("Pocet predpisu: " + statistika.PocetPredpisu.ToString());
            List<Predpis> novely = NajdiNovely(predpisy);
            statistika.PocetNovel = novely.Count();
            Log("Pocet neinterpretovanych predpisu: " + statistika.PocetVadnychPredpisu.ToString());
            Log("Pocet novel: " + statistika.PocetNovel.ToString());


            foreach (Predpis novela in novely)
            {
                ZkontrolujNovelu(novela);   
            }

            Log("Pocet novelizacnich pravidel:\t" + statistika.PocetPravidel);
            Log("Pocet nerozpoznanych pravidel:\t" + statistika.PocetNerozpoznanychPravidel);



            using (StreamWriter outfile = new StreamWriter(adresar.ToString() + "/log_" + rocnik.ToString() + ".txt"))
            {
                foreach (string message in log)
                {
                    outfile.WriteLine(message);
                }
            }

            statistika.ToXmlFile(adresar.ToString() + "/statistika_" + rocnik.ToString() + ".xml");
        }

        private List<Predpis> NajdiPredpisy(int rocnik)
        {
            List<Predpis> vystup = new List<Predpis>();

            DirectoryInfo info = new DirectoryInfo(Index.AdresarPredpisu + "/" + rocnik.ToString());
            foreach (DirectoryInfo predpis in info.EnumerateDirectories())
            {
                int cislo = Int32.Parse(predpis.Name);
                try
                {
                    Predpis p = new Predpis(cislo, rocnik);
                    vystup.Add(p);
                }
                catch (UZException e)
                {
                    statistika.PocetVadnychPredpisu++;
                    Log(cislo, rocnik, e.Message);
                }
            }

            return vystup;
        }

        private List<Predpis> NajdiNovely(List<Predpis> predpisy)
        {
            List<Predpis> vystup = new List<Predpis>();

            foreach (Predpis predpis in predpisy)
            {
                try
                {
                    foreach (string zmena in predpis.Zmeny)
                    {
                        string[] zmeny = zmena.Split('/');
                        int rocnik = Int32.Parse(zmeny[1]);
                        if (rocnik >= 1993)
                        {
                            vystup.Add(predpis);
                            break;
                        }
                    }
                }
                catch (UZException e)
                {
                    statistika.PocetVadnychPredpisu++;
                    Log(predpis.Cislo, predpis.Rocnik, e.Message);
                }
            }

            return vystup;
        }

        private void ZkontrolujNovelu(Predpis predpis)
        {
            List<string> zmeny = new List<string>();
            foreach (string zmena in predpis.Zmeny)
            {
                string[] z = zmena.Split('/');
                int rocnik = Int32.Parse(z[1]);
                if (rocnik >= 1993)
                {
                    zmeny.Add(zmena);
                    break;
                }
                
            }

            int lokPocetPravidel = 0;
            int lokVadnaPravidla = 0;

            foreach (string zmena in zmeny)
            {
                string[] z = zmena.Split('/');
                int c = Int32.Parse(z[0]);
                int r = Int32.Parse(z[1]);
                IAdapter adapter = new Zakon();
                Predpis zmenaPredpis;
                try
                {
                    zmenaPredpis = new Predpis(zmena);
                    adapter = zmenaPredpis.Adapter;
                }
                catch (NullReferenceException e) { }; // neni v indexu
                
                try
                {
                    Novelizator.Statistika stat = Novelizator.UplneZneniTest(c, r, adapter, predpis.Text);
                    lokVadnaPravidla = stat.NerozpoznaneBody.Count();
                    lokPocetPravidel = stat.Operace.Count() + lokVadnaPravidla;

                    statistika.PocetPravidel += lokPocetPravidel;
                    statistika.PocetNerozpoznanychPravidel += lokVadnaPravidla;

                    if (lokVadnaPravidla > 0)
                    {
                        NerozpoznanaPravidla pravidla = new NerozpoznanaPravidla();
                        foreach (Sekce.ISekce bod in stat.NerozpoznaneBody)
                        {
                            pravidla.Pravidla.Add(bod.UvodniUstanoveni);
                        }
                        pravidla.ToXmlFile(adresar.ToString() + "/x" + Index.GetId(predpis.Cislo, predpis.Rocnik) + "_"
                            + Index.GetId(c, r)
                            + ".xml");
                    }

                }
                catch (UZException e)
                {
                    Log(e.Message);
                }

            }
        }
    }
}
