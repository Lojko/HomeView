using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HomeView.Views
{
    interface IKinectView
    {
        void btnMouseEnter(object sender, EventArgs e);
        void btnMouseExit(object sender, EventArgs e);
        void animateCursor();
    }
}
