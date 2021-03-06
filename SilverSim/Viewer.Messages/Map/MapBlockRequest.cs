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

namespace SilverSim.Viewer.Messages.Map
{
    [UDPMessage(MessageType.MapBlockRequest)]
    [Reliable]
    [NotTrusted]
    public class MapBlockRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public MapAgentFlags Flags;
        public UInt32 EstateID;
        public bool IsGodlike;
        public GridVector Min;
        public GridVector Max;

        public static Message Decode(UDPPacket p)
        {
            var m = new MapBlockRequest
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID(),
                Flags = (MapAgentFlags)p.ReadUInt32(),
                EstateID = p.ReadUInt32(),
                IsGodlike = p.ReadBoolean()
            };
            m.Min.GridX = p.ReadUInt16();
            m.Max.GridX = p.ReadUInt16();
            m.Min.GridY = p.ReadUInt16();
            m.Max.GridY = p.ReadUInt16();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt32((uint)Flags);
            p.WriteUInt32(EstateID);
            p.WriteBoolean(IsGodlike);
            p.WriteUInt16(Min.GridX);
            p.WriteUInt16(Max.GridX);
            p.WriteUInt16(Min.GridY);
            p.WriteUInt16(Max.GridY);
        }
    }
}
