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
        public UUID ObjectID = UUID.Zero;
        public Vector3 GrabOffsetInitial = Vector3.Zero;
        public Vector3 GrabPosition = Vector3.Zero;
        public UInt32 TimeSinceLast;

        public List<Data> ObjectData = new List<Data>();

        public static Message Decode(UDPPacket p)
        {
            var m = new ObjectGrabUpdate
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID(),
                ObjectID = p.ReadUUID(),
                GrabOffsetInitial = p.ReadVector3f(),
                GrabPosition = p.ReadVector3f(),
                TimeSinceLast = p.ReadUInt32()
            };
            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.ObjectData.Add(new Data
                {
                    UVCoord = p.ReadVector3f(),
                    STCoord = p.ReadVector3f(),
                    FaceIndex = p.ReadInt32(),
                    Position = p.ReadVector3f(),
                    Normal = p.ReadVector3f(),
                    Binormal = p.ReadVector3f()
                });
            }
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(ObjectID);
            p.WriteVector3f(GrabOffsetInitial);
            p.WriteVector3f(GrabPosition);
            p.WriteUInt32(TimeSinceLast);

            p.WriteUInt8((byte)ObjectData.Count);
            foreach (var d in ObjectData)
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
