using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;
using Hurricane.Music.Playlist;

namespace Hurricane.Settings
{
    public class PlaylistSettings : SettingsBase
    {
        public ObservableCollection<NormalPlaylist> Playlists { get; set; }

        public override void SetStandardValues()
        {
            Playlists = new ObservableCollection<NormalPlaylist> { new NormalPlaylist() { Name = "Default" } };
        }

        public override void Save(string programPath)
        {
        }

        public static PlaylistSettings Load(string programpath)
        {
            var playlistSettings = new PlaylistSettings();
            playlistSettings.SetStandardValues();
            return playlistSettings;
        }
    }
}