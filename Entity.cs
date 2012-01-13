using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
namespace Entity
{
    public class Note
    {
        public string Title {get;set;}
        public string Text { get; set; }
        public string Xml { get; set; }
        public string ShortDateString { get; set; }
    }

    public class Notebook : IComparer
    {
        public string Name { get; set; }
        public List<Note> Notes{get;set;}

        int IComparer.Compare(Object x, Object y)
        {
            return ((new CaseInsensitiveComparer()).Compare(y, x));
        }
    }
    

}
