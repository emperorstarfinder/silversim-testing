﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.Grid;
using SilverSim.Types.Parcel;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Map;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace SilverSim.Viewer.Map
{
    [Description("Viewer Map Handler")]
    public class ViewerMap : IPlugin, IPluginShutdown, IPacketHandlerExtender
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL MAP");

        [PacketHandler(MessageType.MapBlockRequest)]
        [PacketHandler(MessageType.MapNameRequest)]
        readonly BlockingQueue<KeyValuePair<AgentCircuit, Message>> MapBlocksRequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();

        [PacketHandler(MessageType.MapLayerRequest)]
        [PacketHandler(MessageType.MapItemRequest)]
        readonly BlockingQueue<KeyValuePair<AgentCircuit, Message>> MapDetailsRequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();

        List<IForeignGridConnectorPlugin> m_ForeignGridConnectorPlugins;
        SceneList m_Scenes;
        bool m_ShutdownMap;

        public ViewerMap()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            m_ForeignGridConnectorPlugins = loader.GetServicesByValue<IForeignGridConnectorPlugin>();

            ThreadManager.CreateThread(HandlerThread).Start(MapBlocksRequestQueue);
            ThreadManager.CreateThread(HandlerThread).Start(MapDetailsRequestQueue);
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public void HandlerThread(object o)
        {
            BlockingQueue<KeyValuePair<AgentCircuit, Message>> requestQueue = (BlockingQueue<KeyValuePair<AgentCircuit, Message>>)o;
            Thread.CurrentThread.Name = (requestQueue == MapDetailsRequestQueue) ?
                "Map Details Handler Thread" :
                "Map Blocks Handler Thread";

            while (!m_ShutdownMap)
            {
                KeyValuePair<AgentCircuit, Message> req;
                try
                {
                    req = requestQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                Message m = req.Value;
                AgentCircuit circuit = req.Key;
                if(circuit == null)
                {
                    continue;
                }
                SceneInterface scene = circuit.Scene;
                if (scene == null)
                {
                    continue;
                }
                try
                {
                    switch (m.Number)
                    {
                        case MessageType.MapNameRequest:
                            HandleMapNameRequest(circuit.Agent, scene, m);
                            break;

                        case MessageType.MapBlockRequest:
                            HandleMapBlockRequest(circuit.Agent, scene, m);
                            break;

                        case MessageType.MapItemRequest:
                            HandleMapItemRequest(circuit.Agent, scene, m);
                            break;

                        default:
                            break;
                    }
                }
                catch (Exception e)
                {
                    m_Log.Debug("Unexpected exception " + e.Message, e);
                }
            }
        }

        #region MapNameRequest and MapBlockRequest
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void HandleMapBlockRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            List<MapBlockReply.DataEntry> results = new List<MapBlockReply.DataEntry>();
            MapBlockRequest req = (MapBlockRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

#if DEBUG
            m_Log.InfoFormat("MapBlockRequest for {0},{1} {2},{3}", req.Min.GridX, req.Min.GridY, req.Max.GridX, req.Max.GridY);
#endif
            List<RegionInfo> ris;
            try
            {
                ris = scene.GridService.GetRegionsByRange(scene.ScopeID, req.Min, req.Max);
            }
            catch
            {
                ris = new List<RegionInfo>();
            }

            foreach(RegionInfo ri in ris)
            {
                MapBlockReply.DataEntry d = new MapBlockReply.DataEntry();
                d.X = ri.Location.GridX;
                d.Y = ri.Location.GridY;

                d.Name = ri.Name;
                d.Access = ri.Access;
                d.RegionFlags = RegionOptionFlags.None; /* this is same RegionOptionFlags as seen in a sim */
                d.WaterHeight = 21;
                d.Agents = 0;
                d.MapImageID = ri.RegionMapTexture;
                results.Add(d);
            }

            SendMapBlocks(agent, scene, req.Flags, results);
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void HandleMapNameRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            MapNameRequest req = (MapNameRequest)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

#if DEBUG
            m_Log.InfoFormat("MapNameRequest for {0}", req.Name);
#endif
            string[] s;
            bool isForeignGridTarget = false;
            string regionName = req.Name;
            string gatekeeperURI = string.Empty;
            List<MapBlockReply.DataEntry> results = new List<MapBlockReply.DataEntry>();

            s = req.Name.Split(new char[] { ':' }, 3);
            if(s.Length > 1)
            {
                /* could be a foreign grid URI, check for number in second place */
                uint val;
                if(!uint.TryParse(s[1], out val))
                {
                    /* not a foreign grid map name */
                }
                else if(val > 65535)
                {
                    /* not a foreign grid map name */
                }
                else if(!Uri.IsWellFormedUriString("http://" + s[0] + ":" + s[1] + "/", UriKind.Absolute))
                {
                    /* not a foreign grid map name */
                }
                else
                {
                    gatekeeperURI = "http://" + s[0] + ":" + s[1] + "/";
                    regionName = (s.Length > 2) ?
                        s[2] :
                        string.Empty; /* Default Region */
                    isForeignGridTarget = true;
                }
            }
            s = req.Name.Split(new char[] { ' ' }, 2);
            if(s.Length > 1)
            {
                if(Uri.IsWellFormedUriString(s[0],UriKind.Absolute))
                {
                    /* this is a foreign grid URI of form <url> <region name> */
                    gatekeeperURI = s[0];
                    regionName = s[1];
                }
                else
                {
                    /* does not look like a uri at all */
                }
            }
            else if(Uri.IsWellFormedUriString(req.Name, UriKind.Absolute))
            {
                /* this is a foreign Grid URI for the Default Region */
                gatekeeperURI = req.Name;
                regionName = string.Empty;
            }
            else
            {
                /* local Grid URI */
            }

            if(isForeignGridTarget)
            {
                RegionInfo ri = null;
                bool foundRegionButWrongProtocol = false;
                string foundProtocolName = string.Empty;
                foreach(IForeignGridConnectorPlugin foreignGrid in m_ForeignGridConnectorPlugins)
                {
                    if(foreignGrid.IsProtocolSupported(gatekeeperURI))
                    {
                        try
                        {
                            ri = foreignGrid.Instantiate(gatekeeperURI)[regionName];
                        }
                        catch
                        {
                            continue;
                        }
                        if(!foreignGrid.IsAgentSupported(agent.SupportedGridTypes))
                        {
                            foundRegionButWrongProtocol = true;
                            foundProtocolName = agent.DisplayName;
                            ri = null;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if(ri == null && foundRegionButWrongProtocol)
                {
                    agent.SendAlertMessage(string.Format("Your home grid does not support the selected target grid (running {0}).", foundProtocolName), scene.ID);
                }
                else if(ri != null)
                {
                    GridVector hgLoc = agent.CacheHgDestination(ri);
                    MapBlockReply.DataEntry d = new MapBlockReply.DataEntry();
                    /* we map foreign grid locations in specific agent only */
                    d.X = hgLoc.GridX;
                    d.Y = hgLoc.GridY;

                    d.Name = ri.Name;
                    d.Access = ri.Access;
                    d.RegionFlags = RegionOptionFlags.None; /* this is same region flags as seen on a sim */
                    d.WaterHeight = 21;
                    d.Agents = 0;
                    d.MapImageID = ri.RegionMapTexture;
                    results.Add(d);
                }
            }
            else if(string.IsNullOrEmpty(regionName))
            {
                agent.SendAlertMessage("Please enter a string", scene.ID);
            }
            else
            {
                GridServiceInterface service = scene.GridService;
                if(service != null)
                {
                    List<RegionInfo> ris;
                    try
                    {
                        ris = service.SearchRegionsByName(scene.ScopeID, regionName);
                    }
                    catch
                    {
                        ris = new List<RegionInfo>();
                    }

                    foreach(RegionInfo ri in ris)
                    {
                        MapBlockReply.DataEntry d = new MapBlockReply.DataEntry();
                        d.X = ri.Location.GridX;
                        d.Y = ri.Location.GridY;

                        d.Name = ri.Name;
                        d.Access = ri.Access;
                        d.RegionFlags = RegionOptionFlags.None; /* this is same region flags as seen on a sim */
                        d.WaterHeight = 21;
                        d.Agents = 0;
                        d.MapImageID = ri.RegionMapTexture;
                        results.Add(d);
                    }
                }
            }

            SendMapBlocks(agent, scene, req.Flags, results);
        }

        void SendMapBlocks(ViewerAgent agent, SceneInterface scene, MapAgentFlags mapflags, List<MapBlockReply.DataEntry> mapBlocks)
        {
            MapBlockReply.DataEntry end = new MapBlockReply.DataEntry();
            end.Agents = 0;
            end.Access = RegionAccess.NonExistent;
            end.MapImageID = UUID.Zero;
            end.Name = string.Empty;
            end.RegionFlags = RegionOptionFlags.None; /* this is same region flags as seen on a sim */
            end.WaterHeight = 0;
            end.X = 0;
            end.Y = 0;
            mapBlocks.Add(end);

            MapBlockReply replymsg = null;
            int mapBlockReplySize = 20;

            foreach(MapBlockReply.DataEntry d in mapBlocks)
            {
                int mapBlockDataSize = 27 + d.Name.Length;
                if (mapBlockReplySize + mapBlockDataSize > 1400 && null != replymsg)
                {
                    agent.SendMessageAlways(replymsg, scene.ID);
                    replymsg = null;
                }

                if (null == replymsg)
                {
                    replymsg = new MapBlockReply();
                    replymsg.AgentID = agent.ID;
                    replymsg.Flags = mapflags;
                    mapBlockReplySize = 20;
                }

                mapBlockReplySize += mapBlockDataSize;
                replymsg.Data.Add(d);
            }

            if(null != replymsg)
            {
                agent.SendMessageAlways(replymsg, scene.ID);
            }
        }
        #endregion

        #region MapItemRequest
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void HandleMapItemRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            MapItemRequest req = (MapItemRequest)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            MapItemReply reply = new MapItemReply();
            reply.AgentID = agent.ID;
            reply.Flags = req.Flags;
            reply.ItemType = req.ItemType;

            SceneInterface accessScene = null;
            if(req.Location.RegionHandle == 0 ||
                req.Location.Equals(scene.GridPosition))
            {
                accessScene = scene;
            }
            else 
            {
                try
                {
                    accessScene = m_Scenes[req.Location];
                }
                catch
                {
                    accessScene = null; /* remote */
                }
            }

            switch(req.ItemType)
            {
                case MapItemType.AgentLocations:
                    if(null != accessScene)
                    {
                        /* local */
                        foreach(IAgent sceneagent in accessScene.Agents)
                        {
                            if(sceneagent.IsInScene(accessScene) && !sceneagent.Owner.Equals(agent.Owner) && sceneagent is ViewerAgent)
                            {
                                MapItemReply.DataEntry d = new MapItemReply.DataEntry();
                                d.X = (ushort)sceneagent.GlobalPosition.X;
                                d.Y = (ushort)sceneagent.GlobalPosition.Y;
                                d.ID = UUID.Zero;
                                d.Name = sceneagent.Owner.FullName;
                                d.Extra = 1;
                                d.Extra2 = 0;
                                reply.Data.Add(d);
                            }
                        }
                    }
                    else
                    {
                        /* remote */
                    }
                    break;

                case MapItemType.LandForSale:
                    if(null != accessScene)
                    {
                        /* local */
                        foreach(ParcelInfo parcel in accessScene.Parcels)
                        {
                            if((parcel.Flags & ParcelFlags.ForSale) != 0)
                            {
                                MapItemReply.DataEntry d = new MapItemReply.DataEntry();
                                double x = (parcel.AABBMin.X + parcel.AABBMax.X) / 2;
                                double y = (parcel.AABBMin.Y + parcel.AABBMax.Y) / 2;
                                d.X = (ushort)x;
                                d.Y = (ushort)y;
                                d.ID = parcel.ID;
                                d.Name = parcel.Name;
                                d.Extra = parcel.Area;
                                d.Extra2 = parcel.SalePrice;
                                reply.Data.Add(d);
                            }
                        }
                    }
                    else
                    {
                        /* remote */
                    }
                    break;

                case MapItemType.Telehub:
                    if(null != accessScene)
                    {
                        /* local */
                    }
                    else
                    {
                        /* remote */
                    }
                    break;

                default:
                    break;
            }
            agent.SendMessageAlways(reply, scene.ID);
        }
        #endregion

        public ShutdownOrder ShutdownOrder
        {
            get 
            {
                return ShutdownOrder.LogoutRegion;
            }
        }

        public void Shutdown()
        {
            m_ShutdownMap = true;
        }
    }

    [PluginName("ViewerMap")]
    public class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new ViewerMap();
        }
    }
}
