using System;
using System.Collections.Generic;
using System.Text;
using UZ.PDF;
using System.Text.RegularExpressions;

namespace UZ.Sbirka
{
    class Interpret
    {
        public const string BLOK = "blok";
        public const string ZACATEK = "zacatek";
        public const string KONEC = "konec";

        public const string UVOZOVKY_DOLE = "„";
        public const string UVOZOVKY_NAHORE = "“";

        public static Text InterpretujPredpis(Predpis predpis)
        {
            StructuredDocument extrakt = Extraktor.ExtrahujPredpis(predpis, predpis.Castka);
            Interpret instance = new Interpret(predpis.Adapter, extrakt, false);
            return instance.text;
        }

        public static Text InterpretujPredpis(IAdapter adapter, StructuredDocument text)
        {
            Interpret instance = new Interpret(adapter, text, true);
            return instance.text;
        }

        private IAdapter adapter;
        private List<StructuredDocument.Paragraph> odstavce = new List<StructuredDocument.Paragraph>();
        private Dictionary<int, Sekce.ISekce> automat = new Dictionary<int, Sekce.ISekce>();
        private Dictionary<int, List<int>> sukcese = new Dictionary<int, List<int>>();

        private Text text = new Text();

        private Sekce.ISekce ukazatel;
        private int iterator = 0;

        Regex zacatekCitace = new Regex("^" + UVOZOVKY_DOLE);
        Regex konecCitace = new Regex(UVOZOVKY_NAHORE + "(\\.|;)$");
        Regex zaverecneUstanoveni = new Regex("(^Dosavadní)|(^Poznámka pod čarou č\\.)");
        Regex poznamkaPodCarou = new Regex("^[0-9]{1,3}[a-z]{0,1}<>\\)");

        private Interpret(IAdapter adapter, StructuredDocument dokument, bool castZakona)
        {
            this.adapter = adapter;
            PostavAutomat();
            PostavSukcese();
            NactiOdstavce(dokument, castZakona);

            ukazatel = text.Obsah;

            ZpracujPredpis();
            SpojRadky(text.Uvod);
            SpojRadky(text.Obsah);
            SpojRadky(text.Zaver);
        }

        private void PostavAutomat()
        {
            //automat = Sekce.Seznam;
            foreach (KeyValuePair<int, Sekce.ISekce> entry in Sekce.Seznam)
            {
                    automat.Add(entry.Key, entry.Value);
            }
        }

        private void PostavSukcese()
        {
            PostavSukcesi(Sekce.PREAMBULE, Sekce.CAST, Sekce.HLAVA, Sekce.CLANEK, Sekce.PARAGRAF);
            PostavSukcesi(Sekce.CAST, Sekce.HLAVA, Sekce.NADPIS, Sekce.CLANEK, Sekce.PARAGRAF);
            PostavSukcesi(Sekce.HLAVA, Sekce.DIL, Sekce.NADPIS, Sekce.CLANEK, Sekce.PARAGRAF);
            PostavSukcesi(Sekce.DIL, Sekce.ODDIL, Sekce.NADPIS, Sekce.CLANEK, Sekce.PARAGRAF);
            PostavSukcesi(Sekce.ODDIL, Sekce.PODODDIL, Sekce.NADPIS, Sekce.CLANEK, Sekce.PARAGRAF);
            PostavSukcesi(Sekce.PODODDIL, Sekce.NADPIS, Sekce.CLANEK, Sekce.PARAGRAF);
            //PostavSukcesi(Sekce.NADPIS, Sekce.CLANEK, Sekce.PARAGRAF);
            PostavSukcesi(Sekce.NADPIS); // TODO Nadpisy netvori kontejner, neni jasne, kde konci nebo zacinaji ??????
            PostavSukcesi(Sekce.CLANEK, Sekce.ODSTAVEC, Sekce.PISMENO, Sekce.BOD);
            PostavSukcesi(Sekce.PARAGRAF, Sekce.ODSTAVEC, Sekce.PISMENO, Sekce.BOD);
            PostavSukcesi(Sekce.ODSTAVEC, Sekce.PISMENO, Sekce.BOD);
            PostavSukcesi(Sekce.PISMENO, Sekce.BOD);
            PostavSukcesi(Sekce.BOD);
            PostavSukcesi(Sekce.POZNAMKA);
        }

