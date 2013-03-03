using System;
using System.Threading;
using System.Windows.Forms;
using DirectShowLib;
using HomeView.Presenter;
using System.Runtime.InteropServices;
using System.Drawing;

namespace HomeView.Views
{
    public partial class frmConnectCameraView : Form, IKinectView
    {
        #region Declarations 

        private frmConnectCameraPresenter presenter = new frmConnectCameraPresenter();
        public DsDevice Camera;
        public Guid Microphone;

        //Kinect Declarations
        private int m_CursorAnimation;
        private Thread m_cursorThread;
        private ManualResetEvent m_PauseCursorEvent;
        private ManualResetEvent m_StopCursorEvent;
        private Button m_ClickButton;
        public bool m_KinectState;
        private IntPtr[] m_CustomCursors;

        //Constants used in the cursor conversion
        private const int IMAGE_CURSOR = 2;
        private const uint LR_LOADFROMFILE = 0x00000010;

        private bool m_CameraSelected;
        private bool m_MicrophoneSelected;

        //DLLImport to load the cursor image and convert it into a cursor file larger than 32x32
        //Taken from a user comment: http://stackoverflow.com/questions/6668147/when-reading-a-cursor-from-a-resources-file-an-argumentexception-is-thrown
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        #endregion

        /// <summary>
        /// Connect camera view constructor, all of the views required data is delivered
        /// via it's corresponding presenter.
        /// </summary>
        public frmConnectCameraView(bool KinectState)
        {
            InitializeComponent();

            //Retrieve a list of the available cameras
            foreach (string camera in presenter.getCameras())
            {
                comboCams.Items.Add(camera);
            }

            //Set the combo box to the first index if there is a camera to be added
            if (comboCams.Items.Count > 0)
            {
                comboCams.SelectedIndex = 0;
            }

            //Retrieve a list of the available microphones
            foreach (string mic in presenter.getMicrophones())
            {
                comboMics.Items.Add(mic);
            }

            //Set the combo box to the first index if there is a microphone to be added
            if (comboMics.Items.Count > 0)
            {
                comboMics.SelectedIndex = 0;
            }

            m_KinectState = KinectState;
            if (KinectState == true)
            {
                this.MouseMove += new MouseEventHandler(frmConnectCameraView_MouseMove);
                m_CameraSelected = false;
                m_MicrophoneSelected = false;
            }
        }

        void frmConnectCameraView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Location.X < trackCamKinectChoice.Location.X)
            {
                trackCamKinectChoice.Value = 0;
            }
            else if (e.Location.X > (trackCamKinectChoice.Location.X + trackCamKinectChoice.Width))
            {
                trackCamKinectChoice.Value = 2;
            }
            else
            {
                trackCamKinectChoice.Value = 1;
            }
        }

        /// <summary>
        /// Connect camera view constructor, all of the views required data is delivered
        /// via it's corresponding presenter.
        /// </summary>
        public frmConnectCameraView(bool KinectState, IntPtr[] CustomCursors)
            :this(KinectState)
        {
            m_CustomCursors = CustomCursors;

            m_PauseCursorEvent = new ManualResetEvent(true);
            m_StopCursorEvent = new ManualResetEvent(false);
        }

        /// <summary>
        /// Button event to connect both a camera and a microphone
        /// </summary>
        /// <param name="sender">Sending control</param>
        /// <param name="e">Event arguments</param>
        private void btnConnectCameraAndMic_Click(object sender, EventArgs e)
        {
            Camera = presenter.getCamera(comboCams.SelectedIndex);
            Microphone = presenter.getMicrophone(comboMics.SelectedIndex);
        }

        /// <summary>
        /// Button event to connect a camera alone
        /// </summary>
        /// <param name="sender">Sending control</param>
        /// <param name="e">Event arguments</param>
        private void btnConnectCamera_Click(object sender, EventArgs e)
        {
            Camera = presenter.getCamera(comboCams.SelectedIndex);
        }

        /// <summary>
        /// Kinect Button Mouse Enter Function, used to start the cursor manipulation and timer
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        public void btnMouseEnter(object sender, EventArgs e)
        {
            //Only do if the kinect is active
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
            //Only do if the kinect is active
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
                    try
                    {
                        Invoke(new Action(() => { btnMouseExit(this, null); }));
                    }
                    catch (ObjectDisposedException) { }
                    Invoke(new Action(() => { m_ClickButton.PerformClick(); }));
                }

                Thread.Sleep(1000);
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
            if (m_cursorThread != null)
            {
                m_StopCursorEvent.Set();
                m_PauseCursorEvent.Set();
                //If the cursor thread has not been activated, then ignore
                if (m_cursorThread != null)
                {
                    m_cursorThread.Join();
                }
            }
        }

        /// <summary>
        /// Stop the animation event if the kinect is active, this ensures that the application closes elegantly
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Arguments</param>
        private void frmConnectCameraView_FormClosing(object sender, FormClosingEventArgs e)
        {
            stopEvent();
        }

        /// <summary>
        /// The choice change varies and alters the combobox depending on which device has been selected to be altered
        /// (either Camera or Mircophone)
        /// </summary>
        /// <param name="sender">Sending Control</param>
        /// <param name="e">Event arguments</param>
        private void trackCamKinectChoice_Change(object sender, EventArgs e)
        {
            //Check if either the Camera or Microphone is selected to be altered
            if (m_CameraSelected == true)
            {
                changeComboBox(comboCams);
            }
            else if (m_MicrophoneSelected == true)
            {
                changeComboBox(comboMics);
            }
        }

        /// <summary>
        /// The changeComboBox function alters the list programmatically when a user attempted to
        /// increment or decrement outside of the arrays boundaries
        /// </summary>
        /// <param name="deviceList">The combobox object</param>
        private void changeComboBox(ComboBox deviceList)
        {
            if (trackCamKinectChoice.Value == 2)
            {
                if (deviceList.SelectedIndex == (deviceList.Items.Count - 1))
                {
                    deviceList.SelectedIndex = -1;
                }
                deviceList.SelectedIndex++;
            }
            else if (trackCamKinectChoice.Value == 0)
            {
                if (deviceList.SelectedIndex == 0)
                {
                    deviceList.SelectedIndex = (deviceList.Items.Count - 1);
                }
                else
                {
                    deviceList.SelectedIndex--;
                }
            }
        }

        /// <summary>
        /// Click event of the Camera Button, changes the visual display of the button and
        /// 'selects' the camera list ot be altered, deselecting the microphone list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCamera_Click(object sender, EventArgs e)
        {
            if (m_KinectState == true)
            {
                btnMicrophone.BackColor = btnCamera.BackColor;
                btnCamera.BackColor = Color.Red;
                m_CameraSelected = true;
                m_MicrophoneSelected = false;
            }
        }

        /// <summary>
        /// Click event of the Microphone button, changes the visual display of the button and
        /// 'selects' the microphone list to be altered, deselecting the camera list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMicrophone_Click(object sender, EventArgs e)
        {
            if (m_KinectState == true)
            {
                btnCamera.BackColor = btnMicrophone.BackColor;
                btnMicrophone.BackColor = Color.Red;
                m_CameraSelected = false;
                m_MicrophoneSelected = true;
            }
        }
    }
}

