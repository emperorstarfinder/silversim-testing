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

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;

namespace SilverSim.Groups.Common.Permissions
{
    [PluginName("DefaultPermissions")]
    public sealed partial class DefaultPermissionsGroupsService : GroupsServiceInterface, IPlugin
    {
        private GroupsServiceInterface m_InnerService;
        private readonly string m_GroupsServiceName;

        public DefaultPermissionsGroupsService(IConfig ownSection)
        {
            m_GroupsServiceName = ownSection.GetString("GroupsStorage", "GroupsStorage");
        }

        public DefaultPermissionsGroupsService(GroupsServiceInterface service)
        {
            m_InnerService = service;
        }

        public override IGroupSelectInterface ActiveGroup => m_InnerService.ActiveGroup;

        public override IActiveGroupMembershipInterface ActiveMembership => m_InnerService.ActiveMembership;

        public override IGroupsInterface Groups => this;

        public override IGroupInvitesInterface Invites => this;

        public override IGroupMembersInterface Members => this;

        public override IGroupMembershipsInterface Memberships => m_InnerService.Memberships;

        public override IGroupNoticesInterface Notices => this;

        public override IGroupRolemembersInterface Rolemembers => this;

        public override IGroupRolesInterface Roles => this;

        public void Startup(ConfigurationLoader loader)
        {
            m_InnerService = loader.GetService<GroupsServiceInterface>(m_GroupsServiceName);
            if(!m_InnerService.Invites.DoesSupportListGetters)
            {
                throw new ConfigurationLoader.ConfigurationErrorException("Inner service must support list getters");
            }
        }

        public override GroupPowers GetAgentPowers(UGI group, UGUI agent) => m_InnerService.GetAgentPowers(group, agent);

        private bool IsGroupOwner(UGI group, UGUI agent)
        {
            GroupInfo groupInfo;
            try
            {
                if(!Groups.TryGetValue(agent, group, out groupInfo))
                {
                    return false;
                }
                return Rolemembers.ContainsKey(agent, group, groupInfo.OwnerRoleID, agent);
            }
            catch
            {
                return false;
            }
        }
    }
}
