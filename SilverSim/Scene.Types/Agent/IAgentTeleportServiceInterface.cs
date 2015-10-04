﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Grid;

namespace SilverSim.Scene.Types.Agent
{
    /* this interface is needed so we can resolve a cyclic reference */
    public interface IAgentTeleportServiceInterface
    {
        void Cancel();
        void ReleaseAgent(UUID fromSceneID);
        void CloseAgentOnRelease(UUID fromSceneID);
        void DisableSimulator(UUID fromSceneID, IAgent agent, RegionInfo regionInfo);
        void EnableSimulator(UUID fromSceneID, IAgent agent, DestinationInfo destinationRegion);
        GridType GridType { get; }
    }
}
