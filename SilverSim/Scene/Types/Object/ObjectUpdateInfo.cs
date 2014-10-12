﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SilverSim.LL.Messages;
using SilverSim.Types;

namespace SilverSim.Scene.Types.Object
{
    public class ObjectUpdateInfo
    {

        private bool m_Killed = false;
        public uint LocalID;
        private ObjectPart m_Part;
        private int m_SerialNumber;

        public ObjectUpdateInfo(ObjectPart part)
        {
            m_Part = part;
            LocalID = part.LocalID;
            m_SerialNumber = 0;
        }

        public void KillObject()
        {
            lock(this)
            {
                m_Killed = true;
            }
        }

        private int clampSignedTo(double val, double imin, double imax, int omin, int omax)
        {
            double range = val * (omax - omin) / (imax - imin);
            range += omin;
            return (int)range;
        }

        private uint clampUnsignedTo(double val, double imin, double imax, uint omin, uint omax)
        {
            double range = val * (omax - omin) / (imax - imin);
            range += omin;
            return (uint)range;
        }

        public SilverSim.LL.Messages.Object.ObjectUpdate.ObjData SerializeFull()
        {
            lock(this)
            {
                if(m_Killed)
                {
                    return null;
                }
                else
                {
                    SilverSim.LL.Messages.Object.ObjectUpdate.ObjData m = new LL.Messages.Object.ObjectUpdate.ObjData();
                    m.ClickAction = m_Part.ClickAction;
                    m.CRC = (uint)m_SerialNumber;
                    m.ExtraParams = m_Part.ExtraParamsBytes;
                    m.Flags = 0;
                    m.FullID = m_Part.ID;
                    m.Gain = 0;
                    m.JointAxisOrAnchor = Vector3.Zero;
                    m.JointPivot = Vector3.Zero;
                    m.JointType = 0;
                    m.LocalID = m_Part.LocalID;
                    m.LoopedSound = UUID.Zero;
                    m.Material = m_Part.Material;
                    m.MediaURL = "";
                    m.NameValue = m_Part.Name;
                    m.ObjectData = new byte[60];
                    m_Part.Position.ToBytes(m.ObjectData, 0);
                    m_Part.Velocity.ToBytes(m.ObjectData, 12);
                    m_Part.Acceleration.ToBytes(m.ObjectData, 24);
                    m_Part.Rotation.ToBytes(m.ObjectData, 36);
                    m_Part.AngularVelocity.ToBytes(m.ObjectData, 48);
                    m.OwnerID = m_Part.Owner.ID;
                    m.ParentID = m_Part.Group.RootPart.LocalID;
                    ObjectPart.PrimitiveShape shape = m_Part.Shape;
                    m.PathBegin = shape.PathBegin;
                    m.PathEnd = shape.PathEnd;
                    m.PathRadiusOffset = shape.PathRadiusOffset;
                    m.PathRevolutions = shape.PathRevolutions;
                    m.PathScaleX = shape.PathScaleX;
                    m.PathScaleY = shape.PathScaleY;
                    m.PathShearX = shape.PathShearX;
                    m.PathShearY = shape.PathShearY;
                    m.PathSkew = shape.PathSkew;
                    m.PathTaperX = shape.PathTaperX;
                    m.PathTaperY = shape.PathTaperY;
                    m.PathTwist = shape.PathTwist;
                    m.PathTwistBegin = shape.PathTwistBegin;
                    m.PCode = 0;
                    m.ProfileBegin = shape.ProfileBegin;
                    m.ProfileCurve = shape.ProfileCurve;
                    m.ProfileEnd = shape.ProfileEnd;
                    m.ProfileHollow = shape.ProfileHollow;
                    m.PSBlock = m_Part.ParticleSystemBytes;
                    m.Radius = 0;
                    m.Scale = m_Part.Size;
                    m.State = 0;
                    ObjectPart.TextParam textparam = m_Part.Text;
                    m.Text = textparam.Text;
                    m.TextColor = textparam.TextColor;
                    m.TextureAnim = new byte[0];
                    m.TextureEntry = m_Part.TextureEntryBytes;
                    m.UpdateFlags = 0;

                    if(m_Part.Shape.SculptType == SilverSim.Types.Primitive.PrimitiveSculptType.Mesh)
                    {
                        m.ProfileBegin = 12500;
                        m.ProfileEnd = 0;
                        m.ProfileHollow = 27500;
                    }

                    return m;
                }
            }
        }

        public SilverSim.LL.Messages.Object.ImprovedTerseObjectUpdate.ObjData SerializeTerse()
        {
            lock(this)
            {
                if(m_Killed)
                {
                    return null;
                }
                else
                {
                    SilverSim.LL.Messages.Object.ImprovedTerseObjectUpdate.ObjData objdata = new LL.Messages.Object.ImprovedTerseObjectUpdate.ObjData();
                    objdata.Data = m_Part.TerseData;
                    objdata.TextureEntry = m_Part.TextureEntryBytes;
                    return objdata;
                }
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
                    if(m_Part != null && !m_Killed)
                    {
                        return m_Part.Group.IsPhysics;
                    }
                }
                return false;
            }
        }

        public int SerialNumber
        {
            get
            {
                return m_SerialNumber;
            }
        }

        public void IncSerialNumber()
        {
            Interlocked.Increment(ref m_SerialNumber);
        }
    }
}
