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
using System.Linq;
using System.Text;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.UpdateGroupInfo)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class UpdateGroupInfo : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UUID GroupID;
        public string Charter;
        public bool ShowInList;
        public UUID InsigniaID;
        public int MembershipFee;
        public bool OpenEnrollment;
        public bool AllowPublish;
        public bool MaturePublish;

        public UpdateGroupInfo()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            UpdateGroupInfo m = new UpdateGroupInfo();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.Charter = p.ReadStringLen16();
            m.ShowInList = p.ReadBoolean();
            m.InsigniaID = p.ReadUUID();
            m.MembershipFee = p.ReadInt32();
            m.OpenEnrollment = p.ReadBoolean();
            m.AllowPublish = p.ReadBoolean();
            m.MaturePublish = p.ReadBoolean();
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(GroupID);
            p.WriteStringLen16(Charter);
            p.WriteBoolean(ShowInList);
            p.WriteUUID(InsigniaID);
            p.WriteInt32(MembershipFee);
            p.WriteBoolean(OpenEnrollment);
            p.WriteBoolean(AllowPublish);
            p.WriteBoolean(MaturePublish);
        }
    }
}
