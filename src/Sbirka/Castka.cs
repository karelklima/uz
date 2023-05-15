using System;
using System.Collections.Generic;
using System.Text;
using UZ.PDF;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;

namespace UZ.Sbirka
{
    class Castka
    {
        
        private int cislo;
        private int rocnik;

        private string datumRozeslani;
        private List<Predpis> predpisy;

        private StructuredDocument text;

        public int Cislo
        {
            get { return cislo; }
        }

        public int Rocnik
        {
            get { return rocnik; }
        }

        public string Oznaceni
        {
            get
            {
                return String.Format("Částka č. {0}/{1}", Cislo, Rocnik);
            }
        }

        public string DatumRozeslani
        {
            get
            {
                return datumRozeslani;
            }
        }

        public string Adresar
        {
            get
            {
                return Index.AdresarCastek + '/' + rocnik.ToString(EncodingTools.NumberFormat)
                    + '/' + cislo.ToString(EncodingTools.NumberFormat).PadLeft(4, '0');
            }
        }

        public string Soubor
        {
            get
            {
                return "sb" + cislo.ToString(EncodingTools.NumberFormat).PadLeft(4, '0')
                    + '-' + rocnik.ToString(EncodingTools.NumberFormat);
            }
        }

        public string SouborPdf
        {
            get
            {
                return Adresar + '/' + Soubor + ".pdf";
            }
        }

        public string SouborInfo
        {
            get
            {
                return Adresar + '/' + Soubor + "_info.xml";
            }
        }

        public string SouborText
        {
            get
            {
                return Adresar + '/' + Soubor + "_text.xml";
            }
        }

        public string SouborZamek
        {
            get
            {
                return Adresar + '/' + Index.LOCK;
            }
        }

        public StructuredDocument Dokument
        {
            get
            {
                if (text != null)
                    return text;
                if (File.Exists(SouborText))
                {
                    text = StructuredDocument.FromXmlFile(SouborText);
                }
                else
                {
                    try
                    {
                        text = NactiPdf();
                        text.ToXmlFile(SouborText);

                    }
                    catch (PdfException e)
                    {
                        throw new UZException(String.Format("PDF soubor castky {0}/{1} se nepodarilo dekodovat", cislo, rocnik));
                    }
                }
                return text;
            }
        }

        public List<Predpis> Predpisy
        {
            get
            {
                if (predpisy == null)
                {
                    try
                    {
                        if (File.Exists(SouborInfo))
                            NactiInfo();
                        else
                        {
                            NactiObsah();
                            UlozInfo();
                        }
                    }
                    catch (InvalidDataException e)
                    {
                        //throw new UZException("nelze nacist obsah castky");
                        predpisy = new List<Predpis>();
                    }
                }
                return predpisy;
            }
        }

        public Castka(int cislo, int rocnik)
        {
            this.cislo = cislo;
            this.rocnik = rocnik;
        }

        public static Castka ZPdf(string filename)
        {
            FileInfo file = new FileInfo(filename);
            Regex regex = new Regex("^sb[0-9]{1,5}-[0-9]{1,5}.pdf$");
            if (!regex.IsMatch(file.Name))
                throw new UZException("File not valid: " + filename);

            string nazev = file.Name;
            nazev = nazev.Remove(nazev.Length - file.Extension.Length, file.Extension.Length);
            nazev = nazev.Remove(0, 2);
            string[] cisla = nazev.Split('-');

            int castka = Int32.Parse(cisla[0], EncodingTools.NumberFormat);
            int rocnik = Int32.Parse(cisla[1], EncodingTools.NumberFormat);
            if (rocnik < 100)
                rocnik = rocnik < 45 ? 2000 + rocnik : 1900 + rocnik;

            return new Castka(castka, rocnik);
        }

        private StructuredDocument NactiPdf()
        {
            float spaceWidthFactor = 1f;
            float lineBreakFactor = 1.0f;
            /*if (this.rocnik >= 2010) // zvetsilo se radkovani
            {
                lineBreakFactor = 0.8f;
            }*/
            
            return StructuredDocument.FromPdfFile(SouborPdf, 0, spaceWidthFactor, lineBreakFactor);
        }

