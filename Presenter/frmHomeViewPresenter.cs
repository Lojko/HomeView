using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using DirectShowLib;
using HomeView.Model;
using System.ComponentModel;

namespace HomeView.Presenter
{
    /// <summary>
    /// Layer between the View and the model where all of the model integrating functionality of a view is stored
    /// </summary>
    class frmHomeViewPresenter : IKinectPresenter
    {
        #region Declarations

        private CameraControl[] m_LiveCameraControlArray;
        private CameraControl[] m_PlaybackControlArray;
        private Camera[] m_CameraArray;
        private IncidentReview[] m_IncidentReview;
        private int m_ConnectedCameras;
        private int m_SelectedControl;
        private int m_ActiveControl;
        private ViewLayout m_ViewLayout;
        private KinectControl m_Kinect;

        private const int MAXNUMBEROFCAMERAS = 4;

        private DataGridView m_IncidentGridView;
        private SortableBindingList<Incident> m_SortableIncidentList;

        //Delegate used for cross thread access to the DataGridView
        private delegate void updateIncidentList(SortableBindingList<Incident> incidentList);

        #endregion

        private enum ViewLayout
        {
            OneByOne,
            OneByTwo,
            TwoByTwo
        };

        public enum ViewState
        {
            KinectConnected,
            KinectDisconnected
        };

        /// <summary>
        /// HomeView Main View Presenter constructor
        /// </summary>
        public frmHomeViewPresenter()
        {
            //Construct the control array
            m_LiveCameraControlArray = new CameraControl[MAXNUMBEROFCAMERAS];
            //Construct the control array
            m_PlaybackControlArray = new CameraControl[MAXNUMBEROFCAMERAS];
            //Define the number of cameras currently connected
            m_ConnectedCameras = 0;
            //Construct the camera array
            m_CameraArray = new Camera[MAXNUMBEROFCAMERAS];
            //Construct the data structure for an incident being reviewed
            m_IncidentReview = new IncidentReview[MAXNUMBEROFCAMERAS];
            //Current view layout (1x1, 1x2 or 2x2)
            m_ViewLayout = ViewLayout.OneByOne;
            //Current Selected Control in the View Layout
            m_SelectedControl = 0;
            //Current Active Control
            m_ActiveControl = -1;
        }

        /// <summary>
        /// Called when the 'active camera control' is changed, ensuring that the selected camera control
        /// is active and all others are not
        /// </summary>
        /// <param name="sender">Sending control</param>
        /// <param name="e">Arguments</param>
        private void frmHomeViewPresenter_activatedChange(object sender, EventArgs e)
        {
            for (int i = 0; i < m_LiveCameraControlArray.Length; i++)
            {
                if (m_LiveCameraControlArray[i] == sender)
                {
                    m_ActiveControl = i;
                }
                else
                {
                    m_LiveCameraControlArray[i].deactivateControl();
                }
            }
        }

        /// <summary>
        /// When the active camera control is deactivated, no others must be activated, thus the
        /// active control must be recognisable as a non-active control
        /// </summary>
        /// <param name="sender">Sending control</param>
        /// <param name="e">Arguments</param>
        private void frmHomeViewPresenter_deactivatedChange(object sender, EventArgs e)
        {
            m_ActiveControl = -1;
        }

        /// <summary>
        /// Connects a camera to the HomeView main View
        /// </summary>
        /// <param name="camera">The camera object</param>
        /// <param name="treeview">The treeview from the Main live view which needs updating when a
        /// camera is added</param>
        public void connectCamera(DsDevice camera, TreeView treeview)
        {
            if (m_ConnectedCameras < MAXNUMBEROFCAMERAS)
            {
                //Check to see if any other cameras have the same name
                checkForNameDuplicates(m_LiveCameraControlArray[m_ConnectedCameras], camera.Name);
                //Create new Camera
                Camera newCam = new USBCamera(m_LiveCameraControlArray[m_ConnectedCameras], camera);
                newCam.newIncident += new EventHandler(newIncident);
                for (int i = 0; i < m_CameraArray.Length; i++)
                {
                    if (m_CameraArray[i] == null)
                    {
                        //Set the camera control of the new camera
                        setCameraControlProperties(i, newCam, treeview);
                        break;
                    }
                }
                m_ConnectedCameras++;
            }
        }

