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
using SilverSim.Types.Experience;
using SilverSim.Types.Grid;

namespace SilverSim.Viewer.ExperienceTools
{
    public static class ExperienceInfoExtensionMethods
    {
        /* ExperienceInfo
         * <map>
         *   <key>public_id</key><uuid></uuid> x
         *   <key>agent_id</key><uuid></uuid> x
         *   <key>group_id</key><uuid></uuid> x
         *   <key>name</key><string></string> x
         *   <key>properties</key><integer></integer> x
         *   <!--<key>expiration</key><xx/>-->
         *   <key>description</key><string></string> x
         *   <!--<key>quota</key><xx/>-->
         *   <key>maturity</key><integer></integer> x
         *   <key>extended_metadata</key><string></string> x
         *   <key>slurl</key><url></url>
         * </map>
         * 
         * extended_metadata is string with <llsd><map><key>marketplace</key><string></string><key>logo</key><uuid>assetid</uuid></key>
         */
        public static Map ToMap(this ExperienceInfo info) => new Map
        {
            ["public_id"] = info.ID,
            ["agent_id"] = info.Owner.ID,
            ["group_id"] = info.Group.ID,
            ["name"] = (AString)info.Name,
            ["properties"] = (Integer)(int)info.Properties,
            ["description"] = (AString)(info.Description),
            ["maturity"] = (Integer)(int)info.Maturity,
            ["extended_metadata"] = (AString)info.ExtendedMetadata,
            ["slurl"] = (AString)info.SlUrl
        };

        public static ExperienceInfo ToExperienceInfo(this Map m) => new ExperienceInfo
        {
            ID = m["public_id"].AsUUID,
            Owner = new UGUI(m["agent_id"].AsUUID),
            Group = new UGI(m["group_id"].AsUUID),
            Name = m["name"].ToString(),
            Properties = (ExperiencePropertyFlags)m["properties"].AsInt,
            Description = m["description"].ToString(),
            Maturity = (RegionAccess)m["maturity"].AsInt,
            ExtendedMetadata = m["extended_metadata"].ToString(),
            SlUrl = m["slurl"].ToString()
        };
    }
}
