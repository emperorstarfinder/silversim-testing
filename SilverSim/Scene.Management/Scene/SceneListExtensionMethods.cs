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
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.IM;
using SilverSim.Viewer.Messages.Economy;

namespace SilverSim.Scene.Management.Scene
{
    public static class SceneListExtensionMethods
    {
        public static bool TryFindRootAgent(this SceneList list, UUID regionid, UUID agentId, out IAgent agent)
        {
            agent = null;
            SceneInterface scene;
            return list.TryGetValue(regionid, out scene) && scene.RootAgents.TryGetValue(agentId, out agent);
        }

        public static bool TryFindRootAgent(this SceneList list, UUID agentId, out IAgent agent, out UUID regionid)
        {
            agent = null;
            regionid = default(UUID);
            foreach(SceneInterface scene in list.Values)
            {
                if(scene.RootAgents.TryGetValue(agentId, out agent))
                {
                    regionid = scene.ID;
                    return true;
                }
            }
            return false;
        }

        public static void SendMoneyBalance(this SceneList list, UUID agentId, int moneyBalance)
        {
            IAgent agent;
            UUID sceneID;
            if(list.TryFindRootAgent(agentId, out agent, out sceneID))
            {
                agent.SendMessageIfRootAgent(new MoneyBalanceReply
                {
                    AgentID = agentId,
                    MoneyBalance = moneyBalance
                }, sceneID);
            }
        }

        public static bool SendIM(this SceneList list, GridInstantMessage gim)
        {
            IAgent agent;
            UUID sceneID;
            bool result = false;
            if(list.TryFindRootAgent(gim.ToAgent.ID, out agent, out sceneID))
            {
                result = agent.IMSend(gim);
            }
            return result;
        }
    }
}