        /// <summary>
        /// Connects a camera and a microphone to the HomeView main View
        /// </summary>
        /// <param name="camera">The camera dsdevice object</param>
        /// <param name="microphone">The microphones Guid</param>
        /// <param name="treeview">The treeview needs to be updated when a camera is connected</param>
        public void connectCameraAndMic(DsDevice camera, Guid microphone, TreeView treeview)
        {
            if (m_ConnectedCameras < MAXNUMBEROFCAMERAS)
            {
                //Check to see if any other cameras have the same name
                checkForNameDuplicates(m_LiveCameraControlArray[m_ConnectedCameras], camera.Name);
                //Create new Camera with a microphone
                Camera newCam = new USBCamera(m_LiveCameraControlArray[m_ConnectedCameras], camera, microphone);
                newCam.newIncident += new EventHandler(newIncident);
                for (int i = 0; i < m_CameraArray.Length; i++)
                {
                    if (m_CameraArray[i] == null)
                    {
                        //Set the camera control of the new camera
                        setCameraControlProperties(i, newCam, treeview);
                        muteAll(i);
                        break;
                    }
                }
                m_ConnectedCameras++;
            }
        }

        private void newIncident(object sender, EventArgs e)
        {
            getListOfIncidents(m_IncidentGridView);
            m_IncidentGridView.Rows[0].DefaultCellStyle.BackColor = Color.Red;
        }

        /// <summary>
        /// Mute all microphones apart from the microphone just connected
        /// </summary>
        private void muteAll(int index)
        {
            for (int i = 0; i < m_CameraArray.Length; i++)
            {
                if (i != index && m_CameraArray[i] != null && m_CameraArray[i].CameraAudio != null)
                {
                    m_CameraArray[i].CameraAudio.Mute();
                }
            }
        }

        /// <summary>
        /// Sets the control properties of the connected camera
        /// </summary>
        /// <param name="index">index of the control array</param>
        /// <param name="newCam">The new camera object</param>
        /// <param name="treeview">Updates to the treeview to be made</param>
        private void setCameraControlProperties(int index, Camera newCam, TreeView treeview)
        {
            m_CameraArray[index] = newCam;
            changeActiveControl(m_LiveCameraControlArray[index]);
            TreeNode cam = new TreeNode();
            cam.Text = m_LiveCameraControlArray[index].CameraName;
            if (treeview.Nodes.Count > index)
            {
                treeview.Nodes.Insert(index, cam);
            }
            else
            {
                treeview.Nodes.Add(cam);
            }
        }

        /// <summary>
        /// Function which checks the existing initialized camera controls for a camera with the same name
        /// </summary>
        /// <param name="control">Camera control</param>
        /// <param name="cameraName">Name of the camera</param>
        private void checkForNameDuplicates(CameraControl control, string cameraName)
        {
            //Original name of the camera
            string original = cameraName;
            int numberConnected = 1;

            for (int i = 0; i < m_ConnectedCameras; i++)
            {
                if (m_LiveCameraControlArray[i].CameraName.Equals(cameraName))
                {
                    //Check if multiples of the same camera have already been connected
                    numberConnected++;
                    cameraName = original + numberConnected.ToString();
                }
            }
            control.CameraName = cameraName;
        }

        /// <summary>
        /// Mute or unmute the microphone stream on the active control
        /// </summary>
        public void muteMicrophone()
        {
            for (int i = 0; i < m_CameraArray.Length; i++)
            {
                if (i == m_ActiveControl)
                {
                    if (m_CameraArray[m_ActiveControl].CameraAudio.isMuted() == true)
                    {
                        m_CameraArray[m_ActiveControl].CameraAudio.Unmute();
                    }
                    else
                    {
                        m_CameraArray[m_ActiveControl].CameraAudio.Mute();
                    }
                }
                else if (m_CameraArray[i] != null && m_CameraArray[i].CameraAudio != null)
                {
                    m_CameraArray[i].CameraAudio.Mute();
                }
            }

            //Allow muting on playback controls
            for (int i = 0; i < m_IncidentReview.Length; i++)
            {
                if (m_IncidentReview[i] != null && m_PlaybackControlArray[i].Active == true)
                {
                    if (m_IncidentReview[i].muted == true)
                    {
                        m_IncidentReview[i].unmute();
                    }
                    else
                    {
                        m_IncidentReview[i].mute();
                    }
                }
            }
        }

