﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages;

namespace SilverSim.Scene.Types.Scene
{
    public interface ICircuit
    {
        void SendMessage(Message m);
    }
}
