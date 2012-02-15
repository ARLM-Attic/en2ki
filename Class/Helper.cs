using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace en2ki
{
    internal class Helper
    {
        internal static int GetNoteCount(List<Entity.Notebook> enNotebooks)
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

        internal static int GetNotebookProgress(int currentCount, int totalCount, int min, int max)
        {
            return min + Convert.ToInt16((Convert.ToDouble(currentCount) / Convert.ToDouble(totalCount)) * max);
        }

        internal static int LaunchKindleGen(DirectoryInfo tempFolder)
        {
            FileInfo fileApp = new FileInfo(tempFolder + "/kindlegen.exe");
            File.Copy("kindlegen.exe", fileApp.FullName);
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.RedirectStandardError = true;

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            p.StartInfo.CreateNoWindow = true; 
            
            p.StartInfo.FileName = fileApp.FullName;
            p.StartInfo.WorkingDirectory = fileApp.DirectoryName;
            p.StartInfo.Arguments = "en2ki.opf";

            p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_ErrorDataReceived);
            //p.EnableRaisingEvents = true;

            p.Start();
            p.BeginErrorReadLine();
            p.WaitForExit();

            return p.ExitCode;
        }

        static void p_ErrorDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            if (e.Data != null && e.Data.Length > 0)
            {
                //throw new ApplicationException(e.Data);//could not make it work
            }
        }

    }
}