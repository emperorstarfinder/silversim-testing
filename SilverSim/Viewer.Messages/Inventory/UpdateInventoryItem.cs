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
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.UpdateInventoryItem)]
    [Reliable]
    [NotTrusted]
    public class UpdateInventoryItem : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID TransactionID;

        public struct InventoryDataEntry
        {
            public UUID ItemID;
            public UUID FolderID;
            public UInt32 CallbackID;
            public UUID CreatorID;
            public UUID OwnerID;
            public UUID GroupID;
            public InventoryPermissionsMask BaseMask;
            public InventoryPermissionsMask OwnerMask;
            public InventoryPermissionsMask GroupMask;
            public InventoryPermissionsMask EveryoneMask;
            public InventoryPermissionsMask NextOwnerMask;
            public bool IsGroupOwned;
            public UUID TransactionID;
            public AssetType Type;
            public InventoryType InvType;
            public InventoryFlags Flags;
            public InventoryItem.SaleInfoData.SaleType SaleType;
            public Int32 SalePrice;
            public string Name;
            public string Description;
            public UInt32 CreationDate;
            public UInt32 CRC;
        }

        public List<InventoryDataEntry> InventoryData = new List<InventoryDataEntry>();

        public static Message Decode(UDPPacket p)
        {
            var m = new UpdateInventoryItem
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID(),
                TransactionID = p.ReadUUID()
            };
            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.InventoryData.Add(new InventoryDataEntry
                {
                    ItemID = p.ReadUUID(),
                    FolderID = p.ReadUUID(),
                    CallbackID = p.ReadUInt32(),
                    CreatorID = p.ReadUUID(),
                    OwnerID = p.ReadUUID(),
                    GroupID = p.ReadUUID(),
                    BaseMask = (InventoryPermissionsMask)p.ReadUInt32(),
                    OwnerMask = (InventoryPermissionsMask)p.ReadUInt32(),
                    GroupMask = (InventoryPermissionsMask)p.ReadUInt32(),
                    EveryoneMask = (InventoryPermissionsMask)p.ReadUInt32(),
                    NextOwnerMask = (InventoryPermissionsMask)p.ReadUInt32(),
                    IsGroupOwned = p.ReadBoolean(),
                    TransactionID = p.ReadUUID(),
                    Type = (AssetType)p.ReadInt8(),
                    InvType = (InventoryType)p.ReadInt8(),
                    Flags = (InventoryFlags)p.ReadUInt32(),
                    SaleType = (InventoryItem.SaleInfoData.SaleType)p.ReadUInt8(),
                    SalePrice = p.ReadInt32(),
                    Name = p.ReadStringLen8(),
                    Description = p.ReadStringLen8(),
                    CreationDate = p.ReadUInt32(),
                    CRC = p.ReadUInt32()
                });
            }

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(TransactionID);
            p.WriteUInt8((byte)InventoryData.Count);
            foreach (var d in InventoryData)
            {
                p.WriteUUID(d.ItemID);
                p.WriteUUID(d.FolderID);
                p.WriteUInt32(d.CallbackID);
                p.WriteUUID(d.CreatorID);
                p.WriteUUID(d.OwnerID);
                p.WriteUUID(d.GroupID);
                p.WriteUInt32((UInt32)d.BaseMask);
                p.WriteUInt32((UInt32)d.OwnerMask);
                p.WriteUInt32((UInt32)d.GroupMask);
                p.WriteUInt32((UInt32)d.EveryoneMask);
                p.WriteUInt32((UInt32)d.NextOwnerMask);
                p.WriteBoolean(d.IsGroupOwned);
                p.WriteUUID(d.TransactionID);
                p.WriteInt8((sbyte)d.Type);
                p.WriteInt8((sbyte)d.InvType);
                p.WriteUInt32((uint)d.Flags);
                p.WriteUInt8((byte)d.SaleType);
                p.WriteInt32(d.SalePrice);
                p.WriteStringLen8(d.Name);
                p.WriteStringLen8(d.Description);
                p.WriteUInt32(d.CreationDate);
                p.WriteUInt32(d.CRC);
            }
        }
    }
}
