using System;

namespace HomeView.Model
{
    /// <summary>
    /// Incident class dictates what is required to create an incident and the overloaded constructors
    /// that can be used to create an 'incident'
    /// </summary>
    public class Incident
    {
        #region Declarations

        private DateTime m_EventTime;
        private int m_NumberofCameras;
        private string m_TriggeringCameraName;
        private string m_IncidentTrigger;
        private string m_CameraName;
        private string m_Directory;

        #endregion

        public DateTime EventTime
        {
            get { return m_EventTime; }
            set { m_EventTime = value; }
        }

        public int NumberOfCameras
        {
            get { return m_NumberofCameras; }
            set { m_NumberofCameras = value; }
        }

        public string IncidentTrigger
        {
            get { return m_IncidentTrigger; }
            set { m_IncidentTrigger = value; }
        }

        public string TriggeringCameraName
        {
            get { return m_TriggeringCameraName; }
            set { m_TriggeringCameraName = value; }
        }

        public string CameraName
        {
            get { return m_CameraName; }
            set { m_CameraName = value; }
        }

        public string Directory
        {
            get { return m_Directory; }
            set { m_Directory = value; }
        }

        /// <summary>
        /// First constuctor used when creating an incident
        /// </summary>
        /// <param name="timeTriggered">Time the incident was trigger</param>
        /// <param name="triggeringCamera">Name of the camera that triggered the incident</param>
        /// <param name="numberOfCameras">Number of cameras that were part of the incident</param>
        /// <param name="trigger">How the incident was triggered</param>
        /// <param name="cameraName">Camera name of the connected camera</param>
        public Incident(DateTime timeTriggered, string triggeringCamera, int numberOfCameras, string trigger, string cameraName)
        {
            m_EventTime = timeTriggered;
            m_NumberofCameras = numberOfCameras;
            m_CameraName = cameraName;
            m_IncidentTrigger = trigger;
            m_TriggeringCameraName = triggeringCamera;
        }

        /// <summary>
        /// Overloaded Constructor used to access pre-existing incidents
        /// </summary>
        /// <param name="timeTriggered">Time the incident was trigger</param>
        /// <param name="triggeringCamera">Name of the camera that triggered the incident</param>
        /// <param name="numberOfCameras">Number of cameras that were part of the incident</param>
        /// <param name="trigger">How the incident was triggered</param>
        /// <param name="cameraName">Camera name of the connected camera</param>
        /// <param name="directory">The directory the file is stored at</param>
        public Incident(DateTime timeTriggered, string triggeringCamera, int numberOfCameras, string trigger, string cameraName, string directory)
        {
            m_EventTime = timeTriggered;
            m_NumberofCameras = numberOfCameras;
            m_Directory = directory;
            m_IncidentTrigger = trigger;
            m_TriggeringCameraName = triggeringCamera;
        }
    }
}
