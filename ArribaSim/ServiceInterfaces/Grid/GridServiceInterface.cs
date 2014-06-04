﻿/*
 * ArribaSim is distributed under the terms of the
 * GNU General Public License v2 
 * with the following clarification and special exception.
 * 
 * Linking this code statically or dynamically with other modules is
 * making a combined work based on this code. Thus, the terms and
 * conditions of the GNU General Public License cover the whole
 * combination.
 * 
 * As a special exception, the copyright holders of this code give you
 * permission to link this code with independent modules to produce an
 * executable, regardless of the license terms of these independent
 * modules, and to copy and distribute the resulting executable under
 * terms of your choice, provided that you also meet, for each linked
 * independent module, the terms and conditions of the license of that
 * module. An independent module is a module which is not derived from
 * or based on this code. If you modify this code, you may extend
 * this exception to your version of the code, but you are not
 * obligated to do so. If you do not wish to do so, delete this
 * exception statement from your version.
 * 
 * License text is derived from GNU classpath text
 */

using ArribaSim.Types;
using ArribaSim.Types.Grid;
using System.Collections.Generic;

namespace ArribaSim.ServiceInterfaces.Grid
{
    public abstract class GridServiceInterface
    {
        #region Constructor
        public GridServiceInterface()
        {

        }
        #endregion

        #region Accessors
        public abstract RegionInfo this[UUID ScopeID, UUID regionID]
        {
            get;
        }
        public abstract RegionInfo this[UUID ScopeID, GridVector position]
        {
            get;
        }
        public abstract RegionInfo this[UUID ScopeID, uint gridX, uint gridY]
        {
            get;
        }
        public abstract RegionInfo this[UUID ScopeID, string regionName]
        {
            get;
        }

        #endregion

        #region Region Registration
        public abstract void RegisterRegion(RegionInfo regionInfo);
        public abstract void UnregisterRegion(UUID ScopeID, UUID RegionID);
        #endregion

        #region List accessors
        public abstract List<RegionInfo> GetDefaultRegions(UUID ScopeID);
        public abstract List<RegionInfo> GetFallbackRegions(UUID ScopeID);
        public abstract List<RegionInfo> GetDefaultHypergridRegions(UUID ScopeID);
        public abstract List<RegionInfo> GetRegionsByRange(UUID ScopeID, GridVector min, GridVector max);
        public abstract List<RegionInfo> GetNeighbours(UUID ScopeID, UUID RegionID);

        public abstract List<RegionInfo> SearchRegionsByName(UUID ScopeID, string searchString);
        #endregion
    }
}
