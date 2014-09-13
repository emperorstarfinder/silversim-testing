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

using System;

namespace SilverSim.Types.Primitive
{
    public enum Bumpiness : byte
    {
        None = 0,
        Brightness = 1,
        Darkness = 2,
        Woodgrain = 3,
        Bark = 4,
        Bricks = 5,
        Checker = 6,
        Concrete = 7,
        Crustytile = 8,
        Cutstone = 9,
        Discs = 10,
        Gravel = 11,
        Petridish = 12,
        Siding = 13,
        Stonetile = 14,
        Stucco = 15,
        Suction = 16,
        Weave = 17
    }

    public enum Shininess : byte
    {
        None = 0,
        Low = 0x40,
        Medium = 0x80,
        High = 0xc0
    }

    public enum MappingType : byte
    {
        Default = 0,
        Planar = 2,
        Spherical = 4,
        Cylindrical = 6
    }

    [Flags]
    public enum TextureAttributes : uint
    {
        None = 0,
        TextureID = 1 << 0,
        RGBA = 1 << 1,
        RepeatU = 1 << 2,
        RepeatV = 1 << 3,
        OffsetU = 1 << 4,
        OffsetV = 1 << 5,
        Rotation = 1 << 6,
        Material = 1 << 7,
        Media = 1 << 8,
        Glow = 1 << 9,
        MaterialID = 1 << 10,
        All = 0xFFFFFFFF
    }
}
