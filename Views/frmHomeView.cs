using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using HomeView.Presenter;
using System.ComponentModel;

namespace HomeView.Views
{
    public partial class HomeViewMainForm : Form, IKinectView
    {
        #region Declarations

        private frmHomeViewPresenter m_Presenter;

        //Dimensions of the form
        private int m_FrmWidth;
        private int m_FrmHeight;
        private int m_CursorAnimation;

        private Thread m_cursorThread;

        private ManualResetEvent m_PauseCursorEvent;
        private ManualResetEvent m_StopCursorEvent;

        //Constants used in the cursor conversion
        private const int IMAGE_CURSOR = 2;
        private const uint LR_LOADFROMFILE = 0x00000010;

        private Button m_ClickButton;

        private IntPtr[] m_CustomCursors;

        public bool m_KinectState;

        #endregion

        //DLLImport to load the cursor image and convert it into a cursor file larger than 32x32
        //Taken from a user comment: http://stackoverflow.com/questions/6668147/when-reading-a-cursor-from-a-resources-file-an-argumentexception-is-thrown
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        /// <summary>
        /// HomeView Main View Constructor, utilizing the MVP 'Supervising Controller Pattern', some elements
        /// of the View can be directly taken from the model. However the view only constructs the objects within
        /// the WinForm itself (e.g. Camera Controls, height properties)
        /// </summary>
        public HomeViewMainForm()
        {
            InitializeComponent();

            m_Presenter = new frmHomeViewPresenter();

            //Construct the control array
            m_Presenter.addLiveVideoControl(liveCamControl1);
            m_Presenter.addLiveVideoControl(liveCamControl2);
            m_Presenter.addLiveVideoControl(liveCamControl3);
            m_Presenter.addLiveVideoControl(liveCamControl4);

            m_Presenter.addPlaybackVideoControl(playbackCamControl1);
            m_Presenter.addPlaybackVideoControl(playbackCamControl2);
            m_Presenter.addPlaybackVideoControl(playbackCamControl3);
            m_Presenter.addPlaybackVideoControl(playbackCamControl4);

            m_FrmWidth = this.Width;
            m_FrmHeight = this.Height;

            m_PauseCursorEvent = new ManualResetEvent(true);
            m_StopCursorEvent = new ManualResetEvent(false);

            m_Presenter.getListOfIncidents(dgvIncidents);

            dgvIncidents.Columns[0].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss";
            
            dgvIncidents.Columns[0].Width = 180;
            dgvIncidents.Columns[1].Width = 150;
            dgvIncidents.Columns[2].Width = 150;
            dgvIncidents.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvIncidents.Columns[4].Visible = false;
            dgvIncidents.Columns[5].Visible = false;
            //Sort by date, most recent first
            dgvIncidents.Sort(dgvIncidents.Columns[0], ListSortDirection.Descending);
            dgvIncidents.DataSourceChanged += new EventHandler(newIncident);

            m_CustomCursors = new IntPtr[5];
            m_KinectState = false;
        }

        /// <summary>
        /// The Live View Button Event handler
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnLiveView_Click(object sender, EventArgs e)
        {
            btnEject_Click(this, null);
        }

        /// <summary>
        /// The Search Button Event Handler
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnSearch_Click(object sender, EventArgs e)
        {
            pnlSearch.BringToFront();
        }

        /// <summary>
        /// The Mute Button Event Handler
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnMute_Click(object sender, EventArgs e)
        {
            m_Presenter.muteMicrophone();
        }

