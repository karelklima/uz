using System;
using System.Collections.Generic;
using System.Text;
using UZ.PDF.Objects;
using UZ.PDF.Font;
using System.IO;
using System.Drawing;

namespace UZ.PDF
{
    class ContentReader : Pdf
    {
        private Dictionary<string, IOperator> operators = new Dictionary<string, IOperator>();

        private DocumentSkeleton document;
        private IContentRenderer renderer;

        private Stack<GraphicsState> graphicsStateStack = new Stack<GraphicsState>();

        private PdfDictionary currentResources;
        private Matrix currentTextMatrix;
        private Matrix currentTextLineMatrix;

        private PathRenderInfo currentPath;

        private Dictionary<int, CMapFont> fontCache = new Dictionary<int, CMapFont>();
        
        
        public ContentReader(DocumentSkeleton document, IContentRenderer renderer, int pageNumber = 0)
        {
            this.RegisterOperators();
            graphicsStateStack.Push(new GraphicsState());
            this.document = document;
            this.renderer = renderer;


            try
            {

                if (pageNumber == 0)
                {
                    DumpSource("Debug/contentreader_source.txt");
                    foreach (PdfDictionary Page in document.Pages)
                    {
                        this.currentResources = (PdfDictionary)Page.Get("Resources").GetTarget();
                        renderer.BeginPage(this.currentResources);

                        byte[] contents = GetContentBytes(Page.Get("Contents"));
                        ReadPageContent(contents);

                        renderer.EndPage(this.currentResources);
                    }
                    int a = 1;
                }
                else
                {
                    PdfDictionary customPage = document.Pages[pageNumber - 1];
                    this.currentResources = (PdfDictionary)customPage.Get("Resources").GetTarget();
                    renderer.BeginPage(this.currentResources);
                    byte[] contents = GetContentBytes(customPage.Get("Contents"));

                    //byte[] origData = ((PdfStream)customPage.Get("Contents").GetTarget()).Bytes;

                    //FileStream file = new FileStream("test/vzp_contentreader.txt", FileMode.Create);
                    //file.Write(contents, 0, contents.Length);
                    //file.Close();

                    ReadPageContent(contents);
                    renderer.EndPage(this.currentResources);
                }

            }
            catch (KeyNotFoundException e)
            { }
            /*catch (ArgumentOutOfRangeException e)
            {
                throw new PdfException("Unable to parse PDF file");
            }*/
            /*catch (IndexOutOfRangeException e)
            {
                throw new PdfException("Unable to parse PDF file");
            }*/
            /*catch (NullReferenceException e)
            {
                throw new PdfException("Unable to parse PDF file");
            }*/
        }

