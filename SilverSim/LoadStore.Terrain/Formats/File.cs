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

using SilverSim.Viewer.Messages.LayerData;
using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.Terrain;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.ComponentModel;

namespace SilverSim.LoadStore.Terrain.Formats
{
    [TerrainStorageType]
    [Description("Terrain Generic Storage Loader")]
    public class File : ITerrainFileStorage, IPlugin
    {
        public File()
        {

        }

        public string Name
        {
            get 
            { 
                return "file"; 
            }
        }

        public List<LayerPatch> LoadFile(string filename, int suggested_width, int suggested_height)
        {
            using(Bitmap bitmap = new Bitmap(filename))
            {
                return bitmap.ToPatchesFromGrayscale();
            }
        }

        public List<LayerPatch> LoadStream(Stream input, int suggested_width, int suggested_height)
        {
            using(Bitmap bitmap = new Bitmap(input))
            {
                return bitmap.ToPatchesFromGrayscale();
            }
        }

        public void SaveFile(string filename, List<LayerPatch> terrain)
        {
            throw new NotSupportedException();
        }

        public void SaveStream(Stream output, List<LayerPatch> terrain)
        {
            throw new NotSupportedException();
        }

        public bool SupportsLoading
        {
            get
            {
                return true; 
            }
        }

        public bool SupportsSaving
        {
            get 
            {
                return false; 
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
    }
}
