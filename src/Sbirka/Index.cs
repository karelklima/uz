using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using UZ.PDF;
using UZ.Nastroje;

namespace UZ.Sbirka
{
    class Index
    {
        public const string NEZNAMY_PREDPIS = "Neznámý předpis";
        public const string LOCK = "lock.txt";

        private static Dictionary<string, Predpis> predpisy = new Dictionary<string,Predpis>();
        private static Dictionary<string, Castka> castky = new Dictionary<string,Castka>();

        public static string Vstup { get { return Nastaveni.PracovniAdresar + "/" + Nastaveni.Get("AdresarVstup"); } }

        public static string Adresar { get { return Nastaveni.PracovniAdresar + "/" + Nastaveni.Get("AdresarSbirka"); } }

        public static string AdresarCastek { get { return Adresar + "/castky"; } }

        public static string AdresarPredpisu { get { return Adresar + "/predpisy"; } }

        public static Castka ImportCastkyPdf(string filename)
        {
            FileInfo file = new FileInfo(filename);
            if (!file.Exists)
                throw new UZException("File not found: " + filename);

            Castka dokument = Podpora.ExtrahujCislaZNazvuPdf(filename);

            string novaSlozka = dokument.Adresar;
            string novyPdf = dokument.SouborPdf;

            if (!Directory.Exists(novaSlozka))
                Directory.CreateDirectory(novaSlozka);

            file.CopyTo(novyPdf, true);

            return dokument;
                
        }

        public static string GetId(int cislo, int rocnik)
        {
            // Preformatuje na 0001-2012
            return cislo.ToString(EncodingTools.NumberFormat).PadLeft(4, '0')
                        + '-' + rocnik.ToString(EncodingTools.NumberFormat).PadLeft(4, '0');
        }

        public static Predpis NajdiPredpis(int cislo, Castka castka)
        {
            string id = GetId(cislo, castka.Rocnik);
            if (predpisy.ContainsKey(id))
                return predpisy[id];

            foreach (Predpis predpis in castka.Predpisy)
            {
                if (predpis.Cislo == cislo)
                {
                    if (!predpisy.ContainsKey(id)) // predpis mohl byt nacten v ramci nacitani castky
                        predpisy.Add(id, predpis);
                    return predpis;
                }
            }

            throw new UZException(String.Format("Predpis {0} v castce {1}/{2} nebyl nalezen", cislo, castka.Cislo, castka.Rocnik));
        }

        public static Predpis NajdiPredpis(int cislo, int rocnik)
        {
            /*string id = GetId(cislo, rocnik);
            if (predpisy.ContainsKey(id))
                return predpisy[id];*/

            try
            {
                Predpis novy = new Predpis(cislo, rocnik);
                //predpisy.Add(id, novy);
                return novy;
            }
            catch { }

            return null; // exception
        }

        public static Castka NajdiCastku(int cislo, int rocnik)
        {
            string id = GetId(cislo, rocnik);
            if (castky.ContainsKey(id))
                return castky[id];

            try
            {
                Castka nova = new Castka(cislo, rocnik);
                castky.Add(id, nova);
                return nova;
            }
            catch { }

            return null; // exception
        }

        public static void PridejCastku(Castka castka)
        {
            if (!File.Exists(castka.SouborPdf))
                throw new UZException("Castka {0}/{1} nebyla naimportovana", castka.Cislo, castka.Rocnik);

            foreach (Predpis predpis in castka.Predpisy)
            {
                try
                {
                    PridejPredpis(predpis);
                }
                catch (UZException e)
                {
                    throw new UZException("Predpis {0}/{1} nelze zpracovat, castka {2}/{1}, duvod: {3}", predpis.Cislo, predpis.Rocnik, castka.Cislo, e.Message);
                }
            }
            
        }

        public static void OdeberCastku(Castka castka)
        {
            string id = GetId(castka.Cislo, castka.Rocnik);
            if (castky.ContainsKey(id))
                castky.Remove(id);
        }

        public static void PridejPredpis(Predpis predpis)
        {
            if (predpis.JeNeznamy)
                return; // nechceme indexovat nezname predpisy

            Predpis vCache = NajdiPredpis(predpis.Cislo, predpis.Rocnik);
            if (vCache != null)
                return;
            if (!File.Exists(predpis.SouborInfo))
                predpis.Uloz();

            AktualizujZmeny(predpis);

            string id = GetId(predpis.Cislo, predpis.Rocnik);
            //if (!predpisy.ContainsKey(id))
            //    predpisy.Add(id, predpis);

        }

        public static void AktualizujZmeny(Predpis novela)
        {

            foreach (string zmena in novela.Zmeny)
            {
                string[] casti = zmena.Split('/');
                int cislo = int.Parse(casti[0]);
                int rok = int.Parse(casti[1]);
                Predpis original = NajdiPredpis(cislo, rok);
                if (original != null)
                    original.PridejNovelu(novela);
            }
        }

