using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace UZ.Sbirka
{
    class Aktualizace
    {
        private bool interaction = true;
        private bool spusteno = true;
        private CancellationTokenSource cts;
        private Task task;

        private bool nestahovatZnovu = true;

        private Stack<Uri> souboryKeStazeni = new Stack<Uri>();
        private int stav = 0;
        private int rocnik = 0;
        private int stranka = 1;
        private Queue<Uri> souboryNaStrance = new Queue<Uri>();

        private Regex vyjimky = new Regex("&id=(5589|6398)$");

        private Dictionary<Uri, Castka> mapaCastka = new Dictionary<Uri, Castka>();
        private Dictionary<Uri, string> mapaSoubor = new Dictionary<Uri, string>();

        private WebClient client = new WebClient();

        private List<string> log = new List<string>();


        public Castka PosledniCastka
        {
            get
            {
                int rok = Nastaveni.GetInt("PosledniCastkaRocnik");
                if (rok < 1990)
                    rok = 1990;
                return new Castka(Nastaveni.GetInt("PosledniCastkaCislo"), rok);
            }
        }

        public string AdresarStahovani
        {
            get
            {
                return Nastaveni.PracovniAdresar.FullName + "/" + Nastaveni.Get("AdresarVstup");
            }
        }

        private void Dump(string message, params object[] args)
        {
            Console.WriteLine(String.Format(message, args));
        }

        public void Spustit(bool userInteraction = true, int maxRocnik = 0)
        {
            this.interaction = userInteraction;
            if (interaction)
            {
                Console.WriteLine("Spustit aktualizaci? a/n");
                if (Console.ReadKey(true).KeyChar != 'a')
                {
                    Console.WriteLine("Aktualizace zrusena");
                    return;
                }
            }

            stav = 0;
            souboryKeStazeni.Clear();
            souboryNaStrance.Clear();
            mapaCastka.Clear();
            stranka = 1;
            rocnik = DateTime.Now.Year;
            if (maxRocnik > 0)
                rocnik = maxRocnik;

            Console.WriteLine("Aktualizace spustena");
            cts = new CancellationTokenSource();
            task = Task.Factory.StartNew(() => Smycka(cts.Token), cts.Token);

            task.Wait();


            /*if (Console.ReadKey(true).KeyChar == 'q')
            {
                Console.WriteLine("Aktualizace prerusena uzivatelem");
                cts.Cancel();
                task.Wait();
            }*/


            //Console.WriteLine("Aktualizace ukoncena");
            //Console.WriteLine("Log:");
            //foreach (string message in log)
            //{
            //    Console.WriteLine(message);
            //}
        }

        private void Smycka(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                switch (stav)
                {
                    case 0:
                        NactiStranku(rocnik, stranka);
                        if (souboryNaStrance.Count < 1) // skok na download PDF
                        {
                            if (PosledniCastka.Rocnik < rocnik)
                            {
                                rocnik--;
                                stranka = 1;
                                break;
                            }
                            else
                            {
                                stav = 2;
                                break;
                            }
                        }
                        stav = 1;
                        break;
                    case 1:
                        if (souboryNaStrance.Count < 1) // skok na dalsi stranku
                        {
                            stranka++;
                            stav = 0;
                            break;
                        }
                        Uri soubor = souboryNaStrance.Dequeue();
                        if (!vyjimky.IsMatch(soubor.ToString()))
                        {
                            Castka castka = NactiSoubor(soubor);
                            if (castka != null)
                            {
                                if (castka.Rocnik > PosledniCastka.Rocnik || (castka.Rocnik == PosledniCastka.Rocnik && castka.Cislo > PosledniCastka.Cislo))
                                    souboryKeStazeni.Push(soubor);
                                else
                                    stav = 2;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Soubor bude preskocen: {0}", soubor.ToString());
                        }
                        break;
                    case 2:
                        if (souboryKeStazeni.Count < 1)
                        {
                            Dump("Databaze je aktualni");
                            stav = 4;
                            break;
                        }
                        Console.WriteLine("Celkem ke stazeni {0} souboru", souboryKeStazeni.Count);
                        if (!interaction)
                        {
                            stav = 3;
                            Dump("Zahajuji stahovani");
                            break;
                        }
                        Console.WriteLine("Stahnout soubory? a/n");
                        if (Console.ReadKey(true).KeyChar == 'a')
                        {
                            stav = 3;
                            Dump("Zahajuji stahovani");
                        }
                        else
                        {
                            Console.WriteLine("Aktualizace zrusena");
                            spusteno = false;
                            cts.Cancel();
                        }
                        break;
                    case 3: // stahovani nalezenych souboru
                        if (souboryKeStazeni.Count < 1)
                        {
                            stav = 4;
                            Dump("Konec stahovani");
                            break;
                        }
                        Uri souborUri = souboryKeStazeni.Pop();
                        string pdfSoubor = StahniSoubor(souborUri);
                        try
                        {
                            //Index index = new Index();
                            Castka novaCastka = Index.ImportCastkyPdf(pdfSoubor);
                            Dump("Castka {0}/{1} naimportovana", novaCastka.Cislo, novaCastka.Rocnik);
                            Nastaveni.SetInt("PosledniCastkaRocnik", novaCastka.Rocnik);
                            Nastaveni.SetInt("PosledniCastkaCislo", novaCastka.Cislo);
                            Nastaveni.Ulozit();
                        }
                        catch (Exception e)
                        {
                            string toLog = "X: " + pdfSoubor + " " + e.Message;
                            Dump(toLog);
                            log.Add(toLog);
                            //throw new Exception("Problem");
                        }

                        break;
                    case 4:

                        spusteno = false;
                        cts.Cancel();
                        //Console.WriteLine("Zmackni enter pro zobrazeni logu");
                        return; // complete task

                        break;

                }
            }
        }

        private void NactiStranku(int rocnik, int stranka)
        {
            Dump(String.Format("Nacitam stranku: {0}/{1}", stranka, rocnik));
            string strankaURL = String.Format(Nastaveni.Get("MVCRSbirkaURL"), rocnik, stranka);
            Dump(strankaURL);

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(strankaURL);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Dump("Response: {0}", response.StatusDescription);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                StreamReader streamReader = new StreamReader(response.GetResponseStream());
                string pageContent = streamReader.ReadToEnd();
                StringReader reader = new StringReader(pageContent);

                int konec = 0;
                int zacatek;
                while ((zacatek = pageContent.IndexOf("<a", konec)) > 0)
                {
                    if (!Char.IsWhiteSpace(pageContent, zacatek + 2)) // neni to hyperlink
                    {
                        konec = zacatek + 1;
                        continue;
                    }
                    konec = pageContent.IndexOf('>', zacatek);
                    if (konec < 0)
                        break;
                    string hyperlink = pageContent.Substring(zacatek, (konec + 1) - zacatek);
                    int hrefBegin = hyperlink.IndexOf("href=\"") + 6;
                    if (hrefBegin < 6) // preskocit kotvy
                        continue;
                    string href = hyperlink.Substring(hrefBegin, hyperlink.IndexOf('"', hrefBegin) - hrefBegin);
                    Regex regex = new Regex(Nastaveni.Get("MVCRSbirkaURLRegex"));
                    if (regex.IsMatch(href))
                        souboryNaStrance.Enqueue(new Uri(Nastaveni.Get("MVCRSbirkaURLPrefix") + href));
                }

                Dump("Nacteno {0} odkazu", souboryNaStrance.Count);
                response.Close();
            }
            else
            {
                response.Close();
                throw new UZException("Nemuzu nacist stranku MVCR");
            }
        }

        private Castka NactiSoubor(Uri soubor)
        {
            Dump(String.Format("Nacitam soubor: {0}", soubor.PathAndQuery));

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(soubor.OriginalString);
            request.Timeout = 20000;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Dump("Response: {0}", response.StatusDescription);



            if (response.StatusCode == HttpStatusCode.OK)
            {
                WebHeaderCollection headers = response.Headers;
                response.Close();
                string disposition = headers.Get("Content-disposition");
                string pdfSoubor = disposition.Substring(disposition.IndexOf('=') + 1).Trim();
                FileInfo info = new FileInfo(pdfSoubor);
                if (info.Extension != ".pdf")
                    return null;

                mapaSoubor[soubor] = pdfSoubor;

                Castka novaCastka = Castka.ZPdf(pdfSoubor);
                mapaCastka[soubor] = novaCastka;

                return novaCastka;
            }
            else
            {
                response.Close();
                throw new UZException("Nelze nacist soubor" + soubor.OriginalString);
            }

        }

        private string StahniSoubor(Uri soubor)
        {

            string pdfSoubor = mapaSoubor[soubor];
            string pdfCesta = AdresarStahovani + '/' + pdfSoubor;

            FileInfo info = new FileInfo(pdfCesta);

            if (nestahovatZnovu && info.Exists)
            {
                Dump("Soubor byl jiz stazen");
                return pdfCesta;
            }


            Dump("Stahuji soubor z adresy {0}", soubor.OriginalString);

            WebClient client = new WebClient();
            byte[] data = client.DownloadData(soubor.OriginalString);

            DirectoryInfo dir = new DirectoryInfo(AdresarStahovani);
            if (!dir.Exists)
                dir.Create();

            using (FileStream fs = new FileStream(pdfCesta, FileMode.Create, FileAccess.ReadWrite))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write(data);
                }
            }

            Dump("Soubor {0} stazen", pdfCesta);
            return pdfCesta;
        }
    }
}