        /// <summary>
        /// Disconnect a camera that is already connected
        /// </summary>
        public void disconnectCamera(TreeView treeview)
        {
            if (m_ActiveControl != -1)
            {
                m_CameraArray[m_ActiveControl].Disconnect();
                m_CameraArray[m_ActiveControl] = null;
                m_LiveCameraControlArray[m_ActiveControl].deactivateControl();
                m_LiveCameraControlArray[m_ActiveControl].clearControl();
                treeview.Nodes[m_ActiveControl].Remove();
                m_ConnectedCameras--;
            }
        }

        /// <summary>
        /// Record cameras, if no camera is selected, all cameras are recorded. If a single camera is selected, the only
        /// that camera is recorded
        /// </summary>
        public void recordCameras()
        {
            //Check if there is a selected camera control, if not record all available
            if (m_ActiveControl < 0)
            {
                //Check how many cameras are available
                int camerasAvailable = 0;
                for (int i = 0; i < m_CameraArray.Length; i++)
                {
                    if (m_CameraArray[i] != null && m_LiveCameraControlArray[i].CameraStatus != "Recording")
                    {
                        camerasAvailable++;
                    }
                }

                for (int i = 0; i < m_CameraArray.Length; i++)
                {
                    if (m_CameraArray[i] != null && m_LiveCameraControlArray[i].CameraStatus != "Recording")
                    {
                        //Create the incident object for multiple cameras
                        Incident newIncident = new Incident(DateTime.Now, m_LiveCameraControlArray[0].CameraName,
                            camerasAvailable, "Manual", m_LiveCameraControlArray[i].CameraName);
                        //Record for a specific amount of time
                        m_CameraArray[i].Record(30, newIncident);
                    }
                }
            }
            else if(m_ConnectedCameras > 0)
            {
                if (m_LiveCameraControlArray[m_ActiveControl].CameraStatus != "Recording")
                {
                    //Create the incident for a single camera
                    Incident newIncident = new Incident(DateTime.Now, m_LiveCameraControlArray[m_ActiveControl].CameraName,
                        1, "Manual", m_LiveCameraControlArray[m_ActiveControl].CameraName);
                    //Record for a specific amount of time
                    m_CameraArray[m_ActiveControl].Record(30, newIncident);
                }
            }
        }

        /// <summary>
        /// Activate or deactivate motion detection on the active control
        /// </summary>
        public void motionDetection()
        {
            if (m_ActiveControl >= 0)
            {
                m_CameraArray[m_ActiveControl].CameraVideo.activateMotionTrigger();
            }
        }

        /// <summary>
        /// Function used to initialize a kinect device, the function passes the buttons used to increase
        /// and decrease the kinect elevation as well as updates the status of the kinect
        /// </summary>
        /// <param name="kinectUp">Button used to increment the elevation</param>
        /// <param name="kinectDown">Button used to decrement the elevation</param>
        /// <param name="kinectState">Connection state of the kinect</param>
        public void kinect(Button kinectUp, Button kinectDown, out bool kinectState)
        {
            kinectState = false;

            if (m_Kinect == null)
            {
                m_Kinect = new KinectControl();
                //Check if a kinect is connected
                if (m_Kinect.Status.ToString().Equals("Disconnected"))
                {
                    m_Kinect = null;
                }
                else
                {
                    //Display the kinect controls
                    kinectUp.Visible = true;
                    kinectDown.Visible = true;
                    kinectState = true;
                }
            }
            else
            {
                //Stop the kinect and default the values
                m_Kinect.kinectStop();
                m_Kinect = null;
                kinectUp.Visible = false;
                kinectDown.Visible = false;
            }
        }

        /// <summary>
        /// Increase the kinect elevation
        /// </summary>
        public void kinectUp()
        {
            m_Kinect.increaseElevation();
        }

