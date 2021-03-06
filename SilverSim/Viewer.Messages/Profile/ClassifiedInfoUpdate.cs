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
    [UDPMessage(MessageType.ClassifiedInfoUpdate)]
    [Reliable]
    [NotTrusted]
    public class ClassifiedInfoUpdate : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UUID ClassifiedID;
        public int Category;
        public string Name;
        public string Description;
        public ParcelID ParcelID;
        public int ParentEstate;
        public UUID SnapshotID;
        public Vector3 PosGlobal;
        public byte ClassifiedFlags;
        public int PriceForListing;

        public static ClassifiedInfoUpdate Decode(UDPPacket p) => new ClassifiedInfoUpdate
        {
            AgentID = p.ReadUUID(),
            SessionID = p.ReadUUID(),
            ClassifiedID = p.ReadUUID(),
            Category = p.ReadInt32(),
            Name = p.ReadStringLen8(),
            Description = p.ReadStringLen16(),
            ParcelID = new ParcelID(p.ReadBytes(16), 0),
            ParentEstate = p.ReadInt32(),
            SnapshotID = p.ReadUUID(),
            PosGlobal = p.ReadVector3d(),
            ClassifiedFlags = p.ReadUInt8(),
            PriceForListing = p.ReadInt32()
        };

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(ClassifiedID);
            p.WriteInt32(Category);
            p.WriteStringLen8(Name);
            p.WriteStringLen16(Description);
            p.WriteBytes(ParcelID.GetBytes());
            p.WriteInt32(ParentEstate);
            p.WriteUUID(SnapshotID);
            p.WriteVector3d(PosGlobal);
            p.WriteUInt8(ClassifiedFlags);
            p.WriteInt32(PriceForListing);
        }
    }
}
