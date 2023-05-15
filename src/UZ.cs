using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UZ.PDF;
using UZ.PDF.Objects;
using System.IO;
using System.IO.Compression;
using UZ.PDF.Font;
using UZ.Sbirka;
using System.Configuration;
using System.Net;
using System.Xml;
using System.Text.RegularExpressions;
using System.Threading;
using UZ.Nastroje;


namespace UZ
{
    class UZ
    {


        public abstract class IAkce
        {
            private Regex regex;
            public IAkce()
            {
                string[] chunks = GetPattern().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                this.regex = new Regex("^\\s*(" + String.Join(")\\s+(", chunks) + ")\\s*$", RegexOptions.IgnoreCase);
            }
            public bool TryProcess(string commandLine)
            {
                Match match = this.regex.Match(commandLine);

                if (!match.Success)
                    return false;

                List<string> cmds = new List<string>();
                for (int i = 1; i < match.Groups.Count; i++) // preskocit cely match
                    cmds.Add(match.Groups[i].Value);

                Invoke(cmds);
                return true;
            }
            public abstract string GetPattern();
            public abstract void Invoke(List<string> commands);
            public abstract void WriteHelp();
        }

        class AkceAktualizace : IAkce
        {
            public override string GetPattern()
            {
                return "aktualizace [1-2][0-9]{3}";
            }

            public override void Invoke(List<string> commands)
            {
                int maxRocnik = 0;

                if (commands.Count > 1)
                    maxRocnik = int.Parse(commands[1]);

                Aktualizace aktualizace = new Aktualizace();
                aktualizace.Spustit(false, maxRocnik);
            }

            public override void WriteHelp()
            {
                Console.WriteLine("aktualizace %ROK%");
                Console.WriteLine("--- Zahájí aktualizaci Sbírky zákonů s omezením mezi lety 1993 a %ROK%");
                Console.WriteLine("--- Částky budou staženy a naimportovány do adresáře Sbirka");
            }
        }

        class AkceNastavPracovniAdresar : IAkce
        {
            public override string GetPattern()
            {
                return "nastavAdresar|nastavPracovniAdresar [a-zA-Z0-9:.\\\\/]+";
            }

            public override void Invoke(List<string> commands)
            {
                DirectoryInfo info = new DirectoryInfo(commands[1]);

                if (!info.Exists)
                {
                    bool odpoved = AnoNeDotaz("Adresář {0} neexistuje. Přejete si jej vytvořit?", info.FullName);
                    if (!odpoved)
                    {
                        Console.WriteLine("Operace byla zrušena.");
                        return;
                    }
                    info.Create();
                    Console.WriteLine("Adresář byl vytvořen");
                }

                Nastaveni.PracovniAdresar = info;

                List<string> subfolders = new List<string>();
                subfolders.Add(Nastaveni.Get("AdresarVstup"));
                subfolders.Add(Nastaveni.Get("AdresarSbirka"));
                subfolders.Add(Nastaveni.Get("AdresarVystup"));

                foreach (string subfolder in subfolders)
                {
                    DirectoryInfo subdirInfo = new DirectoryInfo(info.FullName + "/" + subfolder);
                    if (!subdirInfo.Exists)
                        subdirInfo.Create();
                }

                Console.WriteLine("Pracovní adresář: " + info.FullName);

            }

            public override void WriteHelp()
            {
                Console.WriteLine("nastavAdresar %ADRESÁŘ%");
                Console.WriteLine("--- Nastaví pracovní adresář, v případě potřeby jej vytvoří spolu se strukturou podadresářů");
                Console.WriteLine("--- Upozornění: cesta nesmí obsahovat mezery");
            }
        }

        class AkcePridejVstup : IAkce
        {

            public override string GetPattern()
            {
                return "pridejVstup";
            }

