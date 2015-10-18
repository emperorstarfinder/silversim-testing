﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SilverSim.Viewer.Messages;
using SilverSim.Types;
using SilverSim.Types.Primitive;

namespace SilverSim.Scene.Types.Object
{
    public class ObjectUpdateInfo
    {

        private bool m_Killed;
        public uint LocalID;
        public ObjectPart Part { get; private set; }
        public ObjectUpdateInfo(ObjectPart part)
        {
            Part = part;
            LocalID = part.LocalID;
        }

        public void KillObject()
        {
            lock(this)
            {
                m_Killed = true;
            }
        }

        public bool IsKilled
        {
            get
            {
                return m_Killed;
            }
        }

        public bool IsPhysics
        {
            get
            {
                lock(this)
                {
                    if(Part != null && !m_Killed)
                    {
                        return Part.ObjectGroup.IsPhysics;
                    }
                }
                return false;
            }
        }

        public byte[] FullUpdate
        {
            get
            {
                lock(this)
                {
                    if(Part != null && !m_Killed)
                    {
                        return Part.FullUpdateData;
                    }
                    return null;
                }
            }
        }

        public byte[] TerseUpdate
        {
            get
            {
                lock (this)
                {
                    if (Part != null && !m_Killed)
                    {
                        return Part.TerseUpdateData;
                    }
                    return null;
                }
            }
        }

        public byte[] PropertiesUpdate
        {
            get
            {
                lock (this)
                {
                    if (Part != null && !m_Killed)
                    {
                        return Part.PropertiesUpdateData;
                    }
                    return null;
                }
            }
        }

        public int SerialNumber
        {
            get
            {
                return Part.SerialNumber;
            }
        }
    }
}
