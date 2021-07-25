using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BigBang1112.TmufAntiSlowMotion.Data.Models
{
    public class VoluntaryModel
    {
        [JsonPropertyName("image")]
        public string Image { get; set; }
    }
}
