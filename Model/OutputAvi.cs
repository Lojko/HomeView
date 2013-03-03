/* Developed using the AVIManager Class taken from
 * http://www.codeproject.com/Articles/7388/A-Simple-C-Wrapper-for-the-AviFile-Library 
 * */
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.DirectX.DirectSound;

namespace HomeView.Model
{
	public class OutputAvi : ICameraOutput
    {
        #region Declerations

        private Avi.BITMAPINFOHEADER m_BitmapInfo;
        private Int16 m_CountBitsPerPixel;
        private FileStream m_WaveFile;
        private BinaryWriter m_Writer;
        private WaveFormat m_WaveFormat;
        private string m_FileName;
        private int m_SampleCount;
        private int m_FrameSize;
        private int m_Width;
        private int m_Height;
        private double m_FrameRate;

        protected IntPtr m_AviStream;
        protected int m_AviFile;
        protected int m_FirstFrame = 0;
        protected int m_CountFrames = 0;
        protected int m_WavFile = 0;

        #endregion

        #region Properties

        /// <summary>
        /// Pointer to the unmanaged AVI file
        /// </summary>
        internal int FilePointer
        {
            get { return m_AviFile; }
        }

        /// <summary>
        /// Pointer to the unmanaged AVI Stream
        /// </summary>
        internal IntPtr StreamPointer
        {
            get { return m_AviStream; }
        }

        /// <summary>
        /// Size of an imge in bytes, stride * height
        /// </summary>
        public int FrameSize 
        {
            get { return m_FrameSize; }
        }

        /// <summary>
        /// Frame rate of the AVI Video
        /// </summary>
        public double FrameRate 
        {
            get{ return m_FrameRate; }
		}

        /// <summary>
        /// Width of the AVI Frame
        /// </summary>
		public int Width
        {
			get{ return m_Width; }
		}

        /// <summary>
        /// Height of the AVI Frame
        /// </summary>
		public int Height
        {
			get{ return m_Height; }
		}

        /// <summary>
        /// Number of bits per pixel of a frame
        /// </summary>
        public Int16 CountBitsPerPixel 
        {
            get{ return m_CountBitsPerPixel; }
		}

        /// <summary>
        /// Count of frames in the stream
        /// </summary>
		public int CountFrames
        {
			get{ return m_CountFrames; }
		}