        private void PostavSukcesi(int rodic, params int[] deti)
        {
            List<int> seznamDeti = new List<int>(deti);
            sukcese.Add(rodic, seznamDeti);
        }

        private void NactiOdstavce(StructuredDocument dokument, bool castZakona)
        {
            bool konec = false;
            int stav = castZakona ? 1 : 0;
            int strana = 0;
            while (!konec && strana < dokument.Pages.Count)
            {
                StructuredDocument.Page blok = dokument.Pages[strana++];
                bool zacatek = true;
                for (int i = 0; i < blok.RenderedObjects.Count; i++)
                {
                    if (blok.RenderedObjects[i].ContentType != StructuredDocument.ContentType.Paragraph)
                        continue; // zpracovavame pouze text

                    StructuredDocument.Paragraph odstavec = (StructuredDocument.Paragraph)blok.RenderedObjects[i];
                    string obsah = SpojRadky(odstavec.Text);

                    switch (stav)
                    {
                        case 0:
                            text.Uvod.AddSubsekce(GetSekceText(obsah));
                            if (adapter.UvodRegex.IsMatch(obsah))
                                stav = 1; // konec uvodu
                            break;
                        case 1:
                            if (!castZakona && adapter.ZaverRegex.IsMatch(obsah))
                            {
                                stav = 2;
                                text.Zaver.AddSubsekce(GetSekceText(obsah));
                                break;
                            }
                            if (zacatek)
                            {
                                odstavec.SetAttribute(BLOK, ZACATEK);
                                zacatek = false;
                            }
                            odstavce.Add(new StructuredDocument.Paragraph(odstavec));
                            break;
                        case 2:
                            text.Zaver.AddSubsekce(GetSekceText(obsah));
                            break;
                    }
                }
            }

        }

        private Sekce.Text GetSekceText(string obsah)
        {
            Sekce.Text vystup = new Sekce.Text();
            vystup.AddUvodniUstanoveni(obsah);
            return vystup;
        }

        private int GetIdSekce(Sekce.ISekce sekce)
        {
            foreach (KeyValuePair<int, Sekce.ISekce> entry in automat)
            {
                if (entry.Value.Typ == sekce.Typ)
                    return entry.Key;
            }
            return -1;
        }

        private int GetIdSekce(int cisloObjektu)
        {
            if (odstavce.Count <= cisloObjektu || cisloObjektu < 0)
                return -1; // neexistuje
            StructuredDocument.IRenderedObject objekt = odstavce[cisloObjektu];
            if (objekt.ContentType != StructuredDocument.ContentType.Paragraph)
                return -1;
            foreach (KeyValuePair<int, Sekce.ISekce> entry in automat)
            {
                if (((StructuredDocument.Paragraph)odstavce[cisloObjektu]).Rows.Count > 0)
                {
                    if (entry.Value.Regex.IsMatch(((StructuredDocument.Paragraph)odstavce[cisloObjektu]).Rows[0]))
                        return entry.Key;
                }
            }
                return -1;
        }

        private int ShodaSukcese(int cisloOdstavce, int idSekce)
        {
            int id = GetIdSekce(cisloOdstavce);
            return sukcese[idSekce].Contains(id) ? id : -1;
        }

