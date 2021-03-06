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
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using System.ComponentModel;

namespace SilverSim.Database.Memory.Inventory
{
    [Description("Memory Inventory Transfer Transaction Backend")]
    [PluginName("InventoryTransferTransaction")]
    public sealed class MemoryInventoryTransferTransactionService : InventoryTransferTransactionServiceInterface, IPlugin
    {
        private readonly RwLockedDictionary<UUID, InventoryTransferInfo> m_Transactions = new RwLockedDictionary<UUID, InventoryTransferInfo>();

        public override bool ContainsKey(UUID userid, UUID dstTransactionID)
        {
            InventoryTransferInfo info;
            return m_Transactions.TryGetValue(dstTransactionID, out info) && info.DstAgent.ID == userid;
        }

        public override bool Remove(UUID userid, UUID dstTransactionID) => m_Transactions.RemoveIf(dstTransactionID, (info) => info.DstAgent.ID == userid);

        public void Startup(ConfigurationLoader loader)
        {
            /* nothing to do */
        }

        public override void Store(InventoryTransferInfo info) => m_Transactions[info.DstTransactionID] = new InventoryTransferInfo(info);

        public override bool TryGetValue(UUID userid, UUID dstTransactionID, out InventoryTransferInfo info)
        {
            InventoryTransferInfo foundInfo;
            if(m_Transactions.TryGetValue(dstTransactionID, out foundInfo) && foundInfo.DstAgent.ID == userid)
            {
                info = new InventoryTransferInfo(foundInfo);
            }
            else
            {
                info = null;
            }
            return info != null;
        }
    }
}
