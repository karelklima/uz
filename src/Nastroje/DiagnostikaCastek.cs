using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UZ.Sbirka;

namespace UZ.Nastroje
{
    class DiagnostikaCastek
    {
        public static void Spustit(bool toConsole)
        {
            string adresar = Nastaveni.PracovniAdresar + "\\DiagnostikaCastek";
            DirectoryInfo dirInfo = new DirectoryInfo(adresar);
            if (!dirInfo.Exists)
                dirInfo.Create();

            Log log = new Log(adresar, "log_" + Log.DateTimeStamp(), toConsole);
            log.Add("Diagnostika castek");
            log.AddLine();

            int locked = 0;
            int missing = 0;
            int counter = 0;

            DirectoryInfo info = new DirectoryInfo(Index.AdresarCastek);
            foreach (DirectoryInfo rocnik in info.EnumerateDirectories())
            {
                foreach (DirectoryInfo castka in rocnik.EnumerateDirectories())
                {
                    counter++;
                    string stamp = rocnik.Name + "/" + castka.Name;
                    if (File.Exists(castka.FullName + "\\" + Index.LOCK)) // zamknuty adresar
                    {
                        log.Add("ZAMEK\t" + stamp);
                        locked++;
                        continue;
                    }
                    else
                    {
                        Castka c = new Castka(Int32.Parse(castka.Name), Int32.Parse(rocnik.Name));
                        if (!File.Exists(c.SouborInfo) || !File.Exists(c.SouborText))
                        {
                            log.Add("CHYBA\t" + stamp);
                            missing++;
                        }
                    }
                }
            }

            log.AddLine();
            log.Add("Pocet castek: " + counter.ToString());
            log.Add("Pocet zamku: " + locked.ToString());
            log.Add("Pocet chybnych zpracovani: " + missing.ToString());

            log.Close();

        }
    }
}
