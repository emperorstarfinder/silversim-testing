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

using SilverSim.Http.Client;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Json;
using SilverSim.Types.StructuredData.XmlRpc;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace SilverSim.Main.Common.Rpc
{
    public static class RPC
    {
        public static IValue DoJson20RpcRequest(string url, string method, string jsonId, IValue param, int timeoutms, X509CertificateCollection clientCertificates = null)
        {
            string jsonReq = Json20Rpc.SerializeRequest(method, jsonId, param);
            using (Stream res = new HttpClient.Post(url, "application/json-rpc", jsonReq)
            {
                TimeoutMs = timeoutms,
                ClientCertificates = clientCertificates
            }.ExecuteStreamRequest())
            {
                return Json20Rpc.DeserializeResponse(res);
            }
        }

        public static XmlRpc.XmlRpcResponse DoXmlRpcRequest(string url, XmlRpc.XmlRpcRequest req, int timeoutms, X509CertificateCollection clientCertificates = null)
        {
            byte[] rpcdata = req.Serialize();
            using (Stream res = new HttpClient.Post(url, "text/xml", rpcdata.Length, (Stream s) => s.Write(rpcdata, 0, rpcdata.Length))
            {
                TimeoutMs = timeoutms,
                ClientCertificates = clientCertificates
            }.ExecuteStreamRequest())
            {
                return XmlRpc.DeserializeResponse(res);
            }
        }
    }
}
