using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UZ.Sbirka;

namespace UZ.Nastroje
{
    class TestInterpretace
    {
        public static void MnohoNadpisuZaSebou(int minRok, int maxRok)
        {
            Log log = new Log("Debug", "testinterpretace_" + Log.DateTimeStamp(), true);

            DirectoryInfo adresarPredpisu = new DirectoryInfo(Index.AdresarPredpisu);

            int counter = 0;
            int error = 0;

            foreach (DirectoryInfo rocnikDir in adresarPredpisu.EnumerateDirectories())
            {
                int r = int.Parse(rocnikDir.Name);
                if (r < minRok || r > maxRok)
                    continue;
                foreach (DirectoryInfo predpisDir in rocnikDir.EnumerateDirectories())
                {
                    int cislo = int.Parse(predpisDir.Name);
                    int rocnik = int.Parse(rocnikDir.Name);

                    Predpis predpis = new Predpis(cislo, rocnik);

                    if (!File.Exists(predpis.SouborText))
                        continue;

                    Text text = predpis.Text;

                    counter++;

                    Queue<Sekce.ISekce> fronta = new Queue<Sekce.ISekce>();

                    fronta.Enqueue(text.Obsah);

                    while (fronta.Count > 0)
                    {
                        Sekce.ISekce sekce = fronta.Dequeue();

                        int x = 0; // kolikrat se za sebou vyskytne Sekce.NADPIS

                        foreach (Sekce.ISekce sub in sekce.Subsekce)
                        {
                            if (sub.Typ == Sekce.NADPIS && sub.Subsekce.Count < 1) // prohledavame jenom "prazdne" nadpisy
                                x++;
                            else
                                x = 0;

                            if (x > 2)
                                throw new UZException("Test nadpisu: " + predpis.Oznaceni);

                            fronta.Enqueue(sub);
                        }
                    }

                }
            }
        }
    }

}
