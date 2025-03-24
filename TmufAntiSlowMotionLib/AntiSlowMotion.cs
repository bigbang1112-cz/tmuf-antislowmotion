using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ManiaAPI.XmlRpc.TMUF;

namespace BigBang1112.TmufAntiSlowMotionLib
{
    // This is TERRIBLENESS from 2021 migrated from TmXmlRpc to ManiaAPI.XmlRpc
    // Don't take this as a prime example of how to use ManiaAPI.XmlRpc PLEASE KEKW

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
                .SelectMany(x => x.Value.ChallengeScores["World"].HighScores)
                .Select(x => x.Login)
                .ToHashSet();

            var pLogins = before.SelectMany(x => x.Value.Maps)
                .SelectMany(x => x.Value.ChallengeScores["World"].HighScores)
                .Select(x => x.Login)
                .ToHashSet();

            return pLogins.Except(cLogins).ToHashSet();
        }

        public static int GetRecordCountDifference(Dictionary<string, CampaignScores> before,
            Dictionary<string, CampaignScores> after)
        {
            var numberOfPrevRecords = before.SelectMany(x => x.Value.Maps)
                .Select(x => x.Value.ChallengeScores["World"].Skillpoints.Sum(x => x.Count)).Sum();

            var numberOfCurrentRecords = after.SelectMany(x => x.Value.Maps)
                .Select(x => x.Value.ChallengeScores["World"].Skillpoints.Sum(x => x.Count)).Sum();

            return numberOfCurrentRecords - numberOfPrevRecords;
        }

        public static Dictionary<LoginInfo, int> GetRecordCountDifferenceByLogin(
            Dictionary<LoginInfo, IEnumerable<Leaderboard>> ownersBefore,
            Dictionary<LoginInfo, IEnumerable<Leaderboard>> ownersAfter)
        {
            var changedRecords = new Dictionary<LoginInfo, int>();

            foreach (var prevRec in ownersBefore)
            {
                var pR = prevRec.Value.Count();

                if (ownersAfter.TryGetValue(prevRec.Key, out IEnumerable<Leaderboard> curRecs))
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

        public static Dictionary<LoginInfo, IEnumerable<(string, CampaignScoresLeaderboard)>> GetRecordOwners(Dictionary<string, CampaignScores> leaderboard)
        {
            return leaderboard.SelectMany(x => x.Value.Maps)
                .SelectMany(loginMapPair =>
                    loginMapPair.Value.ChallengeScores["World"].HighScores
                        .Select(record => (loginMapPair, record))
                    )
                    .Select(mapRecordPair => (
                        new LoginInfo(mapRecordPair.record.Login, mapRecordPair.record.Nickname),
                        (mapRecordPair.loginMapPair.Key, mapRecordPair.loginMapPair.Value))
                        )
                    .GroupBy(mapRecordPair => mapRecordPair.Item1)
                    .ToDictionary(x => x.Key, x => x.Select(x => x.Item2));
        }

        public static Report GetReport(
            Dictionary<string, CampaignScores> before,
            Dictionary<string, CampaignScores> after,
            Dictionary<LoginInfo, IEnumerable<(string, CampaignScoresLeaderboard)>> ownersBefore,
            Dictionary<LoginInfo, IEnumerable<(string, CampaignScoresLeaderboard)>> ownersAfter,
            string mapsJsonFile)
        {
            var maps = new Dictionary<string, Map>();
            var affected = new Dictionary<string, AffectedLogin>();

            foreach (var prevRec in ownersBefore)
            {
                var login = prevRec.Key;
                var prevRecs = prevRec.Value;
                var prevRecsCount = prevRecs.Count();

                AffectedLogin affectedLogin;

                if (ownersAfter.TryGetValue(login, out IEnumerable<(string, CampaignScoresLeaderboard)> curRecs))
                {
                    var curRecsCount = curRecs.Count();

                    var differentRecs = prevRecs.ExceptBy(curRecs.Select(x => x.Item1), x => x.Item1);

                    affectedLogin = AssignAffectedLogin(before, after, maps, differentRecs, login);
                }
                else
                {
                    affectedLogin = AssignAffectedLogin(before, after, maps, prevRecs, login);
                }

                if (affectedLogin is not null)
                {
                    affected.Add(login.Login, affectedLogin);

                    affectedLogin.Previous = prevRecs.Select(x => x.Item1).ToList();
                }
            }

            var unaffectedMaps = new Dictionary<string, Map>();

            foreach (var indifferentMap in affected.Values.SelectMany(x => x.Previous))
            {
                if (!unaffectedMaps.ContainsKey(indifferentMap))
                {
                    unaffectedMaps.Add(indifferentMap, new Map
                    {
                        CurLb = GetMapRecord(before, indifferentMap),
                        PrevLb = GetMapRecord(after, indifferentMap)
                    });
                }
            }

            ScanMapDetailsWithJsonFile(maps, mapsJsonFile);
            ScanMapDetailsWithJsonFile(unaffectedMaps, mapsJsonFile);

            return new Report
            {
                Maps = maps,
                AffectedLogins = affected,
                UnaffectedMaps = unaffectedMaps
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

        public static IEnumerable<KeyValuePair<string, CampaignScoresLeaderboard>> GetAllOfficialMaps(
            Dictionary<string, CampaignScores> any)
        {
            return any.SelectMany(x => x.Value.Maps);
        }

        public static IEnumerable<KeyValuePair<string, CampaignScoresLeaderboard>> GetAllOfficialMapsAsDictionary(
            Dictionary<string, CampaignScores> any)
        {
            return GetAllOfficialMaps(any).ToDictionary(x => x.Key, x => x.Value);
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

        private static AffectedLogin AssignAffectedLogin(Dictionary<string, CampaignScores> before,
            Dictionary<string, CampaignScores> after, Dictionary<string, Map> maps,
            IEnumerable<(string, CampaignScoresLeaderboard)> differentRecs, LoginInfo login)
        {
            var mapUidList = new List<string>();

            if (!differentRecs.Any())
                return null;

            foreach (var (mapUid, _) in differentRecs)
            {
                if (!maps.ContainsKey(mapUid))
                {
                    maps.Add(mapUid, new Map
                    {
                        CurLb = GetMapRecord(after, mapUid),
                        PrevLb = GetMapRecord(before, mapUid)
                    });
                }

                mapUidList.Add(mapUid);
            }

            return new AffectedLogin
            {
                Nickname = login.Nickname,
                Changes = mapUidList
            };
        }

        private static IEnumerable<Record> GetMapRecord(Dictionary<string, CampaignScores> scores, string mapUid)
        {
            return scores
                .SelectMany(x => x.Value.Maps)
                .FirstOrDefault(x => x.Key == mapUid)
                .Value.ChallengeScores["World"].HighScores.Select(rec => new Record
                {
                    Login = rec.Login,
                    Nickname = rec.Nickname,
                    Rank = rec.Rank,
                    Time = TmEssentials.TimeSpanExtensions.ToMilliseconds(rec.Time)
                });
        }
    }
}
