﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectGrabUpdate)]
    [Reliable]
    [NotTrusted]
    public class ObjectGrabUpdate : Message
    {
        public struct Data
        {
            public Vector3 UVCoord;
            public Vector3 STCoord;
            public Int32 FaceIndex;
            public Vector3 Position;
            public Vector3 Normal;
            public Vector3 Binormal;
        }

        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UInt32 ObjectLocalID;
        public Vector3 GrabOffsetInitial = Vector3.Zero;
        public Vector3 GrabPosition = Vector3.Zero;
        public UInt32 TimeSinceLast;

        public List<Data> ObjectData = new List<Data>();

        public ObjectGrabUpdate()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectGrabUpdate m = new ObjectGrabUpdate();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ObjectLocalID = p.ReadUInt32();
            m.GrabOffsetInitial = p.ReadVector3f();
            m.GrabPosition = p.ReadVector3f();
            m.TimeSinceLast = p.ReadUInt32();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                Data d = new Data();
                d.UVCoord = p.ReadVector3f();
                d.STCoord = p.ReadVector3f();
                d.FaceIndex = p.ReadInt32();
                d.Position = p.ReadVector3f();
                d.Normal = p.ReadVector3f();
                d.Binormal = p.ReadVector3f();
                m.ObjectData.Add(d);
            }
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt32(ObjectLocalID);
            p.WriteVector3f(GrabOffsetInitial);
            p.WriteVector3f(GrabPosition);
            p.WriteUInt32(TimeSinceLast);

            p.WriteUInt8((byte)ObjectData.Count);
            foreach (Data d in ObjectData)
            {
                p.WriteVector3f(d.UVCoord);
                p.WriteVector3f(d.STCoord);
                p.WriteInt32(d.FaceIndex);
                p.WriteVector3f(d.Position);
                p.WriteVector3f(d.Normal);
                p.WriteVector3f(d.Binormal);
            }
        }
    }
}
