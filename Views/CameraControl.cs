using System;
using System.Drawing;
using System.Windows.Forms;

namespace HomeView
{
    public partial class CameraControl : UserControl
    {
        #region Declarations

        private Color m_DefaultColour;
        private int m_ControlWidth;
        private int m_ControlHeight;
        private bool m_Active;

        public event EventHandler activated;
        public event EventHandler deactivated;
        public event EventHandler resize;

        #endregion

        /// <summary>
        /// CameraControl constuctor, also dictates that the custom user control should stretch the image
        /// or frame to fit the picturebox output window
        /// </summary>
        public CameraControl()
        {
            InitializeComponent();
            VideoBox.SizeMode = PictureBoxSizeMode.StretchImage;
            m_ControlWidth = this.Width;
            m_ControlHeight = this.Height;
            m_Active = false;
            //Set the default color of the control, this changes if the control is a playback control
            m_DefaultColour = this.BackColor;
        }

        /// <summary> Get the title of the CameraControl </summary>
        public string CameraName
        {
            get { return txtCameraName.Text; }
            set { txtCameraName.Text = value; }
        }

        /// <summary> Get the status of the CameraControl </summary>
        public string CameraStatus
        {
            get { return lblStatus.Text; }
            set 
            {
                //Switch statement changes the state to the appropriate output text
                switch (value)
                {
                    case "Connected":
                        lblStatus.Text = "Ready";
                        lblStatus.ForeColor = Color.Lime;
                        break;

                    case "Connecting":
                        lblStatus.Text = "Connecting";
                        lblStatus.ForeColor = Color.Yellow;
                        break;

                    case "Recording":
                        lblStatus.Text = "Recording";
                        lblStatus.ForeColor = Color.Red;
                        break;

                    case "Creating Files":
                        lblStatus.Text = "Creating Files";
                        lblStatus.ForeColor = Color.Aqua;
                        break;

                    case "Disconnected":
                        lblStatus.Text = "Disconnected";
                        lblStatus.ForeColor = Color.Red;
                        break;

                    case "Failed":
                        lblStatus.Text = "Connection Failed";
                        lblStatus.ForeColor = Color.Red;
                        break;

                    case "Playback":
                        lblStatus.Text = "Playback";
                        lblStatus.Visible = false;
                        break;

                    default:
                        lblStatus.Text = "Status";
                        lblStatus.ForeColor = Color.Black;
                        break;
                }
            }
        }

        /// <summary> Get the VideoBox Ctrl </summary>
        public PictureBox VideoBox
        {
            get { return picboxVideo; }
        }

        /// <summary>
        /// Value for 'active' control
        /// </summary>
        public bool Active
        {
            get { return m_Active; }
            set { m_Active = value; }
        }

        /// <summary>
        /// Raise the event that this control has been activated
        /// </summary>
        /// <param name="e">Event arguments</param>
        private void controlActivated(EventArgs e)
        {
            if (activated != null)
            {
                activated(this, e);
            }
        }

        /// <summary>
        /// Raise the event that this control has been deactivated
        /// </summary>
        /// <param name="e">Event arguments</param>
        private void controlDeactivated(EventArgs e)
        {
            if (deactivated != null)
            {
                deactivated(this, e);
            }
        }

        /// <summary>
        /// Raise the event that this control has been resized
        /// </summary>
        /// <param name="e">Event arguments</param>
        private void resized(EventArgs e)
        {
            if (resize != null)
            {
                resize(this, e);
            }
        }

        /// <summary>
        /// Resizes the camera controls picture box if the control is resized
        /// </summary>
        /// <param name="sender">Sending control</param>
        /// <param name="e">Event arguments</param>
        private void CameraControl_Resize(object sender, EventArgs e)
        {
            int changeX = this.Width - m_ControlWidth;
            int changeY = this.Height - m_ControlHeight;

            VideoBox.Width += changeX;
            VideoBox.Height += changeY;

            m_ControlWidth = this.Width;
            m_ControlHeight = this.Height;
            resized(EventArgs.Empty);
        }

        /// <summary>
        /// Used to make the control the 'active' control within a group of controls
        /// </summary>
        /// <param name="sender">Sending control</param>
        /// <param name="e">Event arguments</param>
        private void activeControl(object sender, EventArgs e)
        {
            //Allow double click if a connected camera or playback
            if (m_Active == false && lblStatus.Text.Equals("Playback") || m_Active == false && this.picboxVideo.Image != null)
            {
                this.txtCameraName.Enabled = true;
                this.BackColor = Color.Firebrick;
                m_Active = true;
                controlActivated(EventArgs.Empty);
            }
            else if (m_Active == true && lblStatus.Text.Equals("Playback") || m_Active == true && this.picboxVideo.Image != null)
            {
                txtCameraName.Enabled = false;
                this.BackColor = m_DefaultColour;
                m_Active = false;
                controlDeactivated(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Used to activate the control on connection
        /// </summary>
        public void activateControl()
        {
            //
            m_DefaultColour = this.BackColor;
            this.txtCameraName.Enabled = true;
            this.BackColor = Color.Firebrick;
            m_Active = true;
        }

        /// <summary>
        /// Used to deactivate the control on connection
        /// </summary>
        public void deactivateControl()
        {
            txtCameraName.Enabled = false;
            this.BackColor = m_DefaultColour;
            m_Active = false;
        }

        /// <summary>
        /// Clear the control if an camera using it is disconnected
        /// </summary>
        public void clearControl()
        {
            CameraName = "Camera Name";
            CameraStatus = "Default";
            this.VideoBox.Image = null;
        }
    }
}
