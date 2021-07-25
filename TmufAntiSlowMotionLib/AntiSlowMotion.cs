using GBX.NET;
using GBX.NET.Engines.Game;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TmXmlRpc;

namespace BigBang1112.TmufAntiSlowMotionLib
{
    public static class AntiSlowMotion
    {
        public static Dictionary<string, CampaignScores> ParseLeaderboard(string filePath)
        {
            using var fs = File.OpenRead(filePath);
            using var zip = new ZipArchive(fs);

            var scoreDictionary = new Dictionary<string, CampaignScores>();

            foreach (var entry in zip.Entries)
            {
                using var stream = entry.Open();
                scoreDictionary.Add(entry.Name, CampaignScores.Parse(stream));
            }

            return scoreDictionary;
        }

        public static HashSet<string> GetMissingLogins(Dictionary<string, CampaignScores> before,
            Dictionary<string, CampaignScores> after)
        {
            var cLogins = after.SelectMany(x => x.Value.Maps)
                .SelectMany(x => x.Value.Zones["World"].Records)
                .Select(x => x.Login)
                .ToHashSet();

            var pLogins = before.SelectMany(x => x.Value.Maps)
                .SelectMany(x => x.Value.Zones["World"].Records)
                .Select(x => x.Login)
                .ToHashSet();

            return pLogins.Except(cLogins).ToHashSet();
        }

        public static int GetRecordCountDifference(Dictionary<string, CampaignScores> before,
            Dictionary<string, CampaignScores> after)
        {
            var numberOfPrevRecords = before.SelectMany(x => x.Value.Maps)
                .Select(x => x.Value.Zones["World"].TotalCount).Sum();

            var numberOfCurrentRecords = after.SelectMany(x => x.Value.Maps)
                .Select(x => x.Value.Zones["World"].TotalCount).Sum();

            return numberOfCurrentRecords - numberOfPrevRecords;
        }

        public static Dictionary<LoginInfo, int> GetRecordCountDifferenceByLogin(
            Dictionary<LoginInfo, IEnumerable<CampaignScoresMap>> ownersBefore,
            Dictionary<LoginInfo, IEnumerable<CampaignScoresMap>> ownersAfter)
        {
            var changedRecords = new Dictionary<LoginInfo, int>();

            foreach (var prevRec in ownersBefore)
            {
                var pR = prevRec.Value.Count();

                if (ownersAfter.TryGetValue(prevRec.Key, out IEnumerable<CampaignScoresMap> curRecs))
                {
                    var cR = curRecs.Count();

                    if (cR - pR < 0)
                        changedRecords.Add(prevRec.Key, cR - pR);
                }
                else
                {
                    if (0 - pR < 0)
                        changedRecords.Add(prevRec.Key, 0 - pR);
                }
            }

            return changedRecords;
        }

        public static Dictionary<LoginInfo, IEnumerable<CampaignScoresMap>> GetRecordOwners(Dictionary<string, CampaignScores> leaderboard)
        {
            return leaderboard.SelectMany(x => x.Value.Maps)
                .SelectMany(loginMapPair =>
                    loginMapPair.Value.Zones["World"].Records
                        .Select(record => (loginMapPair.Value, record))
                    )
                    .Select(mapRecordPair => (
                        new LoginInfo(mapRecordPair.record.Login, mapRecordPair.record.Nickname),
                        mapRecordPair.Value)
                        )
                    .GroupBy(mapRecordPair => mapRecordPair.Item1)
                    .ToDictionary(x => x.Key, x => x.Select(x => x.Value));
        }

        public static Report GetReport(
            Dictionary<string, CampaignScores> before,
            Dictionary<string, CampaignScores> after,
            Dictionary<LoginInfo, IEnumerable<CampaignScoresMap>> ownersBefore,
            Dictionary<LoginInfo, IEnumerable<CampaignScoresMap>> ownersAfter,
            string mapsJsonFile)
        {
            var maps = new Dictionary<string, Map>();
            var affected = new Dictionary<string, AffectedLogin>();

            foreach (var prevRec in ownersBefore)
            {
                var login = prevRec.Key;
                var prevRecs = prevRec.Value;
                var prevRecsCount = prevRecs.Count();

                if (ownersAfter.TryGetValue(login, out IEnumerable<CampaignScoresMap> curRecs))
                {
                    var curRecsCount = curRecs.Count();

                    var differentRecs = prevRecs.Except(curRecs);

                    var affectedLogin = AssignAffectedLogin(before, after, maps, differentRecs, login);
                    if (affectedLogin is not null)
                        affected.Add(login.Login, affectedLogin);
                }
                else
                {
                    var affectedLogin = AssignAffectedLogin(before, after, maps, prevRecs, login);
                    if (affectedLogin is not null)
                        affected.Add(login.Login, affectedLogin);
                }
            }

            ScanMapDetailsWithJsonFile(maps, mapsJsonFile);

            return new Report
            {
                Maps = maps,
                AffectedLogins = affected
            };
        }

