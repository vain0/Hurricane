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
    }
}
