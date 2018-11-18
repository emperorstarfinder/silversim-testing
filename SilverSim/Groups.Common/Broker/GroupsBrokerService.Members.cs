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

namespace SilverSim.Groups.Common.Broker
{
    public sealed partial class GroupsBrokerService : IGroupMembersInterface
    {
        List<GroupMember> IGroupMembersInterface.this[UGUI requestingAgent, UGI group] =>
            GetGroupsService(group).Members[requestingAgent, group];

        List<GroupMember> IGroupMembersInterface.this[UGUI requestingAgent, UGUI principal] => 
            GetGroupsService(principal).Members[requestingAgent, principal];

        GroupMember IGroupMembersInterface.Add(UGUI requestingAgent, UGI group, UGUI principal, UUID roleID, string accessToken) =>
            GetGroupsService(group).Members.Add(requestingAgent, group, principal, roleID, accessToken);

        bool IGroupMembersInterface.ContainsKey(UGUI requestingAgent, UGI group, UGUI principal)
        {
            GroupsServiceInterface groupsService;
            return TryGetGroupsService(group, out groupsService) && groupsService.Members.ContainsKey(requestingAgent, group, principal);
        }

        void IGroupMembersInterface.Delete(UGUI requestingAgent, UGI group, UGUI principal) =>
            GetGroupsService(group).Members.Delete(requestingAgent, group, principal);

        void IGroupMembersInterface.SetContribution(UGUI requestingagent, UGI group, UGUI principal, int contribution) =>
            GetGroupsService(group).Members.SetContribution(requestingagent, group, principal, contribution);

        bool IGroupMembersInterface.TryGetValue(UGUI requestingAgent, UGI group, UGUI principal, out GroupMember gmem)
        {
            gmem = default(GroupMember);
            GroupsServiceInterface groupsService;
            return TryGetGroupsService(group, out groupsService) && groupsService.Members.TryGetValue(requestingAgent, group, principal, out gmem);
        }

        void IGroupMembersInterface.Update(UGUI requestingagent, UGI group, UGUI principal, bool acceptNotices, bool listInProfile) =>
            GetGroupsService(group).Members.Update(requestingagent, group, principal, acceptNotices, listInProfile);
    }
}
