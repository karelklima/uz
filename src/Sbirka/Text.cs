using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Xml;

namespace UZ.Sbirka
{
    class Text
    {
        public const string UVOD = "Úvod";
        public const string OBSAH = "Obsah";
        public const string ZAVER = "Závěr";

        private Sekce.Zasobnik uvod;
        private Sekce.Zasobnik obsah;
        private Sekce.Zasobnik zaver;

        public Sekce.Zasobnik Uvod { get { return uvod; } }
        public Sekce.Zasobnik Obsah { get { return obsah; } }
        public Sekce.Zasobnik Zaver { get { return zaver; } }

        public Text()
        {
            uvod = new Sekce.Zasobnik();
            uvod.Oznaceni = UVOD;
            obsah = new Sekce.Zasobnik();
            obsah.Oznaceni = OBSAH;
            zaver = new Sekce.Zasobnik();
            zaver.Oznaceni = ZAVER;
        }

        private Text(Sekce.Zasobnik uvod, Sekce.Zasobnik obsah, Sekce.Zasobnik zaver)
        {
            this.uvod = uvod;
            this.obsah = obsah;
            this.zaver = zaver;
        }

        public void ToXmlFile(string filename)
        {
            XmlTextWriter writer = Xml.GetXmlTextWriter(filename);
            writer.WriteStartElement(Xml.TEXT);

            uvod.ToXML(writer);
            obsah.ToXML(writer);
            zaver.ToXML(writer);

            writer.WriteEndElement();
            Xml.CloseXmlTextWriter(writer);
            //Thread.Sleep(1000);
        }

        public static Text FromXmlFile(string filename)
        {
            try
            {
                XmlTextReader reader = Xml.GetXmlTextReader(filename);

                Sekce.Zasobnik uvod = (Sekce.Zasobnik)Sekce.ISekce.FromXml(reader);
                Sekce.Zasobnik obsah = (Sekce.Zasobnik)Sekce.ISekce.FromXml(reader);
                Sekce.Zasobnik zaver = (Sekce.Zasobnik)Sekce.ISekce.FromXml(reader);

                Xml.CloseXmlTextReader(reader);

                return new Text(uvod, obsah, zaver);
            }
            catch (UZException e)
            {
                int x = 0;
                Console.WriteLine(e);
            }

            return null;
        }

        public Text Kopie()
        {
            return new Text((Sekce.Zasobnik)uvod.GetKopie(), (Sekce.Zasobnik)obsah.GetKopie(), (Sekce.Zasobnik)zaver.GetKopie());
        }

    }
}
