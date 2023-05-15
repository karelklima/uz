using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UZ.PDF.Objects;

namespace UZ.PDF
{
    class ContentParser : Pdf
    {

        public const int COMMAND_TYPE = 200;

        Tokenizer tokens;

        public Tokenizer Tokenizer
        {
            get { return tokens; }
        }

        public ContentParser(byte[] content)
        {
            tokens = new Tokenizer(content);
            
            FileStream file = new FileStream("Debug/contentparser.txt", FileMode.Create);
            file.Write(content, 0, content.Length);
            file.Close();
        }

        public ContentParser(Tokenizer tokenizer)
        {
            tokens = tokenizer;
        }

        public List<PdfObject> ParseOperands(List<PdfObject> objects)
        {
            if (objects == null)
                objects = new List<PdfObject>();
            else
                objects.Clear();

            PdfObject obj;
            while ((obj = ReadObject()) != null)
            {
                objects.Add(obj);
                if (obj.Type == PdfObject.ObjectType.LITERAL)
                    break; // operator found
            }

            return objects;
        }

        public PdfDictionary ReadDictionary()
        {
            PdfDictionary dictionary = new PdfDictionary();
            while (true)
            {
                Assert(NextContentToken(), "Unexpected end of file reached");
                if (tokens.Type == Tokenizer.TokenType.DICTIONARY_END)
                    break;
                
                Assert(tokens.Type == Tokenizer.TokenType.NAME, "Name token expected inside dictionary");
                PdfName name = new PdfName(tokens.Value);
                PdfObject obj = ReadObject();
                dictionary.Set(name, obj);
            }
            return dictionary;
        }

        private PdfArray ReadArray()
        {
            PdfArray array = new PdfArray();
            while (true)
            {
                PdfObject obj = ReadObject();
                if (tokens.Type == Tokenizer.TokenType.ARRAY_END)
                    break;
                array.Objects.Add(obj);
            }
            return array;
        }

        public PdfObject ReadObject()
        {
            if (!NextContentToken())
                return null;

            switch (tokens.Type)
            {
                case Tokenizer.TokenType.NAME:
                    return new PdfName(tokens.Value);
                case Tokenizer.TokenType.NUMBER:
                    return new PdfNumber(tokens.Value);
                case Tokenizer.TokenType.STRING:
                    return new PdfString(tokens.Value);
                case Tokenizer.TokenType.DICTIONARY_START:
                    return ReadDictionary();
                case Tokenizer.TokenType.ARRAY_START:
                    return ReadArray();
                //case Tokenizer.TokenType.OTHER:
                //    return new PdfCommand(tokens.Value);
                default:
                    return new PdfLiteral(tokens.Value);
            }
        }

        public bool NextContentToken()
        {
            while (tokens.Next())
            {
                if (tokens.Type == Tokenizer.TokenType.COMMENT)
                    continue;
                else
                    return true;
            }
            
            return false;
        }
    }
}
