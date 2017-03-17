﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Database.MySQL._Migration;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.Types;
using SilverSim.Types.Presence;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.Presence
{
    #region Service Implementation
    [Description("MySQL Presence Backend")]
    public sealed class MySQLPresenceService : PresenceServiceInterface, IDBServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL PRESENCE SERVICE");

        #region Constructor
        public MySQLPresenceService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
        #endregion

        public void VerifyConnection()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        public void ProcessMigrations()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.MigrateTables(Migrations, m_Log);
            }
        }

        static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("presence"),
            new AddColumn<UUID>("UserID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("SessionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("SecureSessionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<Date>("LastSeen") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new PrimaryKeyInfo("UserID"),
            new NamedKeyInfo("UserID", "UserID"),
            new NamedKeyInfo("SecureSessionID", "SecureSessionID"),
            new NamedKeyInfo("RegionID", "RegionID"),
            new TableRevision(2),
            /* necessary correction */
            new ChangeColumn<Date>("LastSeen") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
        };

        #region PresenceServiceInterface
        public override List<PresenceInfo> GetPresencesInRegion(UUID regionId)
        {
            List<PresenceInfo> presences = new List<PresenceInfo>();
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM presence WHERE RegionID LIKE ?regionID", conn))
                {
                    cmd.Parameters.AddParameter("?regionID", regionId);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            PresenceInfo pi = new PresenceInfo();
                            pi.UserID.ID = reader.GetUUID("UserID");
                            pi.RegionID = reader.GetUUID("RegionID");
                            pi.SessionID = reader.GetUUID("SessionID");
                            pi.SecureSessionID = reader.GetUUID("SecureSessionID");
                            presences.Add(pi);
                        }
                    }
                }
            }
            return presences;
        }

        public override List<PresenceInfo> this[UUID userID]
        {
            get
            {
                List<PresenceInfo> presences = new List<PresenceInfo>();
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM presence WHERE UserID LIKE ?userID", conn))
                    {
                        cmd.Parameters.AddParameter("?userID", userID);
                        using(MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            PresenceInfo pi = new PresenceInfo();
                            pi.UserID.ID = reader.GetUUID("UserID");
                            pi.RegionID = reader.GetUUID("RegionID");
                            pi.SessionID = reader.GetUUID("SessionID");
                            pi.SecureSessionID = reader.GetUUID("SecureSessionID");
                            presences.Add(pi);
                        }
                    }
                }
                return presences;
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override PresenceInfo this[UUID sessionID, UUID userID]
        {
            get
            {
                throw new NotSupportedException();
            }
            set /* setting null means logout, != null not allowed */
            {
                if(value != null)
                {
                    throw new ArgumentException("setting value != null is not allowed without reportType");
                }
                this[sessionID, userID, SetType.Report] = null;
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public override PresenceInfo this[UUID sessionID, UUID userID, PresenceServiceInterface.SetType reportType]
        { 
            /* setting null means logout, != null login message */
            set 
            {
                if (value == null)
                {
                    using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        using(MySqlCommand cmd = new MySqlCommand("DELETE FROM presence WHERE SessionID LIKE ?sessionID", conn))
                        {
                            cmd.Parameters.AddParameter("?sessionID", sessionID);
                            if(cmd.ExecuteNonQuery() < 1)
                            {
                                throw new PresenceUpdateFailedException();
                            }
                        }
                    }
                }
                else if (reportType == SetType.Login)
                {
                    Dictionary<string, object> post = new Dictionary<string, object>();
                    post["UserID"] = value.UserID.ID;
                    post["SessionID"] = value.SessionID;
                    post["SecureSessionID"] = value.SecureSessionID;
                    post["RegionID"] = UUID.Zero;
                    post["LastSeen"] = Date.Now;

                    using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        try
                        {
                            conn.InsertInto("presence", post);
                        }
                        catch(Exception e)
                        {
                            m_Log.Debug("Presence update failed", e);
                            throw new PresenceUpdateFailedException();
                        }
                    }
                }
                else if (reportType == SetType.Report)
                {
                    using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand("UPDATE presence SET RegionID = ?regionID WHERE SessionID LIKE ?sessionID", conn))
                        {
                            cmd.Parameters.AddParameter("?regionID", value.RegionID);
                            cmd.Parameters.AddParameter("?sessionID", value.SessionID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                else
                {
                    throw new ArgumentException("Invalid reportType specified");
                }
            }
        }

        public override void LogoutRegion(UUID regionID)
        {
            using(MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using(MySqlCommand cmd = new MySqlCommand("DELETE FROM presence WHERE RegionID LIKE ?regionid", connection))
                {
                    cmd.Parameters.AddParameter("?regionid", regionID);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        #endregion

        public override void Remove(UUID scopeID, UUID userAccount)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM presence WHERE UserID LIKE ?userid", conn))
                {
                    cmd.Parameters.AddParameter("?userid", userAccount);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
    #endregion

    #region Factory
    [PluginName("Presence")]
    public class MySQLPresenceServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL PRESENCE SERVICE");
        public MySQLPresenceServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLPresenceService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion

}
