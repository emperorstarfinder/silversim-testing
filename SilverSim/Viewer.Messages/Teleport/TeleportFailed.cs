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
    [UDPMessage(MessageType.TeleportFailed)]
    [Reliable]
    [Trusted]
    public class TeleportFailed : Message
    {
        public UUID AgentID;
        public string Reason;

        public struct AlertInfoEntry
        {
            public string Message;
            public string ExtraParams;
        }

        public List<AlertInfoEntry> AlertInfo = new List<AlertInfoEntry>();

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteStringLen8(Reason);
            p.WriteUInt8((byte)AlertInfo.Count);
            foreach(var e in AlertInfo)
            {
                p.WriteStringLen8(e.Message);
                p.WriteStringLen8(e.ExtraParams);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            var m = new TeleportFailed
            {
                AgentID = p.ReadUUID(),
                Reason = p.ReadStringLen8()
            };
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                var e = new AlertInfoEntry
                {
                    Message = p.ReadStringLen8(),
                    ExtraParams = p.ReadStringLen8()
                };
                m.AlertInfo.Add(e);
            }
            return m;
        }
    }
}
