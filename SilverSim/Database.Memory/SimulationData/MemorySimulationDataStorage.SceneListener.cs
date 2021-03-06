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

using SilverSim.Scene.Types.Object;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using System.IO;

namespace SilverSim.Database.Memory.SimulationData
{
    public partial class MemorySimulationDataStorage
    {
        public class MemorySceneListener : SceneListener
        {
            internal readonly RwLockedDictionary<UUID, Map> m_Objects;
            internal readonly RwLockedDictionary<UUID, Map> m_Primitives;
            internal readonly RwLockedDictionary<string, Map> m_PrimItems;

            public MemorySceneListener(
                RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, Map>> objects,
                RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, Map>> primitives,
                RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, Map>> primitems,
                UUID regionID)
                : base(regionID)
            {
                m_Objects = objects[regionID];
                m_Primitives = primitives[regionID];
                m_PrimItems = primitems[regionID];
            }

            protected override void OnUpdate(ObjectInventoryUpdateInfo info)
            {
                string key = GenItemKey(info.PartID, info.ItemID);

                if(info.IsRemoved)
                {
                    m_PrimItems.Remove(key);
                }
                else
                {
                    m_PrimItems[key] = GenerateUpdateObjectPartInventoryItem(info.PartID, info.Item);
                }
            }

            protected override void OnUpdate(ObjectUpdateInfo info)
            {
                if (info.IsKilled)
                {
                    m_Primitives.Remove(info.ID);
                    m_Objects.Remove(info.ID);
                }
                else
                {
                    m_Objects.Remove(info.ID);
                    m_Primitives[info.ID] = GenerateUpdateObjectPart(info.Part);
                    m_Objects[info.ID] = GenerateUpdateObjectGroup(info.Part.ObjectGroup);
                    foreach(ObjectPartInventoryItem item in info.Part.Inventory.Values)
                    {
                        OnUpdate(item.UpdateInfo);
                    }
                }
            }

            protected override void OnIdle()
            {
                /* intentionally left empty */
            }

            private Map GenerateUpdateObjectPartInventoryItem(UUID primID, ObjectPartInventoryItem item)
            {
                var grantinfo = item.PermsGranter;
                return new Map
                {
                    { "AssetID", item.AssetID },
                    { "AssetType", (int)item.AssetType },
                    { "CreationDate", item.CreationDate },
                    { "Creator", item.Creator.ToString() },
                    { "Description", item.Description },
                    { "Flags", (int)item.Flags },
                    { "Group", item.Group.ToString() },
                    { "GroupOwned", item.IsGroupOwned },
                    { "PrimID", primID },
                    { "Name", item.Name },
                    { "InventoryID", item.ID },
                    { "InventoryType", (int)item.InventoryType },
                    { "LastOwner", item.LastOwner.ToString() },
                    { "Owner", item.Owner.ToString() },
                    { "ParentFolderID", item.ParentFolderID },
                    { "BasePermissions", (int)item.Permissions.Base },
                    { "CurrentPermissions", (int)item.Permissions.Current },
                    { "EveryOnePermissions", (int)item.Permissions.EveryOne },
                    { "GroupPermissions", (int)item.Permissions.Group },
                    { "NextOwnerPermissions", (int)item.Permissions.NextOwner },
                    { "SaleType", (int)item.SaleInfo.Type },
                    { "SalePrice", item.SaleInfo.Price },
                    { "SalePermMask", (int)item.SaleInfo.PermMask },
                    { "PermsGranter", grantinfo.PermsGranter.ToString() },
                    { "PermsMask", (int)grantinfo.PermsMask },
                    { "DebitPermissionKey", item.PermsGranter.DebitPermissionKey },
                    { "NextOwnerAssetID", item.NextOwnerAssetID },
                    { "ExperienceID", item.ExperienceID.ToString() },
                    { "CollisionFilterData", new BinaryData(item.CollisionFilter.DbSerialization) }
                };
            }

            private Map GenerateUpdateObjectGroup(ObjectGroup objgroup) => new Map
            {
                { "ID", objgroup.ID },
                { "IsTemporary", objgroup.IsTemporary },
                { "Owner", objgroup.Owner.ToString() },
                { "LastOwner", objgroup.LastOwner.ToString() },
                { "Group", objgroup.Group.ToString() },
                { "OriginalAssetID", objgroup.OriginalAssetID },
                { "NextOwnerAssetID", objgroup.NextOwnerAssetID },
                { "SaleType", (int)objgroup.SaleType },
                { "SalePrice", objgroup.SalePrice },
                { "PayPrice0", objgroup.PayPrice0 },
                { "PayPrice1", objgroup.PayPrice1 },
                { "PayPrice2", objgroup.PayPrice2 },
                { "PayPrice3", objgroup.PayPrice3 },
                { "PayPrice4", objgroup.PayPrice4 },
                { "AttachedPos", objgroup.AttachedPos },
                { "AttachedRot", objgroup.AttachedRot },
                { "AttachPoint", (int)objgroup.AttachPoint },
                { "IsIncludedInSearch", objgroup.IsIncludedInSearch },
                { "RezzingObjectID", objgroup.RezzingObjectID }
            };

