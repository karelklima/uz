using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Globalization;

namespace UZ
{
    class Nastaveni
    {

        private static Nastaveni instance;
        
        private const string filename = "nastaveni.xml";
        private const string NASTAVENI = "nastaveni";
        private const string ZAZNAM = "zaznam";
        private const string KLIC = "klic";
        private const string HODNOTA = "hodnota";
        private bool zmena = false;
        private Dictionary<string, string> zaznamy = new Dictionary<string, string>();
        private DirectoryInfo workingDirectory;

        public string this[string klic]
        {
            get { return zaznamy.ContainsKey(klic) ? zaznamy[klic] : string.Empty; }
            set { zaznamy[klic] = value; zmena = true; }
        }

        public static Nastaveni Instance()
        {
            if (instance == null)
                instance = new Nastaveni(new DirectoryInfo("."));
            return instance;
        }

        public static void Set(string klic, string hodnota)
        {
            Instance()[klic] = hodnota;
        }

        public static void SetInt(string klic, int hodnota)
        {
            Set(klic, hodnota.ToString(NumberFormatInfo.InvariantInfo));
        }

        public static string Get(string klic)
        {
            return Instance()[klic];
        }

        public static int GetInt(string klic)
        {
            int value;
            if (Int32.TryParse(Get(klic), out value))
                return value;
            return 0;
        }

        public static DirectoryInfo PracovniAdresar
        {
            get
            {
                return Instance().workingDirectory;
            }
            set
            {
                if (instance != null && instance.zmena)
                    instance.Save();
                instance = new Nastaveni(value);
            }
        }

        public static void Ulozit()
        {
            Instance().Save();
        }

        private Nastaveni(DirectoryInfo workingDir)
        {
            this.workingDirectory = new DirectoryInfo(workingDir.FullName);

            string file = ConfigFile();

            if (!File.Exists(file))
            {
                CreateDefaultProperties();
                return;
            }

            FileStream FS = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (FS.Length < 1)
                return;

            XmlTextReader reader = new XmlTextReader(FS);
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == ZAZNAM)
                {
                    string klic = reader.GetAttribute(KLIC);
                    string hodnota = reader.GetAttribute(HODNOTA);
                    zaznamy[klic] = hodnota;
                }
            }
            reader.Close();
            FS.Close();
        }

        ~Nastaveni()
        {
            if (zmena)
            {
                Save();
            }
        }

        private string ConfigFile()
        {
            return workingDirectory.FullName + "/" + filename;
        }

        private void CreateDefaultProperties()
        {
            this["MVCRSbirkaURL"] = "http://aplikace.mvcr.cz/sbirka-zakonu/SearchResult.aspx?q={0}&typeLaw=zakon&what=Rok&stranka={1}";
            this["MVCRSbirkaURLPrefix"] = "http://aplikace.mvcr.cz/sbirka-zakonu/";
            this["MVCRSbirkaURLRegex"] = "^ViewFile\\.aspx\\?type=c&id=[0-9]{1,10}$";
            this["AdresarSbirka"] = "Sbirka";
            this["AdresarVstup"] = "Vstup";
            this["AdresarVystup"] = "Vystup";
            this["PosledniCastkaRocnik"] = "1993";
            this["PosledniCastkaCislo"] = "0";
            this.Save();
        }

        private void Save()
        {
            FileStream NF = new FileStream(ConfigFile(), FileMode.Create, FileAccess.Write, FileShare.Write);
            XmlTextWriter writer = new XmlTextWriter(NF, Encoding.Default);
            writer.Formatting = Formatting.Indented;
            writer.WriteStartDocument();
            writer.WriteStartElement(NASTAVENI);

            foreach (KeyValuePair<string, string> zaznam in zaznamy)
            {
                writer.WriteStartElement(ZAZNAM);
                writer.WriteAttributeString(KLIC, zaznam.Key);
                writer.WriteAttributeString(HODNOTA, zaznam.Value);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();
        }

        
    }
}