        private void ZpracujPredpis()
        {

            // Nejprve zpracujeme poznamky pod carou
            iterator = 0;
            Sekce.Poznamka poznamka = (Sekce.Poznamka)automat[Sekce.POZNAMKA];
            while (iterator < odstavce.Count)
            {
                if (odstavce[iterator].Rows.Count < 1)
                {
                    iterator++;
                    continue;
                }

                StructuredDocument.Paragraph odstavec = odstavce[iterator];
                if (odstavec.Rows.Count > 0 && zacatekCitace.IsMatch(odstavec.Rows[0])) // preskocime poznamky pod carou v novlizaci
                {
                    while (iterator < odstavce.Count - 1 && !konecCitace.IsMatch(odstavec.Text))
                    {
                        iterator++;
                        odstavec = odstavce[iterator];
                    }
                    iterator++;
                    continue;
                }

                Match match = poznamka.Regex.Match(odstavec.Rows[0]);

                if (poznamka.Regex.IsMatch(odstavec.Rows[0]))
                {
                    Sekce.ISekce sekce = poznamka.Factory();
                    sekce.Cislo = match.Groups[1].Value;
                    if (match.Length >= odstavec.Rows[0].Length)
                        sekce.Oznaceni = odstavec.Rows[0];
                    else
                        sekce.Oznaceni = odstavec.Rows[0].Remove(match.Length);
                    string textSekce = odstavec.Text.Remove(0, match.Length).Trim();
                    sekce.AddUvodniUstanoveni(textSekce);
                    text.Obsah.AddPoznamka(sekce); // pridani do rootu
                    odstavce.RemoveAt(iterator); //smazeme poznamku
                }
                else 
                {
                    iterator++;
                }
                
            }

            // nyni zpracujeme zbytek predpisu
            iterator = 0;
            int pojistka = iterator;
            while (iterator < odstavce.Count)
            {
                StructuredDocument.Paragraph odstavec = odstavce[iterator];
                if (odstavec.Rows.Count > 0 && zacatekCitace.IsMatch(odstavec.Rows[0]))
                    ZpracujNovelizaci(odstavec);
                else
                    ZpracujOdstavec(odstavec);
                
                if (pojistka >= iterator)
                    throw new UZException("Problem s ukazatelem");
                pojistka = iterator;
            }
        }

        private void ZpracujNovelizaci(StructuredDocument.Paragraph novyOdstavec)
        {
            StructuredDocument novelizace = new StructuredDocument();
            novyOdstavec.Text = novyOdstavec.Text.Substring(1); // smazani dolnich uvozovek
            novelizace.Add(novyOdstavec);
            iterator++;

            if (konecCitace.IsMatch(novyOdstavec.Text))
            {
                novyOdstavec.Text = novyOdstavec.Text.Remove(novyOdstavec.Text.Length - 2); // smazani hornich uvozovek
            }
            else
            {
                bool konec = false;
                while (!konec)
                {
                    if (iterator >= odstavce.Count)
                    {
                        //throw new UZException("Necekany konec novelizacniho bloku");
                        konec = true;
                        break;
                    }
                    StructuredDocument.Paragraph odstavec = odstavce[iterator];
                    
                    if (konecCitace.IsMatch(odstavec.Text))
                    {
                        odstavec.Text = odstavec.Text.Remove(odstavec.Text.Length - 2); // smazani hornich uvozovek
                        konec = true;
                    }
                    novelizace.Add(odstavec);

                    iterator++;
                }
            }

            // nyni musime odstavce v uvozovkach interpretovat
            Text kontejner = Interpret.InterpretujPredpis(this.adapter, novelizace);
            ukazatel.AddSubsekce(kontejner.Obsah);

           
        }

        private void ZpracujOdstavec(StructuredDocument.Paragraph odstavec)
        {
            int idSekce = GetIdSekce(iterator);

            if (idSekce < 0)
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("Neznama sekce");

                for (int i = Math.Max(0, iterator - 3); i < iterator + 3; i++)
                {
                    if (i == iterator)
                        builder.Append("-> ");
                    if (i < odstavce.Count)
                        builder.AppendLine(odstavce[i].ToString());
                }

                //Console.Write(builder.ToString());

                throw new UZException(builder.ToString());
            }

            int idSukcese = ShodaSukcese(iterator + 1, idSekce);

            Match match = automat[idSekce].Regex.Match(odstavec.Rows[0]);
            Sekce.ISekce sekce = automat[idSekce].Factory();
            sekce.Cislo = match.Groups[1].Value;
            if (match.Length >= odstavec.Rows[0].Length)
                sekce.Oznaceni = odstavec.Rows[0];
            else
                sekce.Oznaceni = odstavec.Rows[0].Remove(match.Length);

            string textSekce = odstavec.Text.Remove(0, match.Length).Trim();

            if (idSekce == Sekce.PREAMBULE)
            {
                while (0 > ShodaSukcese(iterator + 1, Sekce.PREAMBULE))
                    sekce.AddSubsekce(GetSekceText(odstavce[++iterator].Text));
                text.Obsah.AddSubsekce(sekce, 0);
                iterator++;
                return; // stoji mimo hierarchii
            }
            if (idSekce == Sekce.POZNAMKA)
            {
                throw new UZException("Poznamky jsou zpracovany extra");
                /*sekce.AddUvodniUstanoveni(textSekce);
                text.Obsah.AddPoznamka(sekce); // pridani do rootu
                iterator++;
                return; // stoji mimo hierarchii */
            }

