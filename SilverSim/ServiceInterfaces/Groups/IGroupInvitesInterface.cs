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

using SilverSim.Types;
using SilverSim.Types.Groups;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Groups
{
    public interface IGroupInvitesInterface : This.IGroupInvitesThisInterface
    {
        bool TryGetValue(UGUI requestingAgent, UUID groupInviteID, out GroupInvite ginvite);
        bool ContainsKey(UGUI requestingAgent, UUID groupInviteID);

        bool DoesSupportListGetters { get; }

        /** <summary>Only for use of Permission modules</summary> */
        List<GroupInvite> this[UGUI requestingAgent, UGI group, UUID roleID, UGUI principal] { get; }

        /** <summary>Only for use of Permission modules</summary> */
        List<GroupInvite> this[UGUI requestingAgent, UGUI principal] { get; }

        /** <summary>Only for use of Permission modules</summary> */
        List<GroupInvite> GetByGroup(UGUI requestingAgent, UGI group);

        void Add(UGUI requestingAgent, GroupInvite invite);
        void Delete(UGUI requestingAgent, UUID inviteID);
    }
}