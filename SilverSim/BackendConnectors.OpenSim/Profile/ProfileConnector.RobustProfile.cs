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

using SilverSim.Main.Common.Rpc;
using SilverSim.StructuredData.JSON;
using SilverSim.Types;
using SilverSim.Types.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.BackendConnectors.OpenSim.Profile
{
    public partial class ProfileConnector
    {
        public class RobustClassifiedsConnector : IClassifiedsInterface
        {
            string m_Uri;
            ProfileConnector m_Connector;

            public RobustClassifiedsConnector(ProfileConnector connector, string uri)
            {
                m_Connector = connector;
                m_Uri = uri;
            }

            public Dictionary<UUID, string> getClassifieds(UUI user)
            {
                Dictionary<UUID, string> data = new Dictionary<UUID, string>();
                Map m = new Map();
                m["creatorId"] = user.ID;
                IValue res = RPC.DoJson20RpcRequest(m_Uri, "avatarclassifiedsrequest", (string)UUID.Random, m, m_Connector.TimeoutMs);
                AnArray reslist = (((Map)res)["result"]) as AnArray;
                foreach(IValue iv in reslist)
                {
                    Map c = (Map)iv;
                    data[c["classifieduuid"].AsUUID] = c["name"].ToString();
                }
                return data;
            }

            public ProfileClassified this[UUI user, UUID id]
            {
                get 
                {
                    throw new NotImplementedException(); 
                }
            }


            public void Update(ProfileClassified classified)
            {
                throw new NotImplementedException();
            }

            public void Delete(UUID id)
            {
                throw new NotImplementedException();
            }
        }

        public class RobustPicksConnector : IPicksInterface
        {
            public RobustPicksConnector(ProfileConnector connector, string uri)
            {

            }

            public Dictionary<UUID, string> getPicks(UUI user)
            {
                throw new NotImplementedException();
            }

            public ProfilePick this[UUI user, UUID id]
            {
                get
                {
                    throw new NotImplementedException(); 
                }
            }


            public void Update(ProfilePick pick)
            {
                throw new NotImplementedException();
            }

            public void Delete(UUID id)
            {
                throw new NotImplementedException();
            }
        }

        public class RobustNotesConnector : INotesInterface
        {
            public RobustNotesConnector(ProfileConnector connector, string uri)
            {

            }

            public ProfileNotes this[UUI user, UUI target]
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }
        }

        public class RobustUserPreferencesConnector : IUserPreferencesInterface
        {
            public RobustUserPreferencesConnector(ProfileConnector connector, string uri)
            {

            }

            public ProfilePreferences this[UUI user]
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }
        }

        public class RobustPropertiesConnector : IPropertiesInterface
        {
            public RobustPropertiesConnector(ProfileConnector connector, string uri)
            {

            }

            public ProfileProperties this[UUI user]
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
