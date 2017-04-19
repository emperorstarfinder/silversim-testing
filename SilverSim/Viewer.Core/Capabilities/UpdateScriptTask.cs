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
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.Core.Capabilities
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
    public class UpdateScriptTask : UploadAssetAbstractCapability
    {
        private static readonly ILog m_Log = LogManager.GetLogger("UPDATE SCRIPT TASK");

        sealed class TransactionInfo
        {
            public UUID TaskID;
            public UUID ItemID;
            public bool IsScriptRunning;
            public UUID ExperienceID;

            public TransactionInfo(UUID taskID, UUID itemID, bool isScriptRunning, UUID experienceID)
            {
                TaskID = taskID;
                ItemID = itemID;
                IsScriptRunning = isScriptRunning;
                ExperienceID = experienceID;
            }
        }

        readonly ViewerAgent m_Agent;
        readonly SceneInterface m_Scene;
        readonly RwLockedDictionary<UUID, TransactionInfo> m_Transactions = new RwLockedDictionary<UUID, TransactionInfo>();

        public override string CapabilityName
        {
            get
            {
                return "UpdateScriptTask";
            }
        }

        public override int ActiveUploads
        {
            get
            {
                return m_Transactions.Count;
            }
        }

        public UpdateScriptTask(ViewerAgent agent, SceneInterface scene, string serverURI, string remoteip)
            : base(agent.Owner, serverURI, remoteip)
        {
            m_Agent = agent;
            m_Scene = scene;
        }

        public override UUID GetUploaderID(Map reqmap)
        {
            UUID transaction = UUID.Random;
            UUID experienceID = UUID.Zero;
            if(reqmap.ContainsKey("experience"))
            {
                experienceID = reqmap["experience"].AsUUID;
            }
            m_Transactions.Add(transaction, new TransactionInfo(reqmap["task_id"].AsUUID, reqmap["item_id"].AsUUID, reqmap["is_script_running"].AsBoolean, experienceID));
            return transaction;
        }

        public override Map UploadedData(UUID transactionID, AssetData data)
        {
            KeyValuePair<UUID, TransactionInfo> kvp;
            if (m_Transactions.RemoveIf(transactionID, delegate(TransactionInfo v) { return true; }, out kvp))
            {
                Map m = new Map();
                ObjectPartInventoryItem item;
                ObjectPart part = m_Scene.Primitives[kvp.Value.TaskID];
                try
                {
                    item = part.Inventory[kvp.Value.ItemID];
                }
                catch
                {
                    m_Log.WarnFormat("Inventory not found for {0}", transactionID.ToString());
                    throw new UrlNotFoundException();
                }

                if (item.AssetType != data.Type)
                {
                    m_Log.WarnFormat("Wrong inventory type for {0}", transactionID.ToString());
                    throw new UrlNotFoundException();
                }

                if(!part.CheckPermissions(m_Agent.Owner, m_Agent.Group, InventoryPermissionsMask.Modify))
                {
                    throw new UploadErrorException(this.GetLanguageString(m_Agent.CurrentCulture, "NotAllowedToModifyScript", "Not allowed to modify script"));
                }

                UUID oldAssetID = item.AssetID;
                item.AssetID = data.ID;
                data.Creator = item.Creator;
                data.Name = item.Name;
                item.ID = kvp.Value.ItemID;

                try
                {
                    m_Scene.AssetService.Store(data);
                }
                catch
                {
                    throw new UploadErrorException(this.GetLanguageString(m_Agent.CurrentCulture, "FailedToStoreAsset", "Failed to store asset"));
                }

                ScriptInstance instance;
                try
                {
                    part.Inventory.Remove(kvp.Value.ItemID);
                    part.Inventory.Add(item.ID, item.Name, item);
                }
                catch
#if DEBUG
                    (Exception e)
#endif
                {
#if DEBUG
                    m_Log.DebugFormat("Failed to store inventory item: {0}\n{1}", e.Message, e.StackTrace);
#endif
                    throw new UploadErrorException(this.GetLanguageString(m_Agent.CurrentCulture, "FailedToStoreInventoryItem", "Failed to store inventory item"));
                }

                part.SendObjectUpdate();

                try
                {
                    instance = ScriptLoader.Load(part, item, item.Owner, data, m_Agent.CurrentCulture);
                    item.ScriptInstance = instance;
                    item.ScriptInstance.IsRunning = kvp.Value.IsScriptRunning;
                    item.ScriptInstance.Reset();
                    m.Add("compiled", true);
                }
                catch (CompilerException e)
                {
                    AnArray errors = new AnArray();
                    foreach (KeyValuePair<int, string> line in e.Messages)
                    {
                        int lineNumber = line.Key - 1;
                        /* Viewer editors count lines from 0 */
                        if (lineNumber < 0)
                        {
                            lineNumber = 0;
                        }
                        errors.Add(string.Format("{0}:{1}", lineNumber, line.Value));
                    }
                    m.Add("errors", errors);
                    m.Add("compiled", false);
                }
                catch (Exception e)
                {
                    m_Log.ErrorFormat("Unexpected exception: {0}: {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace);
                    AnArray errors = new AnArray();
                    errors.Add("0:Unexpected compiler error " + e.GetType().Name);
                    m.Add("errors", errors);
                    m.Add("compiled", false);
                }

                return m;
            }
            else
            {
                throw new UrlNotFoundException();
            }
        }

        protected override UUID NewAssetID
        {
            get
            {
                return UUID.Random;
            }
        }

        protected override bool AssetIsLocal
        {
            get
            {
                return false;
            }
        }

        protected override bool AssetIsTemporary
        {
            get
            {
                return false;
            }
        }

        protected override AssetType NewAssetType
        {
            get
            {
                return AssetType.LSLText;
            }
        }
    }
}
