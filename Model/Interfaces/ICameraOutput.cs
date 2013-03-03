using Microsoft.DirectX.DirectSound;

namespace HomeView.Model
{
    /// <summary>
    /// ICameraOutput interface dictates all the functionality required for the
    ///  video output creation (existing and future developments).
    /// </summary>
    interface ICameraOutput
    {
        //Set of methods which must be implemented by classes
        void closeOutput();
        void addFrame(byte[] frame);
        void addAudioSample(byte[] audio);
        void initializeAudio(WaveFormat waveFormat);
        void setAudioSettings(int sampleCount);
        void setVideoSettings(double frameRate, byte[] firstFrameData);
    }
}
