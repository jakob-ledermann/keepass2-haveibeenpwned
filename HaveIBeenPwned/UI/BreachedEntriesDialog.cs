﻿using HaveIBeenPwned.BreachCheckers;
using KeePass.Plugins;
using KeePassExtensions;
using KeePassLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace HaveIBeenPwned.UI
{
    public partial class BreachedEntriesDialog : Form
    {
        private IPluginHost pluginHost;

        public BreachedEntriesDialog(IPluginHost pluginHost)
        {
            this.pluginHost = pluginHost;
            InitializeComponent();
            this.Icon = pluginHost.MainWindow.Icon;
        }

        public void AddBreaches(IList<BreachedEntry> breaches)
        {
            var missingEntryGroup = new ListViewGroup("* Entries not in database", HorizontalAlignment.Left);
            this.Text = string.Format(this.Text, breaches.Count, breaches.Count > 1 ? "Entries" : "Entry");

            breachedEntryList.Items.Clear();
            breachedEntryList.Groups.Clear();
            breachedEntryList.SmallImageList = new ImageList();
            var groupNames = breaches.Where(b => b.Entry != null).Select(b => b.Entry.ParentGroup.GetFullPath(" - ", false)).Distinct();
            foreach(var group in groupNames)
            {
                breachedEntryList.Groups.Add(new ListViewGroup(group, HorizontalAlignment.Left));
            }
            breachedEntryList.Groups.Add(missingEntryGroup);
            breachedEntryList.ShowGroups = true;
            foreach (var breach in breaches)
            {
                breachedEntryList.SmallImageList.Images.Add(breach.Entry != null ? breach.Entry.GetIcon(pluginHost) : Resources.hibp.ToBitmap());
                var newItem = new ListViewItem(new[]
                {
                    breach.Entry != null ? breach.Entry.Strings.ReadSafe(PwDefs.TitleField) : breach.BreachName,
                    breach.Entry != null ? breach.Entry.Strings.ReadSafe(PwDefs.UserNameField) : breach.BreachUsername,
                    breach.Entry != null ? breach.Entry.Strings.ReadSafe(PwDefs.UrlField) : breach.BreachUrl,
                    breach.Entry != null ? breach.Entry.GetPasswordLastModified().ToShortDateString() : null,
                    breach.BreachName,
                    breach.BreachDate.ToShortDateString()
                })
                {
                    Tag = breach.Entry,
                    ImageIndex = breachedEntryList.SmallImageList.Images.Count - 1
                };

                if (breach.Entry != null)
                {
                    foreach (ListViewGroup group in breachedEntryList.Groups)
                    {
                        if (group.Header == breach.Entry.ParentGroup.GetFullPath(" - ", false))
                        {
                            newItem.Group = group;
                        }
                    }
                }
                else
                {
                    newItem.Group = missingEntryGroup;
                }

                breachedEntryList.Items.Add(newItem);
            }            
        }

        [STAThread]
        private void breachedEntryList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if(breachedEntryList.SelectedItems != null && breachedEntryList.SelectedItems.Count == 1)
            {
                var entry = ((PwEntry)breachedEntryList.SelectedItems[0].Tag);
                if (entry != null)
                {
                    var pwForm = new KeePass.Forms.PwEntryForm();
                    pwForm.InitEx(entry, KeePass.Forms.PwEditMode.EditExistingEntry, pluginHost.Database, pluginHost.MainWindow.ClientIcons, false, false);
                    var thread = new Thread(() =>
                    {
                        if (pwForm.ShowDialog() == DialogResult.OK)
                        {
                            bool bUpdImg = pluginHost.Database.UINeedsIconUpdate;
                            pluginHost.MainWindow.RefreshEntriesList(); // Update entry
                        pluginHost.MainWindow.UpdateUI(false, null, bUpdImg, null, false, null, pwForm.HasModifiedEntry);
                            breachedEntryList.SelectedItems[0].SubItems[0] = new ListViewItem.ListViewSubItem(breachedEntryList.SelectedItems[0], entry.Strings.ReadSafe(PwDefs.TitleField));
                            breachedEntryList.SelectedItems[0].SubItems[1] = new ListViewItem.ListViewSubItem(breachedEntryList.SelectedItems[0], entry.Strings.ReadSafe(PwDefs.UserNameField));
                            breachedEntryList.SelectedItems[0].SubItems[2] = new ListViewItem.ListViewSubItem(breachedEntryList.SelectedItems[0], entry.Strings.ReadSafe(PwDefs.UrlField));
                            breachedEntryList.SelectedItems[0].SubItems[3] = new ListViewItem.ListViewSubItem(breachedEntryList.SelectedItems[0], entry.GetPasswordLastModified().ToShortDateString());
                        }
                        else
                        {
                            bool bUpdImg = pluginHost.Database.UINeedsIconUpdate;
                            pluginHost.MainWindow.RefreshEntriesList(); // Update last access time
                        pluginHost.MainWindow.UpdateUI(false, null, bUpdImg, null, false, null, false);
                        }
                    });
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.IsBackground = true;
                    thread.Start();
                }
            }
        }
    }
}