        private byte[] GetContentBytes(PdfObject obj)
        {
            try
            {
                if (obj == null)
                    return new byte[0];
                switch (obj.Type)
                {
                    case PdfObject.ObjectType.REFERENCE:
                        return GetContentBytes(obj.GetTarget());
                        break;
                    case PdfObject.ObjectType.STREAM:
                        PdfStream str = (PdfStream)obj;
                        return ((PdfStream)obj).Stream;
                    case PdfObject.ObjectType.ARRAY:
                        // need to concatenate streams
                        PdfArray streams = (PdfArray)obj;
                        MemoryStream memory = new MemoryStream();
                        foreach (PdfObject s in streams.Objects)
                        {
                            byte[] bytes = GetContentBytes(s);
                            memory.Write(bytes, 0, bytes.Length);
                            memory.WriteByte((byte)' ');
                        }
                        byte[] total = memory.ToArray();
                        return total;
                    default:
                        return new byte[0];
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                int x = 1;
                return null;
            }
        }

        private void RegisterOperators()
        {
            operators["q"] = new PushGraphicsState();
            operators["Q"] = new PopGraphicsState();
            operators["cm"] = new ModifyCurrentTransformationMatrix();
            operators["gs"] = new ProcessGraphicsStateResource();

            operators["BT"] = new BeginTextObject();
            operators["ET"] = new EndTextObject();

            operators["Tc"] = new SetCharacterSpacing();
            operators["Tw"] = new SetWordSpacing();
            operators["Tz"] = new SetHorizontalScaling();
            operators["TL"] = new SetTextLeading();
            operators["Tf"] = new SetTextFont();
            operators["Tr"] = new SetTextRenderingMode();
            operators["Ts"] = new SetTextRise();

            operators["Td"] = new MoveToNextLineAndOffset();
            operators["TD"] = new MoveToNextLineAndOffsetAndLeading();
            operators["Tm"] = new SetTextMatrix();
            operators["T*"] = new MoveToNextLine();

            operators["Tj"] = new ShowTextString();
            operators["'"] = new MoveToNextLineAndShowTextString();
            operators["\""] = new MoveToNextLineAndShowTextStringWithSpacing();
            operators["TJ"] = new ShowTextStrings();

            operators["rg"] = new SetRGBFill();
            operators["RG"] = new SetRGBStroke();
            operators["cs"] = new SetColorSpaceFill();
            operators["CS"] = new SetColorSpaceStroke();
            operators["sc"] = new SetColorFill();
            operators["scn"] = new SetColorFill();
            operators["SC"] = new SetColorStroke();
            operators["SCN"] = new SetColorStroke();

            operators["re"] = new ShowRectangle();
            operators["w"] = new SetLineWidth();
            operators["m"] = new BeginPath();
            operators["l"] = new AppendLineToPath();
            operators["h"] = new EndPath();
            operators["f"] = new FillPath();
            operators["f*"] = new FillPathEvenOdd();
            operators["S"] = new StrokePath();
            operators["s"] = new CloseAndStrokePath();
            operators["B"] = new FillAndStrokePath();
            operators["B*"] = new FillAndStrokePathEvenOdd();
        }

        public GraphicsState GraphicsState
        {
            get { return graphicsStateStack.Peek(); }
        }

        public void DumpSource(string filename, int pageNumber = 0)
        {
            FileStream file = new FileStream(filename, FileMode.Create);
            if (pageNumber == 0)
            {
                foreach (PdfDictionary Page in document.Pages)
                {
                    //PdfStream content = (PdfStream)Page.Get("Contents").GetTarget()
                    byte[] contents = GetContentBytes(Page.Get("Contents"));
                    file.Write(contents, 0, contents.Length);
                }
            }
            else
            {
                byte[] contents = GetContentBytes(document.Pages[pageNumber - 1].Get("Contents"));
                file.Write(contents, 0, contents.Length);
            }
            file.Close();
        }

        private void ReadPageContent(byte[] content)
        {
            ContentParser parser = new ContentParser(content);
            List<PdfObject> operands = new List<PdfObject>();
            StringBuilder builder = new StringBuilder();
            
            while (parser.ParseOperands(operands).Count > 0)
            {
                string operatorName = operands[operands.Count - 1].Value;

                if ("BI".Equals(operatorName))
                {
                    // Inline image
                    PdfDictionary colorSpaceDictionary = new PdfDictionary();
                    if (this.currentResources.ContainsKey("ColorSpace"))
                        colorSpaceDictionary = (PdfDictionary)this.currentResources.Get("ColorSpace");
                    ImageReader imageReader = new ImageReader();
                    imageReader.ParseInlineImage(parser, colorSpaceDictionary);
                    ImageRenderInfo renderInfo = new ImageRenderInfo(imageReader.Data, GraphicsState, imageReader.Dictionary, colorSpaceDictionary, true);
                    renderer.RenderImage(renderInfo);
                }

                if (!operators.ContainsKey(operatorName))
                {
                    /*foreach (PdfObject operand in operands)
                    {
                        Console.Write(operand.Value);
                        Console.Write(" ");
                    }
                    Console.WriteLine();*/
                    continue;
                }

                operands.RemoveAt(operands.Count - 1);
                InvokeOperator(operatorName, operands);

                builder.Append(operatorName);
                builder.Append(" ");
                foreach (PdfObject operand in operands)
                {
                    builder.Append(operand.ToString());
                    builder.Append(" ");
                }
                builder.AppendLine();
            }

            //using (StreamWriter outfile = new StreamWriter("test/vzp_operatory.txt"))
            //{
            //    outfile.Write(builder.ToString());
            //}
        }

        public CMapFont GetFont(string FontName)
        {
            PdfDictionary Resources = currentResources;
            PdfDictionary fonts = (PdfDictionary)Resources.Get("Font").GetTarget();
            PdfIndirectReference reference = (PdfIndirectReference)fonts.Get(FontName);
            if (fontCache.ContainsKey(reference.Number))
                return fontCache[reference.Number];
            CMapFont font = new CMapFont((PdfDictionary)reference.Target);
            fontCache.Add(reference.Number, font);
            return font;
        }

        private Color GetColor(string colorSpace, List<PdfObject> operands)
        {
            float[] c = new float[operands.Count];
            for (int i = 0; i < operands.Count; i++)
            {
                c[i] = ((PdfNumber)operands[i]).FloatValue;
            }

            if ("Separation".Equals(colorSpace))
            {
                int shade = 255 - IntColor(c[0]);
                return Color.FromArgb(255, shade, shade, shade);
            }
            if ("DeviceGray".Equals(colorSpace))
            {
                int shade = IntColor(c[0]);
                return Color.FromArgb(255, shade, shade, shade);
            }
            if ("DeviceRGB".Equals(colorSpace))
            {
                return Color.FromArgb(255, IntColor(c[0]), IntColor(c[1]), IntColor(c[2]));
            }
            if ("DeviceCMYK".Equals(colorSpace))
            {
                int cmykShade = IntColor(c[3]); // K
                return Color.FromArgb(255, cmykShade, cmykShade, cmykShade);
            }
            return Color.Transparent;
        }

        private int IntColor(float color)
        {
            return (int)(color * 255);
        }

        private void InvokeOperator(string operatorName, List<PdfObject> operands)
        {
            operators[operatorName].Invoke(this, operands);
        }

        private interface IOperator
        {
            void Invoke(ContentReader reader, List<PdfObject> operands);
        }

        // Operator q
        private class PushGraphicsState : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                GraphicsState oldGS = reader.graphicsStateStack.Peek();
                reader.graphicsStateStack.Push(new GraphicsState(oldGS));
            }
        }