        /// <summary>
        /// The Settings Button Event Handler
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnSettings_Click(object sender, EventArgs e)
        {
            frmSettingsView settingsView = new frmSettingsView();
            settingsView.ShowDialog(this);
            settingsView.Dispose();
            if (settingsView.DialogResult == DialogResult.OK)
            {
                Properties.Settings.Default.OutputLocation = settingsView.outputString;
                Properties.Settings.Default.DropboxLocation = settingsView.dropboxString;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// The Connect Camera Button Event Handler
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnConnectCam_Click(object sender, EventArgs e)
        {
            frmConnectCameraView connectCameraView;

            if (m_KinectState == false)
            {
                connectCameraView = new frmConnectCameraView(m_KinectState);
            }
            else
            {
                connectCameraView = new frmConnectCameraView(m_KinectState,m_CustomCursors);
            }

            connectCameraView.ShowDialog(this);
            connectCameraView.Dispose();
            //Check if the dialogresult was Ok and not Cancelled
            if (connectCameraView.DialogResult == DialogResult.OK && connectCameraView.Camera != null)
            {
                //If the microphone hasn't been initalized connect the camera only
                if (connectCameraView.Microphone == Guid.Empty)
                {
                    m_Presenter.connectCamera(connectCameraView.Camera, treeViewLive);
                }
                //Else, connect both the camera and microphone
                else
                {
                    //Check a the microphone is valid
                    if (connectCameraView.Microphone != Guid.Empty)
                    {
                        m_Presenter.connectCameraAndMic(connectCameraView.Camera, connectCameraView.Microphone, treeViewLive);
                    }
                }
            }
        }

        /// <summary>
        /// The Disconnect Camera Button Event Handler
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnDisconnectCam_Click(object sender, EventArgs e)
        {
            m_Presenter.disconnectCamera(treeViewLive);
        }

        /// <summary>
        /// The Record Button Event Handler
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnRecord_Click(object sender, EventArgs e)
        {
            m_Presenter.recordCameras();
        }

        /// <summary>
        /// The Motion Detection Button Event Handler
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnDetectMotion_Click(object sender, EventArgs e)
        {
            m_Presenter.motionDetection();
        }

        /// <summary>
        /// The Kinect Button Event Handler
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnKinect_Click(object sender, EventArgs e)
        {
            m_Presenter.kinect(btnKinectUp, btnKinectDown, out m_KinectState);
            btnMouseExit(this, null);
            loadCustomCursors();
        }

        /// <summary>
        /// The Previous 'Live View' Button Event Handler
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnPrevious_Click(object sender, EventArgs e)
        {
            m_Presenter.previousControl();
        }

        /// <summary>
        /// The Next Live View Button Event Handler
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnNext_Click(object sender, EventArgs e)
        {
            m_Presenter.nextControl();
        }

        /// <summary>
        /// The '1x1 Live View' Button Event Handler
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btn1x1View_Click(object sender, EventArgs e)
        {
            m_Presenter.oneByOneResize(pnlLiveView.Width, pnlLiveView.Height);
        }

        /// <summary>
        /// The '1x2 Live View' Button Event Handler
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btn1x2View_Click(object sender, EventArgs e)
        {
            m_Presenter.oneByTwoResize(pnlLiveView.Width, pnlLiveView.Height);
        }

        /// <summary>
        /// The '2x2 Live View' Button Event Handler
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btn2x2View_Click(object sender, EventArgs e)
        {
            m_Presenter.twoByTwoResize(pnlLiveView.Width, pnlLiveView.Height);
        }

        /// <summary>
        /// Double click event of the Data grid view, used to load an incident
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void dgvRecentIncidents_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            //If the user clicks on the heading
            if (e.RowIndex < 0)
                return;
            dgvIncidents.Rows[e.RowIndex].DefaultCellStyle.BackColor = Control.DefaultBackColor;
            //Load incident on selected row
            m_Presenter.playbackIncident(e.RowIndex);
            pnlPlayback.BringToFront(); 
        }

        /// <summary>
        /// newIncident function is called when the datasource as been changed as a new incident has occured.
        /// </summary>
        /// <param name="sender">Sending control</param>
        /// <param name="e">Event arguments</param>
        private void newIncident(object sender, EventArgs e)
        {
            //Sort the gridview by date
            dgvIncidents.Sort(dgvIncidents.Columns[0], ListSortDirection.Descending);
            //Highlight the new incident
            dgvIncidents.Rows[0].DefaultCellStyle.BackColor = Color.Red;
        }

        /// <summary>
        /// Event risen when form is resized, panels and controls within the form will
        /// change to reflect the changes made.
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void HomeViewMainForm_Resize(object sender, EventArgs e)
        {
            //Calculate x & y changes
            int changeX = this.Width - m_FrmWidth;
            int changeY = this.Height - m_FrmHeight;

            int newX = pnlControl.Location.X + changeX;
            pnlControl.Location = new Point(newX, 0);
            pnlControl.Height += changeY;

            int newY = pnlControl.Height;

            //If minimised, prevent the camera control from being completely enclosed
            if (newX < 0)
            {
                newX = 10;
            }

            //Change the size of live panel
            pnlLive.Width = newX;
            pnlLive.Height = newY;

            //Change the size of playback panel
            pnlPlayback.Width = newX;
            pnlPlayback.Height = newY;

            //Change the size of the live view panel
            pnlLiveView.Width = newX;
            pnlLiveView.Height = (pnlIncidents.Location.Y - 1);

            //Change the size of the playback view panel
            pnlPlaybackView.Width = newX;
            pnlPlaybackView.Height = (pnlPlaybackControls.Location.Y - 1);

            pnlIncidents.Width = newX;
            pnlSearch.Width = pnlIncidents.Width;
            pnlSearch.Location = pnlIncidents.Location;
            dgvIncidents.Width = newX;

            //Reset the forms dimensions
            m_FrmWidth = this.Width;
            m_FrmHeight = this.Height;
        }

        /// <summary>
        /// Event risen when the live view panel is resized
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void pnlControlView_Resize(object sender, EventArgs e)
        {
            int changeX = pnlLiveView.Width;
            int changeY = pnlLiveView.Height;

            m_Presenter.controlResize(changeX, changeY);
        }

        /// <summary>
        /// Event risen when the form is closing.
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void HomeViewMainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            stopEvent();
            m_Presenter.closeView();
        }

