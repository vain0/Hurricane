using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using Hurricane.Music.Data;
using Hurricane.Music.Track;
using Hurricane.Settings;
using Hurricane.ViewModelBase;
// ReSharper disable ExplicitCallerInfoArgument

namespace Hurricane.Music.Playlist
{
    public abstract class PlaylistBase : PropertyChangedBase, IPlaylist
    {
        // ReSharper disable once InconsistentNaming
        protected Random _random;

        protected PlaylistBase()
        {
            _random = new Random();
        }

        // ReSharper disable once InconsistentNaming
        public abstract ObservableCollection<PlayableBase> Tracks { get; }

        private ICollectionView _viewsource;
        public ICollectionView ViewSource
        {
            get
            {
                return _viewsource;
            }
            set
            {
                SetProperty(value, ref _viewsource);
            }
        }

        private string _searchtext;
        public string SearchText
        {
            get { return _searchtext; }
            set
            {
                if (SetProperty(value, ref _searchtext))
                    ViewSource.Refresh();
            }
        }

        public virtual void LoadList()
        {
            if (Tracks != null)
            {
                Tracks.CollectionChanged += async (s, e) =>
                {
                    if (e.Action != NotifyCollectionChangedAction.Move || e.NewItems == null || e.NewItems.Count <= 0)
                        return;
                    var track = e.NewItems[0] as PlayableBase;
                    if (track == null) return;
                    track.IsAdded = true;
                    await Task.Delay(500);
                    track.IsAdded = false;
                };
                ViewSource = CollectionViewSource.GetDefaultView(Tracks);
                ViewSource.Filter = item => string.IsNullOrWhiteSpace(SearchText) || item.ToString().ToUpper().Contains(SearchText.ToUpper());
                ShuffleList = new List<PlayableBase>(Tracks);
            }
        }

        public abstract void Clear();

        #region Shuffle
        
        public List<PlayableBase> ShuffleList { get; set; }
        
        protected void CreateShuffleList()
        {
            ShuffleList = new List<PlayableBase>(Tracks);
        }

        protected void RemoveFromShuffleList(PlayableBase track)
        {
            ShuffleList.Remove(track);
        }

        public PlayableBase GetRandomTrack(PlayableBase currentTrack)
        {
            if (Tracks.Count == 0) return null;

            if (ShuffleList.Count == 0) CreateShuffleList();
            bool hasrefreshed = false;
            while (true)
            {
                int i = _random.Next(0, ShuffleList.Count);
                var track = ShuffleList[i];

                if (track != currentTrack && track.TrackExists)
                {
                    RemoveFromShuffleList(track);
                    return track;
                }
                RemoveFromShuffleList(track);
                if (ShuffleList.Count == 0)
                {
                    if (hasrefreshed)
                        return null;
                    CreateShuffleList();
                    hasrefreshed = true;
                }
            }
        }

        #endregion

        public virtual void AddTrack(PlayableBase track)
        {
        }

        public virtual void RemoveTrack(PlayableBase track)
        {
        }

        public abstract string Name { get; set; }

        public async Task<int> RemoveDuplicates()
        {
            int counter = Tracks.Count;
            List<PlayableBase> noduplicates = null;
            await Task.Run(() => noduplicates = Tracks.Distinct(new TrackComparer()).ToList());

            if (noduplicates.Count <= 0 || noduplicates.Count == Tracks.Count) return counter - noduplicates.Count;

            var duplicateList = Tracks.ToList();
            foreach (var noduplicate in noduplicates)
            {
                duplicateList.Remove(noduplicate);
            }

            foreach (var track in duplicateList)
            {
                RemoveTrack(track);
            }
            ViewSource.Refresh();

            return counter - noduplicates.Count;
        }

        public abstract bool CanEdit { get; }
        
        public override string ToString()
        {
            return Name;
        }
    }
}
