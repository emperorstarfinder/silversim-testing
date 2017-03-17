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

using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Primitive;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Xml;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        void Cap_RenderMaterials(HttpRequest httpreq)
        {
            if (httpreq.CallerIP != RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            switch (httpreq.Method)
            {
                case "GET":
                    Cap_RenderMaterials_GET(httpreq);
                    break;

                case "POST":
                case "PUT":
                    Cap_RenderMaterials_POST(httpreq);
                    break;

                default:
                    httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method Not Allowed");
                    break;
            }
        }

        void Cap_RenderMaterials_GET(HttpRequest httpreq)
        {
            using (HttpResponse httpres = httpreq.BeginResponse("application/llsd+xml"))
            {
                byte[] matdata = Scene.MaterialsData;
                httpres.GetOutputStream().Write(matdata, 0, matdata.Length);
            }
        }

        void Cap_RenderMaterials_POST(HttpRequest httpreq)
        {
            Map reqmap;
            try
            {
                reqmap = LlsdXml.Deserialize(httpreq.Body) as Map;
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace);
                httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                return;
            }
            if (null == reqmap)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            List<Material> materials = new List<Material>();
            if(reqmap.ContainsKey("Zipped"))
            {
                AnArray zippedDataArray;
                Map zippedDataMap;
                try
                {
                    using (MemoryStream ms = new MemoryStream((BinaryData)reqmap["Zipped"]))
                    {
                        using (GZipStream gz = new GZipStream(ms, CompressionMode.Decompress))
                        {
                            IValue inp = LlsdXml.Deserialize(gz);
                            zippedDataArray = inp as AnArray;
                            zippedDataMap = inp as Map;
                        }
                    }
                }
                catch (Exception e)
                {
                    m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace);
                    httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                    return;
                }
                if (null == zippedDataArray && null == zippedDataMap)
                {
                    httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted Zipped LLSD-XML");
                    return;
                }

                if(null != zippedDataArray)
                {
                    foreach (IValue v in zippedDataArray)
                    {
                        try
                        {
                            materials.Add(Scene.GetMaterial(v.AsUUID));
                        }
                        catch
                        {
                            /* adding faled due to duplicate */
                        }
                    }
                }
                else if(null != zippedDataMap &&
                    zippedDataMap.ContainsKey("FullMaterialsPerFace"))
                {
                    AnArray faceData = zippedDataMap["FullMaterialsPerFace"] as AnArray;
                    if (null != faceData)
                    {
                        foreach (IValue face_iv in faceData)
                        {
                            Map faceDataMap = face_iv as Map;
                            if(null == faceDataMap)
                            {
                                continue;
                            }

                            try
                            {
                                uint primLocalID = faceDataMap["ID"].AsUInt;
                                UUID matID = UUID.Random;
                                Material mat;
                                try
                                {
                                    matID = UUID.Random;
                                    mat = new Material(matID, faceDataMap["Material"] as Map);
                                }
                                catch
                                {
                                    matID = UUID.Zero;
                                    mat = null;
                                }
                                if (mat != null)
                                {
                                    Scene.StoreMaterial(mat);
                                    continue;
                                }
                                ObjectPart p = Scene.Primitives[primLocalID];
                                if (faceDataMap.ContainsKey("Face"))
                                {
                                    uint face = faceDataMap["Face"].AsUInt;
                                    TextureEntryFace te = p.TextureEntry[face];
                                    te.MaterialID = matID;
                                }
                                else
                                {
                                    TextureEntryFace te = p.TextureEntry.DefaultTexture;
                                    te.MaterialID = matID;
                                    p.TextureEntry.DefaultTexture = te;
                                }
                            }
                            catch
                            {
                                /* no action possible */
                            }
                        }
                    }
                }
            }

            byte[] buf;
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream gz = new GZipStream(ms, CompressionMode.Compress))
                {
                    using (XmlTextWriter writer = gz.UTF8XmlTextWriter())
                    {
                        writer.WriteStartElement("llsd");
                        writer.WriteStartElement("array");
                        foreach (Material matdata in materials)
                        {
                            writer.WriteStartElement("map");
                            writer.WriteNamedValue("key", "ID");
                            writer.WriteNamedValue("uuid", matdata.MaterialID);
                            writer.WriteNamedValue("key", "Material");
                            matdata.WriteMap(writer);
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }
                }
                buf = ms.ToArray();
            }

            using (HttpResponse httpres = httpreq.BeginResponse("application/llsd+xml"))
            {
                using (XmlTextWriter writer = httpres.GetOutputStream().UTF8XmlTextWriter())
                {
                    writer.WriteStartElement("llsd");
                    writer.WriteNamedValue("key", "Zipped");
                    writer.WriteNamedValue("binary", buf);
                    writer.WriteEndElement();
                }
            }
        }
    }
}
