namespace HomeView.Model
{
    /// <summary>
    /// ICameraVideo interface dictates all the functionality required for the
    /// streaming of video (existing and future developments).
    /// </summary>
    interface ICameraVideo
    {
        //Set of methods which must be implemented by classes
        void Record();
        void stopRecord();
        void Connect();
        void Disconnect();
        void activateMotionTrigger();

        //The camera output variable which the audiostream is piped to
        ICameraOutput Output { set; }
    }
}
