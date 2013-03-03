using System;
using System.Threading;
using System.Windows.Forms;
using Microsoft.DirectX.DirectSound;

namespace HomeView.Model
{
    class USBCameraAudio : ICameraAudio
    {
        #region Declarations

        private Thread m_CaptureThread;
        private Guid m_MicrophoneIdentifier;
        private Control m_ParentControl;
        private bool m_StreamMicrophone = false;
        private bool m_RecordMicrophone = false;
        private bool m_Mute;
        private int m_SampleCount;
        private WaveFormat m_WaveFormat;

        private ICameraOutput m_Output;

        #endregion

        public ICameraOutput Output
        {
            set { m_Output = value; }
        }

        public WaveFormat WaveFormat
        {
            get { return m_WaveFormat; }
        }

        public int SampleCount
        {
            get { return m_SampleCount; }
        }


        /// <summary>
        /// Microphone constructor, requires a parental control and the GUID of a appropriate microphone
        /// </summary>
        /// <param name="parent">Parent Camera Control</param>
        /// <param name="microphone">Micropohne as a GUID</param>
        public USBCameraAudio(Control parent, Guid microphone)
        {
            m_WaveFormat = new WaveFormat
            {
                //Best Quality for minimal output file size
                SamplesPerSecond = 22050,
                BitsPerSample = 16,
                Channels = 2,
                FormatTag = WaveFormatTag.Pcm,
                AverageBytesPerSecond = 88200 
            };

            m_MicrophoneIdentifier = microphone;
            m_ParentControl = parent;
            m_Mute = false;

            if (!startAudioStream())
            {
                MessageBox.Show("No matching Sound Card was found");
            }
        }

        /// <summary>
        /// Stop the microphone stream
        /// </summary>
        public void Stop()
        {
            //Stop the streaming thread
            m_StreamMicrophone = false;
        }

        /// <summary>
        /// Record the audio of the microphone
        /// </summary>
        public void Record()
        {
            m_Output.initializeAudio(m_WaveFormat);
            m_SampleCount = 0;
            m_RecordMicrophone = true;
        }

        /// <summary>
        /// Mutes the audio stream
        /// </summary>
        public void Mute()
        {
            m_Mute = true;
        }
         
        /// <summary>
        /// Unmutes the audio stream
        /// </summary>
        public void Unmute()    
        {
            m_Mute = false;  
        }

        //Returns whether the camera audio is muted or not
        public bool isMuted()
        {
            return m_Mute;
        }

        /// <summary>
        /// Stops the video recording
        /// </summary>
        public void stopRecord()
        {
            m_RecordMicrophone = false;
            //Use the total amount of samples taken to set audio settings
            m_Output.setAudioSettings(m_SampleCount);
        }

        /// <summary>
        /// Begin streaming of the audio device
        /// </summary>
        /// <returns></returns>
        private bool startAudioStream()
        {
            int bufferSize = 8;
            int offset = 0;
            int devBuffer = 0;

            // BlockAlign = BytesPerSampleAllChannels, AverageBytesPerSecond = BytesPerSecondAllChannels
            m_WaveFormat.BlockAlign = (short)((m_WaveFormat.BitsPerSample / bufferSize) * m_WaveFormat.Channels);
            m_WaveFormat.AverageBytesPerSecond = m_WaveFormat.BlockAlign * m_WaveFormat.SamplesPerSecond;

            // Sets the NotifySize to enough bytes for 1/16th of a second for all channels
            int notifySize = Math.Max(4096, m_WaveFormat.AverageBytesPerSecond / (bufferSize * 2));
            notifySize -= notifySize % m_WaveFormat.BlockAlign;

            /* Using a circular capture array, the allocation should be twice as big at the output
             * to allow continous buffering without overwriting the output*/
            int captureSize = bufferSize * notifySize * 2;
            int outputrSize = bufferSize * notifySize;

            // Create CaptureBufferDescriptor and Capture object
            Capture microphoneCapture = new Capture(m_MicrophoneIdentifier);
            {
                // Create the description and create a CaptureBuffer accordingly
                CaptureBufferDescription captureDescription = new CaptureBufferDescription
                {
                    Format = m_WaveFormat,
                    BufferBytes = captureSize
                };

                CaptureBuffer captureAudio = new CaptureBuffer(captureDescription, microphoneCapture);

                /* Device is the output of the streamed sound, by default this is speakers, however this could
                be changed */
                Device dev = new Device();

                /* As DirectSound can use any window handle,  SetCooperativeLevel() of the parent window */
                dev.SetCooperativeLevel(m_ParentControl, CooperativeLevel.Priority);

                /* Setting the globalfocus to true prevents the sound from stopping when the window is minimized*/
                BufferDescription deviceDescription = new BufferDescription
                {
                    BufferBytes = outputrSize,
                    Format = m_WaveFormat,
                    DeferLocation = true,
                    GlobalFocus = true
                };

                // Subscribe the resetevent to fire when the buffer has been filled
                AutoResetEvent resetEvent = new AutoResetEvent(false);
                Notify notify = new Notify(captureAudio);

                // Create two output buffer to allow simultaneous processing thread access and playback.
                SecondaryBuffer[] streamBuffers = new SecondaryBuffer[2];
                streamBuffers[0] = new SecondaryBuffer(deviceDescription, dev);
                streamBuffers[1] = new SecondaryBuffer(deviceDescription, dev);

                /* Permit two notifications when the buffer is half full and completely full for both
                 * buffers */
                BufferPositionNotify firstBufferNotify = new BufferPositionNotify();
                firstBufferNotify.Offset = captureAudio.Caps.BufferBytes / 2 - 1;
                firstBufferNotify.EventNotifyHandle = resetEvent.SafeWaitHandle.DangerousGetHandle();

                BufferPositionNotify secondBufferNotify = new BufferPositionNotify();
                secondBufferNotify.Offset = captureAudio.Caps.BufferBytes - 1;
                secondBufferNotify.EventNotifyHandle = resetEvent.SafeWaitHandle.DangerousGetHandle();

                notify.SetNotificationPositions(new BufferPositionNotify[] { firstBufferNotify, secondBufferNotify });
                
                //Begin streaming the sound
                m_StreamMicrophone = true;

                m_CaptureThread = new Thread((ThreadStart)delegate
                {
                    // Start capturing the microphone stream
                    captureAudio.Start(true);

                    while (m_StreamMicrophone)
                    {
                        // Event prevents the continuation of the thread until the auto reset event has been triggered
                        resetEvent.WaitOne();

                        Array playbackArray = captureAudio.Read(offset, typeof(byte), LockFlag.None, outputrSize);
                        //Cast the array as a byte[] array for use when creating the output file
                        byte[] outputSoundArray = (byte[])playbackArray;
                        streamBuffers[devBuffer].Write(0, playbackArray, LockFlag.EntireBuffer);
                        offset = (offset + outputrSize) % captureSize;

                        // Playback the live sound
                        if (m_Mute == false)
                        {
                            streamBuffers[devBuffer].SetCurrentPosition(0);
                            streamBuffers[devBuffer].Play(0, BufferPlayFlags.Default);
                        }

                        // Change the buffer used to store the stream
                        devBuffer = 1 - devBuffer;

                        // Output to the file
                        if (m_RecordMicrophone == true)
                        {
                            m_Output.addAudioSample(outputSoundArray);
                            //Increment the samplecount
                            m_SampleCount += outputSoundArray.Length;
                        }
                    }

                    captureAudio.Stop();
                });

                m_CaptureThread.Start();

                return true;
            }
        }
    }
}
