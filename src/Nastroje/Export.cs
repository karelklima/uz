using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UZ.Nastroje
{
    class Export
    {

        private class Node
        {
            private bool isDir;
            private Regex regex;
            private List<Node> childNodes;

            private Node(string pattern, bool isDir)
            {
                this.regex = new Regex(pattern);
                this.isDir = isDir;
                this.childNodes = new List<Node>();
            }

            public static Node Directory(string pattern)
            {
                return new Node(pattern, true);
            }

            public static Node File(string pattern)
            {
                return new Node(pattern, false);
            }

            public bool IsDir()
            {
                return isDir;
            }

            public Regex GetRegex()
            {
                return regex;
            }

            public Node Add(Node node)
            {
                childNodes.Add(node);
                return this;
            }

            public List<Node> GetChildNodes()
            {
                return childNodes;
            }
        }


        private string cil;
        private string zdroj;
        private StreamWriter log;

        public static void DoSlozky(DirectoryInfo ext) {
            new Export(ext);
        }

        private Export(DirectoryInfo ext)
        {
            OverSlozku(ext.FullName);

            log = new StreamWriter(ext.FullName + "\\changelog_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");
            log.WriteLine("UZ CHANGELOG");
            log.WriteLine(DateTime.Now.ToUniversalTime());
            log.WriteLine("------------------------------------------");

            Node Root = Node.Directory("")
                .Add(Node.Directory("castky")
                    .Add(Node.Directory("[0-9]{4}")
                        .Add(Node.Directory("[0-9]{4}")
                            .Add(Node.File("_info\\.xml$"))
                            .Add(Node.File("_text\\.xml$")))))
                .Add(Node.Directory("predpisy")
                    .Add(Node.Directory("[0-9]{4}")
                        .Add(Node.Directory("[0-9]{4}")
                            .Add(Node.File("xml$")))));

            OverSlozku(ext.FullName);

            this.cil = ext.FullName;
            this.zdroj = Sbirka.Index.Adresar;

            KopirujUzel(Root, "");

            log.Close();
        }

        private void OverSlozku(string dir) {
            DirectoryInfo directory = new DirectoryInfo(dir);
            if (!directory.Exists)
                directory.Create();
        }

        private bool StejneSoubory(string path1, string path2)
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

        private void KopirujUzel(Node uzel, string suffix)
        {
            string adresar = zdroj + suffix;
            DirectoryInfo adresarInfo = new DirectoryInfo(adresar);
            OverSlozku(cil + suffix);

            foreach (Node sub in uzel.GetChildNodes())
            {
                if (sub.IsDir())
                {
                    foreach (DirectoryInfo subDir in adresarInfo.EnumerateDirectories())
                    {
                        if (sub.GetRegex().IsMatch(subDir.Name))
                        {
                            string subSuffix = suffix + "\\" + subDir.Name;
                            KopirujUzel(sub, subSuffix);
                        }
                    }
                }
                else
                {
                    foreach (FileInfo file in adresarInfo.EnumerateFiles())
                    {
                        if (sub.GetRegex().IsMatch(file.Name))
                        {
                            string cilSoubor = suffix + "\\" + file.Name;
                            string cilCesta = cil + cilSoubor;
                            if (!File.Exists(cilCesta))
                            {
                                file.CopyTo(cilCesta);
                                log.WriteLine("NOVY\t" + cilSoubor);
                            }
                            if (!StejneSoubory(cilCesta, file.FullName))
                            {
                                file.CopyTo(cilCesta, true);
                                log.WriteLine("ZMENA\t" + cilSoubor);
                            }   
                        }
                    }
                }
            }

        }
    }
}
