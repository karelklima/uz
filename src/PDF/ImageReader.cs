using System;
using System.Collections.Generic;
using System.IO;
using UZ.PDF.Objects;
using UZ.PDF.Font;

namespace UZ.PDF
{
    class ImageReader
    {

        private byte[] currentData;
        private PdfDictionary currentDictionary;
        
        private Dictionary<string, string> inlineParamAbbrMap = new Dictionary<string,string>();
        private Dictionary<string, string> inlineColorAbbrMap = new Dictionary<string,string>();
        private Dictionary<string, string> inlineFilterAbbrMap = new Dictionary<string,string>();

        public byte[] Data
        {
            get { return currentData; }
        }

        public PdfDictionary Dictionary
        {
            get { return currentDictionary; }
        }

        public ImageReader()
        { 

            inlineParamAbbrMap["BPC"] = "BitsPerComponent";
            inlineParamAbbrMap["CS"] = "ColorSpace";
            inlineParamAbbrMap["D"] = "Decode";
            inlineParamAbbrMap["DP"] = "DecodeParams";
            inlineParamAbbrMap["F"] = "Filter";
            inlineParamAbbrMap["H"] = "Height";
            inlineParamAbbrMap["IM"] = "ImageMask";
            inlineParamAbbrMap["I"] = "Interpolate";
            inlineParamAbbrMap["W"] = "Width";

            inlineColorAbbrMap["G"] = "DeviceGray";
            inlineColorAbbrMap["RGB"] = "DeviceRGB";
            inlineColorAbbrMap["CMYK"] = "DeviceCMYK";
            inlineColorAbbrMap["I"] = "Indexed";

            inlineFilterAbbrMap["LZW"] = "LZWDecode";
            inlineFilterAbbrMap["Fl"] = "FlateDecode";
            inlineFilterAbbrMap["CCF"] = "CCITTFaxDecode";
        }

        public void ParseInlineImage(ContentParser parser, PdfDictionary colorSpaceDictionary)
        {
            this.currentDictionary = ParseInlineImageDictionary(parser);
            this.currentData = ParseInlineImageData(this.currentDictionary, colorSpaceDictionary, parser);
        }

        private PdfDictionary ParseInlineImageDictionary(ContentParser parser)
        {
            PdfDictionary dictionary = new PdfDictionary();

            for (PdfObject key = parser.ReadObject(); key != null && !"ID".Equals(key.ToString()); key = parser.ReadObject())
            {
                PdfObject value = parser.ReadObject();
                string trueKey = key.ToString();
                if (inlineParamAbbrMap.ContainsKey(key.ToString()))
                    inlineParamAbbrMap.TryGetValue(key.ToString(), out trueKey);

                string trueValueString = "";
                if (key.ToString() == "Filter" && inlineFilterAbbrMap.ContainsKey(value.ToString()))
                    inlineFilterAbbrMap.TryGetValue(value.ToString(), out trueValueString);
                else if (key.ToString() == "ColorSpace" && inlineColorAbbrMap.ContainsKey(value.ToString()))
                    inlineColorAbbrMap.TryGetValue(value.ToString(), out trueValueString);

                PdfObject trueValue = trueValueString.Length < 1 ? value : new PdfName(trueValueString);

                dictionary.Set(new PdfName(trueKey), trueValue);
            }

            int Char = parser.Tokenizer.Read();

            if (!parser.Tokenizer.IsWhitespace(Char))
                throw new PdfException("Unexpected character " + Char + " found in inline image after ID");

            return dictionary;
        }

        private static int GetComponentsPerPixel(PdfName colorSpaceName, PdfDictionary colorSpaceDictionary)
        {
            if (colorSpaceName == null)
                return 1;
            if (colorSpaceName.Equals("DeviceGrey"))
                return 1;
            if (colorSpaceName.Equals("DeviceRGB"))
                return 3;
            if (colorSpaceName.Equals("DeviceCMYK"))
                return 4;

            if (colorSpaceDictionary != null)
            {
                PdfArray colorSpace = (PdfArray)colorSpaceDictionary.Get(colorSpaceName.ToString());
                if (colorSpace != null)
                {
                    if ("Indexed".Equals(colorSpace.Objects[0]))
                    {
                        return 1;
                    }
                }
            }

            throw new PdfException("Unknown color space " + colorSpaceName);
        }

