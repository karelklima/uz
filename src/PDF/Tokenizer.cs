using System;
using System.Text;
using System.Globalization;
using System.IO;

namespace UZ.PDF
{
    public class Tokenizer
    {

        public enum TokenType
        {
            EOF,
            NUMBER,
            STRING,
            NAME,
            COMMENT,
            ARRAY_START,
            ARRAY_END,
            DICTIONARY_START,
            DICTIONARY_END,
            REFERENCE,
            OTHER
        }

        public const double SupportedPDFVersion = 1.7;
        private double PDFVersion;

        private const int EOF = -1;
        private TokenType currentType;
        private string currentValue;
        private RandomAccessData file;

        private int reference;
        private int generation;

        
        public Tokenizer(string Filename)
        {
            file = new RandomAccessData(Filename);
        }

        public Tokenizer(byte[] stream)
        {
            file = new RandomAccessData(stream);
        }

        public Tokenizer(Stream stream)
        {
            file = new RandomAccessData(stream);
        }

        protected void Assert(bool expression, string message)
        {
            if (!expression)
                throw new PdfException(message);
        }

        public void CheckHeader()
        {
            file.StartOffset = 0;
            String Str = ReadString(1024);
            int id = Str.IndexOf("%PDF-");
            Assert(id >= 0, "Invalid PDF file #1");

            char[] ver = new char[3];
            Str.CopyTo(id+5, ver, 0, 3);

            int major, minor;
            Assert(Int32.TryParse(ver[0].ToString(), out major), "Invalid PDF file #2");
            Assert(ver[1] == '.', "Invalid PDF file #3");
            Assert(Int32.TryParse(ver[2].ToString(), out minor), "Invalid PDF file #4");

            PDFVersion = (double)major + 0.1*(double)minor;
            
            Assert(SupportedPDFVersion >= PDFVersion, "This version of PDF is not supported");

            file.StartOffset = id;
        }

        public TokenType Type
        {
            get { return currentType; }
        }

        public string Value
        {
            get { return currentValue; }
        }

        public int IntValue
        {
            get { return int.Parse(currentValue); }
        }

        public int Pointer
        {
            get { return file.Pointer; }
        }

        public RandomAccessData File
        {
            get { return file; }
        }

        public int Reference
        {
            get { return reference; }
        }

        public int Generation
        {
            get { return generation; }
        }

        public int Startxref
        {
            get
            {
                int EndSize = Math.Min(1024, file.Length);
                int StartPosition = file.Length - EndSize;
                file.Seek(StartPosition);
                string Str = ReadString(1024);
                int id = Str.LastIndexOf("startxref");
                if (id < 0)
                    throw new PdfException("PDF startxref not found");
                return StartPosition + id;
            }
        }

        public bool IsWhitespace(int Char)
        {
            return (Char == 0 || Char == 9 || Char == 10 || Char == 12 || Char == 13 || Char == 32);
        }

        public bool IsDelimiter(int Char)
        {
            return (Char == '(' || Char == ')' || Char == '<' || Char == '>' || Char == '[' || Char == ']' || Char == '/' || Char == '%');
        }

        public static int GetHex(int Char)
        {
            if (Char >= '0' && Char <= '9')
                return Char - '0';
            if (Char >= 'A' && Char <= 'F')
                return Char - 'A' + 10;
            if (Char >= 'a' && Char <= 'f')
                return Char - 'a' + 10;
            return -1;
        }

        public string ReadString(int size)
        {
            StringBuilder Buffer = new StringBuilder();
            int Char;
            while ((size--) > 0)
            {
                Char = file.Read();
                if (Char == EOF) // EOF
                    break;
                Buffer.Append((char)Char);
            }
            return Buffer.ToString();
        }

        public void Seek(int Position)
        {
            file.Seek(Position);
        }

        public int Read()
        {
            return file.Read();
        }

