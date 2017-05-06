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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;

namespace SilverSim.Scene.Physics.Common
{
    public abstract class CommonPhysicsController
    {
        public double CombinedGravityAccelerationConstant
        {
            get
            {
                return BaseGravityAccelerationConstant + AdditionalGravityAccelerationConstant;
            }
        }
        public double AdditionalGravityAccelerationConstant { get; set; }
        protected abstract double BaseGravityAccelerationConstant { get; }

        static CommonPhysicsController()
        {
        }

        protected CommonPhysicsController()
        {

        }

        protected abstract SceneInterface.LocationInfoProvider LocationInfoProvider
        {
            get;
        }

        public struct PositionalForce
        {
            public string Name;
            public Vector3 Force;
            public Vector3 LocalPosition;

            public PositionalForce(string name, Vector3 force, Vector3 localpos)
            {
                Name = name;
                Force = force;
                LocalPosition = localpos;
            }
        }

        #region Gravity and Buoyancy
        protected double GravityConstant(IPhysicalObject obj)
        {
            return obj.PhysicsActor.Mass * CombinedGravityAccelerationConstant * obj.PhysicsGravityMultiplier;
        }

        protected PositionalForce GravityMotor(IPhysicalObject obj, Vector3 pos)
        {
            return new PositionalForce("GravityMotor", new Vector3(0, 0, -GravityConstant(obj)), pos);
        }

        double m_Buoyancy;

        public double Buoyancy 
        {
            get
            {
                return m_Buoyancy;
            }

            set
            {
                m_Buoyancy = (value < 0) ? 0 : value;
            }
        }


        protected PositionalForce BuoyancyMotor(IPhysicalObject obj, Vector3 pos)
        {
            return new PositionalForce("BuoyancyMotor", new Vector3(0, 0, (m_Buoyancy - 1) * GravityConstant(obj)), pos);
        }
        #endregion

        #region Hover Motor
        double m_HoverHeight;
        bool m_HoverEnabled;
        bool m_AboveWater;
        double m_HoverTau;
        object m_HoverParamsLock = new object();
        protected PositionalForce HoverMotor(IPhysicalObject obj, Vector3 pos)
        {
            lock (m_HoverParamsLock)
            {
                if (m_HoverEnabled)
                {
                    Vector3 v = new Vector3(0, 0, (m_Buoyancy - 1) * GravityConstant(obj));
                    double targetHoverHeight;
                    SceneInterface.LocationInfo locInfo = LocationInfoProvider.At(obj.GlobalPosition);
                    targetHoverHeight = locInfo.GroundHeight;
                    if (targetHoverHeight < locInfo.WaterHeight && m_AboveWater)
                    {
                        targetHoverHeight = locInfo.WaterHeight;
                    }
                    v.Z += (targetHoverHeight - obj.Position.Z) * m_HoverTau;
                    return new PositionalForce("HoverMotor", v, pos);
                }
                else
                {
                    return new PositionalForce("HoverMotor", Vector3.Zero, pos);
                }
            }
        }

        public void SetHoverHeight(double height, bool water, double tau)
        {
            lock (m_HoverParamsLock)
            {
                m_HoverEnabled = tau > double.Epsilon;
                m_HoverHeight = height;
                m_AboveWater = water;
                m_HoverTau = tau;
            }
        }

        public void StopHover()
        {
            lock (m_HoverParamsLock)
            {
                m_HoverEnabled = false;
            }
        }

        #endregion

        #region LookAt Motor

        public void SetLookAt(Quaternion q, double strength, double damping)
        {

        }

        public void StopLookAt()
        {

        }
        #endregion

        #region Restitution Motor
        protected PositionalForce LinearRestitutionMotor(IPhysicalObject obj, double factor, Vector3 pos)
        {
            return new PositionalForce("LinearResitutionMotor", - obj.Velocity * factor, pos);
        }
        #endregion

        #region Target Velocity Motor
        protected PositionalForce TargetVelocityMotor(IPhysicalObject obj, Vector3 targetvel, double factor, Vector3 pos)
        {
            return new PositionalForce("TargetVelocityMotor", (targetvel - obj.Velocity) * factor, pos);
        }
        #endregion
    }
}
