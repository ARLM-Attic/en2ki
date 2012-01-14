using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Threading;
using System.Configuration;

using Thrift;
using Thrift.Protocol;
using Thrift.Transport;
using Evernote.EDAM.Type;
using Evernote.EDAM.UserStore;
using Evernote.EDAM.NoteStore;
using Evernote.EDAM.Error;

namespace en2ki
{
    public partial class Form1 : Form
    {
        String consumerKey = "";//to fill
        String consumerSecret = "";//to fill
        public Form1()
        {
            InitializeComponent();
            tbID.Text = Properties.Settings.Default.userid;
            tbSaveFolder.Text = Properties.Settings.Default.saveto;
        }

        DirectoryInfo tempFolder = new DirectoryInfo(Path.GetTempPath() + "/en2ki/" + DateTime.Now.ToString("yyyyMMddHHmmss"));
        string dateNow = DateTime.Now.ToString("yyyy-MM-dd");

        #region eventHandler
        private void btnSaveFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                tbSaveFolder.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult dlgRes = MessageBox.Show(@"
en2ki: Evernote to Kindle
Version: 0.1
Press [OK] button below to visit homepage for more information",
                 "About en2ki",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);
            if (dlgRes == DialogResult.OK)
            {
                Process.Start("http://en2ki.codeplex.com/documentation/");
            }
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            ValidateThenStart(sender, e);
        }

