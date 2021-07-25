using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigBang1112.TmufAntiSlowMotionLib
{
    public class AffectedLogin
    {
        public string Nickname { get; set; }
        public ICollection<string> Changes { get; set; }

        public override string ToString()
        {
            return Nickname + ": " + Changes.Count + " changes";
        }
    }
}
