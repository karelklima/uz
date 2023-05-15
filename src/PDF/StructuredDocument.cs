using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using UZ.PDF;
using UZ.PDF.Objects;
using UZ.PDF.Font;

namespace UZ.PDF
{
    class StructuredDocument
    {

        public static StructuredDocument FromPdfFile(string file, int page = 0, float spaceWidthFactor = 0f, float lineBreakFactor = 1f)
        {
            DocumentSkeleton Doc = new DocumentSkeleton(file);
            ContentRenderer Renderer = new ContentRenderer(spaceWidthFactor, lineBreakFactor);
            ContentReader extractor = new ContentReader(Doc, Renderer, page);

            Renderer.Document.FixDiacritics();
            //Renderer.Document.DetectTables();

            return Renderer.Document;
            
        }

        protected static void Assert(bool expression, string message)
        {
            if (!expression)
                throw new PdfException(message);
        }
    
        public abstract class IRenderedObject
        {
            protected Dictionary<string, string> attributes = new Dictionary<string, string>();
            protected Box box = new Box();

            public Box Box
            {
                get { return box; }
            }

            public Dictionary<string, string> Attributes
            {
                get { return attributes; }
            }

            public void SetAttribute(string key, string value)
            {
                attributes[key] = value;
            }

            public string GetAttribute(string key)
            {
                if (attributes.ContainsKey(key))
                    return attributes[key];
                else
                    return string.Empty;
            }

            abstract public ContentType ContentType { get; }

            public IRenderedObject() { }

            public IRenderedObject(IRenderedObject other)
            {
                attributes = new Dictionary<string, string>(other.attributes);
                box = new Box();
                box.FromString(other.Box.ToString());
            }

        }

        public enum ContentType { Page, Paragraph, Image, Line, Table }

        public static string GetXmlTag(ContentType type)
        {
            switch (type)
            {
                case ContentType.Page:
                    return "page";
                case ContentType.Paragraph:
                    return "paragraph";
                case ContentType.Image:
                    return "image";
                case ContentType.Line:
                    return "line";
                case ContentType.Table:
                    return "table";
                default:
                    return "undefined";
            }
        }

        public static string GetXmlTag(IRenderedObject obj)
        {
            return GetXmlTag(obj.ContentType);
        }

        public static string XmlTag
        {
            get { return "document"; }
        }

        private List<Page> pages = new List<Page>();

        public List<Page> Pages
        {
            get { return pages; }
        }

        public Page LastPage
        {
            get
            {
                if (pages.Count < 1)
                    AddPage();
                return pages[pages.Count - 1];
            }
        }

        public void Add(IRenderedObject obj)
        {
            if (obj.ContentType == ContentType.Page)
                pages.Add((Page)obj);
            else
                LastPage.Add(obj);
        }

        public void AddPage()
        {
            Add(new Page());
        }

        public string ToString()
        {
            StringBuilder output = new StringBuilder();
            int counter = 1;
            foreach (Page page in pages)
            {
                output.Append("BEGIN PAGE " + counter.ToString());
                output.AppendLine();
                output.Append(page.ToString());
                output.AppendLine();
                output.AppendLine();
                output.Append("END PAGE " + counter.ToString());
                output.AppendLine();
                counter++;
            }
            return EncodingTools.FixDiacritics(output.ToString());
        }

        private void FixDiacritics()
        {
            foreach (Page p in pages)
                foreach (IRenderedObject obj in p.RenderedObjects)
                {
                    if (obj.ContentType == ContentType.Paragraph)
                    {
                        ((Paragraph)obj).FixDiacritics();
                    }
                }
        }

