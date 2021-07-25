using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TmEssentials;

namespace BigBang1112.TmufAntiSlowMotionLib
{
    public struct Record
    {
        public int Rank { get; set; }
        public int Time { get; set; }
        public string Login { get; set; }
        public string Nickname { get; set; }

        public override string ToString()
        {
            var time = TimeSpan.FromMilliseconds(Time);
            if (Nickname == null)
                return time.ToStringTm();
            return $"{time.ToStringTm()} by {Formatter.Deformat(Nickname)}";
        }
    }
}
