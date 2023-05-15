using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UZ.PDF;

namespace UZ.Sbirka
{
    class Podpora
    {

        public static Castka ExtrahujCislaZNazvuPdf(string filename)
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

        public static bool ShodaSouboru(string path1, string path2)
        {
            byte[] file1 = File.ReadAllBytes(path1);
            byte[] file2 = File.ReadAllBytes(path2);
            if (file1.Length == file2.Length)
            {
                for (int i = 0; i < file1.Length; i++)
                {
                    if (file1[i] != file2[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

    }
}
