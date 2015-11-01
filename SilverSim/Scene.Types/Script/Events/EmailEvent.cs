﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Script.Events
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
    public struct EmailEvent : IScriptEvent
    {
        public string Time;
        public string Address;
        public string Subject;
        public string Message;
        public int NumberLeft;
    }
}
