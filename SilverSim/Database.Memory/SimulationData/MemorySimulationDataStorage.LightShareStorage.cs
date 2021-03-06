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

using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using EnvController = SilverSim.Scene.Types.SceneEnvironment.EnvironmentController;

namespace SilverSim.Database.Memory.SimulationData
{
    public partial class MemorySimulationDataStorage : ISimulationDataLightShareStorageInterface
    {
        private readonly RwLockedDictionary<UUID, KeyValuePair<EnvController.WindlightSkyData, EnvController.WindlightWaterData>> m_LightShareData = new RwLockedDictionary<UUID, KeyValuePair<EnvController.WindlightSkyData, EnvController.WindlightWaterData>>();

        bool ISimulationDataLightShareStorageInterface.TryGetValue(UUID regionID, out EnvController.WindlightSkyData skyData, out EnvController.WindlightWaterData waterData)
        {
            KeyValuePair<EnvController.WindlightSkyData, EnvController.WindlightWaterData> kvp;
            if(m_LightShareData.TryGetValue(regionID, out kvp))
            {
                skyData = kvp.Key;
                waterData = kvp.Value;
                return true;
            }
            else
            {
                skyData = EnvController.WindlightSkyData.Defaults;
                waterData = EnvController.WindlightWaterData.Defaults;
                return false;
            }
        }

        void ISimulationDataLightShareStorageInterface.Store(UUID regionID, EnvController.WindlightSkyData skyData, EnvController.WindlightWaterData waterData)
        {
            m_LightShareData[regionID] = new KeyValuePair<EnvController.WindlightSkyData, EnvController.WindlightWaterData>(skyData, waterData);
        }

        bool ISimulationDataLightShareStorageInterface.Remove(UUID regionID) =>
            m_LightShareData.Remove(regionID);
    }
}
