﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
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

        private int frameCounter = 0;
        private int frameSkip = 2;

        #region Variables
        readonly List<string> _fileTypes = new List<string> { "jpg", "jpeg", "tif", "tiff", "png", "gif" };

        
        /// <summary>
        /// Size of the RGB pixel in the bitmap
        /// </summary>
        private uint bytesPerPixel;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Reader for color frames
        /// </summary>
        private ColorFrameReader colorFrameReader = null;

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



        /// <summary>
        /// Execute shutdown tasks.
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.colorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }



        private void InitFileSource()
        {
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

            // get the kinectSensor object
            this.kinectSensor = KinectSensor.GetDefault();

            // open the reader for the color frames
            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();

            // wire handler for frame arrival
            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;

            // create the colorFrameDescription from the ColorFrameSource using rgba format
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);

            // rgba is 4 bytes per pixel
            this.bytesPerPixel = colorFrameDescription.BytesPerPixel;

            // allocate space to put the pixels to be rendered
            this.colorPixels = new byte[colorFrameDescription.Width * colorFrameDescription.Height * this.bytesPerPixel];

            // create the bitmap to display
            this.bitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height);

            // get the pixelStream for the writeableBitmap
            this.colorPixelStream = this.bitmap.PixelBuffer.AsStream();

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? "RunningStatusText"
                                                            : "NoSensorStatusText";
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
                frameCounter ++;
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
            ImgKinectTarget.Source = bitmap;
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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public sealed class MyFileContainer : INotifyPropertyChanged
    {
        public MyFileContainer(string filePath)
        {
            FilePath = filePath;

            var startingCut = FilePath.LastIndexOf("\\") + 1; //Ignore leading \
            var endingCut = FilePath.LastIndexOf(".");
            var lengthCut = endingCut - startingCut;

            ShortName = FilePath.Substring(startingCut, lengthCut);

            ImageSource = new Uri(FilePath);

#if DEBUG
            Debug.WriteLine("New FileContainer:\n\tFile Source: {0}\n\tShort Name: {1}", FilePath, ShortName);
#endif
        }


        private string _filePath;
        private string _shortName;
        private Uri _imageSource;


        public string FilePath
        {
            get { return _filePath; }
            set
            {
                if (value == _filePath) return;
                _filePath = value;
                OnPropertyChanged();
            }
        }

        public string ShortName
        {
            get { return _shortName; }
            set
            {
                if (value == _shortName) return;
                _shortName = value;
                OnPropertyChanged();
            }
        }

        public Uri ImageSource
        {
            get { return _imageSource; }
            set
            {
                if (Equals(value, _imageSource)) return;
                _imageSource = value;
                OnPropertyChanged();
            }
        }

        #region Property Changed


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }


        #endregion
    }
}