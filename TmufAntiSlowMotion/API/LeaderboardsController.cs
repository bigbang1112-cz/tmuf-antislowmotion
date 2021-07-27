using BigBang1112.TmufAntiSlowMotion.Data;
using BigBang1112.TmufAntiSlowMotion.Data.Models;
using BigBang1112.TmufAntiSlowMotionLib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TmEssentials;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BigBang1112.TmufAntiSlowMotion.API
{
    [Route("api/v1/leaderboards")]
    [ApiController]
    public class LeaderboardsController : ControllerBase
    {
        private readonly ILogger<LeaderboardsController> logger;
        private readonly IMemoryCache cache;

        public LeaderboardsController(ILogger<LeaderboardsController> logger, IMemoryCache cache)
        {
            this.logger = logger;
            this.cache = cache;
        }

        [HttpGet("map/before/{mapUid}")]
        public ActionResult<IEnumerable<RecordModel>> GetMapLeaderboardsBefore(string mapUid)
        {
            var report = cache.Get<Report>(CacheKeys.Report);

            if (!report.Maps.TryGetValue(mapUid, out Map map))
                return NotFound();

            return Ok(map.PrevLb.Select(x => AdjustedRecord(x)));
        }

        [HttpGet("map/after/{mapUid}")]
        public ActionResult<IEnumerable<RecordModel>> GetMapLeaderboardsAfter(string mapUid)
        {
            var report = cache.Get<Report>(CacheKeys.Report);

            if (!report.Maps.TryGetValue(mapUid, out Map map))
                return NotFound();

            return Ok(map.CurLb.Select(x => AdjustedRecord(x)));
        }

        private static RecordModel AdjustedRecord(Record record)
        {
            return new RecordModel
            {
                Rank = record.Rank,
                Time = TimeSpan.FromMilliseconds(record.Time).ToStringTm(true),
                TimeRaw = record.Time,
                Login = record.Login,
                Nickname = Formatter.Deformat(record.Nickname)
            };
        }
    }
}
