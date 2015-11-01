﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Script.Events
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
    public struct LandCollisionEvent : IScriptEvent
    {
        public enum CollisionType
        {
            Continuous,
            Start,
            End
        }
        public CollisionType Type;
        public Vector3 Position;
    }
}