        public static void VycistiIndex()
        {
            predpisy.Clear();
            castky.Clear();
        }

        public static void SmazCacheCastek()
        {
            DirectoryInfo info = new DirectoryInfo(AdresarCastek);
            foreach (DirectoryInfo rocnik in info.EnumerateDirectories())
            {
                foreach (DirectoryInfo castka in rocnik.EnumerateDirectories())
                {
                    if (File.Exists(castka.FullName + "\\" + LOCK)) // zamknuty adresar
                        continue;
                    foreach (FileInfo soubor in castka.EnumerateFiles())
                    {
                        if (!soubor.Extension.Equals(".pdf"))
                        {
                            soubor.Delete();
                        }
                    }
                }
            }
        }

        public static void SmazCacheCastekRocnik(int rocnik)
        {
            DirectoryInfo adr = new DirectoryInfo(AdresarCastek + "/" + rocnik.ToString());
            
                foreach (DirectoryInfo castka in adr.EnumerateDirectories())
                {
                    Castka c = new Castka(Int32.Parse(castka.Name), rocnik);
                    if (File.Exists(c.SouborZamek)) // zamknuty adresar
                    {
                        continue;
                    }
                    foreach (FileInfo soubor in castka.EnumerateFiles())
                    {
                        if (!soubor.Extension.Equals(".pdf"))
                        {
                            soubor.Delete();
                        }
                    }
                }
            
        }

        public static void SmazCachePredpisu()
        {
            DirectoryInfo info = new DirectoryInfo(AdresarPredpisu);
            foreach (DirectoryInfo rocnik in info.EnumerateDirectories())
            {
                rocnik.Delete(true);
            }
        }

        public static void ImportujVechnyCastkyPdf()
        {
            Log log = new Log("log_importpdf");
            DirectoryInfo vstup = new DirectoryInfo(Vstup);
            FileInfo[] soubory = vstup.GetFiles();

            DateTime start = DateTime.Now;

            log.Add("Import castek PDF");
            log.Add("========================================================");
            log.Add("Zacatek: {0}", start);
            log.Add("Pocet souboru ke zpracovani: {0}", soubory.Length);
            log.Add("========================================================");

            int uspech = 0;

            foreach (FileInfo soubor in soubory)
            {
                try
                {
                    Castka dokument = Podpora.ExtrahujCislaZNazvuPdf(soubor.Name);

                    string novaSlozka = dokument.Adresar;
                    string novyPdf = dokument.SouborPdf;

                    if (!Directory.Exists(novaSlozka))
                        Directory.CreateDirectory(novaSlozka);

                    string flag = "EXISTUJE";

                    if (!File.Exists(novyPdf))
                    {
                        flag = "DOPLNENO";
                        soubor.CopyTo(novyPdf, true);
                    }
                    else if (!Podpora.ShodaSouboru(novyPdf, soubor.FullName))
                    {
                        flag = "PREPSANO";
                        soubor.CopyTo(novyPdf, true);
                    }


                    log.Add("OK\t{0}\t{1}\tCastka {2}/{3}", flag, soubor.Name, dokument.Cislo, dokument.Rocnik);
                    uspech++;
                }
                catch (UZException e)
                {
                    log.Add("CHYBA\t{0}\t{1}", soubor.Name, e.Message);
                }
            }

            log.Add("========================================================");
            log.Add("Konec: {0}", DateTime.Now - start);
            log.Add("Pocet chyb: {0}", soubory.Length - uspech);

            log.Close();
            
        }

        public static void ZkontrolujUplnostCastek()
        {
            Log log = new Log("log_uplnostcastek");
            DateTime start = DateTime.Now;

            log.Add("Uplnost castek v katalogu");
            log.Add("========================================================");
            log.Add("Zacatek: {0}", start);
            log.Add("========================================================");

            for (int r = 1993; r <= start.Year; r++)
            {
                DirectoryInfo rocnik = new DirectoryInfo(Index.AdresarCastek + "/" + r.ToString());
                DirectoryInfo[] dirs = rocnik.GetDirectories();

                int pointer = 0;
                List<int> missing = new List<int>();

                foreach (DirectoryInfo dir in dirs)
                {
                    int dirNumber = Int32.Parse(dir.Name);

                    while (pointer + 1 < dirNumber)
                    {
                        pointer++;
                        missing.Add(pointer);
                    }

                    pointer++;
                }

                if (missing.Count < 1)
                    log.Add("Rocnik: {0}\tOK\tPocet castek: {1}", r, dirs.Length);
                else
                {
                    List<string> cisla = missing.ConvertAll<string>(x => x.ToString());
                    log.Add("Rocnik: {0}\tCHYBA\tPocet castek: {1}\tChybi: {2}", r, dirs.Length, String.Join(", ", cisla));
                }
            }

            log.Add("========================================================");
            log.Add("Konec: {0}", DateTime.Now - start);

            log.Close();

        }

