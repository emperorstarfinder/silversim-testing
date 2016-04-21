﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Types;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.Estate
{
    public partial class MySQLEstateService : IEstateBanServiceInterface, IEstateBanServiceListAccessInterface
    {
        List<UUI> IEstateBanServiceListAccessInterface.this[uint estateID]
        {
            get
            {
                List<UUI> estateusers = new List<UUI>();
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT UserID FROM estate_bans WHERE EstateID = ?estateid", conn))
                    {
                        cmd.Parameters.AddParameter("?estateid", estateID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                estateusers.Add(reader.GetUUI("UserID"));
                            }
                        }
                    }
                }
                return estateusers;
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        bool IEstateBanServiceInterface.this[uint estateID, UUI agent]
        {
            get
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT UserID FROM estate_bans WHERE EstateID = ?estateid AND UserID LIKE \"" + agent.ID.ToString() + "%\"", conn))
                    {
                        cmd.Parameters.AddParameter("?estateid", estateID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while(reader.Read())
                            {
                                UUI uui = reader.GetUUI("UserID");
                                if(uui.EqualsGrid(agent))
                                {
                                    return true;
                                }
                            }
                            return false;
                        }
                    }
                }
            }
            set
            {
                string query = value ?
                    "REPLACE INTO estate_bans (EstateID, UserID) VALUES (?estateid, ?userid)" :
                    "DELETE FROM estate_bans WHERE EstateID = ?estateid AND UserID LIKE \"" + agent.ID.ToString() + "%\"";

                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddParameter("?estateid", estateID);
                        if (value)
                        {
                            cmd.Parameters.AddWithValue("?userid", agent.ID.ToString());
                        }
                        if (cmd.ExecuteNonQuery() < 1 && value)
                        {
                            throw new EstateUpdateFailedException();
                        }
                    }
                }
            }
        }

        IEstateBanServiceListAccessInterface IEstateBanServiceInterface.All
        {
            get
            {
                return this;
            }
        }
    }
}