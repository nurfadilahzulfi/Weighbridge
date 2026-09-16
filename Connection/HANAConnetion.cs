using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Weighbridge.Core.Config;

namespace Weighbridge.Connection
{
    public class HANAConnetion
    {
        public string HanaConnectionString(ModGlobal modGlobal)
        {
            string server = modGlobal.serverName ?? "";
            if (server.Contains("@"))
            {
                server = server.Split('@')[1];
            }

            string host = server;
            string port = "30015";

            if (server.Contains(":"))
            {
                var parts = server.Split(':');
                host = parts[0];
                string configPort = parts[1];
                port = (configPort == "30013") ? "30015" : configPort;
            }

            string hanaServer = host + ":" + port;
            string connectionString = "server=" + hanaServer + ";currentSchema=" + modGlobal.database + ";userid=" + modGlobal.databaseUsername + ";password=" + modGlobal.databasePassword; 

            return connectionString;
        }

        public string HanaConnectionStrings(SBOConfig sboConfig)
        {
            string server = sboConfig.serverName ?? "";
            if (server.Contains("@"))
            {
                server = server.Split('@')[1];
            }

            string host = server;
            string port = "30015";

            if (server.Contains(":"))
            {
                var parts = server.Split(':');
                host = parts[0];
                string configPort = parts[1];
                port = (configPort == "30013") ? "30015" : configPort;
            }

            string hanaServer = host + ":" + port;
            string connectionString = "server=" + hanaServer + ";currentSchema=" + sboConfig.database + ";userid=" + sboConfig.databaseUsername + ";password=" + sboConfig.databasePassword;

            return connectionString;
        }
    }
}
