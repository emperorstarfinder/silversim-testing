﻿/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelMediaUpdate)]
    [Reliable]
    [Trusted]
    public class ParcelMediaUpdate : Message
    {
        public string MediaURL;
        public UUID MediaID;
        public bool MediaAutoScale;
        public string MediaType;
        public string MediaDesc;
        public Int32 MediaWidth;
        public Int32 MediaHeight;
        public byte MediaLoop;

        public ParcelMediaUpdate()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteStringLen8(MediaURL);
            p.WriteUUID(MediaID);
            p.WriteBoolean(MediaAutoScale);
            p.WriteStringLen8(MediaType);
            p.WriteStringLen8(MediaDesc);
            p.WriteInt32(MediaWidth);
            p.WriteInt32(MediaHeight);
            p.WriteUInt8(MediaLoop);
        }
    }
}
