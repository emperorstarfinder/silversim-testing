﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Profile
{
    [UDPMessage(MessageType.UserInfoReply)]
    [Reliable]
    [Trusted]
    public class UserInfoReply : Message
    {
        public UUID AgentID;
        public bool IMViaEmail;
        public string DirectoryVisibility;
        public string EMail = string.Empty;

        public UserInfoReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteBoolean(IMViaEmail);
            p.WriteStringLen8(DirectoryVisibility);
            p.WriteStringLen8(EMail);
        }

        public static Message Decode(UDPPacket p)
        {
            UserInfoReply m = new UserInfoReply();
            m.AgentID = p.ReadUUID();
            m.IMViaEmail = p.ReadBoolean();
            m.DirectoryVisibility = p.ReadStringLen8();
            m.EMail = p.ReadStringLen8();
            return m;
        }
    }
}
