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

namespace SilverSim.Viewer.Messages.Profile
{
    [UDPMessage(MessageType.ClassifiedInfoReply)]
    [Reliable]
    [Trusted]
    public class ClassifiedInfoReply : Message
    {
        public UUID AgentID = UUID.Zero;

        public UUID ClassifiedID = UUID.Zero;
        public UUID CreatorID = UUID.Zero;
        public Date CreationDate = new Date();
        public Date ExpirationDate = new Date();
        public int Category;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public ParcelID ParcelID = new ParcelID();
        public int ParentEstate;
        public UUID SnapshotID = UUID.Zero;
        public string SimName = string.Empty;
        public Vector3 PosGlobal = Vector3.Zero;
        public string ParcelName = string.Empty;
        public byte ClassifiedFlags;
        public int PriceForListing;

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(ClassifiedID);
            p.WriteUUID(CreatorID);
            p.WriteUInt32((uint)CreationDate.DateTimeToUnixTime());
            p.WriteUInt32((uint)ExpirationDate.DateTimeToUnixTime());
            p.WriteInt32(Category);
            p.WriteStringLen8(Name);
            p.WriteStringLen16(Description);
            p.WriteBytes(ParcelID.GetBytes());
            p.WriteInt32(ParentEstate);
            p.WriteUUID(SnapshotID);
            p.WriteStringLen8(SimName);
            p.WriteVector3d(PosGlobal);
            p.WriteStringLen8(ParcelName);
            p.WriteUInt8(ClassifiedFlags);
            p.WriteInt32(PriceForListing);
        }

        public static Message Decode(UDPPacket p) => new ClassifiedInfoReply
        {
            AgentID = p.ReadUUID(),
            ClassifiedID = p.ReadUUID(),
            CreatorID = p.ReadUUID(),
            CreationDate = Date.UnixTimeToDateTime(p.ReadUInt32()),
            ExpirationDate = Date.UnixTimeToDateTime(p.ReadUInt32()),
            Category = p.ReadInt32(),
            Name = p.ReadStringLen8(),
            Description = p.ReadStringLen16(),
            ParcelID = new ParcelID(p.ReadBytes(16), 0),
            ParentEstate = p.ReadInt32(),
            SnapshotID = p.ReadUUID(),
            SimName = p.ReadStringLen8(),
            PosGlobal = p.ReadVector3d(),
            ParcelName = p.ReadStringLen8(),
            ClassifiedFlags = p.ReadUInt8(),
            PriceForListing = p.ReadInt32()
        };
    }
}