        /// <summary>
        /// Decrease the kinect elevation
        /// </summary>
        public void kinectDown()
        {
            m_Kinect.decreaseElevation();
        }


        /// <summary>
        /// Changes the active control of the HomeView Application
        /// </summary>
        private void changeActiveControl(CameraControl control)
        {
            for(int i = 0; i < m_LiveCameraControlArray.Length; i++)
            {
                if (m_LiveCameraControlArray[i] == control)
                {
                    m_ActiveControl = i;
                }

                m_LiveCameraControlArray[i].deactivateControl();
            }

            control.activateControl();
        }

        /// <summary>
        /// Add the video controls that the presenter will handle and how they behave
        /// </summary>
        /// <param name="control">The camera control</param>
        public void addLiveVideoControl(CameraControl control)
        {
            for(int i = 0; i < m_LiveCameraControlArray.Length; i++)
            {
                if(m_LiveCameraControlArray[i] == null)
                {
                    control.activated += new EventHandler(frmHomeViewPresenter_activatedChange);
                    control.deactivated += new EventHandler(frmHomeViewPresenter_deactivatedChange);
                    m_LiveCameraControlArray[i] = control;
                    break;
                }
            }
        }

        /// <summary>
        /// Add the video controls that the presenter will handle and how they behave
        /// </summary>
        /// <param name="control">The camera control</param>
        public void addPlaybackVideoControl(CameraControl control)
        {
            for (int i = 0; i < m_PlaybackControlArray.Length; i++)
            {
                if (m_PlaybackControlArray[i] == null)
                {
                    control.activated += new EventHandler(frmHomeViewPresenter_activatedChange);
                    control.deactivated += new EventHandler(frmHomeViewPresenter_deactivatedChange);
                    m_PlaybackControlArray[i] = control;
                    break;
                }
            }
        }

        /// <summary>
        /// Resizes the video controls
        /// </summary>
        /// <param name="x">Width</param>
        /// <param name="y">Height</param>
        public void controlResize(int x, int y)
        {
            for (int i = 0; i < m_LiveCameraControlArray.Length; i++)
            {
                if (m_ViewLayout == ViewLayout.OneByOne)
                {
                    m_LiveCameraControlArray[i].Width = x;
                    m_LiveCameraControlArray[i].Height = y;

                    m_PlaybackControlArray[i].Width = x;
                    m_PlaybackControlArray[i].Height = y;
                }
                else if (m_ViewLayout == ViewLayout.OneByTwo)
                {
                    m_LiveCameraControlArray[i].Width = x;
                    m_LiveCameraControlArray[i].Height = y / 2;

                    m_PlaybackControlArray[i].Width = x;
                    m_PlaybackControlArray[i].Height = y / 2;

                    if ((i + 2) % 2 != 0)
                    {
                        m_LiveCameraControlArray[i].Location = new Point(m_LiveCameraControlArray[i].Location.X, y / 2);
                        m_PlaybackControlArray[i].Location = new Point(m_PlaybackControlArray[i].Location.X, y / 2);
                    }
                }
                else 
                {
                    m_LiveCameraControlArray[i].Width = x / 2;
                    m_LiveCameraControlArray[i].Height = y / 2;

                    m_PlaybackControlArray[i].Width = x / 2;
                    m_PlaybackControlArray[i].Height = y / 2;

                    if ((i + 1) % 2 == 0)
                    {
                        if ((i + 1) % 4 == 0)
                        {
                            m_LiveCameraControlArray[i].Location = new Point(x / 2, y / 2);
                            m_PlaybackControlArray[i].Location = new Point(x / 2, y / 2);
                        }
                        else
                        {
                            m_LiveCameraControlArray[i].Location = new Point(x / 2, m_LiveCameraControlArray[i].Location.Y);
                            m_PlaybackControlArray[i].Location = new Point(x / 2, m_PlaybackControlArray[i].Location.Y);
                        }
                    }
                    else if ((i + 1) % 3 == 0)
                    {
                        m_LiveCameraControlArray[i].Location = new Point(m_LiveCameraControlArray[i].Location.X, y / 2);
                        m_PlaybackControlArray[i].Location = new Point(m_PlaybackControlArray[i].Location.X, y / 2);
                    }
                }
            }
        }

