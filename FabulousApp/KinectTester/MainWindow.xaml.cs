using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using KinectTester.Annotations;
using Microsoft.Kinect;

namespace KinectTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private KinectSensor kinectSensor = null;
        private ColorFrameReader reader = null;
        private byte[] pixels = null;
        private int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;
        private string statusText = "Init";
        private WriteableBitmap bitmap = null;


        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.bitmap;
            }
        }


        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            //IntializeKinect();
        }

        private void IntializeKinect()
        {
            Console.WriteLine("Init Kinect");
            // for Alpha, one sensor is supported
            this.kinectSensor = KinectSensor.GetDefault();

            //if (this.kinectSensor != null)
            //{
            //    // open the sensor
            //    this.kinectSensor.Open();

            //    FrameDescription frameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;

            //    // open the reader for the color frames
            //    this.reader = this.kinectSensor.ColorFrameSource.OpenReader();

            //    // allocate space to put the pixels being received
            //    this.pixels = new byte[frameDescription.Width * frameDescription.Height * this.bytesPerPixel];

            //    // create the bitmap to display
            //    this.bitmap = new WriteableBitmap(frameDescription.Width, frameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            //    // set the status text
            //    this.StatusText = "Initialized";
            //}
            //else
            //{
            //    // on failure, set the status text
            //    this.StatusText = "No Sensor";
            //    Console.WriteLine("Fail");
            //}
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.reader != null)
            {
                this.reader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.reader != null)
            {
                // ColorFrameReder is IDisposable
                this.reader.Dispose();
                this.reader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the color frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            ColorFrameReference frameReference = e.FrameReference;

            //if (this.startTime.Ticks == 0)
            //{
            //    this.startTime = frameReference.RelativeTime;
            //}

            try
            {
                ColorFrame frame = frameReference.AcquireFrame();

                if (frame != null)
                {
                    // ColorFrame is IDisposable
                    using (frame)
                    {
                        //this.framesSinceUpdate++;

                        FrameDescription frameDescription = frame.FrameDescription;

                        //// update status unless last message is sticky for a while
                        //if (DateTime.Now >= this.nextStatusUpdate)
                        //{
                        //    // calcuate fps based on last frame received
                        //    double fps = 0.0;

                        //    if (this.stopwatch.IsRunning)
                        //    {
                        //        this.stopwatch.Stop();
                        //        fps = this.framesSinceUpdate / this.stopwatch.Elapsed.TotalSeconds;
                        //        this.stopwatch.Reset();
                        //    }

                        //    this.nextStatusUpdate = DateTime.Now + TimeSpan.FromSeconds(1);
                        //    this.StatusText = string.Format(Properties.Resources.StandardStatusTextFormat, fps, frameReference.RelativeTime - this.startTime);
                        //}

                        //if (!this.stopwatch.IsRunning)
                        //{
                        //    this.framesSinceUpdate = 0;
                        //    this.stopwatch.Start();
                        //}

                        // verify data and write the new color frame data to the display bitmap
                        if ((frameDescription.Width == this.bitmap.PixelWidth) && (frameDescription.Height == this.bitmap.PixelHeight))
                        {
                            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
                            {
                                frame.CopyRawFrameDataToArray(this.pixels);
                            }
                            else
                            {
                                frame.CopyConvertedFrameDataToArray(this.pixels, ColorImageFormat.Bgra);
                            }

                            this.bitmap.WritePixels(
                                new Int32Rect(0, 0, frameDescription.Width, frameDescription.Height),
                                this.pixels,
                                frameDescription.Width * this.bytesPerPixel,
                                0);
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
