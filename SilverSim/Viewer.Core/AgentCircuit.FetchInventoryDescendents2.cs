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
using SilverSim.Types;
using SilverSim.Types.Inventory;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        private void WriteInventoryFolderContent(XmlTextWriter writer, InventoryFolderContent folder,
            bool fetch_folders,
            bool fetch_items, List<InventoryItem> linkeditems)
        {
            writer.WriteStartElement("map");
            writer.WriteKeyValuePair("agent_id", folder.Owner.ID);
            writer.WriteKeyValuePair("descendents", folder.Folders.Count + folder.Items.Count);
            writer.WriteKeyValuePair("folder_id", folder.FolderID);
            if(fetch_folders)
            {
                writer.WriteStartElement("key");
                writer.WriteValue("categories");
                writer.WriteEndElement();
                writer.WriteStartElement("array");
                foreach(var childfolder in folder.Folders)
                {
                    writer.WriteStartElement("map");
                    writer.WriteKeyValuePair("folder_id", childfolder.ID);
                    writer.WriteKeyValuePair("parent_id", childfolder.ParentFolderID);
                    writer.WriteKeyValuePair("name", childfolder.Name);
                    if (childfolder.InventoryType != InventoryType.Folder)
                    {
                        writer.WriteKeyValuePair("type", (byte)childfolder.InventoryType);
                    }
                    else
                    {
                        writer.WriteKeyValuePair("type", -1);
                    }
                    writer.WriteKeyValuePair("preferred_type", -1);
                    writer.WriteKeyValuePair("version", childfolder.Version);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            if(fetch_items)
            {
                writer.WriteStartElement("key");
                writer.WriteValue("items");
                writer.WriteEndElement();
                writer.WriteStartElement("array");
                if(linkeditems != null)
                {
                    foreach (var childitem in linkeditems)
                    {
                        writer.WriteStartElement("map");
                        WriteInventoryItem(childitem, writer);
                        writer.WriteEndElement();
                    }
                }
                foreach (var childitem in folder.Items)
                {
                    writer.WriteStartElement("map");
                    WriteInventoryItem(childitem, writer);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            writer.WriteKeyValuePair("owner_id", folder.Owner.ID);
            writer.WriteKeyValuePair("version", folder.Version);
            writer.WriteEndElement();
        }

        private void Cap_FetchInventoryDescendents2(HttpRequest httpreq)
        {
            IValue o;
            if (httpreq.CallerIP != RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            try
            {
                o = LlsdXml.Deserialize(httpreq.Body);
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace);
                httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                return;
            }

            var reqmap = o as Map;
            if (reqmap == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            using (var res = httpreq.BeginResponse("application/llsd+xml"))
            {
                using (var text = res.GetOutputStream().UTF8XmlTextWriter())
                {
                    var badfolders = new Dictionary<UUID, string>();
                    text.WriteStartElement("llsd");
                    text.WriteStartElement("map");
                    var wroteheader = false;

                    var folderRequests = new Dictionary<UUID, List<Map>>();

                    var foldersreqarray = (AnArray)reqmap["folders"];
                    foreach (var iv1 in foldersreqarray)
                    {
                        var itemmap = iv1 as Map;
                        if (itemmap == null)
                        {
                            continue;
                        }

                        if (!itemmap.ContainsKey("folder_id") ||
                            !itemmap.ContainsKey("fetch_folders") ||
                            !itemmap.ContainsKey("fetch_items"))
                        {
                            continue;
                        }
                        var ownerid = itemmap["owner_id"].AsUUID;
                        if (!folderRequests.ContainsKey(ownerid))
                        {
                            folderRequests[ownerid] = new List<Map>();
                        }
                        folderRequests[ownerid].Add(itemmap);
                    }

                    var folderContents = new Dictionary<UUID, Dictionary<UUID, InventoryFolderContent>>();
                    foreach (var req in folderRequests)
                    {
                        var list = new List<UUID>();
                        foreach (var fm in req.Value)
                        {
                            if (fm["folder_id"].AsUUID != UUID.Zero)
                            {
                                list.Add(fm["folder_id"].AsUUID);
                            }
                        }
                        try
                        {
                            var folderContentRes = Agent.InventoryService.Folder.Content[AgentID, list.ToArray()];
                            foreach (var folderContent in folderContentRes)
                            {
                                if (!folderContents.ContainsKey(req.Key))
                                {
                                    folderContents.Add(req.Key, new Dictionary<UUID, InventoryFolderContent>());
                                }
                                folderContents[req.Key][folderContent.FolderID] = folderContent;
                            }
                        }
                        catch
                        {
                            /* no action required */
                        }
                    }

                    foreach (var iv in foldersreqarray)
                    {
                        var itemmap = iv as Map;
                        if (iv == null)
                        {
                            continue;
                        }

                        if (!itemmap.ContainsKey("folder_id") ||
                            !itemmap.ContainsKey("fetch_folders") ||
                            !itemmap.ContainsKey("fetch_items"))
                        {
                            continue;
                        }

                        var folderid = itemmap["folder_id"].AsUUID;
                        var ownerid = itemmap["owner_id"].AsUUID;
                        bool fetch_folders = itemmap["fetch_folders"].AsBoolean;
                        bool fetch_items = itemmap["fetch_items"].AsBoolean;

                        if (folderContents.ContainsKey(ownerid))
                        {
                            if (folderContents[ownerid].ContainsKey(folderid))
                            {
                                var fc = folderContents[ownerid][folderid];
                                var linkeditems = new List<InventoryItem>();
                                var linkeditemids = new List<UUID>();

                                foreach (var item in fc.Items)
                                {
                                    if (item.AssetType == Types.Asset.AssetType.Link)
                                    {
                                        linkeditemids.Add(item.AssetID);
                                    }
                                }

                                try
                                {
                                    linkeditems = Agent.InventoryService.Item[ownerid, linkeditemids];
                                }
                                catch
                                {
                                    /* no action required */
                                }
                                if (!wroteheader)
                                {
                                    wroteheader = true;
                                    text.WriteNamedValue("key", "folders");
                                    text.WriteStartElement("array");
                                }

                                WriteInventoryFolderContent(text, fc, fetch_folders, fetch_items, linkeditems);
                            }
                            else
                            {
                                badfolders.Add(folderid, "Not found");
                            }
                        }
                        else
                        {
                            badfolders.Add(folderid, "Not found");
                        }
                    }
                    if (wroteheader)
                    {
                        text.WriteEndElement();
                    }
                    if (badfolders.Count != 0)
                    {
                        text.WriteNamedValue("key", "bad_folders");
                        text.WriteStartElement("array");
                        foreach (KeyValuePair<UUID, string> id in badfolders)
                        {
                            text.WriteStartElement("map");
                            text.WriteNamedValue("folder_id", id.Key);
                            text.WriteNamedValue("error", id.Value);
                            text.WriteEndElement();
                        }
                        text.WriteEndElement();
                    }
                    text.WriteEndElement();
                    text.WriteEndElement();
                }
            }
        }
    }
}
