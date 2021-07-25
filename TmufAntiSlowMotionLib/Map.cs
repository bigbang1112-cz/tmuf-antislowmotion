using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigBang1112.TmufAntiSlowMotionLib
{
    public class Map
    {
        public string MapName { get; set; }
        public IEnumerable<Record> PrevLb { get; set; }
        public IEnumerable<Record> CurLb { get; set; }

        public override string ToString()
        {
            return MapName;
        }
    }
}
