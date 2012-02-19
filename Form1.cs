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
using Microsoft.WindowsAPICodePack.Taskbar;

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
        string consumerKey = "";
        string consumerSecret = "";

        TaskbarManager taskbar = TaskbarManager.Instance;
        DirectoryInfo tempFolder;
        string dateNow = DateTime.Now.ToString("yyyy-MM-dd");
        List<Entity.Notebook> enNotebooks;

        private delegate void intDelegate(int i);
        private delegate void boolDelegate(bool b);

        public Form1()
        {
            InitializeComponent();
            tbID.Text = Properties.Settings.Default.userid;
            tbSaveFolder.Text = Properties.Settings.Default.saveto;
        }

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
Version: 0.2.2
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
            enNotebooks = new List<Entity.Notebook>();
            tempFolder = new DirectoryInfo(Path.GetTempPath() + @"en2ki\" + DateTime.Now.ToString("yyyyMMddHHmmss")); 
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

            EnableControls(false);

            string[] inputs = new string[] { tbID.Text, tbPW.Text, tbSaveFolder.Text };
            Thread t = new Thread(new ParameterizedThreadStart(GetNotebooks));
            t.Start(inputs);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.userid = tbID.Text;
            Properties.Settings.Default.saveto = tbSaveFolder.Text;
            Properties.Settings.Default.Save();
        }

        #endregion

        public void GetNotebooks(object arguments)
        {
            string[] inputs = (string[])arguments;
            string username = inputs[0];
            string password = inputs[1];
            string savePath = inputs[2];
            string edamBaseUrl = @"https://www.evernote.com";

            UserStore.Client userStore = EvernoteHelper.GetUserStoreClient(edamBaseUrl);
            if (EvernoteHelper.VerifyEDAM(userStore) == false) return;

            try
            {
                AuthenticationResult authResult = EvernoteHelper.Authenticate(username, password, consumerKey, consumerSecret, "www.evernote.com", userStore);
                enNotebooks = ReadEvernoteNotebooks(edamBaseUrl, authResult);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                EnableControls(true);
                return;
            }

            if (rdoExportSelected.Checked)
            {
                NotebookSelection nbSelect = new NotebookSelection();
                nbSelect.ShowDialog(enNotebooks);
                enNotebooks = nbSelect.nbListKeep;
                if (enNotebooks.Count == 0)
                {
                    MessageBox.Show("There were no notebooks selected and will stop processing.", "Notebooks Not Selected", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EnableControls(true);
                    return;
                }
            }

            try
            {
                AuthenticationResult authResult = EvernoteHelper.Authenticate(username, password, consumerKey, consumerSecret, "www.evernote.com", userStore);
                enNotebooks = ReadEvernoteNotes(edamBaseUrl, authResult);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                EnableControls(true);
                return;
            }
            Create(arguments);
        }

        public void Create(object arguments)
        {
            string[] inputs = (string[])arguments;
            string username = inputs[0];
            string password = inputs[1];
            string savePath = inputs[2];
            int noteCount = Helper.GetNoteCount(enNotebooks);//max value to loop creating records/note files

            try
            {
                OpfWriter opf = new OpfWriter(tempFolder.FullName);
                if (!tempFolder.Exists) tempFolder.Create();

                UpdateProgress(83, "Creating NCX (1/5)");
                opf.CreateNCX(enNotebooks);
                UpdateProgress(86, "Creating TOC (2/5)");
                opf.CreateTOC(enNotebooks);
                UpdateProgress(89, "Creating Notes (3/5)");
                opf.CreateDataFiles(enNotebooks);
                UpdateProgress(92, "Creating OPF (4/5)");
                opf.CreateOPF(noteCount);
                UpdateProgress(95, "Creating MOBI (5/5)");

                int kindleResult = Helper.LaunchKindleGen(tempFolder);
                if (kindleResult == 2)
                {
                    throw new ApplicationException("KindleGen Exception. Please refer to online documents for troubleshooting.");
                }

                UpdateProgress(99, "Copying MOBI file");
                File.Copy(tempFolder + @"\en2ki.mobi", savePath + @"\en2ki.mobi", true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                EnableControls(true);
                return;
            }
            finally
            {
                tempFolder.Delete(true);
            }

            UpdateProgress(100, "Complete");
            ConfirmClose();
        }

        #region evernote data process
        internal List<Entity.Notebook> ReadEvernoteNotebooks(String edamBaseUrl, AuthenticationResult authResult)
        {
            string authToken = authResult.AuthenticationToken;
            NoteStore.Client noteStore = EvernoteHelper.GetNoteStoreClient(edamBaseUrl, authResult.User);
            List<Notebook> notebooks = noteStore.listNotebooks(authToken);
            UpdateProgress("Retrieving Notebook List");

            foreach (Notebook notebook in notebooks)
            {
                Entity.Notebook enNotebook = new Entity.Notebook();
                enNotebook.Name = (notebook.Stack + " " + notebook.Name).Trim();
                enNotebook.Guid = notebook.Guid;

                int intProgress = Helper.GetNotebookProgress(enNotebooks.Count, notebooks.Count, 1, 20);
                UpdateProgress(intProgress);

                NoteFilter nf = new NoteFilter();
                nf.NotebookGuid = enNotebook.Guid;

                NoteList nl = noteStore.findNotes(authToken, nf, 0, 1);
                if (nl.Notes.Count > 0)
                {
                    enNotebooks.Add(enNotebook);
                }
            }
            enNotebooks.Sort(delegate(Entity.Notebook p1, Entity.Notebook p2) { return p1.Name.CompareTo(p2.Name); });
            return enNotebooks;
        }

        internal List<Entity.Notebook> ReadEvernoteNotes(String edamBaseUrl, AuthenticationResult authResult)
        {
            string authToken = authResult.AuthenticationToken;
            NoteStore.Client noteStore = EvernoteHelper.GetNoteStoreClient(edamBaseUrl, authResult.User);
            List<Notebook> notebooks = noteStore.listNotebooks(authToken);

            int nbCount=1;
            foreach (Entity.Notebook enNotebook in enNotebooks)
            {
                int intProgress = Helper.GetNotebookProgress(nbCount++, enNotebooks.Count, 20, 60);
                UpdateProgress(intProgress);

                NoteFilter nf = new NoteFilter();
                nf.NotebookGuid = enNotebook.Guid;

                NoteList nl = noteStore.findNotes(authToken, nf, 0, 500);//500 notes limit per notebook
                foreach (Note note in nl.Notes)
                {
                    UpdateProgress(intProgress, "Retrieving " + enNotebook.Name + ", " + note.Title);
                    Entity.Note enNote = new Entity.Note();
                    enNote.Title = note.Title;
                    enNote.ShortDateString = note.Updated.ToString();

                    string enmlContent = noteStore.getNoteContent(authToken, note.Guid);
                    enNote.LoadXml(enmlContent);

                    if (enNotebook.Notes == null)
                        enNotebook.Notes = new List<Entity.Note>();
                    enNotebook.Notes.Add(enNote);
                }
            }
            enNotebooks.Sort(delegate(Entity.Notebook p1, Entity.Notebook p2) { return p1.Name.CompareTo(p2.Name); });
            return enNotebooks;
        }
#endregion

        #region ui/ux helper
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
            else
            {
                statusProgress.ProgressBar.Value = value;
            }
            taskbar.SetProgressValue(value, 100);
        }

        private void EnableControls(bool enable)
        {
            if (btnCreate.InvokeRequired)
            {
                boolDelegate bD = new boolDelegate(EnableControls);
                this.Invoke(bD, new object[] { enable });
            }
            else
            {
                btnCreate.Enabled = enable;
                gbEvernoteLogin.Enabled = enable;
                gbSaveTo.Enabled = enable;
                gbExport.Enabled = enable;
            }
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
            else
            {
                EnableControls(true);
            }
        }

        #endregion

    }
}