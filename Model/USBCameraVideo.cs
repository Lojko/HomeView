/***************************************************************************************
 This class was developed with the aid of the PlayCap sample application, from the 
 * DirectShow samples available at http://sourceforge.net/projects/directshownet/files/
 * and Andrew Kirillov's Motion Detection example 
 * http://www.codeproject.com/KB/audio-video/Motion_Detection.aspx?msg=1603954
***************************************************************************************/

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DirectShowLib;

namespace HomeView.Model
{
    class USBCameraVideo : ICameraVideo
    {
        #region Declarations

        private IVideoWindow m_VideoWindow;
        private IMediaControl m_MediaControl;
        private IMediaEventEx m_MediaEventEx;
        private IGraphBuilder m_GraphBuilder;
        private ISampleGrabber m_SampleGrabber;
        private ICaptureGraphBuilder2 m_CaptureGraphBuilder;

        private CameraControl m_Control;
        private DsDevice m_CameraDevice;
        private byte[] m_CurrentFrame;
        private Bitmap m_CurrentFrameAsBitmap;
        private int m_FramesPerSecond;
        private int m_FramesRecorded;

        private System.Timers.Timer m_FPSTimer;
        private System.Timers.Timer m_RecordTimer;

        private bool m_FPSReset;
        private bool m_Recording;

        private event frameReceived m_FrameTrigger;
        private delegate void frameReceived();

        private ICameraOutput m_Output;

        private IMotionTrigger m_MotionDetection;

        #endregion

        public ICameraOutput Output
        {
            set { m_Output = value; }
        }

        public int FramesPerSecond
        {
            get { return m_FramesPerSecond; }
        }

        public byte[] CurrentFrame
        {
            get { return m_CurrentFrame; }
        }

        //Sample Grabber Class ID
        //Found at: http://social.msdn.microsoft.com/Forums/en-US/windowsdirectshowdevelopment/thread/ac877e2d-80a7-47b6-b315-5e3160b8b219
        private Guid CLSID_SampleGrabber =
            new Guid(0xC1F400A0, 0x3F08, 0x11D3, 0x9F, 0x0B, 0x00, 0x60, 0x08, 0x03, 0x9E, 0x37);

        /// <summary>
        /// Camera class, requires a control and a camera device to initalize
        /// </summary>
        /// <param name="control"></param>
        /// <param name="device"></param>
        public USBCameraVideo(CameraControl control, DsDevice device)
        {
            m_GraphBuilder = (IGraphBuilder)new FilterGraph();
            m_CaptureGraphBuilder = (ICaptureGraphBuilder2)new CaptureGraphBuilder2();
            m_MediaControl = (IMediaControl)m_GraphBuilder;
            m_VideoWindow = (IVideoWindow)m_GraphBuilder;
            m_MediaEventEx = (IMediaEventEx)m_GraphBuilder;

            m_Control = control;
            m_CameraDevice = device;

            m_FPSReset = false;
            m_Recording = false;

            m_FPSTimer = new System.Timers.Timer();
            m_FPSTimer.Interval = 1000;
            m_FPSTimer.Elapsed += new System.Timers.ElapsedEventHandler(m_FPSTimer_Tick);
            m_FrameTrigger += new frameReceived(incrementFramesPerSecond);
            Connect();
            m_FPSTimer.Start();
            m_Control.CameraStatus = "Connected";
        }

        public USBCameraVideo(CameraControl control, DsDevice device, IMotionTrigger motionDetection)
            :this(control, device)
        {
            m_MotionDetection = motionDetection;
        }

        /// <summary>
        /// The incrementFramesPerSecond function is called everytime a frame is received during the intialization of the
        /// camera to calculate an estimate of the current camera framerate. Framerate can vary during live use.
        /// </summary>
        private void incrementFramesPerSecond()
        {
            m_FramesPerSecond++;
        }

        /// <summary>
        /// Function which is called when the camera is connected, last for a second.
        /// Used to capture how many frames are received on that initial second.
        /// </summary>
        /// <param name="myObject">Object passed via the event</param>
        /// <param name="myEventArgs">Event arguments</param>
        private void m_FPSTimer_Tick(Object myObject, EventArgs myEventArgs)
        {
            if (m_FPSReset == false)
            {
                m_FPSReset = true;
                m_FramesPerSecond = 0;
            }
            else
            {
                m_FPSTimer.Stop();
                m_FrameTrigger -= new frameReceived(incrementFramesPerSecond);
            }
        }

