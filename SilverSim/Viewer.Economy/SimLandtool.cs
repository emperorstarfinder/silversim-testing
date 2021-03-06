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
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces;
using SilverSim.Types;
using SilverSim.Types.StructuredData.XmlRpc;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace SilverSim.Viewer.Economy
{
    [Description("Simulator Landtool")]
    [PluginName("SimLandtool")]
    public sealed class SimLandtool : IPlugin, IGridInfoServiceInterface
    {
        private BaseHttpServer m_HttpServer;
        private SceneList m_Scenes;

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            m_Scenes.OnRegionAdd += OnSceneAdded;
            m_HttpServer = loader.HttpServer;
            loader.XmlRpcServer.XmlRpcMethods.Add("preflightBuyLandPrep", HandlePreFlightBuyLandPrep);
            loader.XmlRpcServer.XmlRpcMethods.Add("buyLandPrep", HandleBuyLandPrep);
        }

        private void OnSceneAdded(SceneInterface scene)
        {
            scene.SimulatorFeaturesExtrasMap["currency-base-uri"] = new AString(m_HttpServer.ServerURI);
        }

        public void GetGridInfo(Dictionary<string, string> dict)
        {
            dict["economy"] = m_HttpServer.ServerURI;
        }

        private static CultureInfo GetLanguageCulture(string language)
        {
            try
            {
                return new CultureInfo(language);
            }
            catch
            {
                return new CultureInfo("en");
            }
        }

        private XmlRpc.XmlRpcResponse HandlePreFlightBuyLandPrep(XmlRpc.XmlRpcRequest req)
        {
            Map structParam;
            UUID agentId;
            UUID secureSessionId;
            IValue language;
            IValue currencyBuy;
            if (!req.Params.TryGetValue(0, out structParam) ||
                !structParam.TryGetValue("agentId", out agentId) ||
                !structParam.TryGetValue("secureSessionId", out secureSessionId) ||
                !structParam.TryGetValue("language", out language) ||
                !structParam.TryGetValue("currencyBuy", out currencyBuy))
            {
                throw new XmlRpc.XmlRpcFaultException(4, "Missing parameters");
            }

            bool validated = false;
            IAgent agent;
            foreach (SceneInterface scene in m_Scenes.ValuesByKey1)
            {
                if (scene.Agents.TryGetValue(agentId, out agent) && agent.Session.SecureSessionID == secureSessionId)
                {
                    validated = true;
                    break;
                }
            }

            var resdata = new Map();
            if (!validated)
            {
                resdata.Add("success", false);
                resdata.Add("errorMessage", this.GetLanguageString(GetLanguageCulture(language.ToString()), "UnableToAuthenticate", "Unable to authenticate."));
                resdata.Add("errorURI", string.Empty);
            }
            else
            {
                var membership_level = new Map
                {
                    { "id", UUID.Zero },
                    { "description", "some level" }
                };
                var membership_levels = new Map
                {
                    ["level"] = membership_level
                };
                var landUse = new Map
                {
                    { "upgrade", false },
                    { "action", m_HttpServer.ServerURI }
                };
                var currency = new Map
                {
                    { "estimatedCost", currencyBuy }
                };
                var membership = new Map
                {
                    { "upgrade", false },
                    { "action", m_HttpServer.ServerURI },
                    { "levels", membership_levels }
                };
                resdata.Add("success", true);
                resdata.Add("membership", membership);
                resdata.Add("landUse", landUse);
                resdata.Add("currency", currency);
                if (currency.AsInt != 0)
                {
                    resdata.Add("confirm", "click");
                }
                else
                {
                    resdata.Add("confirm", string.Empty);
                }
            }

            return new XmlRpc.XmlRpcResponse { ReturnValue = resdata };
        }

        private XmlRpc.XmlRpcResponse HandleBuyLandPrep(XmlRpc.XmlRpcRequest req)
        {
            Map structParam;
            UUID agentId;
            UUID secureSessionId;
            IValue language;
            IValue currencyBuy;
            IValue confirm;
            if (!req.Params.TryGetValue(0, out structParam) ||
                !structParam.TryGetValue("agentId", out agentId) ||
                !structParam.TryGetValue("secureSessionId", out secureSessionId) ||
                !structParam.TryGetValue("language", out language) ||
                !structParam.TryGetValue("currencyBuy", out currencyBuy) ||
                !structParam.TryGetValue("confirm", out confirm))
            {
                throw new XmlRpc.XmlRpcFaultException(4, "Missing parameters");
            }

            bool validated = false;
            IAgent agent;
            foreach (SceneInterface scene in m_Scenes.ValuesByKey1)
            {
                if (scene.Agents.TryGetValue(agentId, out agent) && agent.Session.SecureSessionID == secureSessionId)
                {
                    validated = true;
                    break;
                }
            }

            var resdata = new Map();
            if (!validated)
            {
                resdata.Add("success", false);
                resdata.Add("errorMessage", this.GetLanguageString(GetLanguageCulture(language.ToString()), "UnableToAuthenticate", "Unable to authenticate."));
                resdata.Add("errorURI", string.Empty);
            }
            else
            {
                resdata.Add("success", true);
            }

            return new XmlRpc.XmlRpcResponse { ReturnValue = resdata };
        }
    }
}