        public static Dictionary<string, ICollection<LoginInfo>> GetMostAffectedMaps(Report report)
        {
            var maps = new Dictionary<string, ICollection<LoginInfo>>();

            foreach(var login in report.AffectedLogins)
            {
                foreach (var change in login.Value.Changes)
                {
                    if (maps.ContainsKey(change))
                    {
                        maps[change].Add(new LoginInfo(login.Key, login.Value.Nickname));
                    }
                    else
                    {
                        maps.Add(change, new List<LoginInfo>
                        {
                            new LoginInfo(login.Key, login.Value.Nickname)
                        });
                    }
                }
            }

            return maps;
        }

        public static Dictionary<string, Map> GetUnaffectedMaps(Dictionary<string, CampaignScores> before, Dictionary<string, CampaignScores> after, Report report, string mapsJsonFile)
        {
            var unaffectedMaps = new Dictionary<string, Map>();
            var officialMaps = before.SelectMany(x => x.Value.Maps);

            foreach (var map in officialMaps)
            {
                if (!report.Maps.ContainsKey(map.Key))
                    unaffectedMaps.Add(map.Key, new Map
                    {
                        CurLb = GetMapRecord(before, map.Key),
                        PrevLb = GetMapRecord(after, map.Key)
                    });
            }

            ScanMapDetailsWithJsonFile(unaffectedMaps, mapsJsonFile);

            return unaffectedMaps;
        }

        public static IEnumerable<KeyValuePair<string, CampaignScoresMap>> GetAllOfficialMaps(
            Dictionary<string, CampaignScores> any)
        {
            return any.SelectMany(x => x.Value.Maps);
        }

        public static IEnumerable<KeyValuePair<string, CampaignScoresMap>> GetAllOfficialMapsAsDictionary(
            Dictionary<string, CampaignScores> any)
        {
            return GetAllOfficialMaps(any).ToDictionary(x => x.Key, x => x.Value);
        }

        public static void ScanMapDetailsWithGbxFolder(Dictionary<string, Map> maps, string folderPath)
        {
            foreach (var file in Directory.GetFiles(folderPath, "*.gbx", SearchOption.AllDirectories))
            {
                var node = GameBox.ParseNodeHeader(file);

                if (node is CGameCtnChallenge challenge)
                {
                    if (maps.TryGetValue(challenge.MapUid, out Map map))
                    {
                        map.MapName = TmEssentials.Formatter.Deformat(challenge.MapName);
                    }
                }
            }
        }

        public static void ScanMapDetailsWithJsonFile(Dictionary<string, Map> maps, string filePath)
        {
            var mapInfos = JsonConvert.DeserializeObject<Dictionary<string, MapInfo>>(File.ReadAllText(filePath));

            foreach (var map in maps)
            {
                if (mapInfos.TryGetValue(map.Key, out MapInfo mapInfo))
                {
                    maps[map.Key].MapName = mapInfo.Name;
                }
            }
        }

        public static Dictionary<string, MapInfo> CreateDetailedMaps(string folderPath)
        {
            var maps = new Dictionary<string, MapInfo>();

            foreach (var file in Directory.GetFiles(folderPath, "*.gbx", SearchOption.AllDirectories))
            {
                var node = GameBox.ParseNodeHeader(file);

                if (node is CGameCtnChallenge challenge)
                {
                    maps.Add(challenge.MapUid, new MapInfo
                    {
                        Name = challenge.MapName
                    });
                }
            }

            return maps;
        }

        private static AffectedLogin AssignAffectedLogin(Dictionary<string, CampaignScores> before,
            Dictionary<string, CampaignScores> after, Dictionary<string, Map> maps,
            IEnumerable<CampaignScoresMap> differentRecs, LoginInfo login)
        {
            var mapUidList = new List<string>();

            if (differentRecs.Count() <= 0)
                return null;

            foreach (var map in differentRecs)
            {
                if (!maps.ContainsKey(map.MapUid))
                {
                    maps.Add(map.MapUid, new Map
                    {
                        CurLb = GetMapRecord(after, map),
                        PrevLb = GetMapRecord(before, map)
                    });
                }

                mapUidList.Add(map.MapUid);
            }

            return new AffectedLogin
            {
                Nickname = login.Nickname,
                Changes = mapUidList
            };
        }

        private static IEnumerable<Record> GetMapRecord(Dictionary<string, CampaignScores> scores, CampaignScoresMap map)
        {
            return GetMapRecord(scores, map.MapUid);
        }

        private static IEnumerable<Record> GetMapRecord(Dictionary<string, CampaignScores> scores, string mapUid)
        {
            return scores
                .SelectMany(x => x.Value.Maps)
                .FirstOrDefault(x => x.Value.MapUid == mapUid)
                .Value.Zones["World"].Records.Select(rec => new Record
                {
                    Login = rec.Login,
                    Nickname = rec.Nickname,
                    Rank = rec.Rank,
                    Time = TmEssentials.TimeSpanExtensions.ToMilliseconds(rec.Time.GetValueOrDefault())
                });
        }
    }
}
