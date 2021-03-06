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

namespace SilverSim.Types
{
    /** <summary> Universal User Identifier </summary> */
    public sealed class UGUIWithName : IEquatable<UGUIWithName>
    {
        public UUID ID = UUID.Zero;
        public string FirstName = string.Empty;
        public string LastName = string.Empty;
        public Uri HomeURI;
        public bool IsAuthoritative; /* false means User Data has been validated through any available resolving service */

        public static explicit operator string(UGUIWithName v) => (v.HomeURI != null) ?
                string.Format("{0};{1};{2} {3}", v.ID.ToString(), v.HomeURI.ToString(), v.FirstName, v.LastName) :
                string.Format("{0}", v.ID.ToString());

        public override bool Equals(object obj)
        {
            var u = obj as UGUIWithName;
            return u != null && Equals(u);
        }

        public bool IsSet => ID != UUID.Zero;

        public bool Equals(UGUIWithName other) => ID == other.ID;

        public bool EqualsGrid(UGUIWithName uui)
        {
            if((uui.HomeURI != null && HomeURI == null) ||
                (uui.HomeURI == null && HomeURI != null))
            {
                return false;
            }
            else if (uui.HomeURI != null)
            {
                return uui.ID == ID && uui.HomeURI.Equals(HomeURI);
            }
            else
            {
                return uui.ID == ID;
            }
        }

        public override int GetHashCode() => ID.GetHashCode();

        public string CreatorData
        {
            get
            {
                return (HomeURI != null) ?
                    string.Format("{0};{1} {2}", HomeURI.ToString(), FirstName, LastName) :
                    string.Empty;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    HomeURI = null;
                    FirstName = string.Empty;
                    LastName = string.Empty;
                }
                else
                {
                    var parts = value.Split(new char[] { ';' }, 2, StringSplitOptions.None);
                    if (parts.Length < 2)
                    {
                        throw new ArgumentException("\"" + value + "\" is not a valid CreatorData string");
                    }
                    HomeURI = new Uri(parts[0]);
                    var names = parts[1].Split(new char[] { ' ' }, 2, StringSplitOptions.None);
                    FirstName = names[0];
                    LastName = (names.Length > 1) ?
                        names[1] :
                        string.Empty;
                }
            }
        }

        public string FullName
        {
            get
            {
                if (HomeURI == null)
                {
                    return string.Format("{0} {1}", FirstName.Replace(' ', '.'), LastName.Replace(' ', '.'));
                }
                else
                {
                    var hostName = HomeURI.IsDefaultPort ?
                        HomeURI.Host :
                        HomeURI.Host + ":" + HomeURI.Port.ToString();

                    return string.Format("{0}.{1} @{2}", FirstName.Replace(' ', '.'), LastName.Replace(' ', '.'), hostName);
                }
            }
            set
            {
                var names = value.Split(new char[] { ' ' }, 2, StringSplitOptions.None);
                if (names.Length < 2)
                {
                    FirstName = names[0];
                    LastName = string.Empty;
                    HomeURI = null;
                }
                else
                {
                    if (names[1].StartsWith("@"))
                    {
                        /* HG UUI */
                        HomeURI = new Uri("http://" + names[1]);
                        names = names[0].Split(new char[] { '.' }, 2, StringSplitOptions.None);

                        FirstName = names[0];
                        LastName = names.Length < 2 ? string.Empty : names[1];
                    }
                    else
                    {
                        FirstName = names[0];
                        LastName = names[1];
                        HomeURI = null;
                    }
                }
            }
        }

        public UGUIWithName()
        {
        }

        public UGUIWithName(UGUIWithName uui)
        {
            ID = uui.ID;
            FirstName = uui.FirstName;
            LastName = uui.LastName;
            HomeURI = uui.HomeURI;
        }

        public UGUIWithName(UUID ID)
        {
            this.ID = ID;
        }

        public UGUIWithName(UUID ID, string FirstName, string LastName, Uri HomeURI)
        {
            this.ID = ID;
            this.FirstName = FirstName;
            this.LastName = LastName;
            this.HomeURI = HomeURI;
        }

        public UGUIWithName(UUID ID, string FirstName, string LastName)
        {
            this.ID = ID;
            this.FirstName = FirstName;
            this.LastName = LastName;
        }

        public UGUIWithName(UUID ID, string creatorData)
        {
            this.ID = ID;
            var parts = creatorData.Split(Semicolon, 2);
            if (parts.Length < 2)
            {
                throw new ArgumentException("Invald UUI");
            }
            var names = parts[1].Split(Whitespace, 2);
            if (names.Length == 2)
            {
                LastName = names[1];
            }
            FirstName = names[0];
            HomeURI = new Uri(parts[0]);
        }

        public UGUIWithName(string uuiString)
        {
            var parts = uuiString.Split(Semicolon, 4); /* 4 allows for secret from friends entries */
            if (parts.Length < 2)
            {
                ID = new UUID(parts[0]);
                return;
            }
            ID = new UUID(parts[0]);
            if (parts.Length > 2)
            {
                var names = parts[2].Split(Whitespace, 2);
                if (names.Length == 2)
                {
                    LastName = names[1];
                }
                FirstName = names[0];
            }
            HomeURI = new Uri(parts[1]);
        }

        public static bool TryParse(string uuiString, out UGUIWithName uui)
        {
            UUID id;
            var firstName = string.Empty;
            var lastName = string.Empty;
            Uri homeURI;
            uui = default(UGUIWithName);
            var parts = uuiString.Split(Semicolon, 4); /* 4 allows for secrets from friends entries */
            if (parts.Length < 2)
            {
                if(!UUID.TryParse(parts[0], out id))
                {
                    return false;
                }
                uui = new UGUIWithName(id);
                return true;
            }
            if (!UUID.TryParse(parts[0], out id))
            {
                return false;
            }
            if (parts.Length > 2)
            {
                var names = parts[2].Split(Whitespace, 2);
                if (names.Length == 2)
                {
                    lastName = names[1];
                }
                firstName = names[0];
            }
            if (!Uri.TryCreate(parts[1], UriKind.Absolute, out homeURI))
            {
                return false;
            }
            uui = new UGUIWithName(id, firstName, lastName, homeURI);
            return true;
        }

        public override string ToString() => (HomeURI != null) ?
                string.Format("{0};{1};{2} {3}", ID.ToString(), HomeURI, FirstName.Replace(' ', '.'), LastName.Replace(' ', '.')) :
                ID.ToString();

        private static readonly char[] Semicolon = new char[1] { ';' };
        private static readonly char[] Whitespace = new char[1] { ' ' };

        public static UGUIWithName Unknown => new UGUIWithName();

        public static bool operator ==(UGUIWithName l, UGUIWithName r)
        {
            /* get rid of type specifics */
            object lo = l;
            object ro = r;
            if(lo == null && ro == null)
            {
                return true;
            }
            else if(lo == null || ro == null)
            {
                return false;
            }
            return l.Equals(r);
        }

        public static bool operator !=(UGUIWithName l, UGUIWithName r)
        {
            /* get rid of type specifics */
            object lo = l;
            object ro = r;
            if (lo == null && ro == null)
            {
                return false;
            }
            else if (lo == null || ro == null)
            {
                return true;
            }
            return !l.Equals(r);
        }
    }
}