        private void DetectTables()
        {
            foreach (Page p in pages)
            {
                List<IRenderedObject> lines = new List<IRenderedObject>();
                foreach (IRenderedObject obj in p.SortedRenderedObjects)
                {
                    if (obj.ContentType == ContentType.Line)
                        lines.Add(obj);
                }

                // MOZNA STACI JEDEN PRUCHOD

                List<Table> tables = new List<Table>();
                Table table = null;
                
                for (int i = 0; i < lines.Count - 2; i++)
                {
                    if (table != null && table.Box.IntersectsWith(lines[i + 1].Box))
                    {
                        table.Add(lines[i + 1]);
                        p.RenderedObjects.Remove(lines[i + 1]);
                        table.RebuildBox();
                    }
                    else if (table == null && lines[i].Box.IntersectsWith(lines[i + 1].Box))
                    {
                        if (table == null)
                        {
                            table = new Table();
                            table.Add(lines[i]);
                            p.RenderedObjects.Remove(lines[i]);
                        }
                        table.Add(lines[i + 1]);
                        p.RenderedObjects.Remove(lines[i + 1]);
                        table.RebuildBox();
                    }
                    else if (table != null)
                    {
                        tables.Add(table);
                        table = null;
                    }
                }
                if (table != null)
                {
                    tables.Add(table);
                    table = null;
                }

                foreach (Table t in tables)
                {
                    List<IRenderedObject> toRem = new List<IRenderedObject>();
                    foreach (IRenderedObject o in p.RenderedObjects)
                    {
                        if (t.Box.IntersectsWith(o.Box))
                        {
                            t.Add(o);
                            toRem.Add(o);
                        }
                    }
                    foreach (IRenderedObject r in toRem)
                        p.RenderedObjects.Remove(r);
                    p.Add(t);
                }

            }
        }

