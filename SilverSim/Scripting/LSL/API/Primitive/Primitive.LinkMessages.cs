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
using SilverSim.Types;
using SilverSim.Types.Asset;

namespace SilverSim.Scripting.LSL.API.Primitive
{
    public partial class Primitive_API
    {
        private static void enqueue_to_scripts(ObjectPart part, LinkMessageEvent ev)
        {
            foreach(ObjectPartInventoryItem item in part.Inventory.Values)
            {
                if(item.AssetType == AssetType.LSLText || item.AssetType == AssetType.LSLBytecode)
                {
                    ScriptInstance si = item.ScriptInstance;

                    if(si != null)
                    {
                        si.PostEvent(ev);
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public static void llMessageLinked(ScriptInstance Instance, int link, int num, string str, UUID id)
        {
            lock (Instance)
            {
                LinkMessageEvent ev = new LinkMessageEvent();
                ev.SenderNumber = Instance.Part.LinkNumber;
                ev.TargetNumber = link;
                ev.Number = num;
                ev.Data = str;
                ev.Id = id;

                foreach (ObjectPart part in GetLinkTargets(Instance, link))
                {
                    enqueue_to_scripts(part, ev);
                }
            }
        }
    }
}