        /// <summary>
        /// The stopRecord function stops the recording timer and triggers the videoReady event.
        /// </summary>
        /// <param name="myObject">Object passed via the event</param>
        /// <param name="myEventArgs">Event arguments</param>
        public void stopRecord()
        {
            m_Recording = false;
            m_RecordTimer.Stop();
            m_FramesRecorded = 0;
        }

        /// <summary>
        /// Record function which starts the recording timer and initalizes the recording array output.
        /// </summary>
        public void Record()
        {
            //Set the output frame rate to the calculated FPS, converting it to a double first and providing the first frame of the
            //record
            m_Output.setVideoSettings(Convert.ToDouble(m_FramesPerSecond), m_CurrentFrame);
            m_Recording = true;

            //Record timer, to ensure it records for a specified amount of time
            m_RecordTimer = new System.Timers.Timer();
            m_RecordTimer.Interval = 1000;
            m_RecordTimer.Elapsed += new System.Timers.ElapsedEventHandler(resetFrames);
            m_RecordTimer.Start();
        }

        /// <summary>
        /// Reset the amount of frames recorded after a successful record has taken place
        /// </summary>
        /// <param name="myObject">Object passed via the event</param>
        /// <param name="myEventArgs">Event arguments</param>
        private void resetFrames(Object myObject, EventArgs myEventArgs)
        {
            m_FramesRecorded = 0;
        }

        /// <summary>
        /// Capture video funtion which establishes the video stream and plays it
        /// </summary>
        public void Connect()
        {
            try
            {
                // Attach the filter graph to the capture graph
                int hr = this.m_CaptureGraphBuilder.SetFiltergraph(m_GraphBuilder);
                DsError.ThrowExceptionForHR(hr);

                // Bind Moniker of the chosen device with the base filter GUID to create a filter object
                object captureSource;
                Guid baseFilterGUID = typeof(IBaseFilter).GUID;
                m_CameraDevice.Mon.BindToObject(null, null, ref baseFilterGUID, out captureSource);
                IBaseFilter captureFilter = (IBaseFilter)captureSource;

                //Build the Grabber Filter using the class ID of the grabber
                object grabberObj = Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_SampleGrabber));
                m_SampleGrabber = (ISampleGrabber)grabberObj;
                IBaseFilter grabberFilter = (IBaseFilter)grabberObj;

                // Add the Capture filter to the graph.
                hr = this.m_GraphBuilder.AddFilter(captureFilter, "Video Capture");
                DsError.ThrowExceptionForHR(hr);
                hr = this.m_GraphBuilder.AddFilter(grabberFilter, "Image grabber");
                DsError.ThrowExceptionForHR(hr);

                //Set the media type of the grabber filter
                AMMediaType mediaType = new AMMediaType();
                mediaType.majorType = MediaType.Video;
                mediaType.subType = MediaSubType.RGB24;
                m_SampleGrabber.SetMediaType(mediaType);

                //Declare the frame grabber filter
                FrameGrabber streamGrabber = new FrameGrabber(this);

                /*Search and select for the suitable pin for both the capture graph and grabber graph. As the grabber graph
                 * needs to 'peek' on the actual capture graph, the output pin of the capture graph must be found and the input
                 pin of the grabber filter must also be found.*/
                IPin captureOutputPin = searchPins(captureFilter, PinDirection.Output);
                IPin captureInputPin = searchPins(captureFilter, PinDirection.Input);
                IPin grabberInputPin = searchPins(grabberFilter, PinDirection.Input);

                //Connect the capture graph and the grabber graph pins so the grabber can access the video stream
                m_GraphBuilder.Connect(captureOutputPin, grabberInputPin);

                if (m_SampleGrabber.GetConnectedMediaType(mediaType) == 0)
                {
                    VideoInfoHeader streamInfo = 
                        (VideoInfoHeader)Marshal.PtrToStructure(mediaType.formatPtr, typeof(VideoInfoHeader));
                    streamGrabber.Width = streamInfo.BmiHeader.Width;
                    streamGrabber.Height = streamInfo.BmiHeader.Height;
                }

                // Render the camera
                m_GraphBuilder.Render(captureInputPin);

                //Prevent sample data being copied
                m_SampleGrabber.SetBufferSamples(false);
                //Prevent the Sample Grabber from halting after receiving
                m_SampleGrabber.SetOneShot(false);
                //Bind the callback function of the Sample Grabber
                m_SampleGrabber.SetCallback(streamGrabber, 1);

                // Now that the filter has been added to the graph and we have
                // rendered its stream, we can release this reference to the filter.
                Marshal.ReleaseComObject(captureFilter);

