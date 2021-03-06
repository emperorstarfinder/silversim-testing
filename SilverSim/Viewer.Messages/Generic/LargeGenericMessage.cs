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
using System.Linq;
using MapType = SilverSim.Types.Map;

namespace SilverSim.Viewer.Messages.Generic
{
    [EventQueueGet("LargeGenericMessage")]
    [NotTrusted]
    public sealed class LargeGenericMessage : GenericMessageFormat
    {
        public override MessageType Number => MessageType.LargeGenericMessage;

        public override IValue SerializeEQG()
        {
            var paramList = new AnArray();
            foreach(byte[] p in ParamList)
            {
                paramList.Add(new MapType { { "Parameter", p.FromUTF8Bytes() } });
            }

            return new MapType
            {
                { "AgentData",
                    new AnArray
                    {
                        new MapType
                        {
                            { "AgentID", AgentID },
                            { "SessionID", SessionID },
                            { "TransactionID", TransactionID }
                        }
                    }
                },
                { "MethodData",
                    new AnArray
                    {
                        new MapType
                        {
                            { "Method", Method },
                            { "Invoice", Invoice }
                        }
                    }
                },
                { "ParamList", paramList }
            };
        }

        public static Message DeserializeEQG(IValue iv)
        {
            var m = (MapType)iv;
            var agentData = (MapType)((AnArray)m["AgentData"])[0];
            var methodData = (MapType)((AnArray)m["MethodData"])[0];
            var paramList = (AnArray)m["ParamList"];
            var res = new LargeGenericMessage
            {
                AgentID = agentData["AgentID"].AsUUID,
                SessionID = agentData["SessionID"].AsUUID,
                TransactionID = agentData["TransactionID"].AsUUID,
                Method = methodData["Method"].ToString(),
                Invoice = methodData["Invoice"].AsUUID
            };

            foreach(var p in paramList.OfType<MapType>())
            {
                res.ParamList.Add(p["Parameter"].ToString().ToUTF8Bytes());
            }
            return res;
        }
    }
}
