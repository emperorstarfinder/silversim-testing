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
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types.Inventory;
using System;

namespace SilverSim.Scripting.LSL.API.Base
{
    public partial class Base_API
    {
        [APILevel(APIFlags.LSL)]
        public static string llGetScriptName(ScriptInstance Instance)
        {
            lock (Instance)
            {
                try
                {
                    return Instance.Item.Name;
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public static void llResetScript(ScriptInstance Instance)
        {
            throw new ResetScriptException();
        }

        [APILevel(APIFlags.LSL)]
        public static void llResetOtherScript(ScriptInstance Instance, string name)
        {
            lock (Instance)
            {
                ObjectPartInventoryItem item;
                ScriptInstance si;
                if (Instance.Part.Inventory.TryGetValue(name, out item))
                {
                    si = item.ScriptInstance;
                    if (item.InventoryType != InventoryType.LSLText && item.InventoryType != InventoryType.LSLBytecode)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a script", name));
                    }
                    else if (null == si)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a compiled script", name));
                    }
                    else
                    {
                        si.PostEvent(new ResetScriptEvent());
                    }
                }
                else
                {
                    throw new Exception(string.Format("Inventory item {0} does not exist", name));
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public static int llGetScriptState(ScriptInstance Instance, string script)
        {
            ObjectPartInventoryItem item;
            ScriptInstance si;
            lock (Instance)
            {
                if (Instance.Part.Inventory.TryGetValue(script, out item))
                {
                    si = item.ScriptInstance;
                    if (item.InventoryType != InventoryType.LSLText && item.InventoryType != InventoryType.LSLBytecode)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a script", script));
                    }
                    else if (null == si)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a compiled script", script));
                    }
                    else
                    {
                        return si.IsRunning ? TRUE : FALSE;
                    }
                }
                else
                {
                    throw new Exception(string.Format("Inventory item {0} does not exist", script));
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public static void llSetScriptState(ScriptInstance Instance, string script, int running)
        {
            ObjectPartInventoryItem item;
            ScriptInstance si;
            lock (Instance)
            {
                if (Instance.Part.Inventory.TryGetValue(script, out item))
                {
                    si = item.ScriptInstance;
                    if (item.InventoryType != InventoryType.LSLText && item.InventoryType != InventoryType.LSLBytecode)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a script", script));
                    }
                    else if (null == si)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a compiled script", script));
                    }
                    else
                    {
                        si.IsRunning = running != 0;
                    }
                }
                else
                {
                    throw new Exception(string.Format("Inventory item {0} does not exist", script));
                }
            }
        }
    }
}
