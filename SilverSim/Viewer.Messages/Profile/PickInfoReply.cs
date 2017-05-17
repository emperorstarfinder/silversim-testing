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

namespace SilverSim.Viewer.Messages.Profile
{
    [UDPMessage(MessageType.PickInfoReply)]
    [Reliable]
    [NotTrusted]
    public class PickInfoReply : Message
    {
        public UUID AgentID;
        public UUID PickID;
        public UUID CreatorID;
        public bool TopPick;
        public UUID ParcelID;
        public string Name;
        public string Description;
        public UUID SnapshotID;
        public string User;
        public string OriginalName;
        public Vector3 PosGlobal;
        public Int32 SortOrder;
        public bool IsEnabled;

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(PickID);
            p.WriteUUID(CreatorID);
            p.WriteBoolean(TopPick);
            p.WriteUUID(ParcelID);
            p.WriteStringLen8(Name);
            p.WriteStringLen16(Description);
            p.WriteUUID(SnapshotID);
            p.WriteStringLen8(User);
            p.WriteStringLen8(OriginalName);
            p.WriteVector3d(PosGlobal);
            p.WriteInt32(SortOrder);
            p.WriteBoolean(IsEnabled);
        }

        public static Message Decode(UDPPacket p)
        {
            return new PickInfoReply()
            {
                AgentID = p.ReadUUID(),
                PickID = p.ReadUUID(),
                CreatorID = p.ReadUUID(),
                TopPick = p.ReadBoolean(),
                ParcelID = p.ReadUUID(),
                Name = p.ReadStringLen8(),
                Description = p.ReadStringLen16(),
                SnapshotID = p.ReadUUID(),
                User = p.ReadStringLen8(),
                OriginalName = p.ReadStringLen8(),
                PosGlobal = p.ReadVector3d(),
                SortOrder = p.ReadInt32(),
                IsEnabled = p.ReadBoolean()
            };
        }
    }
}
