using System;
using System.Collections.Generic;
using System.Text;
using UZ.PDF;
using System.Text.RegularExpressions;

namespace UZ.Sbirka
{
    class Extraktor
    {
        public const string BLOK = "column";
        public const string STRED = "center";
        public const string LEVY = "left";
        public const string PRAVY = "right";

        public enum Blok { Stred, Levy, Pravy }

        public static StructuredDocument ExtrahujPredpis(Predpis predpis, Castka castka)
        {
            Extraktor instance = new Extraktor(predpis, castka);
            return instance.vystup;
        }

        private StructuredDocument text;
        private StructuredDocument vystup;

        private string BlokText(Blok blok)
        {
            switch (blok)
            {
                case Blok.Stred:
                    return STRED;
                case Blok.Levy:
                    return LEVY;
                case Blok.Pravy:
                    return PRAVY;
                default:
                    return STRED;
            }
        }

        private Extraktor(Predpis predpis, Castka castka)
        {
            int index = -1;
            for (int i = 0; i < castka.Predpisy.Count; i++)
                if (castka.Predpisy[i].Cislo == predpis.Cislo)
                {
                    index = i;
                    break;
                }

            if (index < 0 || predpis.Castka.Cislo != castka.Cislo || predpis.Castka.Rocnik != castka.Rocnik)
                throw new UZException("Dany predpis {0} nenalezi dane castce {1}", predpis.Oznaceni, castka.Oznaceni);

            vystup = new StructuredDocument();
            text = castka.Dokument;
            
            
            if (index == 0) // jde o prvni predpis, jeho prvni radky se mohou nachazet uz na prvni strane sbirky pod obsahem
            {
                List<StructuredDocument.IRenderedObject> objekty = text.Pages[0].SortedRenderedObjects;
                StructuredDocument firstPage = new StructuredDocument();
                firstPage.AddPage();
                //List<StructuredDocument.IRenderedObject> noveObjekty = new List<StructuredDocument.IRenderedObject>();
                // nyni najdeme posledni horizontalni caru (ta je prave pod obsahem) a zbytek nacteme do extraktu
                Regex obsahRegex = new Regex("(^OBSAH:$)|(^O B S A H:$)");
                int stav = 0;
                for (int i = 0; i < objekty.Count; i++)
                {
                    switch (stav)
                    {
                        case 0:
                            if (objekty[i].ContentType == StructuredDocument.ContentType.Paragraph
                                && obsahRegex.IsMatch(((StructuredDocument.Paragraph)objekty[i]).Text))
                                stav = 1;
                            break;
                        case 1:
                            if (objekty[i].ContentType == StructuredDocument.ContentType.Line)
                                stav = 2;
                            break;
                        case 2:
                            firstPage.Pages[0].Add(objekty[i]);
                            break;
                    }
                }

                if (firstPage.Pages[0].RenderedObjects.Count > 0)
                {
                    ZpracujObjekty(firstPage.Pages[0].SortedRenderedObjects);
                    //ZalozStranku(noveObjekty, Blok.Stred);
                }

                //text.Pages
            }

            //Regex hlavickaRegex = new Regex(String.Format("č\\.[ ]?{0}[ ]?/[ ]?{1}", predpis.Cislo, predpis.Rocnik));
            //Regex hlavickaKombinKonec = new Regex(String.Format("č\\.[ ]?{0}[ ]?a[ ]?([0-9]+)[ ]?/[ ]?{1}", predpis.Cislo, predpis.Rocnik));
            //Regex hlavickaKombinZacatek = new Regex(String.Format("č\\.[ ]?([0-9]+)[ ]?a[ ]?{0}[ ]?/[ ]?{1}", predpis.Cislo, predpis.Rocnik));

            Regex hlavickaRegex = new Regex(String.Format("č\\.[ ]?([0-9]*)[ ]?[a]?[ ]?{0}[ ]?[a]?[ ]?([0-9]*)[ ]?/[ ]?{1}", predpis.Cislo, predpis.Rocnik));
            

            for (int i = 1; i < text.Pages.Count; i++)
            {
                StructuredDocument.Page page = text.Pages[i];
                List<StructuredDocument.IRenderedObject> sortedObjects = page.SortedRenderedObjects;
                List<StructuredDocument.IRenderedObject> outputObjects = new List<StructuredDocument.IRenderedObject>();

                // sortedObjects[0] je odstavec, ktery odpovida hlavicce - popis cisla zakona, castky a strany
                // sortedObjects[1] je horizontalni cara
                // tyto dva odstavce mohou byt prohozene

                //if (sortedObjects[0].ContentType == StructuredDocument.ContentType.Line)
                //    sortedObjects.RemoveAt(0);
                while (sortedObjects.Count > 0 && sortedObjects[0].ContentType != StructuredDocument.ContentType.Paragraph)
                    sortedObjects.RemoveAt(0);

                StructuredDocument.Paragraph prvniOdstavec = (StructuredDocument.Paragraph)sortedObjects[0];


                if (!hlavickaRegex.IsMatch(prvniOdstavec.Rows[0]))
                    continue;

                sortedObjects.RemoveAt(0); // odstraneni hlavicky

                if (sortedObjects.Count > 0 && sortedObjects[0].ContentType == StructuredDocument.ContentType.Line)
                    sortedObjects.RemoveAt(0);

                Match match = hlavickaRegex.Match(prvniOdstavec.Rows[0]);

                string predchoziCislo = match.Groups[1].Value;
                string nasledujiciCislo = match.Groups[2].Value;

                int state = 0;
                if (predchoziCislo.Length < 1)
                    state = 1;

                foreach (StructuredDocument.IRenderedObject obj in sortedObjects)
                {
                    switch (state)
                    {
                        case 0:
                            if (obj.ContentType == StructuredDocument.ContentType.Paragraph
                                && obj.ToString().Equals(predpis.Cislo.ToString())
                                && Pozice(obj, page.Box) == Blok.Stred)
                            {
                                state = 1;
                                outputObjects.Add(obj);
                            }
                            break;
                        case 1:
                            if (nasledujiciCislo.Length > 0
                                && obj.ContentType == StructuredDocument.ContentType.Paragraph
                                && obj.ToString().Equals(nasledujiciCislo)
                                && Pozice(obj, page.Box) == Blok.Stred)
                            {
                                state = 2;
                            }
                            else
                                outputObjects.Add(obj);
                            break;
                        case 2:
                            // do nothing
                            break;
                    }
                }

                ZpracujObjekty(outputObjects);

            }
        }

