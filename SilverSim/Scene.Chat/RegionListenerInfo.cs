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

using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;

namespace SilverSim.Scene.Chat
{
    public class RegionListenerInfo : ListenerInfo
    {
        public override bool IsIgnorePosition => true;

        private static Vector3 GetPositionFunc() => Vector3.Zero;

        internal RegionListenerInfo(
            ChatHandler handler,
            int channel,
            string name,
            UUID id,
            string message,
            Func<UUID> getuuid,
            Func<UUID> getowner,
            Action<ListenEvent> send)
            : base(handler, channel, name, id, message, getuuid, GetPositionFunc, getowner, send, false)
        {
        }
    }
}
