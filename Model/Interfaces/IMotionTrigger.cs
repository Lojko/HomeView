using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace HomeView.Model
{
    /// <summary>
    /// IMotionTrigger interface dictates all the functionality required for triggers
    /// (existing and future developments).
    /// </summary>
    interface IMotionTrigger
    {
        Bitmap detectMotion(Bitmap newFrame);
        void activateMotionTrigger();
        void setBackgroundFrame(Bitmap frame);
        bool getMotionTriggerActiveState();
        bool checkBackgroundFrame();

        event EventHandler triggered;
    }
}
