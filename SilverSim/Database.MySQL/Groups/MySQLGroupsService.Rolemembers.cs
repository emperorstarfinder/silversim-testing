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

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Groups
{
    partial class MySQLGroupsService : GroupsServiceInterface.IGroupRolemembersInterface
    {
        List<GroupRolemember> IGroupRolemembersInterface.this[UUI requestingAgent, UGI group]
        {
            get
            {
                List<GroupRolemember> rolemembers = new List<GroupRolemember>();

                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT rm.*, r.Powers FROM grouprolememberships AS rm INNER JOIN grouproles AS r ON rm.GroupID LIKE r.GroupID AND rm.RoleID LIKE r.RoleID WHERE rm.GroupID LIKE ?groupid", conn))
                    {
                        cmd.Parameters.AddParameter("?groupid", group.ID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                GroupRolemember grolemem = reader.ToGroupRolemember();
                                grolemem.Principal = ResolveName(grolemem.Principal);
                                grolemem.Group = ResolveName(requestingAgent, grolemem.Group);
                                rolemembers.Add(grolemem);
                            }
                        }
                    }

                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM groupmemberships WHERE rm.PrincipalID LIKE ?principalid", conn))
                    {
                        cmd.Parameters.AddParameter("?groupid", group.ID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                GroupRole groupRole;
                                if (Roles.TryGetValue(requestingAgent, group, UUID.Zero, out groupRole))
                                {
                                    GroupRolemember grolemem = reader.ToGroupRolememberEveryone(groupRole.Powers);
                                    grolemem.Principal = ResolveName(grolemem.Principal);
                                    grolemem.Group = ResolveName(requestingAgent, grolemem.Group);
                                    rolemembers.Add(grolemem);
                                }
                            }
                        }
                    }
                }

                return rolemembers;
            }
        }

        List<GroupRolemembership> IGroupRolemembersInterface.this[UUI requestingAgent, UUI principal]
        {
            get
            {
                List<GroupRolemembership> rolemembers = new List<GroupRolemembership>();
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT rm.*, r.Powers, r.Title FROM grouprolememberships AS rm INNER JOIN grouproles AS r ON rm.GroupID LIKE r.GroupID AND rm.RoleID LIKE r.RoleID WHERE rm.PrincipalID LIKE ?principalid", conn))
                    {
                        cmd.Parameters.AddParameter("?principalid", principal.ID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                GroupRolemembership grolemem = reader.ToGroupRolemembership();
                                grolemem.Principal = ResolveName(grolemem.Principal);
                                grolemem.Group = ResolveName(requestingAgent, grolemem.Group);
                                rolemembers.Add(grolemem);
                            }
                        }
                    }

                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM groupmemberships WHERE rm.PrincipalID LIKE ?principalid", conn))
                    {
                        cmd.Parameters.AddParameter("?principalid", principal.ID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                UGI group = new UGI(reader.GetUUID("GroupID"));
                                GroupRole groupRole;
                                if (Roles.TryGetValue(requestingAgent, group, UUID.Zero, out groupRole))
                                {
                                    GroupRolemembership grolemem = reader.ToGroupRolemembershipEveryone(groupRole.Powers);
                                    grolemem.Principal = ResolveName(grolemem.Principal);
                                    grolemem.Group = ResolveName(requestingAgent, grolemem.Group);
                                    grolemem.GroupTitle = groupRole.Title;
                                    rolemembers.Add(grolemem);
                                }
                            }
                        }
                    }
                }

                return rolemembers;
            }
        }

        List<GroupRolemember> IGroupRolemembersInterface.this[UUI requestingAgent, UGI group, UUID roleID]
        {
            get
            {
                List<GroupRolemember> rolemembers = new List<GroupRolemember>();

                if(UUID.Zero == roleID)
                {
                    GroupRole groupRole;
                    if(!Roles.TryGetValue(requestingAgent, group, roleID, out groupRole))
                    {
                        return rolemembers;
                    }

                    using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM groupmemberships WHERE rm.GroupID LIKE ?groupid", conn))
                        {
                            cmd.Parameters.AddParameter("?groupid", group.ID);
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    GroupRolemember grolemem = reader.ToGroupRolememberEveryone(groupRole.Powers);
                                    grolemem.Principal = ResolveName(grolemem.Principal);
                                    grolemem.Group = ResolveName(requestingAgent, grolemem.Group);
                                    rolemembers.Add(grolemem);
                                }
                            }
                        }
                    }
                }
                else
                {
                    using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand("SELECT rm.*, r.Powers FROM grouprolememberships AS rm INNER JOIN grouproles AS r ON rm.GroupID LIKE r.GroupID AND rm.RoleID LIKE r.RoleID WHERE rm.GroupID LIKE ?groupid AND rm.RoleID LIKE ?roleid", conn))
                        {
                            cmd.Parameters.AddParameter("?groupid", group.ID);
                            cmd.Parameters.AddParameter("?roleid", roleID);
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    GroupRolemember grolemem = reader.ToGroupRolemember();
                                    grolemem.Principal = ResolveName(grolemem.Principal);
                                    grolemem.Group = ResolveName(requestingAgent, grolemem.Group);
                                    rolemembers.Add(grolemem);
                                }
                            }
                        }
                    }
                }
                return rolemembers;
            }
        }

        GroupRolemember IGroupRolemembersInterface.this[UUI requestingAgent, UGI group, UUID roleID, UUI principal]
        {
            get
            {
                GroupRolemember rolemem;
                if(!Rolemembers.TryGetValue(requestingAgent, group, roleID, principal, out rolemem))
                {
                    throw new KeyNotFoundException();
                }
                return rolemem;
            }
        }

        void IGroupRolemembersInterface.Add(UUI requestingAgent, GroupRolemember rolemember)
        {
            if(rolemember.RoleID == UUID.Zero)
            {
                return; /* ignore those */
            }
            Dictionary<string, object> vals = new Dictionary<string, object>();
            vals.Add("GroupID", rolemember.Group.ID);
            vals.Add("RoleID", rolemember.RoleID);
            vals.Add("PrincipalID", rolemember.Principal.ID);
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsertInto("grouprolememberships", vals);
            }
        }

        bool IGroupRolemembersInterface.ContainsKey(UUI requestingAgent, UGI group, UUID roleID, UUI principal)
        {
            if(UUID.Zero == roleID)
            {
                return Members.ContainsKey(requestingAgent, group, principal);
            }
            else
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT rm.GroupID FROM grouprolememberships AS rm INNER JOIN grouproles AS r ON rm.GroupID LIKE r.GroupID AND rm.RoleID LIKE r.RoleID WHERE rm.GroupID LIKE ?groupid AND rm.RoleID LIKE ?roleid and rm.PrincipalID LIKE ?principalid", conn))
                    {
                        cmd.Parameters.AddParameter("?groupid", group.ID);
                        cmd.Parameters.AddParameter("?roleid", roleID);
                        cmd.Parameters.AddParameter("?principalid", principal.ID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            return reader.Read();
                        }
                    }
                }
            }
        }

        void IGroupRolemembersInterface.Delete(UUI requestingAgent, UGI group, UUID roleID, UUI principal)
        {
            if(UUID.Zero == roleID)
            {
                throw new NotSupportedException();
            }
            else
            {
                string[] tablenames = new string[] { "groupinvites", "grouprolememberships" };

                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    conn.InsideTransaction(delegate ()
                    {
                        using (MySqlCommand cmd = new MySqlCommand("UPDATE groupmemberships SET SelectedRoleID=?zeroid WHERE SelectedRoleID LIKE ?roleid AND GroupID LIKE ?groupid AND PrincipalID LIKE ?principalid", conn))
                        {
                            cmd.Parameters.AddParameter("?zeroid", UUID.Zero);
                            cmd.Parameters.AddParameter("?principalid", principal.ID);
                            cmd.Parameters.AddParameter("?groupid", group.ID);
                            cmd.Parameters.AddParameter("?roleid", roleID);
                            cmd.ExecuteNonQuery();
                        }

                        foreach(string table in tablenames)
                        {
                            using (MySqlCommand cmd = new MySqlCommand("DELETE FROM " + table + " WHERE GroupID LIKE ?groupid AND RoleID LIKE ?roleid AND PrincipalID LIKE ?principalid", conn))
                            {
                                cmd.Parameters.AddParameter("?principalid", principal.ID);
                                cmd.Parameters.AddParameter("?groupid", group.ID);
                                cmd.Parameters.AddParameter("?roleid", roleID);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    });
                }
            }
        }

        bool IGroupRolemembersInterface.TryGetValue(UUI requestingAgent, UGI group, UUID roleID, UUI principal, out GroupRolemember grolemem)
        {
            grolemem = null;
            if(UUID.Zero == roleID)
            {
                GroupMember gmem;
                GroupRole role;
                if(Members.TryGetValue(requestingAgent, group, principal, out gmem) &&
                    Roles.TryGetValue(requestingAgent, group, UUID.Zero, out role))
                {
                    grolemem = new GroupRolemember();
                    grolemem.Powers = role.Powers;
                    grolemem.Principal = ResolveName(principal);
                    grolemem.RoleID = UUID.Zero;
                    grolemem.Group = gmem.Group;

                    return true;
                }
            }
            else
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT rm.*, r.Powers FROM grouprolememberships AS rm INNER JOIN grouproles AS r ON rm.GroupID LIKE r.GroupID AND rm.RoleID LIKE r.RoleID WHERE rm.GroupID LIKE ?groupid AND rm.RoleID LIKE ?roleid and rm.PrincipalID LIKE ?principalid", conn))
                    {
                        cmd.Parameters.AddParameter("?groupid", group.ID);
                        cmd.Parameters.AddParameter("?roleid", roleID);
                        cmd.Parameters.AddParameter("?principalid", principal.ID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if(reader.Read())
                            {
                                grolemem = reader.ToGroupRolemember();
                                grolemem.Principal = ResolveName(grolemem.Principal);
                                grolemem.Group = ResolveName(requestingAgent, grolemem.Group);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