        /// <summary>
        /// CloseView function iterates through the cameras, disconnecting them before the application is
        /// closed.
        /// </summary>
        public void closeView()
        {
            //Ensure all of the cameras are disconnected appropriately
            for (int i = 0; i < m_CameraArray.Length; i++)
            {
                if (m_CameraArray[i] != null)
                {
                    m_CameraArray[i].Disconnect();
                }
            }
        }

        /// <summary>
        /// The nextControl function is used to display the next control depending on the current
        /// camera control view (e.g. 1x2)
        /// </summary>
        public void nextControl()
        {
            //If 1x1 view
            if (m_ViewLayout == ViewLayout.OneByOne)
            {
                m_SelectedControl++;

                if (m_SelectedControl > (m_LiveCameraControlArray.Length - 1))
                {
                    m_SelectedControl = 0;
                }

                m_LiveCameraControlArray[m_SelectedControl].BringToFront();
                m_PlaybackControlArray[m_SelectedControl].BringToFront();
            }
            else
            {
                //1x2 View
                if (m_SelectedControl != 0)
                {
                    m_SelectedControl = 0;
                    m_LiveCameraControlArray[0].BringToFront();
                    m_LiveCameraControlArray[1].BringToFront();

                    m_PlaybackControlArray[0].BringToFront();
                    m_PlaybackControlArray[1].BringToFront();
                }
                else
                {
                    m_SelectedControl = 2;
                    m_LiveCameraControlArray[2].BringToFront();
                    m_LiveCameraControlArray[3].BringToFront();

                    m_PlaybackControlArray[2].BringToFront();
                    m_PlaybackControlArray[3].BringToFront();
                }
            }
        }

        /// <summary>
        /// The previousControl function is used to display the previous control depending on the current
        /// camera control view (e.g. 1x2)
        /// </summary>
        public void previousControl()
        {
            //If 1x1 View
            if (m_ViewLayout == ViewLayout.OneByOne)
            {
                m_SelectedControl--;

                if (m_SelectedControl < 0)
                {
                    m_SelectedControl = (m_LiveCameraControlArray.Length - 1);
                }

                m_LiveCameraControlArray[m_SelectedControl].BringToFront();
                m_PlaybackControlArray[m_SelectedControl].BringToFront();
            }
            else
            {
                //1x2 View
                if (m_SelectedControl != 0)
                {
                    m_SelectedControl = 0;
                    m_LiveCameraControlArray[0].BringToFront();
                    m_LiveCameraControlArray[1].BringToFront();

                    m_PlaybackControlArray[0].BringToFront();
                    m_PlaybackControlArray[1].BringToFront();
                }
                else
                {
                    m_SelectedControl = 2;
                    m_LiveCameraControlArray[2].BringToFront();
                    m_LiveCameraControlArray[3].BringToFront();

                    m_PlaybackControlArray[2].BringToFront();
                    m_PlaybackControlArray[3].BringToFront();
                }
            }
        }

        /// <summary>
        /// The getListofIncidents function searches the output folder for all incidents and
        /// makes them displayable as a datagridview
        /// </summary>
        /// <param name="incidents"></param>
        public void getListOfIncidents(DataGridView incidents)
        {
            m_IncidentGridView = incidents;
            //Check if the directory doesn't exist
            if (!Directory.Exists(Properties.Settings.Default.OutputLocation.ToString()))
            {
                //Create if the directory doesnt exist
                System.IO.Directory.CreateDirectory(Properties.Settings.Default.OutputLocation.ToString());
            }
            string[] folders = Directory.GetDirectories(Properties.Settings.Default.OutputLocation);
            m_SortableIncidentList = new SortableBindingList<Incident>();
            foreach (string incident in folders)
            {
                string[] incidentDetails = incident.Split('#');
                string[] incidentDate = incidentDetails[1].Split(',');

                //Parse the file string to retrieve all the details of the incident
                try
                {
                    DateTime date = new DateTime(Convert.ToInt16(incidentDate[2]), Convert.ToInt16(incidentDate[1]), Convert.ToInt16(incidentDate[0]),
                       Convert.ToInt16(incidentDate[3]), Convert.ToInt16(incidentDate[4]), Convert.ToInt16(incidentDate[5]));
                    Incident newIncident = new Incident(date, incidentDetails[2], Convert.ToInt32(incidentDetails[3]),
                        incidentDetails[4], null, incident);
                    m_SortableIncidentList.Add(newIncident);
                }
                catch (FormatException fe)
                {
                    MessageBox.Show(fe.ToString());
                }
            }

            updateIncidents(m_SortableIncidentList);
        }

