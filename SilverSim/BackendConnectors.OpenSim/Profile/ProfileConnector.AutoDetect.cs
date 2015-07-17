﻿/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using log4net;
using SilverSim.Types;
using SilverSim.Types.Profile;
using System;
using System.Collections.Generic;

namespace SilverSim.BackendConnectors.OpenSim.Profile
{
    public partial class ProfileConnector
    {
#if DEBUG
        static readonly ILog m_Log = LogManager.GetLogger("PROFILE AUTO-DETECT HANDLER");
#endif

        public class AutoDetectClassifiedsConnector : IClassifiedsInterface
        {
            OpenSimClassifiedsConnector m_OpenSim;
            RobustClassifiedsConnector m_Robust;
            ProfileConnector m_Connector;
            
            public AutoDetectClassifiedsConnector(ProfileConnector connector, string url)
            {
                m_Connector = connector;
                m_OpenSim = new OpenSimClassifiedsConnector(connector, url);
                m_Robust = new RobustClassifiedsConnector(connector, url);
            }

            public Dictionary<UUID, string> getClassifieds(UUI user)
            {
                Dictionary<UUID, string> res;
                try
                {
                    res = m_OpenSim.getClassifieds(user);
                    m_Connector.m_Classifieds = m_OpenSim;
                    return res;
                }
                catch
#if DEBUG
                    (Exception e)
#endif
                {
#if DEBUG
                    m_Log.Debug("Classifieds.getClassifieds: OpenSimProfile", e);
#endif
                }
                try
                {
                    res = m_Robust.getClassifieds(user);
                    m_Connector.m_Classifieds = m_Robust;
                    return res;
                }
                catch
#if DEBUG
                    (Exception e)
#endif
                {
#if DEBUG
                    m_Log.Debug("Classifieds.getClassifieds: CoreProfile", e);
#endif
                }
                throw new NotImplementedException();
            }

            public ProfileClassified this[UUI user, UUID id]
            {
                get 
                {
                    ProfileClassified res;
                    try
                    {
                        res = m_OpenSim[user, id];
                        m_Connector.m_Classifieds = m_OpenSim;
                        return res;
                    }
                    catch
#if DEBUG
                        (Exception e)
#endif
                    {
#if DEBUG
                        m_Log.Debug("Classifieds.this[UUI, UUID]: OpenSimProfile", e);
#endif
                    }
                    try
                    {
                        res = m_Robust[user, id];
                        m_Connector.m_Classifieds = m_Robust;
                        return res;
                    }
                    catch
#if DEBUG
                        (Exception e)
#endif
                    {
#if DEBUG
                        m_Log.Debug("Classifieds.this[UUI, UUID]: CoreProfile", e);
#endif
                    }
                    throw new NotImplementedException();
                }
            }


            public void Update(ProfileClassified classified)
            {
                try
                {
                    m_OpenSim.Update(classified);
                    m_Connector.m_Classifieds = m_OpenSim;
                    return;
                }
                catch
#if DEBUG
                    (Exception e)
#endif
                {
#if DEBUG
                    m_Log.Debug("Classifieds.Update: OpenSimProfile", e);
#endif
                }
                try
                {
                    m_Robust.Update(classified);
                    m_Connector.m_Classifieds = m_Robust;
                    return;
                }
                catch
#if DEBUG
                    (Exception e)
#endif
                {
#if DEBUG
                    m_Log.Debug("Classifieds.Update: CoreProfile", e);
#endif
                }
                throw new NotImplementedException();
            }

            public void Delete(UUID id)
            {
                try
                {
                    m_OpenSim.Delete(id);
                    m_Connector.m_Classifieds = m_OpenSim;
                    return;
                }
                catch
#if DEBUG
                    (Exception e)
#endif
                {
#if DEBUG
                    m_Log.Debug("Classifieds.Delete: OpenSimProfile", e);
#endif
                }
                try
                {
                    m_Robust.Delete(id);
                    m_Connector.m_Classifieds = m_Robust;
                    return;
                }
                catch
#if DEBUG
                    (Exception e)
#endif
                {
#if DEBUG
                    m_Log.Debug("Classifieds.Delete: CoreProfile", e);
#endif
                }
                throw new NotImplementedException();
            }
        }

        public class AutoDetectPicksConnector : IPicksInterface
        {
            OpenSimPicksConnector m_OpenSim;
            RobustPicksConnector m_Robust;
            ProfileConnector m_Connector;

            public AutoDetectPicksConnector(ProfileConnector connector, string url)
            {
                m_Connector = connector;
                m_OpenSim = new OpenSimPicksConnector(connector, url);
                m_Robust = new RobustPicksConnector(connector, url);
            }

