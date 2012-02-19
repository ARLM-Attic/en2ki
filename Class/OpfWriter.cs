using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Web;

namespace en2ki
{
    class OpfWriter
    {
        string _tempFolder;
        string dateNow;

        public OpfWriter(string tempFolderIncoming)
        {
            _tempFolder = tempFolderIncoming;
            dateNow = DateTime.Now.ToString("yyyy-MM-dd");
        }

       /// <summary>
        /// create NCX file (Navigation Control file for XML applications)
        /// </summary>
        /// <param name="enNotebooks"></param>
        /// <param name="tempFolder"></param>
        /// <returns></returns>
        internal void CreateNCX(List<Entity.Notebook> enNotebooks)
        {
            int noteCount = 1;
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(_tempFolder + "/nav-contents.ncx", false, Encoding.UTF8))
            {
                file.WriteLine(
@"<?xml version='1.0' encoding='utf-8' ?>
            <!DOCTYPE ncx PUBLIC '-//NISO//DTD ncx 2005-1//EN' 'http://www.daisy.org/z3986/2005/ncx-2005-1.dtd'>
            <ncx xmlns:mbp='http://mobipocket.com/ns/mbp' xmlns='http://www.daisy.org/z3986/2005/ncx/' version='2005-1' xml:lang='en-US'>
              <head>
                <meta content='en2ki_20110112' name='dtb:uid'/>
                <meta content='2' name='dtb:depth'/>
                <meta content='0' name='dtb:totalPageCount'/>
                <meta content='0' name='dtb:maxPageNumber'/>
              </head>
              <docTitle>
                <text>en2ki</text>
              </docTitle>
              <docAuthor>
                <text>en2ki</text>
              </docAuthor>
              <navMap>
                <navPoint class='periodical' id='periodical' playOrder='0'>
");
                //foreach notebook
                foreach (Entity.Notebook nb in enNotebooks)
                {
                    file.WriteLine(
@"                  <navPoint class='section' id=""" + nb.Name + @""" playOrder='" + noteCount.ToString() + @"' >
                    <navLabel>
                      <text>" + nb.Name + @"</text>
                    </navLabel>
                    <content src='" + noteCount.ToString() + @".html'/>");
                    //foreach note
                    if (nb.Notes != null)
                    {
                        foreach (Entity.Note n in nb.Notes)
                        {
                            string desc = HttpUtility.HtmlEncode(n.Text);
                            if (desc.Length > 20)
                            {
                                desc = desc.Substring(0, 20) + "...";
                            }

                            file.WriteLine(
@"                   <navPoint class='article' id='item-" + noteCount.ToString() + @"' playOrder='" + noteCount.ToString() + @"' >
                      <navLabel>
                        <text>" + HttpUtility.HtmlEncode(n.Title) + @"</text>
                      </navLabel>
                      <content src='" + noteCount.ToString() + @".html'/>
                      <mbp:meta name='description'>" + desc + @"</mbp:meta>
                      <mbp:meta name='author'>" + n.ShortDateString + @"</mbp:meta>
                    </navPoint>");
                            noteCount++;
                        }
                    }
                    file.WriteLine(
@"                </navPoint>");
                }
                file.WriteLine(
@"                  <navLabel>
                    <text>Table of Contents</text>
                  </navLabel>
                  <content src='contents.html'/>
	            </navPoint>
	
              </navMap>
            </ncx>
        ");
            }
        }

        internal void CreateTOC(List<Entity.Notebook> enNotebooks)
        {
            int noteCount = 1;
            //create content.html
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(_tempFolder + "/contents.html", false, Encoding.UTF8))
            {
                file.WriteLine(
@"<head><meta http-equiv='Content-Type' content='text/html;charset=UTF-8' /></head>
<body>
    <h1>Contents</h1>
");
                //foreach notebook
                foreach (Entity.Notebook nb in enNotebooks)
                {
                    file.WriteLine(
@"    <h4>" + nb.Name + @"</h4>
    <ul>
                ");
                    //foreach note
                    if (nb.Notes != null)
                    {
                        foreach (Entity.Note n in nb.Notes)
                        {
                            file.WriteLine(
@"      <li>
        <a href='" + noteCount.ToString() + @".html'>" + HttpUtility.HtmlEncode(n.Title) + @"</a>
      </li>
                    ");
                            noteCount++;
                        }
                    }
                    file.WriteLine(
@"    </ul>
                ");
                }
                file.WriteLine(
@"	</body>
");
            }
        }

        internal void CreateDataFiles(List<Entity.Notebook> enNotebooks)
        {
            //create data.html files
            int noteCount = 1;
            foreach (Entity.Notebook nb in enNotebooks)
            {
                if (nb.Notes != null)
                {
                    foreach (Entity.Note n in nb.Notes)
                    {
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(_tempFolder + "/" + noteCount.ToString() + @".html", false, Encoding.UTF8))
                        {
                            file.WriteLine(
@"<head><meta http-equiv='Content-Type' content='text/html;charset=UTF-8' /></head><body><h1>" + HttpUtility.HtmlEncode(n.Title) + "</h1>" + n.Xml + @"</body>
                        ");
                        }
                        noteCount++;
                    }
                }
            }
        }

        internal void CreateOPF(int noteCount)
        {
            //create opf files
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(_tempFolder + "/en2ki.opf", false, Encoding.UTF8))
            {
                file.WriteLine(
@"<?xml version='1.0' encoding='utf-8'?>
<package xmlns='http://www.idpf.org/2007/opf' version='2.0' unique-identifier='en2ki_" + dateNow + @"'>
  <metadata>
    <dc-metadata xmlns:dc='http://purl.org/dc/elements/1.1/'>
      <dc:title>en2ki.</dc:title>
      <dc:language>en-us</dc:language>
      <meta content='cover-image' name='cover'/>
      <dc:creator>en2ki </dc:creator>
      <dc:publisher>en2ki </dc:publisher>
      <dc:subject>en2ki </dc:subject>
      <dc:date>" + dateNow + @"</dc:date>
      <dc:description></dc:description>
    </dc-metadata>
    <x-metadata>
      <output content-type='application/x-mobipocket-subscription-magazine' encoding='utf-8'/>
    </x-metadata>
  </metadata>
  <manifest>
");
                for (int i = 1; i < noteCount; i++)
                {
                    file.WriteLine(
@"    <item href='" + i.ToString() + @".html' media-type='application/xhtml+xml' id='" + i.ToString() + @"'/>
");
                }
                file.WriteLine(
@"    <item href='contents.html' media-type='application/xhtml+xml' id='contents'/>
    <item href='nav-contents.ncx' media-type='application/x-dtbncx+xml' id='nav-contents'/>
  </manifest>
  
  <spine toc='nav-contents'>
    <itemref idref='contents'/>
");
                for (int i = 1; i < noteCount; i++)
                {
                    file.WriteLine(
@"    <itemref idref='" + i.ToString() + @"'/>
");
                }
                file.WriteLine(
@"  </spine>
  
  <guide>
    <reference href='contents.html' type='toc' title='Table of Contents'/>
  </guide>
</package>
");
            }
        }

    }
}