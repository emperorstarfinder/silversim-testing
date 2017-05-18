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

using SilverSim.Viewer.Messages.Object;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart
    {
        public ObjectPropertiesFamily PropertiesFamily => new ObjectPropertiesFamily()
        {
            ObjectID = ID,
            OwnerID = Owner.ID,
            GroupID = ObjectGroup.Group.ID,
            BaseMask = m_Permissions.Base,
            OwnerMask = m_Permissions.Current,
            GroupMask = m_Permissions.Group,
            EveryoneMask = m_Permissions.EveryOne,
            NextOwnerMask = m_Permissions.NextOwner,
            OwnershipCost = ObjectGroup.OwnershipCost,
            SaleType = ObjectGroup.SaleType,
            SalePrice = ObjectGroup.SalePrice,
            Category = ObjectGroup.Category,
            LastOwnerID = ObjectGroup.LastOwner.ID,
            Name = Name,
            Description = Description
        };
    }
}
