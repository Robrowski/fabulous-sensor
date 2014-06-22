using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238
using WindowsPreview.Kinect;
using FabulousBrowserApp.Annotations;

namespace FabulousBrowserApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
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


        private int frameCounter = 0;
        private int frameSkip = 2;

        #region Variables
        readonly List<string> _fileTypes = new List<string> { "jpg", "jpeg", "tif", "tiff", "png", "gif" };

        

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for depth/color/body index frames
        /// </summary>
        private MultiSourceFrameReader reader = null;

        /// <summary>
        /// Intermediate storage for receiving depth frame data from the sensor
        /// </summary>
        private ushort[] depthFrameData = null;

        /// <summary>
        /// Intermediate storage for receiving color frame data from the sensor
        /// </summary>
        private byte[] colorFrameData = null;

        /// <summary>
        /// Intermediate storage for receiving body index frame data from the sensor
        /// </summary>
        private byte[] bodyIndexFrameData = null;

        /// <summary>
        /// Intermediate storage for frame data converted to color
        /// </summary>
        private byte[] displayPixels = null;

        /// <summary>
        /// Intermediate storage for the depth to color mapping
        /// </summary>
        private ColorSpacePoint[] colorPoints = null;
        
        /// <summary>
        /// Size of the RGB pixel in the bitmap
        /// </summary>
        private uint bytesPerPixel;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;


        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap bitmap = null;

        /// <summary>
        /// PixelStream for writeableBitmap
        /// </summary>
        private Stream colorPixelStream = null;

        /// <summary>
        /// Intermediate storage for receiving frame data from the sensor
        /// </summary>
        private byte[] colorPixels = null;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
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

        #region DP's

        public static readonly DependencyProperty FileSourceProperty = DependencyProperty.Register(
            "FileSource", typeof(ObservableCollection<MyFileContainer>), typeof(MainPage),
            new PropertyMetadata(default(ObservableCollection<MyFileContainer>)));

        public ObservableCollection<MyFileContainer> FileSource
        {
            get { return (ObservableCollection<MyFileContainer>)GetValue(FileSourceProperty); }
            set { SetValue(FileSourceProperty, value); }
        }

        #endregion


        #endregion

        


        public MainPage()
        {
            FileSource = new ObservableCollection<MyFileContainer>();

            InitializeComponent();

            InitFileSource();

            InitKinect();
        }







        private async void InitFileSource()
        {
            StorageFolder picturesFolder = KnownFolders.PicturesLibrary;
            Debug.WriteLine("Init file source: {0}", picturesFolder.Name);


            IReadOnlyList<StorageFile> fileList = await picturesFolder.GetFilesAsync();

            foreach (StorageFile file in fileList)
            {
                foreach (var fileExtension in _fileTypes)
                {
                    if (file.Name.ToLower().EndsWith(fileExtension))
                    {
                        var fileContainer = new MyFileContainer(file);
                        fileContainer.Initialize();
                        FileSource.Add(fileContainer);

                        break;
                    }
                }
            }

            //foreach (var file in files.Where(file => _fileTypes.Any(filetype => file.ToLower().EndsWith(filetype))))
            //{
            //    FileSource.Add(new MyFileContainer(file));
            //}

            //var pictureDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            //var files = Directory.GetFiles(pictureDir);

            //foreach (var file in files.Where(file => _fileTypes.Any(filetype => file.ToLower().EndsWith(filetype))))
            //{
            //    FileSource.Add(new MyFileContainer(file));
            //}
        }

        private void InitKinect()
        {
            Debug.WriteLine("InitKinect");

            // for Alpha, one sensor is supported
            this.kinectSensor = KinectSensor.GetDefault();

            if (this.kinectSensor != null)
            {
                // get the coordinate mapper
                this.coordinateMapper = this.kinectSensor.CoordinateMapper;

                // open the sensor
                this.kinectSensor.Open();

                FrameDescription depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

                int depthWidth = depthFrameDescription.Width;
                int depthHeight = depthFrameDescription.Height;

                // allocate space to put the pixels being received and converted
                this.depthFrameData = new ushort[depthWidth*depthHeight];
                this.bodyIndexFrameData = new byte[depthWidth*depthHeight];
                this.displayPixels = new byte[depthWidth*depthHeight*this.bytesPerPixel];
                this.colorPoints = new ColorSpacePoint[depthWidth*depthHeight];


                FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;
            // create the bitmap to display
            this.bitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height);

                int colorWidth = colorFrameDescription.Width;
                int colorHeight = colorFrameDescription.Height;

                // allocate space to put the pixels being received
                this.colorFrameData = new byte[colorWidth*colorHeight*this.bytesPerPixel];

                this.reader =
                    this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color |
                                                                 FrameSourceTypes.BodyIndex);

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? "RunningStatusText"
                                                            : "NoSensorStatusText";
        }
        }




        /// <summary>
        /// Handles the color frame data arriving from the sensor.
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {

            if (frameCounter <= frameSkip)
            {
                frameCounter++;
                return;
            }
            frameCounter = 0;

            //Debug.WriteLine("Reader ColorFrame Arrived");
            bool colorFrameProcessed = false;

            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    // verify data and write the new color frame data to the display bitmap
                    if ((colorFrameDescription.Width == this.bitmap.PixelWidth) && (colorFrameDescription.Height == this.bitmap.PixelHeight))
                    {
                        if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                        {
                            colorFrame.CopyRawFrameDataToArray(this.colorPixels);
                        }
                        else
                        {
                            colorFrame.CopyConvertedFrameDataToArray(this.colorPixels, ColorImageFormat.Bgra);
                        }

                        colorFrameProcessed = true;
                    }
                }
            }

            // we got a frame, render
            if (colorFrameProcessed)
            {
                RenderColorPixels(this.colorPixels);
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        /// <param name="pixels">pixel data</param>
        private void RenderColorPixels(byte[] pixels)
        {
            //Debug.WriteLine("RenderColorPixels");
            colorPixelStream.Seek(0, SeekOrigin.Begin);
            colorPixelStream.Write(pixels, 0, pixels.Length);
            bitmap.Invalidate();
            //OnPropertyChanged("ImageSource");
//            ImgKinectTarget.Source = bitmap; // Removed from XAML
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? "RunningStatusText"
                                                            : "SensorNotAvailableStatusText";
        }

        #region Prop Changed

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Button Clicks

        private void PrevBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var index = LbFileSource.SelectedIndex;
            var size = LbFileSource.Items.Count;

            index--;
            var next = Math.Abs(index % size);
            if (index == -1)
                next = size - 1;
            
            LbFileSource.SelectedIndex = next;
            Debug.WriteLine("index is {0}, next is{1}",index, next);
        }

        private void NextBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var index = LbFileSource.SelectedIndex;
            var size = LbFileSource.Items.Count;

            index++;
            var next = index%size;
            LbFileSource.SelectedIndex = next;
