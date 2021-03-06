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
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System.Net;

namespace SilverSim.Viewer.Core.Capabilities
{
    public sealed class EstateAccess : ICapabilityInterface
    {
        private readonly SceneInterface m_Scene;
        private readonly ViewerAgent m_Agent;
        private readonly string m_RemoteIP;

        public string CapabilityName => "EstateAccess";

        public EstateAccess(ViewerAgent agent, SceneInterface scene, string remoteip)
        {
            m_Agent = agent;
            m_Scene = scene;
            m_RemoteIP = remoteip;
        }

        public void HttpRequestHandler(HttpRequest httpreq)
        {
            if (httpreq.CallerIP != m_RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (httpreq.Method != "GET")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            var allowedAgents = new AnArray();
            var bannedAgents = new AnArray();
            var allowedGroups = new AnArray();
            var managers = new AnArray();
            uint estateID = m_Scene.ParentEstateID;

            foreach(UGUI allowed in m_Scene.EstateService.EstateAccess.All[estateID])
            {
                allowedAgents.Add(new Map
                {
                    { "id", allowed.ID }
                });
            }

            foreach(UGUI banned in m_Scene.EstateService.EstateBans.All[estateID])
            {
                bannedAgents.Add(new Map
                {
                    { "id", banned.ID },
                    { "last_login_date", string.Empty },
                    { "ban_date", "0" },
                    { "banning_id", UUID.Zero }
                });
            }

            foreach(UGI group in m_Scene.EstateService.EstateGroup.All[estateID])
            {
                allowedGroups.Add(new Map
                {
                    { "id", group.ID }
                });
            }

            foreach(UGUI manager in m_Scene.EstateService.EstateManager.All[estateID])
            {
                managers.Add(new Map
                {
                    { "agent_id", manager.ID }
                });
            }

            httpreq.LlsdXmlResponse(new Map
            {
                { "AllowedAgents", allowedAgents },
                { "BannedAgents", bannedAgents },
                { "AllowedGroups", allowedGroups },
                { "Managers", managers }
            });
        }
    }
}
