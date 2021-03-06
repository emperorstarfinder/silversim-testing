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

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelClaim)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ParcelClaim : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UUID GroupID;
        public bool IsGroupOwned;
        public bool IsFinal;

        public struct ParcelDataEntry
        {
            public double West;
            public double South;
            public double East;
            public double North;
        }

        public List<ParcelDataEntry> ParcelData = new List<ParcelDataEntry>();

        public static Message Decode(UDPPacket p)
        {
            var m = new ParcelClaim
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID(),

                GroupID = p.ReadUUID(),
                IsGroupOwned = p.ReadBoolean(),
                IsFinal = p.ReadBoolean()
            };
            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.ParcelData.Add(new ParcelDataEntry
                {
                    West = p.ReadFloat(),
                    South = p.ReadFloat(),
                    East = p.ReadFloat(),
                    North = p.ReadFloat()
                });
            }

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(GroupID);
            p.WriteBoolean(IsGroupOwned);
            p.WriteBoolean(IsFinal);

            p.WriteUInt8((byte)ParcelData.Count);
            foreach(var d in ParcelData)
            {
                p.WriteFloat((float)d.West);
                p.WriteFloat((float)d.South);
                p.WriteFloat((float)d.East);
                p.WriteFloat((float)d.North);
            }
        }
    }
}
