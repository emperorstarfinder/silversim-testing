﻿/*

ArribaSim is distributed under the terms of the
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;

namespace ArribaSim.Linden.Messages.Parcel
{
    public class ParcelBuy : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UUID GroupID;
        public bool IsGroupOwned;
        public bool RemoveContribution;
        public Int32 LocalID;
        public bool IsFinal;
        public Int32 Price;
        public Int32 Area;

        public ParcelBuy()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.ParcelBuy;
            }
        }

        public static Message Decode(UDPPacket p)
        {
            ParcelBuy m = new ParcelBuy();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            m.GroupID = p.ReadUUID();
            m.IsGroupOwned = p.ReadBoolean();
            m.RemoveContribution = p.ReadBoolean();
            m.LocalID = p.ReadInt32();
            m.IsFinal = p.ReadBoolean();
            m.Price = p.ReadInt32();
            m.Area = p.ReadInt32();

            return m;
        }
    }
}
