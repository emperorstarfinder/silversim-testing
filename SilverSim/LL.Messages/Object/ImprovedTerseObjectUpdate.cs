﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;

namespace SilverSim.LL.Messages.Object
{
    public class ImprovedTerseObjectUpdate : Message
    {
        public GridVector GridPosition;
        public UInt16 TimeDilation = 0;

        public class ObjData
        {
            public ObjData()
            {

            }

            public byte[] Data;
            public byte[] TextureEntry;
        }

        public List<ObjData> ObjectData = new List<ObjData>();

        public ImprovedTerseObjectUpdate()
        {

        }

        public override MessageType Number
        {
            get
            {
                return MessageType.ImprovedTerseObjectUpdate;
            }
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUInt64(GridPosition.RegionHandle);
            p.WriteUInt16(TimeDilation);
            p.WriteUInt8((byte)ObjectData.Count);
            foreach (ObjData d in ObjectData)
            {
                p.WriteUInt8((byte)d.Data.Length);
                p.WriteBytes(d.Data);
                p.WriteUInt16((UInt16)d.TextureEntry.Length);
                p.WriteBytes(d.TextureEntry);
            }
        }
    }
}
