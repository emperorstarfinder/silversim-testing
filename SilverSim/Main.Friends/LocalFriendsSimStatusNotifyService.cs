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

using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Agent;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.Types;
using SilverSim.Viewer.Messages.Friend;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Main.Friends
{
    [PluginName("LocalFriendsSimNotifier")]
    [Description("Local friends on regions notifier")]
    public sealed class LocalFriendsSimStatusNotifyService : IFriendsSimStatusNotifyService, IPlugin
    {
        private SceneList m_Scenes;

        public void NotifyStatus(UGUI notifier, List<UGUI> list, bool isOnline)
        {
            foreach (UGUI id in list)
            {
                IAgent agent;
                UUID regionid;
                if (m_Scenes.TryFindRootAgent(id.ID, out agent, out regionid))
                {
                    if (isOnline)
                    {
                        var onlineNotification = new OnlineNotification();
                        onlineNotification.AgentIDs.Add(notifier.ID);
                        agent.SendMessageAlways(onlineNotification, regionid);
                    }
                    else
                    {
                        var offlineNotification = new OfflineNotification();
                        offlineNotification.AgentIDs.Add(notifier.ID);
                        agent.SendMessageAlways(offlineNotification, regionid);
                    }
                }
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
        }
    }
}
