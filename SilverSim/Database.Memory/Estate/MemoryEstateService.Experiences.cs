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
using SilverSim.Types.Experience;
using System.Collections.Generic;

namespace SilverSim.Database.Memory.Estate
{
    public sealed partial class MemoryEstateService : IEstateExperienceServiceInterface
    {
        private readonly RwLockedDictionaryAutoAdd<uint, RwLockedDictionary<UUID, EstateExperienceInfo>> m_Experiences = new RwLockedDictionaryAutoAdd<uint, RwLockedDictionary<UUID, EstateExperienceInfo>>(() => new RwLockedDictionary<UUID, EstateExperienceInfo>());
        List<EstateExperienceInfo> IEstateExperienceServiceInterface.this[uint estateID]
        {
            get
            {
                RwLockedDictionary<UUID, EstateExperienceInfo> experiences;
                var result = new List<EstateExperienceInfo>();
                if(m_Experiences.TryGetValue(estateID, out experiences))
                {
                    foreach(EstateExperienceInfo info in experiences.Values)
                    {
                        result.Add(new EstateExperienceInfo(info));
                    }
                }
                return result;
            }
        }

        bool IEstateExperienceServiceInterface.Remove(uint estateID, UEI experienceID)
        {
            RwLockedDictionary<UUID, EstateExperienceInfo> experiences;
            return m_Experiences.TryGetValue(estateID, out experiences) && experiences.Remove(experienceID.ID);
        }

        void IEstateExperienceServiceInterface.Store(EstateExperienceInfo info)
        {
            m_Experiences[info.EstateID][info.ExperienceID.ID] = new EstateExperienceInfo(info);
        }

        bool IEstateExperienceServiceInterface.TryGetValue(uint estateID, UEI experienceID, out EstateExperienceInfo info)
        {
            RwLockedDictionary<UUID, EstateExperienceInfo> experiences;
            if(m_Experiences.TryGetValue(estateID, out experiences) && experiences.TryGetValue(experienceID.ID, out info))
            {
                info = new EstateExperienceInfo(info);
                return true;
            }
            info = default(EstateExperienceInfo);
            return false;
        }
    }
}