            if (idSekce >= Sekce.CAST && idSekce <= Sekce.PODODDIL)
            {
                if (idSukcese == Sekce.NADPIS || idSukcese < 0) // fakultativni nadpis
                {
                    if (odstavce.Count <= iterator + 1)
                        throw new UZException("neocekavany konec #2");
                    sekce.AddNadpis(odstavce[++iterator].Text);
                }

            }
            else if (idSekce == Sekce.CLANEK || idSekce == Sekce.PARAGRAF)
            {

                int id1 = GetIdSekce(iterator + 1);
                int id2 = GetIdSekce(iterator + 2);

                if (id1 == Sekce.NADPIS && id2 == Sekce.ODSTAVEC)
                {
                    sekce.AddNadpis(odstavce[++iterator].Text);
                }
                else if ((id1 == Sekce.NADPIS && id2 == Sekce.NADPIS)
                    && (odstavce[iterator + 2].GetAttribute(BLOK) != ZACATEK) // uvodni ustanoveni neni na zacatku stranky
                    && !zacatekCitace.IsMatch(odstavce[iterator + 2].Text) // uvodni ustanoveni neni citace
                    && !odstavce[iterator + 1].Text.EndsWith(".")) // nadpis je skutecne nadpis
                {
                    sekce.AddNadpis(odstavce[++iterator].Text);
                    sekce.AddUvodniUstanoveni(odstavce[++iterator].Text);
                }
                else if (id1 != Sekce.ODSTAVEC)
                {
                    if (odstavce.Count <= iterator + 1)
                        throw new UZException("neocekavany konec #1");
                    sekce.AddUvodniUstanoveni(odstavce[++iterator].Text);
                    // Novelizace o jedinem bodu - syntakticky to neni bod, ale veta
                    if (sekce.UvodniUstanoveni.EndsWith(":") && id2 < 0 && iterator < odstavce.Count)
                    {
                        Sekce.ISekce bod = Sekce.Seznam[Sekce.BOD].Factory();
                        if (iterator + 1 >= odstavce.Count)
                            throw new UZException("neocekavany konec");
                        bod.AddUvodniUstanoveni(odstavce[++iterator].Text);
                        sekce.AddSubsekce(bod);
                    }
                }

            }
            else if (idSekce == Sekce.ODSTAVEC || idSekce == Sekce.PISMENO || idSekce == Sekce.BOD)
            {
                sekce.AddUvodniUstanoveni(textSekce);
            }
            else if (idSekce == Sekce.NADPIS)
            {
                // Nadpis nebo zaverecna cast ustanoveni odstavce nebo paragrafu nebo clanku nasledujici po pismenu nebo bodu
                // nebo zaverecna cast ustanoveni bodu v ramci novelizace
                if (ukazatel.Typ == Sekce.BOD && zaverecneUstanoveni.IsMatch(textSekce)) // bod obsahuje 
                {
                    ukazatel.AddZaverecneUstanoveni(textSekce);
                    iterator++;
                    return;
                }
                else if ((ukazatel.Typ == Sekce.PISMENO || ukazatel.Typ == Sekce.BOD)
                    && !ukazatel.UvodniUstanoveni.EndsWith(".")
                    && ukazatel.Rodic != null)
                { // je to zaverecne ustanoveni
                    ukazatel.Rodic.AddZaverecneUstanoveni(textSekce);
                    ukazatel = ukazatel.Rodic;
                    iterator++;
                    return; // neni potreba pridavat dalsi sekci
                }
                sekce.AddNadpis(textSekce);
            }
            else
                throw new UZException("Nevalidni sekce: " + idSekce.ToString());

