using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsPreview.Kinect;

namespace FabulousBrowserApp
{
    public abstract class GestureTracker
    {
        protected readonly int _maxHistorySize; // 30fps*4seconds
        protected readonly int _sampleRate;


        protected GestureTracker(int sampleRate, float gestureTime)
        {
            _sampleRate = sampleRate;
            _maxHistorySize = Convert.ToInt32(Math.Floor(sampleRate * gestureTime));
        }



        public int SampleRate
        {
            get { return _sampleRate; }
        }

    }
    
    
    
    public class HandsTracker : GestureTracker
    {
        private readonly Queue<float> _verticalHandPositionHistory = new Queue<float>();
        public readonly float MinimumUpwardsDisplacement;

        public HandsTracker(int sampleRate, float gestureTime, float minimumUpwardsDisplacement) : base(sampleRate, gestureTime)
        {
            MinimumUpwardsDisplacement = minimumUpwardsDisplacement;
        }

        // Returns true if both hands performed a table flip
        public bool Record_hand_positions(Joint left, Joint right)
        {
            // Check to make sure the hands are close enough vertically (X and Z ignored)
            // If they are too far apart, give up and reset to history
            if (Math.Abs(left.Position.Y - right.Position.Y) > .08)
            {
                _verticalHandPositionHistory.Clear();
                return false;
            }

            // Record the current hand position
            float handPosition = (left.Position.Y + right.Position.Y)/2;
            _verticalHandPositionHistory.Enqueue(handPosition);
            // Maintain queue size
            if (_verticalHandPositionHistory.Count > _maxHistorySize)
            {
                _verticalHandPositionHistory.Dequeue();
            }

            // See if the desired displacement was achieved
            foreach (float f in _verticalHandPositionHistory)
            {
                // If the displacement happened in recorded history, the gesture was achieved
                /// TODO: use ratio of head to torso to centroid of body as reference for table flip displacement
                if (handPosition - f > MinimumUpwardsDisplacement)
                {
                    _verticalHandPositionHistory.Clear();
                    return true;
                }
            }

            return false;
        }
    }



    public class BoneAngleTracker : GestureTracker
    {
        // Only tracks angles in the XY plane
        private readonly Queue<double> _angleHistory = new Queue<double>(); // history size = hz * time = max num samples to capture a gesture
        public readonly float _minimumAngularDisplacement;


        public BoneAngleTracker(int sampleRate, float gestureTime, float minimumAngularDisplacement)
            : base(sampleRate, gestureTime)
        {
            _minimumAngularDisplacement = minimumAngularDisplacement;
        }

        public float MinimumAngularDisplacement
        {
            get { return _minimumAngularDisplacement; }
        }


        // Return true if gesture happened. Queue is cleared
        public bool Record_elbow_and_wrist_positions(Joint jointA, Joint jointB)
        {
            // Calculate and store the bone's angular position in the XY plane
            float dX = jointA.Position.X - jointB.Position.X;
            float dY = jointA.Position.Y - jointB.Position.Y;
            double angle = Math.Atan2(dY,dX);           
            _angleHistory.Enqueue(  angle*180/Math.PI); // store angle as degrees because
           
            // Maintain queue size
            if (_angleHistory.Count > _maxHistorySize)
            {
                _angleHistory.Dequeue();
            }

            double currentPosition = _angleHistory.Peek();
            foreach (float f in _angleHistory)
            {
                double displacement = currentPosition - f;

                // Make sure the displacement is between +/- PI
                if (displacement > Math.PI)
                {
                    displacement -= Math.PI;
                }
                else if (displacement < -Math.PI)
                {
                    displacement += Math.PI;
                }

                // If magnitude of displacement > desired AND the displacements are in the same direction, we have a gesture
                if (Math.Abs(displacement) >= Math.Abs(_minimumAngularDisplacement) && displacement*_minimumAngularDisplacement > 0)
                {
                    _angleHistory.Clear();
                    return true;
                }

            }

            
            //// for finding when just the minimum desired displacement occured
            // Calculate sum of angles from whole queue
//            float max = float.MinValue;
//            float min = float.MaxValue;
//            foreach (float f in _angleHistory)
//            {
//                max = Math.Max(max, f);
//                min = Math.Min(min, f);
//            }
//            
//            // Check if enough displacement occured in the given maximum amount of time. 
//            if (Math.Abs(max - min) >= _minimumAngularDisplacement )
//            {
//                _angleHistory.Clear();
//                return true;
//            }
            
            return false;
        }
    }
}
