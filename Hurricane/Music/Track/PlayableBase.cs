using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CSCore;
using Hurricane.Database;
using Hurricane.Music.AudioEngine;
using Hurricane.Music.Data;
using Hurricane.Music.MusicCover;
using Hurricane.Settings;
using Hurricane.Utilities;
using Hurricane.ViewModelBase;
using Hurricane.ViewModels;

// ReSharper disable ExplicitCallerInfoArgument

namespace Hurricane.Music.Track
{
    public abstract class PlayableBase : PropertyChangedBase, IEquatable<PlayableBase>, IRepresentable,
        IMusicInformation
    {
        private track _track;
        
        private CancellationTokenSource _disposeImageCancellationToken;

        private BitmapImage _image;

        private bool _isAdded;

        private bool _isChecked;

        private bool _isFavorite;

        private bool _isLoadingImage;

        private bool _isOpened;

        private bool _isRemoving;

        private string _queueId;
        
        protected PlayableBase()
        {
            IsChecked = true;

            _track = Entity.Instance.tracks.Add(new track()
            {
                AuthenticationCode = DateTime.Now.Ticks,
                TimeAdded = DateTime.Now
            });
            Entity.Instance.SaveChanges();
        }

        protected PlayableBase(track track)
        {
            _track = track;
        }

        // TODO: We can use Id instead of this
        public long AuthenticationCode
        {
            get { return _track.AuthenticationCode; }
            private set
            {
                _track.AuthenticationCode = value;
            }
        }

        public int Id
        {
            get { return _track.Id; }
        }

        public string Duration
        {
            get
            {
                return DurationTimespan.ToString(DurationTimespan.Hours == 0 ? @"mm\:ss" : @"hh\:mm\:ss");
            }
            set
            {
                var duration = TimeSpan.ParseExact(value, value.Split(':').Length == 2 ? @"mm\:ss" : @"hh\:mm\:ss", null);
                if (duration == null) throw new ArgumentException();
                DurationTimespan = duration;
            }
        }
        
        // ReSharper disable once InconsistentNaming
        [DefaultValue(0)]
        public int kHz
        {
            get { return _track.kHz; }
            set
            {
                _track.kHz = value;
                OnPropertyChanged();
            }
        }

        // ReSharper disable once InconsistentNaming
        public int kbps
        {
            get { return _track.kbps; }
            set
            {
                _track.kbps = value;
                OnPropertyChanged();
            }
        }

        public DateTime TimeAdded
        {
            get { return _track.TimeAdded; }
            set
            {
                _track.TimeAdded = value;
                OnPropertyChanged();
            }
        }

        public DateTime? LastTimePlayed
        {
            get { return _track.LastTimePlayed; }
            set
            {
                _track.LastTimePlayed = value;
                OnPropertyChanged();
            }
        }

        // number of this track in album; useful for sorting
        [DefaultValue(0)]
        public int TrackNumber
        {
            get { return _track.TrackNumber; }
            set
            {
                _track.TrackNumber = value;
                OnPropertyChanged();
            }
        }

        [DefaultValue(0.0)]
        public double StartTime { get; set; }

        [DefaultValue(0.0)]
        public double EndTime { get; set; }

        [DefaultValue(true)]
        public bool IsChecked
        {
            get { return _isChecked; }
            set { SetProperty(value, ref _isChecked); }
        }

        public bool IsOpened
        {
            get { return _isOpened; }
            set { SetProperty(value, ref _isOpened); }
        }

        public string QueueId
            //I know that the id should be an int, but it wouldn't make sense because what would be the id for non queued track? We would need a converter -> less performance -> string is wurf
        {
            get { return _queueId; }
            set { SetProperty(value, ref _queueId); }
        }

        public TimeSpan DurationTimespan
        {
            get { return _track.Duration ?? new TimeSpan(); }
            set
            {
                _track.Duration = value;
                OnPropertyChanged();
            }
        }

        public string DisplayText
        {
            get
            {
                return !string.IsNullOrEmpty(Artist) && HurricaneSettings.Instance.Config.ShowArtistAndTitle
                    ? string.Format("{0} - {1}", Artist, Title)
                    : Title;
            }
        }

        public abstract bool TrackExists { get; }
        public abstract TrackType TrackType { get; }

        public bool IsRemoving
        {
            get { return _isRemoving; }
            set { SetProperty(value, ref _isRemoving); }
        }

        public bool IsAdded
        {
            get { return _isAdded; }
            set { SetProperty(value, ref _isAdded); }
        }

        public abstract bool Equals(PlayableBase other);

        public string Album
        {
            get { return _track.Album; }
            set
            {
                _track.Album = value;
                OnPropertyChanged();
            }
        }

        public uint Year
        {
            get { return (uint)_track.Year; }
            set
            {
                _track.Year = (short)value;
                OnPropertyChanged();
            }
        }

        public List<Genre> Genres { get; set; }

        public string Title
        {
            get { return _track.Title; }
            set
            {
                _track.Title = value;
                OnPropertyChanged("DisplayText");
                OnPropertyChanged();
            }
        }

        public string Artist
        {
            get { return _track.Artist; }
            set
            {
                _track.Artist = value;
                OnPropertyChanged("DisplayText");
                OnPropertyChanged();
            }
        }

        TimeSpan IMusicInformation.Duration
        {
            get { return DurationTimespan; }
            set { throw new NotImplementedException(); }
        }

        public async Task<BitmapImage> GetImage()
        {
            if (Image == null)
            {
                var waiter = new AutoResetEvent(false);

                ImageLoadedComplete += (s, e) => { waiter.Set(); };
                Load();
                await Task.Run(() => waiter.WaitOne(2000));
            }
            return Image;
        }

        public event EventHandler ImageLoadedComplete;

        public BitmapImage Image
        {
            get { return _image; }
            set
            {
                if (value != null && !value.IsFrozen) value.Freeze(); //The image has to be thread save
                SetProperty(value, ref _image);
            }
        }

        public bool IsLoadingImage
        {
            get { return _isLoadingImage; }
            set { SetProperty(value, ref _isLoadingImage); }
        }

        public abstract Task<bool> LoadInformation();
        public abstract void OpenTrackLocation();
        public abstract Task<IWaveSource> GetSoundSource();
        protected abstract Task LoadImage(DirectoryInfo albumCoverDirectory);

        public async void Load()
        {
            if (_disposeImageCancellationToken != null) _disposeImageCancellationToken.Cancel();
            if (Image == null)
            {
                IsLoadingImage = true;
                var albumCoverDirectory = new DirectoryInfo(HurricaneSettings.Paths.CoverDirectory);
                Image = MusicCoverManager.GetTrackImage(this, albumCoverDirectory);
                if (Image == null) await LoadImage(albumCoverDirectory);
                IsLoadingImage = false;
            }

            OnImageLoadComplete();
        }

        public virtual async void Unload()
        {
            if (Image != null)
            {
                _disposeImageCancellationToken = new CancellationTokenSource();
                try
                {
                    await Task.Delay(2000, _disposeImageCancellationToken.Token); //Some animations need that
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                if (Image.StreamSource != null) Image.StreamSource.Dispose();
                Image = null;
            }
        }

        public virtual void RefreshTrackExists()
        {
            OnPropertyChanged("TrackExists");
        }

        public override string ToString()
        {
            return DisplayText;
        }

        protected virtual void OnImageLoadComplete()
        {
            var handler = ImageLoadedComplete;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected IWaveSource CutWaveSource(IWaveSource source)
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (StartTime == 0 && EndTime == 0)
                return source;

            var startTime = TimeSpan.FromMilliseconds(StartTime);
            var endTime = TimeSpan.FromMilliseconds(EndTime);
            return source.AppendSource(x => new CutSource(x, startTime, endTime - startTime));
        }

        public virtual async Task<bool> CheckTrack()
        {
            if (!TrackExists) return false;
            try
            {
                using (var soundSource = await GetSoundSource())
                {
                    DurationTimespan = soundSource.GetLength();
                    kHz = soundSource.WaveFormat.SampleRate/1000;
                }
            }
            catch (Exception)
            {
                return false;
            }

            IsChecked = true;
            return true;
        }

        public static Genre StringToGenre(string genre)
        {
            switch (genre)
            {
                case "Country":
                    return Genre.Country;

                case "Reggae":
                    return Genre.Reggae;

                case "Alternative":
                    return Genre.Alternative;

                case "Vocal":
                    return Genre.AcousticAndVocal;

                case "Trance":
                    return Genre.Trance;

                case "Classical":
                case "Classic":
                    return Genre.Classical;

                case "Game":
                case "Sound Clip":
                case "Soundtrack":
                    return Genre.SoundTrack;

                case "Bass":
                case "Drum & Bass":
                    return Genre.DrumAndBass;

                case "Ethnic":
                    return Genre.Ethnic;

                case "Darkwave":
                case "Gothic":
                case "Grunge":
                case "Metal":
                case "Polsk Punk":
                case "Acid Punk":
                case "Death Metal":
                case "Heavy Metal":
                case "Black Metal":
                case "Thrash Metal":
                    return Genre.Metal;

                case "Techno-Industrial":
                case "Electronic":
                case "Euro-Techno":
                case "Techno":
                case "Disco":
                case "Electropop":
                case "Electropop & Disco":
                    return Genre.ElectropopAndDisco;

                case "Eurodance":
                case "Dance":
                case "House":
                case "Club":
                case "Dance Hall":
                case "Club-House":
                case "Dance & House":
                    return Genre.DanceAndHouse;

                case "Christian Rap":
                case "Trip-Hop":
                case "Rap":
                case "Hip-Hop":
                case "Freestyle":
                case "Rap & Hip-Hop":
                    return Genre.RapAndHipHop;

                case "Pop/Funk":
                case "Pop-Folk":
                case "Pop":
                    return Genre.Pop;

                case "Trailer":
                case "Symphony":
                case "Dream":
                case "Instrumental Rock":
                case "Instrumental Pop":
                case "Meditative":
                case "Instrumental":
                    return Genre.Instrumental;

                case "Swing":
                case "Acid Jazz":
                case "Soul":
                case "Jazz+Funk":
                case "R&B":
                case "Oldies":
                case "Jazz":
                case "Funk":
                case "Blues":
                case "Rhythmic Soul":
                    return Genre.JazzAndBlues;

                case "Gothic Rock":
                case "Progressive Rock":
                case "Psychedelic Rock":
                case "Symphonic Rock":
                case "Slow Rock":
                case "Rock & Roll":
                case "Hard Rock":
                case "Classic Rock":
                case "Southern Rock":
                case "Punk":
                case "Alternative Rock":
                case "Rock":
                case "Punk Rock":
                    return Genre.Rock;

                case "Indie":
                case "Indie Pop":
                    return Genre.IndiePop;

                default:
                    return Genre.Other;
            }
        }

        public static string GenreToString(Genre genre)
        {
            return genre.ToString().ToSentenceCase().Replace("And", "&");
        }
    }

    public enum TrackType
    {
        File,
        Stream
    }
}