        private StructuredDocument.Box ObalovyBox(List<StructuredDocument.IRenderedObject> objekty)
        {
            StructuredDocument.Box box = new StructuredDocument.Box();
            foreach (StructuredDocument.IRenderedObject obj in objekty)
                box.AddBox(obj.Box);
            return box;
        }

        private Blok Pozice(StructuredDocument.IRenderedObject objekt, StructuredDocument.Box obal)
        {
            float stred = obal.MinX + (obal.MaxX - obal.MinX) / 2f;
            if (objekt.Box.MaxX < stred)
                return Blok.Levy;
            if (objekt.Box.MinX > stred)
                return Blok.Pravy;
            return Blok.Stred;
        }

        private void ZpracujObjekty(List<StructuredDocument.IRenderedObject> objekty)
        {
            StructuredDocument.Box obal = ObalovyBox(objekty);

            List<StructuredDocument.IRenderedObject> frontaStred = new List<StructuredDocument.IRenderedObject>();
            List<StructuredDocument.IRenderedObject> frontaLevy = new List<StructuredDocument.IRenderedObject>();
            List<StructuredDocument.IRenderedObject> frontaPravy = new List<StructuredDocument.IRenderedObject>();

            foreach (StructuredDocument.IRenderedObject obj in objekty)
            {
                Blok pozice = Pozice(obj, obal);
                if (pozice == Blok.Stred)
                {
                    if (frontaLevy.Count > 0)
                    {
                        ZalozStranku(frontaLevy, Blok.Levy);
                        frontaLevy.Clear();
                    }
                    if (frontaPravy.Count > 0)
                    {
                        ZalozStranku(frontaPravy, Blok.Pravy);
                        frontaPravy.Clear();
                    }
                    frontaStred.Add(obj);
                }
                else
                {
                    if (frontaStred.Count > 0)
                    {
                        ZalozStranku(frontaStred, Blok.Stred);
                        frontaStred.Clear();
                    }
                    if (pozice == Blok.Levy)
                        frontaLevy.Add(obj);
                    else if (pozice == Blok.Pravy)
                        frontaPravy.Add(obj);
                }
            }

            if (frontaStred.Count > 0)
                ZalozStranku(frontaStred, Blok.Stred);
            else
            {
                if (frontaLevy.Count > 0)
                    ZalozStranku(frontaLevy, Blok.Levy);
                if (frontaPravy.Count > 0)
                    ZalozStranku(frontaPravy, Blok.Pravy);
            }


        }

        private void ZalozStranku(List<StructuredDocument.IRenderedObject> objekty, Blok blok)
        {
            vystup.AddPage();

            foreach (StructuredDocument.IRenderedObject obj in objekty)
                vystup.LastPage.Add(obj);

            vystup.LastPage.RebuildBox();
            vystup.LastPage.SetAttribute(BLOK, BlokText(blok));
        }


    }
}