            public Dictionary<UUID, string> getPicks(UUI user)
            {
                Dictionary<UUID, string> res;
                try
                {
                    res = m_OpenSim.getPicks(user);
                    m_Connector.m_Picks = m_OpenSim;
                    return res;
                }
                catch
#if DEBUG
                    (Exception e)
#endif
                {
#if DEBUG
                    m_Log.Debug("Picks.getPicks: OpenSimProfile", e);
#endif
                }
                try
                {
                    res = m_Robust.getPicks(user);
                    m_Connector.m_Picks = m_Robust;
                    return res;
                }
                catch
#if DEBUG
                    (Exception e)
#endif
                {
#if DEBUG
                    m_Log.Debug("Picks.getPicks: CoreProfile", e);
#endif
                }
                throw new NotImplementedException();
            }

            public ProfilePick this[UUI user, UUID id]
            {
                get 
                {
                    ProfilePick res;
                    try
                    {
                        res = m_OpenSim[user, id];
                        m_Connector.m_Picks = m_OpenSim;
                        return res;
                    }
                    catch
#if DEBUG
                        (Exception e)
#endif
                    {
#if DEBUG
                        m_Log.Debug("Picks.this[UUI, UUID]: OpenSimProfile", e);
#endif
                    }
                    try
                    {
                        res = m_Robust[user, id];
                        m_Connector.m_Picks = m_Robust;
                        return res;
                    }
                    catch
#if DEBUG
                        (Exception e)
#endif
                    {
#if DEBUG
                        m_Log.Debug("Picks.this[UUI, UUID]: CoreProfile", e);
#endif
                    }
                    throw new NotImplementedException();
                }
            }


            public void Update(ProfilePick pick)
            {
                try
                {
                    m_OpenSim.Update(pick);
                    m_Connector.m_Picks = m_OpenSim;
                    return;
                }
                catch
#if DEBUG
                    (Exception e)
#endif
                {
#if DEBUG
                    m_Log.Debug("Picks.Update: OpenSimProfile", e);
#endif
                }
                try
                {
                    m_Robust.Update(pick);
                    m_Connector.m_Picks = m_Robust;
                    return;
                }
                catch
#if DEBUG
                    (Exception e)
#endif
                {
#if DEBUG
                    m_Log.Debug("Picks.Update: CoreProfile", e);
#endif
                }
                throw new NotImplementedException();
            }

            public void Delete(UUID id)
            {
                try
                {
                    m_OpenSim.Delete(id);
                    m_Connector.m_Picks = m_OpenSim;
                    return;
                }
                catch
#if DEBUG
                    (Exception e)
#endif
                {
#if DEBUG
                    m_Log.Debug("Picks.Delete: OpenSimProfile", e);
#endif
                }
                try
                {
                    m_Robust.Delete(id);
                    m_Connector.m_Picks = m_Robust;
                    return;
                }
                catch
#if DEBUG
                    (Exception e)
#endif
                {
#if DEBUG
                    m_Log.Debug("Picks.Delete: CoreProfile", e);
#endif
                }
                throw new NotImplementedException();
            }
        }

        public class AutoDetectNotesConnector : INotesInterface
        {
            ProfileConnector m_Connector;
            OpenSimNotesConnector m_OpenSim;
            RobustNotesConnector m_Robust;

            public AutoDetectNotesConnector(ProfileConnector connector, string url)
            {
                m_Connector = connector;
                m_OpenSim = new OpenSimNotesConnector(connector, url);
                m_Robust = new RobustNotesConnector(connector, url);
            }

            public string this[UUI user, UUI target]
            {
                get
                {
                    string res;
                    try
                    {
                        res = m_OpenSim[user, target];
                        m_Connector.m_Notes = m_OpenSim;
                        return res;
                    }
                    catch
#if DEBUG
                        (Exception e)
#endif
                    {
#if DEBUG
                        m_Log.Debug("Notes.this[UUI, UUI]: OpenSimProfile", e);
#endif
                    }
                    try
                    {
                        res = m_Robust[user, target];
                        m_Connector.m_Notes = m_Robust;
                        return res;
                    }
                    catch
#if DEBUG
                        (Exception e)
#endif
                    {
#if DEBUG
                        m_Log.Debug("Notes.this[UUI, UUI]: CoreProfile", e);
#endif
                    }
                    throw new NotImplementedException();
                }
                set
                {
                    try
                    {
                        m_OpenSim[user, target] = value;
                        m_Connector.m_Notes = m_OpenSim;
                        return;
                    }
                    catch
#if DEBUG
                        (Exception e)
#endif
                    {
#if DEBUG
                        m_Log.Debug("Notes.this[UUI, UUI]: OpenSimProfile", e);
#endif
                    }
                    try
                    {
                        m_Robust[user, target] = value;
                        m_Connector.m_Notes = m_Robust;
                        return;
                    }
                    catch
#if DEBUG
                        (Exception e)
#endif
                    {
#if DEBUG
                        m_Log.Debug("Notes.this[UUI, UUI]: CoreProfile", e);
#endif
                    }
                    throw new NotImplementedException();
                }
            }
        }