            // overime, zda-li dany clanek/paragraf/odstavec/pismeno/bod nepokracuje na dalsi strane
            if (!odstavec.Text.EndsWith(".") && iterator + 1 < odstavce.Count)
            {
                if (odstavce[iterator + 1].GetAttribute(BLOK) == ZACATEK && GetIdSekce(iterator + 1) == Sekce.NADPIS)
                {
                    StructuredDocument.Paragraph dalsiOdstavec = odstavce[iterator + 1];
                    // Sekce oznacena jako nadpis na dalsi strane je mozna pokracovanim popisu soucasne sekce
                    // Pokud jde o pokracovani, musime prislusne sekce spojit
                    bool pokracovani = sekce.UvodniUstanoveni.Length > 0 && !sekce.UvodniUstanoveni.EndsWith(".");
                    if (sekce.ZaverecneUstanoveni.Length > 0)
                        pokracovani = !sekce.ZaverecneUstanoveni.EndsWith(".");
                    if (pokracovani)
                    {
                        if (sekce.ZaverecneUstanoveni.Length > 0)
                            sekce.AddZaverecneUstanoveni(dalsiOdstavec.Text);
                        else
                            sekce.AddUvodniUstanoveni(dalsiOdstavec.Text);
                        iterator++;
                    }
                }
            }


            int rodicID = GetIdSekce(ukazatel); // nalezneme spravne misto ve strome pro umisteni nove sekce
            while (ukazatel != text.Obsah && !sukcese[rodicID].Contains(idSekce))
            {
                ukazatel = ukazatel.Rodic;
                rodicID = GetIdSekce(ukazatel);
            }

            // nyni osetrime pripad, kdy sekce s vlastnim nadpisem nasleduje po skupine sekci
            // sdruzenych do Sekce.NADPIS; nova sekce se presune na stejnou uroven jako NADPIS
            if (ukazatel.Rodic != null && ukazatel.Typ == Sekce.NADPIS && sekce.Nadpis.Length > 0)
                ukazatel = ukazatel.Rodic;

            // Nyni jeste musime rozdelit sekce, ktere pokracuji popisem na prvnim radku
            // Tyto sekce totiz nejsou vertikalne oddelene, tudiz mohou byt slite v jednom Paragraphu
            // Jde o sekce Odstavec, Pismeno a Bod

            if (/*!sekce.PopisNaDalsimRadku && */idSekce != Sekce.NADPIS)
            {
                List<Sekce.ISekce> podsekce = RozdelSekci(sekce);
                foreach (Sekce.ISekce pod in podsekce)
                {
                    ukazatel.AddSubsekce(pod);
                }
                ukazatel = podsekce[podsekce.Count - 1];
            }
            else
            {
                ukazatel.AddSubsekce(sekce);
                ukazatel = sekce;

                if (ukazatel.Subsekce.Count > 0) // novelizacni bod
                    ukazatel = ukazatel.Subsekce[ukazatel.Subsekce.Count - 1];
            }
            iterator++;
        }