        private int ComputeBytesPerRow(PdfDictionary imageDictionary, PdfDictionary colorSpaceDictionary)
        {
            PdfNumber wObj = (PdfNumber)imageDictionary.Get("Width");
            PdfNumber bpcObj = (PdfNumber)imageDictionary.Get("BitsPerComponent");

            PdfName colorSpaceName = null;
            if (imageDictionary.ContainsKey("ColorSpace"))
                colorSpaceName = (PdfName)imageDictionary.Get("ColorSpace");
            int cpp = GetComponentsPerPixel(colorSpaceName, colorSpaceDictionary);

            int w = wObj.IntValue;
            int bpc = bpcObj != null ? bpcObj.IntValue : 1;


            int bytesPerRow = (w * bpc * cpp + 7) / 8;

            return bytesPerRow;
        }

        private byte[] ParseUnfilteredImageData(PdfDictionary imageDictionary, PdfDictionary colorSpaceDictionary, ContentParser parser)
        {
            PdfNumber h = (PdfNumber)imageDictionary.Get("Height");

            int bytesToRead = ComputeBytesPerRow(imageDictionary, colorSpaceDictionary) * h.IntValue;
            byte[] bytes = new byte[bytesToRead];
            Tokenizer tokenizer = parser.Tokenizer;

            int shouldBeWhiteSpace = tokenizer.Read();
            int startIndex = 0;
            if (!tokenizer.IsWhitespace(shouldBeWhiteSpace) || shouldBeWhiteSpace == 0)
            { 
                bytes[0] = (byte)shouldBeWhiteSpace;
                startIndex++;
            }
            for (int i = startIndex; i < bytesToRead; i++)
            {
                int Char = tokenizer.Read();
                if (Char == -1)
                    throw new PdfException("Could not parse inline image, end of content stream reached");

                bytes[i] = (byte)Char;
            }
            PdfObject ei = parser.ReadObject();
            if (!ei.ToString().Equals("EI")) // end of image
                throw new PdfException("End of inline image EI not found");

            return bytes;
        }

        private byte[] ParseInlineImageData(PdfDictionary imageDictionary, PdfDictionary colorSpaceDictionary, ContentParser parser)
        {
            if (!imageDictionary.ContainsKey("Filter"))
            {
                return ParseUnfilteredImageData(imageDictionary, colorSpaceDictionary, parser);
            }

            MemoryStream output = new MemoryStream();
            MemoryStream buffer = new MemoryStream();
            int Char;
            int found = 0;
            Tokenizer tokenizer = parser.Tokenizer;
            byte[] ff = null;

            while ((Char = tokenizer.Read()) != -1)
            {
                if (found == 0 && tokenizer.IsWhitespace(Char))
                {
                    found++;
                    buffer.WriteByte((byte)Char);
                }
                else if (found == 1 && Char == 'E')
                {
                    found++;
                    buffer.WriteByte((byte)Char);
                }
                else if (found == 1 && tokenizer.IsWhitespace(Char))
                {
                    output.Write(ff = buffer.ToArray(), 0, ff.Length);
                    buffer.SetLength(0);
                    buffer.WriteByte((byte)Char);
                }
                else if (found == 2 && Char == 'I')
                {
                    found++;
                    buffer.WriteByte((byte)Char);
                }
                else if (found == 3 && tokenizer.IsWhitespace(Char))
                {
                    return output.ToArray();
                }
                else
                {
                    output.Write(ff = buffer.ToArray(), 0, ff.Length);
                    buffer.SetLength(0);

                    output.WriteByte((byte)Char);
                    found = 0;
                }
            }
            throw new PdfException("Could not parse inline image data");
        }
    }
}