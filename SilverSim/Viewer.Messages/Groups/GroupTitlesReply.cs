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
using System.Collections.Generic;
using System.Text;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.GroupTitlesReply)]
    [Reliable]
    [Trusted]
    public class GroupTitlesReply : Message
    {
        public UUID AgentID;
        public UUID GroupID;
        public UUID RequestID;

        public struct GroupDataEntry
        {
            public string Title;
            public UUID RoleID;
            public bool Selected;
            public int SizeInMessage
            {
                get
                {
                    return 20 + Title.ToUTF8ByteCount();
                }
            }
        }

        public List<GroupDataEntry> GroupData = new List<GroupDataEntry>();

        public GroupTitlesReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(GroupID);
            p.WriteUUID(RequestID);
            p.WriteUInt8((byte)GroupData.Count);
            foreach(GroupDataEntry e in GroupData)
            {
                p.WriteStringLen8(e.Title);
                p.WriteUUID(e.RoleID);
                p.WriteBoolean(e.Selected);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            GroupTitlesReply m = new GroupTitlesReply();
            m.AgentID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.RequestID = p.ReadUUID();
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                GroupDataEntry d = new GroupDataEntry();
                d.Title = p.ReadStringLen8();
                d.RoleID = p.ReadUUID();
                d.Selected = p.ReadBoolean();
                m.GroupData.Add(d);
            }
            return m;
        }
    }
}
