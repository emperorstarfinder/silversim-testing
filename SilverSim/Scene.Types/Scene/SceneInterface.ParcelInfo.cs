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

using SilverSim.Scene.Types.Agent;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Parcel;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        private Int32[,] m_ParcelLayer; /* initialized in constructor */
        private bool[] m_ParcelLayerDirty; /* RegionWidth / PARCEL_BLOCK_SIZE * RegionHeight / PARCEL_BLOCK_SIZE / 1024 */
        private readonly ReaderWriterLock m_ParcelLayerRwLock = new ReaderWriterLock();
        protected readonly RwLockedDoubleDictionary<UUID, Int32, ParcelInfo> m_Parcels = new RwLockedDoubleDictionary<UUID, int, ParcelInfo>();
        private readonly object m_ParcelOverlayUpdateLock = new object();

        private void InitializeParcelLayer()
        {
            m_ParcelLayer = new Int32[SizeY / PARCEL_BLOCK_SIZE, SizeX / PARCEL_BLOCK_SIZE];
            m_ParcelLayerDirty = new bool[(SizeY / PARCEL_BLOCK_SIZE) * (SizeX / PARCEL_BLOCK_SIZE) / 1024];
        }

        public void AddParcel(ParcelInfo p)
        {
            m_Parcels.Add(p.ID, p.LocalID, p);
            TriggerParcelUpdate(p);
        }

        public void AddParcelNoTrigger(ParcelInfo p)
        {
            m_Parcels.Add(p.ID, p.LocalID, p);
        }

        public abstract bool RemoveParcel(ParcelInfo p, UUID mergeTo);

        public void ClearParcels()
        {
            m_Parcels.Clear();
        }

        public abstract void TriggerParcelUpdate(ParcelInfo pInfo);

        public int CalcMaxTotalParcelPrims(ParcelInfo p)
        {
            int regionTotal = RegionSettings.MaxTotalPrims;
            long regionArea = (long)SizeX * SizeY;
            long parcelPrims = p.Area * regionTotal / regionArea;
            return (int)(p.ParcelPrimBonus * parcelPrims);
        }

        private ParcelOverlayType GetParcelLayerByte(int x, int y, UUI agentID)
        {
            ParcelInfo pi;
            var ov = ParcelOverlayType.Public;
            Int32 parcelLocalID = m_ParcelLayer[x, y];
            if (m_Parcels.TryGetValue(parcelLocalID, out pi))
            {
                if(pi.Owner == agentID)
                {
                    ov |= ParcelOverlayType.OwnedBySelf;
                }
                else if(pi.SalePrice >= 0 &&
                    (pi.AuthBuyer.ID == UUID.Zero ||
                    pi.AuthBuyer.ID == agentID.ID))
                {
                    ov |= ParcelOverlayType.ForSale;
                }
                else if(pi.Owner.ID == UUID.Zero)
                {
                    ov |= ParcelOverlayType.Public;
                }
                else
                {
                    ov |= ParcelOverlayType.OwnedByOther;
                }

                if(x == 0 ||
                    m_ParcelLayer[x - 1, y] != parcelLocalID && m_Parcels.ContainsKey(m_ParcelLayer[x - 1, y]))
                {
                    ov |= ParcelOverlayType.BorderWest;
                }

                if (y == 0 ||
                    m_ParcelLayer[x, y - 1] != parcelLocalID && m_Parcels.ContainsKey(m_ParcelLayer[x, y - 1]))
                {
                    ov |= ParcelOverlayType.BorderSouth;
                }
            }

            return ov;
        }

        public void SendAllParcelOverlaysTo(IAgent agent)
        {
            var c = new byte[SizeX * SizeY / PARCEL_BLOCK_SIZE / PARCEL_BLOCK_SIZE];
            m_ParcelLayerRwLock.AcquireReaderLock(-1);
            try
            {
                int bytePos = 0;
                UUI agentID = agent.Owner;
                for (int y = 0; y < SizeY / PARCEL_BLOCK_SIZE; ++y)
                {
                    for (int x = 0; x < SizeX / PARCEL_BLOCK_SIZE; ++x)
                    {
                        c[bytePos++] = (byte)GetParcelLayerByte(x, y, agentID);
                    }
                }
            }
            finally
            {
                m_ParcelLayerRwLock.ReleaseReaderLock();
            }

            int sequenceID = 0;
            int offset;
            ParcelOverlay m;

            for(offset = 0; offset < c.Length; offset += 1024, ++sequenceID)
            {
                m = new ParcelOverlay()
                {
                    SequenceID = sequenceID,
                    Data = (c.Length - offset >= 1024) ?
                    new byte[1024] :
                    new byte[c.Length - offset]
                };
                Buffer.BlockCopy(c, offset, m.Data, 0, m.Data.Length);
                agent.SendMessageAlways(m, ID);
            }
        }

        private void SendParcelUpdates()
        {
            lock(m_ParcelOverlayUpdateLock)
            {
                m_ParcelLayerRwLock.AcquireReaderLock(-1);
                try
                {
                    int sequenceID = 0;
                    int totalLen = (int)(SizeX / PARCEL_BLOCK_SIZE * SizeY / PARCEL_BLOCK_SIZE);
                    int xwidth = (int)(SizeX / PARCEL_BLOCK_SIZE);
                    for (int offset = 0; offset < totalLen; offset += 1024, ++sequenceID)
                    {
                        if (m_ParcelLayerDirty[offset / 1024])
                        {
                            foreach (IAgent a in Agents)
                            {
                                ParcelOverlay m = new ParcelOverlay()
                                {
                                    Data = (totalLen - offset >= 1024) ?
                                    new byte[1024] :
                                    new byte[totalLen - offset],

                                    SequenceID = sequenceID
                                };
                                UUI agentID = a.Owner;
                                for (int pos = 0; pos < m.Data.Length; ++pos)
                                {
                                    m.Data[pos] = (byte)GetParcelLayerByte((offset + pos) % xwidth, (offset + pos) / xwidth, agentID);
                                }
                                a.SendMessageAlways(m, ID);
                            }
                            m_ParcelLayerDirty[offset / 1024] = false;
                        }
                    }
                }
                finally
                {
                    m_ParcelLayerRwLock.ReleaseReaderLock();
                }
            }
        }

        public ParcelProperties ParcelInfo2ParcelProperties(UUID agentID, ParcelInfo pinfo, int sequenceId, ParcelProperties.RequestResultType requestResult) =>
            new ParcelProperties()
        {
            RequestResult = requestResult,
            SequenceID = sequenceId,
            SnapSelection = false,
#warning Implement user-specific counts
            SelfCount = 0, /* TODO: */
            OtherCount = 0,
            PublicCount = 0,
            LocalID = pinfo.LocalID,
            IsGroupOwned = pinfo.GroupOwned,
            OwnerID = (pinfo.GroupOwned) ?
                pinfo.Group.ID :
                pinfo.Owner.ID,
            AuctionID = pinfo.AuctionID,
            ClaimDate = pinfo.ClaimDate,
            ClaimPrice = pinfo.ClaimPrice,
            RentPrice = pinfo.RentPrice,
            AABBMax = pinfo.AABBMax,
            AABBMin = pinfo.AABBMin,
            Bitmap = pinfo.LandBitmap.Data,
            Area = pinfo.Area,
            Status = pinfo.Status,
            SimWideMaxPrims = 15000,
            SimWideTotalPrims = 15000,
            MaxPrims = 15000,
            TotalPrims = 15000,
            OwnerPrims = 0,
            GroupPrims = 0,
            OtherPrims = 0,
            SelectedPrims = 0,
            ParcelPrimBonus = pinfo.ParcelPrimBonus,
            OtherCleanTime = pinfo.OtherCleanTime,
            ParcelFlags = pinfo.Flags,
            SalePrice = pinfo.SalePrice,
            Name = pinfo.Name,
            Description = pinfo.Description,
            MusicURL = pinfo.MusicURI ?? string.Empty,
            MediaURL = pinfo.MediaURI ?? string.Empty,
            MediaID = pinfo.MediaID,
            MediaAutoScale = pinfo.MediaAutoScale,
            GroupID = pinfo.Group.ID,
            PassPrice = pinfo.PassPrice,
            PassHours = pinfo.PassHours,
            Category = pinfo.Category,
            AuthBuyerID = pinfo.AuthBuyer.ID,
            SnapshotID = pinfo.SnapshotID,
            UserLocation = pinfo.LandingPosition,
            UserLookAt = pinfo.LandingLookAt,
            LandingType = pinfo.LandingType,
            RegionPushOverride = false,
            RegionDenyAnonymous = false,
            RegionDenyIdentified = false,
            RegionDenyTransacted = false,
            RegionDenyAgeUnverified = false,
#warning Other Parcel Details here
            Privacy = pinfo.IsPrivate,
            SeeAVs = pinfo.SeeAvatars,
            AnyAVSounds = pinfo.AnyAvatarSounds,
            GroupAVSounds = pinfo.GroupAvatarSounds,
            MediaDesc = pinfo.MediaDescription,
            MediaHeight = pinfo.MediaHeight,
            MediaWidth = pinfo.MediaWidth,
            MediaLoop = pinfo.MediaLoop,
            MediaType = pinfo.MediaType,
            ObscureMedia = pinfo.ObscureMedia,
            ObscureMusic = pinfo.ObscureMusic
        };

        [PacketHandler(MessageType.ParcelPropertiesRequest)]
        public void HandleParcelPropertiesRequest(Message m)
        {
            var results = new Dictionary<UUID, ParcelInfo>();
            var req = (ParcelPropertiesRequest)m;
            if(req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            var start_x = (int)(req.West + 0.5);
            var start_y = (int)(req.South + 0.5);
            var end_x = (int)(req.East + 0.5);
            var end_y = (int)(req.North + 0.5);
            if(start_x < 0)
            {
                start_x = 0;
            }
            if(start_y < 0)
            {
                start_y = 0;
            }
            if(end_x >= SizeX)
            {
                end_x = (int)SizeX - 1;
            }
            if (end_y >= SizeY)
            {
                end_y = (int)SizeY - 1;
            }

            for(int x = start_x; x <= end_x; ++x)
            {
                for(int y = start_y; y <= end_y; ++y)
                {
                    ParcelInfo pinfo;
                    try
                    {
                        pinfo = Parcels[new Vector3(x, y, 0)];
                    }
                    catch
                    {
                        continue;
                    }

                    if(!results.ContainsKey(pinfo.ID))
                    {
                        results.Add(pinfo.ID, pinfo);
                    }
                }
            }

            IAgent agent;
            if(!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }
            foreach(ParcelInfo pinfo in results.Values)
            {
                ParcelProperties props = ParcelInfo2ParcelProperties(req.AgentID, pinfo, req.SequenceID,
                    (results.Count > 1) ? ParcelProperties.RequestResultType.Multiple : ParcelProperties.RequestResultType.Single);
                props.SnapSelection = req.SnapSelection;
                agent.SendMessageAlways(props, ID);
            }
        }

        [PacketHandler(MessageType.ParcelPropertiesRequestByID)]
        public void HandleParcelPropertiesRequestByID(Message m)
        {
            var req = (ParcelPropertiesRequestByID)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
            ParcelInfo pinfo;
            if(Parcels.TryGetValue(req.LocalID, out pinfo))
            {
                ParcelProperties props = ParcelInfo2ParcelProperties(req.AgentID, pinfo, req.SequenceID, ParcelProperties.RequestResultType.Single);
                Agents[req.AgentID].SendMessageAlways(props, ID);
            }
        }

        [PacketHandler(MessageType.ParcelGodForceOwner)]
        public void HandleParcelGodForceOwner(Message m)
        {
            var req = (ParcelGodForceOwner)m;
            UUI agentID;
            ParcelInfo pInfo;
            IAgent godAgent;
            if(req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID ||
                req.OwnerID != req.AgentID ||
                !Agents.TryGetValue(req.AgentID, out godAgent) ||
                !AvatarNameService.TryGetValue(req.OwnerID, out agentID) ||
                !Parcels.TryGetValue(req.LocalID, out pInfo) ||
                !godAgent.IsActiveGod ||
                !godAgent.IsInScene(this))
            {
                return;
            }
            m_Log.InfoFormat("Forced parcel {0} ({1}) to be owned by {2}", pInfo.Name, pInfo.ID, agentID.FullName);
            pInfo.Group = UGI.Unknown;
            pInfo.GroupOwned = false;
            pInfo.ClaimDate = Date.Now;
            pInfo.SalePrice = 0;
            pInfo.AuthBuyer = UUI.Unknown;
            pInfo.Owner = agentID;
            pInfo.Flags &= ~(ParcelFlags.ForSale | ParcelFlags.ForSaleObjects | ParcelFlags.SellParcelObjects | ParcelFlags.ShowDirectory);
            TriggerParcelUpdate(pInfo);
        }

        [PacketHandler(MessageType.ParcelDeedToGroup)]
        public void HandleParcelDeedToGroup(Message m)
        {
            var req = (ParcelDeedToGroup)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            ParcelInfo pInfo;
            if (Parcels.TryGetValue(req.LocalID, out pInfo) &&
                CanDeedParcel(agent.Owner, pInfo))
            {
                if (!pInfo.Group.Equals(UUI.Unknown))
                {
                    pInfo.GroupOwned = true;
                }
                TriggerParcelUpdate(pInfo);
            }
        }

        [PacketHandler(MessageType.ParcelGodMarkAsContent)]
        public void HandleParcelGodMarkAsContent(Message m)
        {
            var req = (ParcelGodForceOwner)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            ParcelInfo pInfo;
            if (Parcels.TryGetValue(req.LocalID, out pInfo) &&
                CanGodMarkParcelAsContent(agent.Owner, pInfo))
            {
                pInfo.Group = UGI.Unknown;
                pInfo.GroupOwned = false;
                pInfo.ClaimDate = Date.Now;
                pInfo.SalePrice = 0;
                pInfo.AuthBuyer = UUI.Unknown;
                pInfo.Owner = Owner;
                pInfo.Flags &= ~(ParcelFlags.ForSale | ParcelFlags.ForSaleObjects | ParcelFlags.SellParcelObjects | ParcelFlags.ShowDirectory);
                TriggerParcelUpdate(pInfo);
            }
        }

        [PacketHandler(MessageType.ParcelRelease)]
        public void HandleParcelRelease(Message m)
        {
            var req = (ParcelRelease)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            ParcelInfo pInfo;
            if (Parcels.TryGetValue(req.LocalID, out pInfo) &&
                CanReleaseParcel(agent.Owner, pInfo))
            {
                pInfo.Group = UGI.Unknown;
                pInfo.GroupOwned = false;
                pInfo.Owner = UUI.Unknown;
                pInfo.SalePrice = 0;
                pInfo.AuthBuyer = UUI.Unknown;
                pInfo.Flags &= ~(ParcelFlags.ForSale | ParcelFlags.ForSaleObjects | ParcelFlags.SellParcelObjects | ParcelFlags.ShowDirectory);
                TriggerParcelUpdate(pInfo);
            }
        }

        [PacketHandler(MessageType.ParcelJoin)]
        public void HandleParcelJoin(Message m)
        {
            var req = (ParcelJoin)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            JoinParcels(agent.Owner, (int)Math.Round(req.West), (int)Math.Round(req.South), (int)Math.Round(req.East), (int)Math.Round(req.North));
        }

        [PacketHandler(MessageType.ParcelDivide)]
        public void HandleParcelDivide(Message m)
        {
            var req = (ParcelDivide)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            DivideParcel(agent.Owner, (int)Math.Round(req.West), (int)Math.Round(req.South), (int)Math.Round(req.East), (int)Math.Round(req.North));
        }

        [PacketHandler(MessageType.ParcelReclaim)]
        public void HandleParcelReclaim(Message m)
        {
            var req = (ParcelReclaim)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            ParcelInfo pInfo;
            if (Parcels.TryGetValue(req.LocalID, out pInfo) &&
                CanReclaimParcel(agent.Owner, pInfo))
            {
                pInfo.Group = UGI.Unknown;
                pInfo.GroupOwned = false;
                pInfo.ClaimDate = Date.Now;
                pInfo.SalePrice = 0;
                pInfo.AuthBuyer = UUI.Unknown;
                pInfo.Owner = Owner;
                pInfo.Flags &= ~(ParcelFlags.ForSale | ParcelFlags.ForSaleObjects | ParcelFlags.SellParcelObjects | ParcelFlags.ShowDirectory);
                TriggerParcelUpdate(pInfo);
            }
        }

        [PacketHandler(MessageType.ParcelSetOtherCleanTime)]
        public void HandleParcelSetOtherCleanTime(Message m)
        {
            var req = (ParcelSetOtherCleanTime)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }
            ParcelInfo pInfo;
            if (Parcels.TryGetValue(req.LocalID, out pInfo) &&
                CanEditParcelDetails(agent.Owner, pInfo))
            {
                pInfo.OtherCleanTime = req.OtherCleanTime;
                TriggerParcelUpdate(pInfo);
            }
        }

        [PacketHandler(MessageType.ParcelPropertiesUpdate)]
        public void HandleParcelPropertiesUpdate(Message m)
        {
            var req = (ParcelPropertiesUpdate)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            ParcelInfo pInfo;
            if (Parcels.TryGetValue(req.LocalID, out pInfo) &&
                CanEditParcelDetails(agent.Owner, pInfo))
            {
                pInfo.Flags = req.ParcelFlags;
                pInfo.SalePrice = req.SalePrice;
                pInfo.Name = req.Name;
                pInfo.Description = req.Description;
                pInfo.MusicURI = (req.MusicURL.Length != 0) && Uri.IsWellFormedUriString(req.MusicURL, UriKind.Absolute) ?
                        new URI(req.MusicURL) : null;

                pInfo.MediaURI = (req.MediaURL.Length != 0) && Uri.IsWellFormedUriString(req.MediaURL, UriKind.Absolute) ?
                    new URI(req.MediaURL) : null;
                pInfo.MediaAutoScale = req.MediaAutoScale;
                UGI ugi;
                if (req.GroupID == UUID.Zero)
                {
                    ugi = UGI.Unknown;
                }
                else if (GroupsNameService.TryGetValue(req.GroupID, out ugi))
                {
                    pInfo.Group = ugi;
                }
                else
                {
                    pInfo.Group = UGI.Unknown;
                }

                pInfo.PassPrice = req.PassPrice;
                pInfo.PassHours = req.PassHours;
                pInfo.Category = req.Category;
                if (req.AuthBuyerID == UUID.Zero ||
                    !AvatarNameService.TryGetValue(req.AuthBuyerID, out pInfo.AuthBuyer))
                {
                    pInfo.AuthBuyer = UUI.Unknown;
                }

                pInfo.SnapshotID = req.SnapshotID;
                pInfo.LandingPosition = req.UserLocation;
                pInfo.LandingLookAt = req.UserLookAt;
                pInfo.LandingType = req.LandingType;
                TriggerParcelUpdate(pInfo);
            }
        }

        [PacketHandler(MessageType.ParcelDwellRequest)]
        public void HandleParcelDwellRequest(Message m)
        {
            var req = (ParcelDwellRequest)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            ParcelInfo pInfo;
            if (Parcels.TryGetValue(req.LocalID, out pInfo))
            {
                var reply = new ParcelDwellReply()
                {
                    AgentID = req.AgentID,
                    LocalID = req.LocalID,
                    ParcelID = new ParcelID(GetRegionInfo().Location, pInfo.ParcelBasePosition),
                    Dwell = pInfo.Dwell
                };
                agent.SendMessageAlways(reply, ID);
            }
        }

        public bool JoinParcels(UUI requestingAgent, int start_x, int start_y, int end_x, int end_y)
        {
            start_x &= (~3);
            start_y &= (~3);
            end_x &= (~3);
            end_y &= (~3);

            var parcels = new List<ParcelInfo>();

            for (int py = start_y; py < end_y; py += 4)
            {
                for (int px = start_x; px < end_x; px += 4)
                {
                    ParcelInfo pInfo;
                    if(Parcels.TryGetValue(new Vector3(px, py, 0), out pInfo) && 
                        !parcels.Contains(pInfo))
                    {
                        parcels.Add(pInfo);
                    }
                }
            }

            if(parcels.Count < 2)
            {
                return false;
            }

            ParcelInfo remainingParcel = parcels[0];

            /* check rights on parcels */
            foreach (ParcelInfo pInfo in parcels)
            {
                if(!remainingParcel.Owner.EqualsGrid(pInfo.Owner) || !CanDivideJoinParcel(requestingAgent, pInfo))
                {
                    return false;
                }
            }

            parcels.RemoveAt(0);

            /* merge parcels */
            foreach(ParcelInfo pInfo in parcels)
            {
                RemoveParcel(pInfo, remainingParcel.ID);
            }

            return true;
        }

        public bool DivideParcel(UUI requestingAgent, int start_x, int start_y, int end_x, int end_y)
        {
            ParcelInfo pInfo;
            if(!Parcels.TryGetValue(new Vector3(start_x, start_y, 0), out pInfo))
            {
                return false;
            }
            ParcelInfo.ParcelDataLandBitmap bmap = pInfo.LandBitmap;
            if(!bmap[start_x, end_y - 1] || !bmap[end_x - 1, start_x] || !bmap[end_x -1, end_y - 1])
            {
                /* not dividable */
                return false;
            }

            start_x &= (~3);
            start_y &= (~3);
            end_x &= (~3);
            end_y &= (~3);
            for(int py = start_y; py < end_y; py += 4)
            {
                for(int px = start_x; px < end_x; px += 4)
                {
                    if(!bmap[px, py])
                    {
                        return false;
                    }
                }
            }

            if(!CanDivideJoinParcel(requestingAgent, pInfo))
            {
                return false;
            }

            ParcelInfo newParcel = new ParcelInfo(pInfo, true);
            newParcel.ID = UUID.Random;
            Parcels.Add(newParcel);
            ParcelInfo.ParcelDataLandBitmap newBmap = newParcel.LandBitmap;
            for (int py = start_y; py < end_y; ++py)
            {
                for (int px = start_x; px < end_x; ++px)
                {
                    newBmap[px, py] = true;
                    bmap[px, py] = false;
                }
            }
            TriggerParcelUpdate(pInfo);
            TriggerParcelUpdate(newParcel);
            return true;
        }
    }
}
