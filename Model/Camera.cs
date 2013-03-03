using System;

namespace HomeView.Model
{
    /// <summary>
    /// The base abstract Camera class dictates all of the required functionality that a camera class should
    /// have implemented, including any future version of camera that could be developed and included in the
    /// application.
    /// </summary>
    abstract class Camera
    {
        #region Declarations

        protected CameraControl m_ParentControl;
        protected ICameraVideo m_CameraVideo;
        protected ICameraAudio m_CameraAudio;
        protected ICameraOutput m_CameraOutput;
        public event EventHandler newIncident;

        #endregion

        public ICameraAudio CameraAudio
        {
            get { return m_CameraAudio; }
        }

        public ICameraVideo CameraVideo
        {
            get { return m_CameraVideo; }
        }

        /// <summary>
        /// The first camera constructor can be used when only the video of a camera is required or available,
        /// the audio GUID is not a parameter requirement.
        /// </summary>
        /// <param name="parentControl">The controlling parent camera control</param>
        public Camera(CameraControl parentControl)
        {
            m_ParentControl = parentControl;
        }

        /// <summary>
        /// Function to trigger the new incident event
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected void incidentComplete(EventArgs e)
        {
            EventHandler incidentHandler = newIncident;
            if (incidentHandler != null)
            {
                incidentHandler(this, e);
            }
        }

        /// <summary>
        /// Abstract Record function which all derived classes must override and implement with their own 
        /// implementation, but adhere to to the method skeleton. The record method requires a length of time
        /// to record.
        /// </summary>
        public abstract void Record(int time, Incident incident);

        /// <summary>
        /// Abstract stopRecord function which all derived classes must override and implement with their own 
        /// implementation, but adhere to to the method skeleton.
        /// </summary>
        public abstract void stopRecord(Object myObject, EventArgs myEventArgs);

        /// <summary>
        /// Abstract Disconnect function which all derived classes must override and implement with their own 
        /// implementation, but adhere to to the method skeleton.
        /// </summary>
        public abstract void Disconnect();
    }
}