            private Map GenerateUpdateObjectPart(ObjectPart objpart)
            {
                var data = new Map
                {
                    { "ID", objpart.ID },
                    { "LinkNumber", objpart.LinkNumber },
                    { "RootPartID", objpart.ObjectGroup.RootPart.ID },
                    { "Position", objpart.Position },
                    { "Rotation", objpart.Rotation },
                    { "SitText", objpart.SitText },
                    { "TouchText", objpart.TouchText },
                    { "Name", objpart.Name },
                    { "Description", objpart.Description },
                    { "SitTargetOffset", objpart.SitTargetOffset },
                    { "SitTargetOrientation", objpart.SitTargetOrientation },
                    { "SitAnimation", objpart.SitAnimation },
                    { "PhysicsShapeType", (int)objpart.PhysicsShapeType },
                    { "PathfindingType", (int)objpart.PathfindingType },
                    { "PathfindingCharacterType", (int)objpart.PathfindingCharacterType },
                    { "WalkableCoefficientAvatar", objpart.WalkableCoefficientAvatar },
                    { "WalkableCoefficientA", objpart.WalkableCoefficientA },
                    { "WalkableCoefficientB", objpart.WalkableCoefficientB },
                    { "WalkableCoefficientC", objpart.WalkableCoefficientC },
                    { "WalkableCoefficientD", objpart.WalkableCoefficientD },
                    { "Material", (int)objpart.Material },
                    { "Size", objpart.Size },
                    { "MediaURL", objpart.MediaURL },
                    { "Creator", objpart.Creator.ToString() },
                    { "CreationDate", objpart.CreationDate },
                    { "RezDate", objpart.RezDate },
                    { "Flags", (int)objpart.Flags },
                    { "AngularVelocity", objpart.AngularVelocity },
                    { "LightData", new BinaryData(objpart.PointLight.DbSerialization) },
                    { "ProjectionData", new BinaryData(objpart.Projection.DbSerialization) },
                    { "HoverTextData", new BinaryData(objpart.Text.Serialization) },
                    { "FlexibleData", new BinaryData(objpart.Flexible.DbSerialization) },
                    { "LoopedSoundData", new BinaryData(objpart.Sound.Serialization) },
                    { "ImpactSoundData", new BinaryData(objpart.CollisionSound.Serialization) },
                    { "ExtendedMeshData", new BinaryData(objpart.ExtendedMesh.DbSerialization) },
                    { "PrimitiveShapeData", new BinaryData(objpart.Shape.Serialization) },
                    { "ParticleSystem", new BinaryData(objpart.ParticleSystemBytes) },
                    { "TextureEntryBytes", new BinaryData(objpart.TextureEntryBytes) },
                    { "TextureAnimationBytes", new BinaryData(objpart.TextureAnimationBytes) },
                    { "ScriptAccessPin", objpart.ScriptAccessPin },
                    { "CameraAtOffset", objpart.CameraAtOffset },
                    { "CameraEyeOffset", objpart.CameraEyeOffset },
                    { "ForceMouselook", objpart.ForceMouselook },
                    { "BasePermissions", (int)objpart.BaseMask },
                    { "CurrentPermissions", (int)objpart.OwnerMask },
                    { "EveryOnePermissions", (int)objpart.EveryoneMask },
                    { "GroupPermissions", (int)objpart.GroupMask },
                    { "NextOwnerPermissions", (int)objpart.NextOwnerMask },
                    { "ClickAction", (int)objpart.ClickAction },
                    { "LocalizationData", new BinaryData(objpart.LocalizationSerialization) },
                    { "VehicleData", new BinaryData(objpart.VehicleParams.ToSerialization()) },
                    { "Damage", objpart.Damage },
                    { "PassCollisionMode", (int)objpart.PassCollisionMode },
                    { "PassTouchMode", (int)objpart.PassTouchMode },
                    { "Velocity", objpart.Velocity },
                    { "IsSoundQueueing", objpart.IsSoundQueueing },
                    { "IsAllowedDrop", objpart.IsAllowedDrop },
                    { "PhysicsDensity", objpart.PhysicsDensity },
                    { "PhysicsFriction", objpart.PhysicsFriction },
                    { "PhysicsRestitution", objpart.PhysicsRestitution },
                    { "PhysicsGravityMultiplier", objpart.PhysicsGravityMultiplier },
                    { "IsRotateXEnabled", objpart.IsRotateXEnabled },
                    { "IsRotateYEnabled", objpart.IsRotateYEnabled },
                    { "IsRotateZEnabled", objpart.IsRotateZEnabled },
                    { "IsVolumeDetect", objpart.IsVolumeDetect },
                    { "IsPhantom", objpart.IsPhantom },
                    { "IsPhysics", objpart.IsPhysics },
                    { "IsSandbox", objpart.IsSandbox },
                    { "IsBlockGrab", objpart.IsBlockGrab },
                    { "IsDieAtEdge", objpart.IsDieAtEdge },
                    { "IsReturnAtEdge", objpart.IsReturnAtEdge },
                    { "IsBlockGrabObject", objpart.IsBlockGrabObject },
                    { "SandboxOrigin", objpart.SandboxOrigin },
                    { "IsSitTargetActive", objpart.IsSitTargetActive },
                    { "IsScriptedSitOnly", objpart.IsScriptedSitOnly },
                    { "AllowUnsit", objpart.AllowUnsit },
                    { "IsUnSitTargetActive", objpart.IsUnSitTargetActive },
                    { "UnSitTargetOffset", objpart.UnSitTargetOffset },
                    { "UnSitTargetOrientation", objpart.UnSitTargetOrientation },
                    { "AnimationData", new BinaryData(objpart.AnimationController.DbSerialization) }
                };
                using (var ms = new MemoryStream())
                {
                    LlsdBinary.Serialize(objpart.DynAttrs, ms);
                    data.Add("DynAttrs", new BinaryData(ms.ToArray()));
                }

                return data;
            }
        }

        public override SceneListener GetSceneListener(UUID regionID) =>
            new MemorySceneListener(m_Objects, m_Primitives, m_PrimItems, regionID);
    }
}
