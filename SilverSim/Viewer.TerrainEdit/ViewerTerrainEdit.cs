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

#pragma warning disable IDE0018
#pragma warning disable RCS1029

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Land;
using System;
using System.ComponentModel;

namespace SilverSim.Viewer.TerrainEdit
{
    [Description("Viewer Terraforming Handler")]
    [PluginName("ViewerTerrainEdit")]
    public class ViewerTerrainEdit : IPlugin, IPacketHandlerExtender
    {
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [PacketHandler(MessageType.ModifyLand)]
        public void HandleMessage(ViewerAgent agent, AgentCircuit circuit, Message m)
        {
            var req = (ModifyLand)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
            var scene = circuit.Scene;
            if(scene == null)
            {
                return;
            }

            Action<UGUI, SceneInterface, ModifyLand, ModifyLand.Data> modifier;

            foreach (var data in req.ParcelData)
            {
                if (data.South == data.North && data.West == data.East)
                {
                    if (Terraforming.PaintEffects.TryGetValue((Terraforming.StandardTerrainEffect)req.Action, out modifier))
                    {
                        modifier(agent.Owner, scene, req, data);
                    }
                }
                else
                {
                    if (Terraforming.FloodEffects.TryGetValue((Terraforming.StandardTerrainEffect)req.Action, out modifier))
                    {
                        modifier(agent.Owner, scene, req, data);
                    }
                }
            }
        }
    }
}
