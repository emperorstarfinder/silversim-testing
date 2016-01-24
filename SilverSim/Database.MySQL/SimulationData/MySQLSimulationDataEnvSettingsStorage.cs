﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.WindLight;
using SilverSim.Types;
using System.Collections.Generic;
using System.IO;

namespace SilverSim.Database.MySQL.SimulationData
{
    public class MySQLSimulationDataEnvSettingsStorage : SimulationDataEnvSettingsStorageInterface
    {
#if DEBUG
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL ENVIRONMENT SETTINGS SERVICE");
#endif

        readonly string m_ConnectionString;

        public MySQLSimulationDataEnvSettingsStorage(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public override bool TryGetValue(UUID regionID, out EnvironmentSettings settings)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT EnvironmentSettings FROM environmentsettings WHERE RegionID LIKE ?regionid", conn))
                {
                    cmd.Parameters.AddParameter("?regionid", regionID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            using (MemoryStream ms = new MemoryStream(reader.GetBytes("EnvironmentSettings")))
                            {
                                settings = EnvironmentSettings.Deserialize(ms);
                                return true;
                            }
                        }
                    }
                }
            }
            settings = null;
            return false;
        }

        /* setting value to null will delete the entry */
        public override EnvironmentSettings this[UUID regionID]
        {
            get
            {
                EnvironmentSettings settings;
                if (!TryGetValue(regionID, out settings))
                {
                    throw new KeyNotFoundException();
                }
                return settings;
            }
            set
            {
                using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    if(value == null)
                    {
#if DEBUG
                        m_Log.DebugFormat("Removing environment settings for {0}", regionID.ToString());
#endif
                        using (MySqlCommand cmd = new MySqlCommand("DELETE FROM environmentsettings WHERE RegionID LIKE ?regionid", conn))
                        {
                            cmd.Parameters.AddParameter("?regionid", regionID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
#if DEBUG
                        m_Log.DebugFormat("Storing new environment settings for {0}", regionID.ToString());
#endif
                        Dictionary<string, object> param = new Dictionary<string,object>();
                        param["RegionID"] = regionID;
                        using(MemoryStream ms = new MemoryStream())
                        {
                            value.Serialize(ms, regionID);
                            param["EnvironmentSettings"] = ms.GetBuffer();
                        }
                        conn.ReplaceInto("environmentsettings", param);
                    }
                }
            }
        }

        public override bool Remove(UUID regionID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM environmentsettings WHERE RegionID LIKE ?regionid", conn))
                {
                    cmd.Parameters.AddParameter("?regionid", regionID);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
