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
using SilverSim.ServiceInterfaces.Avatar;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.Avatar
{
    #region Service Implementation
    [Description("MySQL Avatar Backend")]
    public sealed class MySQLAvatarService : AvatarServiceInterface, IDBServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        readonly string m_ConnectionString;
        static readonly ILog m_Log = LogManager.GetLogger("MYSQL AVATAR SERVICE");

        public MySQLAvatarService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public override Dictionary<string, string> this[UUID avatarID]
        {
            get
            {
                Dictionary<string, string> result = new Dictionary<string, string>();
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT `Name`,`Value` FROM avatars WHERE PrincipalID LIKE ?principalid", connection))
                    {
                        cmd.Parameters.AddParameter("?principalid", avatarID);
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            while (dbReader.Read())
                            {
                                result.Add(dbReader.GetString("Name"), dbReader.GetString("Value"));
                            }
                        }
                    }
                }

                return result;
            }
            set
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    if (null == value)
                    {
                        using (MySqlCommand cmd = new MySqlCommand("DELETE FROM avatars WHERE PrincipalID LIKE ?principalid", connection))
                        {
                            cmd.Parameters.AddParameter("?principalid", avatarID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        connection.InsideTransaction(delegate()
                        {
                            using (MySqlCommand cmd = new MySqlCommand("DELETE FROM avatars WHERE PrincipalID LIKE ?principalid", connection))
                            {
                                cmd.Parameters.AddParameter("?principalid", avatarID);
                                cmd.ExecuteNonQuery();
                            }

                            Dictionary<string, object> vals = new Dictionary<string, object>();
                            vals["PrincipalID"] = avatarID;
                            foreach (KeyValuePair<string, string> kvp in value)
                            {
                                vals["Name"] = kvp.Key;
                                vals["Value"] = kvp.Value;
                                connection.ReplaceInto("avatars", vals);
                            }
                        });
                    }
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override List<string> this[UUID avatarID, IList<string> itemKeys]
        {
            get
            {
                List<string> result = new List<string>();
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();

                    connection.InsideTransaction(delegate()
                    {
                        foreach (string key in itemKeys)
                        {
                            using (MySqlCommand cmd = new MySqlCommand("SELECT `Value` FROM avatars WHERE PrincipalID LIKE ?principalid AND `Name` LIKE ?name", connection))
                            {
                                cmd.Parameters.AddWithValue("?principalid", avatarID.ToString());
                                cmd.Parameters.AddWithValue("?name", key);
                                using (MySqlDataReader dbReader = cmd.ExecuteReader())
                                {
                                    result.Add(dbReader.Read() ? dbReader.GetString("Value") : string.Empty);
                                }
                            }
                        }
                    });
                }
                return result;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                else if (itemKeys == null)
                {
                    throw new ArgumentNullException("itemKeys");
                }
                if (itemKeys.Count != value.Count)
                {
                    throw new ArgumentException("value and itemKeys must have identical Count");
                }

                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();

                    Dictionary<string, object> vals = new Dictionary<string, object>();
                    vals["PrincipalID"] = avatarID;

                    connection.InsideTransaction(delegate()
                    {
                        for (int i = 0; i < itemKeys.Count; ++i)
                        {
                            vals["Name"] = itemKeys[i];
                            vals["Value"] = value[i];
                            connection.ReplaceInto("avatars", vals);
                        }
                    });
                }
            }
        }

        public override bool TryGetValue(UUID avatarID, string itemKey, out string value)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT `Value` FROM avatars WHERE PrincipalID LIKE ?principalid AND `Name` LIKE ?name", connection))
                {
                    cmd.Parameters.AddWithValue("?principalid", avatarID.ToString());
                    cmd.Parameters.AddWithValue("?name", itemKey);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            value = dbReader.GetString("Value");
                            return true;
                        }
                    }
                }
            }

            value = string.Empty;
            return false;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override string this[UUID avatarID, string itemKey]
        {
            get
            {
                string s;
                if (!TryGetValue(avatarID, itemKey, out s))
                {
                    throw new KeyNotFoundException(string.Format("{0},{1} not found", avatarID, itemKey));
                }
                return s;
            }
            set
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    Dictionary<string, object> vals = new Dictionary<string, object>();
                    vals["PrincipalID"] = avatarID;
                    vals["Name"] = itemKey;
                    vals["Value"] = value;
                    connection.ReplaceInto("avatars", vals);
                }
            }
        }

        public override void Remove(UUID avatarID, IList<string> nameList)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsideTransaction(delegate()
                {
                    foreach (string name in nameList)
                    {
                        using (MySqlCommand cmd = new MySqlCommand("DELETE FROM avatars WHERE PrincipalID LIKE ?principalid AND `Name` LIKE ?name", connection))
                        {
                            cmd.Parameters.AddWithValue("?principalid", avatarID);
                            cmd.Parameters.AddWithValue("?name", name);
                            cmd.ExecuteNonQuery();
                        }
                    }
                });
            }
        }

        public override void Remove(UUID avatarID, string name)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM avatars WHERE PrincipalID LIKE ?principalid AND `Name` LIKE ?name", connection))
                {
                    cmd.Parameters.AddWithValue("?principalid", avatarID.ToString());
                    cmd.Parameters.AddWithValue("?name", name);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void VerifyConnection()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        public void ProcessMigrations()
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                conn.MigrateTables(Migrations, m_Log);
            }
        }

        static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("avatars"),
            new AddColumn<UUID>("PrincipalID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("Name") { Cardinality = 32, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("Value"),
            new PrimaryKeyInfo("PrincipalID", "Name"),
            new NamedKeyInfo("avatars_principalid", new string[] { "PrincipalID" })
        };

        public void Remove(UUID scopeID, UUID userAccount)
        {
            this[userAccount] = null;
        }
    }
    #endregion

    #region Factory
    [PluginName("Avatar")]
    public class MySQLInventoryServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL AVATAR SERVICE");
        public MySQLInventoryServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLAvatarService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion
}
