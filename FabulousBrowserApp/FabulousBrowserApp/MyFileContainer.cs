using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using FabulousBrowserApp.Annotations;

namespace FabulousBrowserApp
{
    public sealed class MyFileContainer : INotifyPropertyChanged
    {
        public MyFileContainer(StorageFile filePath)
        {
            _originalFile = filePath;

            FilePath = filePath.Path;

            var length = filePath.Name.LastIndexOf(".");

            ShortName = filePath.Name.Substring(0, length);

#if DEBUG
            Debug.WriteLine("New FileContainer:\n\tFile Source: {0}\n\tShort Name: {1}\n\tImageSource: {2}", FilePath, ShortName, ImageSource);
#endif
        }

        public async void Initialize()
        {
            var file = await Windows.Storage.KnownFolders.PicturesLibrary.GetFileAsync(_originalFile.Name);
            var stream = await file.OpenReadAsync();
            var bi = new BitmapImage();
            bi.SetSource(stream);
            
            ImageSource = bi;
        }


        private string _filePath;
        private string _shortName;
        private BitmapImage _imageSource;
        private StorageFile _originalFile;


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

        public BitmapImage ImageSource
        {
            get { return _imageSource; }
            set
            {
                if (Equals(value, _imageSource)) return;
                _imageSource = value;
                OnPropertyChanged();
            }
        }

        public async static Task<BitmapImage> ImageFromRelativePath(StorageFile storageFile)
        {
            var file = await Windows.Storage.KnownFolders.PicturesLibrary.GetFileAsync(storageFile.Name);
            var stream = await file.OpenReadAsync();
            var bi = new BitmapImage();
            bi.SetSource(stream);

            return bi;
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