		/// <summary>initial frame index
		public int FirstFrame
		{
			get { return m_FirstFrame; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// OutputAvi construct takes an incident that has been triggered and uses its properties to build and output
        /// path to create the file at. It also checks that the folder exists already, creating it if it does not.
        /// </summary>
        /// <param name="incident">Incident object</param>
        public OutputAvi(Incident incident)
        {
            if (!File.Exists(Properties.Settings.Default.OutputLocation + 
                @"\#" + incident.EventTime.ToString("dd,MM,yyyy,HH,mm,ss") + "#" + incident.TriggeringCameraName + 
                "#" + incident.NumberOfCameras + "#" + incident.IncidentTrigger))
            {
                System.IO.Directory.CreateDirectory(Properties.Settings.Default.OutputLocation +
                @"\#" + incident.EventTime.ToString("dd,MM,yyyy,HH,mm,ss") + "#" + incident.TriggeringCameraName +
                "#" + incident.NumberOfCameras + "#" + incident.IncidentTrigger);
            }

            m_FileName = Properties.Settings.Default.OutputLocation + @"\#" +
                incident.EventTime.ToString("dd,MM,yyyy,HH,mm,ss") + "#" + incident.TriggeringCameraName +
                "#" + incident.NumberOfCameras + "#" + incident.IncidentTrigger + @"\" + incident.CameraName;
        }

        /// <summary>
        /// The setVideoSettings function uses the calculated framerate of the video and the first frame received when
        /// recording to set up the output AVI file.
        /// </summary>
        /// <param name="frameRate"></param>
        /// <param name="firstFrameData"></param>
        public void setVideoSettings(double frameRate, byte[] firstFrameData)
        {
            //Convert the frame into a memorystream
            MemoryStream image = new MemoryStream(firstFrameData);
            
            //Convert the byte[] into a tangible bitmap frame
            Bitmap copy = (Bitmap)Bitmap.FromStream(image);
            Bitmap bitmapImage = new Bitmap(copy.Size.Width, copy.Size.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            //Initialize the AVIFile creation process
            Initialize(frameRate, bitmapImage);

            m_BitmapInfo = new Avi.BITMAPINFOHEADER();
            m_BitmapInfo.biSize = Marshal.SizeOf(m_BitmapInfo);
            m_BitmapInfo.biWidth = m_Width;
            m_BitmapInfo.biHeight = m_Height;
            m_BitmapInfo.biPlanes = 1;
            m_BitmapInfo.biBitCount = 24;
            m_BitmapInfo.biCompression = 1196444237;
            m_BitmapInfo.biSizeImage = 1843200;

            //Create the file
            CreateFiles(m_FileName + ".avi");
            //Initialize it, so it is ready to receive frames
            InitializeFile(m_FileName + ".avi");
        }

        /// <summary>
        /// The initializeAudio function takes the waveformat of the audio file and uses it to build an audio
        /// output file
        /// </summary>
        /// </summary>
        /// <param name="waveFormat">The waveformat of the audio stream</param>
        public void initializeAudio(WaveFormat waveFormat)
        {
            m_WaveFormat = waveFormat;
            BuildOutputFile();
        }

        /// <summary>
        /// The setAudioSettings function sets the samplecount of the audio stream
        /// </summary>
        /// <param name="waveFormat"></param>
        /// <param name="sampleCount"></param>
        public void setAudioSettings(int sampleCount)
        {
            m_SampleCount = sampleCount;
        }


        /// <summary>
        /// CreateRiff method which builds a appropriate .wav file so the microphone can be recorded and written to
        /// the wav file. Taken from the CaptureSound DirectX SDK DirectSound Sample.
        /// </summary>
        private void BuildOutputFile()
        {
            /**************************************************************************
                Here is where the file will be created. A
                wave file is a RIFF file, which has chunks
                of data that describe what the file contains.
                A wave RIFF file is put together like this:
			 
                The 12 byte RIFF chunk is constructed like this:
                Bytes 0 - 3 :	'R' 'I' 'F' 'F'
                Bytes 4 - 7 :	Length of file, minus the first 8 bytes of the RIFF description.
                                (4 bytes for "WAVE" + 24 bytes for format chunk length +
                                8 bytes for data chunk description + actual sample data size.)
                Bytes 8 - 11:	'W' 'A' 'V' 'E'
			
                The 24 byte FORMAT chunk is constructed like this:
                Bytes 0 - 3 :	'f' 'm' 't' ' '
                Bytes 4 - 7 :	The format chunk length. This is always 16.
                Bytes 8 - 9 :	File padding. Always 1.
                Bytes 10- 11:	Number of channels. Either 1 for mono,  or 2 for stereo.
                Bytes 12- 15:	Sample rate.
                Bytes 16- 19:	Number of bytes per second.
                Bytes 20- 21:	Bytes per sample. 1 for 8 bit mono, 2 for 8 bit stereo or
                                16 bit mono, 4 for 16 bit stereo.
                Bytes 22- 23:	Number of bits per sample.
			
                The DATA chunk is constructed like this:
                Bytes 0 - 3 :	'd' 'a' 't' 'a'
                Bytes 4 - 7 :	Length of data, in bytes.
                Bytes 8 -...:	Actual sample data.
            ***************************************************************************/

            // Set up file with RIFF chunk info.
            char[] chunkRiff = { 'R', 'I', 'F', 'F' };
            char[] chunkType = { 'W', 'A', 'V', 'E' };
            char[] chunkFmt = { 'f', 'm', 't', ' ' };
            char[] chunkData = { 'd', 'a', 't', 'a' };

            // File padding
            short padding = 1;
            // Format chunk length.
            int formatChunkLength = 0x10;
            // File length, minus first 8 bytes of RIFF description.
            int nLength = 0;
            // Bytes per sample.
            short bytesPerSample = 4;

            // Open up the output file for writing.
            m_WaveFile = new FileStream(m_FileName + ".wav", FileMode.Create);
            m_Writer = new BinaryWriter(m_WaveFile);

            // Fill in the riff info for the wave file.
            m_Writer.Write(chunkRiff);
            m_Writer.Write(nLength);
            m_Writer.Write(chunkType);

            // Fill in the format info for the wave file.
            m_Writer.Write(chunkFmt);
            m_Writer.Write(formatChunkLength);
            m_Writer.Write(padding);
            m_Writer.Write(m_WaveFormat.Channels);
            m_Writer.Write(m_WaveFormat.SamplesPerSecond);
            m_Writer.Write(m_WaveFormat.AverageBytesPerSecond);
            m_Writer.Write(bytesPerSample);
            m_Writer.Write(m_WaveFormat.BitsPerSample);

            // Now fill in the data chunk.
            m_Writer.Write(chunkData);
            m_Writer.Write(0);
        }

        /// <summary>
        /// The createAudio function finishes off the creation of the output wav file, writing the last
        /// bits of data to the file and closing the resources.
        /// </summary>
        private void createAudio()
        {
            // Seek to the length descriptor of the RIFF file.
            m_Writer.Seek(4, SeekOrigin.Begin);
            // Write the file length, minus first 8 bytes of RIFF description.
            m_Writer.Write((int)(m_SampleCount + 36));
            // Seek to the data length descriptor of the RIFF file.
            m_Writer.Seek(40, SeekOrigin.Begin);
            // Write the length of the sample data in bytes.
            m_Writer.Write(m_SampleCount);

            //Close the writer and set dispose of the objects appropriately.
            m_Writer.Close();
            //Release resources
            m_Writer = null;
            m_WaveFile = null;
        }

        /// <summary>Initialize a new VideoStream</summary>
        /// <remarks>Used only by constructors</remarks>
        /// <param name="frameRate">Frames per second</param>
        /// <param name="firstFrame">Image to write into the stream as the first frame</param>
        private void Initialize(double frameRate, Bitmap firstFrameBitmap) 
        {
            this.m_FrameRate = frameRate - 1;
			this.m_FirstFrame = 0;

			BitmapData frameData = firstFrameBitmap.LockBits(new Rectangle(
				0, 0, firstFrameBitmap.Width, firstFrameBitmap.Height),
				ImageLockMode.ReadOnly, firstFrameBitmap.PixelFormat);

            this.m_FrameSize = frameData.Stride * frameData.Height;
			this.m_Width = firstFrameBitmap.Width;
			this.m_Height = firstFrameBitmap.Height;
			this.m_CountBitsPerPixel = ConvertPixelFormatToBitCount(firstFrameBitmap.PixelFormat);

			firstFrameBitmap.UnlockBits(frameData);
        }

        /// <summary>Initalize the actual AVI file</summary>
        public void InitializeFile(string fileName)
        {
            //Call the aviInit dll function
            Avi.AVIFileInit();
            int result;

            //Create an empty file	
            result = Avi.AVIFileOpen(
                    ref this.m_AviFile, fileName,
                    Avi.OF_WRITE | Avi.OF_CREATE, 0);

            if (result != 0)
            {
                MessageBox.Show(result.ToString());
                throw new Exception("Exception in AVIFileOpen: " + result.ToString());
            }

            //Create the videostream
            CreateStream();
        }

        /// <summary>
        /// The CreateFiles Method creates the folders and files required by the AVIOutput class
        /// in the event that they do not already exist.
        /// </summary>
        /// <param name="fileName"></param>
        public void CreateFiles(string fileName)
        {
            string[] directoryName = fileName.Split('\\');
            string directory = "";
            for (int i = 0; i < directoryName.Length - 1; i++)
            {
                if (i == directoryName.Length - 2)
                {
                    directory += directoryName[i];
                }
                else
                {
                    directory += directoryName[i] + "\\";
                }
            }
            if (!File.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            File.Open(fileName, FileMode.Create).Close();
        }

        /// <summary>Get the count of bits per pixel from a PixelFormat value</summary>
		/// <param name="format">One of the PixelFormat members beginning with "Format..." - all others are not supported</param>
		/// <returns>bit count</returns>
		private Int16 ConvertPixelFormatToBitCount(PixelFormat format)
        {
			String formatName = format.ToString();
			if(formatName.Substring(0, 6) != "Format"){
				throw new Exception("Unknown pixel format: "+formatName);
			}

			formatName = formatName.Substring(6, 2);
			Int16 bitCount = 0;
			if( Char.IsNumber(formatName[1]) ){	//16, 32, 48
				bitCount = Int16.Parse(formatName);
			}else{								//4, 8
				bitCount = Int16.Parse(formatName[0].ToString());
			}

			return bitCount;
		}

        /// <summary>
        /// Calculate the scale and rate of the AVI
        /// </summary>
        /// <param name="frameRate">Frame rate of the AVI File</param>
        /// <param name="scale"></param>
        private void AviFrameRateAndScale(ref double frameRate, ref int scale)
        {
            while (frameRate != (long)frameRate)
            {
                frameRate = frameRate * 10;
                scale *= 10;
            }
        }

        /// <summary>
        /// Create a new video stream without any formatting
        /// </summary>
        private IntPtr CreateStreamWithoutFormat() 
        {
            int scale = 1;
            AviFrameRateAndScale(ref m_FrameRate, ref scale);

            Avi.AVISTREAMINFO aviHeader = new Avi.AVISTREAMINFO();
            aviHeader.fccType = Avi.mmioStringToFOURCC("vids", 0);
            aviHeader.fccHandler = Avi.mmioStringToFOURCC("MJPG", 0);
            aviHeader.dwFlags = 0;
            aviHeader.dwCaps = 0;
            aviHeader.wPriority = 0;
            aviHeader.wLanguage = 0;
            aviHeader.dwScale = (int)1000;
            aviHeader.dwRate = (int)m_FrameRate * 1000;
            aviHeader.dwStart = 0;
            aviHeader.dwLength = 0;
            aviHeader.dwInitialFrames = 0;
            aviHeader.dwQuality = 5000;
            aviHeader.dwSampleSize = 1;
            aviHeader.rcFrame.top = 0;
            aviHeader.rcFrame.left = 0;
            aviHeader.rcFrame.bottom = (uint)m_Width;
            aviHeader.rcFrame.right = (uint)m_Height;
            aviHeader.dwEditCount = 0;
            aviHeader.dwFormatChangeCount = 0;
            aviHeader.szName = new UInt16[64];

            //Use the physical file, the video stream and the header to create an accessible avi stream
            IntPtr aviStream;
            int result = Avi.AVIFileCreateStream(m_AviFile, out aviStream, ref aviHeader);

            if (result != 0) {
                throw new Exception("Exception in AVIFileCreateStream: " + result.ToString());
            }

            return aviStream;
        }

        /// <summary>
        /// Create a new stream and set the format of the stream
        /// </summary>
        private void CreateStream() 
        {
            IntPtr aviStream = CreateStreamWithoutFormat();
            //Set the format of the stream
            SetFormat(aviStream);
        }

        /// <summary>
        /// Add an audio sample to the output wav file, the function should be called with a audio sample
        /// byte array
        /// </summary>
        /// <param name="audio">The audio sample in byte[] format</param>
        public void addAudioSample(byte[] audio)
        {
            //Write the audio to the output file
            m_Writer.Write(audio);
        }
 
        /// <summary>
        /// Add a frame to the stream. This works only with uncompressed streams,
        /// and compressed streams that have not been saved yet.
        /// Use DecompressToNewFile to edit saved compressed streams.
        /// </summary>
        /// <param name="bmp"></param>
		public void addFrame(byte[] frame)
        {
            //Prevent the Garbage handler from disposing the frame
            GCHandle outputImageHandle = GCHandle.Alloc(frame, GCHandleType.Pinned);

            int result = Avi.AVIStreamWrite(m_AviStream,
                m_CountFrames, 1,
                outputImageHandle.AddrOfPinnedObject(),
                Convert.ToInt32(frame.Length),
                Avi.AVIIF_KEYFRAME, 0, 0);

			if (result!= 0) 
				throw new Exception("Exception in VideoStreamWrite: " + result.ToString());
			

            outputImageHandle.Free();
			m_CountFrames++;        
		}

        /// <summary>
        /// Apply a format to a new stream. The format must be set before the first frame can be written and it cannot be changed later.
        /// </summary>
        /// <param name="aviStream">The aviStream IntPtr</param>
        private void SetFormat(IntPtr aviStream)
        {
            //Set the format of the AVI using the avi header
            int result = Avi.AVIStreamSetFormat(aviStream, 0, ref m_BitmapInfo, Convert.ToInt32(64));

            if (result != 0) 
                throw new Exception("Error in VideoStreamSetFormat: " + result.ToString());
            m_AviStream = aviStream;
        }

        /// <summary>
        /// Add an existing wave audio stream to the fileThe index of the video frame at which the sound is going to start.
        /// '0' inserts the sound at the beginning of the video.
        /// </summary>
        /// <param name="audioStream">The audio stream to add</param>
        /// <param name="beginningIndex">Index of the file to add the audio at</param>
        public void AddAudioStream(AudioStream audioStream, int beginningIndex)
        {
            Avi.AVISTREAMINFO streamInfo = new Avi.AVISTREAMINFO();
            Avi.PCMWAVEFORMAT streamFormat = new Avi.PCMWAVEFORMAT();
            int streamLength = 0;

            //Reference to the audio data
            IntPtr audioData = audioStream.GetStreamData(ref streamInfo, ref streamFormat, ref streamLength);

            if (beginningIndex > 0)
            {
                double framesPerSecond = m_FrameRate;
                double samplesPerSecond = audioStream.CountSamplesPerSecond;
                double startAtSecond = beginningIndex / framesPerSecond;
                int startAtSample = (int)(samplesPerSecond * startAtSecond);
            }

            //Create the AviStream with audio
            IntPtr aviStream;
            int result = Avi.AVIFileCreateStream(m_AviFile, out aviStream, ref streamInfo);
            if (result != 0)
            {
                throw new Exception("Exception in AVIFileCreateStream: " + result.ToString());
            }

            //Reset the stream format
            result = Avi.AVIStreamSetFormat(aviStream, 0, ref streamFormat, Marshal.SizeOf(streamFormat));
            if (result != 0)
            {
                throw new Exception("Exception in AVIStreamSetFormat: " + result.ToString());
            }

            //Write the audio data to the stream
            result = Avi.AVIStreamWrite(aviStream, 0, streamLength, audioData, streamLength, Avi.AVIIF_KEYFRAME, 0, 0);
            if (result != 0)
            {
                throw new Exception("Exception in AVIStreamWrite: " + result.ToString());
            }

            //Release the stream
            result = Avi.AVIStreamRelease(aviStream);
            if (result != 0)
            {
                throw new Exception("Exception in AVIStreamRelease: " + result.ToString());
            }

            //Release resources
            Avi.AVIFileRelease(m_AviFile);
            Avi.AVIFileRelease(m_WavFile);
            Avi.AVIFileExit();
            Marshal.FreeHGlobal(audioData);
            audioStream.Close();
        }

        /// <summary>Add a wave audio stream from another file to this file</summary>
        /// <param name="wavFileName">Name of the wave file to add</param>
        public void AddAudioStream(string wavFileName)
        {
            //Create a new audiostream from a phyisically existing audio file
            AudioStream newStream = GetWaveStream(wavFileName);
            AddAudioStream(newStream, 0);
        }

        /// <summary>Get the first wave audio stream</summary>
        /// <returns>AudioStream object for the stream</returns>
        public AudioStream GetWaveStream(string wavFileName)
        {
            //Reference to the audio stream
            IntPtr aviStream;

            //Open the output wav file
            int result = Avi.AVIFileOpen(
                ref m_WavFile, wavFileName,
                Avi.OF_READWRITE, 0);

            //Obtain the wav file as an Audio stream, in a format that can be used to 
            //add to an AVI file
            result = Avi.AVIFileGetStream(
                m_WavFile,
                out aviStream,
                Avi.streamtypeAUDIO, 0);

            if (result != 0)
            {
                throw new Exception("Exception in AVIFileGetStream: " + result.ToString());
            }

            AudioStream stream = new AudioStream(m_WavFile, aviStream);
            return stream;
        }

        /// <summary>Close the AVI output stream, add sound to the AVI file, release the resources and delete the 
        /// existing wav file.
        /// </summary>
        public void closeOutput()
        {
            //Release the avi stream
            Avi.AVIStreamRelease(m_AviStream);
            //Check if a audio has been added and release its resoruces
            if (m_Writer != null)
            {
                //Create the required audio
                createAudio();
                AddAudioStream(m_FileName + ".wav");
                File.Delete(m_FileName + ".wav");
            }
            else
            {
                Avi.AVIFileRelease(m_AviFile);
                Avi.AVIFileExit();
            }
        }

        #endregion
    }
}
