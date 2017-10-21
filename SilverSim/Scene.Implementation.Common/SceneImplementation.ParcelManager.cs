﻿using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using System;
using System.Collections.Generic;
using System.Timers;

namespace SilverSim.Scene.Implementation.Common
{
    public partial class SceneImplementation
    {
        public class ParcelAccessManager
        {
            private readonly IParcelAccessList m_WhiteListStorage;
            private readonly IParcelAccessList m_BlackListStorage;
            private readonly IParcelAccessList m_LandpassListStorage;
            private readonly SceneInterface m_Scene;
            private readonly RwLockedDictionary<UUI, Date> m_ExpiryList = new RwLockedDictionary<UUI, Date>();
            private readonly IParcelAccessList[] m_StorageList;
            private Timer m_Timer;

            public ParcelAccessManager(SceneInterface scene, SimulationDataStorageInterface simulationDataStorage)
            {
                m_Timer = new Timer(1000);
                m_Timer.Elapsed += CheckAccessTimer;
                m_Scene = scene;
                m_WhiteListStorage = simulationDataStorage.Parcels.WhiteList;
                m_BlackListStorage = simulationDataStorage.Parcels.BlackList;
                m_LandpassListStorage = simulationDataStorage.Parcels.LandpassList;
                m_StorageList = new IParcelAccessList[] { m_WhiteListStorage, m_BlackListStorage, m_LandpassListStorage };
            }

            ~ParcelAccessManager()
            {
                m_Timer.Elapsed -= CheckAccessTimer;
            }

            public IParcelAccessList WhiteList { get; }
            public IParcelAccessList BlackList { get; }
            public IParcelAccessList LandpassList { get; }

            public void Start() => m_Timer.Start();
            public void Stop() => m_Timer.Stop();

            private void CheckAccess(UUI accessor, UUID parcelID)
            {
                try
                {
                    Date minDate = null;
                    ParcelAccessEntry entry;
                    foreach (IParcelAccessList list in m_StorageList)
                    {
                        if (m_WhiteListStorage.TryGetValue(m_Scene.ID, parcelID, accessor, out entry) && 
                            entry.ExpiresAt != null && (minDate == null || minDate.AsULong > entry.ExpiresAt.AsULong))
                        {
                            minDate = new Date(entry.ExpiresAt);
                        }
                    }
                    if(minDate != null)
                    {
                        m_ExpiryList[accessor] = minDate;
                    }
                    else
                    {
                        m_ExpiryList.Remove(accessor);
                    }
                }
                catch(Exception e)
                {
                    m_Log.Warn("Failed to retrieve list data", e);
                }

                try
                {
                    IAgent agent;
                    ParcelInfo parcelInfo;
                    string reason;
                    if (m_Scene.Agents.TryGetValue(accessor.ID, out agent) &&
                        agent.IsInScene(m_Scene) &&
                        m_Scene.Parcels.TryGetValue(parcelID, out parcelInfo) &&
                        parcelInfo.LandBitmap.ContainsLocation(agent.GlobalPosition))
                    {
                        if (m_Scene.CheckParcelAccessRights(agent, parcelInfo, out reason))
                        {
                            return;
                        }

                        m_Scene.EjectFromParcel(accessor.ID, parcelInfo.ID);
                    }
                }
                catch(Exception e)
                {
                    m_Log.Warn("CheckAccess failed", e);
                }
            }

            private void CheckAccessTimer(object o, ElapsedEventArgs args)
            {
                ParcelInfo parcelInfo;
                IAgent agent;
                try
                {
                    foreach (KeyValuePair<UUI, Date> kvp in new Dictionary<UUI, Date>(m_ExpiryList))
                    {
                        if (kvp.Value.AsULong <= Date.Now.AsULong &&
                            m_Scene.RootAgents.TryGetValue(kvp.Key.ID, out agent) &&
                            m_Scene.Parcels.TryGetValue(agent.GlobalPosition, out parcelInfo))
                        {
                            CheckAccess(kvp.Key, parcelInfo.ID);
                        }
                        else if(!m_Scene.RootAgents.TryGetValue(kvp.Key.ID, out agent))
                        {
                            m_ExpiryList.Remove(kvp.Key);
                        }
                    }
                }
                catch(Exception e)
                {
                    m_Log.Warn("Exception at CheckAccessTimer", e);
                }
            }

            private void CheckAllAccesses()
            {
                try
                {
                    foreach (IAgent agent in m_Scene.RootAgents)
                    {
                        ParcelInfo pInfo;
                        string reason;
                        try
                        {
                            if (m_Scene.Parcels.TryGetValue(agent.GlobalPosition, out pInfo) &&
                                !m_Scene.CheckParcelAccessRights(agent, pInfo, out reason))
                            {
                                m_Scene.EjectFromParcel(agent.Owner.ID, pInfo.ID);
                            }
                        }
                        catch (Exception e)
                        {
                            m_Log.Warn("CheckAllAccesses failed for " + agent.Owner.FullName, e);
                        }
                    }
                }
                catch(Exception e)
                {
                    m_Log.Warn("CheckAllAccesses failed", e);
                }
            }

            public class StatusUpdateListener : IParcelAccessList
            {
                private readonly ParcelAccessManager m_ParcelManager;
                private readonly IParcelAccessList m_StorageList;

                public StatusUpdateListener(ParcelAccessManager parcelManager, IParcelAccessList storageList)
                {
                    m_ParcelManager = parcelManager;
                    m_StorageList = storageList;
                }

                public List<ParcelAccessEntry> this[UUID regionID, UUID parcelID] => m_StorageList[regionID, parcelID];

                public bool this[UUID regionID, UUID parcelID, UUI accessor]
                {
                    get
                    {
                        ParcelAccessEntry e;
                        return TryGetValue(regionID, parcelID, accessor, out e);
                    }
                }

                public void ExtendExpiry(UUID regionID, UUID parcelID, UUI accessor, ulong extendseconds)
                {
                    m_StorageList.ExtendExpiry(regionID, parcelID, accessor, extendseconds);
                    m_ParcelManager.CheckAccess(accessor, parcelID);
                    
                }

                public bool Remove(UUID regionID, UUID parcelID)
                {
                    bool result = m_StorageList.Remove(regionID, parcelID);
                    if(result)
                    {
                        m_ParcelManager.CheckAllAccesses();
                    }
                    return result;
                }

                public bool Remove(UUID regionID, UUID parcelID, UUI accessor)
                {
                    bool result = m_StorageList.Remove(regionID, parcelID, accessor);
                    if(result)
                    {
                        m_ParcelManager.CheckAccess(accessor, parcelID);
                    }
                    return result;
                }

                public void Store(ParcelAccessEntry entry)
                {
                    m_StorageList.Store(entry);
                    m_ParcelManager.CheckAccess(entry.Accessor, entry.ParcelID);
                }

                public bool TryGetValue(UUID regionID, UUID parcelID, UUI accessor, out ParcelAccessEntry e)
                {
                    bool res = m_StorageList.TryGetValue(regionID, parcelID, accessor, out e);
                    if(res)
                    {
                        m_ParcelManager.CheckAccess(accessor, parcelID);
                    }
                    return res;
                }
            }
        }
    }
}
