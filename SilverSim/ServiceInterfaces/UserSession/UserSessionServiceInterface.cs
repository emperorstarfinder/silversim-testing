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

using SilverSim.Types;
using SilverSim.Types.UserSession;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.UserSession
{
    public abstract class UserSessionServiceInterface
    {
        public abstract UserSessionInfo CreateSession(UGUI user);
        public abstract UserSessionInfo CreateSession(UGUI user, UUID sessionID, UUID secureSessionID);

        #region Session access
        public abstract UserSessionInfo this[UUID sessionID] { get; }
        public abstract List<UserSessionInfo> this[UGUI user] { get; }
        public abstract bool TryGetValue(UUID sessionID, out UserSessionInfo sessionInfo);
        public abstract bool ContainsKey(UUID sessionID);
        public abstract bool ContainsKey(UGUI user);
        public abstract bool Remove(UUID sessionID);
        #endregion

        #region Session variable access
        public abstract string this[UUID sessionID, string assoc, string varname]
        {
            get; set;
        }
        public abstract bool TryGetValue(UUID sessionID, string assoc, string varname, out string value);
        public abstract bool ContainsKey(UUID sessionID, string assoc, string varname);
        public abstract bool Remove(UUID sessionID, string assoc, string varname);
        #endregion
    }
}
