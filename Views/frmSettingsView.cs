using System;
using System.Windows.Forms;

namespace HomeView.Views
{
    public partial class frmSettingsView : Form
    {
        #region Declarations

        public string outputString;
        public string dropboxString;

        #endregion

        public frmSettingsView()
        {
            InitializeComponent();
            //Set the textbox to the values of the default properties
            txtOutput.Text = Properties.Settings.Default.OutputLocation;
            txtDropbox.Text = Properties.Settings.Default.DropboxLocation;
        }

        /// <summary>
        /// Event for the output browse button click, opens a file browse window
        /// for the user to select an appropriate directory to save as the location to place
        /// generic video output files
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnOutputBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog browse = new FolderBrowserDialog();
            if (browse.ShowDialog() == DialogResult.OK)
            {
                txtOutput.Text = browse.SelectedPath;
                outputString = browse.SelectedPath;
            }
        }

        /// <summary>
        /// Event for the dropbox browse button click, opens a file browse window
        /// for the user to select an appropriate directory to save as the location to place
        /// dropbox related video files
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnDropboxBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog browse = new FolderBrowserDialog();
            if (browse.ShowDialog() == DialogResult.OK)
            {
                txtOutput.Text = browse.SelectedPath;
                dropboxString = browse.SelectedPath;
            }
        }

        /// <summary>
        /// Clear the output text box
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnOutputClear_Click(object sender, EventArgs e)
        {
            txtOutput.Text = "";
        }

        /// <summary>
        /// Clear the dropbox textbox
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnDropboxClear_Click(object sender, EventArgs e)
        {
            txtDropbox.Text = "";
        }
    }
}
