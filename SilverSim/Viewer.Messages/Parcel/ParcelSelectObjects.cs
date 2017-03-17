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

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelSelectObjects)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ParcelSelectObjects : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public Int32 LocalID;
        public UInt32 ReturnType;

        public List<UUID> ReturnIDs = new List<UUID>();

        public ParcelSelectObjects()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ParcelSelectObjects m = new ParcelSelectObjects();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.LocalID = p.ReadInt32();
            m.ReturnType = p.ReadUInt32();

            uint cnt;
            cnt = p.ReadUInt8();
            for (uint i = 0; i < cnt; ++i)
            {
                m.ReturnIDs.Add(p.ReadUUID());
            }

            return m;
        }
    }
}
