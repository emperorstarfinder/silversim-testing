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

using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using System.Linq;

namespace SilverSim.Database.Memory.Estate
{
    public partial class MemoryEstateService : IEstateBanServiceInterface, IEstateBanServiceListAccessInterface
    {
        private readonly RwLockedDictionaryAutoAdd<uint, RwLockedDictionary<UGUI, bool>> m_EstateBanData = new RwLockedDictionaryAutoAdd<uint, RwLockedDictionary<UGUI, bool>>(() => new RwLockedDictionary<UGUI, bool>());

        List<UGUI> IEstateBanServiceListAccessInterface.this[uint estateID]
        {
            get
            {
                RwLockedDictionary<UGUI, bool> res;
                return (m_EstateBanData.TryGetValue(estateID, out res)) ?
                    new List<UGUI>(from uui in res.Keys where true select new UGUI(uui)) :
                    new List<UGUI>();
            }
        }

        bool IEstateBanServiceInterface.this[uint estateID, UGUI agent]
        {
            get
            {
                RwLockedDictionary<UGUI, bool> res;
                return m_EstateBanData.TryGetValue(estateID, out res) && res.ContainsKey(agent);
            }
            set
            {
                if (value)
                {
                    m_EstateBanData[estateID][agent] = true;
                }
                else
                {
                    m_EstateBanData[estateID].Remove(agent);
                }
            }
        }

        IEstateBanServiceListAccessInterface IEstateBanServiceInterface.All => this;
    }
}
