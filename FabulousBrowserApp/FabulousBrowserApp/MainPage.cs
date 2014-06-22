﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
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
        /// The time of the first frame received
        /// </summary>
        private TimeSpan startTime;

        /// <summary>
        /// Next time to update FPS/frame time status
        /// </summary>
        private DateTime nextStatusUpdate = DateTime.MinValue;

        /// <summary>
        /// Number of frames since last FPS/frame time status
        /// </summary>
        private uint framesSinceUpdate = 0;

        /// <summary>
        /// Timer for FPS calculation
        /// </summary>
        private Stopwatch stopwatch = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        Dictionary<String,JointInfo> jointInfos = null;

        private double fps = 0.0;

        private HandsTracker handTracker = new HandsTracker(30,(float)1.0,(float)0.3);

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
//                        this.framesSinceUpdate++;
//
//                        // update status unless last message is sticky for a while
//                        if (DateTime.Now >= this.nextStatusUpdate)
//                        {
//                            // calcuate fps based on last frame received
//                            fps = 0.0;
//
//                            if (this.stopwatch.IsRunning)
//                            {
//                                this.stopwatch.Stop();
//                                fps = this.framesSinceUpdate / this.stopwatch.Elapsed.TotalSeconds;
//                                this.stopwatch.Reset();
//                            }
//
//                            this.nextStatusUpdate = DateTime.Now + TimeSpan.FromSeconds(1);
////                            this.StatusText = string.Format(Properties.Resources.StandardStatusTextFormat, fps, frameReference.RelativeTime - this.startTime);
//                        }
//
//                        if (!this.stopwatch.IsRunning)
//                        {
//                            this.framesSinceUpdate = 0;
//                            this.stopwatch.Start();
//                        }
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
//                            Debug.WriteLine("BODY");
                            if (body.IsTracked)
                            {
//                                Debug.WriteLine("TRACKED");
                                IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                                // convert the joint points to depth (display) space
//                                Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();


                                foreach (JointType jointType in joints.Keys)
                                {
//                                    Debug.WriteLine(jointType.ToString());

//                                    if (jointInfos.ContainsKey(jointType.ToString()))
//                                    {
//                                        Debug.WriteLine(jointType.ToString());
//////                                        jointInfos[jointType.ToString()].UpdateJoint(joints[jointType]);
//                                    }

 
                                }
//
//                                Debug.WriteLine("X: {0}, Y{1}, Z{2}", joints[JointType.HandRight].Position.X,
//                                     joints[JointType.HandRight].Position.Y,
//                                      joints[JointType.HandRight].Position.Z);
                                var happened = handTracker.Record_hand_positions(joints[JointType.HandLeft], joints[JointType.HandRight]);

                                if (happened)
                                {
                                    Debug.WriteLine("flipped{0}", happened);
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