        // Operator Q
        private class PopGraphicsState : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.graphicsStateStack.Pop();
            }
        }

        // Operator cm
        private class ModifyCurrentTransformationMatrix : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                float a = ((PdfNumber)operands[0]).FloatValue;
                float b = ((PdfNumber)operands[1]).FloatValue;
                float c = ((PdfNumber)operands[2]).FloatValue;
                float d = ((PdfNumber)operands[3]).FloatValue;
                float e = ((PdfNumber)operands[4]).FloatValue;
                float f = ((PdfNumber)operands[5]).FloatValue;
                Matrix NewCTM = new Matrix(a, b, c, d, e, f);
                GraphicsState GState = reader.GraphicsState;
                GState.CTM = NewCTM.Multiply(GState.CTM);
            }
        }

        // Operator BT
        private class BeginTextObject : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.renderer.BeginTextObject();
                reader.currentTextMatrix = new Matrix();
                reader.currentTextLineMatrix = new Matrix();
            }
        }

        // Operator ET
        private class EndTextObject : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.renderer.EndTextObject();
                reader.currentTextMatrix = null;
                reader.currentTextLineMatrix = null;
            }
        }

        // Operator Tc
        private class SetCharacterSpacing : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.GraphicsState.CharacterSpacing = ((PdfNumber)operands[0]).FloatValue;
            }
        }

        // Operator Tw
        private class SetWordSpacing : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.GraphicsState.WordSpacing = ((PdfNumber)operands[0]).FloatValue;
            }
        }

        // Operator Tz
        private class SetHorizontalScaling : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.GraphicsState.HorizontalScaling = ((PdfNumber)operands[0]).FloatValue / 100f;
            }
        }

        // Operator TL
        private class SetTextLeading : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.GraphicsState.TextLeading = ((PdfNumber)operands[0]).FloatValue;
            }
        }

        // Operator Tf
        private class SetTextFont : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.GraphicsState.TextFont = reader.GetFont(((PdfName)operands[0]).Value);
                reader.GraphicsState.TextFontSize = ((PdfNumber)operands[1]).FloatValue;
            }

         
        }

        // Operator Tr
        private class SetTextRenderingMode : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.GraphicsState.TextRenderingMode = ((PdfNumber)operands[0]).IntValue;
            }
        }

        // Operator Ts
        private class SetTextRise : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.GraphicsState.TextRise = ((PdfNumber)operands[0]).FloatValue;
            }
        }

        // Operator Td
        private class MoveToNextLineAndOffset : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                float x = ((PdfNumber)operands[0]).FloatValue;
                float y = ((PdfNumber)operands[1]).FloatValue;
                Matrix MultMatrix = new Matrix(x, y);
                reader.currentTextMatrix = MultMatrix.Multiply(reader.currentTextLineMatrix);
                reader.currentTextLineMatrix = MultMatrix.Multiply(reader.currentTextLineMatrix);
            }
        }

        // Operator TD
        private class MoveToNextLineAndOffsetAndLeading : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                float y = -((PdfNumber)operands[1]).FloatValue;
                List<PdfObject> OpList = new List<PdfObject>();
                OpList.Add(new PdfNumber(y.ToString(EncodingTools.NumberFormat)));
                reader.InvokeOperator("TL", OpList);
                reader.InvokeOperator("Td", operands);
            }
        }

        // Operator Tm
        private class SetTextMatrix : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                float a = ((PdfNumber)operands[0]).FloatValue;
                float b = ((PdfNumber)operands[1]).FloatValue;
                float c = ((PdfNumber)operands[2]).FloatValue;
                float d = ((PdfNumber)operands[3]).FloatValue;
                float e = ((PdfNumber)operands[4]).FloatValue;
                float f = ((PdfNumber)operands[5]).FloatValue;
                reader.currentTextMatrix = new Matrix(a, b, c, d, e, f);
                reader.currentTextLineMatrix = new Matrix(a, b, c, d, e, f);
            }
        }

        // Operator T*
        private class MoveToNextLine : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                List<PdfObject> OpList = new List<PdfObject>();
                OpList.Add(new PdfNumber(0.ToString()));
                OpList.Add(new PdfNumber((-reader.GraphicsState.TextLeading).ToString(EncodingTools.NumberFormat)));
                reader.InvokeOperator("Td", OpList);
            }
        }

        // Operator Tj
        private class ShowTextString : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                String unicodeString = Decode((PdfString)operands[0], reader.GraphicsState);
                string res = reader.GraphicsState.TextFont.Decode(new byte[] { 32 }, 0, 1);
                char wordSpaceCharacter = res.Length > 0 ? res[0] : ' ';

                TextRenderInfo textRenderInfo = new TextRenderInfo(unicodeString, new GraphicsState(reader.GraphicsState), new Matrix(reader.currentTextMatrix), wordSpaceCharacter);

                reader.renderer.RenderText(textRenderInfo);

                reader.currentTextMatrix = new Matrix(textRenderInfo.FullUnscaledWidth, 0).Multiply(reader.currentTextMatrix);
            }

            private string Decode(PdfString Input, GraphicsState GS)
            {
                return GS.TextFont.Decode(Input.Bytes, 0, Input.Bytes.Length);
            }
        }

        // Operator '
        private class MoveToNextLineAndShowTextString : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.InvokeOperator("T*", null);
                reader.InvokeOperator("Tj", operands);
            }
        }

        // Operator "
        private class MoveToNextLineAndShowTextStringWithSpacing : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.InvokeOperator("Tw", operands.GetRange(0, 1));
                reader.InvokeOperator("Tc", operands.GetRange(1, 1));
                reader.InvokeOperator("'", operands.GetRange(2, 1));
            }
        }

        // Operator TJ
        private class ShowTextStrings : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                PdfArray text = (PdfArray)operands[0];
                for (int i = 0; i < text.Objects.Count; i++)
                {
                    if (text.Objects[i].IsString())
                    {
                        // render string
                        reader.InvokeOperator("Tj", text.Objects.GetRange(i, 1));
                    }
                    else
                    {
                        // apply space to the text matrix
                        float rawSpace = ((PdfNumber)text.Objects[i]).FloatValue;
                        float adjust = -rawSpace / 1000f * reader.GraphicsState.HorizontalScaling * reader.GraphicsState.TextFontSize;
                        reader.currentTextMatrix = new Matrix(adjust, 0).Multiply(reader.currentTextMatrix);
                    }
                }
            }
        }

        // Operator rg
        private class SetRGBFill : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                /*int red = (int)(((PdfNumber)operands[0]).FloatValue * 255f);
                int green = (int)(((PdfNumber)operands[1]).FloatValue * 255f);
                int blue = (int)(((PdfNumber)operands[2]).FloatValue * 255f);
                reader.GraphicsState.Color = Color.FromArgb(red, green, blue);*/
                reader.GraphicsState.FillColor = reader.GetColor("DeviceRGB", operands);
            }
        }

        // Operator RG
        private class SetRGBStroke : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.GraphicsState.StrokeColor = reader.GetColor("DeviceRGB", operands);
            }
        }

        // Operator re
        private class ShowRectangle : IOperator
        {
            private PdfNumber FloatNumber(float num)
            {
                return new PdfNumber(num.ToString(EncodingTools.NumberFormat));
            }

            private void InvokePath(ContentReader reader, string op, float first, float second)
            {
                List<PdfObject> mOperands = new List<PdfObject>();
                mOperands.Add(FloatNumber(first));
                mOperands.Add(FloatNumber(second));
                reader.InvokeOperator(op, mOperands);
            }

            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                float x = ((PdfNumber)operands[0]).FloatValue;
                float y = ((PdfNumber)operands[1]).FloatValue;
                float width = ((PdfNumber)operands[2]).FloatValue;
                float height = ((PdfNumber)operands[3]).FloatValue;
                //reader.renderer.RenderLine(new LineRenderInfo(x, y, width, height, new GraphicsState(reader.GraphicsState)));

                InvokePath(reader, "m", x, y);
                InvokePath(reader, "l", x + width, y);
                InvokePath(reader, "l", x + width, y + height);
                InvokePath(reader, "l", x, y + height);
                reader.InvokeOperator("h", null);
            }
        }

        // Operator gs
        private class ProcessGraphicsStateResource : IOperator
        {
            public void Invoke(ContentReader processor, List<PdfObject> operands)
            {
                if (!processor.currentResources.ContainsKey("ExtGState"))
                    throw new PdfException("Resources do not contain ExtGState entry");
                PdfDictionary extGState = (PdfDictionary)processor.currentResources.Get("ExtGState").Target;
                
                PdfName dictionaryName = (PdfName)operands[0];

                if (!extGState.ContainsKey(dictionaryName.Value))
                    throw new PdfException("Unknown graphics state dictionary");


                PdfDictionary gsDic = (PdfDictionary)extGState.Get(dictionaryName.Value).Target;
                
                // at this point, all we care about is the FONT entry in the GS dictionary
                if (gsDic.ContainsKey("Font"))
                {
                    PdfArray fontParameter = (PdfArray)gsDic.Get("Font");
                    CMapFont font = processor.GetFont(fontParameter.Objects[0].Target.Value);
                    float size = ((PdfNumber)fontParameter.Objects[1]).FloatValue;

                    processor.GraphicsState.TextFont = font;
                    processor.GraphicsState.TextFontSize = size;
                }
            }
        }

        // Operator cs
        private class SetColorSpaceFill : IOperator
        {
            public void Invoke(ContentReader processor, List<PdfObject> operands)
            {
                string colorSpace = ((PdfName)operands[0]).Value;
                if (processor.currentResources.ContainsKey("ColorSpace"))
                {
                    PdfDictionary cSpace = (PdfDictionary)processor.currentResources.Get("ColorSpace").Target;
                    if (cSpace.ContainsKey(colorSpace))
                    {
                        PdfArray cSpaceArray = (PdfArray)cSpace.Get(colorSpace).Target;
                        colorSpace = cSpaceArray.Objects[0].Value;
                    }
                }
                processor.GraphicsState.ColorSpaceFill = colorSpace;
            }
        }

        // Operator CS
        private class SetColorSpaceStroke : IOperator
        {
            public void Invoke(ContentReader processor, List<PdfObject> operands)
            {
                string colorSpace = ((PdfName)operands[0]).Value;
                if (processor.currentResources.ContainsKey("ColorSpace"))
                {
                    PdfDictionary cSpace = (PdfDictionary)processor.currentResources.Get("ColorSpace").Target;
                    if (cSpace.ContainsKey(colorSpace))
                    {
                        PdfArray cSpaceArray = (PdfArray)cSpace.Get(colorSpace).Target;
                        colorSpace = cSpaceArray.Objects[0].Value;
                    }
                }
                processor.GraphicsState.ColorSpaceStroke = colorSpace;
            }
        }

        // Operator sc / scn
        private class SetColorFill : IOperator
        {
            public void Invoke(ContentReader processor, List<PdfObject> operands)
            {
                processor.GraphicsState.FillColor = processor.GetColor(processor.GraphicsState.ColorSpaceFill, operands);
            }
        }

        // Operator SC / SCN
        private class SetColorStroke : IOperator
        {
            public void Invoke(ContentReader processor, List<PdfObject> operands)
            {
                processor.GraphicsState.StrokeColor = processor.GetColor(processor.GraphicsState.ColorSpaceStroke, operands);
            }
        }

        // Operator w
        private class SetLineWidth : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.GraphicsState.LineWidth = ((PdfNumber)operands[0]).FloatValue;
            }
        }

        // Operator m
        private class BeginPath : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                float x = ((PdfNumber)operands[0]).FloatValue;
                float y = ((PdfNumber)operands[1]).FloatValue;
                reader.currentPath = new PathRenderInfo(reader.GraphicsState, new Vector(x, y));
            }
        }

        // Operator l
        private class AppendLineToPath : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                float x = ((PdfNumber)operands[0]).FloatValue;
                float y = ((PdfNumber)operands[1]).FloatValue;
                reader.currentPath.Add(new Vector(x, y));
            }
        }

        // Operator h
        private class EndPath : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.currentPath.Add(reader.currentPath.Start);
            }
        }

        // Operator f
        private class FillPath : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.currentPath.SetMode(PathRenderInfo.Mode.Fill);
                reader.renderer.RenderPath(reader.currentPath);
            }
        }

        // Operator f*
        private class FillPathEvenOdd : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.currentPath.SetMode(PathRenderInfo.Mode.FillEvenOdd);
                reader.renderer.RenderPath(reader.currentPath);
            }
        }

        // Operator S
        private class StrokePath : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.currentPath.SetMode(PathRenderInfo.Mode.Stroke);
                reader.renderer.RenderPath(reader.currentPath);
            }
        }

        // Operator s
        private class CloseAndStrokePath : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.InvokeOperator("h", null);
                reader.InvokeOperator("S", null);
            }
        }

        // Operator B
        private class FillAndStrokePath : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.currentPath.SetMode(PathRenderInfo.Mode.FillAndStroke);
                reader.renderer.RenderPath(reader.currentPath);
            }
        }

        // Operator B*
        private class FillAndStrokePathEvenOdd : IOperator
        {
            public void Invoke(ContentReader reader, List<PdfObject> operands)
            {
                reader.currentPath.SetMode(PathRenderInfo.Mode.FillAndStrokeEvenOdd);
                reader.renderer.RenderPath(reader.currentPath);
            }
        }
        
    }
}
