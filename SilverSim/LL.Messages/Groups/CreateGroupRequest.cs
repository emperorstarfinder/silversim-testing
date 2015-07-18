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

namespace SilverSim.LL.Messages.Groups
{
    [UDPMessage(MessageType.CreateGroupRequest)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class CreateGroupRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public string Name;
        public string Charter;
        public bool ShowInList;
        public UUID InsigniaID;
        public int MembershipFee;
        public bool OpenEnrollment;
        public bool AllowPublish;
        public bool MaturePublish;

        public CreateGroupRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            CreateGroupRequest m = new CreateGroupRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.Name = p.ReadStringLen8();
            m.Charter = p.ReadStringLen16();
            m.ShowInList = p.ReadBoolean();
            m.InsigniaID = p.ReadUUID();
            m.MembershipFee = p.ReadInt32();
            m.OpenEnrollment = p.ReadBoolean();
            m.AllowPublish = p.ReadBoolean();
            m.MaturePublish = p.ReadBoolean();

            return m;
        }
    }
}
