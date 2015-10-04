﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        private const int IMAGE_PACKET_SIZE = 1000;
        private const int IMAGE_FIRST_PACKET_SIZE = 600;

        #region Texture Download Thread
        public bool LogUDPTextureDownloads = false;
        private void TextureDownloadThread(object param)
        {
            Thread.CurrentThread.Name = string.Format("LLUDP:Texture Downloader for CircuitCode {0} / IP {1}", CircuitCode, RemoteEndPoint.ToString());
            Queue<Messages.Image.RequestImage.RequestImageEntry> bakedReqs = new Queue<Messages.Image.RequestImage.RequestImageEntry>();
            Queue<Messages.Image.RequestImage.RequestImageEntry> normalReqs = new Queue<Messages.Image.RequestImage.RequestImageEntry>();
            HashSet<UUID> activeRequestImages = new HashSet<UUID>();
#warning Implement Priority handling

            while(true)
            {
                if(!m_TextureDownloadThreadRunning)
                {
                    return;
                }
                try
                {
                    Messages.Image.RequestImage req;
                    if (bakedReqs.Count != 0 || normalReqs.Count != 0)
                    {
                        req = (Messages.Image.RequestImage)m_TextureDownloadQueue.Dequeue(0);
                    }
                    else
                    {
                        req = (Messages.Image.RequestImage)m_TextureDownloadQueue.Dequeue(1000);
                    }
                    foreach(Messages.Image.RequestImage.RequestImageEntry imageRequest in req.RequestImageList)
                    {
                        if (!activeRequestImages.Contains(imageRequest.ImageID))
                        {
                            activeRequestImages.Add(imageRequest.ImageID);
                            if (imageRequest.Type == Messages.Image.RequestImage.ImageType.Baked ||
                                imageRequest.Type == Messages.Image.RequestImage.ImageType.ServerBaked)
                            {
                                bakedReqs.Enqueue(imageRequest);
                            }
                            else
                            {
                                normalReqs.Enqueue(imageRequest);
                            }
                        }
                    }
                }
                catch
                {
                    if (bakedReqs.Count == 0 && normalReqs.Count == 0)
                    {
                        continue;
                    }
                }

                if (bakedReqs.Count != 0 || normalReqs.Count != 0)
                {
                    Messages.Image.RequestImage.RequestImageEntry imageRequest;
                    try
                    {
                        imageRequest = bakedReqs.Dequeue();
                    }
                    catch
                    {
                        try
                        {
                            imageRequest = normalReqs.Dequeue();
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    if(LogUDPTextureDownloads)
                    {
                        m_Log.InfoFormat("Processing texture {0}", imageRequest.ImageID);
                    }
                    /* let us prefer the scene's asset service */
                    AssetData asset;
                    try
                    {
                        asset = Scene.AssetService[imageRequest.ImageID];
                    }
                    catch(Exception e1)
                    {
                        try
                        {
                            /* now we try the agent's asset service */
                            asset = Agent.AssetService[imageRequest.ImageID];
                            try
                            {
                                /* let us try to store the image locally */
                                asset.Temporary = true;
                                Scene.AssetService.Store(asset);
                            }
                            catch(Exception e3)
                            {
                                m_Log.DebugFormat("Failed to store asset {0} locally (RequestImage): {1}", imageRequest.ImageID, e3.Message);
                            }
                        }
                        catch (Exception e2)
                        {
                            if (Server.LogAssetFailures)
                            {
                                m_Log.DebugFormat("Failed to download image {0} (RequestImage): {1} or {2}\nA: {3}\nB: {4}", imageRequest.ImageID, e1.Message, e2.Message, e1.StackTrace.ToString(), e2.StackTrace.ToString());
                            }
                            Messages.Image.ImageNotInDatabase failres = new Messages.Image.ImageNotInDatabase();
                            failres.ID = imageRequest.ImageID;
                            SendMessage(failres);
                            if (LogUDPTextureDownloads)
                            {
                                m_Log.InfoFormat("texture {0} not found", imageRequest.ImageID);
                            }
                            activeRequestImages.Remove(imageRequest.ImageID);
                            continue;
                        }
                    }

                    Messages.Image.ImageCodec codec;

                    switch (asset.Type)
                    {
                        case AssetType.ImageJPEG:
                            codec = Messages.Image.ImageCodec.JPEG;
                            break;

                        case AssetType.ImageTGA:
                        case AssetType.TextureTGA:
                            codec = Messages.Image.ImageCodec.TGA;
                            break;

                        case AssetType.Texture:
                            codec = Messages.Image.ImageCodec.J2C;
                            break;

                        default:
                            Messages.Image.ImageNotInDatabase failres = new Messages.Image.ImageNotInDatabase();
                            failres.ID = imageRequest.ImageID;
                            SendMessage(failres);
                            activeRequestImages.Remove(imageRequest.ImageID);
                            if(LogUDPTextureDownloads)
                            {
                                m_Log.InfoFormat("Asset {0} is not a texture", imageRequest.ImageID);
                            }
                            continue;
                    }

                    Messages.Image.ImageData res = new Messages.Image.ImageData();
                    res.Codec = codec;
                    res.ID = imageRequest.ImageID;
                    res.Size = (uint)asset.Data.Length;

                    if (asset.Data.Length > IMAGE_FIRST_PACKET_SIZE)
                    {
                        if (imageRequest.Packet == 0)
                        {
                            res.Data = new byte[IMAGE_FIRST_PACKET_SIZE];
                            uint numpackets = 1 + ((uint)asset.Data.Length - IMAGE_FIRST_PACKET_SIZE + IMAGE_PACKET_SIZE - 1) / IMAGE_PACKET_SIZE;
                            res.Packets = (ushort)numpackets;

                            Buffer.BlockCopy(asset.Data, 0, res.Data, 0, IMAGE_FIRST_PACKET_SIZE);
                            SendMessage(res);
                        } 

                        int offset = IMAGE_FIRST_PACKET_SIZE;
                        ushort packetno = 0;
                        while(offset < asset.Data.Length)
                        {
                            Messages.Image.ImagePacket ip = new Messages.Image.ImagePacket();
                            ip.ID = imageRequest.ImageID;
                            ip.Packet = ++packetno;
                            if(asset.Data.Length - offset > IMAGE_PACKET_SIZE)
                            {
                                ip.Data = new byte[IMAGE_PACKET_SIZE];
                            }
                            else
                            {
                                ip.Data = new byte[asset.Data.Length - offset];
                            }

                            Buffer.BlockCopy(asset.Data, offset, ip.Data, 0, ip.Data.Length);
                            SendMessage(ip);
                            offset += IMAGE_PACKET_SIZE;
                        }
                    }
                    else if (imageRequest.Packet == 0)
                    {
                        res.Data = asset.Data;
                        res.Packets = 1;
                        SendMessage(res);
                    }
                    activeRequestImages.Remove(imageRequest.ImageID);
                    if (LogUDPTextureDownloads)
                    {
                        m_Log.InfoFormat("Download of texture {0} finished", imageRequest.ImageID);
                    }
                }
            }
        }
        #endregion

    }
}
