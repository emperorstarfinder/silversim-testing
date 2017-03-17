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

namespace SilverSim.Viewer.Messages.Search
{
    [UDPMessage(MessageType.AvatarPickerReply)]
    [Reliable]
    [Trusted]
    public class AvatarPickerReply : Message
    {
        public UUID AgentID;
        public UUID QueryID;

        public struct DataEntry
        {
            public UUID AvatarID;
            public string FirstName;
            public string LastName;
        }

        public List<DataEntry> Data = new List<DataEntry>();

        public AvatarPickerReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(QueryID);
            p.WriteUInt8((byte)Data.Count);
            foreach (DataEntry d in Data)
            {
                p.WriteUUID(d.AvatarID);
                p.WriteStringLen8(d.FirstName);
                p.WriteStringLen8(d.LastName);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            AvatarPickerReply m = new AvatarPickerReply();
            m.AgentID = p.ReadUUID();
            m.QueryID = p.ReadUUID();
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                DataEntry d = new DataEntry();
                d.AvatarID = p.ReadUUID();
                d.FirstName = p.ReadStringLen8();
                d.LastName = p.ReadStringLen8();
                m.Data.Add(d);
            }
            return m;
        }
    }
}
