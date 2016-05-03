using Hurricane.Music.Download;

namespace Hurricane.Music.Track
{
    public interface IDownloadable
    {
        string DownloadParameter { get; }
        string DownloadFilename { get; }
        bool CanDownload { get; }
    }
}
