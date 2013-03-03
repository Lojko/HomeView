using System;
using DirectShowLib;
using System.Timers;

namespace HomeView.Model
{
    /// <summary>
    /// USBCamera Class, derived from the absract camera class and overriding all defined absract methods.
    /// The Video Stream, Audio Stream and Output are all encapsulated in the camera class.
    /// </summary>
    class USBCamera : Camera
    {
        private Timer m_RecordTimer;
        private IMotionTrigger motionDetection;

        //Delegate for cross thread access
        private delegate void updateControlStatus(string status);

        /// <summary>
        /// First inherited constructor used when connecting a Video stream only
        /// </summary>
        /// <param name="parentControl">Parent Camera Control</param>
        /// <param name="videoDevice">The camera as a DsDevice</param>
        public USBCamera(CameraControl parentControl, DsDevice videoDevice)
            : base (parentControl)
        {
            motionDetection = new MotionDetection();
            motionDetection.triggered += new EventHandler(motionDetection_triggered);
            updateStatus("Connecting");
            m_CameraVideo = new USBCameraVideo(parentControl, videoDevice, motionDetection);
        }

        /// <summary>
        /// Second Inherited constructor used when connecting a Video stream and a Microphone audio
        /// stream output
        /// </summary>
        /// <param name="parentControl">Parent Camera Control</param>
        /// <param name="videoDevice">The camera as a DsDevice</param>
        /// <param name="microphoneGuid">The microphone as a GUID</param>
        public USBCamera(CameraControl parentControl, DsDevice videoDevice, Guid microphoneGuid)
            : base (parentControl)
        {
            motionDetection = new MotionDetection();
            motionDetection.triggered += new EventHandler(motionDetection_triggered);
            updateStatus("Connecting");
            m_CameraVideo = new USBCameraVideo(parentControl, videoDevice, motionDetection);
            m_CameraAudio = new USBCameraAudio(parentControl, microphoneGuid);
        }

        private void motionDetection_triggered(object sender, EventArgs e)
        {
            if (this.m_ParentControl.CameraStatus != "Recording")
            {
                updateStatus("Recording");
                //Create the incident object for multiple cameras
                Incident newIncident = new Incident(DateTime.Now, this.m_ParentControl.CameraName,
                    1, "Motion Detection", this.m_ParentControl.CameraName);
                //Record for a specific amount of time
                Record(30, newIncident);
            }
        }

        /// <summary>
        /// Overridden record method, this triggers the audio and video stream objects to record and collect
        /// their respective data.
        /// </summary>
        /// <param name="time"></param>
        public override void Record(int time, Incident incident)
        {
            m_CameraOutput = new OutputAvi(incident);
            m_CameraVideo.Output = m_CameraOutput;

            updateStatus("Recording");
            m_CameraVideo.Record();
            //Check if a microphone has been connected
            if (CameraAudio != null)
            {
                CameraAudio.Output = m_CameraOutput;
                CameraAudio.Record();
            }

            m_RecordTimer = new Timer();
            m_RecordTimer.Elapsed += new ElapsedEventHandler(stopRecord);
            m_RecordTimer.Interval = 1000 * time;
            m_RecordTimer.Enabled = true;
        }

        /// <summary>
        /// The stopRecord function finishes off the recording and production of the video output
        /// file, with or without the audio (if a microphone has been connected)
        /// </summary>
        public override void stopRecord(Object myObject, EventArgs myEventArgs)
        {
            m_RecordTimer.Stop();
            m_CameraVideo.stopRecord();

            //Check if a microphone has been connected
            if (CameraAudio != null)
            {
                CameraAudio.stopRecord();
            }

            //Cross thread access, updating the camera control
            m_ParentControl.Invoke(new updateControlStatus(this.updateStatus),
                new object[] { "Creating Files" });
            m_CameraOutput.closeOutput();
            m_CameraOutput = null;
            m_ParentControl.Invoke(new updateControlStatus(this.updateStatus),
                new object [] {"Connected"});
            incidentComplete(EventArgs.Empty);
        }

        /// <summary>
        /// The disconnect method stops all of the video and audio output to the main form and
        /// releases all resources
        /// </summary>
        public override void Disconnect()
        {
            if (CameraAudio != null)
            {
                CameraAudio.Stop();
            }
            m_CameraVideo.Disconnect();
        }

        /// <summary>
        /// Mutes the audio of the microphone
        /// </summary>
        public void Mute()
        {
            CameraAudio.Mute();
        }

        /// <summary>
        /// Updates the status of the Camera control
        /// </summary>
        /// <param name="status">Status that is to be updated as a string</param>
        private void updateStatus(string status)
        {
            if (m_ParentControl.InvokeRequired)
            {
                m_ParentControl.Invoke(new updateControlStatus(this.updateStatus), new object[] { status });
            }
            else
            {
                m_ParentControl.CameraStatus = status;
            }
        }
    }
}
