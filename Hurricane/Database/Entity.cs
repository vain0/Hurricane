using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hurricane.Database
{
    public static class Entity
    {
        private static HurricaneEntities _instance;
        public static HurricaneEntities Instance
        {
            get { return _instance ?? (_instance = new HurricaneEntities()); }
        }

        public static IQueryable<track> TrackList(this HurricaneEntities self, int playlistId)
        {
            return
                from item in self.playlist_items
                where item.PlaylistId == playlistId
                join track in self.tracks on item.TrackId equals track.Id
                select track;
        }
    }
}
