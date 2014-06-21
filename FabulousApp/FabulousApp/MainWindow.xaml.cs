using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using FabulousApp.Annotations;

namespace FabulousApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        readonly List<string> _fileTypes = new List<string>{"jpg", "jpeg", "tif", "tiff", "png", "gif"};

        #region DP's

        public static readonly DependencyProperty FileSourceProperty = DependencyProperty.Register(
            "FileSource", typeof(ObservableCollection<MyFileContainer>), typeof(MainWindow),
            new PropertyMetadata(default(ObservableCollection<MyFileContainer>)));

        public ObservableCollection<MyFileContainer> FileSource
        {
            get { return (ObservableCollection<MyFileContainer>)GetValue(FileSourceProperty); }
            set { SetValue(FileSourceProperty, value); }
        }

        #endregion

        public MainWindow()
        {
            //Console.WriteLine("WTF");
            //FileSource = new ObservableCollection<MyFileContainer>();

            InitializeComponent();

            //InitFileSource();
        }

        private void InitFileSource()
        {
            var pictureDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            var files = Directory.GetFiles(pictureDir);

#if DEBUG
            Console.WriteLine("UserDir {0}", pictureDir);

            foreach(var file in files)
                Console.WriteLine("Found file {0}", file);
#endif

            foreach (var file in files.Where(file => _fileTypes.Any(filetype => file.ToLower().EndsWith(filetype))))
            {
                FileSource.Add(new MyFileContainer(file));
            }
        }
    }

    public sealed class MyFileContainer: INotifyPropertyChanged
    {
        public MyFileContainer(string filePath)
        {
            FilePath = filePath;

            var startingCut = FilePath.LastIndexOf("\\") + 1; //Ignore leading \
            var endingCut = FilePath.LastIndexOf(".");
            var lengthCut = endingCut - startingCut;

            ShortName = FilePath.Substring(startingCut, lengthCut);

#if DEBUG
            Console.WriteLine("New FileContainer:\n\tFile Source: {0}\n\tShort Name: {1}", FilePath, ShortName);
#endif
        }


        private string _filePath;
        private string _shortName;


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

        #region Property changed

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

    }
}
