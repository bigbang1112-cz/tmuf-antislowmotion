using BigBang1112.TmufAntiSlowMotion.Data;
using BigBang1112.TmufAntiSlowMotion.Data.Models;
using BigBang1112.TmufAntiSlowMotionLib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TmXmlRpc;

namespace BigBang1112.TmufAntiSlowMotion.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> logger;
        private readonly IMemoryCache cache;

        public Report Report { get; set; }
        public Dictionary<string, ICollection<LoginInfo>> MostAffectedMaps { get; set; }
        public int RecordsRemoved { get; set; }
        public Dictionary<string, Map> UnaffectedMaps { get; set; }
        public int OfficialMapCount { get; set; }
        public Dictionary<string, VoluntaryModel> Voluntary { get; set; }

        public IndexModel(ILogger<IndexModel> logger, IMemoryCache cache)
        {
            this.logger = logger;
            this.cache = cache;
        }

        public IActionResult OnGet(string format)
        {
            Report = cache.Get<Report>(CacheKeys.Report);

            if (format == "json")
            {
                return Content(cache.Get<string>(CacheKeys.ReportJson), "application/json");
            }

            MostAffectedMaps = cache.Get<Dictionary<string, ICollection<LoginInfo>>>(CacheKeys.MostAffectedMaps);
            RecordsRemoved = Math.Abs(cache.Get<int>(CacheKeys.RecordCountDifference));
            UnaffectedMaps = cache.Get<Dictionary<string, Map>>(CacheKeys.UnaffectedMaps);
            OfficialMapCount = cache.Get<int>(CacheKeys.OfficialMapCount);
            Voluntary = cache.Get<Dictionary<string, VoluntaryModel>>(CacheKeys.Voluntary);

            return Page();
        }
    }
}
