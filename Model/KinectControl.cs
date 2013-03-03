using System;
using System.Windows.Forms;
using Microsoft.Kinect;

namespace HomeView.Model
{
    class KinectControl
    {
        #region Declarations

        private KinectSensor m_Kinect;
        private Skeleton[] m_SkeletonData;
        private Screen m_PrimaryScreen;
        private KinectStatus m_KinectStatus;

        #endregion

        public KinectStatus Status
        {
            get { return m_KinectStatus; }
            set { m_KinectStatus = value; }
        }

        /// <summary>
        /// KinectControl Constuctor, used to start the Kinect and retrieve important variable values
        /// </summary>
        public KinectControl()
        {
            //The Kinect SDK now handles multiple kinects connected to the machine, 
            //for HomeView only the first is required
            if (KinectSensor.KinectSensors.Count < 1)
            {
                //Kinect not connected
                MessageBox.Show("Kinect not Connected");
                m_KinectStatus = KinectStatus.Disconnected;
            }
            else
            {
                //Start the Kinect
                m_Kinect = KinectSensor.KinectSensors[0];
                m_Kinect.SkeletonStream.Enable();
                m_Kinect.Start();

                //Bind all delegate functions
                m_Kinect.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(kinect_AllFramesReady);
                this.m_SkeletonData = new Skeleton[m_Kinect.SkeletonStream.FrameSkeletonArrayLength];
                m_Kinect.SkeletonStream.Enable(new TransformSmoothParameters()
                {
                    //Smoothing ratio to prevent cursor shaking
                    Smoothing = 0.8f,
                    Correction = 0.5f,
                    Prediction = 0.5f,
                    JitterRadius = 0.05f,
                    MaxDeviationRadius = 0.04f
                });

                //Retrieve the primary screens dimensions for cursor control
                foreach (Screen screen in System.Windows.Forms.Screen.AllScreens)
                {
                    if (screen.Primary == true)
                    {
                        m_PrimaryScreen = screen;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void kinect_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            //Use the skeleton frame when one is detected
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    if ((this.m_SkeletonData == null) || (this.m_SkeletonData.Length != skeletonFrame.SkeletonArrayLength))
                    {
                        this.m_SkeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    }

                    skeletonFrame.CopySkeletonDataTo(this.m_SkeletonData);
                }
            }

            //Use the data of the skeletal points
            foreach (Skeleton data in this.m_SkeletonData)
            {
                //Data only required from the left and right hand. An array of 3 floats used to
                //track x,y and z position of hands
                float[] rightHand = new float[3];
                float[] leftHand = new float[3];
                if (SkeletonTrackingState.Tracked == data.TrackingState)
                {
                    foreach (Joint joint in data.Joints)
                    {
                        //If the joint is the right hand, set the dimensions
                        if (joint.JointType == JointType.HandRight)
                        {
                            rightHand[0] = joint.Position.Z;
                            rightHand[1] = joint.Position.X;
                            rightHand[2] = joint.Position.Y;
                        }
                        //If the joint is the left hand, set the dimensions
                        else if (joint.JointType == JointType.HandLeft)
                        {
                            leftHand[0] = joint.Position.Z;
                            leftHand[1] = joint.Position.X;
                            leftHand[2] = joint.Position.Y;
                        }
                    }

                    //If the right hand is being used to control (checked via the Z value of the hand)
                    if (rightHand[0] < leftHand[0])
                    {
                        setCursorPosition(rightHand[1], rightHand[2]);
                    }
                    else
                    {
                        setCursorPosition(leftHand[1], leftHand[2]);
                    }
                }
            }
        }

        /// <summary>
        /// The setCursorPostion function uses the x and y value of the control hand and converts it to a appropriate 
        /// 2D x,y value of the monior, setting the position of the cursor using the width and height of the primary monitor.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void setCursorPosition(float x, float y)
        {
            //Calculate the x value of the cursor using the x value of the hand
            int widthPos = (int)((((m_PrimaryScreen.Bounds.Width + (2 * 400)) / 2) * x) + (m_PrimaryScreen.Bounds.Width / 2));
            //Calculate the y value of the cursor using the y value of the hand
            int heightPos = (int)((-((m_PrimaryScreen.Bounds.Height + (2 * 400)) / 2) * y) + (m_PrimaryScreen.Bounds.Height / 2));

            //Set the position of the cursor
            System.Drawing.Point mousePos = new System.Drawing.Point(widthPos, heightPos);
            System.Windows.Forms.Cursor.Position = mousePos;
        }
        
        /// <summary>
        /// The increase elevation function is used to change the position of the kinect, if the kinect is raised too far
        /// an error is handled.
        /// </summary>
        public void increaseElevation()
        {
            try
            {
                m_Kinect.ElevationAngle += 10;
            }
            //If the kinect is risen too far exception is raised, prevent the user from raising the kinect too far
            catch(ArgumentOutOfRangeException){}
        }

        /// <summary>
        /// The decrease elevation function is used to change the position of the kinect, if the kinect is lowered too far
        /// an error is handled.
        /// </summary>
        public void decreaseElevation()
        {
            try
            {
                m_Kinect.ElevationAngle -= 10;
            }
            //If the kinect is lowered too far exception is raised, prevent the user from lowering the kinect too far
            catch (ArgumentOutOfRangeException){}
        }

        /// <summary>
        /// the kinectStop function is used to stop the sensor
        /// </summary>
        public void kinectStop()
        {
            foreach (KinectSensor sensor in KinectSensor.KinectSensors)
            {
                sensor.Stop();
            }
        }
    }
}
