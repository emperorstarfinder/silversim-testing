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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using System;

namespace SilverSim.Scripting.LSL.API.Notecards
{
    public partial class Notecard_API
    {
        #region osMakeNotecard
        [APILevel(APIFlags.OSSL)]
        public void osMakeNotecard(ScriptInstance Instance, string notecardName, AnArray contents)
        {
            string nc = string.Empty;

            foreach(IValue val in contents)
            {
                if(!string.IsNullOrEmpty(nc))
                {
                    nc += "\n";
                }
                nc += val.ToString();
            }
            osMakeNotecard(Instance, notecardName, nc);
        }

        [APILevel(APIFlags.OSSL)]
        public void osMakeNotecard(ScriptInstance Instance, string notecardName, string contents)
        {
            lock (Instance)
            {
                Notecard nc = new Notecard();
                nc.Text = contents;
                AssetData asset = nc;
                asset.ID = UUID.Random;
                asset.Name = notecardName;
                asset.Creator = Instance.Part.ObjectGroup.Owner;
                asset.Description = "osMakeNotecard";
                Instance.Part.ObjectGroup.Scene.AssetService.Store(asset);
                ObjectPartInventoryItem item = new ObjectPartInventoryItem(asset);
                item.ParentFolderID = Instance.Part.ID;

                for (uint i = 0; i < 1000; ++i)
                {
                    if (i == 0)
                    {
                        item.Name = notecardName;
                    }
                    else
                    {
                        item.Name = string.Format("{0} {1}", notecardName, i);
                    }
                    try
                    {
                        Instance.Part.Inventory.Add(item.ID, item.Name, item);
                    }
                    catch
                    {
                        return;
                    }
                }
            }
            throw new Exception(string.Format("Could not store notecard with name {0}", notecardName));
        }
        #endregion

        #region osGetNotecard
        [APILevel(APIFlags.OSSL)]
        public string osGetNotecard(ScriptInstance Instance, string name)
        {
            lock (Instance)
            {
                ObjectPartInventoryItem item;
                if (Instance.Part.Inventory.TryGetValue(name, out item))
                {
                    if (item.InventoryType != InventoryType.Notecard)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a notecard", name));
                    }
                    else
                    {
                        Notecard nc = Instance.Part.ObjectGroup.Scene.GetService<NotecardCache>()[item.AssetID];
                        return nc.Text;
                    }
                }
                else
                {
                    throw new Exception(string.Format("Inventory item {0} does not exist", name));
                }
            }
        }
        #endregion

        #region osGetNotecardLine
        [APILevel(APIFlags.OSSL)]
        public string osGetNotecardLine(ScriptInstance Instance, string name, int line)
        {
            ObjectPartInventoryItem item;
            lock (Instance)
            {
                if (Instance.Part.Inventory.TryGetValue(name, out item))
                {
                    if (item.InventoryType != InventoryType.Notecard)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a notecard", name));
                    }
                    else
                    {
                        Notecard nc = Instance.Part.ObjectGroup.Scene.GetService<NotecardCache>()[item.AssetID];
                        string[] lines = nc.Text.Split('\n');
                        if (line >= lines.Length || line < 0)
                        {
                            return EOF;
                        }
                        return lines[line];
                    }
                }
                else
                {
                    throw new Exception(string.Format("Inventory item {0} does not exist", name));
                }
            }
        }
        #endregion

        #region osGetNumberOfNotecardLines
        [APILevel(APIFlags.OSSL)]
        public int osGetNumberOfNotecardLines(ScriptInstance Instance, string name)
        {
            ObjectPartInventoryItem item;
            lock (Instance)
            {
                if (Instance.Part.Inventory.TryGetValue(name, out item))
                {
                    if (item.InventoryType != InventoryType.Notecard)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a notecard", name));
                    }
                    else
                    {
                        Notecard nc = Instance.Part.ObjectGroup.Scene.GetService<NotecardCache>()[item.AssetID];
                        return nc.Text.Split('\n').Length;
                    }
                }
                else
                {
                    throw new Exception(string.Format("Inventory item {0} does not exist", name));
                }
            }
        }
        #endregion
    }
}
