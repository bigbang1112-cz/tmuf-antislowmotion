using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigBang1112.TmufAntiSlowMotion.Data;
using BigBang1112.TmufAntiSlowMotionLib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TmEssentials;
using TmXmlRpc;

namespace BigBang1112.TmufAntiSlowMotion.Pages
{
    public class Leaderboards2008Model : PageModel
    {
        private readonly ILogger<IndexModel> logger;
        private readonly IMemoryCache cache;

        public Dictionary<string, CampaignScores> Leaderboards { get; set; }
        public Dictionary<string, MapInfo> MapInfos { get; set; }

        public Leaderboards2008Model(ILogger<IndexModel> logger, IMemoryCache cache)
        {
            this.logger = logger;
            this.cache = cache;
        }

        public void OnGet()
        {
            Leaderboards = cache.Get<Dictionary<string, CampaignScores>>(CacheKeys.Leaderboard2008);
            MapInfos = cache.Get<Dictionary<string, MapInfo>>(CacheKeys.MapInfos);
        }
    }
}