        public void ToXML(Stream outputStream)
        {
            XmlTextWriter writer = new XmlTextWriter(outputStream, Encoding.UTF8);
            writer.Formatting = Formatting.Indented;
            writer.WriteStartDocument();
            writer.WriteStartElement(XmlTag);

            int counter = 1;
            foreach (Page page in pages)
            {
                writer.WriteStartElement(GetXmlTag(ContentType.Page));
                page.RebuildBox();
                writer.WriteAttributeString("box", page.Box.ToString());
                foreach (KeyValuePair<string, string> attr in page.Attributes)
                    writer.WriteAttributeString(attr.Key, attr.Value);

                foreach (IRenderedObject obj in page.SortedRenderedObjects)
                {
                    if (obj.ContentType == ContentType.Table)
                    {
                        Table table = (Table)obj;
                        writer.WriteStartElement(GetXmlTag(ContentType.Table));
                        table.RebuildBox();
                        writer.WriteAttributeString("box", table.Box.ToString());
                        foreach (KeyValuePair<string, string> attr in table.Attributes)
                            writer.WriteAttributeString(attr.Key, attr.Value);

                        foreach (IRenderedObject tobj in table.SortedRenderedObjects)
                        {
                            writer.WriteStartElement(GetXmlTag(tobj));
                            writer.WriteAttributeString("box", tobj.Box.ToString());
                            foreach (KeyValuePair<string, string> attr in tobj.Attributes)
                                writer.WriteAttributeString(attr.Key, attr.Value);
                            if (tobj.ContentType == ContentType.Paragraph)
                                writer.WriteValue(EncodingTools.FixDiacritics(tobj.ToString()));
                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();
                    }
                    else
                    {
                        writer.WriteStartElement(GetXmlTag(obj));
                        writer.WriteAttributeString("box", obj.Box.ToString());
                        foreach (KeyValuePair<string, string> attr in obj.Attributes)
                            writer.WriteAttributeString(attr.Key, attr.Value);
                        if (obj.ContentType == ContentType.Paragraph)
                            writer.WriteValue(EncodingTools.FixDiacritics(obj.ToString()));
                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();
                counter++;
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();

            writer.Flush();
            
        }

        public void ToXmlFile(string filename)
        {
            File.WriteAllText(filename, string.Empty); // smaze obsah souboru
            FileInfo souborXml = new FileInfo(filename);
            FileStream xmlstream = souborXml.OpenWrite();
            this.ToXML(xmlstream);
            xmlstream.Close();
        }

        public static StructuredDocument FromXml(Stream inputStream)
        {
            StructuredDocument output = new StructuredDocument();

            XmlTextReader reader = new XmlTextReader(inputStream);

            Stack<IRenderedObject> parentStack = new Stack<IRenderedObject>();

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == XmlTag)
                            continue; // zacatek dokumentu

                        IRenderedObject obj = null;
                        if (reader.Name == GetXmlTag(ContentType.Page))
                            obj = new Page();
                        else if (reader.Name == GetXmlTag(ContentType.Paragraph))
                            obj = new Paragraph();
                        else if (reader.Name == GetXmlTag(ContentType.Image))
                            obj = new Image();
                        else if (reader.Name == GetXmlTag(ContentType.Line))
                            obj = new Line();
                        else if (reader.Name == GetXmlTag(ContentType.Table))
                            obj = new Table();

                        if (obj == null)
                            throw new PdfException("Invalid element found in document: " + reader.Name);

                        while (reader.MoveToNextAttribute())
                        {
                            if (reader.Name == "box")
                                obj.Box.FromString(reader.Value);
                            else
                                obj.SetAttribute(reader.Name, reader.Value);
                        }

                        if (parentStack.Count < 1)
                        {
                            Assert(obj.ContentType == ContentType.Page, "Page object expected");
                            parentStack.Push(obj);
                            output.Add(obj);
                        }
                        else
                        {
                            Assert(obj.ContentType != ContentType.Page, "Page object not expected");
                            if (parentStack.Peek().ContentType != ContentType.Page && parentStack.Peek().ContentType != ContentType.Table)
                            {
                                parentStack.Pop(); // empty inline element
                            }
                            Assert(parentStack.Peek().ContentType == ContentType.Page || parentStack.Peek().ContentType == ContentType.Table, "Page or table expected");
                            ((Page)parentStack.Peek()).Add(obj);
                            parentStack.Push(obj);
                        }

                        reader.MoveToContent();
                        break;
                    case XmlNodeType.EndElement:
                        if (parentStack.Count > 0)
                            parentStack.Pop();
                        if (parentStack.Count > 0 && reader.Name.Equals(GetXmlTag(parentStack.Peek().ContentType)))
                            parentStack.Pop(); // previous pop was closing unclosed element
                        break;
                    case XmlNodeType.Text:
                        switch (parentStack.Peek().ContentType)
                        {
                            case ContentType.Paragraph:
                                Paragraph par = (Paragraph)parentStack.Peek();
                                par.Append(reader.Value);
                                par.SplitToRows();
                                break;
                            case ContentType.Line: // do nothing
                                break;
                            default:
                                throw new PdfException("Unexpected text content");
                                break;
                        }
                        break;
                    default:
                        // do nothing
                        break;
                }
            }

            return output;
        }

        public static StructuredDocument FromXmlFile(string filename)
        {
            FileStream FS = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            StructuredDocument output = FromXml(FS);
            FS.Close();
            return output;
        }

        public class Box
        {
            private bool init = false;
            private float minX;
            private float maxX;
            private float minY;
            private float maxY;

            public float MinX
            {
                get { return minX; }
            }

            public float MaxX
            {
                get { return maxX; }
            }

            public float MidX
            {
                get { return (minX + maxX) / 2f; }
            }

            public float MinY
            {
                get { return minY; }
            }

            public float MaxY
            {
                get { return maxY; }
            }

            public float MidY
            {
                get { return (minY + maxY) / 2f; }
            }

            public bool IsInit
            {
                get { return init; }
            }

            public void AddPoint(Vector point)
            {
                if (!IsInit)
                {
                    minX = maxX = point.X;
                    minY = maxY = point.Y;
                    init = true;
                }
                else
                {
                    if (point.X < minX)
                        minX = point.X;
                    if (point.X > maxX)
                        maxX = point.X;
                    if (point.Y < minY)
                        minY = point.Y;
                    if (point.Y > maxY)
                        maxY = point.Y;
                }
            }

            public void AddBox(Box other)
            {
                AddPoint(new Vector(other.minX, other.minY));
                AddPoint(new Vector(other.maxX, other.maxY));
            }

            public void Clear()
            {
                init = false;
                minX = maxX = minY = maxY = 0;
            }

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(minX.ToString(EncodingTools.NumberFormat));
                builder.Append(" ");
                builder.Append(maxX.ToString(EncodingTools.NumberFormat));
                builder.Append(" ");
                builder.Append(minY.ToString(EncodingTools.NumberFormat));
                builder.Append(" ");
                builder.Append(maxY.ToString(EncodingTools.NumberFormat));

                return builder.ToString();
            }

            public void FromString(string data)
            {
                string[] chunks = data.Split(' ');
                minX = float.Parse(chunks[0], EncodingTools.NumberFormat);
                maxX = float.Parse(chunks[1], EncodingTools.NumberFormat);
                minY = float.Parse(chunks[2], EncodingTools.NumberFormat);
                maxY = float.Parse(chunks[3], EncodingTools.NumberFormat);
            }

            public bool IntersectsWith(Box other)
            {
                if (maxY < other.minY
                    || minY > other.maxY
                    || maxX < other.minX
                    || minX > other.maxX)
                    return false;

                return true;                 

            }

        }

