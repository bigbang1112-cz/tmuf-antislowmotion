using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigBang1112.TmufAntiSlowMotionLib
{
    public class Report
    {
        /// <summary>
        /// Key: map uid
        /// </summary>
        public Dictionary<string, Map> Maps { get; set; }
        /// <summary>
        /// Key: login
        /// </summary>
        public Dictionary<string, AffectedLogin> AffectedLogins { get; set; }
        public Dictionary<string, Map> UnaffectedMaps { get; set; }

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

        public Dictionary<string, Map> GetRemaining(string login)
        {
            var dictionary = new Dictionary<string, Map>();

            if (!AffectedLogins.TryGetValue(login, out AffectedLogin affected))
                return dictionary;

            foreach (var mapUid in affected.Previous.Except(affected.Changes))
            {
                Maps.TryGetValue(mapUid, out Map map);
                UnaffectedMaps.TryGetValue(mapUid, out map);
                dictionary.Add(mapUid, map);
            }

            return dictionary;
        }
    }
}
