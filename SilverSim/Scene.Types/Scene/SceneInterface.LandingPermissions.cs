﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.Grid;
using SilverSim.Types.Groups;
using SilverSim.Types.Parcel;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        [Serializable]
        public class ParcelAccessDeniedException : Exception
        {
            public ParcelAccessDeniedException()
            {

            }

            public ParcelAccessDeniedException(string msg)
                : base(msg)
            {

            }

            protected ParcelAccessDeniedException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public ParcelAccessDeniedException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }

        bool CheckParcelAccessRights(IAgent agent, ParcelInfo parcel)
        {
            string nop;
            return CheckParcelAccessRights(agent, parcel, out nop);
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        bool CheckParcelAccessRights(IAgent agent, ParcelInfo parcel, out string reason)
        {
            reason = string.Empty;
            /* EO,EM,RO,PO must be able to enter parcel */
            if(agent.Owner.EqualsGrid(parcel.Owner) ||
                agent.Owner.EqualsGrid(Owner) ||
                IsEstateManager(agent.Owner))
            {
                return true;
            }

            if ((parcel.Flags & ParcelFlags.UseBanList) != 0 &&
                Parcels.BlackList[parcel.ID, agent.Owner])
            {
                reason = this.GetLanguageString(agent.CurrentCulture, "YouAreBannedFromTheParcel", "You are banned from the parcel.");
                return false;
            }

            if ((parcel.Flags & ParcelFlags.UseAccessList) != 0 &&
                Parcels.WhiteList[parcel.ID, agent.Owner])
            {
                return true;
            }

            if ((parcel.Flags & ParcelFlags.UseAccessGroup) != 0)
            {
                if(null == GroupsService)
                {
                    reason = this.GetLanguageString(agent.CurrentCulture, "ParcelIsGroupRestricted", "Parcel is group restricted");
                    return false;
                }
                else
                {
                    try
                    {
                        GroupMembership res = GroupsService.Memberships[parcel.Owner, parcel.Group, agent.Owner];
                        if(!res.Principal.EqualsGrid(agent.Owner) || !res.Group.Equals(parcel.Group))
                        {
                            reason = this.GetLanguageString(agent.CurrentCulture, "ParcelGroupDidNotValidate", "Parcel group did not validate.");
                            return false;
                        }
                    }
                    catch
                    {
                        reason = this.GetLanguageString(agent.CurrentCulture, "YouAreNotAMemberOfTheParcelGroup", "You are not a member of the parcel group.");
                        if((parcel.Flags & ParcelFlags.UseAccessList) != 0)
                        {
                            reason = this.GetLanguageString(agent.CurrentCulture, "YouAreNeitherAMemberOfTheParcelGroupOrOnTheAccessList", "You are neither a member of the parcel group or on the access list.");
                        }
                        return false;
                    }
                }
            }
            if((parcel.Flags & ParcelFlags.UseAccessList) != 0)
            {
                reason = this.GetLanguageString(agent.CurrentCulture, "YouAreNotOnTheParcelAccessList", "You are not on the parcel access list.");
                return false;
            }
            return true;
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        ParcelInfo FindNonBlockedParcel(IAgent agent, Vector3 destinationLocation)
        {
            ParcelInfo selectedParcel = null;
            foreach(ParcelInfo parcel in Parcels)
            {
                if(TeleportLandingType.Blocked == parcel.LandingType || 
                    parcel.PassPrice != 0 /* skip parcels with pass price here */ ||
                    !CheckParcelAccessRights(agent, parcel))
                {
                    continue;
                }

                if (null != selectedParcel)
                {
                    Vector3 a, b, c, d;
                    a = selectedParcel.AABBMin;
                    b = a;
                    c = selectedParcel.AABBMax;
                    d = c;
                    d.Y = a.Y;
                    b.Y = c.Y;
                    double parceldist = (a - destinationLocation).LengthSquared;
                    double f = (b - destinationLocation).LengthSquared;
                    if (f < parceldist)
                    {
                        parceldist = f;
                    }
                    f = (c - destinationLocation).LengthSquared;
                    if (f < parceldist)
                    {
                        parceldist = f;
                    }
                    f = (d - destinationLocation).LengthSquared;
                    if (f < parceldist)
                    {
                        parceldist = f;
                    }

                    a = parcel.AABBMin;
                    b = a;
                    c = parcel.AABBMax;
                    d = c;
                    d.Y = a.Y;
                    b.Y = c.Y;

                    if (parceldist > (a - destinationLocation).LengthSquared ||
                        parceldist > (b - destinationLocation).LengthSquared)
                    {
                        selectedParcel = parcel;
                    }
                }
                else
                {
                    selectedParcel = parcel;
                }
            }

            if(null == selectedParcel)
            {
                throw new ParcelAccessDeniedException(this.GetLanguageString(agent.CurrentCulture, "NoParcelsForTeleportingToFound", "No parcels for teleporting to found."));
            }
            return selectedParcel;
        }

        EstateInfo CheckEstateRights(IAgent agent)
        {
            UUI agentOwner = agent.Owner;
            uint estateID = EstateService.RegionMap[ID];
            EstateInfo estateInfo = EstateService[estateID];

            if ((estateInfo.Flags & RegionOptionFlags.PublicAllowed) == 0 &&
                !EstateService.EstateAccess[estateID, agentOwner])
            {
                List<UGI> estateGroups = EstateService.EstateGroup.All[estateID];
                List<GroupMembership> groups = GroupsService.Memberships[agentOwner, agentOwner];
                foreach (GroupMembership group in groups)
                {
                    if (estateGroups.Contains(group.Group))
                    {
                        return estateInfo;
                    }
                }
                throw new ParcelAccessDeniedException(this.GetLanguageString(agent.CurrentCulture, "YouAreNotAllowedToEnterTheEstate", "You are not allowed to enter the estate."));
            }

            return estateInfo;
        }

        public bool EnableLandingOwnerOverride { get; set; }

        public void DetermineInitialAgentLocation(IAgent agent, TeleportFlags teleportFlags, Vector3 destinationLocation, Vector3 destinationLookAt)
        {
            UUI agentOwner = agent.Owner;
            if (destinationLocation.X < 0 || destinationLocation.X >= SizeX)
            {
                destinationLocation.X = SizeX / 2f;
            }
            if (destinationLocation.Y < 0 || destinationLocation.Y >= SizeY)
            {
                destinationLocation.Y = SizeY / 2f;
            }

            ParcelInfo p = Parcels[destinationLocation];
            EstateInfo estateInfo;

            if ((!p.Owner.EqualsGrid(agentOwner) &&
                !IsEstateManager(agentOwner) &&
                !IsPossibleGod(agentOwner)) ||
                !EnableLandingOwnerOverride)
            {
                estateInfo = CheckEstateRights(agent);
                if(RegionSettings.TelehubObject != UUID.Zero && (estateInfo.Flags & RegionOptionFlags.AllowDirectTeleport) == 0)
                {
                }

                if(!CheckParcelAccessRights(agent, p))
                {
                    p = FindNonBlockedParcel(agent, destinationLocation);
                }

                /* do not block parcel owner, estate manager or estate owner when landing override is enabled */

                switch (p.LandingType)
                {
                    case TeleportLandingType.Blocked:
                        /* let's find another parcel */
                        p = FindNonBlockedParcel(agent, destinationLocation);
                        break;

                    case TeleportLandingType.Anywhere:
                        break;

                    case TeleportLandingType.LandingPoint:
                        destinationLocation = p.LandingPosition;
                        destinationLookAt = p.LandingLookAt;
                        break;

                    default:
                        break;
                }
            }
            else if(!EstateService.ContainsKey(EstateService.RegionMap[ID]))
            {
                throw new ParcelAccessDeniedException(this.GetLanguageString(agent.CurrentCulture, "EstateDataNotAvailable", "Estate data not available."));
            }

            if (destinationLocation.X < 0 || destinationLocation.X >= SizeX)
            {
                destinationLocation.X = SizeX / 2f;
            }
            if (destinationLocation.Y < 0 || destinationLocation.Y >= SizeY)
            {
                destinationLocation.Y = SizeY / 2f;
            }

            agent.Rotation = destinationLookAt.AgentLookAtToQuaternion();

            double t0_0 = Terrain[(uint)Math.Floor(destinationLocation.X), (uint)Math.Floor(destinationLocation.Y)];
            double t0_1 = Terrain[(uint)Math.Floor(destinationLocation.X), (uint)Math.Ceiling(destinationLocation.Y)];
            double t1_0 = Terrain[(uint)Math.Ceiling(destinationLocation.X), (uint)Math.Floor(destinationLocation.Y)];
            double t1_1 = Terrain[(uint)Math.Ceiling(destinationLocation.X), (uint)Math.Ceiling(destinationLocation.Y)];
            double t_x = agent.Position.X - Math.Floor(destinationLocation.X);
            double t_y = agent.Position.Y - Math.Floor(destinationLocation.Y);

            double t0 = t0_0.Lerp(t0_1, t_y);
            double t1 = t1_0.Lerp(t1_1, t_y);

            destinationLocation.Z = t0.Lerp(t1, t_x) + 1;

            agent.Position = destinationLocation;
        }
    }
}
