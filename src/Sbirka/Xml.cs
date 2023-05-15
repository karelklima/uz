using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace UZ.Sbirka
{
    class Xml
    {
        public const string TEXT = "text";
        public const string PREDPIS = "predpis";
        public const string PREDPISY = "predpisy";
        public const string CASTKA = "castka";
        public const string SEKCE = "sekce";
        public const string TYP = "typ";
        public const string CISLO = "cislo";
        public const string NAZEV = "nazev";
        public const string ZMENY = "zmeny";
        public const string ZMENA = "zmena";
        public const string ROCNIK = "rocnik";
        public const string NOVELIZACE = "novelizace";
        public const string NOVELA = "novela";
        public const string DATUM = "datum";
        public const string NADPIS = "nadpis";
        public const string OZNACENI = "oznaceni";
        public const string UVODNIUSTANOVENI = "uvodniustanoveni";
        public const string ZAVERECNEUSTANOVENI = "zaverecneustanoveni";
        public const string OBSAH = "obsah";
        public const string POZNAMKY = "poznamky";
        public const string POZNAMKA = "poznamka";
        public const string POSLEDNIZMENA = "poslednizmena";

        public static XmlTextWriter GetXmlTextWriter(string filename)
        {
            File.WriteAllText(filename, string.Empty); // smaze obsah souboru
            FileInfo souborXml = new FileInfo(filename);
            FileStream xmlstream = souborXml.OpenWrite();
            XmlTextWriter writer = new XmlTextWriter(xmlstream, Encoding.UTF8);
            writer.Formatting = Formatting.Indented;
            return writer;
        }

        public static void CloseXmlTextWriter(XmlTextWriter writer)
        {
            writer.Close(); // zavre i filestream, do ktereho zapisuje
        }

        public static XmlTextReader GetXmlTextReader(string filename)
        {
            FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            XmlTextReader reader = new XmlTextReader(stream);
            return reader;
        }

        public static void CloseXmlTextReader(XmlTextReader reader)
        {
            reader.Close();
        }

      
    }
}