        public class Page : IRenderedObject
        {
            private List<IRenderedObject> renderedObjects = new List<IRenderedObject>();

            public override ContentType ContentType
            {
                get { return ContentType.Page; }
            }

            public List<IRenderedObject> RenderedObjects
            {
                get { return renderedObjects; }
            }

            public List<IRenderedObject> SortedRenderedObjects
            {
                get 
                {
                    List<IRenderedObject> sorted = new List<IRenderedObject>(RenderedObjects);
                    sorted.Sort(delegate(IRenderedObject o1, IRenderedObject o2)
                    {
                        if (o1.Box.MinY > o2.Box.MaxY)
                            return -1;
                        if (o1.Box.MaxY < o2.Box.MinY)
                            return 1;
                        if (o1.Box.MinX == o2.Box.MinX)
                            return 0;
                        return o1.Box.MinX < o2.Box.MinX ? -1 : 1;
                    });
                    return sorted;
                }
            }

            public IRenderedObject LastRenderedObject
            {
                get
                {
                    if (renderedObjects.Count < 1)
                        throw new PdfException("Rendered objects set is empty");
                    return renderedObjects[renderedObjects.Count - 1];
                }
            }

            public Paragraph LastParagraph
            {
                get
                {
                    int index = renderedObjects.Count - 1;
                    while (index >= 0 && renderedObjects[index].ContentType != ContentType.Paragraph)
                        index--;
                    if (index < 0)
                    {
                        AddParagraph();
                        index = renderedObjects.Count - 1;
                    }
                    
                    return (Paragraph)renderedObjects[index];
                }
            }

            public void Add(IRenderedObject obj)
            {
                renderedObjects.Add(obj);
            }

            public void AddParagraph()
            {
                Add(new Paragraph());
            }

            public void AddLine(Vector start, Vector end, float thickness)
            {
                Add(new Line(start, end, thickness));
            }

            public void RebuildBox()
            {
                box.Clear();
                foreach (IRenderedObject obj in renderedObjects)
                {
                    box.AddBox(obj.Box);
                }
            }

            public override string ToString()
            {
                StringBuilder output = new StringBuilder();
                foreach (IRenderedObject obj in renderedObjects)
                {
                    output.Append(obj.ToString());
                    output.AppendLine();
                    output.AppendLine();
                }
                return output.ToString();
            }

            
        }

        public class Paragraph : IRenderedObject
        {
            public const string FONT_STYLE = "fontstyle";
            public const string FONT_NORMAL = "normal";
            public const string FONT_BOLD = "bold";
            public const string FONT_ITALIC = "italic";

