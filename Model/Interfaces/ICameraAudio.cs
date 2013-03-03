using Microsoft.DirectX.DirectSound;

namespace HomeView.Model
{
    /// <summary>
    /// ICameraAudio interface dictates all the functionality required for the
    /// streaming of audio devices (existing and future developments).
    /// </summary>
    interface ICameraAudio
    {
        //Set of methods which must be implemented by classes
        void Record();
        void stopRecord();
        void Stop();
        void Mute();
        void Unmute();
        bool isMuted();

        //The camera output variable which the audiostream is piped to
        ICameraOutput Output { set; }
    }
}
