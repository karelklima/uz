using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UZ.Sbirka;

namespace UZ.Nastroje
{
    class Log
    {

        private StreamWriter log;
        private bool copy;

        public Log(string file) : this(file, true)
        { }

        public Log(string file, bool copyToConsole)
        {
            log = new StreamWriter(Index.Adresar + "/" + file + ".txt", false);
            this.copy = copyToConsole;
            
        }

        public Log(string path, string name, bool copyToConsole)
        {
            log = new StreamWriter(path + "\\" + name + ".txt");
            this.copy = copyToConsole;
        }

        public static string DateTimeStamp()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        public void AddLine()
        {
            Add("=======================================================");
        }

        public void Add(string message)
        {
            log.WriteLine(message);
            if (copy)
                Console.WriteLine(message);
        }

        public void Add(string message, params object[] args)
        {
            Add(String.Format(message, args));
        }

        public void Close()
        {
            log.Close();
        }
    }
}
