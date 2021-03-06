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
using SilverSim.Types.Asset.Format;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Appearance
{
    [UDPMessage(MessageType.AgentIsNowWearing)]
    [Reliable]
    [NotTrusted]
    public class AgentIsNowWearing : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public struct WearableDataEntry
        {
            public UUID ItemID;
            public WearableType WearableType;
        }

        public List<WearableDataEntry> WearableData = new List<WearableDataEntry>();

        public static Message Decode(UDPPacket p)
        {
            var m = new AgentIsNowWearing
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID()
            };
            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.WearableData.Add(new WearableDataEntry
                {
                    ItemID = p.ReadUUID(),
                    WearableType = (WearableType)p.ReadUInt8()
                });
            }

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt8((byte)WearableData.Count);
            foreach(WearableDataEntry d in WearableData)
            {
                p.WriteUUID(d.ItemID);
                p.WriteUInt8((byte)d.WearableType);
            }
        }
    }
}