        private void ZpracujOdstavecOld(StructuredDocument.Paragraph odstavec)
        {
            int idSekce = GetIdSekce(iterator);

            if (idSekce < 0)
            {
                int x = 1;
                throw new UZException("neznama sekce");
            }

            int idSukcese = ShodaSukcese(iterator + 1, idSekce);

            Match match = automat[idSekce].Regex.Match(odstavec.Rows[0]);
            Sekce.ISekce sekce = automat[idSekce].Factory();
            sekce.Cislo = match.Groups[1].Value;
            if (match.Length >= odstavec.Rows[0].Length)
                sekce.Oznaceni = odstavec.Rows[0];
            else
                sekce.Oznaceni = odstavec.Rows[0].Remove(match.Length);

            string textSekce = odstavec.Text.Remove(0, match.Length).Trim();

            if (idSekce == Sekce.PREAMBULE)
            {
                while (0 > ShodaSukcese(iterator + 1, Sekce.PREAMBULE))
                    sekce.AddSubsekce(GetSekceText(odstavce[++iterator].Text));
                text.Obsah.AddSubsekce(sekce, 0);
                iterator++;
                return; // stoji mimo hierarchii
            }
            if (idSekce == Sekce.POZNAMKA)
            {
                throw new UZException("Poznamky jsou zpracovany extra");
                /*sekce.AddUvodniUstanoveni(textSekce);
                text.Obsah.AddPoznamka(sekce); // pridani do rootu
                iterator++;
                return; // stoji mimo hierarchii */
            }

            if (idSekce >= Sekce.CAST && idSekce <= Sekce.PODODDIL)
            {
                if (idSukcese == Sekce.NADPIS || idSukcese < 0) // fakultativni nadpis
                {
                    if (odstavce.Count <= iterator + 1)
                        throw new UZException("neocekavany konec #2");
                    sekce.AddNadpis(odstavce[++iterator].Text);
                }

            }
            else if (idSekce == Sekce.CLANEK || idSekce == Sekce.PARAGRAF)
            {
                
                int id1 = GetIdSekce(iterator + 1);
                int id2 = GetIdSekce(iterator + 2);

                if (id1 == Sekce.NADPIS && id2 == Sekce.ODSTAVEC)
                {
                    sekce.AddNadpis(odstavce[++iterator].Text);
                }
                else if ((id1 == Sekce.NADPIS && id2 == Sekce.NADPIS)
                    && (odstavce[iterator + 2].GetAttribute(BLOK) != ZACATEK) // uvodni ustanoveni neni na zacatku stranky
                    && !zacatekCitace.IsMatch(odstavce[iterator + 2].Text) // uvodni ustanoveni neni citace
                    && !odstavce[iterator + 1].Text.EndsWith(".")) // nadpis je skutecne nadpis
                {
                    sekce.AddNadpis(odstavce[++iterator].Text);
                    sekce.AddUvodniUstanoveni(odstavce[++iterator].Text);
                }
                else if (id1 != Sekce.ODSTAVEC)
                {
                    if (odstavce.Count <= iterator + 1)
                        throw new UZException("neocekavany konec #1");
                    sekce.AddUvodniUstanoveni(odstavce[++iterator].Text);
                    // Novelizace o jedinem bodu - syntakticky to neni bod, ale veta
                    if (sekce.UvodniUstanoveni.EndsWith(":") && id2 < 0 && iterator < odstavce.Count)
                    {
                        Sekce.ISekce bod = Sekce.Seznam[Sekce.BOD].Factory();
                        if (iterator + 1 >= odstavce.Count)
                            throw new UZException("neocekavany konec");
                        bod.AddUvodniUstanoveni(odstavce[++iterator].Text);
                        sekce.AddSubsekce(bod);
                    }
                }
                
            }
            else if (idSekce == Sekce.ODSTAVEC || idSekce == Sekce.PISMENO || idSekce == Sekce.BOD)
            {
                sekce.AddUvodniUstanoveni(textSekce);
            }
            else if (idSekce == Sekce.NADPIS)
            {
                // Nadpis nebo zaverecna cast ustanoveni odstavce nebo paragrafu nebo clanku nasledujici po pismenu nebo bodu
                // nebo zaverecna cast ustanoveni bodu v ramci novelizace
                if (ukazatel.Typ == Sekce.BOD && zaverecneUstanoveni.IsMatch(textSekce)) // bod obsahuje 
                {
                    ukazatel.AddZaverecneUstanoveni(textSekce);
                    iterator++;
                    return;
                }
                else if ((ukazatel.Typ == Sekce.PISMENO || ukazatel.Typ == Sekce.BOD)
                    && !ukazatel.UvodniUstanoveni.EndsWith(".")
                    && ukazatel.Rodic != null)
                { // je to zaverecne ustanoveni
                    ukazatel.Rodic.AddZaverecneUstanoveni(textSekce);
                    ukazatel = ukazatel.Rodic;
                    iterator++;
                    return; // neni potreba pridavat dalsi sekci
                }
                sekce.AddNadpis(textSekce);
            }
            else
                throw new UZException("Nevalidni sekce: " + idSekce.ToString());
            
            // overime, zda-li dany clanek/paragraf/odstavec/pismeno/bod nepokracuje na dalsi strane
            if (!odstavec.Text.EndsWith(".") && iterator + 1 < odstavce.Count)
            {
                if (odstavce[iterator + 1].GetAttribute(BLOK) == ZACATEK && GetIdSekce(iterator + 1) == Sekce.NADPIS)
                {
                    StructuredDocument.Paragraph dalsiOdstavec = odstavce[iterator + 1];
                    // Sekce oznacena jako nadpis na dalsi strane je mozna pokracovanim popisu soucasne sekce
                    // Pokud jde o pokracovani, musime prislusne sekce spojit
                    bool pokracovani = sekce.UvodniUstanoveni.Length > 0 && !sekce.UvodniUstanoveni.EndsWith(".");
                    if (sekce.ZaverecneUstanoveni.Length > 0)
                        pokracovani = !sekce.ZaverecneUstanoveni.EndsWith(".");
                    if (pokracovani)
                    {
                        if (sekce.ZaverecneUstanoveni.Length > 0)
                            sekce.AddZaverecneUstanoveni(dalsiOdstavec.Text);
                        else
                            sekce.AddUvodniUstanoveni(dalsiOdstavec.Text);
                        iterator++;
                    }
                }
            }
            

            int rodicID = GetIdSekce(ukazatel); // nalezneme spravne misto ve strome pro umisteni nove sekce
            while (ukazatel != text.Obsah && !sukcese[rodicID].Contains(idSekce))
            {
                ukazatel = ukazatel.Rodic;
                rodicID = GetIdSekce(ukazatel);
            }

            // nyni osetrime pripad, kdy sekce s vlastnim nadpisem nasleduje po skupine sekci
            // sdruzenych do Sekce.NADPIS; nova sekce se presune na stejnou uroven jako NADPIS
            if (ukazatel.Rodic != null && ukazatel.Typ == Sekce.NADPIS && sekce.Nadpis.Length > 0)
                ukazatel = ukazatel.Rodic;

            // Nyni jeste musime rozdelit sekce, ktere pokracuji popisem na prvnim radku
            // Tyto sekce totiz nejsou vertikalne oddelene, tudiz mohou byt slite v jednom Paragraphu
            // Jde o sekce Odstavec, Pismeno a Bod

            if (/*!sekce.PopisNaDalsimRadku && */idSekce != Sekce.NADPIS)
            {
                List<Sekce.ISekce> podsekce = RozdelSekci(sekce);
                foreach (Sekce.ISekce pod in podsekce)
                {
                    ukazatel.AddSubsekce(pod);
                }
                ukazatel = podsekce[podsekce.Count - 1];
            }
            else
            {
                ukazatel.AddSubsekce(sekce);
                ukazatel = sekce;

                if (ukazatel.Subsekce.Count > 0) // novelizacni bod
                    ukazatel = ukazatel.Subsekce[ukazatel.Subsekce.Count - 1];
            }
            iterator++;
        }

