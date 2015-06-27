﻿/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;

namespace SilverSim.BackendConnectors.Simian
{
    public class SimianInventoryPlugin : ServicePluginHelo, IInventoryServicePlugin, IPlugin
    {
        public SimianInventoryPlugin()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        public InventoryServiceInterface Instantiate(string url)
        {
            return new Inventory.SimianInventoryConnector(url, (string)UUID.Zero);
        }

        public override string Name
        {
            get
            {
                return "opensim-simian";
            }
        }
    }
    [PluginName("InventoryPlugin")]
    public class SimianInventoryPluginFactory : IPluginFactory
    {
        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new SimianInventoryPlugin();
        }
    }

    public class SimianAssetPlugin : ServicePluginHelo, IAssetServicePlugin, IPlugin
    {
        public SimianAssetPlugin()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        public AssetServiceInterface Instantiate(string url)
        {
            return new Asset.SimianAssetConnector(url, (string)UUID.Zero);
        }

        public override string Name
        {
            get
            {
                return "opensim-simian";
            }
        }
    }

    [PluginName("AssetPlugin")]
    public class SimianAssetPluginFactory : IPluginFactory
    {
        public SimianAssetPluginFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new SimianAssetPlugin();
        }
    }
}
