﻿using System;
using System.Reflection;
using System.Windows;
using Hurricane.Music.AudioEngine;
using Hurricane.Music.CustomEventArgs;
using Hurricane.Music.Track;
using Hurricane.Notification.Views;
using Hurricane.Settings;
using Hurricane.Utilities;
using Hurricane.Utilities.Native;

namespace Hurricane.Notification
{
    public class NotificationService
    {
        public NotificationService(CSCoreEngine cscore)
        {
            cscore.TrackChanged += cscore_TrackChanged;
        }

        void cscore_TrackChanged(object sender, TrackChangedEventArgs e)
        {
            TrackChanged(e.NewTrack);
        }

        private Window _lastwindow;
        private PlayableBase _lasttrack;

        void TrackChanged(PlayableBase newtrack)
        {
            ConfigSettings config = HurricaneSettings.Instance.Config;
            if (config.DisableNotificationInGame && WindowHelper.WindowIsFullscreen(UnsafeNativeMethods.GetForegroundWindow())) return;
            ShowNotification(newtrack, config.Notification);

            _lasttrack = newtrack;
        }

        protected void ShowNotification(PlayableBase track, NotificationType type)
        {
            ConfigSettings config = HurricaneSettings.Instance.Config;
            if (config.Notification == NotificationType.None) return;
            if (_lastwindow != null && _lastwindow.Visibility == Visibility.Visible) _lastwindow.Close();
            TimeSpan timetostayopen = TimeSpan.FromMilliseconds(config.NotificationShowTime);

            Window messagewindow;
            switch (type)
            {
                case NotificationType.Top:
                    messagewindow = new NotificationTopWindow(track, timetostayopen);
                    break;
                case NotificationType.RightBottom:
                    messagewindow = new NotificationRightBottomWindow(track, timetostayopen);
                    break;
                default:
                    return;
            }
            messagewindow.Show();
            _lastwindow = messagewindow;
        }

        public void Test(NotificationType type)
        {
            PlayableBase trackToUse = _lasttrack ?? new LocalTrack(Assembly.GetExecutingAssembly().Location)
            {
                Artist = "Alkaline",
                Title = "Sample Track",
                Duration = "03:26",
                kHz = 44
            };
            ShowNotification(trackToUse, type);
        }
    }

    public enum NotificationType
    {
        None,
        Top,
        RightBottom
    }
}