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

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Transfer;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Agent
{
    public partial class Agent
    {
        protected readonly RwLockedDoubleDictionary<UUID /* ItemID */, UInt32 /* LocalID */, KeyValuePair<UUID /* SceneID */, UUID /* ObjectID */>> m_AttachmentsList = new RwLockedDoubleDictionary<UUID, UInt32, KeyValuePair<UUID, UUID>>();

        protected struct DetachEntry
        {
            public UUID ItemID;
            public UUID SceneID;
            public UUID ObjectID;

            public DetachEntry(UUID itemID, UUID sceneID, UUID objectID)
            {
                ItemID = itemID;
                SceneID = sceneID;
                ObjectID = objectID;
            }
        }


        #region Actual attachment handling
        public void DetachAllAttachments()
        {
            var detachList = new List<DetachEntry>();
            foreach (KeyValuePair<UUID, KeyValuePair<UUID, UUID>> kvp in m_AttachmentsList.Key1ValuePairs)
            {
                detachList.Add(new DetachEntry(kvp.Key, kvp.Value.Key, kvp.Value.Value));
            }
            foreach (var entry in detachList)
            {
                DetachAttachment(entry);
            }
        }

        protected abstract void DetachAttachment(DetachEntry entry);

        protected class RezAttachmentHandler : AssetTransferWorkItem
        {
            private readonly SceneInterface m_Scene;
            private readonly UUID m_ItemID;
            private readonly UUI m_RezzingAgent;
            private readonly AttachmentPoint m_AttachPoint;
            private readonly RwLockedDoubleDictionary<UUID /* ItemID */, UInt32 /* LocalID */, KeyValuePair<UUID /* SceneID */, UUID /* ObjectID */>> m_AttachmentsList = new RwLockedDoubleDictionary<UUID, UInt32, KeyValuePair<UUID, UUID>>();

            public RezAttachmentHandler(
                SceneInterface scene,
                UUID itemid,
                UUID assetid,
                AssetServiceInterface source,
                UUI rezzingagent,
                AttachmentPoint attachPoint,
                RwLockedDoubleDictionary<UUID /* ItemID */, UInt32 /* LocalID */, KeyValuePair<UUID /* SceneID */, UUID /* ObjectID */>> attachmentsList)
                : base(scene.AssetService, source, assetid, ReferenceSource.Destination)
            {
                m_Scene = scene;
                m_RezzingAgent = rezzingagent;
                m_ItemID = itemid;
                m_AttachPoint = attachPoint;
                m_AttachmentsList = attachmentsList;
            }

            private void SendAlertMessage(string msg)
            {
                IAgent agent;
                if (m_Scene.Agents.TryGetValue(m_RezzingAgent.ID, out agent))
                {
                    agent.SendAlertMessage(msg, m_Scene.ID);
                }
            }

            public override void AssetTransferComplete()
            {
                AssetData data;
                List<ObjectGroup> objgroups;
                try
                {
                    data = m_Scene.AssetService[AssetID];
                }
                catch
                {
                    SendAlertMessage("ALERT: CantFindObject");
                    return;
                }

#if DEBUG
                m_Log.DebugFormat("Deserializing object asset {0} for agent {1} {2} ({3})", data.ID, m_RezzingAgent.FirstName, m_RezzingAgent.LastName, m_RezzingAgent.ID);
#endif
                try
                {
                    objgroups = ObjectXML.FromAsset(data, m_RezzingAgent);
                }
                catch (Exception e)
                {
                    m_Log.WarnFormat("Deserialization error for object asset {0} for agent {1} {2} ({3}): {4}: {5}",
                        data.ID, m_RezzingAgent.FirstName, m_RezzingAgent.LastName, m_RezzingAgent.ID, e.GetType().FullName, e.ToString());
                    SendAlertMessage("ALERT: InvalidObjectParams");
                    return;
                }

                if (objgroups.Count != 1)
                {
                    SendAlertMessage("ALERT: InvalidObjectParams");
                    return;
                }

                ObjectGroup grp = objgroups[0];

                foreach (var part in grp.Values)
                {
                    if (part.Shape.PCode == PrimitiveCode.Grass ||
                        part.Shape.PCode == PrimitiveCode.Tree ||
                        part.Shape.PCode == PrimitiveCode.NewTree)
                    {
                        SendAlertMessage("ALERT: WhyAreYouTryingToWearShrubbery");
                        return;
                    }
                }

                var attachAt = m_AttachPoint & AttachmentPoint.PositionMask;
                if (attachAt != AttachmentPoint.Default && attachAt != grp.AttachPoint)
                {
                    grp.AttachedPos = Vector3.Zero;
                }

                if (attachAt == AttachmentPoint.Default)
                {
                    attachAt = grp.AttachPoint;

                    if (attachAt == AttachmentPoint.NotAttached)
                    {
                        grp.AttachPoint = AttachmentPoint.LeftHand;
                        grp.AttachedPos = Vector3.Zero;
                    }
                }

                grp.Owner = m_RezzingAgent;
                grp.FromItemID = m_ItemID;
                grp.IsAttached = true;
                grp.Position = grp.AttachedPos;
                grp.IsChangedEnabled = true;

#if DEBUG
                m_Log.DebugFormat("Adding attachment asset {0} at {4} for agent {1} {2} ({3})", data.ID, m_RezzingAgent.FirstName, m_RezzingAgent.LastName, m_RezzingAgent.ID, grp.AttachPoint.ToString());
#endif
                try
                {
                    m_Scene.Add(grp);
                    m_AttachmentsList.Add(m_ItemID, grp.LocalID, new KeyValuePair<UUID, UUID>(m_Scene.ID, grp.ID));
                }
                catch
                {
                    SendAlertMessage("ALERT: RezAttemptFailed");
                    return;
                }
            }

            public override void AssetTransferFailed(Exception e)
            {
                IAgent agent;
                if (m_Scene.Agents.TryGetValue(m_RezzingAgent.ID, out agent))
                {
                    agent.SendAlertMessage("ALERT: CantFindObject", m_Scene.ID);
                }
            }
        }
        #endregion
    }
}