        private void NactiObsah()
        {
            StructuredDocument.Page prvniStrana = Dokument.Pages[0];

            List<StructuredDocument.IRenderedObject> objekty = prvniStrana.SortedRenderedObjects;

            string rozeslana = "Rozeslána dne";
            string obsah1 = "OBSAH";
            string obsah2 = "O B S A H";

            List<Predpis> vlastniObsah = new List<Predpis>();

            int stav = 0;
            foreach (StructuredDocument.IRenderedObject obj in objekty)
            {
                switch (stav)
                {
                    case 0: // datum rozeslani
                        if (obj.ContentType != StructuredDocument.ContentType.Paragraph)
                            continue;
                        foreach (string radek in ((StructuredDocument.Paragraph)obj).Rows)
                        {
                            if (radek.StartsWith(rozeslana))
                            {
                                datumRozeslani = radek.Substring(rozeslana.Length).Trim();
                                stav = 1;
                            }
                        }
                        break;
                    case 1: // hledani markeru pro obsah
                        if (obj.ContentType != StructuredDocument.ContentType.Paragraph)
                            continue;
                        List<string> radky = ((StructuredDocument.Paragraph)obj).Rows;
                        if (radky.Count == 1 && (radky[0].StartsWith(obsah1) || radky[0].StartsWith(obsah2)))
                        {
                            stav = 2;
                        }
                        break;
                    case 2: // vlastni nacitani obsahu
                        if (obj.ContentType != StructuredDocument.ContentType.Paragraph)
                        {
                            if (vlastniObsah.Count > 0)
                            {
                                predpisy = vlastniObsah;
                                return;
                            }
                            continue;
                        }
                        string odstavec = obj.ToString(); // Format: "1. Zakon c. 1/2000 o ..."

                        Regex reg = new Regex("^[0-9]{1,4}\\.");
                        if (reg.IsMatch(odstavec))
                        {
                            Predpis predpis = Predpis.ZObsahuCastky(odstavec, this);
                            vlastniObsah.Add(predpis);
                        }
                        else if (vlastniObsah.Count > 0)
                        {
                            Predpis minuly = vlastniObsah[vlastniObsah.Count - 1];
                            minuly.SetNazev(minuly.Nazev + " " + odstavec);
                        }
                        break;
                    
                }

            }

            if (stav == 2 && vlastniObsah.Count > 0) // chybi horizontalni cara
            {
                predpisy = vlastniObsah;
                return; // hotovo
            }

            throw new UZException(String.Format("Nepodarilo se nacist obsah z castky {0}/{1}", cislo, rocnik));
        }

        public void UlozInfo()
        {

            if (!Directory.Exists(Adresar))
                Directory.CreateDirectory(Adresar);

            XmlTextWriter writer = Xml.GetXmlTextWriter(SouborInfo);

            writer.WriteStartDocument();
            writer.WriteStartElement(Xml.CASTKA);

            writer.WriteElementString(Xml.CISLO, cislo.ToString(EncodingTools.NumberFormat));
            writer.WriteElementString(Xml.ROCNIK, rocnik.ToString(EncodingTools.NumberFormat));
            writer.WriteElementString(Xml.DATUM, datumRozeslani);

            writer.WriteStartElement(Xml.PREDPISY);
            foreach (Predpis p in Predpisy)
            {
                writer.WriteStartElement(Xml.PREDPIS);
                writer.WriteElementString(Xml.TYP, p.Adapter.Typ);
                writer.WriteElementString(Xml.CISLO, p.Cislo.ToString(EncodingTools.NumberFormat));
                writer.WriteElementString(Xml.NAZEV, p.Nazev);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.WriteEndDocument();

            Xml.CloseXmlTextWriter(writer);
        }

        private void NactiInfo()
        {
            XmlTextReader reader = Xml.GetXmlTextReader(SouborInfo);

            predpisy = new List<Predpis>();

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == Xml.CASTKA)
                            continue; // zacatek sekce
                        else if (reader.Name == Xml.CISLO)
                            this.cislo = reader.ReadElementContentAsInt();
                        else if (reader.Name == Xml.ROCNIK)
                            this.rocnik = reader.ReadElementContentAsInt();
                        else if (reader.Name == Xml.DATUM)
                            this.datumRozeslani = reader.ReadElementContentAsString();
                        else if (reader.Name == Xml.PREDPIS)
                        {
                            int prCislo = 0;
                            string prTyp = string.Empty, prNazev = string.Empty;
                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == Xml.PREDPIS)
                                {
                                    Predpis predpis = Index.NajdiPredpis(prCislo, Rocnik);
                                    if (predpis == null)
                                    {
                                        predpis = Predpis.ZObsahuCastky(prTyp, prCislo, prNazev, this);
                                    }
                                    predpisy.Add(predpis);
                                    break; // konec definice
                                }

                                switch (reader.NodeType)
                                {
                                    case XmlNodeType.Element:
                                        if (reader.Name == Xml.CISLO)
                                            prCislo = reader.ReadElementContentAsInt();
                                        else if (reader.Name == Xml.TYP)
                                            prTyp = reader.ReadElementContentAsString();
                                        else if (reader.Name == Xml.NAZEV)
                                            prNazev = reader.ReadElementContentAsString();
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

        }



    }
}
