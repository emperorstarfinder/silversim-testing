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
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelAccessListUpdate)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ParcelAccessListUpdate : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public ParcelAccessList Flags;
        public Int32 LocalID;
        public UUID TransactionID;
        public Int32 SequenceID;
        public Int32 Sections;

        public struct Data
        {
            public UUID ID;
            public UInt32 Time;
            public ParcelAccessList Flags;
        }

        public List<Data> AccessList = new List<Data>();

        public static Message Decode(UDPPacket p)
        {
            var m = new ParcelAccessListUpdate
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID(),
                Flags = (ParcelAccessList)p.ReadUInt32(),
                LocalID = p.ReadInt32(),
                TransactionID = p.ReadUUID(),
                SequenceID = p.ReadInt32(),
                Sections = p.ReadInt32()
            };
            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.AccessList.Add(new Data
                {
                    ID = p.ReadUUID(),
                    Time = p.ReadUInt32(),
                    Flags = (ParcelAccessList)p.ReadUInt32()
                });
            }

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt32((uint)Flags);
            p.WriteInt32(LocalID);
            p.WriteUUID(TransactionID);
            p.WriteInt32(SequenceID);
            p.WriteInt32(Sections);

            p.WriteUInt8((byte)AccessList.Count);
            foreach(var d in AccessList)
            {
                p.WriteUUID(d.ID);
                p.WriteUInt32(d.Time);
                p.WriteUInt32((uint)d.Flags);
            }
        }
    }
}
