using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigBang1112.TmufAntiSlowMotionLib
{
    public class Report
    {
        public Dictionary<string, Map> Maps { get; set; }
        public Dictionary<string, AffectedLogin> AffectedLogins { get; set; }

        public Dictionary<string, Map> GetChanges(string login)
        {
            var dictionary = new Dictionary<string, Map>();

            if (!AffectedLogins.TryGetValue(login, out AffectedLogin affected))
                return dictionary;

            foreach (var mapUid in affected.Changes)
            {
                dictionary.Add(mapUid, Maps[mapUid]);
            }

            return dictionary;
        }

        public Map GetChange(string login, string mapUid)
        {
            if (!AffectedLogins.TryGetValue(login, out AffectedLogin affected))
                return null;

            if (!affected.Changes.Contains(mapUid))
                return null;

            Maps.TryGetValue(mapUid, out Map map);

            return map;
        }
    }
}
