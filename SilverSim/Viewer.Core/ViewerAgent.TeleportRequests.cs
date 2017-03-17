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

using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Grid;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Teleport;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        static RwLockedDictionary<ulong, KeyValuePair<ulong, RegionInfo>> m_HypergridDestinations = new RwLockedDictionary<ulong, KeyValuePair<ulong, RegionInfo>>();
        private static Random m_RandomNumber = new Random();
        private static object m_RandomNumberLock = new object();

        private ushort NewHgRegionLocY
        {
            get
            {
                int rand;
                lock (m_RandomNumberLock)
                {
                    rand = m_RandomNumber.Next(1, 65535);
                }
                return (ushort)rand;
            }
        }

        void CleanDestinationCache()
        {
            List<ulong> regionHandles = new List<ulong>();
            foreach (KeyValuePair<ulong, KeyValuePair<ulong, RegionInfo>> kvp in m_HypergridDestinations)
            {
                if (Date.GetUnixTime() - kvp.Value.Key > 240)
                {
                    regionHandles.Add(kvp.Key);
                }
            }
            foreach (ulong r in regionHandles)
            {
                m_HypergridDestinations.Remove(r);
            }
        }

        public GridVector CacheHgDestination(RegionInfo di)
        {
            CleanDestinationCache();
            GridVector hgRegionHandle = new GridVector();
            hgRegionHandle.GridX = 0;
            hgRegionHandle.GridY = NewHgRegionLocY;
            m_HypergridDestinations.Add(di.Location.RegionHandle, new KeyValuePair<ulong, RegionInfo>(Date.GetUnixTime(), di));
            return hgRegionHandle;
        }

        bool TryGetDestination(GridVector gv, out RegionInfo di)
        {
            KeyValuePair<ulong, RegionInfo> dest;
            di = default(RegionInfo);
            if (m_HypergridDestinations.TryGetValue(gv.RegionHandle, out dest) &&
                Date.GetUnixTime() - dest.Key <= 240)
            {
                di = dest.Value;
                return true;
            }

            CleanDestinationCache();

            return false;
        }


        [PacketHandler(MessageType.TeleportCancel)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void HandleTeleportCancel(Message m)
        {
            TeleportCancel req = (TeleportCancel)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            m_Log.Warn("Implement TeleportCancel");
        }

        [PacketHandler(MessageType.TeleportLandmarkRequest)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void HandleTeleportLandmarkRequest(Message m)
        {
            TeleportLandmarkRequest req = (TeleportLandmarkRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            m_Log.Warn("Implement TeleportLandmarkRequest");
        }

        [PacketHandler(MessageType.TeleportLocationRequest)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void HandleTeleportLocationRequest(Message m)
        {
            TeleportLocationRequest req = (TeleportLocationRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            AgentCircuit circuit;
            if(Circuits.TryGetValue(req.CircuitSceneID, out circuit))
            {
                RegionInfo hgRegionInfo;

                /* check whether HG destination is addressed */
                if(TryGetDestination(req.GridPosition, out hgRegionInfo))
                {
                    if (!TeleportTo(circuit.Scene, hgRegionInfo.GridURI, hgRegionInfo.ID, req.Position, req.LookAt, TeleportFlags.ViaLocation))
                    {
                        TeleportFailed failedmsg = new TeleportFailed();
                        failedmsg.AgentID = ID;
                        failedmsg.Reason = this.GetLanguageString(CurrentCulture, "TeleportNotPossibleToRegion", "Teleport to region not possible");
                        SendMessageAlways(failedmsg, req.CircuitSceneID);
                    }
                }
                else if(!TeleportTo(circuit.Scene, req.GridPosition, req.Position, req.LookAt, TeleportFlags.ViaLocation))
                {
                    TeleportFailed failedmsg = new TeleportFailed();
                    failedmsg.AgentID = ID;
                    failedmsg.Reason = this.GetLanguageString(CurrentCulture, "TeleportNotPossibleToRegion", "Teleport to region not possible");
                    SendMessageAlways(failedmsg, req.CircuitSceneID);
                }
            }
        }

        [PacketHandler(MessageType.TeleportRequest)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void HandleTeleportRequest(Message m)
        {
            TeleportRequest req = (TeleportRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            AgentCircuit circuit;
            if (Circuits.TryGetValue(req.CircuitSceneID, out circuit))
            {
                /* TODO: we need the specific local list for HG destinations */
                if (!TeleportTo(circuit.Scene, req.RegionID, req.Position, req.LookAt, Types.Grid.TeleportFlags.ViaLocation))
                {
                    TeleportFailed failedmsg = new TeleportFailed();
                    failedmsg.AgentID = ID;
                    failedmsg.Reason = this.GetLanguageString(CurrentCulture, "TeleportNotPossibleToRegion", "Teleport to region not possible");
                    SendMessageAlways(failedmsg, req.CircuitSceneID);
                }
            }
        }
    }
}