        public class AutoDetectUserPreferencesConnector : IUserPreferencesInterface
        {
            ProfileConnector m_Connector;
            OpenSimUserPreferencesConnector m_OpenSim;
            RobustUserPreferencesConnector m_Robust;

            public AutoDetectUserPreferencesConnector(ProfileConnector connector, string url)
            {
                m_Connector = connector;
                m_OpenSim = new OpenSimUserPreferencesConnector(connector, url);
                m_Robust = new RobustUserPreferencesConnector(connector, url);
            }

            public ProfilePreferences this[UUI user]
            {
                get
                {
                    ProfilePreferences res;
                    try
                    {
                        res = m_OpenSim[user];
                        m_Connector.m_Preferences = m_OpenSim;
                        return res;
                    }
                    catch
#if DEBUG
                        (Exception e)
#endif
                    {
#if DEBUG
                        m_Log.Debug("Preferences.this[UUI, UUI]: OpenSimProfile", e);
#endif
                    }
                    try
                    {
                        res = m_Robust[user];
                        m_Connector.m_Preferences = m_Robust;
                        return res;
                    }
                    catch
#if DEBUG
                        (Exception e)
#endif
                    {
#if DEBUG
                        m_Log.Debug("Preferences.this[UUI, UUI]: CoreProfile", e);
#endif
                    }
                    throw new NotImplementedException();
                }
                set
                {
                    try
                    {
                        m_OpenSim[user] = value;
                        m_Connector.m_Preferences = m_OpenSim;
                        return;
                    }
                    catch
#if DEBUG
                        (Exception e)
#endif
                    {
#if DEBUG
                        m_Log.Debug("Preferences.this[UUI, UUI]: OpenSimProfile", e);
#endif
                    }
                    try
                    {
                        m_Robust[user] = value;
                        m_Connector.m_Preferences = m_Robust;
                        return;
                    }
                    catch
#if DEBUG
                        (Exception e)
#endif
                    {
#if DEBUG
                        m_Log.Debug("Preferences.this[UUI, UUI]: CoreProfile", e);
#endif
                    }
                    throw new NotImplementedException();
                }
            }
        }

        public class AutoDetectPropertiesConnector : IPropertiesInterface
        {
            ProfileConnector m_Connector;
            OpenSimPropertiesConnector m_OpenSim;
            RobustPropertiesConnector m_Robust;

            public AutoDetectPropertiesConnector(ProfileConnector connector, string url)
            {
                m_Connector = connector;
                m_OpenSim = new OpenSimPropertiesConnector(connector, url);
                m_Robust = new RobustPropertiesConnector(connector, url);
            }

            public ProfileProperties this[UUI user]
            {
                get
                {
                    ProfileProperties res;
                    try
                    {
                        res = m_OpenSim[user];
                        m_Connector.m_Properties = m_OpenSim;
                        return res;
                    }
                    catch
#if DEBUG
                        (Exception e)
#endif
                    {
#if DEBUG
                        m_Log.Debug("Properties.this[UUI, UUI]: OpenSimProfile", e);
#endif
                    }
                    try
                    {
                        res = m_Robust[user];
                        m_Connector.m_Properties = m_Robust;
                        return res;
                    }
                    catch
#if DEBUG
                        (Exception e)
#endif
                    {
#if DEBUG
                        m_Log.Debug("Properties.this[UUI, UUI]: CoreProfile", e);
#endif
                    }
                    throw new NotImplementedException();
                }
            }
            public ProfileProperties this[UUI user, PropertiesUpdateFlags flags] 
            { 
                set
                {
                    try
                    {
                        m_OpenSim[user, flags] = value;
                        m_Connector.m_Properties = m_OpenSim;
                        return;
                    }
                    catch
#if DEBUG
                        (Exception e)
#endif
                    {
#if DEBUG
                        m_Log.Debug("Properties.this[UUI, UUI]: OpenSimProfile", e);
#endif
                    }
                    try
                    {
                        m_Robust[user, flags] = value;
                        m_Connector.m_Properties = m_Robust;
                        return;
                    }
                    catch
#if DEBUG
                        (Exception e)
#endif
                    {
#if DEBUG
                        m_Log.Debug("Properties.this[UUI, UUI]: CoreProfile", e);
#endif
                    }
                    throw new NotImplementedException();
                }
            }
        }
    }
}
