using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using Hurricane.Music.Track;
using Hurricane.Utilities;
using Hurricane.ViewModelBase;
using TagLib;
using File = TagLib.File;

namespace Hurricane.Music.Download
{
    [Serializable]
    public class DownloadManager : PropertyChangedBase
    {
        [XmlIgnore]
        public ObservableCollection<DownloadEntry> Entries { get; set; }

        private bool _isOpen;
        [XmlIgnore]
        public bool IsOpen
        {
            get { return _isOpen; }
            set
            {
                SetProperty(value, ref _isOpen);
            }
        }
        
        public async static Task AddTags(IMusicInformation information, string path)
        {
            var filePath = new FileInfo(path);
            if (!filePath.Exists) return;
            try
            {
                using (var file = File.Create(filePath.FullName))
                {
                    file.Tag.Album = information.Album;
                    file.Tag.Performers = new[] { information.Artist };
                    file.Tag.Year = information.Year;
                    file.Tag.Title = information.Title;
                    var image = await information.GetImage();
                    if (image != null)
                    {
                        byte[] data;
                        JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(image));
                        using (MemoryStream ms = new MemoryStream())
                        {
                            encoder.Save(ms);
                            data = ms.ToArray();
                        }
                        file.Tag.Pictures = new IPicture[] { new Picture(new ByteVector(data, data.Length)) };
                    }
                    await Task.Run(() => file.Save());
                }
            }
            catch (CorruptFileException)
            {
            }
        }

        public DownloadManager()
        {
            Entries = new ObservableCollection<DownloadEntry>();
            SelectedService = 0;
            Searches = new ObservableCollection<string>();
        }

        #region Settings

        private int _selectedService;
        public int SelectedService
        {
            get { return _selectedService; }
            set
            {
                SetProperty(value, ref _selectedService);
            }
        }

        public ObservableCollection<string> Searches { get; set; }


        private bool _hasEntries;
        [XmlIgnore]
        public bool HasEntries
        {
            get { return _hasEntries; }
            set
            {
                SetProperty(value, ref _hasEntries);
            }
        }

        #endregion
    }
}
