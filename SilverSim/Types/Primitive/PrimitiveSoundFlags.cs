﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types.Primitive
{
    [Flags]
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public enum PrimitiveSoundFlags : byte
    {
        Looped = 1,
        SyncMaster = 2,
        SyncSlave = 4,
        SyncPending = 8,
        Queue = 16,
        Stop = 32
    }
}
