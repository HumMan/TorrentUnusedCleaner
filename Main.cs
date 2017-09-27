using Alphaleonis.Win32.Filesystem;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace TorrentUnusedCleaner
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();

            TryFindDirs();
        }

        public void TryFindDirs()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"qBittorrent\BT_backup\");
            if (Directory.Exists(folder))
                textBox1.Text = folder;

            var iniFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"qBittorrent\qBittorrent.ini");
            if(File.Exists(iniFile))
            {
                var content = File.ReadAllLines(iniFile);
                var targetString = @"Downloads\SavePath=";
                foreach (var l in content)
                    if(l.StartsWith(targetString))
                    {
                        textBox2.Text = l.Substring(targetString.Length);
                        break;
                    }
            }

            var uTorrentResumeFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"uTorrent\resume.dat");
            if (File.Exists(uTorrentResumeFile))
            {
                textBox9.Text = uTorrentResumeFile;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            tabControl1.Enabled = false;
            startButton.Enabled = false;
            stopButton.Enabled = true;
            progressBar1.Enabled = true;
            progressBar1.Value = 0;

            if (backgroundWorker1.IsBusy != true)
            {
                // Start the asynchronous operation.
                backgroundWorker1.RunWorkerAsync(tabControl1.SelectedTab.Name);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            tabControl1.Enabled = true;
            startButton.Enabled = true;
            stopButton.Enabled = false;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            switch (e.Argument as string)
            {
                case "qBittorent":
                    TorrentUnusedCleaner.QBittorentFindUnusedFiles(textBox2.Text, textBox1.Text, backgroundWorker1.ReportProgress);
                    break;
                case "uTorrent":
                    TorrentUnusedCleaner.UTorrentFindUnusedFiles(textBox3.Text, textBox10.Text, textBox9.Text, backgroundWorker1.ReportProgress);
                    break;
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.Enabled = false;
            tabControl1.Enabled = true;
            startButton.Enabled = true;
            stopButton.Enabled = false;
        }

        private void button13_Click(object sender, EventArgs e)
        {
            SelectFolder(textBox3);
        }

        private void SelectFolder(TextBox result)
        {
            if(folderBrowserDialog1.ShowDialog()==DialogResult.OK)
            {
                result.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void SelectFile(TextBox result)
        {
            if(openFileDialog1.ShowDialog()==DialogResult.OK)
            {
                result.Text = openFileDialog1.FileName;
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            SelectFolder(textBox10);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            SelectFile(textBox9);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SelectFolder(textBox1);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SelectFolder(textBox2);
        }
    }
}
