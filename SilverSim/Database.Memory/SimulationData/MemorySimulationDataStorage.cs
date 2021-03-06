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

using log4net;
using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Types;
using System.ComponentModel;

namespace SilverSim.Database.Memory.SimulationData
{
    #region Service Implementation
    [Description("Memory Simulation Data Backend")]
    [PluginName("SimulationData")]
    public sealed partial class MemorySimulationDataStorage : SimulationDataStorageInterface, IPlugin
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MEMORY SIMULATION STORAGE");

        #region Constructor
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
        #endregion

        #region Properties
        public override ISimulationDataPhysicsConvexStorageInterface PhysicsConvexShapes => this;

        public override ISimulationDataEnvControllerStorageInterface EnvironmentController => this;

        public override ISimulationDataLightShareStorageInterface LightShare => this;

        public override ISimulationDataSpawnPointStorageInterface Spawnpoints => this;

        public override ISimulationDataEnvSettingsStorageInterface EnvironmentSettings => this;

        public override ISimulationDataObjectStorageInterface Objects => this;

        public override ISimulationDataParcelStorageInterface Parcels => this;
        public override ISimulationDataScriptStateStorageInterface ScriptStates => this;

        public override ISimulationDataTerrainStorageInterface Terrains => this;

        public override ISimulationDataRegionSettingsStorageInterface RegionSettings => this;

        public override ISimulationDataRegionExperiencesStorageInterface RegionExperiences => this;

        public override ISimulationDataRegionTrustedExperiencesStorageInterface TrustedExperiences => this;
        #endregion

        public override void RemoveRegion(UUID regionID)
        {
            RemoveAllScriptStatesInRegion(regionID);
            RegionSettings.Remove(regionID);
            RemoveTerrain(regionID);
            RemoveAllParcelsInRegion(regionID);
            EnvironmentController.Remove(regionID);
            LightShare.Remove(regionID);
            Spawnpoints.Remove(regionID);
            EnvironmentSettings.Remove(regionID);
            RemoveAllObjectsInRegion(regionID);
            Parcels.Experiences.RemoveAllFromRegion(regionID);
            RegionExperiences.RemoveRegion(regionID);
            TrustedExperiences.RemoveRegion(regionID);
        }
    }
    #endregion
}