            public const string FONT_SIZE = "fontsize";
            public const string FONT_SIZE_DEFAULT = "0";

        

            private List<string> rows = new List<string>();
            private StringBuilder currentRow = new StringBuilder();
            private PdfObject xobject;

            public string FontStyle
            {
                get
                {
                    if (attributes.ContainsKey(FONT_STYLE))
                        return attributes[FONT_STYLE];
                    else
                        return FONT_NORMAL;
                }
                set
                {
                    if (attributes.ContainsKey(FONT_STYLE) && value == FONT_NORMAL)
                        attributes.Remove(FONT_STYLE); // styl normal kvuli uspore mista neni ulozen
                    else
                        attributes[FONT_STYLE] = value;
                }
            }

            public Boolean FontStyleBoldOrItalic
            {
                get
                {
                    return attributes.ContainsKey(FONT_STYLE) && !attributes[FONT_STYLE].Equals(FONT_NORMAL);
                }
            }

            public string FontSize
            {
                get
                {
                    if (attributes.ContainsKey(FONT_SIZE))
                        return attributes[FONT_SIZE];
                    else
                        return FONT_SIZE_DEFAULT;
                }
                set
                {
                    if (attributes.ContainsKey(FONT_SIZE) && value == FONT_SIZE_DEFAULT)
                        attributes.Remove(FONT_SIZE); // dtto
                    else
                        attributes[FONT_SIZE] = value;
                }
            }

            public override ContentType ContentType
            {
                get { return ContentType.Paragraph; }
            }

            public List<string> Rows { get { return rows; } }

            public string Text
            {
                get { return ToString(); }
                set
                {
                    rows.Clear();
                    currentRow.Clear();
                    Append(value);
                    SplitToRows();
                }
            }

            public Paragraph() { }

            public Paragraph(Paragraph other) : base(other)
            {
                rows = new List<string>(other.rows);
                currentRow = new StringBuilder(other.currentRow.ToString());
            }

            public void Append(string text, Vector start = null, Vector end = null)
            {
                if (rows.Count < 1)
                    NewRow();

                currentRow.Append(text);
                rows[rows.Count - 1] = currentRow.ToString();

                if (start != null)
                    box.AddPoint(start);
                if (end != null)
                    box.AddPoint(end);
            }

            public void NewRow()
            {
                rows.Add("");
                currentRow.Clear();
            }

            public void FixDiacritics()
            {
                for (int i = 0; i < rows.Count; i++)
                    rows[i] = EncodingTools.FixDiacritics(rows[i]).Trim();
            }

            public void SplitToRows()
            {
                string current = ToString();
                string[] newRows = current.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                rows = new List<string>(newRows);
            }

            public override string ToString()
            {
                StringBuilder output = new StringBuilder();
                foreach (string row in rows)
                {
                    output.Append(row);
                    output.AppendLine();
                }
                return output.ToString().Trim();
            }

        }

        public class Image : IRenderedObject
        {
            public override ContentType ContentType
            {
                get { return ContentType.Image; }
            }

            public Image() { }

            public Image(Image other) : base(other) { }

            public override string ToString()
            {
                return "image";
            }
        }

        public class Line : IRenderedObject
        {
            public override ContentType ContentType
            {
                get { return ContentType.Line; }
            }

            public Vector Start
            {
                get { return new Vector(box.MinX, box.MidY); }
            }

            public Vector End
            {
                get { return new Vector(box.MaxX, box.MidY); }
            }

            public Line() { }

            public Line(Line other) : base(other) { }

            public Line(Vector start, Vector end, float thickness)
            {
                box.AddPoint(new Vector(start.X, start.Y - thickness / 2f));
                box.AddPoint(new Vector(end.X, end.Y + thickness / 2f));
            }

            public override string ToString()
            {
                return "-----";
            }
        }

        public class Table : Page
        {

            public override ContentType ContentType
            {
                get { return ContentType.Table; }
            }

        }
    }
}
