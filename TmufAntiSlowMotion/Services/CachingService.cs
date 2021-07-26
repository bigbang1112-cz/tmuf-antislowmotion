using BigBang1112.TmufAntiSlowMotion.Data;
using BigBang1112.TmufAntiSlowMotion.Data.Models;
using BigBang1112.TmufAntiSlowMotionLib;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TmXmlRpc;

namespace BigBang1112.TmufAntiSlowMotion.Services
{
    public class CachingService : IHostedService
    {
        private readonly ILogger<CachingService> logger;
        private readonly IMemoryCache cache;
        private readonly string mapsJson = "wwwroot/maps.json";

        public CachingService(ILogger<CachingService> logger, IMemoryCache cache)
        {
            this.logger = logger;
            this.cache = cache;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var before = cache.GetOrCreate(CacheKeys.LeaderboardBefore, entry =>
            {
                logger.LogInformation("Parsing before.zip ...");
                return AntiSlowMotion.ParseLeaderboard("wwwroot/leaderboards/before.zip");
            });

            var after = cache.GetOrCreate(CacheKeys.LeaderboardAfter, entry =>
            {
                logger.LogInformation("Parsing after.zip ...");
                return AntiSlowMotion.ParseLeaderboard("wwwroot/leaderboards/after.zip");
            });

            cache.GetOrCreate(CacheKeys.RecordCountDifference, entry =>
            {
                logger.LogInformation("Getting record owners (before) ...");
                return AntiSlowMotion.GetRecordCountDifference(before, after);
            });

            var recordOwnersBefore = cache.GetOrCreate(CacheKeys.RecordOwnersBefore, entry =>
            {
                logger.LogInformation("Getting record owners (before) ...");
                return AntiSlowMotion.GetRecordOwners(before);
            });

            var recordOwnersAfter = cache.GetOrCreate(CacheKeys.RecordOwnersAfter, entry =>
            {
                logger.LogInformation("Getting record owners (after) ...");
                return AntiSlowMotion.GetRecordOwners(after);
            });

            var report = cache.GetOrCreate(CacheKeys.Report, entry =>
            {
                logger.LogInformation("Creating the report ...");
                return AntiSlowMotion.GetReport(before, after, recordOwnersBefore, recordOwnersAfter, mapsJson);
            });

            cache.GetOrCreate(CacheKeys.MostAffectedMaps, entry =>
            {
                logger.LogInformation("Creating most affected maps ...");
                return AntiSlowMotion.GetMostAffectedMaps(report);
            });

            cache.GetOrCreate(CacheKeys.UnaffectedMaps, entry =>
            {
                logger.LogInformation("Creating unaffected maps ...");
                return AntiSlowMotion.GetUnaffectedMaps(before, after, report, mapsJson);
            });

            cache.GetOrCreate(CacheKeys.OfficialMapCount, entry =>
            {
                logger.LogInformation("Extracting official map count ...");
                return AntiSlowMotion.GetAllOfficialMaps(before).Count();
            });

            cache.GetOrCreate(CacheKeys.Leaderboard2008, entry =>
            {
                logger.LogInformation("Extracting 2008 leaderboards ...");
                return AntiSlowMotion.ParseLeaderboard("wwwroot/leaderboards/2008.zip");
            });

            cache.GetOrCreate(CacheKeys.ReportJson, entry =>
            {
                logger.LogInformation("Serializing the report into JSON ...");
                return JsonSerializer.Serialize(report);
            });

            cache.GetOrCreate(CacheKeys.MapInfos, entry =>
            {
                logger.LogInformation("Parsing maps.json ...");
                return JsonSerializer.Deserialize<Dictionary<string, MapInfo>>(File.ReadAllText("wwwroot/maps.json"));
            });

            cache.GetOrCreate(CacheKeys.Voluntary, entry =>
            {
                logger.LogInformation("Parsing voluntary.json ...");
                return JsonSerializer.Deserialize<Dictionary<string, VoluntaryModel>>(File.ReadAllText("wwwroot/voluntary.json"));
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
