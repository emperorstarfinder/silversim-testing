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
using SilverSim.Types.IM;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SilverSim.ServiceInterfaces.IM
{
    [Serializable]
    public class IMOfflineStoreFailedException : Exception
    {
        public IMOfflineStoreFailedException()
        {
        }

        public IMOfflineStoreFailedException(string message)
            : base(message)
        {
        }

        protected IMOfflineStoreFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public IMOfflineStoreFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    public class IMOfflineRetrieveFailedException : Exception
    {
        public IMOfflineRetrieveFailedException()
        {
        }

        public IMOfflineRetrieveFailedException(string message)
            : base(message)
        {
        }

        protected IMOfflineRetrieveFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public IMOfflineRetrieveFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public abstract class OfflineIMServiceInterface
    {
        #region Methods
        public abstract void StoreOfflineIM(GridInstantMessage im);
        public abstract List<GridInstantMessage> GetOfflineIMs(UUID principalID);
        public abstract void DeleteOfflineIM(ulong offlineImID);
        #endregion
    }
}