        public bool Next()
        {
            int Char = 0;
            do
            {
                Char = file.Read();
            } while (Char != EOF && IsWhitespace(Char));
            if (Char == EOF)
            { // EOF
                currentType = TokenType.EOF;
                return false;
            }
            StringBuilder Buffer = null;
            switch (Char)
            {
                case '/':
                    Buffer = new StringBuilder();
                    currentType = TokenType.NAME;
                    while (true)
                    {
                        Char = file.Read();
                        if (Char == EOF || IsDelimiter(Char) || IsWhitespace(Char))
                            break;
                        if (Char == '#')
                        {
                            Char = (GetHex(file.Read()) << 4) + GetHex(file.Read());
                        }
                        Buffer.Append((char)Char);
                    }
                    file.Back();
                    break;
                    
                case '<':
                    int C1 = file.Read();
                    if (C1 == '<')
                    {
                        currentType = TokenType.DICTIONARY_START;
                        break;
                    }
                    // string
                    Buffer = new StringBuilder();
                    currentType = TokenType.STRING;
                    int C2;
                    while (true)
                    {
                        while (IsWhitespace(C1))
                            C1 = file.Read();
                        if (C1 == '>')
                            break; // end of string

                        C1 = GetHex(C1);
                        if (C1 < 0)
                            break;
                        C2 = file.Read();
                        while (IsWhitespace(C2))
                            C2 = file.Read();
                        if (C2 == '>')
                        {
                            Char = C1 << 4;
                            Buffer.Append((char)Char);
                            break;
                        }
                        C2 = GetHex(C2);
                        if (C2 < 0)
                            break;
                        Char = (C1 << 4) + C2; // pair
                        Buffer.Append((char)Char);
                        C1 = file.Read();
                    }
                    break; // end reading string
                case '>':
                    Char = file.Read();
                    if (Char != '>')
                    {
                        throw new PdfException("Unexpected char '>'.");
                    }
                    currentType = TokenType.DICTIONARY_END;
                    break;
                case '[':
                    currentType = TokenType.ARRAY_START;
                    break;
                case ']':
                    currentType = TokenType.ARRAY_END;
                    break;
                case '%': 
                    currentType = TokenType.COMMENT;
                    do
                    {
                        Char = file.Read();
                    } while (Char != EOF && Char != '\r' && Char != '\n'); // remove
                    break;
                case '(':
                    Buffer = new StringBuilder();
                    currentType = TokenType.STRING;
                    int level = 0;
                    while (true) {
                        Char = file.Read();
                        if (Char == -1)
                            break;
                        if (Char == '(') {
                            ++level;
                        }
                        else if (Char == ')') {
                            --level;
                        }
                        if (Char == '\\')
                        {
                            Char = file.Read();
                            bool newLine = false;
                            switch (Char)
                            {
                                case 'n':
                                    Char = '\n';
                                    break;
                                case 'r':
                                    Char = '\r';
                                    break;
                                case 't':
                                    Char = '\t';
                                    break;
                                case 'b':
                                    Char = '\b';
                                    break;
                                case 'f':
                                    Char = '\f';
                                    break;
                                case '(':
                                case ')':
                                case '\\':
                                    break;
                                case '\r':
                                    newLine = true;
                                    Char = file.Read();
                                    if (Char != '\n')
                                        File.Back();
                                    break;
                                case '\n':
                                    newLine = true;
                                    break;
                                default: // parse octal character
                                    if (Char < '0' || Char > '7')
                                    {
                                        break;
                                    }
                                    int octal = Char - '0';
                                    Char = file.Read();
                                    if (Char < '0' || Char > '7')
                                    {
                                        File.Back();
                                        Char = octal;
                                        break;
                                    }
                                    octal = (octal << 3) + Char - '0';
                                    Char = file.Read();
                                    if (Char < '0' || Char > '7')
                                    {
                                        File.Back();
                                        Char = octal;
                                        break;
                                    }
                                    octal = (octal << 3) + Char - '0';
                                    Char = octal & 0xff;
                                    break; // end of switch-default
                            }
                            if (newLine)
                                continue;
                            if (Char < 0)
                                break;
                        }
                        else if (Char == '\r') // eliminate
                        {
                            Char = file.Read();
                            if (Char < 0)
                                break;
                            if (Char != '\n')
                            {
                                File.Back();
                                Char = '\n';
                            }
                        }
                        if (level < 0)
                            break;
                        Assert(Char >= 0, "Error reading string");
                        Buffer.Append((char)Char);
                    }
                    break;
                default:
                    Buffer = new StringBuilder();
                    if (Char == '-' || Char == '+' || Char == '.' || (Char >= '0' && Char <= '9'))
                    {
                        currentType = TokenType.NUMBER;
                        do
                        { // read whole number
                            Buffer.Append((char)Char);
                            Char = file.Read();
                        } while (Char != EOF && ((Char >= '0' && Char <= '9') || Char == '.'));
                    }
                    else
                    {
                        currentType = TokenType.OTHER;
                        do
                        {
                            Buffer.Append((char)Char);
                            Char = file.Read();
                        } while (Char != EOF && !IsDelimiter(Char) && !IsWhitespace(Char));
                    }
                    if (Char != EOF)
                        file.Back();
                    break;
            }

            if (Buffer != null)
                currentValue = Buffer.ToString();
            return true;

        }

        public void NextValid()
        {
            int level = 0;
            string n1 = null;
            string n2 = null;
            int ptr = 0;
            while (Next())
            {
                TokenType t = currentType;
                if (currentType == TokenType.COMMENT) // skip comments
                    continue;
                switch (level)
                {
                    case 0:
                        {
                            if (currentType != TokenType.NUMBER)
                                return;
                            ptr = file.Pointer;
                            n1 = currentValue;
                            ++level;
                            break; // skip comments again
                        }
                    case 1:
                        {
                            if (currentType != TokenType.NUMBER)
                            {
                                file.Seek(ptr);
                                currentType = TokenType.NUMBER;
                                currentValue = n1;
                                return;
                            }
                            n2 = currentValue;
                            ++level;
                            break; // skip comments again
                        }
                    default:
                        {
                            if (currentType != TokenType.OTHER || !currentValue.Equals("R"))
                            {
                                file.Seek(ptr);
                                currentType = TokenType.NUMBER;
                                currentValue = n1;
                                return;
                            }
                            currentType = TokenType.REFERENCE;
                            reference = int.Parse(n1);
                            generation = int.Parse(n2);
                            return;
                        }
                }
            }
        }

    }
}
