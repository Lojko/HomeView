using System;
using System.Runtime.InteropServices;
using System.Timers;
using DirectShowLib;

namespace HomeView.Model
{
    class IncidentReview
    {
        #region Declarations

        private IVideoWindow m_VideoWindow;
        private IBasicAudio m_VideoWindowAudio;
        private IMediaControl m_MediaControl;
        private IGraphBuilder m_GraphBuilder;
        private IMediaSeeking m_VideoSeek;
        private CameraControl m_ParentControl;

        private Timer m_RewindTimer;
        private Timer m_FastForwardTimer;

        public bool muted;

        #endregion

        /// <summary>
        /// Incident review class, used to gather all of the pre-recording avi file incidents and display them
        /// appropriately.
        /// </summary>
        /// <param name="videofile">The location of the existing video file</param>
        /// <param name="control">The control the video file will be bound to</param>
        public IncidentReview(string videofile, CameraControl control)
        {
            muted = true;
            control.CameraStatus = "Playback";
            //Define the parent control
            m_ParentControl = control;
            control.resize += new EventHandler(resized);

            //Create the Filter graph builder used to 'construct' a video stream
            m_GraphBuilder = (IGraphBuilder)new FilterGraph();

            //Have the graph builder construct its the appropriate graph automatically, rendering
            //the video file
            int errorOutput = this.m_GraphBuilder.RenderFile(videofile, null);

            //Ensure rendering has begun correctly
            DsError.ThrowExceptionForHR(errorOutput);

            //Initialize the media control used to encapsulate the video stream
            m_MediaControl = (IMediaControl)m_GraphBuilder;
            //Initialize the output window the video stream will be displayed on
            m_VideoWindow = m_GraphBuilder as IVideoWindow;

            //Allow the custom video control to become the parent control of the actual
            //video window, in addition, change the display properties of the output window so it can be
            //placed elegantly into the custom video control and reposition within the control
            m_VideoWindow.put_Owner(control.Handle);
            m_VideoWindow.put_WindowStyle(WindowStyle.ClipSiblings | WindowStyle.ClipChildren | WindowStyle.Child);
            m_VideoWindow.SetWindowPosition(control.VideoBox.Location.X, control.VideoBox.Location.Y, control.VideoBox.Width, control.VideoBox.Height);
            DsError.ThrowExceptionForHR(errorOutput);

            //Declare the variables required to modify the audio and the video
            m_VideoWindowAudio = (IBasicAudio)m_GraphBuilder;
            m_VideoSeek = (IMediaSeeking)m_GraphBuilder;

            //All videos are played, then paused to ensure the first frame is displayed
            errorOutput = m_MediaControl.Run();
            m_MediaControl.Pause();

            //Delcare the timers used to fast forward and rewind events
            m_FastForwardTimer = new Timer(25);
            m_RewindTimer = new Timer(25);

            //Attached the functions to be triggered when the timers tick
            m_FastForwardTimer.Elapsed += new ElapsedEventHandler(m_FastForwardTimer_Elapsed);
            m_RewindTimer.Elapsed += new ElapsedEventHandler(m_RewindTimer_Elapsed);
        }

        /// <summary>
        /// Function called when the 'rewind timer' ticks, it is called every 25ms to alter the position
        /// of the video to give the appearence of rewind functionality.
        /// </summary>
        /// <param name="sender">Control that triggered the event</param>
        /// <param name="e">Arguments passed related to the triggering</param>
        private void m_RewindTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            long totalTime = calculateTotalTime();
            //Duration in seconds (1 second unit = 1000000 for position
            long duration = (totalTime / 1000000);
            long rewind = ((totalTime / duration));
            long currentPosition = new long();
            m_VideoSeek.GetCurrentPosition(out currentPosition);
            if (currentPosition - rewind < 0)
            {
                rewind = 0;
                //If the position is out of bounds, set it to the beginning of the stream
                m_VideoSeek.SetPositions(rewind, AMSeekingSeekingFlags.AbsolutePositioning, null, AMSeekingSeekingFlags.NoPositioning);
            }
            else
            {
                //Continue to rewind through the video stream
                m_VideoSeek.SetPositions(currentPosition - rewind, AMSeekingSeekingFlags.AbsolutePositioning, null, AMSeekingSeekingFlags.NoPositioning);
            }
        }

