using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Hurricane.Music.Track
{
    public interface IMusicInformation
    {
        string Title { get; set; }
        TimeSpan Duration { get; set; }
        string Artist { get; set; }
        Task<BitmapImage> GetImage();
        uint Year { get; set; }
        string Album { get; set; }
    }
}