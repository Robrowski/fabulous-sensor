using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core.AnimationMetrics;
using Windows.UI.Xaml;
using WindowsPreview.Kinect;

namespace FabulousBrowserApp
{
    public sealed partial class MainPage
    {
        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper BodyCoordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader BodyReader = null;


        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        private double lastGesture = 0;


        private HandsTracker tableFlipGestureTracker = new HandsTracker(30,(float)1.1,(float)0.4);
        private BoneAngleTracker rightSwipeGestureTracker = new BoneAngleTracker(30, (float) 1, -90);
        private BoneAngleTracker leftSwipeGestureTracker  = new BoneAngleTracker(30, (float)1, 90);



        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
//            Debug.WriteLine("BODY HAS ARRIVED");
            BodyFrameReference frameReference = e.FrameReference;

            try
            {
                BodyFrame frame = frameReference.AcquireFrame();

                if (frame != null)
                {

                    // BodyFrame is IDisposable
                    using (frame)
                    {

                        if (this.bodies == null)
                        {
                            this.bodies = new Body[frame.BodyCount];
                        }

                        // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                        // As long as those body objects are not disposed and not set to null in the array,
                        // those body objects will be re-used.
                        frame.GetAndRefreshBodyData(this.bodies);

                        foreach (Body body in this.bodies)
                        {
                            if (body.IsTracked)
                            {
                                IReadOnlyDictionary<JointType, Joint> joints = body.Joints;


                                if (rightSwipeGestureTracker.Record_elbow_and_wrist_positions(joints[JointType.HandRight], joints[JointType.ElbowRight]))
                                {
                                    Swipe("right");
                                }

                                if (tableFlipGestureTracker.Record_hand_positions(joints[JointType.HandLeft], joints[JointType.HandRight]))
                                {
                                    //Debug.WriteLine("flipped");
                                    Flip();
                                }

                                if (leftSwipeGestureTracker.Record_elbow_and_wrist_positions(
                                    joints[JointType.HandLeft], joints[JointType.ElbowLeft]))
                                {
                                    Swipe("left");
                                }

                            }

                        }
                        
                    }
                }
            }
            catch (Exception)
            {
                // ignore if the frame is no longer available
            }
        }

    }
}