                // Start previewing video data
                IVideoWindow videoWindow = (IVideoWindow)m_GraphBuilder;
                videoWindow.put_AutoShow(OABool.False);
                hr = m_MediaControl.Run();
                DsError.ThrowExceptionForHR(hr);
            }
            catch
            {
                MessageBox.Show(m_CameraDevice.Name + "Error",
                    "The camera " + m_CameraDevice.Name  +" could not be initalized due to an internal error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                m_Control.CameraStatus = "Disconnected";
            }
        }

        /// <summary>
        /// SearchPins method which finds the correct pin based on its
        /// queried direction and the inputted filter
        /// </summary>
        /// <param name="filter">The filter the pin is required for</param>
        /// <param name="queryDirection">Which pin direction is needed (e.g. Input or Output)</param>
        /// <returns></returns>
        private IPin searchPins(IBaseFilter filter, PinDirection queryDirection)
        {
            IEnumPins pins;
            IPin[] searchPin = new IPin[1];
            IntPtr n = IntPtr.Zero;
            filter.EnumPins(out pins);
            while (pins.Next(1, searchPin, n) == 0)
            {
                PinDirection direction;
                //Query the direction of the pin
                searchPin[0].QueryDirection(out direction);
                if (direction == queryDirection)
                {
                    return searchPin[0];
                }
            }
            return null;
        }

        /// <summary>
        /// Disconnect method which ensures that all resources used by the camera stream are disposed
        /// and the camera is stopped safely
        /// </summary>
        public void Disconnect()
        {
            //Stop the Camera stream
            m_MediaControl.StopWhenReady();

            //Release the remaining com objects
            Marshal.ReleaseComObject(m_MediaControl); m_MediaControl = null;
            Marshal.ReleaseComObject(m_MediaEventEx); m_MediaEventEx = null;
            Marshal.ReleaseComObject(m_VideoWindow); m_VideoWindow = null;
            Marshal.ReleaseComObject(m_GraphBuilder); m_GraphBuilder = null;
            Marshal.ReleaseComObject(m_CaptureGraphBuilder); m_CaptureGraphBuilder = null;
        }

        /// <summary>
        /// New Frame method passes the frame from the video stream into the appropriate displaying video box.
        /// Alternatively the image could also be passed as a byte[], to be stored and reverted back to an image later.
        /// </summary>
        /// <param name="frame"></param>
        public void newFrame(Bitmap frame)
        {
            try
            {
                if (frame != null)
                {
                    //Create a MemoryStream
                    MemoryStream frameMemoryStream = new MemoryStream();
                    //Convert the bitmap into a JPEG Image
                    frame.Save(frameMemoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    //Dispose the memorystream
                    frameMemoryStream.Dispose();

                    //Check if an inital frame has not been set
                    if (m_CurrentFrameAsBitmap != null)
                    {
                        //Set the background frame
                        if (m_MotionDetection != null && m_MotionDetection.checkBackgroundFrame() == false)
                        {
                            m_MotionDetection.setBackgroundFrame(m_CurrentFrameAsBitmap);
                            m_Control.VideoBox.Image =  frame;
                        }
                    }

                    //Check if motion detection is active
                    if (m_MotionDetection != null && m_MotionDetection.getMotionTriggerActiveState() == true)
                    {
                        //Continue to detect motion
                        m_Control.VideoBox.Image = m_MotionDetection.detectMotion(frame);
                    }
                    else
                    {
                        //Set the videobox control to the image
                        m_Control.VideoBox.Image = frame;
                    }

                    //Convert the current frame into a Byte[]
                    m_CurrentFrame = frameMemoryStream.ToArray();
                    m_CurrentFrameAsBitmap = frame;

                    if (m_FrameTrigger != null)
                    {
                        //Trigger frame received
                        m_FrameTrigger();
                    }
                    //If recording has been started, add the frame to the output
                    if (m_Recording == true)
                    {
                        if (m_FramesRecorded <= (m_FramesPerSecond - 1))
                        {
                            m_Output.addFrame(m_CurrentFrame);
                            m_FramesRecorded++;
                        }
                    }
                }
            }            
            catch (Exception) 
            {
                MessageBox.Show("The camera " + m_CameraDevice.Name + " is unable to stream frames.", m_CameraDevice.Name + "error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Activate or Deactivates Motion Detection
        /// </summary>
        public void activateMotionTrigger()
        {
            if (m_MotionDetection != null)
            {
                m_MotionDetection.activateMotionTrigger();
            }
        }
    }
}