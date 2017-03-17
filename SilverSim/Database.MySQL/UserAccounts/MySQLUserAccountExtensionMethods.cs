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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using SilverSim.Types.GridUser;
using SilverSim.Types;
using SilverSim.Types.Account;

namespace SilverSim.Database.MySQL.UserAccounts
{
    public static class MySQLUserAccountExtensionMethods
    {
        public static UserAccount ToUserAccount(this MySqlDataReader reader)
        {
            UserAccount info = new UserAccount();

            info.Principal.ID = reader.GetUUID("ID");
            info.Principal.FirstName = reader.GetString("FirstName");
            info.Principal.LastName = reader.GetString("LastName");
            info.Principal.HomeURI = null;
            info.Principal.IsAuthoritative = true;
            info.ScopeID = reader.GetUUID("ScopeID");
            info.Email = reader.GetString("Email");
            info.Created = reader.GetDate("Created");
            info.UserLevel = reader.GetInt32("UserLevel");
            info.UserFlags = reader.GetUInt32("UserFlags");
            info.UserTitle = reader.GetString("UserTitle");
            info.IsLocalToGrid = true;
            info.IsEverLoggedIn = reader.GetBool("IsEverLoggedIn");

            return info;
        }
    }
}