        public static void IndexujVsechnyPredpisy(int minRocnik = 1990, int maxRocnik = 2020)
        {

            Log log = new Log("log_indexpredpisu");
            DateTime start = DateTime.Now;

            log.Add("Index predpisu");
            log.Add("========================================================");
            log.Add("Zacatek: {0}", start);
            


            List<Castka> kIndexaci = new List<Castka>();
            DirectoryInfo adresarCastek = new DirectoryInfo(Index.AdresarCastek);

            foreach (DirectoryInfo rocnikDir in adresarCastek.EnumerateDirectories())
            {
                foreach (DirectoryInfo castkaDir in rocnikDir.EnumerateDirectories())
                {
                    int cislo = int.Parse(castkaDir.Name);
                    int rocnik = int.Parse(rocnikDir.Name);
                    if (rocnik < minRocnik || rocnik > maxRocnik)
                        continue;
                    Castka castka = new Castka(cislo, rocnik);
                    if (File.Exists(castka.SouborInfo))
                        kIndexaci.Add(castka);
                }
            }

            log.Add("Pocet castek k zaindexovani: {0}", kIndexaci.Count);
            log.Add("========================================================");

            kIndexaci.Sort(delegate(Castka c1, Castka c2)
            {
                if (c1.Rocnik == c2.Rocnik)
                    return c1.Cislo.CompareTo(c2.Cislo);
                else
                    return c1.Rocnik.CompareTo(c2.Rocnik);
            });

            foreach (Castka castka in kIndexaci)
            {
                //if (castka.Cislo == 32 && castka.Rocnik == 1998) // ???
                //    continue;
                try
                {
                    Index.PridejCastku(castka);
                }
                catch (UZException e)
                {
                    log.Add(e.Message);
                }
            }

            log.Add("========================================================");
            log.Add("Konec: {0}", DateTime.Now - start);

            log.Close();

        }

        public static void VygenerujUplnaZneni(int minRocnik = 1990, int maxRocnik = 2020)
        {
            Log log = new Log("log_uplnazneni");
            DateTime start = DateTime.Now;

            log.Add("Generovani uplnych zneni");
            log.Add("========================================================");
            log.Add("Zacatek: {0}", start);

            log.Add("========================================================");


            for (int i = minRocnik; i <= maxRocnik; i++)
            {
                Index.predpisy.Clear();
                Index.castky.Clear();
                Generator.ZpracujRocnik(i, log);
                log.Add("-------------------------------------------------------");
            }

            log.Add("========================================================");
            log.Add("Konec: {0}", DateTime.Now - start);

            log.Close();

        }

        public static void ParsujObsahCastekZPdf(int minRok, int maxRok, bool preskocParsovane)
        {
            DirectoryInfo adresarCastek = new DirectoryInfo(Index.AdresarCastek);

            foreach (DirectoryInfo rocnikDir in adresarCastek.EnumerateDirectories())
            {
                int r = int.Parse(rocnikDir.Name);
                if (r < minRok || r > maxRok)
                    continue;
                foreach (DirectoryInfo castkaDir in rocnikDir.EnumerateDirectories())
                {
                    int cislo = int.Parse(castkaDir.Name);
                    int rocnik = int.Parse(rocnikDir.Name);
                    Castka castka = new Castka(cislo, rocnik);

                    if (File.Exists(castka.SouborZamek))
                        continue;

                    if (preskocParsovane && File.Exists(castka.SouborText) && File.Exists(castka.SouborInfo))
                        continue;

                    if (!File.Exists(castka.SouborInfo) && File.Exists(castka.SouborText))
                        File.Delete(castka.SouborText); // pravdepodobne spatny read PDF

                    try
                    {
                        List<Predpis> predpisy = castka.Predpisy; // spusti se kaskada nacteni textu a obsahu
                    }
                    catch (UZException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }

        public static void InterpretujPredpisyZObsahuCastek(int minRok, int maxRok, bool preskocParsovane = true)
        {
            DirectoryInfo adresarPredpisu = new DirectoryInfo(Index.AdresarPredpisu);

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

                    if (preskocParsovane && File.Exists(predpis.SouborText))
                        continue;

                    if (File.Exists(predpis.SouborText))
                        File.Delete(predpis.SouborText);

                    Console.WriteLine(predpis.Oznaceni);

                    Text text = predpis.Text;

                    Console.WriteLine("Ok");
                    /*try
                    {
                        List<Predpis> predpisy = castka.Predpisy; // spusti se kaskada nacteni textu a obsahu
                    }
                    catch (UZException e)
                    {
                        Console.WriteLine(e.Message);
                    }*/
                }
            }
        }
        
    }
}
