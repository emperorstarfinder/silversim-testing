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
using SilverSim.Viewer.Messages.LayerData;
using System.Collections.Generic;

namespace SilverSim.Database.Memory.SimulationData
{
    public partial class MemorySimulationDataStorage : ISimulationDataTerrainStorageInterface
    {
        internal readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, byte[]>> m_TerrainData = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, byte[]>>(() => new RwLockedDictionary<uint, byte[]>());
        internal readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, byte[]>> m_DefaultTerrainData = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, byte[]>>(() => new RwLockedDictionary<uint, byte[]>());

        List<LayerPatch> ISimulationDataTerrainStorageInterface.this[UUID regionID]
        {
            get
            {
                RwLockedDictionary<uint, byte[]> patchesData;
                var patches = new List<LayerPatch>();
                if (m_TerrainData.TryGetValue(regionID, out patchesData))
                {
                    foreach(var kvp in patchesData)
                    {
                        var patch = new LayerPatch
                        {
                            ExtendedPatchID = kvp.Key,
                            Serialization = kvp.Value
                        };
                        patches.Add(patch);
                    }
                }
                return patches;
            }
        }

        public void SaveAsDefault(UUID regionID)
        {
            RwLockedDictionary<uint, byte[]> patchesData;
            if (m_TerrainData.TryGetValue(regionID, out patchesData))
            {
                foreach (var kvp in patchesData)
                {
                    m_DefaultTerrainData[regionID][kvp.Key] = kvp.Value;
                }
            }
        }

        public bool TryGetDefault(UUID regionID, List<LayerPatch> list)
        {
            RwLockedDictionary<uint, byte[]> patchesData;
            if (m_DefaultTerrainData.TryGetValue(regionID, out patchesData))
            {
                foreach (var kvp in patchesData)
                {
                    var patch = new LayerPatch
                    {
                        ExtendedPatchID = kvp.Key,
                        Serialization = kvp.Value
                    };
                    list.Add(patch);
                }
                return true;
            }
            return false;
        }

        private void RemoveTerrain(UUID regionID)
        {
            m_TerrainData.Remove(regionID);
        }
    }
}
