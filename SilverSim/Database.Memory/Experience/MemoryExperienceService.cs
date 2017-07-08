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

using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Experience;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Experience;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Database.Memory.Experience
{
    [Description("Memory Experience Backend")]
    [PluginName("Experience")]
    public sealed partial class MemoryExperienceService : ExperienceServiceInterface
    {
        private readonly RwLockedDictionary<UUID, ExperienceInfo> m_Experiences = new RwLockedDictionary<UUID, ExperienceInfo>();

        public override IExperiencePermissionsInterface Permissions => this;
        public override IExperienceAdminInterface Admins => this;
        public override IExperienceKeyInterface KeyValueStore => this;

        public override ExperienceInfo this[UUID experienceID]
        {
            get
            {
                ExperienceInfo res;
                if (m_Experiences.TryGetValue(experienceID, out res))
                {
                    return new ExperienceInfo(res);
                }
                throw new KeyNotFoundException();
            }
        }

        public override void Add(ExperienceInfo info)
        {
            m_Experiences.Add(info.ID, info);
        }

        public override List<UUID> FindExperienceByName(string query)
        {
            List<UUID> res = new List<UUID>();
            foreach (KeyValuePair<UUID, ExperienceInfo> kvp in m_Experiences)
            {
                if (kvp.Value.Name.Contains(query))
                {
                    res.Add(kvp.Key);
                }
            }
            return res;
        }

        public override List<ExperienceInfo> FindExperienceInfoByName(string query)
        {
            List<ExperienceInfo> res = new List<ExperienceInfo>();
            foreach (KeyValuePair<UUID, ExperienceInfo> kvp in m_Experiences)
            {
                if (kvp.Value.Name.Contains(query))
                {
                    res.Add(new ExperienceInfo(kvp.Value));
                }
            }
            return res;
        }

        public override List<UUID> GetCreatorExperiences(UUI creator)
        {
            List<UUID> res = new List<UUID>();
            foreach (KeyValuePair<UUID, ExperienceInfo> kvp in m_Experiences)
            {
                if (kvp.Value.Creator.Equals(creator))
                {
                    res.Add(kvp.Key);
                }
            }
            return res;
        }

        public override List<UUID> GetGroupExperiences(UGI group)
        {
            List<UUID> res = new List<UUID>();
            foreach(KeyValuePair<UUID, ExperienceInfo> kvp in m_Experiences)
            {
                if(kvp.Value.Group.Equals(group))
                {
                    res.Add(kvp.Key);
                }
            }
            return res;
        }

        public override bool Remove(UUI requestingAgent, UUID id)
        {
            if(!Admins[id, requestingAgent])
            {
                return false;
            }

            bool f = m_Experiences.Remove(id);
            m_Perms.Remove(id);
            m_KeyValues.Remove(id);
            m_Admins.Remove(id);
            return f;
        }

        public override bool TryGetValue(UUID experienceID, out ExperienceInfo experienceInfo)
        {
            ExperienceInfo res;
            if(m_Experiences.TryGetValue(experienceID, out res))
            {
                experienceInfo = new ExperienceInfo(res);
                return true;
            }
            experienceInfo = null;
            return false;
        }

        public override void Update(UUI requestingAgent, ExperienceInfo info)
        {
            if(!m_Experiences.ContainsKey(info.ID))
            {
                throw new KeyNotFoundException();
            }
            if(!Admins[info.ID, requestingAgent])
            {
                throw new InvalidOperationException();
            }
            m_Experiences[info.ID] = new ExperienceInfo(info);
        }
    }
}
