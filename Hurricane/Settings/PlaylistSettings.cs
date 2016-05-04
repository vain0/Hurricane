using System;
using System.Collections.ObjectModel;
using System.Linq;
using Hurricane.Database;
using Hurricane.Music.Playlist;
using Hurricane.Utilities;

namespace Hurricane.Settings
{
    public class PlaylistSettings : SettingsBase
    {
        public ObservableCollection<NormalPlaylist> Playlists { get; set; }

        private static ObservableCollection<NormalPlaylist> DefaultPlaylists()
        {
            return new ObservableCollection<NormalPlaylist> { new NormalPlaylist(name: "Default") };
        }

        public override void SetStandardValues()
        {
            Playlists = DefaultPlaylists();
        }

        public override void Save(string programPath)
        {
            foreach (var playlist in Playlists)
            {
                playlist.FixIndexes();
            }
        }

        public static PlaylistSettings Load(string programpath)
        {
            var playlistSettings = new PlaylistSettings();

            var playlists =
                Entity.Instance.playlists
                .ToList()   // to be strict
                .Select(playlist => new NormalPlaylist(playlist))
                .ToObservableCollection();

            playlistSettings.Playlists = playlists.IsEmpty() ? DefaultPlaylists() : playlists;
            return playlistSettings;
        }
    }
}