        private List<Sekce.ISekce> RozdelSekci(Sekce.ISekce sekce)
        {
            string[] vstup = sekce.UvodniUstanoveni.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            sekce.UvodniUstanoveni = string.Empty;
            List<Sekce.ISekce> vystup = new List<Sekce.ISekce>();
            vystup.Add(sekce);
            for (int i = 0; i < vstup.Length; i++)
            {
                Match match = sekce.Regex.Match(vstup[i]);
                if (match.Success)
                {
                    Sekce.ISekce novaSekce = sekce.Factory();
                    novaSekce.Cislo = match.Groups[1].Value;
                    if (match.Length >= vstup[i].Length)
                        novaSekce.Oznaceni = vstup[i];
                    else
                        novaSekce.Oznaceni = vstup[i].Remove(match.Length);
                    novaSekce.UvodniUstanoveni = vstup[i].Substring(novaSekce.Oznaceni.Length).Trim();
                    vystup.Add(novaSekce);
                }
                else
                    vystup[vystup.Count - 1].AddUvodniUstanoveni(vstup[i]);
            }
            return vystup;
        }

        private void SpojRadky(Sekce.ISekce sekce)
        {
            sekce.Nadpis = SpojRadky(sekce.Nadpis);
            sekce.UvodniUstanoveni = SpojRadky(sekce.UvodniUstanoveni);
            sekce.ZaverecneUstanoveni = SpojRadky(sekce.ZaverecneUstanoveni);

            foreach (Sekce.ISekce pozn in sekce.Poznamky)
                SpojRadky(pozn); // poznamky

            foreach (Sekce.ISekce sub in sekce.Subsekce)
                SpojRadky(sub); // propagace do dalsich urovni
        }

        private string SpojRadky(string text)
        {
            if (text == null)
                return null;
            string[] radky = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (radky.Length < 1)
                return text; // nic ke spojovani
            StringBuilder spojenyText = new StringBuilder();
            for (int i = 0; i < radky.Length - 1; i++)
            {
                string radek = radky[i];
                if (radek.EndsWith("-"))
                {
                    radek = radek.Substring(0, radek.Length - 1);
                    spojenyText.Append(radek);
                }
                else
                {
                    spojenyText.Append(radek);
                    spojenyText.Append(" ");
                }
            }
            spojenyText.Append(radky[radky.Length - 1]);
            return spojenyText.ToString();
        }
    }
}
