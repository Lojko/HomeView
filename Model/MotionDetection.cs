using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace HomeView.Model
{
    /// <summary>
    /// The motion detection algorithmn class is used to calculate when motion is experienced between
    /// a base frame and the current frame.
    /// </summary>
    class MotionDetection : Trigger, IMotionTrigger
    {
        #region Declarations

        public event EventHandler triggered;

        private bool m_Active;
        private Bitmap m_BackgroundFrame;
        private int m_NoMovementCounter;
        private int m_PreviousPixelsChanged;
        private const int PIXELDIFFERENCE = 65;

        #endregion

        /// <summary>
        /// Motion detection is constructed with an inital frame which all other frames are compared against
        /// </summary>
        /// <param name="firstFrame">First frame received from the video source</param>
        public MotionDetection()
            : base()
        {
            m_NoMovementCounter = 0;
            m_PreviousPixelsChanged = 0;
        }

        /// <summary>
        /// Gets the state of the motion trigger
        /// </summary>
        /// <returns>A bool ditacting whether it is active or not</returns>
        public bool getMotionTriggerActiveState()
        {
            return m_Active;
        }

        /// <summary>
        /// Activates and deactivates the motion detection trigger
        /// </summary>
        public void activateMotionTrigger()
        {
            if(m_Active == true)
            {
                m_Active = false;
            }
            else
            {
                m_Active = true;
            }
        }

        /// <summary>
        /// Function which checks the state of the background frame
        /// </summary>
        /// <returns>Returns true if a background frame has already been set</returns>
        public bool checkBackgroundFrame()
        {
            if (m_BackgroundFrame != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Function used to get a series of pixels as a byte[] array, this way each pixel can be accessed and modified
        /// safely
        /// </summary>
        /// <param name="frameBitmapData">Bitmap data of the frame</param>
        /// <returns>Byte array of the frame (each pixel is three slots in the array R,G and B)</returns>
        private byte[] getFramePixels(BitmapData frameBitmapData)
        {
            //Assign the first line of the frame
            IntPtr frameFirstLine = frameBitmapData.Scan0;
            //Retrieve the bytes associated with the first bytes of the frame
            int frameBytes = Math.Abs(frameBitmapData.Stride) * frameBitmapData.Height;
            //Assign the byte array
            byte[] frameRGBValues = new byte[frameBytes];
            //Copy the bytes of the first line into the byte array
            System.Runtime.InteropServices.Marshal.Copy(frameFirstLine, frameRGBValues, 0, frameBytes);
            return frameRGBValues;
        }

        /// <summary>
        /// The setFramePixelFormat is used to change the format of the actual frame, different formats yeild different
        /// fileds of colour, which in turn have a difference on the pixels of the frame. Format24BppRgb is used as it is 
        /// decent quality and has an RGB value for each pixel of the frame
        /// </summary>
        /// <param name="frame">The frame that the pixel format is being changed for</param>
        /// <returns>A bitmap of the frame in the appropriate pixelformat</returns>
        private Bitmap setFramePixelFormat(Bitmap frame)
        {
            return frame.Clone(new Rectangle(0, 0, frame.Width, frame.Height), System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        }

        /// <summary>
        /// The setBackgroundFrame function changes the background frame to the current frame. This occurs if an object has moved into
        /// the view of the frame and has stopped, thus still being considered as movement. The change in frame will render the object as
        /// not moving.
        /// </summary>
        /// <param name="backgroundFrame">Frame to be set as the background</param>
        public void setBackgroundFrame(Bitmap backgroundFrame)
        {
            m_BackgroundFrame = setFramePixelFormat(backgroundFrame);
        }

        /// <summary>
        /// Motion detection function. By comparing the current newFrame to the current frame stored as the
        /// background frame, a motion analysis can be performed and the resulting motion can be displayed on the
        /// outputted frame.
        /// </summary>
        /// <param name="newFrame">New frame received</param>
        /// <returns>An altered 'newFrame' with motion</returns>
        public Bitmap detectMotion(Bitmap newFrame)
        {
            //Clone the frame
            Bitmap editableNewFrame = setFramePixelFormat(newFrame);
            //Get the editable area of the frame
            Rectangle editableArea = new Rectangle(0, 0, editableNewFrame.Width, editableNewFrame.Height);
            //Lock the bits
            BitmapData firstFrameBitmapData = m_BackgroundFrame.LockBits(editableArea, System.Drawing.Imaging.ImageLockMode.ReadWrite, m_BackgroundFrame.PixelFormat);
            BitmapData secondFrameBitmapData = editableNewFrame.LockBits(editableArea, System.Drawing.Imaging.ImageLockMode.ReadWrite, editableNewFrame.PixelFormat);
            byte[] firstFrameRGBValues = getFramePixels(firstFrameBitmapData);
            byte[] secondFrameRGBValues = getFramePixels(secondFrameBitmapData);
            int pixelsChanged = 0;
            for (int i = 0; i < secondFrameRGBValues.Length; i += 3)
            {
                //Check the blue pixel channel, displaying only the red channel when motion is detected
                if (secondFrameRGBValues[i] < firstFrameRGBValues[i] - PIXELDIFFERENCE || secondFrameRGBValues[i] > firstFrameRGBValues[i] + PIXELDIFFERENCE)
                {
                    secondFrameRGBValues[i] = 0;
                    secondFrameRGBValues[i + 1] = 0;
                    secondFrameRGBValues[i + 2] = 255;
                    pixelsChanged++;
                }
                //Check the green pixel channel, displaying only the red channel when motion is detected
                else if (secondFrameRGBValues[i + 1] < firstFrameRGBValues[i + 1] - PIXELDIFFERENCE || secondFrameRGBValues[i + 1] > firstFrameRGBValues[i + 1] + PIXELDIFFERENCE)
                {
                    secondFrameRGBValues[i] = 0;
                    secondFrameRGBValues[i + 1] = 0;
                    secondFrameRGBValues[i + 2] = 255;
                    pixelsChanged++;
                }
                //Check the red pixel channel, displaying only the red channel when motion is detected
                else if (secondFrameRGBValues[i + 2] < firstFrameRGBValues[i + 2] - PIXELDIFFERENCE || secondFrameRGBValues[i + 2] > firstFrameRGBValues[i + 2] + PIXELDIFFERENCE)
                {
                    secondFrameRGBValues[i] = 0;
                    secondFrameRGBValues[i + 1] = 0;
                    secondFrameRGBValues[i + 2] = 255;
                    pixelsChanged++;
                }
            }

            //Assign the first line of the frame
            IntPtr secondFrameFirstLine = secondFrameBitmapData.Scan0;
            //Retrieve the bytes associated with the first bytes of the frame
            int secondFrameBytes = Math.Abs(secondFrameBitmapData.Stride) * secondFrameBitmapData.Height;
            System.Runtime.InteropServices.Marshal.Copy(secondFrameRGBValues, 0, secondFrameFirstLine, secondFrameBytes);
            m_BackgroundFrame.UnlockBits(firstFrameBitmapData);
            editableNewFrame.UnlockBits(secondFrameBitmapData);

            //Check for motion and reset the currentframe if new objects in the frame have stopped moving
            if (pixelsChanged > 6500)
            {
                triggered(this,null);

                //Motion Detected, Check which is greater
                if (m_PreviousPixelsChanged > pixelsChanged)
                {
                    //Subtract and if the amount of pixels that is changing is similar, increase the counter
                    if (m_PreviousPixelsChanged - pixelsChanged < 350)
                    {
                        m_NoMovementCounter++;
                    }
                }
                else if (pixelsChanged > m_PreviousPixelsChanged)
                {
                    //Subtract and if the amount of pixels that is changing is similar, increase the counter
                    if (m_PreviousPixelsChanged - pixelsChanged < 350)
                    {
                        m_NoMovementCounter++;
                    }
                }
                else
                {
                    //Reset the movement counter as no more movement is detected
                    m_NoMovementCounter = 0;
                }

                //If the counter has been incremented too many times in a row, the background frame should be reset as an object
                //may have moved into the view, but has stopped in the view.
                if (m_NoMovementCounter > 75)
                {
                    setBackgroundFrame(newFrame);
                    m_NoMovementCounter = 0;
                }

                m_PreviousPixelsChanged = pixelsChanged;
            }

            return editableNewFrame;
        }
    }
}
