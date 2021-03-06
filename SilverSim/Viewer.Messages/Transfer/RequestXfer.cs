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

namespace SilverSim.Viewer.Messages.Transfer
{
    [UDPMessage(MessageType.RequestXfer)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class RequestXfer : Message
    {
        public UInt64 ID;
        public string Filename;
        public byte FilePath;
        public bool DeleteOnCompletion;
        public bool UseBigPackets;
        public UUID VFileID;
        public Int16 VFileType;

        public static Message Decode(UDPPacket p) => new RequestXfer
        {
            ID = p.ReadUInt64(),
            Filename = p.ReadStringLen8(),
            FilePath = p.ReadUInt8(),
            DeleteOnCompletion = p.ReadBoolean(),
            UseBigPackets = p.ReadBoolean(),
            VFileID = p.ReadUUID(),
            VFileType = p.ReadInt16()
        };

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt64(ID);
            p.WriteStringLen8(Filename);
            p.WriteUInt8(FilePath);
            p.WriteBoolean(DeleteOnCompletion);
            p.WriteBoolean(UseBigPackets);
            p.WriteUUID(VFileID);
            p.WriteInt16(VFileType);
        }
    }
}
