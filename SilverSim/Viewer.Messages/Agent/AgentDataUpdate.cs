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
using System;

namespace SilverSim.Viewer.Messages.Agent
{
    [UDPMessage(MessageType.AgentDataUpdate)]
    [Reliable]
    [Trusted]
    public class AgentDataUpdate : Message
    {
        public UUID AgentID;
        public string FirstName = string.Empty;
        public string LastName = string.Empty;
        public string GroupTitle = string.Empty;
        public UUID ActiveGroupID = UUID.Zero;
        public GroupPowers GroupPowers;
        public string GroupName = string.Empty;

        public static Message Decode(UDPPacket p) => new AgentDataUpdate
        {
            AgentID = p.ReadUUID(),
            FirstName = p.ReadStringLen8(),
            LastName = p.ReadStringLen8(),
            GroupTitle = p.ReadStringLen8(),
            ActiveGroupID = p.ReadUUID(),
            GroupPowers = (GroupPowers)p.ReadUInt64(),
            GroupName = p.ReadStringLen8()
        };

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteStringLen8(FirstName);
            p.WriteStringLen8(LastName);
            p.WriteStringLen8(GroupTitle);
            p.WriteUUID(ActiveGroupID);
            p.WriteUInt64((UInt64)GroupPowers);
            if (GroupName.Length == 0)
            {
                p.WriteUInt8(0);
            }
            else
            {
                p.WriteStringLen8(GroupName);
            }
        }
    }
}