        private void updateIncidents(SortableBindingList<Incident> incidentList)
        {
            if (m_IncidentGridView.InvokeRequired)
            {
                m_IncidentGridView.Invoke(new updateIncidentList(updateIncidents), m_SortableIncidentList);
            }
            else
            {
                m_IncidentGridView.DataSource = m_SortableIncidentList;
            }
        }

        /// <summary>
        /// Resize the camera controls as 1x1
        /// </summary>
        /// <param name="x">The width of the panel</param>
        /// <param name="y">The height of the panel</param>
        public void oneByOneResize(int x, int y)
        {
            //Iterate through the controls (final one first), ensuring that the first control
            //is brought to the front last.
            for (int i = (m_LiveCameraControlArray.Length - 1); i >= 0; i--)
            {
                //Adjust the live view
                m_LiveCameraControlArray[i].Width = x;
                m_LiveCameraControlArray[i].Height = y;
                m_LiveCameraControlArray[i].Location = new Point(0,0);
                m_LiveCameraControlArray[i].BringToFront();

                //Adjust the playback view
                m_PlaybackControlArray[i].Width = x;
                m_PlaybackControlArray[i].Height = y;
                m_PlaybackControlArray[i].Location = new Point(0, 0);
                m_PlaybackControlArray[i].BringToFront();
            }
            m_ViewLayout = ViewLayout.OneByOne;
            m_SelectedControl = 0;
        }

        /// <summary>
        /// Resize the camera controls as 2x3
        /// </summary>
        /// <param name="x">The width of the panel</param>
        /// <param name="y">The height of the panel</param>
        public void oneByTwoResize(int x, int y)
        {
            for (int i = (m_LiveCameraControlArray.Length - 1); i >= 0; i--)
            {
                m_LiveCameraControlArray[i].Width = x;
                m_LiveCameraControlArray[i].Height = y / 2;

                m_PlaybackControlArray[i].Width = x;
                m_PlaybackControlArray[i].Height = y / 2;

                //Distinguish between the controls and reposition/resize appropriately
                if ((i + 2) % 2 == 0)
                {
                    m_LiveCameraControlArray[i].Location = new Point(0, 0);
                    m_LiveCameraControlArray[i].BringToFront();

                    m_PlaybackControlArray[i].Location = new Point(0, 0);
                    m_PlaybackControlArray[i].BringToFront();
                }
                else
                {
                    m_LiveCameraControlArray[i].Location = new Point(0, y / 2);
                    m_PlaybackControlArray[i].Location = new Point(0, y / 2);
                }
            }
            m_ViewLayout = ViewLayout.OneByTwo;
            m_SelectedControl = 0;
        }

        /// <summary>
        /// Resize the camera controls as 2x2
        /// </summary>
        /// <param name="x">The width of the panel</param>
        /// <param name="y">The height of the panel</param>
        public void twoByTwoResize(int x, int y)
        {
            for (int i = (m_LiveCameraControlArray.Length - 1); i >= 0; i--)
            {
                m_LiveCameraControlArray[i].Width = x / 2;
                m_LiveCameraControlArray[i].Height = y / 2;

                m_PlaybackControlArray[i].Width = x / 2;
                m_PlaybackControlArray[i].Height = y / 2;

                //Distinguish between the controls and reposition/resize appropriately
                if ((i + 2) % 2 == 0)
                {
                    if ((i + 2) % 4 != 0)
                    {
                        m_LiveCameraControlArray[i].Location = new Point(0, 0);
                        m_PlaybackControlArray[i].Location = new Point(0, 0);
                    }
                    else
                    {
                        m_LiveCameraControlArray[i].Location = new Point(0, y / 2);
                        m_PlaybackControlArray[i].Location = new Point(0, y / 2);
                    }
                }
                else
                {
                    if ((i + 2) % 3 == 0)
                    {
                        m_LiveCameraControlArray[i].Location = new Point(x / 2, 0);
                        m_PlaybackControlArray[i].Location = new Point(x / 2, 0);
                    }
                    else
                    {
                        m_LiveCameraControlArray[i].Location = new Point(x / 2, y / 2);
                        m_PlaybackControlArray[i].Location = new Point(x / 2, y / 2);
                    }
                }
            }
            m_ViewLayout = ViewLayout.TwoByTwo;
        }

