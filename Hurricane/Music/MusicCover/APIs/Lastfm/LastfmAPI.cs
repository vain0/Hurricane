using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using Exceptionless.Json;
using Hurricane.Music.Track;
using Hurricane.Settings;
using Hurricane.Utilities;

namespace Hurricane.Music.MusicCover.APIs.Lastfm
{
    class LastfmAPI
    {
        public async static Task<BitmapImage> GetImage(ImageQuality imagequality, bool saveimage, DirectoryInfo directory, PlayableBase track, bool trimtrackname, bool useArtist = true)
        {
            return null;
        }

        protected static string GetImageLink(lfmArtistImage[] image, ImageQuality quality)
        {
            if (image.Length == 1) return image[0].Value;
            switch (quality)
            {
                case ImageQuality.Small:
                    return image.First().Value;
                case ImageQuality.Medium:
                case ImageQuality.Large:
                    var items = image.Where((x) => x.size == (quality == ImageQuality.Large ? "large" : "medium"));
                    if (items.Any()) return items.First().Value;
                    break;
            }
            return image.Last().Value;
        }

        protected static string GetImageLink(lfmTrackAlbumImage[] image, ImageQuality quality)
        {
            if (image.Length == 1) return image[0].Value;
            switch (quality)
            {
                case ImageQuality.Small:
                    return image.First().Value;
                case ImageQuality.Medium:
                case ImageQuality.Large:
                    var items = image.Where((x) => x.size == (quality == ImageQuality.Large ? "large" : "medium"));
                    if (items.Any()) return items.First().Value;
                    break;
            }
            return image.Last().Value;
        }

        protected static string TrimTrackTitle(string title)
        {
            title = title.ToLower();
            title = title.Replace("official music", string.Empty);

            title = title.Trim(new char[] { ' ', '-' });
            if (title.EndsWith("hd")) title = title.Remove(title.Length - 2, 2);
            return Regex.Replace(title, @"[\[\(](?!ft).*[\]\)]", "");
        }

        //David Guetta - Memories (ft. Kid Cudi) [with lyrics]
        //Memories - David guetta feat Kid cudi - Official Music
        //David Guetta ft. Kid Cudi Memories (Not Official) HD
    }
}