        private void ValidateThenStart(object sender, EventArgs e)
        {
            if (tbID.Text.Length == 0)
            {
                MessageBox.Show("User ID is required", "Required: User ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tbID.Focus();
                return;
            }
            else if (tbPW.Text.Length == 0)
            {
                MessageBox.Show("Password is required", "Required: Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tbPW.Focus();
                return;
            }
            else if (tbSaveFolder.Text.Length == 0)
            {
                MessageBox.Show("Destination (Save To) Folder is required", "Required: Destination Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnSaveFolder_Click(sender, e);
                return;
            }

            DirectoryInfo di = new DirectoryInfo(tbSaveFolder.Text);
            if (!di.Exists)
            {
                MessageBox.Show("Destination (Save To) Folder not found", "Not Found: Destination Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string[] inputs = new string[] { tbID.Text, tbPW.Text, tbSaveFolder.Text };
            Thread t = new Thread(new ParameterizedThreadStart(Start));
            t.Start(inputs);
        }

        private void ConfirmClose()
        {
            DialogResult dlgRes = MessageBox.Show(@"Process Complete. 
Would you like to close en2ki application and open the output folder?",
                 "Complete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (dlgRes == DialogResult.Yes)
            {
                Process.Start(tbSaveFolder.Text);
                Application.Exit();
            }
        }
#endregion

        public void Start(object arguments)
        {
            string[] inputs = (string[])arguments;
            string username = inputs[0];
            string password = inputs[1];
            string savePath = inputs[2];

            String evernoteHost = "www.evernote.com";
            String edamBaseUrl = "https://" + evernoteHost;

            Uri userStoreUrl = new Uri(edamBaseUrl + "/edam/user");
            TTransport userStoreTransport = new THttpClient(userStoreUrl);
            TProtocol userStoreProtocol = new TBinaryProtocol(userStoreTransport);
            UserStore.Client userStore = new UserStore.Client(userStoreProtocol);

            UpdateProgress("Starting");
            if (VerifyEDAM(userStore))
            {
                AuthenticationResult authResult = Authenticate(username, password, consumerKey, consumerSecret, evernoteHost, userStore);

                if (authResult == null)
                    return;

                int noteCount = 1;
                List<Entity.Notebook> enNotebooks;
                try
                {
                    enNotebooks = ReadEvernote(edamBaseUrl, authResult);
                    noteCount = GetNoteCount(enNotebooks);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    if (!tempFolder.Exists) tempFolder.Create();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                UpdateProgress(83, "Creating NCX (1/5)");
                try
                {
                    CreateNCX(enNotebooks);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                UpdateProgress(86, "Creating TOC (2/5)");
                try
                {
                    CreateTOC(enNotebooks);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                UpdateProgress(89, "Creating Notes (3/5)");
                try
                {
                    CreateDataFiles(enNotebooks);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                UpdateProgress(92, "Creating OPF (4/5)");
                try
                {
                    CreateOPF(noteCount);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                UpdateProgress(95, "Creating MOBI (5/5)");
                try
                {
                    int kindleResult = LaunchKindleGen();
                    if (kindleResult == 2)
                    {
                        throw new Exception("KindleGen Exception");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                UpdateProgress(98, "Copying MOBI file");
                try
                {
                    File.Copy(tempFolder + @"\en2ki.mobi", savePath + @"\en2ki.mobi", true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                UpdateProgress(100, "Complete");
                ConfirmClose();
            }
        }

        #region processor
        /// <summary>
        /// create NCX file (Navigation Control file for XML applications)
        /// </summary>
        /// <param name="enNotebooks"></param>
        /// <param name="tempFolder"></param>
        /// <returns></returns>
        private bool VerifyEDAM(UserStore.Client userStore)
        {
            bool versionOK =
                userStore.checkVersion("C# EDAMTest",
                   Evernote.EDAM.UserStore.Constants.EDAM_VERSION_MAJOR,
                   Evernote.EDAM.UserStore.Constants.EDAM_VERSION_MINOR);
            Console.WriteLine("Is my EDAM protocol version up to date? " + versionOK);
            return versionOK;
        }

        private AuthenticationResult Authenticate(String username, String password, String consumerKey, String consumerSecret, String evernoteHost, UserStore.Client userStore)
        {
            AuthenticationResult authResult = null;
            try
            {
                authResult = userStore.authenticate(username, password, consumerKey, consumerSecret);
            }
            catch (EDAMUserException ex)
            {
                String parameter = ex.Parameter;
                EDAMErrorCode errorCode = ex.ErrorCode;

                if (parameter.ToLower() == "consumerkey")
                {
                    MessageBox.Show(String.Format("API Key Missing. \r\n Please download latest release from homepage", parameter), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    statusLabel.Text = "Authentication Failed, " + parameter + ": " + errorCode;
                    MessageBox.Show(String.Format("Authentication Failed \r\n (Make sure {0} is correct)", parameter), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            return authResult;
        }

        private List<Entity.Notebook> ReadEvernote(String edamBaseUrl, AuthenticationResult authResult)
        {
            User user = authResult.User;
            String authToken = authResult.AuthenticationToken;
            statusLabel.Text = "Authentication successful for: " + user.Username;
            //Console.WriteLine("Authentication token = " + authToken);

            Uri noteStoreUrl = new Uri(edamBaseUrl + "/edam/note/" + user.ShardId);
            TTransport noteStoreTransport = new THttpClient(noteStoreUrl);
            TProtocol noteStoreProtocol = new TBinaryProtocol(noteStoreTransport);
            NoteStore.Client noteStore = new NoteStore.Client(noteStoreProtocol);

            List<Notebook> notebooks = noteStore.listNotebooks(authToken);
            Notebook defaultNotebook = notebooks[0];

            List<Entity.Notebook> enNotebooks = new List<Entity.Notebook>();

            foreach (Notebook notebook in notebooks)
            {
                //if (enNotebooks.Count > 5) return enNotebooks;//debug
                Entity.Notebook enNotebook = new Entity.Notebook();
                enNotebook.Name = notebook.Stack + " " + notebook.Name;
                UpdateProgress(1 + Convert.ToInt16((Convert.ToDouble(enNotebooks.Count) / Convert.ToDouble(notebooks.Count)) * 80), "Reading (" + (enNotebooks.Count + 1) + "/" + notebooks.Count + "): " + enNotebook.Name);

                NoteFilter nf = new NoteFilter();
                nf.NotebookGuid = notebook.Guid;

                NoteList nl = noteStore.findNotes(authToken, nf, 0, 500);
                foreach (Note note in nl.Notes)
                {
                    Entity.Note enNote = new Entity.Note();
                    enNote.Title = note.Title;
                    enNote.ShortDateString = note.Updated.ToString();

                    Console.WriteLine("  " + note.Title + ": ");
                    string enmlContent = noteStore.getNoteContent(authToken, note.Guid);
                    enmlContent = enmlContent.Replace("\r\n", "");
                    enmlContent = enmlContent.Replace("<!DOCTYPE en-note SYSTEM \"http://xml.evernote.com/pub/enml2.dtd\">", "");
                    enmlContent = enmlContent.Replace("&nbsp;", " ");
                    enmlContent = enmlContent.Replace("&", "&amp;");
                    {
                        XmlDocument xDoc = new XmlDocument();
                        xDoc.LoadXml(enmlContent);
                        XmlNode xn = xDoc.SelectSingleNode("en-note");
                        enNote.Text = xn.InnerText;
                        enNote.Xml = xn.InnerXml.Replace("<span> </span>", " ");
                        enNote.Xml = xn.InnerXml.Replace("<img style=\"cursor: default; vertical-align: middle;\" />", " ");
                    }

                    if (enNotebook.Notes == null)
                        enNotebook.Notes = new List<Entity.Note>();
                    enNotebook.Notes.Add(enNote);
                }
                if (enNotebook.Notes != null)
                {
                    enNotebooks.Add(enNotebook);
                }
            }

            enNotebooks.Sort(delegate(Entity.Notebook p1, Entity.Notebook p2) { return p1.Name.CompareTo(p2.Name); });

            return enNotebooks;
        }

        private void CreateNCX(List<Entity.Notebook> enNotebooks)
        {
            int noteCount = 1;
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(tempFolder + "/nav-contents.ncx"))
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
                            string desc = n.Text;
                            if (desc.Length > 20)
                            {
                                desc = desc.Substring(0, 20) + "...";
                            }

                            file.WriteLine(
@"                   <navPoint class='article' id='item-" + noteCount.ToString() + @"' playOrder='" + noteCount.ToString() + @"' >
                      <navLabel>
                        <text>" + n.Title + @"</text>
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

        private void CreateTOC(List<Entity.Notebook> enNotebooks)
        {
            int noteCount = 1;
            //create content.html
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(tempFolder + "/contents.html"))
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
        <a href='" + noteCount.ToString() + @".html'>" + n.Title + @"</a>
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

        private void CreateDataFiles(List<Entity.Notebook> enNotebooks)
        {
            //create data.html files
            int noteCount = 1;
            foreach (Entity.Notebook nb in enNotebooks)
            {
                if (nb.Notes != null)
                {
                    foreach (Entity.Note n in nb.Notes)
                    {
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(tempFolder + "/" + noteCount.ToString() + @".html"))
                        {
                            file.WriteLine(
@"<head><meta http-equiv='Content-Type' content='text/html;charset=UTF-8' /></head><body><h1>" + n.Title + "</h1>" + n.Xml + @"</body>
                        ");
                        }
                        noteCount++;
                    }
                }
            }
        }

        private void CreateOPF(int noteCount)
        {
            //create opf files
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(tempFolder + "/en2ki.opf"))
            {
                file.WriteLine(
@"<?xml version='1.0' encoding='utf-8'?>
<package xmlns='http://www.idpf.org/2007/opf' version='2.0' unique-identifier='en2ki_" + dateNow + @"'>
  <metadata>
    <dc-metadata xmlns:dc='http://purl.org/dc/elements/1.1/'>
      <dc:title>en2ki.</dc:title>
      <dc:language>en-us</dc:language>
      <meta content='cover-image' name='cover'/>
      <dc:creator>en2ki</dc:creator>
      <dc:publisher>en2ki</dc:publisher>
      <dc:subject>News</dc:subject>
      <dc:date>" + dateNow + @"</dc:date>
      <dc:description>Evernote to Kindle 0.1</dc:description>
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
        #endregion

        private int LaunchKindleGen()
        {
            FileInfo fileApp = new FileInfo(tempFolder + "/kindlegen.exe");
            File.Copy("kindlegen.exe", fileApp.FullName);
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            //p.StartInfo.UseShellExecute = false;
            //p.StartInfo.RedirectStandardOutput = true; //false for show command prompt output
            p.StartInfo.FileName = fileApp.FullName;
            p.StartInfo.WorkingDirectory = fileApp.DirectoryName;
            p.StartInfo.Arguments = "en2ki.opf";
            p.Start();
            p.WaitForExit();
            return p.ExitCode;

        }
        #region helper
        public byte[] ReadFully(Stream stream)
        {
            byte[] buffer = new byte[32768];
            using (MemoryStream ms = new MemoryStream())
            {
                while (true)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                    {
                        return ms.ToArray();
                    }
                    ms.Write(buffer, 0, read);
                }
            }
        }

        private int GetNoteCount(List<Entity.Notebook> enNotebooks)
        {
            int noteCount = 1;
            foreach (Entity.Notebook nb in enNotebooks)
            {
                if (nb.Notes != null)
                {
                    foreach (Entity.Note n in nb.Notes)
                    {
                        noteCount++;
                    }
                }
            }
            return noteCount;
        }
        private delegate void intDelegate(int i);
        private void UpdateProgress(string text)
        {
            statusLabel.Text = text;
        }
        private void UpdateProgress(int value, string text)
        {
            statusLabel.Text = value + "% " + text;
            UpdateProgress(value);
        }
        private void UpdateProgress(int value)
        {
            if (statusProgress.ProgressBar.InvokeRequired)
            {
                intDelegate id = new intDelegate(UpdateProgress);
                this.Invoke(id, new object[] { value });
            }
            statusProgress.ProgressBar.Value = value;
        }

        #endregion

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.userid = tbID.Text;
            Properties.Settings.Default.saveto = tbSaveFolder.Text;
            Properties.Settings.Default.Save();   
        }


    }
}