﻿/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.BackendConnectors.Robust.GroupsV2
{
    public partial class RobustGroupsConnector
    {
        class InvitesAccessor : IGroupInvitesInterface
        {
            public int TimeoutMs = 20000;
            string m_Uri;

            public InvitesAccessor(string uri)
            {
                m_Uri = uri;
            }

            public GroupInvite this[UUI requestingAgent, UUID groupInviteID]
            {
                get { throw new NotImplementedException(); }
            }

            public List<GroupInvite> this[UUI requestingAgent, UGI group, UUID roleID, UUI principal]
            {
                get { throw new NotImplementedException(); }
            }

            public List<GroupInvite> this[UUI requestingAgent, UUI principal]
            {
                get { throw new NotImplementedException(); }
            }

            public List<GroupInvite> GetByGroup(UUI requestingAgent, UGI group)
            {
                throw new NotImplementedException();
            }

            public void Add(UUI requestingAgent, GroupInvite invite)
            {
                throw new NotImplementedException();
            }

            public void Update(UUI requestingAgent, GroupInvite invite)
            {
                throw new NotImplementedException();
            }

            public void Delete(UUI requestingAgent, UUID inviteID)
            {
                throw new NotImplementedException();
            }
        }
    }
}
