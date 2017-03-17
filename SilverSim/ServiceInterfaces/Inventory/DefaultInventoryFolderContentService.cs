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
using SilverSim.Types.Inventory;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.ServiceInterfaces.Inventory
{
    public class DefaultInventoryFolderContentService : IInventoryFolderContentServiceInterface
    {
        readonly IInventoryFolderServiceInterface m_Service;

        public DefaultInventoryFolderContentService(IInventoryFolderServiceInterface service)
        {
            m_Service = service;
        }

        public bool TryGetValue(UUID principalID, UUID folderID, out InventoryFolderContent inventoryFolderContent)
        {
            try
            {
                inventoryFolderContent = this[principalID, folderID];
                return true;
            }
            catch
            {
                inventoryFolderContent = null;
                return false;
            }
        }

        public bool ContainsKey(UUID principalID, UUID folderID)
        {
            return m_Service.ContainsKey(principalID, folderID);
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public List<InventoryFolderContent> this[UUID principalID, UUID[] folderIDs]
        {
            get
            {
                List<InventoryFolderContent> res = new List<InventoryFolderContent>();
                foreach(UUID folder in folderIDs)
                {
                    try
                    {
                        res.Add(this[principalID, folder]);
                    }
                    catch
                    {
                        /* nothing that we should do here */
                    }
                }

                return res;
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public InventoryFolderContent this[UUID principalID, UUID folderID]
        {
            get 
            {
                InventoryFolderContent folderContent = new InventoryFolderContent();
                InventoryFolder folder;
                folder = m_Service[principalID, folderID];

                folderContent.Version = folder.Version;
                folderContent.Owner = folder.Owner;
                folderContent.FolderID = folder.ID;

                try
                {
                    folderContent.Folders = m_Service.GetFolders(principalID, folderID);
                }
                catch
                {
                    folderContent.Folders = new List<InventoryFolder>();
                }

                try
                {
                    folderContent.Items = m_Service.GetItems(principalID, folderID);
                }
                catch
                {
                    folderContent.Items = new List<InventoryItem>();
                }

                return folderContent;
            }
        }
    }
}