            public override void Invoke(List<string> commands)
            {
                DirectoryInfo info = new DirectoryInfo(Nastaveni.PracovniAdresar.FullName + "/" + Nastaveni.Get("AdresarVstup"));

                List<FileInfo> pdfFiles = new List<FileInfo>();
                foreach (FileInfo pdfFile in info.EnumerateFiles())
                {
                    if (String.Compare(pdfFile.Extension, ".pdf", StringComparison.InvariantCultureIgnoreCase) == 0)
                        pdfFiles.Add(pdfFile);
                }

                if (pdfFiles.Count < 1)
                {
                    Console.WriteLine("Adresář vstupu neobsahuje žádné PDF soubory k nahrání");
                    return;
                }

                Console.WriteLine("Následující soubory budou přídány:");
                foreach (FileInfo pdfFile in pdfFiles)
                    Console.WriteLine(pdfFile.Name);

                if (!AnoNeDotaz("Pokračovat?"))
                {
                    Console.WriteLine("Operace byla zrušena.");
                    return;
                }

                //Index index = new Index();
                foreach (FileInfo pdfFile in pdfFiles)
                {
                    Castka castka = Index.ImportCastkyPdf(pdfFile.FullName);
                    Console.WriteLine("{0} naimportována", castka.Oznaceni);
                }

            }

            public override void WriteHelp()
            {
                Console.WriteLine("pridejVstup");
                Console.WriteLine("--- Vezme všechny PDF soubory z adresáře Vstup a začlení je systematicky do adresáře Sbirka");
            }
        }

        class AkceIndexujCastku : IAkce
        {

            public override string GetPattern()
            {
                return "indexujCastku [0-9]+\\/[0-9]+";
            }

            public override void Invoke(List<string> commands)
            {
                string[] cisla = commands[1].Split('/');
                int cislo = int.Parse(cisla[0]);
                int rocnik = int.Parse(cisla[1]);

                Castka castka = new Castka(cislo, rocnik);
                if (!File.Exists(castka.SouborPdf))
                {
                    Console.WriteLine(castka.Oznaceni + " neexistuje nebo zatím nebyla naimportována");
                    return;
                }

                try
                {
                    Index.PridejCastku(castka);
                    Console.WriteLine(castka.Oznaceni + " byla přidána do indexu");
                }
                catch (UZException e)
                {
                    Console.WriteLine(e.Message);
                }

                
                
            }

            public override void WriteHelp()
            {
                Console.WriteLine("indexujCastku %ČÍSLO%/%ROČNÍK%");
                Console.WriteLine("--- Vezme PDF přidané ze vstupu a vytvoří k nim index, tzn. zjistí, co je obsahem daných částek");
                Console.WriteLine("--- Upozornění: Částky musí být indexovány ve správném pořadí (na základě data rozeslání)");
            }
        }

        class AkceIndexujVse : IAkce
        {

            public override string GetPattern()
            {
                return "indexujVse";
            }

