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

namespace SilverSim.Groups.Common
{
    partial class DefaultPermissionsGroupsService : GroupsServiceInterface.IGroupsInterface
    {
        GroupInfo IGroupsInterface.this[UUI requestingAgent, string groupName]
        {
            get
            {
                return m_InnerService.Groups[requestingAgent, groupName];
            }
        }

        GroupInfo IGroupsInterface.this[UUI requestingAgent, UGI group]
        {
            get
            {
                return m_InnerService.Groups[requestingAgent, group];
            }
        }

        UGI IGroupsInterface.this[UUI requestingAgent, UUID groupID]
        {
            get
            {
                return m_InnerService.Groups[requestingAgent, groupID];
            }
        }

        bool IGroupsInterface.ContainsKey(UUI requestingAgent, string groupName)
        {
            return m_InnerService.Groups.ContainsKey(requestingAgent, groupName);
        }

        bool IGroupsInterface.ContainsKey(UUI requestingAgent, UGI groupID)
        {
            return m_InnerService.Groups.ContainsKey(requestingAgent, groupID);
        }

        bool IGroupsInterface.ContainsKey(UUI requestingAgent, UUID groupID)
        {
            return m_InnerService.Groups.ContainsKey(requestingAgent, groupID);
        }

        GroupInfo IGroupsInterface.Create(UUI requestingAgent, GroupInfo group)
        {
            return m_InnerService.Groups.Create(requestingAgent, group);
        }

        void IGroupsInterface.Delete(UUI requestingAgent, UGI group)
        {
            if(!IsGroupOwner(group, requestingAgent))
            {
                throw new GroupInsufficientPowersException(GroupPowers.OwnerPowers);
            }
            m_InnerService.Groups.Delete(requestingAgent, group);
        }

        List<DirGroupInfo> IGroupsInterface.GetGroupsByName(UUI requestingAgent, string query)
        {
            return m_InnerService.Groups.GetGroupsByName(requestingAgent, query);
        }

        bool IGroupsInterface.TryGetValue(UUI requestingAgent, string groupName, out GroupInfo groupInfo)
        {
            return m_InnerService.Groups.TryGetValue(requestingAgent, groupName, out groupInfo);
        }

        bool IGroupsInterface.TryGetValue(UUI requestingAgent, UGI groupID, out GroupInfo groupInfo)
        {
            return m_InnerService.Groups.TryGetValue(requestingAgent, groupID, out groupInfo);
        }

        bool IGroupsInterface.TryGetValue(UUI requestingAgent, UUID groupID, out UGI ugi)
        {
            return m_InnerService.Groups.TryGetValue(requestingAgent, groupID, out ugi);
        }

        GroupInfo IGroupsInterface.Update(UUI requestingAgent, GroupInfo group)
        {
            VerifyAgentPowers(group.ID, requestingAgent, GroupPowers.ChangeOptions);
            return m_InnerService.Groups.Update(requestingAgent, group);
        }
    }
}
