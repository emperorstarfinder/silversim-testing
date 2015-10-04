﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.RequestObjectPropertiesFamily)]
    [Reliable]
    [NotTrusted]
    public class RequestObjectPropertiesFamily : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public UInt32 RequestFlags = 0;
        public UUID ObjectID = UUID.Zero;

        public RequestObjectPropertiesFamily()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            RequestObjectPropertiesFamily m = new RequestObjectPropertiesFamily();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.RequestFlags = p.ReadUInt32();
            m.ObjectID = p.ReadUUID();
            return m;
        }
    }
}