            public override void Invoke(List<string> commands)
            {
                List<Castka> kIndexaci = new List<Castka>();
                DirectoryInfo adresarCastek = new DirectoryInfo(Index.AdresarCastek);

                if (!adresarCastek.Exists)
                {
                    Console.WriteLine("Index je aktuální");
                    return;
                }

                foreach (DirectoryInfo rocnikDir in adresarCastek.EnumerateDirectories())
                {
                    foreach (DirectoryInfo castkaDir in rocnikDir.EnumerateDirectories())
                    {
                        int cislo = int.Parse(castkaDir.Name);
                        int rocnik = int.Parse(rocnikDir.Name);
                        Castka castka = new Castka(cislo, rocnik);
                        if (File.Exists(castka.SouborPdf) && !File.Exists(castka.SouborInfo))
                            kIndexaci.Add(castka);
                    }
                }

                if (kIndexaci.Count < 1)
                {
                    Console.WriteLine("Index je aktuální");
                    return;
                }

                Console.WriteLine("{0} položek je připraveno k indexaci", kIndexaci.Count);

                if (!AnoNeDotaz("Pokračovat?"))
                {
                    Console.WriteLine("Operace byla zrušena.");
                    return;
                }

                kIndexaci.Sort(delegate(Castka c1, Castka c2) {
                    if (c1.Rocnik == c2.Rocnik)
                        return c1.Cislo.CompareTo(c2.Cislo);
                    else
                        return c1.Rocnik.CompareTo(c2.Rocnik);
                });

                foreach (Castka castka in kIndexaci)
                {
                    if (castka.Cislo == 32 && castka.Rocnik == 1998)
                        continue;
                    try
                    {
                        Index.PridejCastku(castka);
                    }
                    catch (UZException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

                Console.WriteLine("Částky byly přidány do indexu");
                    
            }

            public override void WriteHelp()
            {
                Console.WriteLine("indexujVse");
                Console.WriteLine("--- Vytvoří index ke všem přidaným částkám v adresáří Sbirka, které dosud nebyly zaindexovány");
            }
        }

        class AkceZobrazCastku : IAkce
        {

            public override string GetPattern()
            {
                return "zobrazCastku [0-9]+\\/[0-9]+";
            }

            public override void Invoke(List<string> commands)
            {
                Console.WriteLine();
                int cislo, rocnik;
                Cisla(commands[1], out cislo, out rocnik);
                Castka castka = Index.NajdiCastku(cislo, rocnik);
                if (castka == null || !File.Exists(castka.SouborInfo))
                {
                    Console.WriteLine("Částka č. " + commands[1] + " neexistuje nebo nebyla zaindexována");
                    return;
                }
                Console.WriteLine("Sbírka zákonů České republiky");
                Console.WriteLine(castka.Oznaceni);
                Console.WriteLine("Rozeslána dne " + castka.DatumRozeslani);
                Console.WriteLine();
                Console.WriteLine("Obsah");
                Console.WriteLine("=====");

                foreach (Predpis predpis in castka.Predpisy)
                {
                    Console.WriteLine("{0}. {1}", predpis.Cislo, NormalizujMezery(predpis.Nazev));
                }
            }

            public override void WriteHelp()
            {
                Console.WriteLine("zobrazCastku %ČÍSLO%/%ROČNÍK%");
                Console.WriteLine("--- Zobrazí informace o dané částce a její obsah (seznam předpisů)");
            }
        }

        class AkceZobrazPredpis : IAkce
        {

            public override string GetPattern()
            {
                return "zobrazPredpis [0-9]+\\/[0-9]+";
            }

            public override void Invoke(List<string> commands)
            {
                Console.WriteLine();
                int cislo, rocnik;
                Cisla(commands[1], out cislo, out rocnik);
                Predpis predpis = Index.NajdiPredpis(cislo, rocnik);
                if (predpis == null)
                {
                    Console.WriteLine("Předpis č. " + commands[1] + " neexistuje nebo nebyl zaindexován");
                    return;
                }
                Console.WriteLine(predpis.Oznaceni);
                Console.WriteLine("Název: " + NormalizujMezery(predpis.Nazev));
                Console.WriteLine("Částka: {0}/{1}", predpis.Castka.Cislo, predpis.Castka.Rocnik);
                Console.Write("Novely: ");

                if (predpis.Novely.Count > 0)
                {
                    List<string> novely = new List<string>();
                    foreach (Predpis.Novela novela in predpis.Novely)
                        novely.Add(novela.Cislo + "/" + novela.Castka.Rocnik);
                    Console.WriteLine(String.Join(", ", novely));
                }
                else
                {
                    Console.WriteLine("žádné");
                }

                Console.Write("Změny předpisů: ");

                if (predpis.Zmeny.Count > 0)
                {
                    Console.WriteLine(String.Join(", ", predpis.Zmeny));
                }
                else
                {
                    Console.WriteLine("žádné");
                }

                
            }

            public override void WriteHelp()
            {
                Console.WriteLine("zobrazPredpis %ČÍSLO%/%ROČNÍK%");
                Console.WriteLine("--- Zobrazí informace o daném předpisu, seznam jeho novel a seznam změn");
            }
        }

        class AkceVypisPredpis : IAkce
        {

            public override string GetPattern()
            {
                return "vypisPredpis text|xml [0-9]+\\/[0-9]+";
            }

            public override void Invoke(List<string> commands)
            {
                Console.WriteLine();
                string rezim = commands[1];
                int cislo, rocnik;
                Cisla(commands[2], out cislo, out rocnik);
                Predpis predpis = Index.NajdiPredpis(cislo, rocnik);
                if (predpis == null)
                {
                    Console.WriteLine("Předpis č. " + commands[2] + " neexistuje nebo nebyl zaindexován");
                    return;
                }
                Console.WriteLine(predpis.Oznaceni + " " + NormalizujMezery(predpis.Nazev));

                int zneni = 0;
                List<Predpis.Novela> novely = predpis.Novely.ToList();


                if (predpis.Novely.Count > 0)
                {
                    Console.WriteLine("Předpis byl novelizován. Vyberte, jaká varianta se má zpracovat:");
                    Console.WriteLine("(0)  Původní znění");
                    for (int i = 1; i <= novely.Count; i++)
                    {
                        string poznamka = "";
                        if (i == novely.Count)
                            poznamka = "(úplné znění)";
                        Console.WriteLine("({0})  Novela {1}/{2} {3}", i, novely[i - 1].Cislo, novely[i - 1].Castka.Rocnik, poznamka);
                    }
                    Console.WriteLine("Zadejte číslo varianty:");

                    string line = Console.ReadLine();
                    int vstup = 0;
                    if (!int.TryParse(line, out vstup) || vstup > novely.Count)
                    {
                        Console.WriteLine("Nevalidní vstup.");
                        return;
                    }
                    zneni = vstup;

                }

                string adresarVystup = Nastaveni.PracovniAdresar + "\\" + Nastaveni.Get("AdresarVystup") + "\\";
                Text text = (zneni == 0) ? predpis.Text : novely[zneni - 1].UplneZneni;
                string soubor = (zneni == 0) ? predpis.Soubor : novely[zneni - 1].Soubor;


                if (rezim == "xml")
                {
                    string souborXml = adresarVystup + soubor + ".xml";
                    text.ToXmlFile(souborXml);
                    Console.WriteLine("Předpis byl vypsán do souboru " + souborXml);
                }
                else if (rezim == "text")
                {
                    string souborText = adresarVystup + soubor + ".txt";
                    Sazec.VysazejTextDoSouboru(souborText, text);
                    Console.WriteLine("Předpis byl vysázen do souboru " + souborText);
                }
               
            }

            public override void WriteHelp()
            {
                Console.WriteLine("vypisPredpis text %ČÍSLO%/%ROČNÍK%");
                Console.WriteLine("--- Vysází kompletní text předpisu do souboru v adresáři Vystup");
                Console.WriteLine();
                Console.WriteLine("vypisPredpis xml %ČÍSLO%/%ROČNÍK%");
                Console.WriteLine("--- Vypíše zdrojový kód předpisu ve formátu XML do souboru v adresáři Vystup");
                
            }
        }

        class AkceSmazIndex : IAkce
        {
            public override string GetPattern()
            {
                return "smazIndex";
            }

            public override void Invoke(List<string> commands)
            {
                try
                {
                    Index.SmazCacheCastek();
                    Index.SmazCachePredpisu();
                }
                catch (IOException e)
                {
                    Console.WriteLine("Nepodarilo se smazat cache, pristup odepren");
                }
            }

            public override void WriteHelp()
            {
                Console.WriteLine("smazIndex");
                Console.WriteLine("--- Smaže celý index, kromě stažených souborů PDF");
            }
        }

        class AkceSmazNepotrebneCastky : IAkce
        {
            public override string GetPattern()
            {
                return "smazNepotrebneCastky";
            }

            public override void Invoke(List<string> commands)
            {

                if (!AnoNeDotaz("Částky budou nevratně smazány. Pokračovat?"))
                {
                    Console.WriteLine("Operace byla zrušena.");
                    return;
                }

                int smazano = 0;
                
                DirectoryInfo adresar = new DirectoryInfo(Index.AdresarCastek);
                DirectoryInfo[] rocniky = adresar.GetDirectories();
                
                for (int i = 0; i < rocniky.Length; i++)
                {
                    DirectoryInfo rocnik = rocniky[i];
                    DirectoryInfo[] castky = rocnik.GetDirectories();
                    for (int j = 0; j < castky.Length; j++)
                    {
                        DirectoryInfo castka = castky[j];
                        int rocnikCislo = Int32.Parse(rocnik.Name);
                        int castkaCislo = Int32.Parse(castka.Name);

                        try
                        {

                            if (castka.GetFiles().Length < 1)
                            {
                                castka.Delete(true);
                                Console.WriteLine("Částka {0}/{1} byla smazána", castkaCislo, rocnikCislo);
                                continue;
                            }

                            try
                            {
                                Castka c = Index.NajdiCastku(castkaCislo, rocnikCislo);
                                bool zakon = false;
                                foreach (Predpis p in c.Predpisy)
                                {
                                    if (!p.JeNeznamy)
                                    {
                                        zakon = true;
                                        break;
                                    }
                                }
                                if (!zakon)
                                {
                                    castka.Delete(true);
                                    smazano++;
                                    Console.WriteLine("Částka {0}/{1} byla smazána", castkaCislo, rocnikCislo);
                                }
                            }
                            catch (UZException e)
                            {
                                castka.Delete(true);
                                smazano++;
                                Console.WriteLine("Částka {0}/{1} byla smazána", castkaCislo, rocnikCislo);
                            }
                        }
                        catch (IOException ioe)
                        {
                            Console.WriteLine("Částku {0}/{1} se nepodařilo smazat, přístup zamítnut", castkaCislo, rocnikCislo);
                        }
                    }
                }

                Console.WriteLine("{0} částek celkem bylo smazáno", smazano);

                

                
            }

            public override void WriteHelp()
            {
                Console.WriteLine("smazNepotrebneCastky");
                Console.WriteLine("--- Odstraní z indexu i ze souborového systému všechny částky, které neobsahují zákony");
            }
        }

        class AkceExport : IAkce
        {
            public override string GetPattern()
            {
                return "export";
            }
            public override void Invoke(List<string> commands)
            {
                DirectoryInfo export = new DirectoryInfo(Nastaveni.PracovniAdresar.FullName + "\\Export");

                Console.Write("Export do složky: ");
                Console.Write(export);
                Console.WriteLine();
                if (AnoNeDotaz("Pokračovat?"))
                {
                    Console.WriteLine("Exportuji data");
                    Nastroje.Export.DoSlozky(export);
                    Console.WriteLine("Export dokončen");
                }
                else
                    Console.WriteLine("Export zrušen");
            }
            public override void WriteHelp()
            {
                Console.WriteLine("export");
                Console.WriteLine("--- Exportuje vygenerovaná znění do externího adresáře.");
            }
        }

        class AkceDiagnostikaCastek : IAkce
        {
            public override string GetPattern()
            {
                return "diagnostikaCastek";
            }
            public override void Invoke(List<string> commands)
            {
                Nastroje.DiagnostikaCastek.Spustit(false);
            }
            public override void WriteHelp()
            {
                Console.WriteLine("diagnostikaCastek");
                Console.WriteLine("--- Vygeneruje zpravu o poctu zamcenych a chybne zpracovanych castek.");
            }
        }

        class AkceDebug : IAkce
        {
            public override string GetPattern()
            {
                return "debug";
            }
            public override void Invoke(List<string> commands)
            {
                Debug();
            }
            public override void WriteHelp()
            {
                Console.WriteLine("debug");
                Console.WriteLine("--- Spusti skript v metode Debug()");
            }
        }
        
        public static void Cisla(string vstup, out int cislo, out int rocnik)
        {
            string[] cisla = vstup.Split('/');
            cislo = int.Parse(cisla[0]);
            rocnik = int.Parse(cisla[1]);
        }

        public static bool AnoNeDotaz(string dotaz, params object[] parametry)
        {
            Console.WriteLine(dotaz + " (ano/ne)", parametry);
            string odpoved = Console.ReadLine();
            return String.Compare(odpoved, "ano", true) == 0 ? true : false;
        }

        public static string NormalizujMezery(string vstup)
        {
            return Regex.Replace(vstup, @"\s+", " ");
        }

       

        static void UI()
        {

            List<IAkce> akce = new List<IAkce>();

            akce.Add(new AkceAktualizace());
            akce.Add(new AkceNastavPracovniAdresar());
            akce.Add(new AkcePridejVstup());
            akce.Add(new AkceIndexujCastku());
            akce.Add(new AkceIndexujVse());
            akce.Add(new AkceZobrazCastku());
            akce.Add(new AkceZobrazPredpis());
            akce.Add(new AkceVypisPredpis());
            akce.Add(new AkceSmazIndex());
            akce.Add(new AkceSmazNepotrebneCastky());
            akce.Add(new AkceExport());
            akce.Add(new AkceDiagnostikaCastek());
            akce.Add(new AkceDebug());
            
            


            Console.WriteLine();
            Console.WriteLine("Úplné znění zákonů");
            Console.WriteLine("==================");
            Console.WriteLine();
            Console.WriteLine("Pro zobrazení návodu zadejte příkaz \"pomoc\"");
            Console.WriteLine("Pro ukončení programu napište \"konec\"");
            Console.WriteLine();
            Console.WriteLine("Pracovní adresář: " + Nastaveni.PracovniAdresar.FullName);
            //Console.WriteLine("Adresář sbírky: " + Nastaveni.Get("AdresarSbirka"));
            //Console.WriteLine("Adresář stahování: " + Nastaveni.Get("AdresarVstup"));
            //Console.WriteLine("Adresář výstupu: " + Nastaveni.Get("AdresarVystup"));

            List<string> subfolders = new List<string>();
            subfolders.Add(Nastaveni.Get("AdresarVstup"));
            subfolders.Add(Nastaveni.Get("AdresarSbirka"));
            subfolders.Add(Nastaveni.Get("AdresarVystup"));

            foreach (string subfolder in subfolders)
            {
                DirectoryInfo subdirInfo = new DirectoryInfo(Nastaveni.PracovniAdresar.FullName + "/" + subfolder);
                if (!subdirInfo.Exists)
                    subdirInfo.Create();
            }

            try
            {
                bool konec = false;
                while (true)
                {
                    Console.WriteLine();
                    Console.WriteLine("Zadejte příkaz:");
                    string line = Console.ReadLine();
                    List<string> commands = new List<string>(line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries));
                    if (commands.Count > 0)
                    {
                        string c = commands[0];
                        if (c == "konec")
                            konec = true;
                        else if (c == "pomoc")
                        {
                            Console.WriteLine();
                            Console.WriteLine("Nápověda k programu");
                            Console.WriteLine();
                            foreach (IAkce a in akce)
                            {
                                a.WriteHelp();
                                Console.WriteLine();
                            }
                        }
                        else
                        {
                            bool nalezeno = false;
                            foreach (IAkce a in akce)
                            {
                                if (a.TryProcess(line))
                                {
                                    nalezeno = true;
                                    break;
                                }
                            }
                            if (!nalezeno)
                                Console.WriteLine("Příkaz nerozpoznán. Pro zobrazení nápovědy zadejte příkaz \"pomoc\"");
                        }
                    }

                    if (konec)
                        break;

                }
            }
            catch (PdfException e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Konec programu.");

        }

        static void Main(string[] args)
        {

            Nastaveni.PracovniAdresar = new DirectoryInfo("C:\\Users\\Karel\\Documents\\Visual Studio 2012\\Projects\\UZ\\Sbirka");

            UI();
        }

        static void Dump(string text, string fileName)
        {
            StreamWriter writer = new StreamWriter(fileName);
            writer.Write(text);
            writer.Close();
        }

        
    }
}
