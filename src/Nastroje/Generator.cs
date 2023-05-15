using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UZ.Sbirka;

namespace UZ.Nastroje
{
    class Generator
    {

        private Log log;

        public static void ZpracujRocnik(int rocnik, Log extLog)
        {
            new Generator(rocnik, extLog);
        }

        private void Log(int cislo, int rocnik, string message)
        {
            Log(rocnik.ToString() + "/" + cislo.ToString() + "\t" + message);
        }

        private void Log(string message)
        {
            log.Add(message);
        }

        private Generator(int rocnik, Log extLog)
        {
            this.log = extLog;
            Log("Rocnik: " + rocnik.ToString());
            List<Predpis> predpisy = NajdiPredpisy(rocnik);
            Log("Pocet predpisu: " + predpisy.Count.ToString());

            int counter = 0;
            
            foreach (Predpis predpis in predpisy)
            {
                if (!File.Exists(predpis.SouborText))
                    continue;
                try
                {
                    Index.VycistiIndex();

                    Text t = predpis.UplneZneni;

                    /*foreach (string zmena in predpis.Zmeny)
                    {
                        string[] casti = zmena.Split('/');
                        int cislo = int.Parse(casti[0]);
                        int rok = int.Parse(casti[1]);
                        Predpis original = Index.NajdiPredpis(cislo, rok);
                        if (original != null)
                        {
                            if (File.Exists(original.SouborText))
                            {
                                //Thread.Sleep(1000);
                                Index.VycistiIndex();
                                counter++;
                                original.PridejNovelu(predpis);
                                Text text = original.UplneZneni;
                            }

                        }
                    }*/
                }
                catch (UZException e)
                {
                    Log(e.Message);
                }
            }

            Log("Pocet novelizovanych predpisu: " + counter.ToString());

            
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

                    Predpis p = Index.NajdiPredpis(cislo, rocnik);
                    if (p == null)
                        throw new UZException("Nullovy predpis");
                    vystup.Add(p);
                }
                catch (UZException e)
                {
                    //statistika.PocetVadnychPredpisu++;
                    Log(cislo, rocnik, e.Message);
                }
            }

            return vystup;
        }



        
    }
}
