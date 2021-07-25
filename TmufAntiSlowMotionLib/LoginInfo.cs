using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigBang1112.TmufAntiSlowMotionLib
{
    public struct LoginInfo
    {
        public string Login { get; set; }
        public string Nickname { get; set; }

        public LoginInfo(string login, string nickname)
        {
            Login = login;
            Nickname = nickname;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Equals(LoginInfo login)
        {
            return login.Login == Login;
        }

        public override bool Equals(object obj)
        {
            return Equals((LoginInfo)obj);
        }

        public override string ToString()
        {
            return Login;
        }
    }
}
