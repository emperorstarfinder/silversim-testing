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

using SilverSim.Types.Asset.Format;
using System;
using Color3 = SilverSim.Types.Color;

namespace SilverSim.Scene.Agent.Bakery.SubBakers.Clothing
{
    public sealed class SkirtSubBaker : AbstractSubBaker
    {
        public SkirtSubBaker(Wearable skirt)
        {
            if(skirt.Type != WearableType.Skirt)
            {
                throw new ArgumentException(nameof(skirt));
            }
        }

        public override WearableType Type => WearableType.Skirt;

        public override void Dispose()
        {
        }

        private static Color3 GetSkirtColor(Wearable skirt)
        {
            var col = new Color3(1, 1, 1);
            double val;

            if (skirt.Params.TryGetValue(921, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(922, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(923, out val))
            {
                col.B = val.LimitRange(0, 1);
            }

            return col;
        }
    }
}
