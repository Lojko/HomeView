using System;
using System.Collections.Generic;
using DirectShowLib;
using Microsoft.DirectX.DirectSound;

namespace HomeView.Presenter
{
    /// <summary>
    /// Layer between the View and the model where all of the model integrating functionality of a view is stored
    /// </summary>
    class frmConnectCameraPresenter
    {
        #region Declarations

        //Create a list of dsdevices and empty string list to add the names of the cameras to.
        private DsDevice[] m_DeviceArray = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

        //Create a capture device collection and string list to add the names of the microphones to.
        private CaptureDevicesCollection m_MicrophoneCollection = new CaptureDevicesCollection();

        #endregion

        /// <summary>
        /// Function which retrieves a list of the avaliable cameras
        /// </summary>
        /// <returns>List of Cameras</returns>
        public List<String> getCameras()
        {
            List<String> cameras = new List<string>();

            if (m_DeviceArray.Length > 0)
            {
                foreach (DsDevice device in m_DeviceArray)
                {
                    cameras.Add(device.Name);
                }
            }

            return cameras;
        }

        /// <summary>
        /// Function which retrieves all of the names of the available microphones
        /// </summary>
        /// <returns>String list of Microphones</returns>
        public List<String> getMicrophones()
        {
            List<String> microphones = new List<string>();
            if (m_MicrophoneCollection.Count > 0)
            {
                for (int i = 0; i < m_MicrophoneCollection.Count; i++)
                {
                    microphones.Add(m_MicrophoneCollection[i].Description);
                }
            }

            return microphones;
        }

        /// <summary>
        /// Get a specific camera
        /// </summary>
        /// <param name="selectedIndex">The index of the dropdownlist which has been selected</param>
        /// <returns></returns>
        public DsDevice getCamera(int selectedIndex)
        {
            if (selectedIndex >= 0)
            {
                return m_DeviceArray[selectedIndex];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get a specific microphone
        /// </summary>
        /// <param name="selectedIndex">The index of the dropdownlist which has been selected</param>
        /// <returns></returns>
        public Guid getMicrophone(int selectedIndex)
        {
            if (selectedIndex >= 0)
            {
                return m_MicrophoneCollection[selectedIndex].DriverGuid;
            }
            else
            {
                return Guid.Empty;
            }
        }
    }
}
