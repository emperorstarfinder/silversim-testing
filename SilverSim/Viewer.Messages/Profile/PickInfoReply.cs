﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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

        public PickInfoReply()
        {

        }

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
            PickInfoReply m = new PickInfoReply();
            m.AgentID = p.ReadUUID();
            m.PickID = p.ReadUUID();
            m.CreatorID = p.ReadUUID();
            m.TopPick = p.ReadBoolean();
            m.ParcelID = p.ReadUUID();
            m.Name = p.ReadStringLen8();
            m.Description = p.ReadStringLen16();
            m.SnapshotID = p.ReadUUID();
            m.User = p.ReadStringLen8();
            m.OriginalName = p.ReadStringLen8();
            m.PosGlobal = p.ReadVector3d();
            m.SortOrder = p.ReadInt32();
            m.IsEnabled = p.ReadBoolean();
            return m;
        }
    }
}
