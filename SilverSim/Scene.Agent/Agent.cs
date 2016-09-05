﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using SilverSim.Scene.Types.Neighbor;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Script;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Threading;
using SilverSim.Types.Estate;
using SilverSim.Types.Grid;
using SilverSim.Types.IM;
using SilverSim.Types.Parcel;
using SilverSim.Types.Script;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Agent;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Agent
{
    public abstract partial class Agent : IAgent
    {
        protected readonly object m_DataLock = new object();

        #region Agent fields
        readonly UUID m_AgentID;
        double m_Health = 100f;
        #endregion

        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        #region Properties
        public Uri HomeURI { get; private set; }
        public UInt32 LocalID { get; set; }
        #endregion

        protected Agent(UUID agentId, Uri homeURI)
        {
            m_AgentID = agentId;
            HomeURI = homeURI;
            m_AnimationController = new AgentAnimationController(ID, SendAnimations);
        }

        ~Agent()
        {
            lock (m_DataLock)
            {
                m_SittingOnObject = null;
            }
        }

        public abstract ClientInfo Client { get; }
        public abstract UserAccount UntrustedAccountInfo { get; }
        public abstract SessionInfo Session { get; }
        public abstract List<GridType> SupportedGridTypes { get; }
        public abstract IAgentTeleportServiceInterface ActiveTeleportService { get; set; }

        public void GetBoundingBox(out BoundingBox box)
        {
            box = new BoundingBox();
            box.CenterOffset = Vector3.Zero;
            box.Size = Size * Rotation;
        }

        public abstract void InvokeOnPositionUpdate();

        #region IObject Properties

        private IObject m_SittingOnObject;

        public IObject SittingOnObject
        {
            /* we need to guard against our position routines and so on */
            get
            {
                lock (m_DataLock)
                {
                    return m_SittingOnObject;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_SittingOnObject = value;
                }
            }
        }

        public UUID ID
        {
            get
            {
                return m_AgentID;
            }
        }

        public string Name
        {
            get
            {
                return string.Format("{0} {1}", FirstName, LastName);
            }
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
            get
            {
                Vector3 angle = new Vector3(1, 0, 0);
                return angle * Rotation;
            }
            set
            {
                Vector3 delta = value.Normalize();
                Rotation = Quaternion.CreateFromEulers(new Vector3(0, 0, Math.Atan2(delta.Y, delta.X)));
            }
        }

        public UUI Owner
        {
            get
            {
                UUI n = new UUI();
                n.FirstName = FirstName;
                n.LastName = LastName;
                n.ID = ID;
                n.HomeURI = HomeURI;
                return n;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public string Description
        {
            get
            {
                return string.Empty;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        private double m_HoverHeight;
        public double HoverHeight
        {
            get
            {
                return m_HoverHeight;
            }
            set
            {
                m_HoverHeight = value.Clamp(-2f, 2f);
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
            }
        }

        public abstract bool IsInScene(SceneInterface scene);

        public abstract UUID SceneID { get; set; }

        public abstract void AddWaitForRoot(SceneInterface scene, Action<object, bool> del, object o);
        #endregion

        #region IObject Methods
        public void GetPrimitiveParams(AnArray.Enumerator enumerator, AnArray paramList)
        {
            switch (ParamsHelper.GetPrimParamType(enumerator))
            {
                case PrimitiveParamsType.Name:
                    paramList.Add(Name);
                    break;

                case PrimitiveParamsType.Desc:
                    paramList.Add(Description);
                    break;

                case PrimitiveParamsType.Type:
                    throw new ArgumentException("PRIM_TYPE not allowed for agents");

                case PrimitiveParamsType.Slice:
                    throw new ArgumentException("PRIM_SLICE not allowed for agents");

                case PrimitiveParamsType.PhysicsShapeType:
                    throw new ArgumentException("PRIM_PHYSICSSHAPETYPE not allowed for agents");

                case PrimitiveParamsType.Material:
                    throw new ArgumentException("PRIM_MATERIAL not allowed for agents");

                case PrimitiveParamsType.Position:
                    paramList.Add(Position);
                    break;

                case PrimitiveParamsType.PosLocal:
                    paramList.Add(LocalPosition);
                    break;

                case PrimitiveParamsType.Rotation:
                    paramList.Add(Rotation);
                    break;

                case PrimitiveParamsType.RotLocal:
                    paramList.Add(LocalRotation);
                    break;

                case PrimitiveParamsType.Size:
                    paramList.Add(Size);
                    break;

                case PrimitiveParamsType.Texture:
                    throw new ArgumentException("PRIM_TEXTURE not allowed for agents");

                case PrimitiveParamsType.Text:
                    throw new ArgumentException("PRIM_TEXT not allowed for agents");

                case PrimitiveParamsType.Color:
                    throw new ArgumentException("PRIM_COLOR not allowed for agents");

                case PrimitiveParamsType.BumpShiny:
                    throw new ArgumentException("PRIM_BUMPSHINY not allowed for agents");

                case PrimitiveParamsType.PointLight:
                    throw new ArgumentException("PRIM_POINTLIGHT not allowed for agents");

                case PrimitiveParamsType.FullBright:
                    throw new ArgumentException("PRIM_FULLBRIGHT not allowed for agents");

                case PrimitiveParamsType.Flexible:
                    throw new ArgumentException("PRIM_FLEXIBLE not allowed for agents");

                case PrimitiveParamsType.TexGen:
                    throw new ArgumentException("PRIM_TEXGEN not allowed for agents");

                case PrimitiveParamsType.Glow:
                    throw new ArgumentException("PRIM_GLOW not allowed for agents");

                case PrimitiveParamsType.Omega:
                    throw new ArgumentException("PRIM_OMEGA not allowed for agents");

                case PrimitiveParamsType.Specular:
                    throw new ArgumentException("PRIM_SPECULAR not allowed for agents");

                case PrimitiveParamsType.Normal:
                    throw new ArgumentException("PRIM_NORMAL not allowed for agents");

                case PrimitiveParamsType.AlphaMode:
                    throw new ArgumentException("PRIM_ALPHA_MODE not allowed for agents");

                case PrimitiveParamsType.Alpha:
                    throw new ArgumentException("PRIM_ALPHA not allowed for agents");

                case PrimitiveParamsType.Projector:
                    throw new ArgumentException("PRIM_PROJECTOR not allowed for agents");

                case PrimitiveParamsType.ProjectorEnabled:
                    throw new ArgumentException("PRIM_PROJECTOR_ENABLED not allowed for agents");

                case PrimitiveParamsType.ProjectorTexture:
                    throw new ArgumentException("PRIM_PROJECTOR_TEXTURE not allowed for agents");

                case PrimitiveParamsType.ProjectorFov:
                    throw new ArgumentException("PRIM_PROJECTOR_FOV not allowed for agents");

                case PrimitiveParamsType.ProjectorFocus:
                    throw new ArgumentException("PRIM_PROJECTOR_FOCUS not allowed for agents");

                case PrimitiveParamsType.ProjectorAmbience:
                    throw new ArgumentException("PRIM_PROJECTOR_AMBIENCE not allowed for agents");

                default:
                    throw new ArgumentException(String.Format("Invalid primitive parameter type {0}", enumerator.Current.AsUInt));
            }
        }

        public void SetPrimitiveParams(AnArray.MarkEnumerator enumerator)
        {
            switch (ParamsHelper.GetPrimParamType(enumerator))
            {
                case PrimitiveParamsType.Name:
                    Name = ParamsHelper.GetString(enumerator, "PRIM_NAME");
                    break;

                case PrimitiveParamsType.Desc:
                    Description = ParamsHelper.GetString(enumerator, "PRIM_DESC");
                    break;

                case PrimitiveParamsType.Type:
                    throw new ArgumentException("PRIM_TYPE not allowed for agents");

                case PrimitiveParamsType.Slice:
                    throw new ArgumentException("PRIM_SLICE not allowed for agents");

                case PrimitiveParamsType.PhysicsShapeType:
                    throw new ArgumentException("PRIM_PHYSICSSHAPETYPE not allowed for agents");

                case PrimitiveParamsType.Material:
                    throw new ArgumentException("PRIM_MATERIAL not allowed for agents");

                case PrimitiveParamsType.Position:
                    Position = ParamsHelper.GetVector(enumerator, "PRIM_POSITION");
                    break;

                case PrimitiveParamsType.PosLocal:
                    LocalPosition = ParamsHelper.GetVector(enumerator, "PRIM_POS_LOCAL");
                    break;

                case PrimitiveParamsType.Rotation:
                    Rotation = ParamsHelper.GetRotation(enumerator, "PRIM_ROTATION");
                    break;

                case PrimitiveParamsType.RotLocal:
                    LocalRotation = ParamsHelper.GetRotation(enumerator, "PRIM_ROT_LOCAL");
                    break;

                case PrimitiveParamsType.Size:
                    throw new ArgumentException("PRIM_SIZE not allowed for agents");

                case PrimitiveParamsType.Texture:
                    throw new ArgumentException("PRIM_TEXTURE not allowed for agents");

                case PrimitiveParamsType.Text:
                    throw new ArgumentException("PRIM_TEXT not allowed for agents");

                case PrimitiveParamsType.Color:
                    throw new ArgumentException("PRIM_COLOR not allowed for agents");

                case PrimitiveParamsType.BumpShiny:
                    throw new ArgumentException("PRIM_BUMPSHINY not allowed for agents");

                case PrimitiveParamsType.PointLight:
                    throw new ArgumentException("PRIM_POINTLIGHT not allowed for agents");

                case PrimitiveParamsType.FullBright:
                    throw new ArgumentException("PRIM_FULLBRIGHT not allowed for agents");

                case PrimitiveParamsType.Flexible:
                    throw new ArgumentException("PRIM_FLEXIBLE not allowed for agents");

                case PrimitiveParamsType.TexGen:
                    throw new ArgumentException("PRIM_TEXGEN not allowed for agents");

                case PrimitiveParamsType.Glow:
                    throw new ArgumentException("PRIM_GLOW not allowed for agents");

                case PrimitiveParamsType.Omega:
                    throw new ArgumentException("PRIM_OMEGA not allowed for agents");

                case PrimitiveParamsType.Specular:
                    throw new ArgumentException("PRIM_SPECULAR not allowed for agents");

                case PrimitiveParamsType.Normal:
                    throw new ArgumentException("PRIM_NORMAL not allowed for agents");

                case PrimitiveParamsType.AlphaMode:
                    throw new ArgumentException("PRIM_ALPHA_MODE not allowed for agents");

                case PrimitiveParamsType.Alpha:
                    throw new ArgumentException("PRIM_ALPHA not allowed for agents");

                case PrimitiveParamsType.Projector:
                    throw new ArgumentException("PRIM_PROJECTOR not allowed for agents");

                case PrimitiveParamsType.ProjectorEnabled:
                    throw new ArgumentException("PRIM_PROJECTOR_ENABLED not allowed for agents");

                case PrimitiveParamsType.ProjectorTexture:
                    throw new ArgumentException("PRIM_PROJECTOR_TEXTURE not allowed for agents");

                case PrimitiveParamsType.ProjectorFov:
                    throw new ArgumentException("PRIM_PROJECTOR_FOV not allowed for agents");

                case PrimitiveParamsType.ProjectorFocus:
                    throw new ArgumentException("PRIM_PROJECTOR_FOCUS not allowed for agents");

                case PrimitiveParamsType.ProjectorAmbience:
                    throw new ArgumentException("PRIM_PROJECTOR_AMBIENCE not allowed for agents");

                default:
                    throw new ArgumentException(String.Format("Invalid primitive parameter type {0}", enumerator.Current.AsInt));
            }
        }

        public void GetObjectDetails(AnArray.Enumerator enumerator, AnArray paramList)
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
                    case ObjectDetailsType.Root:
                        paramList.Add(ID);
                        break;

                    case ObjectDetailsType.Group:
                        paramList.Add(Group.ID);
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

                    case ObjectDetailsType.ScriptTime:
                    case ObjectDetailsType.ServerCost:
                    case ObjectDetailsType.StreamingCost:
                    case ObjectDetailsType.PhysicsCost:
                        paramList.Add(0f);
                        break;

                    case ObjectDetailsType.ScriptMemory:
                    case ObjectDetailsType.CharacterTime:
                    case ObjectDetailsType.AttachedPoint:
                    case ObjectDetailsType.PathfindingType:
                        paramList.Add(0);
                        break;

                    case ObjectDetailsType.Physics:
                        paramList.Add(true);
                        break;

                    case ObjectDetailsType.Phantom:
                    case ObjectDetailsType.TempOnRez:
                        paramList.Add(false);
                        break;

                    case ObjectDetailsType.HoverHeight:
                        paramList.Add(HoverHeight);
                        break;

                    case ObjectDetailsType.BodyShapeType:
                        byte[] vp = VisualParams;
                        if (vp.Length > 31)
                        {
                            paramList.Add(vp[31] / 255f);
                        }
                        else
                        {
                            paramList.Add(-1f);
                        }
                        break;

                    case ObjectDetailsType.ClickAction:
                        paramList.Add((int)ClickActionType.None);
                        break;

                    case ObjectDetailsType.Omega:
                        paramList.Add(AngularVelocity);
                        break;

                    case ObjectDetailsType.RenderWeight:
                    default:
                        paramList.Add(-1);
                        break;
                }
            }
        }

        public void PostEvent(IScriptEvent ev)
        {
            /* intentionally left empty */
        }
        #endregion

        int m_NextParcelSequenceId;

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

        UUID m_CurrentOutfitFolder = UUID.Zero;

        public abstract event Action<IObject> OnPositionChange;

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
                lock (m_DataLock)
                {
                    m_Health = value.Clamp(0, 100);
#warning Implement death
                }
            }
        }

        public abstract RwLockedDictionary<UUID, AgentChildInfo> ActiveChilds { get; }
        public abstract RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, uint>> TransmittedTerrainSerials { get; }
        public abstract RwLockedDictionary<UUID, FriendStatus> KnownFriends { get; }
        public abstract int LastMeasuredLatencyTickCount { get; set; }
        public abstract Vector3 CameraPosition { get; set; }
        public abstract Quaternion CameraRotation { get; set; }
        public abstract Vector3 CameraAtAxis { get; set; }
        public abstract Vector3 CameraLeftAxis { get; set; }
        public abstract Vector3 CameraUpAxis { get; set; }
        public abstract AssetServiceInterface AssetService { get; }
        public abstract InventoryServiceInterface InventoryService { get; }
        public abstract GroupsServiceInterface GroupsService { get; }
        public abstract ProfileServiceInterface ProfileService { get; }
        public abstract FriendsServiceInterface FriendsService { get; }
        public abstract UserAgentServiceInterface UserAgentService { get; }
        public abstract PresenceServiceInterface PresenceService { get; }
        public abstract GridUserServiceInterface GridUserService { get; }
        public abstract EconomyServiceInterface EconomyService { get; }
        public abstract OfflineIMServiceInterface OfflineIMService { get; }
        public abstract bool IsActiveGod { get; }
        public abstract bool IsNpc { get; }
        public abstract bool IsInMouselook { get; }
        public abstract RwLockedDictionary<UUID, IPhysicsObject> PhysicsActors { get; }
        public abstract IPhysicsObject PhysicsActor { get; }
        public abstract DetectedTypeFlags DetectedType { get; }

        public void IncreaseHealth(double v)
        {
            lock (m_DataLock)
            {
                if (v >= 0)
                {
                    m_Health = (m_Health + v).Clamp(0, 100);
                }
            }
        }

        public void DecreaseHealth(double v)
        {
            lock (m_DataLock)
            {
                if (v <= 0)
                {
                    m_Health = (m_Health - v).Clamp(0, 100);
#warning Implement death
                }
            }
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
        public abstract void SendRegionNotice(UUI fromAvatar, string message, UUID fromSceneID);
        public abstract void HandleMessage(ChildAgentUpdate m);
        public abstract void HandleMessage(ChildAgentPositionUpdate m);
        public abstract bool UnSit();
        public abstract RwLockedList<UUID> SelectedObjects(UUID scene);
        public abstract ulong AddNewFile(string filename, byte[] data);
        public abstract ScriptPermissions RequestPermissions(ObjectPart part, UUID itemID, ScriptPermissions permissions);
        public abstract ScriptPermissions RequestPermissions(ObjectPart part, UUID itemID, ScriptPermissions permissions, UUID experienceID);
        public abstract void RevokePermissions(UUID sourceID, UUID itemID, ScriptPermissions permissions);
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
        public abstract void ScheduleUpdate(ObjectUpdateInfo info, UUID fromSceneID);
        #endregion

        Vector4 m_CollisionPlane = new Vector4(0, 0, 1, -1);

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
                if (SceneID == value.SceneID && null == m_SittingOnObject)
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

        Quaternion m_HeadRotation = Quaternion.Identity;
        Quaternion m_BodyRotation = Quaternion.Identity;

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
    }
}