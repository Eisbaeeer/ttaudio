// Copyright (c) https://github.com/sidiandi 2016
// 
// This file is part of tta.
// 
// tta is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// tta is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Foobar.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using log4net.Layout;
using log4net.Core;
using RavSoft;
using ttaenc;

namespace ttaudio
{
    public partial class Editor : Form
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        readonly Document document;

        public Editor(Document document)
        {
            this.document = document;

            InitializeComponent();

            CueProvider.SetCue(textBoxTitle, "automatic");
            CueProvider.SetCue(textBoxProductId, "automatic");

            ConfigureLogging();

            New();
        }

        void ConfigureLogging()
        {
            log4net.Config.BasicConfigurator.Configure();

            /*
            var a = new TextboxAppender(textBoxLog)
            {
                Layout = new PatternLayout("%utcdate{ISO8601} %level %message%newline"),
                Threshold = Level.Info,
                Name = textBoxLog.Name,
            };
            a.ActivateOptions();
            log4net.Config.BasicConfigurator.Configure(a);
            */
        }

        private void listViewInputFiles_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                Add(files);
            }
        }

        /// <summary>
        /// Add input files to the list view
        /// </summary>
        /// <param name="inputFiles"></param>
        public void Add(IEnumerable<string> inputFiles)
        {
            foreach (var audioFile in AlbumReader.GetAudioFiles(inputFiles))
            {
                this.listViewInputFiles.Items.Add(new ListViewItem(audioFile)
                {
                    Tag = audioFile
                });
            }
            this.listViewInputFiles.Columns[this.listViewInputFiles.Columns.Count - 1].Width = -1;
        }

        private void listViewInputFiles_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void buttonConvert_Click(object sender, EventArgs e)
        {
            StartConversion();
        }

        Task Convert(CancellationToken cancel, IList<string> files, string title, string productId)
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    var dataDirectory = AlbumMaker.GetDefaultDataDirectory();
                    var albumMaker = new AlbumMaker(dataDirectory);
                    var collection = new AlbumReader().FromTags(files);
                    if (!String.IsNullOrEmpty(title))
                    {
                        collection.Title = title;
                    }
                    if (!String.IsNullOrEmpty(productId))
                    {
                        collection.ProductId = UInt16.Parse(productId);
                    }

                    albumMaker.Create(cancel, collection).Wait();
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }, cancel);
        }

        void StartConversion()
        {
            var files = listViewInputFiles.Items.Cast<ListViewItem>().Select(_ => (string)_.Tag).ToList();
            if (!files.Any())
            {
                MessageBox.Show("Drop some audio files into the list first.");
                return;
            }

            var cancellationTokenSource = new System.Threading.CancellationTokenSource();
            var task = Convert(cancellationTokenSource.Token, files, textBoxTitle.Text, textBoxProductId.Text);

            var taskForm = new TaskForm(task, cancellationTokenSource)
            {
                Text = "Convert and Copy to Pen"
            };
                
            taskForm.Show();

            New();
        }

        private void buttonStartNewConversion_Click(object sender, EventArgs e)
        {
            New();
        }

        void New()
        {
            textBoxTitle.Text = String.Empty;
            textBoxProductId.Text = String.Empty;
            this.listViewInputFiles.Items.Clear();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(About.GitUri.ToString());
        }

        private void exploreDataDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", AlbumMaker.GetDefaultDataDirectory().Quote());
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        void DeleteSelected()
        {
            var remainingItems = listViewInputFiles.Items.Cast<ListViewItem>().Where(_ => !_.Selected).ToArray();
            listViewInputFiles.Items.Clear();
            listViewInputFiles.Items.AddRange(remainingItems);
        }

        void SelectAll()
        {
            foreach (ListViewItem i in listViewInputFiles.Items)
            {
                i.Selected = true;
            }
        }

        private void listViewInputFiles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.A:
                        SelectAll();
                        break;
                }
            }
            else
            {
                switch (e.KeyCode)
                {
                    case Keys.Delete:
                        DeleteSelected();
                        break;
                }
            }
        }
    }
}