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
using System;

namespace SilverSim.Viewer.Messages.Search
{
    [UDPMessage(MessageType.DirClassifiedQuery)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class DirClassifiedQuery : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UUID QueryID;
        public string QueryText;
        public SearchFlags QueryFlags;
        public UInt32 Category;
        public int QueryStart;

        public static DirClassifiedQuery Decode(UDPPacket p) => new DirClassifiedQuery
        {
            AgentID = p.ReadUUID(),
            SessionID = p.ReadUUID(),
            QueryID = p.ReadUUID(),
            QueryText = p.ReadStringLen8(),
            QueryFlags = (SearchFlags)p.ReadUInt32(),
            Category = p.ReadUInt32(),
            QueryStart = p.ReadInt32()
        };

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(QueryID);
            p.WriteStringLen8(QueryText);
            p.WriteUInt32((uint)QueryFlags);
            p.WriteUInt32(Category);
            p.WriteInt32(QueryStart);
        }
    }
}
