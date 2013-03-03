/* Taken from the AVIFile C# wrapper project from 
 * http://www.codeproject.com/Articles/7388/A-Simple-C-Wrapper-for-the-AviFile-Library 
 * */
using System;

namespace HomeView.Model
{
    /// <summary>
    /// Abstract AVIStream Class, the Avistream class can be used to generate a stream of data for
    /// any type of AVI output (e.g. Audio or Video)
    /// </summary>
	public abstract class AviStream
    {
        #region Delcarations

        protected int aviFile;
		protected IntPtr aviStream;
		protected IntPtr compressedStream;
		protected bool writeCompressed;

        #endregion

        #region Properties
        /// <summary>Pointer to the unmanaged AVI file</summary>
        internal int FilePointer {
            get { return aviFile; }
        }

        /// <summary>Pointer to the unmanaged AVI Stream</summary>
        internal virtual IntPtr StreamPointer {
            get { return aviStream; }
        }

        /// <summary>Flag: The stream is compressed/uncompressed</summary>
        internal bool WriteCompressed {
            get { return writeCompressed; }
        }
        #endregion

        /// <summary>Close the stream</summary>
        public virtual void Close(){
			if(writeCompressed){
				Avi.AVIStreamRelease(compressedStream);
			}
			Avi.AVIStreamRelease(StreamPointer);
		}

        /// <summary>Export the stream into a new file</summary>
        /// <param name="fileName"></param>
		public abstract void ExportStream(String fileName);
		
	}
}
