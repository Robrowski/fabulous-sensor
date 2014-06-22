using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsPreview.Kinect;

namespace FabulousBrowserApp
{
    class JointInfo
    {
        private float X;
        private float Y;
        private float Z;
        private float jointType;

        public void UpdateJoint(Joint joint)
        {
            this.X = joint.Position.X;
            this.Y = joint.Position.Y;
            this.Z = joint.Position.Z;
        }
    }

    
}
