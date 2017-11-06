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
using SilverSim.Types.Parcel;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelReturnObjects)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ParcelReturnObjects : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public Int32 LocalID;
        public ObjectReturnType ReturnType;
        public List<UUID> TaskIDs = new List<UUID>();
        public List<UUID> OwnerIDs = new List<UUID>();

        public static Message Decode(UDPPacket p)
        {
            var m = new ParcelReturnObjects
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID(),
                LocalID = p.ReadInt32(),
                ReturnType = (ObjectReturnType)p.ReadUInt32()
            };
            uint cnt = p.ReadUInt8();
            for (uint i = 0; i < cnt; ++i)
            {
                m.TaskIDs.Add(p.ReadUUID());
            }

            cnt = p.ReadUInt8();
            for (uint i = 0; i < cnt; ++i)
            {
                m.OwnerIDs.Add(p.ReadUUID());
            }
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteInt32(LocalID);
            p.WriteUInt32((uint)ReturnType);
            p.WriteUInt8((byte)TaskIDs.Count);
            foreach(UUID tid in TaskIDs)
            {
                p.WriteUUID(tid);
            }
            p.WriteUInt8((byte)OwnerIDs.Count);
            foreach(UUID id in OwnerIDs)
            {
                p.WriteUUID(id);
            }
        }
    }
}
