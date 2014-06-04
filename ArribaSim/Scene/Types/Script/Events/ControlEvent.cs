﻿/*
 * ArribaSim is distributed under the terms of the
 * GNU General Public License v2 
 * with the following clarification and special exception.
 * 
 * Linking this code statically or dynamically with other modules is
 * making a combined work based on this code. Thus, the terms and
 * conditions of the GNU General Public License cover the whole
 * combination.
 * 
 * As a special exception, the copyright holders of this code give you
 * permission to link this code with independent modules to produce an
 * executable, regardless of the license terms of these independent
 * modules, and to copy and distribute the resulting executable under
 * terms of your choice, provided that you also meet, for each linked
 * independent module, the terms and conditions of the license of that
 * module. An independent module is a module which is not derived from
 * or based on this code. If you modify this code, you may extend
 * this exception to your version of the code, but you are not
 * obligated to do so. If you do not wish to do so, delete this
 * exception statement from your version.
 * 
 * License text is derived from GNU classpath text
 */

using ArribaSim.Types;

namespace ArribaSim.Scene.Types.Script.Events
{
    public struct ControlEvent : IScriptEvent
    {
        public enum ControlFlags : int
        {
            Forward = 0x00000001,
            Back = 0x00000002,
            Left = 0x00000004,
            Right = 0x00000008,
            RotateLeft = 0x00000100,
            RotateRight = 0x00000200,
            Up = 0x00000010,
            Down = 0x00000020,
            LButton = 0x10000000,
            MouseLook_LButton = 0x40000000
        }

        public UUID AgentID;
        public int Level;
        public int Flags;
    }
}
