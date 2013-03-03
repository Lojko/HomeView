using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DirectShowLib;

namespace HomeView.Model
{
    /// <summary>
    /// The FrameGrabber class which pull frames from the video stream, when a frame has been sucessfully pulled,
    /// the callback method is called and
    /// </summary>
    class FrameGrabber : ISampleGrabberCB
    {
        //DLLImport of native function memcpy, used to efficently copy memory from one item to another
        [DllImport("ntdll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int memcpy(
            int dst,
            int src,
            int count);

        #region Declarations

        private USBCameraVideo m_Parent;
        private int m_FrameWidth, m_FrameHeight;

        #endregion

        // Constructor
        public FrameGrabber(USBCameraVideo parent)
        {
            this.m_Parent = parent;
        }

        // Width property of the frame
        public int Width
        {
            get { return m_FrameWidth; }
            set { m_FrameWidth = value; }

        }
        // Height property of the Frame
        public int Height
        {
            get { return m_FrameHeight; }
            set { m_FrameHeight = value; }
        }

        /// <summary>
        /// Callback method that receives a pointer to the sample buffer
        /// </summary>
        /// <param name="SampleTime"></param>
        /// <param name="buffer"></param>
        /// <param name="bufferLength"></param>
        /// <returns></returns>
        public int BufferCB(double SampleTime, IntPtr buffer, int bufferLength)
        {
            try
            {
                //Create a new image variable
                System.Drawing.Bitmap frame = new Bitmap(m_FrameWidth, m_FrameHeight, PixelFormat.Format24bppRgb);

                //Lock the bits of the bitmap whilst we alter it
                BitmapData frameData = frame.LockBits(new Rectangle(0, 0, m_FrameWidth, m_FrameHeight),
                    ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

                //Copy the Image data
                int initalFrameWidth = frameData.Stride;
                int copiedFrameWidth = frameData.Stride;

                int initialFrame = buffer.ToInt32();
                int copiedFrame = frameData.Scan0.ToInt32() + copiedFrameWidth * (m_FrameHeight - 1);

                //Copy the frame using the height of the frame
                for (int y = 0; y < m_FrameHeight; y++)
                {
                    memcpy(copiedFrame, initialFrame, initalFrameWidth);
                    copiedFrame -= copiedFrameWidth;
                    initialFrame += initalFrameWidth;
                }

                //Unlock the bits of the bitmap
                frame.UnlockBits(frameData);

                //Pass the bitmap image to the parent
                m_Parent.newFrame(frame);
            }
            catch (Exception)
            {
                MessageBox.Show("Failed to grab frames", "Failed to grab frames", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return 0;
        }

        /// <summary>
        /// Fufill the requirements of the ISampleGrabber interface, although the ISampleGrabberCB interface is now
        /// technically a 'fat interface' this method must be implemented.
        /// </summary>
        /// <param name="SampleTime"></param>
        /// <param name="pSample"></param>
        /// <returns></returns>
        public int SampleCB(double SampleTime, IMediaSample pSample)
        {
            return 0;
        }
    }
}
