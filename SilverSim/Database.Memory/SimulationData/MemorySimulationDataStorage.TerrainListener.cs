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

using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Viewer.Messages.LayerData;
using System.Threading;

namespace SilverSim.Database.Memory.SimulationData
{
    public partial class MemorySimulationDataStorage
    {
        public class MemoryTerrainListener : TerrainListener
        {
            readonly UUID m_RegionID;
            readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, byte[]>> m_Data;

            public MemoryTerrainListener(RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, byte[]>> data, UUID regionID)
            {
                m_Data = data;
                m_RegionID = regionID;
            }

            protected override void StorageTerrainThread()
            {
                Thread.CurrentThread.Name = "Storage Terrain Thread: " + m_RegionID.ToString();

                var knownSerialNumbers = new C5.TreeDictionary<uint, uint>();

                while (!m_StopStorageThread || m_StorageTerrainRequestQueue.Count != 0)
                {
                    LayerPatch req;
                    try
                    {
                        req = m_StorageTerrainRequestQueue.Dequeue(1000);
                    }
                    catch
                    {
                        continue;
                    }

                    var serialNumber = req.Serial;

                    if (!knownSerialNumbers.Contains(req.ExtendedPatchID) || knownSerialNumbers[req.ExtendedPatchID] != req.Serial)
                    {
                        m_Data[m_RegionID][req.ExtendedPatchID] = req.Serialization;
                        knownSerialNumbers[req.ExtendedPatchID] = serialNumber;
                    }
                }
            }
        }

        public override TerrainListener GetTerrainListener(UUID regionID)
        {
            return new MemoryTerrainListener(m_TerrainData, regionID);
        }
    }
}
