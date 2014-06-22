using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsPreview.Kinect;

namespace FabulousBrowserApp
{
    public class JointInfo
    {
        public float X;
        public float Y;
        public float Z;
        public float jointType;

        public JointInfo(Joint joint)
        {
            this.X = joint.Position.X;
            this.Y = joint.Position.Y;
            this.Z = joint.Position.Z;
        
        }
        public void UpdateJoint(Joint joint)
        {
            this.X = joint.Position.X;
            this.Y = joint.Position.Y;
            this.Z = joint.Position.Z;
        }
    }

    
}