        /// <summary>
        /// Function called when the 'fast forward timer' ticks, it is called every 25ms to alter the position
        /// of the video to give the appearence of fast forward functionality.
        /// </summary>
        /// <param name="sender">Control that triggered the event</param>
        /// <param name="e">Arguments passed related to the triggering</param>
        private void m_FastForwardTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            long totalTime = calculateTotalTime();
            //Duration in seconds
            long duration = (totalTime / 1000000);
            long fastFoward = ((totalTime / duration));
            long currentPosition = new long();
            m_VideoSeek.GetCurrentPosition(out currentPosition);
            if (currentPosition + fastFoward > totalTime)
            {
                //Typically 25 frames per second, this is for rate of fast foward/rewind
                m_VideoSeek.SetPositions(totalTime, AMSeekingSeekingFlags.AbsolutePositioning, null, AMSeekingSeekingFlags.NoPositioning);
            }
            else
            {
                m_VideoSeek.SetPositions(currentPosition + fastFoward, AMSeekingSeekingFlags.AbsolutePositioning, null, AMSeekingSeekingFlags.NoPositioning);
            }
        }

        /// <summary>
        /// Function for calculating the total time of a video
        /// </summary>
        /// <returns>Long representing the duration of time the video stream runs for</returns>
        private long calculateTotalTime()
        {
            //Pause the stream
            m_MediaControl.Pause();
            long totalTime = new long();
            //Attain the end of the video
            m_VideoSeek.GetStopPosition(out totalTime);
            return totalTime;
        }

        /// <summary>
        /// The resized function which is called when the resize event is triggered, the size of the
        /// parent control window determines the size and position of the DirectShow video window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void resized(object sender, EventArgs e)
        {
            m_VideoWindow.SetWindowPosition(m_ParentControl.VideoBox.Location.X, m_ParentControl.VideoBox.Location.Y, 
                m_ParentControl.VideoBox.Width, m_ParentControl.VideoBox.Height);
        }

        /// <summary>
        /// Mute function which mutes the video stream
        /// </summary>
        public void mute()
        {
            muted = true;
            m_VideoWindowAudio.put_Volume(-10000);
        }

        /// <summary>
        /// Unmute funciton which plays the audio of a video
        /// </summary>
        public void unmute()
        {
            muted = false;
            m_VideoWindowAudio.put_Volume(0);
        }

        /// <summary>
        /// Play function which begins the video stream
        /// </summary>
        public void play()
        {
            m_RewindTimer.Stop();
            m_FastForwardTimer.Stop();
            m_MediaControl.Run();
        }

        /// <summary>
        /// Pause function which pauses the stream whilst it's playing
        /// </summary>
        public void pause()
        {
            m_RewindTimer.Stop();
            m_FastForwardTimer.Stop();
            m_MediaControl.Pause();
        }

        /// <summary>
        /// Beginning function which changes the position of the video stream to the beginning of the video
        /// </summary>
        public void beginning()
        {
            m_RewindTimer.Stop();
            m_FastForwardTimer.Stop();
            DsLong beginning = new DsLong(0);
            //Set the position of the video stream
            m_VideoSeek.SetPositions(beginning,AMSeekingSeekingFlags.AbsolutePositioning, null, AMSeekingSeekingFlags.NoPositioning);
        }

        /// <summary>
        /// End function which changes the position of the video stream to the end of the video
        /// </summary>
        public void end()
        {
            m_RewindTimer.Stop();
            m_FastForwardTimer.Stop();
            long end = new long();
            m_VideoSeek.GetDuration(out end);
            //Set the position of the video stream
            m_VideoSeek.SetPositions(end, AMSeekingSeekingFlags.AbsolutePositioning, null, AMSeekingSeekingFlags.NoPositioning);
        }

        /// <summary>
        /// Fast forward function which actives the fast forward timer and stops the rewind timer (if it is already running)
        /// </summary>
        public void fastForward()
        {
            m_RewindTimer.Stop();
            m_FastForwardTimer.Start();
        }

        /// <summary>
        /// Rewind function which actives the rewind timer and stops the fast forward timer (if it is already running)
        /// </summary>
        public void rewind()
        {
            m_FastForwardTimer.Stop();
            m_RewindTimer.Start();
        }

        /// <summary>
        /// Eject function which defaults all media controls, releases DirectShow and stops all timers
        /// </summary>
        public void eject()
        {
            //Stop timers and video streaming
            m_RewindTimer.Stop();
            m_FastForwardTimer.Stop();
            m_MediaControl.Stop();
            m_ParentControl.resize -= new EventHandler(resized);
            m_ParentControl = null;
            m_VideoWindow.put_Visible(OABool.False);
            m_VideoWindow.put_Owner(IntPtr.Zero);
            //Release DirectShow
            m_RewindTimer = null;
            m_FastForwardTimer = null;
            m_VideoWindow = null;
            m_MediaControl = null;
            m_VideoWindowAudio = null;
            Marshal.ReleaseComObject(m_GraphBuilder); m_GraphBuilder = null;
            m_VideoSeek = null;
        }
    }
}
