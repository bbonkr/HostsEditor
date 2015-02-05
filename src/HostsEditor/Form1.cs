using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HostsEditor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            this.Setup();

            this.Load += (s, e) =>
            {
                try
                {
                    this.LoadContent();
                }
                catch (Exception ex)
                {
                    // log
                    this.Log(ex);
                }
            };

            this.mainTextBox.TextChanged += (s, e) =>
            {
                //this.SetTextColor();
                this.ContentChanged = true;
                this.SetStatusLabel("Modified");
            };

            this.saveToolStripMenuItem.Click += (s, e) => { RunSave(); };

            this.exitToolStripMenuItem.Click += (s, e) => { this.Close(); };

            this.reloadToolStripMenuItem.Click += (s, e) => { this.LoadContent(); };

            this.FormClosing += (s, e) =>
            {
                if (this.ContentChanged)
                {
                    DialogResult dialogResult = MessageBox.Show("The content was modified. Do want to cancel the modification?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (DialogResult.Yes != dialogResult)
                    {
                        e.Cancel = true;
                    }
                }
            };

            this.wordWrapToolStripMenuItem.Click += (s, e) =>
            {
                ToolStripMenuItem thisControl = (ToolStripMenuItem)s;
                thisControl.Checked = !thisControl.Checked;
                Properties.Settings.Default.WordWrap = thisControl.Checked;
                Properties.Settings.Default.Save();
            };

            this.wordWrapToolStripMenuItem.CheckedChanged += (s, e) =>
            {
                ToolStripMenuItem thisControl = (ToolStripMenuItem)s;
                this.mainTextBox.WordWrap = thisControl.Checked;
            };

            this.useBackupToolStripMenuItem.Click += (s, e) =>
            {
                ToolStripMenuItem thisControl = (ToolStripMenuItem)s;
                thisControl.Checked = !thisControl.Checked;
                Properties.Settings.Default.UseBackup = thisControl.Checked;
                Properties.Settings.Default.Save();
            };

            this.openBackupFolderToolStripMenuItem.Click += (s, e) =>
            {
                string strBackupPath = this.GetBackupPath();
                if (!Directory.Exists(strBackupPath))
                {
                    Directory.CreateDirectory(strBackupPath);
                }

                Process.Start(strBackupPath);
            };
        }

        private void Setup()
        {
            this.Size = new Size(600, 480);
            Properties.Settings.Default.Reload();

            this.wordWrapToolStripMenuItem.Checked = Properties.Settings.Default.WordWrap;
            this.useBackupToolStripMenuItem.Checked = Properties.Settings.Default.UseBackup;

            this.Font = new Font(System.Drawing.SystemFonts.DefaultFont, FontStyle.Regular);
            this.mainTextBox.WordWrap = this.wordWrapToolStripMenuItem.Checked;
        }

        private void Log(object obj)
        {
            // Do nothing.
        }

        private void LoadContent()
        {
            string strFilepath = this.GetFilepath();
            //FileInfo hostsFile = new FileInfo(strFilepath);
            // Remove Tab char
            string strContent = File.ReadAllText(strFilepath);
            //if (!string.IsNullOrEmpty(strContent)) { strContent = strContent.Replace("\t", "".PadLeft(4, ' ')); }
            this.mainTextBox.Text = strContent;

            //this.SetTextColor();
            this.ContentChanged = false;
            this.SetStatusLabel("Ready");
        }

        private void SetTextColor()
        {
            int nCurrentIndex = this.mainTextBox.SelectionStart;
            Color commentColor = Color.DarkGreen;
            string strNewLine = System.Environment.NewLine;

            int nIndex = 0;
            int nLineLength = 0;

            this.mainTextBox.AutoWordSelection = false;

            foreach (string str in this.mainTextBox.Lines)
            {
                nLineLength = str.Length;

                if (str.StartsWith("#"))
                {
                    this.mainTextBox.Select(nIndex, nLineLength);
                    this.mainTextBox.SelectionColor = commentColor;
                }

                nIndex += (nLineLength - 1 + strNewLine.Length);
            }

            this.mainTextBox.Select(nCurrentIndex, 0);
        }

        private string GetFilepath()
        {
            string strWindowsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows);
            string strFilepath = string.Format(@"{0}\system32\drivers\etc\hosts", strWindowsPath);
            return strFilepath;
        }

        private string GetBackupPath()
        {
            string strApplicaionDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string strBackupPath = string.Format(@"{0}\{1}\{2}\backup", strApplicaionDataPath, this.CompanyName, this.ProductName);
            return strBackupPath;
        }

        private bool Backup(out string message)
        {
            message = string.Empty;
            // ****************************************************
            // Backup file format
            // hosts-yyyy-MM-dd-nnnnn
            // ****************************************************
            string strBackupFilePrefix = "hosts";
            int nMaxWhileLoop = 10000;
            int nWhileLoop = 0;
            string strBackupPath = this.GetBackupPath();

            if (!Directory.Exists(strBackupPath)) { Directory.CreateDirectory(strBackupPath); }

            string[] files = Directory.GetFiles(strBackupPath, string.Format("{0}-{1:yyyy-MM-dd}*", strBackupFilePrefix, DateTime.Now));

            int nFileCount = files.Length;
            string strBackupFilename = string.Empty;
            int maxSeq = 0;

            while (true)
            {
                if (nFileCount > 0)
                {
                    string strLastFile = files.OrderByDescending(f => f.Substring(f.Length - 5, 5)).First();
                    int.TryParse(strLastFile.Substring(strLastFile.Length - 5, 5), out maxSeq);
                }

                nFileCount++;
                maxSeq++;

                strBackupFilename = string.Format("{0}\\{1}-{2:yyyy-MM-dd}-{3}", strBackupPath, strBackupFilePrefix, DateTime.Now, maxSeq.ToString().PadLeft(5, '0'));
                if (!File.Exists(strBackupFilename))
                {
                    File.Copy(this.GetFilepath(), strBackupFilename);
                    if (File.Exists(strBackupFilename)) { break; }
                }

                nWhileLoop++;
                if (nWhileLoop > nMaxWhileLoop)
                {
                    message = string.Format("Try {0:n0} times, but could not save backup file.", nWhileLoop);
                    return false;
                }
            }

            return true;
        }

        private bool Save(out string message)
        {
            message = string.Empty;
            string strFilepath = this.GetFilepath();
            try
            {
                if (this.useBackupToolStripMenuItem.Checked)
                {
                    if (!this.Backup(out message)) { throw new Exception(message); }
                }
                File.WriteAllText(strFilepath, this.mainTextBox.Text);
                this.ContentChanged = false;
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                this.Log(ex);
                return false;
            }
        }

        private void RunSave()
        {
            string strMessage = string.Empty;
            //DialogResult dialogResult = MessageBox.Show("Do you want to Save hosts file?\r\n* Caution Overwrite it.", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            //if (DialogResult.Yes == dialogResult)
            //{
            //    if (this.Save(out strMessage))
            //    {
            //        // saved
            //        this.SetStatusLabel("Saved.");
            //    }
            //    else
            //    {
            //        // Could not save
            //        this.SetStatusLabel("Could not save the file.");
            //    }
            //}

            // I don't want to confirm windows.
            if (this.Save(out strMessage))
            {
                // saved
                this.SetStatusLabel("Saved.");
            }
            else
            {
                // Could not save
                this.SetStatusLabel("Could not save the file.");
            }
        }

        private void SetStatusLabel(string message)
        {
            this.toolStripStatusLabel.Text = message;
        }

        private void CanSave()
        {
            this.saveToolStripMenuItem.Enabled = this.ContentChanged;
        }

        private bool _TextChanged = false;

        private bool ContentChanged
        {
            set
            {
                this._TextChanged = value;
                this.CanSave();
            }
            get { return this._TextChanged; }
        }
    }
}