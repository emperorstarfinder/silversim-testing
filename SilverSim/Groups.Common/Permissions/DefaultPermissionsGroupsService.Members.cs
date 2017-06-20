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

using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System.Collections.Generic;

namespace SilverSim.Groups.Common.Permissions
{
    partial class DefaultPermissionsGroupsService : GroupsServiceInterface.IGroupMembersInterface
    {
        GroupMember IGroupMembersInterface.this[UUI requestingAgent, UGI group, UUI principal] =>
            m_InnerService.Members[requestingAgent, group, principal];

        bool IGroupMembersInterface.TryGetValue(UUI requestingAgent, UGI group, UUI principal, out GroupMember gmem) =>
            m_InnerService.Members.TryGetValue(requestingAgent, group, principal, out gmem);

        bool IGroupMembersInterface.ContainsKey(UUI requestingAgent, UGI group, UUI principal) =>
            m_InnerService.Members.ContainsKey(requestingAgent, group, principal);

        List<GroupMember> IGroupMembersInterface.this[UUI requestingAgent, UGI group] =>
            m_InnerService.Members[requestingAgent, group];

        List<GroupMember> IGroupMembersInterface.this[UUI requestingAgent, UUI principal] =>
            m_InnerService.Members[requestingAgent, principal];

        GroupMember IGroupMembersInterface.Add(UUI requestingAgent, UGI group, UUI principal, UUID roleID, string accessToken)
        {
            if(Invites[requestingAgent, group, roleID, principal].Count == 0)
            {
                VerifyAgentPowers(group, requestingAgent, GroupPowers.Invite);
            }
            return m_InnerService.Members.Add(requestingAgent, group, principal, roleID, accessToken);
        }

        void IGroupMembersInterface.SetContribution(UUI requestingagent, UGI group, UUI principal, int contribution)
        {
            m_InnerService.Members.SetContribution(requestingagent, group, principal, contribution);
        }

        void IGroupMembersInterface.Update(UUI requestingagent, UGI group, UUI principal, bool acceptNotices, bool listInProfile)
        {
            m_InnerService.Members.Update(requestingagent, group, principal, acceptNotices, listInProfile);
        }

        void IGroupMembersInterface.Delete(UUI requestingAgent, UGI group, UUI principal)
        {
            if(requestingAgent != principal && !IsGroupOwner(group, requestingAgent))
            {
                VerifyAgentPowers(group, requestingAgent, GroupPowers.Eject);
            }

            m_InnerService.Members.Delete(requestingAgent, group, principal);
        }
    }
}