        /// <summary>
        /// The playbackIncident function is called using the row index of the binding list to obtain all of the
        /// incident files related to the incident
        /// </summary>
        /// <param name="rowIndex"></param>
        public void playbackIncident(int rowIndex)
        {
            //Mute all live audio streams
            muteAll(-1);
            //Get the directory of the incident files
            string directory = m_SortableIncidentList[rowIndex].Directory;
            //List the videos
            string[] videoFiles = Directory.GetFiles(directory, "*.avi");
            //Load the videos for review
            for (int i = 0; i < videoFiles.Length; i++)
            {
                IncidentReview video = new IncidentReview(videoFiles[i], m_PlaybackControlArray[i]);
                video.mute();
                m_IncidentReview[i] = video;
            }

            m_IncidentReview[0].unmute();
            changeActiveControl(m_PlaybackControlArray[0]);
        }

        /// <summary>
        /// The playbackEnd function is used to handle a 'end' button press from the
        /// playback view
        /// </summary>
        public void playbackEnd()
        {
            for (int i = 0; i < m_IncidentReview.Length; i++)
            {
                if (m_IncidentReview[i] != null)
                {
                    m_IncidentReview[i].end();
                }
            }
        }

        /// <summary>
        /// The playbackBeginning function is used to handle a 'beginning' button press from the
        /// playback view
        /// </summary>
        public void playbackBeginning()
        {
            for (int i = 0; i < m_IncidentReview.Length; i++)
            {
                if (m_IncidentReview[i] != null)
                {
                    m_IncidentReview[i].beginning();
                }
            }
        }

        /// <summary>
        /// The playbackPlay function is used to handle a 'play' button press from the
        /// playback view 
        /// </summary>
        public void playbackPlay()
        {
            for (int i = 0; i < m_IncidentReview.Length; i++)
            {
                if (m_IncidentReview[i] != null)
                {
                    m_IncidentReview[i].play();
                }
            }
        }

        /// <summary>
        /// The playbackPause function is used to handle a 'pause' button press from the
        /// playback view
        /// </summary>
        public void playbackPause()
        {
            for (int i = 0; i < m_IncidentReview.Length; i++)
            {
                if (m_IncidentReview[i] != null)
                {
                    m_IncidentReview[i].pause();
                }
            }
        }

        /// <summary>
        /// The playbackFastForward function is used to handle a 'fast forward' button press from the
        /// playback view
        /// </summary>
        public void playbackFastForward()
        {
            for (int i = 0; i < m_IncidentReview.Length; i++)
            {
                if (m_IncidentReview[i] != null)
                {
                    m_IncidentReview[i].fastForward();
                }
            }
        }

        /// <summary>
        /// The playbackRewind function is used to handle a 'Rewind' button press from the
        /// playback view
        /// </summary>
        public void playbackRewind()
        {
            for (int i = 0; i < m_IncidentReview.Length; i++)
            {
                if (m_IncidentReview[i] != null)
                {
                    m_IncidentReview[i].rewind();
                }
            }
        }

        /// <summary>
        /// The playbackEject function is used to handle a 'Eject' button press from the
        /// playback view
        /// </summary>
        public void playbackEject()
        {
            for (int i = 0; i < m_IncidentReview.Length; i++)
            {
                if (m_IncidentReview[i] != null)
                {
                    m_IncidentReview[i].eject();
                    m_IncidentReview[i] = null;
                }
            }
        }
    }
}
