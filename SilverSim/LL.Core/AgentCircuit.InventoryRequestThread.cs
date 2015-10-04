﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        #region Fetch Inventory Thread
        private const int MAX_FOLDERS_PER_PACKET = 6;
        private const int MAX_ITEMS_PER_PACKET = 5;

        private void SendAssetNotFound(Messages.Transfer.TransferRequest req)
        {
            Messages.Transfer.TransferInfo res = new Messages.Transfer.TransferInfo();
            res.ChannelType = 2;
            res.Status = -2;
            res.TargetType = (int)req.SourceType;
            res.Params = req.Params;
            res.Size = 0;
            res.TransferID = req.TransferID;
            SendMessage(res);
        }

        private void SendAssetInsufficientPermissions(Messages.Transfer.TransferRequest req)
        {
            Messages.Transfer.TransferInfo res = new Messages.Transfer.TransferInfo();
            res.ChannelType = 2;
            res.Status = -5;
            res.TargetType = (int)req.SourceType;
            res.Params = req.Params;
            res.Size = 0;
            res.TransferID = req.TransferID;
            SendMessage(res);
        }

        private void FetchInventoryThread(object param)
        {
            Thread.CurrentThread.Name = string.Format("LLUDP:Inventory Fetch for CircuitCode {0} / IP {1}", CircuitCode, RemoteEndPoint.ToString());

            while (true)
            {
                Message m;
                if (!m_InventoryThreadRunning)
                {
                    return;
                }
                try
                {
                    m = m_InventoryRequestQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                switch(m.Number)
                {
                    case MessageType.ChangeInventoryItemFlags:
                        {
                            Messages.Inventory.ChangeInventoryItemFlags req = (Messages.Inventory.ChangeInventoryItemFlags)m;
                            if(req.SessionID != SessionID || req.AgentID != AgentID)
                            {
                                break;
                            }

                            foreach(Messages.Inventory.ChangeInventoryItemFlags.InventoryDataEntry d in req.InventoryData)
                            {
                                InventoryItem item;
                                try
                                {
                                    item = Agent.InventoryService.Item[AgentID, d.ItemID];
                                    item.Flags = d.Flags;
                                    Agent.InventoryService.Item.Update(item);
                                }
                                catch
                                {

                                }
                            }
                        }
                        break;

                    case MessageType.TransferRequest:
                        {
                            UUID assetID;
                            bool denyLSLTextViaDirect = false;
                            Messages.Transfer.TransferRequest req = (Messages.Transfer.TransferRequest)m;
                            if (req.SourceType == Messages.Transfer.SourceType.SimInventoryItem)
                            {
                                UUID itemID = new UUID(req.Params, 64);
                                assetID = new UUID(req.Params, 80);
                                InventoryItem item;
                                try
                                {
                                    item = Agent.InventoryService.Item[AgentID, itemID];
                                }
                                catch(Exception e)
                                {
                                    m_Log.DebugFormat("Failed to request inventory asset (TransferRequest) for Agent {0}: {1}", AgentID, e.Message);
                                    SendAssetNotFound(req);
                                    break;
                                }

                                if (item.AssetType == AssetType.LSLText)
                                {
                                    if (0 == ((item.Permissions.Current | item.Permissions.EveryOne) & InventoryPermissionsMask.Modify))
                                    {
                                        SendAssetInsufficientPermissions(req);
                                        break;
                                    }
                                }
                                else if (item.AssetID != assetID)
                                {
                                    m_Log.DebugFormat("Failed to request inventory asset (TransferRequest) for Agent {0}: Provided AssetID != Item AssetID", AgentID);
                                    SendAssetNotFound(req);
                                    break;
                                }
                            }
                            else if(req.SourceType == Messages.Transfer.SourceType.Asset)
                            {
                                assetID = new UUID(req.Params, 0);
                                denyLSLTextViaDirect = true;
                            }
                            else
                            {
                                m_Log.DebugFormat("Failed to request (TransferRequest) for Agent {0}: Provided AssetID != Item AssetID", AgentID);
                                SendAssetNotFound(req);
                                break;
                            }

                            /* let us prefer the scene's asset service */
                            AssetData asset;
                            try
                            {
                                asset = Scene.AssetService[assetID];
                            }
                            catch(Exception e1)
                            {
                                /* let's try the user's asset server */
                                try
                                {
                                    asset = Agent.AssetService[assetID];
                                    try
                                    {
                                        /* let us try to store the asset locally */
                                        asset.Temporary = true;
                                        Scene.AssetService.Store(asset);
                                    }
                                    catch(Exception e3)
                                    {
                                        m_Log.DebugFormat("Failed to store asset {0} locally (TransferPacket): {1}", assetID, e3.Message);
                                    }
                                }
                                catch(Exception e2)
                                {
                                    if (Server.LogAssetFailures)
                                    {
                                        m_Log.DebugFormat("Failed to download asset {0} (TransferPacket): {1} or {2}", assetID, e1.Message, e2.Message);
                                    }
                                    SendAssetNotFound(req);
                                    break;
                                }
                            }

                            if(Server.LogTransferPacket)
                            {
                                m_Log.DebugFormat("Starting to download asset {0} (TransferPacket)", assetID);
                            }
                            if(denyLSLTextViaDirect && asset.Type == AssetType.LSLText)
                            {
                                return;
                            }

                            Messages.Transfer.TransferInfo ti = new Messages.Transfer.TransferInfo();
                            ti.Params = req.Params;
                            ti.ChannelType = 2;
                            ti.Status = 0;
                            ti.TargetType = 0;
                            ti.TransferID = req.TransferID;
                            ti.Size = asset.Data.Length;
                            if(req.SourceType == Messages.Transfer.SourceType.Asset)
                            {
                                ti.Params = new byte[20];
                                assetID.ToBytes(ti.Params, 0);
                                int assetType = (int)asset.Type;
                                byte[] b = BitConverter.GetBytes(assetType);
                                if(!BitConverter.IsLittleEndian)
                                {
                                    Array.Reverse(b);
                                }
                                Array.Copy(b, 0, ti.Params, 16, 4);
                            }
                            else if(req.SourceType == Messages.Transfer.SourceType.SimInventoryItem)
                            {
                                ti.Params = req.Params;
                            }
                            SendMessage(ti);

                            const int MAX_PACKET_SIZE = 1100;
                            int packetNumber = 0;
                            int assetOffset = 0;
                            while(assetOffset < asset.Data.Length)
                            {
                                Messages.Transfer.TransferPacket tp = new Messages.Transfer.TransferPacket();
                                tp.Packet = packetNumber++;
                                tp.ChannelType = 2;
                                tp.TransferID = req.TransferID;
                                if(asset.Data.Length - assetOffset > MAX_PACKET_SIZE)
                                {
                                    tp.Data = new byte[MAX_PACKET_SIZE];
                                    Buffer.BlockCopy(asset.Data, assetOffset, tp.Data, 0, MAX_PACKET_SIZE);
                                    assetOffset += MAX_PACKET_SIZE;
                                    tp.Status = 0;
                                }
                                else 
                                {
                                    tp.Data = new byte[asset.Data.Length - assetOffset];
                                    Buffer.BlockCopy(asset.Data, assetOffset, tp.Data, 0, asset.Data.Length - assetOffset);
                                    tp.Status = 1;
                                    assetOffset = asset.Data.Length;
                                }
                                SendMessage(tp);
                            }
                            if (Server.LogTransferPacket)
                            {
                                m_Log.DebugFormat("Completed download of asset {0} (TransferPacket)", assetID);
                            }
                        }
                        break;

                    case MessageType.CopyInventoryFromNotecard:
                        break;

                    case MessageType.CopyInventoryItem:
                        {
                            Messages.Inventory.CopyInventoryItem req = (Messages.Inventory.CopyInventoryItem)m;
                            if(req.SessionID != SessionID || req.AgentID != AgentID)
                            {
                                break;
                            }

                            foreach (Messages.Inventory.CopyInventoryItem.InventoryDataEntry reqd in req.InventoryData)
                            {
                                InventoryItem item;
                                try
                                {
                                    item = Agent.InventoryService.Item[reqd.OldAgentID, reqd.OldItemID];
                                    if((item.Permissions.Current & InventoryPermissionsMask.Copy) == 0)
                                    {
                                        /* skip item */
                                        continue;
                                    }
                                    item.ID = UUID.Random;
                                    if(reqd.NewName != "")
                                    {
                                        item.Name = reqd.NewName;
                                    }
                                    if(item.Owner.ID != Agent.Owner.ID)
                                    {
                                        if((item.Permissions.Current & InventoryPermissionsMask.Transfer) == 0)
                                        {
                                            continue;
                                        }
                                        item.Permissions.Current = item.Permissions.NextOwner;
                                        item.Permissions.Base = item.Permissions.NextOwner;
                                        item.Permissions.EveryOne &= item.Permissions.NextOwner;
                                    }
                                    item.Owner = Agent.Owner;

                                }
                                catch
                                {
                                    continue;
                                }

                                try
                                {
                                    Agent.InventoryService.Item.Add(item);
                                    SendMessage(new Messages.Inventory.UpdateCreateInventoryItem(AgentID, true, UUID.Zero, item, reqd.CallbackID));
                                }
                                catch
                                {
                                    Messages.Alert.AlertMessage res = new Messages.Alert.AlertMessage();
                                    res.Message = "Failed to copy item";
                                    SendMessage(res);
                                }
                            }
                        }
                        break;

                    case MessageType.CreateInventoryItem:
                        HandleCreateInventoryItem((Messages.Inventory.CreateInventoryItem)m);
                        break;

                    case MessageType.CreateInventoryFolder:
                        {
                            Messages.Inventory.CreateInventoryFolder req = (Messages.Inventory.CreateInventoryFolder)m;
                            if(req.SessionID != SessionID || req.AgentID != AgentID)
                            {
                                break;
                            }

                            try
                            {

                                InventoryFolder folder;
                                folder = Agent.InventoryService.Folder[AgentID, req.ParentFolderID];
                                folder = new InventoryFolder();
                                folder.ID = req.FolderID;
                                folder.InventoryType = req.FolderType;
                                folder.Name = req.FolderName;
                                folder.Owner = Agent.Owner;
                                folder.ParentFolderID = req.ParentFolderID;
                                folder.Version = 1;
                                Agent.InventoryService.Folder.Add(folder);
                            }
                            catch(Exception e)
                            {
                                m_Log.DebugFormat("Cannot create inventory folder: {0} {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace.ToString());
                                SendMessage(new Messages.Alert.AlertMessage("ALERT: CantCreateRequestedInvFolder"));
                            }
                        }
                        break;

                    case MessageType.FetchInventory:
                        {
                            Messages.Inventory.FetchInventory req = (Messages.Inventory.FetchInventory)m;
                            if(req.SessionID != SessionID || req.AgentID != AgentID)
                            {
                                break;
                            }

                            Messages.Inventory.FetchInventoryReply res = null;
                            InventoryItem item;
                            foreach(Messages.Inventory.FetchInventory.InventoryDataEntry d in req.InventoryData)
                            {
                                try
                                {
                                    item = Agent.InventoryService.Item[d.OwnerID, d.ItemID];
                                }
                                catch
                                {
                                    continue;
                                }

                                if(null == res)
                                {
                                    res = new Messages.Inventory.FetchInventoryReply();
                                    res.AgentID = req.AgentID;
                                }

                                Messages.Inventory.FetchInventoryReply.ItemDataEntry rd = new Messages.Inventory.FetchInventoryReply.ItemDataEntry();
                                rd.ItemID = item.ID;
                                rd.FolderID = item.ParentFolderID;
                                rd.CreatorID = item.Creator.ID;
                                rd.OwnerID = item.Owner.ID;
                                rd.GroupID = item.Group.ID;
                                rd.BaseMask = item.Permissions.Current;
                                rd.OwnerMask = item.Permissions.Current;
                                rd.GroupMask = item.Permissions.Group;
                                rd.EveryoneMask = item.Permissions.EveryOne;
                                rd.NextOwnerMask = item.Permissions.NextOwner;
                                rd.IsGroupOwned = false;
                                rd.AssetID = item.AssetID;
                                rd.Type = item.AssetType;
                                rd.InvType = item.InventoryType;
                                rd.Flags = item.Flags;
                                rd.SaleType = item.SaleInfo.Type;
                                rd.SalePrice = item.SaleInfo.Price;
                                rd.Name = item.Name;
                                rd.Description = item.Description;
                                rd.CreationDate = (uint)item.CreationDate.DateTimeToUnixTime();

                                res.ItemData.Add(rd);

                                if(res.ItemData.Count == MAX_ITEMS_PER_PACKET)
                                {
                                    SendMessage(res);
                                    res = null;
                                }
                            }

                            if(null != res)
                            {
                                SendMessage(res);
                            }
                        }
                        break;

                    case MessageType.FetchInventoryDescendents:
                        {
                            Messages.Inventory.FetchInventoryDescendents req = (Messages.Inventory.FetchInventoryDescendents)m;
                            if(req.SessionID != SessionID || req.AgentID != AgentID)
                            {
                                break;
                            }

                            InventoryFolder thisfolder;
                            
                            try
                            {
                                thisfolder = Agent.InventoryService.Folder[req.OwnerID, req.FolderID];
                            }
                            catch
                            {
                                break;
                            }

                            List<InventoryFolder> folders;
                            List<InventoryItem> items;

                            try
                            {
                                folders = Agent.InventoryService.Folder.getFolders(req.OwnerID, req.FolderID);
                            }
                            catch
                            {
                                folders = new List<InventoryFolder>();
                            }

                            try
                            {
                                items = Agent.InventoryService.Folder.getItems(req.OwnerID, req.FolderID);
                            }
                            catch
                            {
                                items = new List<InventoryItem>();
                            }

                            Messages.Inventory.InventoryDescendents res = null;
                            bool message_sent = false;

                            if(req.FetchFolders)
                            {
                                foreach(InventoryFolder folder in folders)
                                {
                                    if(null == res)
                                    {
                                        res = new Messages.Inventory.InventoryDescendents();
                                        res.AgentID = req.AgentID;
                                        res.FolderID = req.FolderID;
                                        res.OwnerID = thisfolder.Owner.ID;
                                        res.Version = thisfolder.Version;
                                        res.Descendents = folders.Count + items.Count;
                                    }
                                    Messages.Inventory.InventoryDescendents.FolderDataEntry d = new Messages.Inventory.InventoryDescendents.FolderDataEntry();
                                    d.FolderID = folder.ID;
                                    d.ParentID = folder.ParentFolderID;
                                    d.Type = folder.InventoryType;
                                    d.Name = folder.Name;
                                    res.FolderData.Add(d);
                                    if(res.FolderData.Count == MAX_FOLDERS_PER_PACKET)
                                    {
                                        SendMessage(res);
                                        message_sent = true;
                                        res = null;
                                    }
                                }
                                if(null != res)
                                {
                                    SendMessage(res);
                                    message_sent = true;
                                    res = null;
                                }
                            }

                            if(req.FetchItems)
                            {
                                foreach(InventoryItem item in items)
                                {
                                    if (null == res)
                                    {
                                        res = new Messages.Inventory.InventoryDescendents();
                                        res.AgentID = req.AgentID;
                                        res.FolderID = req.FolderID;
                                        res.OwnerID = thisfolder.Owner.ID;
                                        res.Version = thisfolder.Version;
                                        res.Descendents = folders.Count + items.Count;
                                    }
                                    Messages.Inventory.InventoryDescendents.ItemDataEntry d = new Messages.Inventory.InventoryDescendents.ItemDataEntry();

                                    d.ItemID = item.ID;
                                    d.FolderID = item.ParentFolderID;
                                    d.CreatorID = item.Creator.ID;
                                    d.OwnerID = item.Owner.ID;
                                    d.GroupID = item.Group.ID;
                                    d.BaseMask = item.Permissions.Current;
                                    d.OwnerMask = item.Permissions.Current;
                                    d.GroupMask = item.Permissions.Group;
                                    d.EveryoneMask = item.Permissions.EveryOne;
                                    d.NextOwnerMask = item.Permissions.NextOwner;
                                    d.IsGroupOwned = item.IsGroupOwned;
                                    d.AssetID = item.AssetID;
                                    d.Type = item.AssetType;
                                    d.InvType = item.InventoryType;
                                    d.Flags = item.Flags;
                                    d.SaleType = item.SaleInfo.Type;
                                    d.SalePrice = item.SaleInfo.Price;
                                    d.Name = item.Name;
                                    d.Description = item.Description;
                                    d.CreationDate = (uint)item.CreationDate.DateTimeToUnixTime();
                                    res.ItemData.Add(d);

                                    if(res.ItemData.Count == MAX_ITEMS_PER_PACKET)
                                    {
                                        SendMessage(res);
                                        message_sent = true;
                                        res = null;
                                    }
                                }
                                if(null != res)
                                {
                                    SendMessage(res);
                                    message_sent = true;
                                    res = null;
                                }
                            }

                            if (!message_sent)
                            {
                                res = new Messages.Inventory.InventoryDescendents();
                                res.AgentID = req.AgentID;
                                res.FolderID = req.FolderID;
                                res.OwnerID = thisfolder.Owner.ID;
                                res.Version = thisfolder.Version;
                                res.Descendents = folders.Count + items.Count;
                                SendMessage(res);
                            }
                        }
                        break;

                    case MessageType.LinkInventoryItem:
                        {
                            Messages.Inventory.LinkInventoryItem req = (Messages.Inventory.LinkInventoryItem)m;
                            if(req.SessionID != SessionID || req.AgentID != AgentID)
                            {
                                break;
                            }

                            InventoryItem item = new InventoryItem();
                            item.Owner = Agent.Owner;
                            item.Creator = Agent.Owner;
                            item.ParentFolderID = req.FolderID;
                            item.Name = req.Name;
                            item.Description = req.Description;
                            item.Flags = 0;
                            item.AssetID = req.OldItemID;
                            item.AssetType = req.AssetType;
                            item.InventoryType = req.InvType;
                            item.Permissions.Base = InventoryPermissionsMask.All | InventoryPermissionsMask.Export;
                            item.Permissions.Current = InventoryPermissionsMask.All | InventoryPermissionsMask.Export;
                            item.Permissions.EveryOne = InventoryPermissionsMask.All;
                            item.Permissions.NextOwner = InventoryPermissionsMask.All | InventoryPermissionsMask.Export;
                            item.Permissions.Group = InventoryPermissionsMask.All | InventoryPermissionsMask.Export;
                            try
                            {
                                Agent.InventoryService.Item.Add(item);
                                SendMessage(new Messages.Inventory.UpdateCreateInventoryItem(AgentID, true, req.TransactionID, item, req.CallbackID));
                            }
                            catch(Exception e)
                            {
                                m_Log.DebugFormat("LinkInventoryItem failed {0} {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace.ToString());

                                Messages.Alert.AlertMessage res = new Messages.Alert.AlertMessage();
                                res.Message = "ALERT: CantCreateInventory";
                                SendMessage(res);
                            }
                        }
                        break;

                    case MessageType.MoveInventoryFolder:
                        {
                            Messages.Inventory.MoveInventoryFolder req = (Messages.Inventory.MoveInventoryFolder)m;
                            if (req.SessionID != SessionID || req.AgentID != AgentID)
                            {
                                break;
                            }

                            foreach (Messages.Inventory.MoveInventoryFolder.InventoryDataEntry d in req.InventoryData)
                            {
                                try
                                {
                                    Agent.InventoryService.Folder.Move(AgentID, d.FolderID, d.ParentID);
                                }
                                catch(Exception e)
                                {
                                    m_Log.DebugFormat("MoveInventoryFolder failed {0} {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace.ToString());
                                }
                            }
                        }
                        break;

                    case MessageType.MoveInventoryItem:
                        {
                            Messages.Inventory.MoveInventoryItem req = (Messages.Inventory.MoveInventoryItem)m;
                            if (req.SessionID != SessionID || req.AgentID != AgentID)
                            {
                                break;
                            }

                            foreach (Messages.Inventory.MoveInventoryItem.InventoryDataEntry d in req.InventoryData)
                            {
                                try
                                {
                                    Agent.InventoryService.Item.Move(AgentID, d.ItemID, d.FolderID);
                                }
                                catch (Exception e)
                                {
                                    m_Log.DebugFormat("MoveInventoryItem failed {0} {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace.ToString());
                                }
                            }
                        }
                        break;

                    case MessageType.PurgeInventoryDescendents:
                        {
                            Messages.Inventory.PurgeInventoryDescendents req = (Messages.Inventory.PurgeInventoryDescendents)m;
                            if(req.SessionID != SessionID || req.AgentID != AgentID)
                            {
                                break;
                            }

                            Agent.InventoryService.Folder.Purge(AgentID, req.FolderID);
                        }
                        break;

                    case MessageType.RemoveInventoryFolder:
                        {
                            Messages.Inventory.RemoveInventoryFolder req = (Messages.Inventory.RemoveInventoryFolder)m;
                            if (req.SessionID != SessionID || req.AgentID != AgentID)
                            {
                                break;
                            }

                            foreach (UUID id in req.FolderData)
                            {
                                try
                                {
                                    Agent.InventoryService.Folder.Delete(AgentID, id);
                                }
                                catch
                                {

                                }
                            }
                        }
                        break;

                    case MessageType.RemoveInventoryItem:
                        {
                            Messages.Inventory.RemoveInventoryItem req = (Messages.Inventory.RemoveInventoryItem)m;
                            if (req.SessionID != SessionID || req.AgentID != AgentID)
                            {
                                break;
                            }

                            foreach (UUID id in req.InventoryData)
                            {
                                try
                                {
                                    Agent.InventoryService.Item.Delete(AgentID, id);
                                }
                                catch
                                {

                                }
                            }
                        }
                        break;

                    case MessageType.RemoveInventoryObjects:
                        {
                            Messages.Inventory.RemoveInventoryObjects req = (Messages.Inventory.RemoveInventoryObjects)m;
                            if (req.SessionID != SessionID || req.AgentID != AgentID)
                            {
                                break;
                            }

                            Agent.InventoryService.Folder.Delete(AgentID, req.FolderIDs);
                            Agent.InventoryService.Item.Delete(AgentID, req.ItemIDs);
                        }
                        break;

                    case MessageType.UpdateInventoryFolder:
                        {
                            Messages.Inventory.UpdateInventoryFolder req = (Messages.Inventory.UpdateInventoryFolder)m;
                            if(req.SessionID != SessionID || req.AgentID != AgentID)
                            {
                                break;
                            }

                            foreach(Messages.Inventory.UpdateInventoryFolder.InventoryDataEntry d in req.InventoryData)
                            {
                                try
                                {
                                    InventoryFolder folder = Agent.InventoryService.Folder[AgentID, d.FolderID];
                                    folder.Name = d.Name;
                                    folder.InventoryType = d.Type;
                                    folder.ParentFolderID = d.ParentID;
                                    Agent.InventoryService.Folder.Update(folder);
                                }
                                catch
                                {
                                    Messages.Alert.AlertMessage res = new Messages.Alert.AlertMessage();
                                    res.Message = string.Format("Could not update folder {0}", d.Name);
                                    SendMessage(res);
                                }
                            }
                        }
                        break;

                    case MessageType.UpdateInventoryItem:
                        {
                            Messages.Inventory.UpdateInventoryItem req = (Messages.Inventory.UpdateInventoryItem)m;
                            if (req.SessionID != SessionID || req.AgentID != AgentID)
                            {
                                break;
                            }

                            foreach(Messages.Inventory.UpdateInventoryItem.InventoryDataEntry d in req.InventoryData)
                            {
                                InventoryItem item;
                                try
                                {
                                    item = Agent.InventoryService.Item[AgentID, d.ItemID];
                                }
                                catch
                                {
                                    continue;
                                }

                                if(item.Owner.ID != AgentID)
                                {
                                    continue;
                                }

                                item.Name = d.Name;
                                item.Description = d.Description;

                                bool sendUpdate = false;
                                if(d.NextOwnerMask != 0)
                                {
                                    InventoryPermissionsData p = new InventoryPermissionsData();
                                    p.Base = d.BaseMask;
                                    p.Current = d.OwnerMask;
                                    p.NextOwner = d.NextOwnerMask;
                                    p.EveryOne = d.EveryoneMask;
                                    p.Group = d.GroupMask;

                                    if((item.Permissions.Base & InventoryPermissionsMask.All | InventoryPermissionsMask.Export) != (InventoryPermissionsMask.All | InventoryPermissionsMask.Export) ||
                                        (item.Permissions.Current & InventoryPermissionsMask.Export) == 0 ||
                                        item.Creator.ID != item.Owner.ID)
                                    {
                                        // If we are not allowed to change it, then force it to the
                                        // original item's setting and if it was on, also force full perm
                                        if ((item.Permissions.EveryOne & InventoryPermissionsMask.Export) != 0)
                                        {
                                            p.NextOwner = InventoryPermissionsMask.All;
                                            p.EveryOne |= InventoryPermissionsMask.Export;
                                        }
                                        else
                                        {
                                            p.EveryOne &= ~InventoryPermissionsMask.Export;
                                        }
                                    }
                                    else
                                    {
                                        // If the new state is exportable, force full perm
                                        if ((p.EveryOne & InventoryPermissionsMask.Export) != 0)
                                        {
                                            p.NextOwner = InventoryPermissionsMask.All;
                                        }
                                    }

                                    if (item.Permissions.NextOwner != (p.NextOwner & item.Permissions.Base))
                                    {
                                        item.Permissions.NextOwner = p.NextOwner & item.Permissions.Base;
                                    }

                                    if (item.Permissions.EveryOne != (p.EveryOne & item.Permissions.Base))
                                    {
                                        item.Permissions.EveryOne = p.EveryOne & item.Permissions.Base;
                                    }

                                    if (item.Permissions.Group != (p.Group & item.Permissions.Base))
                                    {
                                        item.Permissions.Group = p.Group & item.Permissions.Base;
                                    }

                                    try
                                    {
                                        item.Group = Agent.GroupsService.Groups[Agent.Owner, new UGI(d.GroupID)].ID;
                                    }
                                    catch
                                    {
                                        item.Group.ID = d.GroupID;
                                    }
                                    item.IsGroupOwned = d.IsGroupOwned;

                                    if (d.CreationDate == 0)
                                    {
                                        item.CreationDate = new Date();
                                    }
                                    else
                                    {
                                        item.CreationDate = Date.UnixTimeToDateTime(d.CreationDate);
                                    }

                                    item.InventoryType = d.InvType;

                                    item.SaleInfo.Price = d.SalePrice;
                                    item.SaleInfo.Type = d.SaleType;

                                    if (item.InventoryType == InventoryType.Wearable && (d.Flags & 0xf) == 0 && (d.Flags & 0xf) != 0)
                                    {
                                        item.Flags = (uint)(item.Flags & 0xfffffff0) | (d.Flags & 0xf);
                                        sendUpdate = true;
                                    }

                                    try
                                    {
                                        Agent.InventoryService.Item.Update(item);
                                        SendMessage(new Messages.Inventory.UpdateCreateInventoryItem(AgentID, true, req.TransactionID, item, 0));
                                    }
                                    catch
                                    {

                                    }
                                }

                                if (UUID.Zero != req.TransactionID)
                                {
                                    //AgentTransactionsModule.HandleItemUpdateFromTransaction(remoteClient, transactionID, item);
                                }
                                else
                                {
                                    // In other situations we cannot send out a bulk update here, since this will cause editing of clothing to start 
                                    // failing frequently.  Possibly this is a race with a separate transaction that uploads the asset.
                                    if (sendUpdate)
                                    {
                                        SendMessage(new Messages.Inventory.BulkUpdateInventory(AgentID, UUID.Zero, 0, item));
                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }

        private void HandleCreateInventoryItem(Messages.Inventory.CreateInventoryItem req)
        {
            if (req.SessionID != SessionID || req.AgentID != AgentID)
            {
                return;
            }
            if (req.TransactionID == UUID.Zero)
            {
                InventoryFolder folder;
                InventoryItem item;
                try
                {
                    /* check availability for folder first before doing anything else */
                    folder = Agent.InventoryService.Folder[AgentID, req.FolderID];
                }
                catch
                {
                    SendMessage(new Messages.Alert.AlertMessage("ALERT: CantCreateInventory"));
                    return;
                }

                item = new InventoryItem();
                item.InventoryType = req.InvType;
                item.AssetType = req.AssetType;
                item.Description = req.Description;
                item.Name = req.Name;
                item.Owner = Agent.Owner;
                item.Creator = Agent.Owner;
                item.SaleInfo.Type = InventoryItem.SaleInfoData.SaleType.NoSale;
                item.SaleInfo.Price = 0;
                item.SaleInfo.PermMask = InventoryPermissionsMask.All;
                item.ParentFolderID = req.FolderID;

                item.Permissions.Base = InventoryPermissionsMask.All | InventoryPermissionsMask.Export;
                item.Permissions.Current = InventoryPermissionsMask.All | InventoryPermissionsMask.Export;
                item.Permissions.Group = InventoryPermissionsMask.None;
                item.Permissions.EveryOne = InventoryPermissionsMask.None;
                item.Permissions.NextOwner = req.NextOwnerMask;

                if(item.InventoryType == InventoryType.Landmark)
                {
                    Vector3 pos = Agent.GlobalPosition;
                    UUID curSceneID = Agent.SceneID;
                    SceneInterface curScene;
                    try
                    {
                        curScene = Agent.Circuits[curSceneID].Scene;
                    }
                    catch
                    {
                        SendMessage(new Messages.Alert.AlertMessage("ALERT: CantCreateLandmark"));
                        return;
                    }

                    Landmark lm = new Landmark();
                    if (!string.IsNullOrEmpty(GatekeeperURI))
                    {
                        lm.GatekeeperURI = new URI(GatekeeperURI);
                    }
                    lm.LocalPos = pos;
                    lm.RegionID = curSceneID;
                    lm.Location = curScene.RegionData.Location;

                    AssetData asset = lm;
                    asset.Name = item.Name;
                    asset.Creator = Agent.Owner;
                    asset.ID = UUID.Random;
                    try
                    {
                        Agent.AssetService.Store(asset);
                    }
                    catch(Exception e)
                    {
                        SendMessage(new Messages.Alert.AlertMessage("ALERT: CantCreateLandmark"));
                        m_Log.Error("Failed to create asset for landmark", e);
                        return;
                    }
                    try
                    {
                        Agent.InventoryService.Item.Add(item);
                    }
                    catch(Exception e)
                    {
                        SendMessage(new Messages.Alert.AlertMessage("ALERT: CantCreateLandmark"));
                        m_Log.Error("Failed to create inventory item for landmark", e);
                        return;
                    }
                    SendMessage(new Messages.Inventory.UpdateCreateInventoryItem(AgentID, true, req.TransactionID, item, req.CallbackID));
                }
                else
                {
                    SendMessage(new Messages.Alert.AlertMessage("ALERT: CantCreateInventory"));
                    return;
                }
            }
            else
            {

            }
        }
        #endregion
    }
}
