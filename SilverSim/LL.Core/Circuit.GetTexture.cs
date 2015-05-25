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

using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        void Cap_GetTexture(HttpRequest httpreq)
        {
            string[] parts = httpreq.RawUrl.Substring(1).Split('/');

            if (httpreq.Method != "GET")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method Not Allowed");
                return;
            }

            if (parts.Length < 4)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            UUID textureID;
            if(parts[3].Substring(0, 1) != "?")
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            string texturereq = parts[3].Substring(1);
            string[] texreq = texturereq.Split('&');
            string texID = string.Empty;

            foreach(string texreqentry in texreq)
            {
                if(texreqentry.StartsWith("texture_id="))
                {
                    texID = texreqentry.Substring(11);
                }
            }
            try
            {
                textureID = UUID.Parse(texID);
            }
            catch
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            bool j2k_accepted = true;
            if(httpreq.ContainsHeader("Accept"))
            {
                j2k_accepted = false;
                parts = httpreq["Accept"].Split(',');
                foreach(string p in parts)
                {
                    string[] comps = p.Trim().Split(';');
                    if (comps[0].Trim() == "image/x-j2c")
                    {
                        j2k_accepted = true;
                    }
                }
                if (!j2k_accepted)
                {
                    httpreq.ErrorResponse(HttpStatusCode.NotAcceptable, "Not acceptable");
                    return;
                }
            }

            AssetData asset;
            try
            {
                /* let us prefer the sim asset service */
                asset = Scene.AssetService[textureID];
            }
            catch(Exception e1)
            {
                try
                {
                    asset = Agent.AssetService[textureID];
                    try
                    {
                        /* try to store the asset on our sim's asset service */
                        asset.Temporary = true;
                        Scene.AssetService.Store(asset);
                    }
                    catch(Exception e3)
                    {
                        m_Log.DebugFormat("Failed to store asset {0} locally (Cap_GetTexture): {1}", textureID, e3.Message);
                    }
                }
                catch(Exception e2)
                {
                    if (m_Server.LogAssetFailures)
                    {
                        m_Log.DebugFormat("Failed to download image {0} (Cap_GetTexture): {1} or {2}\nA: {3}\nB: {4}", textureID, e1.Message, e2.Message, e1.StackTrace.ToString(), e2.StackTrace.ToString());
                    }
                    httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                    return;
                }
            }

            if(asset.Type != AssetType.Texture)
            {
                if(asset.Type != AssetType.Mesh)
                {
                    m_Log.DebugFormat("Failed to download image (Cap_GetTexture): Viewer for AgentID {0} tried to download non-texture asset ({1})", AgentID, asset.Type.ToString());
                }
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            if (httpreq.ContainsHeader("Range"))
            {
                HttpResponse httpres;
                Stream o;
                List<KeyValuePair<int, int>> contentranges = new List<KeyValuePair<int, int>>();

                string[] ranges = httpreq["Range"].Split(' ');
                if(ranges.Length > 1)
                {
                    httpres = httpreq.BeginResponse("image/x-j2c");
                    o = httpres.GetOutputStream(asset.Data.LongLength);
                    o.Write(asset.Data, 0, asset.Data.Length);
                    httpres.Close();
                    return;
                }

                string[] p = ranges[0].Split('=');
                if(p.Length != 2)
                {
                    httpreq.ErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
                    m_Log.WarnFormat("Requested range for GetTexture is not decoded. {0}", ranges[0]);
                    return;
                }
                string[] v = p[1].Split('-');
                if(v.Length != 2)
                {
                    httpreq.ErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
                    m_Log.WarnFormat("Requested range for GetTexture is not decoded. {0} see {1}", ranges[0], p[1]);
                    return;
                }

                if(p[0] != "bytes")
                {
                    httpres = httpreq.BeginResponse("image/x-j2c");
                    o = httpres.GetOutputStream(asset.Data.LongLength);
                    o.Write(asset.Data, 0, asset.Data.Length);
                    httpres.Close();
                    return;
                }

                int start;
                int end;

                try
                {
                    start = int.Parse(v[0]);
                    if (string.IsNullOrEmpty(v[1]))
                    {
                        end = asset.Data.Length - 1;
                    }
                    else
                    {
                        end = int.Parse(v[1]);
                    }

                    /* The following check is regarding some weirdness of some viewers trying to retrieve data past the file size.
                     * Yet, RFC2616 requires a RequestedRangeNotSatisfiable here but those viewers would not accept it.
                     */
                    if(start >= asset.Data.Length)
                    {
                        httpreq.BeginResponse(HttpStatusCode.PartialContent, "Partial Content", "image/x-j2c").Close();
                        return;
                    }
                    if (start > end)
                    {
                        httpreq.ErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
                        return;
                    }
                    if(end > asset.Data.Length - 1)
                    {
                        end = asset.Data.Length - 1;
                    }
                    if (end >= asset.Data.Length)
                    {
                        httpreq.ErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
                        return;
                    }
                    if (start == 0 && end == asset.Data.Length - 1)
                    {
                        httpres = httpreq.BeginResponse("image/x-j2c");
                        o = httpres.GetOutputStream(asset.Data.LongLength);
                        o.Write(asset.Data, 0, asset.Data.Length);
                        httpres.Close();
                        return;
                    }
                }
                catch(Exception e)
                {
                    m_Log.Debug("Exception when parsing requested range (GetTexture)", e);
                    httpreq.ErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, "Requested range not satisfiable");
                    return;
                }

                httpres = httpreq.BeginResponse(HttpStatusCode.PartialContent, "Partial Content", "image/x-j2c");
                httpres.Headers["Content-Range"] = string.Format("bytes {0}-{1}/{2}", start, end, asset.Data.Length);
                o = httpres.GetOutputStream(end - start + 1);
                o.Write(asset.Data, start, end - start + 1);
                httpres.Close();
            }
            else
            {
                HttpResponse httpres = httpreq.BeginResponse("image/x-j2c");
                Stream o = httpres.GetOutputStream(asset.Data.LongLength);
                o.Write(asset.Data, 0, asset.Data.Length);
                httpres.Close();
            }
        }
    }
}
