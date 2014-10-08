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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script.Events;
using System.Reflection;

namespace SilverSim.Scripting.LSL.Variants.OSSL
{
    public partial class OSSLScript
    {
        /// <summary>
        /// Set parameters for light projection in host prim 
        /// </summary>
        public void osSetProjectionParams(bool projection, UUID texture, double fov, double focus, double amb)
        {
            osSetProjectionParams(UUID.Zero, projection, texture, fov, focus, amb);
        }

        /// <summary>
        /// Set parameters for light projection with uuid of target prim
        /// </summary>
        public void osSetProjectionParams(UUID prim, bool projection, UUID texture, double fov, double focus, double amb)
        {
            CheckThreatLevel(MethodBase.GetCurrentMethod().Name, ThreatLevelType.High);
            lock (this)
            {
                ObjectPart part;
                if (prim == UUID.Zero)
                {
                    part = Part;
                }
                else
                {
                    try
                    {
                        part = Part.Group.Scene.Primitives[prim];
                    }
                    catch
                    {
                        return;
                    }
                }

                ObjectPart.ProjectionParam p = new ObjectPart.ProjectionParam();
                p.IsProjecting = projection;
                p.ProjectionTextureID = texture;
                p.ProjectionFOV = fov;
                p.ProjectionFocus = focus;
                p.ProjectionAmbience = amb;
                part.Projection = p;
            }
        }

    }
}
