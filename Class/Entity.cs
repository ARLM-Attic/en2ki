using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Xml;

namespace en2ki.Entity
{

    public class Note
    {
        public string Title {get;set;}
        public string Text { get; set;}
        public string Xml { get; set; }
        public string ShortDateString { get; set; }

        public void LoadXml(string enmlContent)
        {
            enmlContent = enmlContent.Replace("\r\n", "");//causes unexpected line breaks to parse xml lines
            enmlContent = enmlContent.Replace("<!DOCTYPE en-note SYSTEM \"http://xml.evernote.com/pub/enml2.dtd\">", ""); //slows down app for doc validation
            enmlContent = enmlContent.Replace("&nbsp;", "&#160;");//http://changelog.ca/log/2006/06/12/making_nbsp_work_with_xml_rss_and_atom

            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(enmlContent);
            XmlNode xn = xDoc.SelectSingleNode("en-note");
            string content = xn.InnerXml.Replace("<img style=\"cursor: default; vertical-align: middle;\" />", " ").Replace("<span> </span>", " ");

            Text = xn.InnerText;
            Xml = content;
            //Xml = System.Web.HttpUtility.HtmlDecode(content); //tag in xml causes kindlegen.exe throwing exceptions?
        }
    }

    public class Notebook : IComparer
    {
        public string Name { get; set; }
        public List<Note> Notes{get;set;}
        public string Guid { get; set; }

        int IComparer.Compare(Object x, Object y)
        {
            return ((new CaseInsensitiveComparer()).Compare(y, x));
        }
    }
    

}