//            Debug.WriteLine("index is {0}", next);
        }
    }

        #endregion

        private void MainPage_OnLoaded(object sender, RoutedEventArgs e)
    {
            if (this.reader != null)
        {
                this.reader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;
            }
        }

        /// <summary>
        /// Execute shutdown tasks.
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            //if (this.colorFrameReader != null)
            //{
            //    // ColorFrameReder is IDisposable
            //    this.colorFrameReader.Dispose();
            //    this.colorFrameReader = null;
            //}

            //if (this.kinectSensor != null)
            //{
            //    this.kinectSensor.Close();
            //    this.kinectSensor = null;
            //}

            if (this.reader != null)
            {
                // MultiSourceFrameReder is IDisposable
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
        /// Handles the depth/color/body index frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrameReference frameReference = e.FrameReference;

            MultiSourceFrame multiSourceFrame = null;
            DepthFrame depthFrame = null;
            ColorFrame colorFrame = null;
            BodyIndexFrame bodyIndexFrame = null;

            try
            {
                multiSourceFrame = frameReference.AcquireFrame();

                if (multiSourceFrame != null)
        {
                    DepthFrameReference depthFrameReference = multiSourceFrame.DepthFrameReference;
                    ColorFrameReference colorFrameReference = multiSourceFrame.ColorFrameReference;
                    BodyIndexFrameReference bodyIndexFrameReference = multiSourceFrame.BodyIndexFrameReference;
            
                    if (this.startTime.Ticks == 0)
                    {
                        this.startTime = depthFrameReference.RelativeTime;
        }

                    depthFrame = depthFrameReference.AcquireFrame();
                    colorFrame = colorFrameReference.AcquireFrame();
                    bodyIndexFrame = bodyIndexFrameReference.AcquireFrame();

                    if ((depthFrame != null) && (colorFrame != null) && (bodyIndexFrame != null))
                    {
                        this.framesSinceUpdate++;

                        FrameDescription depthFrameDescription = depthFrame.FrameDescription;
                        FrameDescription colorFrameDescription = colorFrame.FrameDescription;
                        FrameDescription bodyIndexFrameDescription = bodyIndexFrame.FrameDescription;

                        // update status unless last message is sticky for a while
                        if (DateTime.Now >= this.nextStatusUpdate)
        {
                            // calcuate fps based on last frame received
                            double fps = 0.0;

                            if (this.stopwatch.IsRunning)
            {
                                this.stopwatch.Stop();
                                fps = this.framesSinceUpdate / this.stopwatch.Elapsed.TotalSeconds;
                                this.stopwatch.Reset();
            }

                            this.nextStatusUpdate = DateTime.Now + TimeSpan.FromSeconds(1);
                            this.StatusText = string.Format("FPS: {0} -- Time: {1}", fps, depthFrameReference.RelativeTime - this.startTime);
        }

                        if (!this.stopwatch.IsRunning)
            {
                            this.framesSinceUpdate = 0;
                            this.stopwatch.Start();
        }

                        int depthWidth = depthFrameDescription.Width;
                        int depthHeight = depthFrameDescription.Height;

                        int colorWidth = colorFrameDescription.Width;
                        int colorHeight = colorFrameDescription.Height;

                        int bodyIndexWidth = bodyIndexFrameDescription.Width;
                        int bodyIndexHeight = bodyIndexFrameDescription.Height;

                        // verify data and write the new registered frame data to the display bitmap
                        if (((depthWidth * depthHeight) == this.depthFrameData.Length) &&
                            ((colorWidth * colorHeight * this.bytesPerPixel) == this.colorFrameData.Length) &&
                            ((bodyIndexWidth * bodyIndexHeight) == this.bodyIndexFrameData.Length))
        {
                            depthFrame.CopyFrameDataToArray(this.depthFrameData);
                            if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                                colorFrame.CopyRawFrameDataToArray(this.colorFrameData);
            }
                            else
                            {
                                colorFrame.CopyConvertedFrameDataToArray(this.colorFrameData, ColorImageFormat.Bgra);
        }

                            bodyIndexFrame.CopyFrameDataToArray(this.bodyIndexFrameData);

                            this.coordinateMapper.MapDepthFrameToColorSpace(this.depthFrameData, this.colorPoints);

                            Array.Clear(this.displayPixels, 0, this.displayPixels.Length);

                            // loop over each row and column of the depth
                            for (int y = 0; y < depthHeight; ++y)
                            {
                                for (int x = 0; x < depthWidth; ++x)
        {
                                    // calculate index into depth array
                                    int depthIndex = (y * depthWidth) + x;

                                    byte player = this.bodyIndexFrameData[depthIndex];

                                    // if we're tracking a player for the current pixel, sets its color and alpha to full
                                    if (player != 0xff)
                                    {
                                        // retrieve the depth to color mapping for the current depth pixel
                                        ColorSpacePoint colorPoint = this.colorPoints[depthIndex];

                                        // make sure the depth pixel maps to a valid point in color space
                                        int colorX = (int)Math.Floor(colorPoint.X + 0.5);
                                        int colorY = (int)Math.Floor(colorPoint.Y + 0.5);
                                        if ((colorX >= 0) && (colorX < colorWidth) && (colorY >= 0) && (colorY < colorHeight))
                                        {
                                            // calculate index into color array
                                            int colorIndex = (int)(((colorY * colorWidth) + colorX) * this.bytesPerPixel);

                                            // set source for copy to the color pixel
                                            int displayIndex = (int)(depthIndex * this.bytesPerPixel);
                                            this.displayPixels[displayIndex] = this.colorFrameData[colorIndex];
                                            this.displayPixels[displayIndex + 1] = this.colorFrameData[colorIndex + 1];
                                            this.displayPixels[displayIndex + 2] = this.colorFrameData[colorIndex + 2];
                                            this.displayPixels[displayIndex + 3] = 0xff;
                                        }
                                    }
                                }
                            }

                            RenderColorPixels(displayPixels);

                            //this.bitmap.WritePixels(new Int32Rect(0, 0, depthWidth, depthHeight),
                            //                        this.displayPixels,
                            //                        depthWidth * this.bytesPerPixel,
                            //                        0);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignore if the frame is no longer available
            }
            finally
            {
                // DepthFrame, ColorFrame, BodyIndexFrame are IDispoable
                if (depthFrame != null)
        {
                    depthFrame.Dispose();
                    depthFrame = null;
        }

                if (colorFrame != null)
                {
                    colorFrame.Dispose();
                    colorFrame = null;
                }

                if (bodyIndexFrame != null)
                {
                    bodyIndexFrame.Dispose();
                    bodyIndexFrame = null;
                }
            }
        }
    }
}
