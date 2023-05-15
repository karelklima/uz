using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

namespace UZ.PDF.Font {

    class GlyphList : Pdf
    {

        private static Dictionary<int,string> unicode2names = new Dictionary<int,string>();
        private static Dictionary<string,int[]> names2unicode = new Dictionary<string,int[]>();
    
        static GlyphList() {
            //Stream inputStream = null;
            try {
                /*FileInfo info = new FileInfo("glyphlist.txt");
                if (!info.Exists)
                    throw new PdfException("glyphlist.txt not found");

                inputStream = info.OpenRead();
                
                byte[] buffer = new byte[1024];
                MemoryStream output = new MemoryStream();
                while (true) {
                    int size = inputStream.Read(buffer, 0, buffer.Length);
                    if (size == 0)
                        break;
                    output.Write(buffer, 0, size);
                }
                inputStream.Close();
                inputStream = null;

                String source = EncodingTools.ConvertToString(output.ToArray(), null);
                StringReader reader = new StringReader(source);*/

                StringReader reader = new StringReader(Properties.Resources.glyphlist);

                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null) // end of file
                        break;
                    if (line.Length > 0 && line[0] == '#') // comment
                        continue;
                    string[] chunks = line.Split(';');
                    if (chunks.Length != 2)
                        throw new PdfException("Invalid glyphlist.txt");

                    string name = chunks[0];
                    string hexcode = chunks[1];
                    if (hexcode.Contains(' '))
                        hexcode = hexcode.Remove(hexcode.IndexOf(' '));
                    
                    int number = int.Parse(hexcode, NumberStyles.HexNumber);
                    unicode2names[number] = name;
                    names2unicode[name] = new int[] { number };
                }
                
            }
            catch (Exception e) {
                throw new PdfException("Unable to read glyphlist.exe: " + e.Message);
            }
            
        }
    
        public static int[] NameToUnicode(string name) {
            int[] a;
            try
            {
                names2unicode.TryGetValue(name, out a);
                return a;
            }
            catch { return null; }
        }
    
        public static string UnicodeToName(int num) {
            string a;
            try
            {
                unicode2names.TryGetValue(num, out a);
                return a;
            }
            catch { return null; }
        }
    }
}

