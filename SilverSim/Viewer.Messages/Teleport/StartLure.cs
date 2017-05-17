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
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Teleport
{
    [UDPMessage(MessageType.StartLure)]
    [Reliable]
    [NotTrusted]
    public class StartLure : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public byte LureType;
        public string Message;
        public List<UUID> TargetData = new List<UUID>();

        public static Message Decode(UDPPacket p)
        {
            var m = new StartLure()
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID(),
                LureType = p.ReadUInt8(),
                Message = p.ReadStringLen8()
            };
            uint count = p.ReadUInt8();
            for (uint i = 0; i < count; ++i)
            {
                m.TargetData.Add(p.ReadUUID());
            }

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt8(LureType);
            p.WriteStringLen8(Message);
            p.WriteUInt8((byte)TargetData.Count);
            foreach(var id in TargetData)
            {
                p.WriteUUID(id);
            }
        }
    }
}
