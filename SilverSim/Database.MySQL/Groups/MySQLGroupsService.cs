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
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Groups
{
    public partial class MySQLGroupsService : GroupsServiceInterface, IPlugin, IDBServiceInterface, IUserAccountDeleteServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL GROUPS SERVICE");
        AggregatingAvatarNameService m_AvatarNameService;

        const string GCountQuery = "(SELECT COUNT(m.PrincipalID) FROM groupmemberships AS m WHERE m.GroupID LIKE g.GroupID) AS MemberCount," +
                                    "(SELECT COUNT(r.RoleID) FROM grouproles AS r WHERE r.GroupID LIKE g.GroupID) AS RoleCount";

        const string MCountQuery = "(SELECT COUNT(xr.RoleID) FROM grouproles AS xr WHERE xr.GroupID LIKE g.GroupID) AS RoleCount";

        const string RCountQuery = "(SELECT COUNT(xrm.PrincipalID) FROM grouprolememberships AS xrm WHERE xrm.RoleID LIKE r.RoleID AND xrm.GroupID LIKE r.GroupID) AS RoleMembers," +
                                    "(SELECT COUNT(xm.PrincipalID) FROM groupmemberships AS xm WHERE xm.GroupID LIKE r.GroupID) AS GroupMembers";

        UUI ResolveName(UUI uui)
        {
            UUI resultuui;
            if (m_AvatarNameService.TryGetValue(uui, out resultuui))
            {
                return resultuui;
            }
            return uui;
        }

        UGI ResolveName(UUI requestingAgent, UGI group)
        {
            UGI resolved;
            return Groups.TryGetValue(requestingAgent, group.ID, out resolved) ? resolved : group;
        }

        public override IGroupSelectInterface ActiveGroup
        {
            get
            {
                return this;
            }
        }

        public override IActiveGroupMembershipInterface ActiveMembership
        {
            get
            {
                return this;
            }
        }

        public override IGroupsInterface Groups
        {
            get
            {
                return this;
            }
        }

        public override IGroupInvitesInterface Invites
        {
            get
            {
                return this;
            }
        }

        public override IGroupMembersInterface Members
        {
            get
            {
                return this;
            }
        }

        public override IGroupMembershipsInterface Memberships
        {
            get
            {
                return this;
            }
        }

        public override IGroupNoticesInterface Notices
        {
            get
            {
                return this;
            }
        }

        public override IGroupRolemembersInterface Rolemembers
        {
            get
            {
                return this;
            }
        }

        public override IGroupRolesInterface Roles
        {
            get
            {
                return this;
            }
        }

        bool TryGetGroupRoleRights(UUI requestingAgent, UGI group, UUID roleID, out GroupPowers powers)
        {
            powers = GroupPowers.None;
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT Powers FROM grouproles AS r WHERE r.GroupID LIKE ?groupid AND r.RoleID LIKE ?grouproleid", conn))
                {
                    cmd.Parameters.AddParameter("?groupid", group.ID);
                    cmd.Parameters.AddParameter("?grouproleid", roleID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if(reader.Read())
                        {
                            powers = reader.GetEnum<GroupPowers>("Powers");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override GroupPowers GetAgentPowers(UGI group, UUI agent)
        {
            if(!Members.ContainsKey(agent, group, agent))
            {
                return GroupPowers.None;
            }

            GroupPowers powers;
            if (!TryGetGroupRoleRights(agent, group, UUID.Zero, out powers))
            {
                return GroupPowers.None;
            }

            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(
                    "SELECT Powers FROM roles AS r INNER JOIN " +
                    "((grouprolemembers AS rm INNER JOIN groupmembers AS m ON rm.GroupID LIKE m.GroupID AND rm.PrincipalID LIKE m.PrincipalID) ON " +
                    "r.RoleID LIKE rm.RoleID WHERE rm.GroupID LIKE ?groupid AND rm.PrincipalID LIKE ?principalid", conn))
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            powers |= reader.GetEnum<GroupPowers>("Powers");
                        }
                    }
                }
            }
            return powers;
        }

        public void Startup(ConfigurationLoader loader)
        {
            RwLockedList<AvatarNameServiceInterface> avatarNameServices = new RwLockedList<AvatarNameServiceInterface>();
            foreach(string name in m_AvatarNameServiceNames.Trim().Split(','))
            {
                avatarNameServices.Add(loader.GetService<AvatarNameServiceInterface>(name));
            }
            m_AvatarNameService = new AggregatingAvatarNameService(avatarNameServices);
        }

        public void Remove(UUID scopeID, UUID accountID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsideTransaction(delegate ()
                {
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM groupinvites WHERE PrincipalID LIKE ?id", conn))
                    {
                        cmd.Parameters.AddParameter("?id", accountID);
                    }
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM groupmemberships WHERE PrincipalID LIKE ?id", conn))
                    {
                        cmd.Parameters.AddParameter("?id", accountID);
                    }
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM activegroup WHERE PrincipalID LIKE ?id", conn))
                    {
                        cmd.Parameters.AddParameter("?id", accountID);
                    }
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM grouprolememberships WHERE PrincipalID LIKE ?id", conn))
                    {
                        cmd.Parameters.AddParameter("?id", accountID);
                    }
                });
            }
        }

        readonly string m_ConnectionString;
        readonly string m_AvatarNameServiceNames;

        public MySQLGroupsService(IConfig ownSection)
        {
            m_ConnectionString = MySQLUtilities.BuildConnectionString(ownSection, m_Log);
            m_AvatarNameServiceNames = ownSection.GetString("AvatarNameServices", "AvatarNameStorage");
        }
    }

    [PluginName("Groups")]
    public class MySQLGroupsServiceFactory : IPluginFactory
    {
        public MySQLGroupsServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLGroupsService(ownSection);
        }
    }
}