        /// <summary>
        /// Kinect Button Mouse Enter Function, used to start the cursor manipulation and timer
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        public void btnMouseEnter(object sender, EventArgs e)
        {
            if (m_KinectState == true)
            {
                if (m_cursorThread == null)
                {
                    startThread();
                }
                else
                {
                    resumeEvent();
                }

                m_ClickButton = (System.Windows.Forms.Button)sender;
            }
        }

        /// <summary>
        /// Kinect Button Mouse Exit Function, used to  end the cursor manipulation and timer
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        public void btnMouseExit(object sender, EventArgs e)
        {
            if (m_KinectState == true)
            {
                pauseEvent();
                this.Cursor = Cursors.Arrow;
                m_CursorAnimation = m_CustomCursors.Length;
            }
        }

        /// <summary>
        /// The animate cursor function which is called from each tick of the cursor hover timer, the function
        /// changes the cursor to give the appearence of animation when the user is using kinect as a method of control.
        /// When the loop reach zero, the button which is being hovered on is clicked programmatically
        /// </summary>
        public void animateCursor()
        {
            for (m_CursorAnimation = 4; m_CursorAnimation >= -1; m_CursorAnimation--)
            {
                m_PauseCursorEvent.WaitOne(Timeout.Infinite);

                if (m_StopCursorEvent.WaitOne(0))
                    break;

                if (m_CursorAnimation >= 0 && m_CursorAnimation < m_CustomCursors.Length)
                {
                    Invoke(new Action(() => { this.Cursor = new Cursor(m_CustomCursors[m_CursorAnimation]); }));
                }
                else if (m_CursorAnimation == -1)
                {
                    Invoke(new Action(() => { m_ClickButton.PerformClick(); }));
                    Invoke(new Action(() => { btnMouseExit(this, null); }));
                }

                Thread.Sleep(1000);
            }
        }

        private void loadCustomCursors()
        {
            for (int i = 0; i < m_CustomCursors.Length; i++)
            {
                m_CustomCursors[i] = LoadImage(IntPtr.Zero, Application.StartupPath.ToString() + @"\Custom Cursors\kinect" + (i + 1).ToString() + ".cur", IMAGE_CURSOR, 0, 0, LR_LOADFROMFILE);
            }
        }

        /// <summary>
        /// Begin the cursor animation thread
        /// </summary>
        private void startThread()
        {
            m_cursorThread = new Thread(new ThreadStart(animateCursor));
            m_cursorThread.Start();
        }

        /// <summary>
        /// Pause the cursor animation thread
        /// </summary>
        private void pauseEvent()
        {
            m_PauseCursorEvent.Reset();
        }

        /// <summary>
        /// Resume the cursor animation thread
        /// </summary>
        private void resumeEvent()
        {
            m_PauseCursorEvent.Set();
        }
        
        /// <summary>
        /// Stop the cursor animation thread
        /// </summary>
        private void stopEvent()
        {
            m_StopCursorEvent.Set();
            m_PauseCursorEvent.Set();
            //If the cursor thread has not been activated, then ignore
            if (m_cursorThread != null)
            {
                m_cursorThread.Join();
            }
        }

        /// <summary>
        /// The playback button end click event
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnEnd_Click(object sender, EventArgs e)
        {
            m_Presenter.playbackEnd();
        }

        /// <summary>
        /// The playback button beginning click event
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnBeginning_Click(object sender, EventArgs e)
        {
            m_Presenter.playbackBeginning();
        }

        /// <summary>
        /// The playback button play click event
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnPlay_Click(object sender, EventArgs e)
        {
            m_Presenter.playbackPlay();
        }

        /// <summary>
        /// The playback button pause click event
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnPause_Click(object sender, EventArgs e)
        {
            m_Presenter.playbackPause();
        }

        /// <summary>
        /// The playback button fast forward click event
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnFastForward_Click(object sender, EventArgs e)
        {
            m_Presenter.playbackFastForward();
        }

        /// <summary>
        /// The playback button rewind click event
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnRewind_Click(object sender, EventArgs e)
        {
            m_Presenter.playbackRewind();
        }

        /// <summary>
        /// The playback button eject click event
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnEject_Click(object sender, EventArgs e)
        {
            m_Presenter.playbackEject();
            pnlLive.BringToFront();
            pnlSearch.BringToFront();
        }

        /// <summary>
        /// The Kinect elevation down button click event
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnKinectDown_Click(object sender, EventArgs e)
        {
            m_Presenter.kinectDown();
        }

        /// <summary>
        /// The Kinect elevation up button click event
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void btnKinectUp_Click(object sender, EventArgs e)
        {
            m_Presenter.kinectUp();
        }
    }
}
