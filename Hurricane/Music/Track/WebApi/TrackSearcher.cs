﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Hurricane.Music.Download;
using Hurricane.Music.Playlist;
using Hurricane.Settings;
using Hurricane.ViewModelBase;
using Hurricane.Views;
using MahApps.Metro.Controls.Dialogs;

namespace Hurricane.Music.Track.WebApi
{
    public class TrackSearcher : PropertyChangedBase
    {
        public string SearchText { get; set; }
        public ObservableCollection<WebTrackResultBase> Results { get; set; }

        private readonly AutoResetEvent _cancelWaiter;
        private bool _isSearching; //Difference between _IsRunning and IsSearching: _IsRunning is also true if pictures are downloading
        private bool _canceled;

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                SetProperty(value, ref _isLoading);
            }
        }

        private bool _nothingFound;
        public bool NothingFound
        {
            get { return _nothingFound; }
            set
            {
                SetProperty(value, ref _nothingFound);
            }
        }

        private WebTrackResultBase _selectedTrack;
        public WebTrackResultBase SelectedTrack
        {
            get { return _selectedTrack; }
            set
            {
                SetProperty(value, ref _selectedTrack);
            }
        }

        private IPlaylistResult _playlistResult;
        public IPlaylistResult PlaylistResult
        {
            get { return _playlistResult; }
            set
            {
                SetProperty(value, ref _playlistResult);
            }
        }
        
        private RelayCommand _searchCommand;
        public RelayCommand SearchCommand
        {
            get
            {
                return _searchCommand ?? (_searchCommand = new RelayCommand(async parameter =>
                {
                    if (string.IsNullOrWhiteSpace(SearchText)) return;
                    IsLoading = true;
                    if (_isSearching)
                    {
                        _canceled = true;
                        await Task.Run(() => _cancelWaiter.WaitOne());
                    }

                    _isSearching = true;
                    PlaylistResult = null;
                    try
                    {
                        await Search();
                    }
                    catch (WebException ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    IsLoading = false;

                    foreach (var track in Results)
                    {
                        await track.DownloadImage();
                        if (CheckForCanceled()) return;
                    }
                    _isSearching = false;
                }));
            }
        }

        private async Task Search()
        {
        }

        private RelayCommand _playSelectedTrack;
        public RelayCommand PlaySelectedTrack
        {
            get
            {
                return _playSelectedTrack ?? (_playSelectedTrack = new RelayCommand(async parameter =>
                {
                    if (SelectedTrack == null) return;
                    IsLoading = true;
                    await _manager.CSCoreEngine.OpenTrack(SelectedTrack.ToPlayable());
                    IsLoading = false;
                    _manager.CSCoreEngine.TogglePlayPause();
                }));
            }
        }

        private RelayCommand _addToPlaylist;
        public RelayCommand AddToPlaylist
        {
            get
            {
                return _addToPlaylist ?? (_addToPlaylist = new RelayCommand(async parameter =>
                {
                    if (parameter == null) return;
                    var playlist = parameter as IPlaylist;

                    IsLoading = true;
                    if (playlist == null)
                    {
                        string result = await _baseWindow.WindowDialogService.ShowInputDialog(Application.Current.Resources["NewPlaylist"].ToString(), Application.Current.Resources["NameOfPlaylist"].ToString(), Application.Current.Resources["Create"].ToString(), "", DialogMode.Single);
                        if (string.IsNullOrEmpty(result))
                        {
                            IsLoading = false;
                            return;
                        }
                        var newPlaylist = new NormalPlaylist() { Name = result };
                        _manager.Playlists.Add(newPlaylist);
                        _manager.RegisterPlaylist(newPlaylist);
                        playlist = newPlaylist;
                    }

                    var track = SelectedTrack.ToPlayable();
                    playlist.AddTrack(track);
                    IsLoading = false;
                    ViewModels.MainViewModel.Instance.MainTabControlIndex = 0;
                    _manager.SelectedPlaylist = playlist;
                    _manager.SelectedTrack = track;
                    _manager.SaveToSettings();
                    AsyncTrackLoader.Instance.RunAsync(playlist);
                    HurricaneSettings.Instance.Save();
                }));
            }
        }
        
        private RelayCommand _addPlaylistToNewPlaylist;
        public RelayCommand AddPlaylistToNewPlaylist
        {
            get
            {
                return _addPlaylistToNewPlaylist ?? (_addPlaylistToNewPlaylist = new RelayCommand(async parameter =>
                {
                    if (PlaylistResult == null) return;
                    string result = await _baseWindow.WindowDialogService.ShowInputDialog(Application.Current.Resources["NewPlaylist"].ToString(), Application.Current.Resources["NameOfPlaylist"].ToString(), Application.Current.Resources["Create"].ToString(), PlaylistResult.Title, DialogMode.Single);
                    if (string.IsNullOrEmpty(result)) return;
                    var playlist = new NormalPlaylist() { Name = result };
                    _manager.Playlists.Add(playlist);
                    _manager.RegisterPlaylist(playlist);

                    if (await AddTracksToPlaylist(playlist, PlaylistResult))
                    {
                        ViewModels.MainViewModel.Instance.MainTabControlIndex = 0;
                        _manager.SelectedPlaylist = playlist;
                    }
                }));
            }
        }

        private RelayCommand _addPlaylistToExisitingPlaylist;
        public RelayCommand AddPlaylistToExisitingPlaylist
        {
            get
            {
                return _addPlaylistToExisitingPlaylist ?? (_addPlaylistToExisitingPlaylist = new RelayCommand(async parameter =>
                {
                    if (PlaylistResult == null) return;
                    var playlist = parameter as NormalPlaylist;
                    if (playlist == null) return;
                    if (await AddTracksToPlaylist(playlist, PlaylistResult))
                    {
                        ViewModels.MainViewModel.Instance.MainTabControlIndex = 0;
                        _manager.SelectedPlaylist = playlist;
                    }
                }));
            }
        }

        private async Task<bool> AddTracksToPlaylist(IPlaylist playlist, IPlaylistResult result)
        {
            await Task.Delay(500);
            var controller = await _baseWindow.ShowProgressAsync(Application.Current.Resources["ImportTracks"].ToString(), string.Empty, true, new MetroDialogSettings { NegativeButtonText = Application.Current.Resources["Cancel"].ToString() });
            result.LoadingTracksProcessChanged += (s, e) =>
            {
                controller.SetMessage(string.Format(Application.Current.Resources["LoadingTracks"].ToString(), e.CurrentTrackName, e.Value, e.Maximum));
                controller.SetProgress(e.Value / e.Maximum);
            };

            var tracks = await result.GetTracks(controller);
            if (tracks == null)
            {
                await controller.CloseAsync();
                return false;
            }

            foreach (var track in tracks)
            {
                playlist.AddTrack(track);
            }
            _manager.SaveToSettings();
            HurricaneSettings.Instance.Save();
            AsyncTrackLoader.Instance.RunAsync(playlist);
            await controller.CloseAsync();
            return true;
        }

        private bool CheckForCanceled()
        {
            if (_canceled)
            {
                _cancelWaiter.Set();
                _canceled = false;
                _isSearching = false;
                return true;
            }
            return false;
        }

        private void SortResults(IEnumerable<WebTrackResultBase> list)
        {
            Results.Clear();
            foreach (var track in list.OrderByDescending(x => x.Views).ToList())
            {
                Results.Add(track);
            }
        }

        private readonly MusicManager _manager;
        private readonly MainWindow _baseWindow;
        public TrackSearcher(MusicManager manager, MainWindow baseWindow)
        {
            Results = new ObservableCollection<WebTrackResultBase>();
            _cancelWaiter = new AutoResetEvent(false);
            _manager = manager;
            _baseWindow = baseWindow;
        }
    }
}