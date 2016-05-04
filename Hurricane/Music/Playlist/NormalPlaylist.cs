using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Hurricane.Database;
using Hurricane.Music.CustomEventArgs;
using Hurricane.Music.Track;
using Hurricane.Utilities;

namespace Hurricane.Music.Playlist
{
    public class NormalPlaylist : PlaylistBase
    {
        private playlist _playlist;
        private ObservableCollection<PlayableBase> _tracks;

        public override string Name
        {
            get { return _playlist.Name; }
            set
            {
                _playlist.Name = value;
                OnPropertyChanged();
            }
        }

        public override ObservableCollection<PlayableBase> Tracks
        {
            get
            {
                return _tracks;
            }
        }

        public NormalPlaylist(string name)
            : base()
        {
            _playlist = Entity.Instance.playlists.Add(new playlist() { Name = name });
            Entity.Instance.SaveChanges();
        }

        public NormalPlaylist(playlist playlist)
            : base()
        {
            _playlist = playlist;
            
            _tracks =
                Entity.Instance.TrackList(_playlist.Id)
                .ToList()   // be strict
                .Select(track =>
                {
                    var localTrack = Entity.Instance.local_tracks.Find(track.Id);
                    return (PlayableBase)new LocalTrack(localTrack);
                })
                .ToObservableCollection();
        }
        
        public async Task AddFiles(IEnumerable<PlayableBase> tracks)
        {
            foreach (var track in tracks)
            {
                if (!await track.LoadInformation())
                    continue;
                track.TimeAdded = DateTime.Now;
                track.IsChecked = false;
                AddTrack(track);
            }

            AsyncTrackLoader.Instance.RunAsync(this);
        }

        public async Task AddFiles(EventHandler<TrackImportProgressChangedEventArgs> progresschanged, IEnumerable<string> paths)
        {
            int index = 0;
            var count = paths.Count();

            foreach (FileInfo fi in paths.Select(path => new FileInfo(path)))
            {
                if (fi.Exists)
                {
                    if (progresschanged != null) progresschanged(this, new TrackImportProgressChangedEventArgs(index, count, fi.Name));
                    var t = new LocalTrack(fi.FullName);
                    if (!await t.LoadInformation()) continue;
                    t.IsChecked = false;
                    AddTrack(t);
                }
                ++index;
            }
            AsyncTrackLoader.Instance.RunAsync(this);
        }

        public async Task AddFiles(params string[] paths)
        {
            await AddFiles(null, paths);
        }

        public async Task AddFiles(IEnumerable<string> paths)
        {
            await AddFiles(null, paths);
        }

        public async Task ReloadTrackInformation(EventHandler<TrackImportProgressChangedEventArgs> progresschanged)
        {
            foreach (var t in Tracks)
            {
                if (progresschanged != null) progresschanged(this, new TrackImportProgressChangedEventArgs(Tracks.IndexOf(t), Tracks.Count, t.ToString()));
                if (t.TrackExists)
                {
                    await t.LoadInformation();
                }
            }
        }

        public override void AddTrack(PlayableBase track)
        {
            Entity.Instance.playlist_items.Add(new playlist_items()
            {
                PlaylistId = _playlist.Id,
                Index = Tracks.Count,
                TrackId = track.Id
            });
            Tracks.Add(track);

            if (ShuffleList != null)
                ShuffleList.Add(track);

            track.IsAdded = true;
            var tmr = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            tmr.Tick += (s, e) =>
            {
                track.IsAdded = false;
                tmr.Stop();
            };
            tmr.Start();
        }

        public async override void RemoveTrack(PlayableBase track)
        {
            ShuffleList.Remove(track);
            track.IsRemoving = true;

            await Task.Delay(500);

            // Values of playlist_items.Index become inconsistent. FixIndexes is called to fix this before exiting.
            var playlist_item =
                Entity.Instance.playlist_items
                .Where(item => item.PlaylistId == _playlist.Id && item.TrackId == track.Id)
                .FirstOrDefault();
            if (playlist_item != null)
            {
                Entity.Instance.playlist_items.Remove(playlist_item);
            }

            if (!track.TrackExists)
            {
                for (int i = 0; i < Tracks.Count; i++)
                {
                    if (Tracks[i].AuthenticationCode == track.AuthenticationCode)
                    {
                        Tracks.RemoveAt(i);
                        break;
                    }
                }
            }
            else { Tracks.Remove(track); }
            
            track.IsRemoving = false; //The track could be also in another playlist
        }

        public override void Clear()
        {
            Tracks.Clear();
            ShuffleList.Clear();
        }

        public void FixIndexes()
        {
            var playlistItems = Entity.Instance.playlist_items.Where(item => item.PlaylistId == _playlist.Id);
            var indexFromTrackId = new MultiDictionary<int, int>();
            for (var i = 0; i < Tracks.Count; i ++)
            {
                indexFromTrackId.Add(Tracks[i].Id, i);
            }

            foreach (var item in playlistItems)
            {
                var indexList = indexFromTrackId.GetValueList(item.TrackId);
                var i = indexList.FirstOrDefault();     // Never default if no bugs
                item.Index = i;
                indexFromTrackId.Remove(item.TrackId, i);
            }
        }

        public override bool CanEdit
        {
            get { return true; }
        }
    }
}
