using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using en2ki.Entity;

namespace en2ki
{
    public partial class NotebookSelection : Form
    {
        internal List<Notebook> nbListIncoming;
        internal List<Notebook> nbListKeep = new List<Notebook>();

        public NotebookSelection()
        {
            InitializeComponent();
        }

        internal void ShowDialog(List<Entity.Notebook> inc)
        {
            nbListIncoming = inc;
            foreach (Notebook nb in nbListIncoming)
            {
                cbNotebookList.Items.Add(nb.Name);
            }
            base.ShowDialog();

        }

        private void cbAll_CheckedChanged(object sender, EventArgs e)
        {
            for (int i=0;i<cbNotebookList.Items.Count;i++)
            {
                cbNotebookList.SetItemChecked(i, cbAll.Checked);
            }
        }

        private void btnContinue_Click(object sender, EventArgs e)
        {
            if (cbNotebookList.CheckedItems.Count > 0)
            {
                CopyNotebooks();
                this.Close();
            }
            else
            {
                MessageBox.Show("Check at least one notebook to continue", "Check at least one notebook to continue", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CopyNotebooks()
        {
            for (int i = 0; i < cbNotebookList.CheckedItems.Count; i++)
            {
                Notebook nb = new Notebook();
                nb.Name = cbNotebookList.CheckedItems[i].ToString();
                nbListKeep.Add(nb);
            }

            foreach (Notebook nbInc in nbListIncoming)
            {
                foreach (Notebook nbKeep in nbListKeep)
                {
                    if (nbKeep.Name == nbInc.Name)
                    {
                        nbKeep.Guid = nbInc.Guid;
                    }
                }
            }
        }
    }
}