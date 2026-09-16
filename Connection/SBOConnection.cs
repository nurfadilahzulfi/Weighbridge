using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Weighbridge.Core.Config;
using Weighbridge.Core.Encryptions;

namespace Weighbridge
{
    public class SBOConnection
    {
        public Company oCompany = new Company();

        public void connectSBO(ModGlobal modGlobal)
        {
            if (!oCompany.Connected)
            {
                oCompany = new Company();
                //oCompany.LicenseServer = "";
                oCompany.Server = modGlobal.serverName;
                oCompany.DbServerType = BoDataServerTypes.dst_HANADB;
                oCompany.CompanyDB = modGlobal.database;
                oCompany.DbUserName = modGlobal.databaseUsername;
                oCompany.DbPassword = modGlobal.databasePassword;
                oCompany.UserName = modGlobal.sboUsername;
                oCompany.Password = modGlobal.sboPassword;
                //oCompany.language = BoSuppLangs.ln_English;

                if (oCompany.Connect() != 0)
                {
                    throw new Exception("Error connection : " + oCompany.GetLastErrorCode().ToString() + " - " + oCompany.GetLastErrorDescription());
                }
            }
        }

        public void connectSBOS(SBOConfig config)
        {
            if (!oCompany.Connected)
            {
                oCompany = new Company();
                //oCompany.LicenseServer = "";
                oCompany.Server = config.serverName;
                oCompany.DbServerType = BoDataServerTypes.dst_HANADB;
                oCompany.CompanyDB = config.database;
                oCompany.DbUserName = config.databaseUsername;
                oCompany.DbPassword = config.databasePassword;
                oCompany.UserName = config.sboUsername;
                oCompany.Password = config.sboPassword;
                //oCompany.language = BoSuppLangs.ln_English;

                if (oCompany.Connect() != 0)
                {
                    throw new Exception("Error connection : " + oCompany.GetLastErrorCode().ToString() + " - " + oCompany.GetLastErrorDescription());
                }
            }
        }

        public bool connectionSBO(ModGlobal modGlobal)
        {
            if (!oCompany.Connected)
            {
                oCompany = new Company();
                //oCompany.LicenseServer = "";
                oCompany.Server = modGlobal.serverName;
                oCompany.DbServerType = BoDataServerTypes.dst_HANADB;
                oCompany.CompanyDB = modGlobal.database;
                oCompany.DbUserName = modGlobal.databaseUsername;
                oCompany.DbPassword = modGlobal.databasePassword;
                oCompany.UserName = modGlobal.sboUsername;
                oCompany.Password = modGlobal.sboPassword;
                //oCompany.language = BoSuppLangs.ln_English;

                if (oCompany.Connect() != 0)
                {
                    //return false;
                    throw new Exception("Error connection : " + oCompany.GetLastErrorCode().ToString() + " - " + oCompany.GetLastErrorDescription());
                }
            }

            return true;
        }

        public bool connectionSBOS(SBOConfig config)
        {
            Encryption encryption = new Encryption();
            if (!oCompany.Connected)
            {
                oCompany = new Company();
                //oCompany.LicenseServer = "";
                oCompany.Server = config.serverName;
                oCompany.DbServerType = BoDataServerTypes.dst_HANADB;
                oCompany.CompanyDB = config.database;
                oCompany.DbUserName = config.databaseUsername;
                oCompany.DbPassword = encryption.decrypt(config.databasePassword);
                oCompany.UserName = config.sboUsername;
                oCompany.Password = encryption.decrypt(config.sboPassword);
                //oCompany.language = BoSuppLangs.ln_English;

                if (oCompany.Connect() != 0)
                {
                    //return false;
                    throw new Exception("Error connection : " + oCompany.GetLastErrorCode().ToString() + " - " + oCompany.GetLastErrorDescription());
                }
            }

            return true;
        }
    }
}
