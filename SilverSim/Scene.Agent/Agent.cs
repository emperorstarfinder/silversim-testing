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

using log4net;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Neighbor;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.ServiceInterfaces.Experience;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.MuteList;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.ServiceInterfaces.UserSession;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.Estate;
using SilverSim.Types.Grid;
using SilverSim.Types.Groups;
using SilverSim.Types.IM;
using SilverSim.Types.Parcel;
using SilverSim.Types.Primitive;
using SilverSim.Types.Script;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Agent;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SilverSim.Scene.Agent
{
    public abstract partial class Agent : IAgent
    {
        public bool IgnorePhysicsLocationUpdates => false;

        private static readonly ILog m_Log = LogManager.GetLogger("AGENT");
        protected readonly object m_DataLock = new object();

        #region Agent fields
        private double m_Health = 100f;
        private double m_HealRate = 0.5;
        #endregion

        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        #region Properties
        public Uri HomeURI { get; }
        public abstract ILocalIDAccessor LocalID { get; }
        #endregion

        private UUID m_TracksAgentID;

        public UUID TracksAgentID
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_TracksAgentID;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_TracksAgentID = value;
                }
            }
        }

        public double PhysicsGravityMultiplier
        {
            get { return 1; }

            set
            {
                /* nothing to do */
            }
        }

        public PathfindingType PathfindingType
        {
            get { return PathfindingType.Avatar; }

            set
            {
                /* setting intentionally ignored */
            }
        }

        public double WalkableCoefficientAvatar
        {
            get { return 0; }
            set { /* setting intentionally ignored */ }
        }

        public double WalkableCoefficientA
        {
            get { return 0; }
            set { /* setting intentionally ignored */ }
        }

        public double WalkableCoefficientB
        {
            get { return 0; }
            set { /* setting intentionally ignored */ }
        }

        public double WalkableCoefficientC
        {
            get { return 0; }
            set { /* setting intentionally ignored */ }
        }

        public double WalkableCoefficientD
        {
            get { return 0; }
            set { /* setting intentionally ignored */ }
        }

        public double Damage
        {
            get { return 0; }
            set {  /* setting intentionally ignored */ }
        }

        public bool HasCausedDamage
        {
            get { return false; }
            set {  /* setting intentionally ignored */ }
        }

        public abstract AgentUpdateInfo GetUpdateInfo(UUID sceneID);
        public void IncUpdateInfoSerialNo()
        {
            GetUpdateInfo(SceneID)?.IncSerialNumber();
        }

        public abstract void SendKillObject(UUID sceneID);
        public abstract void SendUpdateObject(UUID sceneID);

        protected Agent(UUID agentId, Uri homeURI)
        {
            Attachments = new AgentAttachments();
            Group = UGI.Unknown;
            ID = agentId;
            HomeURI = homeURI;
            m_AnimationController = new AgentAnimationController(this, SendAnimations);
            AllowUnsit = true;
        }

        ~Agent()
        {
            lock (m_DataLock)
            {
                m_SittingOnObject = null;
            }
            m_BakeCache?.Dispose();
        }

        public abstract ClientInfo Client { get; }
        public abstract UserAccount UntrustedAccountInfo { get; }
        public abstract SessionInfo Session { get; }
        public abstract List<GridType> SupportedGridTypes { get; }
        public abstract IAgentTeleportServiceInterface ActiveTeleportService { get; set; }
        public abstract bool IsAway { get; }
        public virtual double DrawDistance { get; }
        public abstract List<AgentControlData> ActiveControls { get; }

        public void GetBoundingBox(out BoundingBox box) => box = new BoundingBox
        {
            CenterOffset = Vector3.Zero,
            Size = Size * Rotation
        };

        public virtual void InvokeOnPositionUpdate()
        {
            foreach (Action<IObject> del in OnPositionChange?.GetInvocationList().OfType<Action<IObject>>())
            {
                try
                {
                    del(this);
                }
                catch (Exception e)
                {
                    m_Log.Debug("Exception during OnPositionUpdate processing", e);
                }
            }
        }

        #region IObject Properties

        private ObjectGroup m_SittingOnObject;

        public bool AllowUnsit { get; set; }

        public void SetSittingOn(ObjectGroup sitOn, Vector3 position, Quaternion rotation)
        {
            if(sitOn == null)
            {
                throw new ArgumentNullException(nameof(sitOn));
            }
            lock (m_DataLock)
            {
                m_SittingOnObject = sitOn;
                m_GlobalRotation = rotation * sitOn.GlobalRotation;
                m_GlobalPosition = position * sitOn.GlobalRotation + sitOn.GlobalPosition;
            }
            IncUpdateInfoSerialNo();
            UUID sceneID = SceneID;
            if(sceneID != null)
            {
                SendUpdateObject(sceneID);
            }
        }

        public void ClearSittingOn(Vector3 targetPosition, Quaternion targetRotation)
        {
            lock (m_DataLock)
            {
                if (m_SittingOnObject != null)
                {
                    m_SittingOnObject = null;
                    m_GlobalPosition = targetPosition;
                    m_GlobalRotation = targetRotation;
                    AllowUnsit = false;
                }
            }
            IncUpdateInfoSerialNo();
            UUID sceneID = SceneID;
            if (sceneID != UUID.Zero)
            {
                SendUpdateObject(sceneID);
            }
        }

        public ObjectGroup SittingOnObject
        {
            /* we need to guard against our position routines and so on */
            get
            {
                lock (m_DataLock)
                {
                    return m_SittingOnObject;
                }
            }
        }

        public abstract bool IsRunning { get; }
        public abstract bool IsFlying { get; }

        public UUID ID { get; }

        public string Name
        {
            get { return string.Format("{0} {1}", FirstName, LastName); }

            set
            {
                string[] parts = value.Split(new char[] { ' ' }, 2);
                FirstName = parts[0];
                if (parts.Length > 1)
                {
                    LastName = parts[1];
                }
            }
        }

        public UGI Group { get; set; }

        public Vector3 LookAt
        {
            get { return new Vector3(1, 0, 0) * Rotation; }

            set
            {
                Vector3 delta = value.Normalize();
                Rotation = Quaternion.CreateFromEulers(new Vector3(0, 0, Math.Atan2(delta.Y, delta.X)));
            }
        }

        public UGUIWithName NamedOwner => new UGUIWithName
        {
            FirstName = FirstName,
            LastName = LastName,
            ID = ID,
            HomeURI = HomeURI
        };

        public UGUI Owner
        {
            get
            {
                return new UGUI
                {
                    ID = ID,
                    HomeURI = HomeURI
                };
            }

            set { throw new NotSupportedException(); }
        }

        public string Description
        {
            get { return string.Empty; }

            set { throw new NotSupportedException(); }
        }

        private double m_HoverHeight;
        public double HoverHeight
        {
            get { return m_HoverHeight; }

            set { m_HoverHeight = value.Clamp(-2f, 2f); }
        }

        public Vector3 GlobalPositionOnGround
        {
            get
            {
                Vector3 v = GlobalPosition;
                v.Z -= m_AvatarSize.Z / 2;
                return v;
            }
        }

        private Vector3 m_GlobalPosition = Vector3.Zero;

        public Vector3 Position
        {
            get
            {
                lock (m_DataLock)
                {
                    return (m_SittingOnObject != null) ?
                        m_GlobalPosition - m_SittingOnObject.Position :
                        m_GlobalPosition;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_GlobalPosition = (m_SittingOnObject != null) ?
                        value + m_SittingOnObject.Position :
                        value;
                }
                IncUpdateInfoSerialNo();
                InvokeOnPositionUpdate();
            }
        }

        private Vector3 m_Velocity = Vector3.Zero;
        public Vector3 Velocity
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_Velocity;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_Velocity = value;
                }
                IncUpdateInfoSerialNo();
            }
        }

        private Vector3 m_AngularVelocity = Vector3.Zero;
        public Vector3 AngularVelocity
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_AngularVelocity;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_AngularVelocity = value;
                }
                IncUpdateInfoSerialNo();
            }
        }

        private Vector3 m_AngularAcceleration = Vector3.Zero;
        public Vector3 AngularAcceleration
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_AngularAcceleration;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_AngularAcceleration = value;
                }
                IncUpdateInfoSerialNo();
            }
        }

        public Vector3 GlobalPosition
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_GlobalPosition;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_GlobalPosition = value;
                }
                IncUpdateInfoSerialNo();
                InvokeOnPositionUpdate();
            }
        }

        public Vector3 LocalPosition
        {
            get
            {
                lock (m_DataLock)
                {
                    return (m_SittingOnObject != null) ?
                        m_GlobalPosition - m_SittingOnObject.Position :
                        m_GlobalPosition;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_GlobalPosition = (m_SittingOnObject != null) ?
                        value + m_SittingOnObject.Position :
                        value;
                }
                IncUpdateInfoSerialNo();
                InvokeOnPositionUpdate();
            }
        }

        private Vector3 m_Acceleration = Vector3.Zero;

        public Vector3 Acceleration
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_Acceleration;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_Acceleration = value;
                }
                IncUpdateInfoSerialNo();
            }
        }

        private Quaternion m_GlobalRotation = Quaternion.Identity;

        public Quaternion GlobalRotation
        {
            get
            {
                lock (m_DataLock)
                {
                    return (m_SittingOnObject != null) ?
                        m_GlobalRotation * m_SittingOnObject.Rotation :
                        m_GlobalRotation;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_GlobalRotation = (m_SittingOnObject != null) ?
                        value / m_SittingOnObject.Rotation :
                        value;
                }
                IncUpdateInfoSerialNo();
                InvokeOnPositionUpdate();
            }
        }

        public Quaternion LocalRotation
        {
            get
            {
                lock (m_DataLock)
                {
                    return (m_SittingOnObject != null) ?
                        m_GlobalRotation / m_SittingOnObject.Rotation :
                        m_GlobalRotation;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_GlobalRotation = (m_SittingOnObject != null) ?
                        value * m_SittingOnObject.Rotation :
                        value;
                }
                IncUpdateInfoSerialNo();
                InvokeOnPositionUpdate();
            }
        }

        public Quaternion Rotation
        {
            get
            {
                lock (m_DataLock)
                {
                    return LocalRotation;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    LocalRotation = value;
                }
                IncUpdateInfoSerialNo();
            }
        }

        public abstract bool IsInScene(SceneInterface scene);

        public abstract UUID SceneID { get; set; }

        public abstract void AddWaitForRoot(SceneInterface scene, Action<object, bool> del, object o);
        #endregion

        #region IObject Methods
        public void GetPrimitiveParams(IEnumerator<IValue> enumerator, AnArray paramList, CultureInfo currentCulture) =>
            GetPrimitiveParams(enumerator, paramList);

        public void GetPrimitiveParams(IEnumerator<IValue> enumerator, AnArray paramList)
        {
            PrimitiveParamsType paramtype = ParamsHelper.GetPrimParamType(enumerator);
            switch (paramtype)
            {
                case PrimitiveParamsType.Name:
                    paramList.Add(Name);
                    break;

                case PrimitiveParamsType.Desc:
                    paramList.Add(Description);
                    break;

                case PrimitiveParamsType.Position:
                    paramList.Add(Position);
                    break;

                case PrimitiveParamsType.PosLocal:
                    paramList.Add(LocalPosition);
                    break;

                case PrimitiveParamsType.Rotation:
                    paramList.Add(GlobalRotation);
                    break;

                case PrimitiveParamsType.RotLocal:
                    paramList.Add(LocalRotation);
                    break;

                case PrimitiveParamsType.Size:
                    paramList.Add(Size);
                    break;

                default:
                    if (Enum.IsDefined(typeof(PrimitiveParamsType), (int)paramtype))
                    {
                        throw new LocalizedScriptErrorException(this, "PRIM0NotAllowedForAgents", "{0} not allowed for agents", paramtype.GetLslName());
                    }
                    else
                    {
                        throw new LocalizedScriptErrorException(this, "PRIMInvalidParameterType0", "Invalid primitive parameter type {0}", paramtype.GetLslName());
                    }
            }
        }

        public void SetPrimitiveParams(AnArray.MarkEnumerator enumerator)
        {
            PrimitiveParamsType paramtype = ParamsHelper.GetPrimParamType(enumerator);
            switch (paramtype)
            {
                case PrimitiveParamsType.Name:
                    Name = ParamsHelper.GetString(enumerator, "PRIM_NAME");
                    break;

                case PrimitiveParamsType.Desc:
                    Description = ParamsHelper.GetString(enumerator, "PRIM_DESC");
                    break;

                case PrimitiveParamsType.Position:
                    Position = ParamsHelper.GetVector(enumerator, "PRIM_POSITION");
                    break;

                case PrimitiveParamsType.PosLocal:
                    LocalPosition = ParamsHelper.GetVector(enumerator, "PRIM_POS_LOCAL");
                    break;

                case PrimitiveParamsType.Rotation:
                    GlobalRotation = ParamsHelper.GetRotation(enumerator, "PRIM_ROTATION").Normalize();
                    break;

                case PrimitiveParamsType.RotLocal:
                    LocalRotation = ParamsHelper.GetRotation(enumerator, "PRIM_ROT_LOCAL").Normalize();
                    break;

                default:
                    if (Enum.IsDefined(typeof(PrimitiveParamsType), (int)paramtype))
                    {
                        throw new LocalizedScriptErrorException(this, "PRIM0NotAllowedForAgents", "{0} not allowed for agents", paramtype.GetLslName());
                    }
                    else
                    {
                        throw new LocalizedScriptErrorException(this, "PRIMInvalidParameterType0", "Invalid primitive parameter type {0}", paramtype.GetLslName());
                    }
            }
        }

        public double GenderVp
        {
            get
            {
                byte[] vp = m_VisualParams;
                return (vp.Length > 31) ? vp[31] / 255 : 0;
            }
        }

        public void GetObjectDetails(IEnumerator<IValue> enumerator, AnArray paramList)
        {
            while (enumerator.MoveNext())
            {
                /* LSL ignores non-integer parameters, see http://wiki.secondlife.com/wiki/LlGetObjectDetails. */
                if (enumerator.Current.LSL_Type != LSLValueType.Integer)
                {
                    continue;
                }
                switch (ParamsHelper.GetObjectDetailsType(enumerator))
                {
                    case ObjectDetailsType.Name:
                        paramList.Add(Name);
                        break;

                    case ObjectDetailsType.Desc:
                        paramList.Add(Description);
                        break;

                    case ObjectDetailsType.Pos:
                        paramList.Add(Position);
                        break;

                    case ObjectDetailsType.Rot:
                        paramList.Add(GlobalRotation);
                        break;

                    case ObjectDetailsType.Velocity:
                        paramList.Add(Velocity);
                        break;

                    case ObjectDetailsType.LastOwner:
                    case ObjectDetailsType.Owner:
                    case ObjectDetailsType.Creator:
                    case ObjectDetailsType.RezzerKey:
                        paramList.Add(ID);
                        break;

                    case ObjectDetailsType.Root:
                        {
                            ObjectGroup grp = SittingOnObject;
                            paramList.Add(grp != null ? grp.ID : ID);
                        }
                        break;

                    case ObjectDetailsType.Group:
                        paramList.Add(Group.ID);
                        break;

                    case ObjectDetailsType.GroupTag:
                        paramList.Add(GetGroupTag());
                        break;

                    case ObjectDetailsType.AttachedSlotsAvailable:
                        paramList.Add(Attachments.AvailableSlots);
                        break;

                    case ObjectDetailsType.RunningScriptCount:
                        {
                            int runningScriptCount = 0;
                            foreach (ObjectGroup grp in Attachments.All)
                            {
                                foreach (ObjectPart part in grp.Values)
                                {
                                    runningScriptCount += part.Inventory.CountRunningScripts;
                                }
                            }
                            paramList.Add(runningScriptCount);
                        }
                        break;

                    case ObjectDetailsType.TotalScriptCount:
                        {
                            int n = 0;
                            foreach (ObjectGroup grp in Attachments.All)
                            {
                                foreach (ObjectPart part in grp.Values)
                                {
                                    n += part.Inventory.CountScripts;
                                }
                            }
                            paramList.Add(n);
                        }
                        break;

                    case ObjectDetailsType.PrimEquivalence:
                        paramList.Add(1);
                        break;

                    case ObjectDetailsType.PathfindingType:
                        paramList.Add(1); // this is OPT_AVATAR
                        break;

                    case ObjectDetailsType.ScriptTime:
                    case ObjectDetailsType.ServerCost:
                    case ObjectDetailsType.StreamingCost:
                    case ObjectDetailsType.PhysicsCost:
                        paramList.Add(0f);
                        break;

                    case ObjectDetailsType.ScriptMemory:
                    case ObjectDetailsType.CharacterTime:
                    case ObjectDetailsType.AttachedPoint:
                        paramList.Add(0);
                        break;

                    case ObjectDetailsType.Physics:
                        paramList.Add(true);
                        break;

                    case ObjectDetailsType.Phantom:
                    case ObjectDetailsType.TempOnRez:
                    case ObjectDetailsType.TempAttached:
                        paramList.Add(false);
                        break;

                    case ObjectDetailsType.HoverHeight:
                        paramList.Add(HoverHeight);
                        break;

                    case ObjectDetailsType.BodyShapeType:
                        paramList.Add(GenderVp);
                        break;

                    case ObjectDetailsType.ClickAction:
                        paramList.Add((int)ClickActionType.None);
                        break;

                    case ObjectDetailsType.Omega:
                        paramList.Add(AngularVelocity);
                        break;

                    case ObjectDetailsType.CreationTime:
                        paramList.Add(string.Empty);
                        break;

                    case ObjectDetailsType.SelectCount:
                    case ObjectDetailsType.SitCount:
                        paramList.Add(0);
                        break;

                    case ObjectDetailsType.RenderWeight:
                    default:
                        paramList.Add(-1);
                        break;
                }
            }
        }

        public virtual string GetGroupTag()
        {
            if (GroupsService != null)
            {
                GroupActiveMembership gam;
                GroupRole role;
                if (GroupsService.ActiveMembership.TryGetValue(Owner, Owner, out gam) &&
                    GroupsService.Roles.TryGetValue(Owner, gam.Group, gam.SelectedRoleID, out role))
                {
                    return role.Title;
                }
            }

            return string.Empty;
        }

        public void PostEvent(IScriptEvent ev)
        {
            Type evType = ev.GetType();
            if(evType == typeof(CollisionEvent))
            {
                PostCollisionEvent((CollisionEvent)ev);
            }
            else if(evType == typeof(LandCollisionEvent))
            {
                /* relay collision events to attachment root prims */
                foreach (ObjectGroup attached in Attachments.All)
                {
                    ObjectPart part;
                    if (attached.TryGetRootPart(out part))
                    {
                        part.PostEvent(ev);
                    }
                }
            }
        }

        protected abstract SceneInterface RootAgentScene { get; }

        private void PostCollisionEvent(CollisionEvent ev)
        {
            SceneInterface scene = RootAgentScene;
            if (scene != null)
            {
                foreach (DetectInfo di in ev.Detected)
                {
                    if (di.CausingDamage > 0)
                    {
                        ParcelInfo pInfo;
                        if (scene.Parcels.TryGetValue(GlobalPosition, out pInfo) && (pInfo.Flags & ParcelFlags.AllowDamage) != 0)
                        {
                            DecreaseHealth(di.CausingDamage);
                        }
                    }
                }
            }

            /* relay collision events to attachments */
            foreach(ObjectGroup attached in Attachments.All)
            {
                ObjectPart part;
                if(attached.TryGetRootPart(out part))
                {
                    part.PostEvent(ev);
                }
            }
        }
        #endregion

        private int m_NextParcelSequenceId;

        public int NextParcelSequenceId
        {
            get
            {
                lock (m_DataLock)
                {
                    int seqid = ++m_NextParcelSequenceId;
                    if (seqid < 0)
                    {
                        seqid = 1;
                        m_NextParcelSequenceId = seqid;
                    }
                    return seqid;
                }
            }
        }

        private UUID m_CurrentOutfitFolder = UUID.Zero;

        public event Action<IObject> OnPositionChange;

        public UUID CurrentOutfitFolder
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_CurrentOutfitFolder;
                }
            }

            set
            {
                lock (m_DataLock)
                {
                    m_CurrentOutfitFolder = value;
                }
            }
        }

        #region Health
        public double Health
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_Health;
                }
            }
            set
            {
                bool agentDies;
                double healthvalue = value.Clamp(0, 100);
                bool sendUpdate;
                lock (m_DataLock)
                {
                    sendUpdate = m_Health != healthvalue;
                    m_Health = healthvalue;
                    agentDies = m_Health < 0.0001 && sendUpdate;
                }

                if (sendUpdate)
                {
                    SendMessageAlways(new HealthMessage
                    {
                        Health = healthvalue
                    }, SceneID);
                }

                if (agentDies)
                {
                    DieAgent();
                }
            }
        }

        public double HealRate
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_HealRate;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_HealRate = value.Clamp(0, 100);
                }
            }
        }

        public void ProcessHealing(double dt)
        {
            double inchealth;
            lock(m_DataLock)
            {
                inchealth = m_HealRate * dt * 10.0;
            }
            IncreaseHealth(inchealth);
        }

        public abstract RwLockedDictionary<UUID, AgentChildInfo> ActiveChilds { get; }
        public abstract RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, uint>> TransmittedTerrainSerials { get; }
        public abstract RwLockedDictionary<UUID, FriendStatus> KnownFriends { get; }
        public abstract int LastMeasuredLatencyMsecs { get; }
        public abstract Vector3 CameraPosition { get; set; }
        public abstract Quaternion CameraRotation { get; set; }
        public abstract Vector3 CameraAtAxis { get; set; }
        public abstract Vector3 CameraLeftAxis { get; set; }
        public abstract Vector3 CameraUpAxis { get; set; }
        public abstract AssetServiceInterface AssetService { get; }
        public abstract InventoryServiceInterface InventoryService { get; }
        public abstract GroupsServiceInterface GroupsService { get; }
        public abstract ExperienceServiceInterface ExperienceService { get; }
        public abstract ProfileServiceInterface ProfileService { get; }
        public abstract FriendsServiceInterface FriendsService { get; }
        public abstract UserAgentServiceInterface UserAgentService { get; }
        public abstract IPresenceServiceInterface PresenceService { get; }
        public abstract EconomyServiceInterface EconomyService { get; }
        public abstract MuteListServiceInterface MuteListService { get; }
        public abstract OfflineIMServiceInterface OfflineIMService { get; }
        public abstract bool IsActiveGod { get; }
        public abstract bool IsNpc { get; }
        public abstract bool IsInMouselook { get; }
        public abstract RwLockedDictionary<UUID, IPhysicsObject> PhysicsActors { get; }
        public abstract IPhysicsObject PhysicsActor { get; }
        public abstract DetectedTypeFlags DetectedType { get; }

        public void IncreaseHealth(double v)
        {
            bool sendUpdate = false;
            double healthvalue = 100;
            lock (m_DataLock)
            {
                if (v >= 0)
                {
                    healthvalue = (m_Health + v).Clamp(0, 100);
                    sendUpdate = m_Health != healthvalue;
                    m_Health = healthvalue;
                }
            }

            if (sendUpdate)
            {
                SendMessageAlways(new HealthMessage
                {
                    Health = healthvalue
                }, SceneID);
            }
        }

        protected abstract void DieAgent();

        public void DecreaseHealth(double v)
        {
            bool agentDies = false;
            bool sendUpdate = false;
            double healthvalue = 0;
            lock (m_DataLock)
            {
                if (v >= 0)
                {
                    healthvalue = (m_Health - v).Clamp(0, 100);
                    sendUpdate = healthvalue != m_Health;
                    m_Health = healthvalue;
                    agentDies = m_Health < 0.0001 && sendUpdate;
                }
            }


            if (sendUpdate)
            {
                SendMessageAlways(new HealthMessage
                {
                    Health = healthvalue
                }, SceneID);
            }

            if (agentDies)
            {
                DieAgent();
            }
        }

        public bool UnSit()
        {
            IObject obj = SittingOnObject;
            if (obj == null)
            {
                return false;
            }
            var grp = (ObjectGroup)obj;
            return grp.AgentSitting.UnSit(this);
        }

        public bool UnSit(Vector3 targetOffset, Quaternion targetOrientation)
        {
            IObject obj = SittingOnObject;
            if (obj == null)
            {
                return false;
            }
            var grp = (ObjectGroup)obj;
            return grp.AgentSitting.UnSit(this, targetOffset, targetOrientation);
        }

        public abstract bool IMSend(GridInstantMessage im);
        public abstract void ClearKnownFriends();
        public abstract void EnableSimulator(UUID originSceneID, uint circuitCode, string capsURI, DestinationInfo destinationInfo);
        public abstract void SendUpdatedParcelInfo(ParcelInfo pinfo, UUID fromSceneID);
        public abstract void SendEstateUpdateInfo(UUID invoice, UUID transactionID, EstateInfo estate, UUID fromSceneID, bool sendToAgentOnly = true);
        public abstract void RemoveActiveTeleportService(IAgentTeleportServiceInterface service);
        public abstract void SendMessageIfRootAgent(Message m, UUID fromSceneID);
        public abstract void SendMessageAlways(Message m, UUID fromSceneID);
        public abstract void SendAlertMessage(string msg, UUID fromSceneID);
        public abstract void SendAlertMessage(string msg, string notification, IValue llsd, UUID fromSceneID);
        public abstract void SendRegionNotice(UGUI fromAvatar, string message, UUID fromSceneID);
        public abstract void HandleMessage(ChildAgentUpdate m);
        public abstract void HandleMessage(ChildAgentPositionUpdate m);
        public abstract RwLockedList<UUID> SelectedObjects(UUID scene);
        public abstract ulong AddNewFile(string filename, byte[] data);
        public abstract ScriptPermissions RequestPermissions(ObjectPart part, UUID itemID, ScriptPermissions permissions);
        public abstract ScriptPermissions RequestPermissions(ObjectPart part, UUID itemID, ScriptPermissions permissions, UUID experienceID);
        public abstract void RevokePermissions(UUID sourceID, UUID itemID, ScriptPermissions permissions);
        public abstract bool WaitsForExperienceResponse(ObjectPart part, UUID itemID);
        public abstract void TakeControls(ScriptInstance instance, int controls, int accept, int pass_on);
        public abstract void ReleaseControls(ScriptInstance instance);
        public abstract bool TeleportTo(SceneInterface sceneInterface, string regionName, Vector3 position, Vector3 lookAt, TeleportFlags flags);
        public abstract bool TeleportTo(SceneInterface sceneInterface, GridVector location, Vector3 position, Vector3 lookAt, TeleportFlags flags);
        public abstract bool TeleportTo(SceneInterface sceneInterface, string gatekeeperURI, GridVector location, Vector3 position, Vector3 lookAt, TeleportFlags flags);
        public abstract bool TeleportTo(SceneInterface sceneInterface, UUID regionID, Vector3 position, Vector3 lookAt, TeleportFlags flags);
        public abstract bool TeleportTo(SceneInterface sceneInterface, string gatekeeperURI, UUID regionID, Vector3 position, Vector3 lookAt, TeleportFlags flags);
        public abstract bool TeleportHome(SceneInterface sceneInterface);
        public abstract void KickUser(string msg);
        public abstract void KickUser(string msg, Action<bool> callbackDelegate);
        public abstract void ScheduleUpdate(AgentUpdateInfo info, UUID fromSceneID);
        public abstract void ScheduleUpdate(ObjectUpdateInfo info, UUID fromSceneID);
        public void ScheduleUpdate(ObjectInventoryUpdateInfo info, UUID fromSceneID)
        {
            /* intentionally left empty */
        }
        #endregion

        private Vector4 m_CollisionPlane = new Vector4(0, 0, 1, -1);

        public Vector4 CollisionPlane
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_CollisionPlane;
                }
            }
            set
            {
                /* nothing to do for now */
            }
        }

        public void PhysicsUpdate(PhysicsStateData value)
        {
            bool updateProcessed = false;
            lock (m_DataLock)
            {
                if (SceneID == value.SceneID && m_SittingOnObject == null)
                {
                    m_GlobalPosition = value.Position;
                    m_GlobalRotation = value.Rotation;
                    m_Velocity = value.Velocity;
                    m_AngularVelocity = value.AngularVelocity;
                    m_Acceleration = value.Acceleration;
                    m_AngularAcceleration = value.AngularAcceleration;
                    m_CollisionPlane = value.CollisionPlane;
                    updateProcessed = true;
                }
            }
            if (updateProcessed)
            {
                InvokeOnPositionUpdate();
            }
        }

        private Quaternion m_HeadRotation = Quaternion.Identity;
        private Quaternion m_BodyRotation = Quaternion.Identity;

        public Quaternion HeadRotation
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_HeadRotation;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_HeadRotation = value;
                }
            }
        }

        public Quaternion BodyRotation
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_BodyRotation;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_BodyRotation = value;
                }
            }
        }

        public AgentStateFlags StateFlags { get; set; }

        public virtual void SetAssetUploadAsCompletionAction(UUID transactionID, UUID sceneID, Action<UUID> action)
        {
            /* intentionally left empty */
        }

        public bool IsAvatarFreezed { get; set; }

        public abstract bool OwnsAssetID(UUID id);
    }
}
