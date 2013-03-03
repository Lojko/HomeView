using System.Windows.Forms;

namespace HomeView.Presenter
{
    /// <summary>
    /// Kinect inteface dicatating what a presenter needs to implement to utilize kinect
    /// </summary>
    interface IKinectPresenter
    {
        void kinect(Button kinectUp, Button kinectDown, out bool kinectState);
        void kinectDown();
        void kinectUp();
    }
}
