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
using System.Net;

namespace SilverSim.Viewer.Messages.Circuit
{
    [EventQueueGet("EstablishAgentCommunication")]
    [Trusted]
    public class EstablishAgentCommunication : Message
    {
        public UUID AgentID;
        public IPEndPoint SimIpAndPort = new IPEndPoint(0, 0);
        public string SeedCapability;
        public GridVector GridPosition;
        public GridVector RegionSize;

        public override IValue SerializeEQG() => new Types.Map
        {
            { "agent-id", AgentID },
            { "sim-ip-and-port", SimIpAndPort.ToString() },
            { "seed-capability", SeedCapability },
            { "region-handle", new BinaryData(GridPosition.AsBytes) },
            { "region-size-x", RegionSize.X },
            { "region-size-y", RegionSize.Y }
        };

        public static Message DeserializeIQG(IValue value)
        {
            var map = (Types.Map)value;
            byte[] reghandle = (BinaryData)map["Handle"];
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(reghandle);
            }
            return new EstablishAgentCommunication
            {
                AgentID = map["agent-id"].AsUUID,
                SimIpAndPort = IPEndPointHelpers.CreateIPEndPoint(map["sim-ip-and-port"].ToString()),
                SeedCapability = map["seed-capability"].ToString(),
                RegionSize = new GridVector(map["region-size-x"].AsUInt, map["region-size-y"].AsUInt),
                GridPosition = new GridVector(BitConverter.ToUInt64(reghandle, 0))
            };
        }
    }
}
