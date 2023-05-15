using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UZ.PDF.Objects
{
    class PdfDictionary : PdfObject
    {
        protected Dictionary<string, PdfObject> objects = new Dictionary<string, PdfObject>();
        
        public PdfDictionary()
            :base(ObjectType.DICTIONARY, "Dictionary") { }

        protected PdfDictionary(ObjectType type, string content) 
            :base(type, content) { }

        public Dictionary<string, PdfObject> Objects
        {
            get { return objects; }
        }

        public void Set(PdfName key, PdfObject value)
        {
            if (value == null || value.Type == ObjectType.NULL)
                objects.Remove(key.ToString());
            else
                objects[key.ToString()] = value;
        }

        public bool ContainsKey(string key)
        {
            return objects.ContainsKey(key);
        }

        public PdfObject Get(string key)
        {
            PdfObject obj;
            Assert(objects.TryGetValue(key, out obj), "Invalid dictionary index: " + key);
                return obj;
        }

        public void Merge(PdfDictionary other)
        {
            foreach (string key in other.Objects.Keys)
                objects[key] = other.Objects[key];
        }
    }
}
