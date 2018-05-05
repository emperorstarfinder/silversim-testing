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

using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Scene.Types.Physics.Vehicle
{
    internal class TimescaleData<T>
    {
        public T Timescale { get; }
        public T OneByTimescale { get; }

        private TimescaleData(T timescale, T onebytimescale)
        {
            Timescale = timescale;
            OneByTimescale = onebytimescale;
        }

        public static TimescaleData<Vector3> Create(Vector3 value) => new TimescaleData<Vector3>(
            value,
            Vector3.One.ElementDivide(value.ComponentMax(double.Epsilon)));

        public static TimescaleData<double> Create(double value) => new TimescaleData<double>(
            value,
            1.0 / Math.Max(double.Epsilon, value));
    }

    internal static class TimescaleExtensionMethods
    {
        public static TimescaleData<Vector3> ToTimescale(this Vector3 value) => TimescaleData<Vector3>.Create(value);

        public static TimescaleData<double> ToTimescale(this double value) => TimescaleData<double>.Create(value);
    }

    public sealed class VehicleParams
    {
        private VehicleType m_VehicleType;

        private ReferenceBoxed<Quaternion> m_ReferenceFrame;

        private TimescaleData<Vector3> m_AngularFrictionTimescale = TimescaleData<Vector3>.Create(Vector3.Zero);
        public Vector3 OneByAngularFrictionTimescale => m_AngularFrictionTimescale.OneByTimescale;

        private ReferenceBoxed<Vector3> m_AngularMotorDirection;
        private TimescaleData<Vector3> m_LinearFrictionTimescale = TimescaleData<Vector3>.Create(Vector3.Zero);
        public Vector3 OneByLinearFrictionTimescale => m_LinearFrictionTimescale.OneByTimescale;

        private ReferenceBoxed<Vector3> m_LinearMotorDirection;
        private ReferenceBoxed<Vector3> m_LinearMotorOffset;

        private ReferenceBoxed<double> m_AngularDeflectionEfficiency;
        private TimescaleData<double> m_AngularDeflectionTimescale = 0.0.ToTimescale();
        public double OneByAngularDeflectionTimescale => m_AngularDeflectionTimescale.OneByTimescale;

        private TimescaleData<Vector3> m_AngularMotorDecayTimescale = Vector3.Zero.ToTimescale();
        public Vector3 OneByAngularMotorDecayTimescale => m_AngularMotorDecayTimescale.OneByTimescale;

        private TimescaleData<Vector3> m_AngularMotorTimescale = Vector3.Zero.ToTimescale();
        public Vector3 OneByAngularMotorTimescale => m_AngularMotorTimescale.OneByTimescale;

        private ReferenceBoxed<double> m_BankingEfficiency;
        private ReferenceBoxed<double> m_BankingMix;
        private TimescaleData<double> m_BankingTimescale = TimescaleData<double>.Create(0);
        public double OneByBankingTimescale => m_BankingTimescale.OneByTimescale;

        private ReferenceBoxed<double> m_Buoyancy;
        private ReferenceBoxed<double> m_HoverHeight;
        private ReferenceBoxed<double> m_HoverEfficiency;
        private TimescaleData<double> m_HoverTimescale = 0.0.ToTimescale();
        public double OneByHoverTimescale => m_HoverTimescale.OneByTimescale;

        private ReferenceBoxed<double> m_LinearDeflectionEfficiency;
        private TimescaleData<double> m_LinearDeflectionTimescale = 0.0.ToTimescale();
        public double OneByLinearDeflectionTimescale => m_LinearDeflectionTimescale.OneByTimescale;

        private TimescaleData<Vector3> m_LinearMotorDecayTimescale = Vector3.Zero.ToTimescale();
        public Vector3 OneByLinearMotorDecayTimescale => m_LinearMotorDecayTimescale.OneByTimescale;

        private TimescaleData<Vector3> m_LinearMotorTimescale = Vector3.Zero.ToTimescale();
        public Vector3 OneByLinearMotorTimescale => m_LinearMotorTimescale.OneByTimescale;

        private ReferenceBoxed<double> m_VerticalAttractionEfficiency;
        private TimescaleData<double> m_VerticalAttractionTimescale = 0.0.ToTimescale();
        public double OneByVerticalAttractionTimescale => m_VerticalAttractionTimescale.OneByTimescale;

        private int m_FlagsStore;

        private ReferenceBoxed<Vector3> m_LinearWindEfficiency;
        private ReferenceBoxed<Vector3> m_AngularWindEfficiency;

        private ReferenceBoxed<double> m_MouselookAzimuth;
        private ReferenceBoxed<double> m_MouselookAltitude;
        private ReferenceBoxed<double> m_BankingAzimuth;
        private ReferenceBoxed<double> m_DisableMotorsAbove;
        private ReferenceBoxed<double> m_DisableMotorsAfter;
        private ReferenceBoxed<double> m_InvertedBankingModifier;

        public void DecayDirections(double dt)
        {
            m_AngularMotorDirection = m_AngularMotorDirection.ElementDivide(m_AngularMotorDecayTimescale.OneByTimescale * dt);
            m_LinearMotorDirection = m_LinearMotorDirection.ElementDivide(m_LinearMotorDecayTimescale.OneByTimescale * dt);
        }

        public VehicleType VehicleType
        {
            get { return m_VehicleType; }

            set
            {
                switch (value)
                {
                    case VehicleType.None:
                        break;

                    case VehicleType.Sled:
                        m_LinearFrictionTimescale = new Vector3(30, 1, 1000).ToTimescale();
                        m_AngularFrictionTimescale = new Vector3(1000, 1000, 1000).ToTimescale();
                        m_LinearMotorDirection = Vector3.Zero;
                        m_LinearMotorTimescale = new Vector3(1000, 1000, 1000).ToTimescale();
                        m_LinearMotorDecayTimescale = new Vector3(120, 120, 120).ToTimescale();
                        m_AngularMotorDirection = Vector3.Zero;
                        m_AngularMotorTimescale = new Vector3(1000, 1000, 1000).ToTimescale();
                        m_AngularMotorDecayTimescale = new Vector3(120, 120, 120).ToTimescale();
                        m_HoverHeight = 0;
                        m_HoverEfficiency = 10;
                        m_HoverTimescale = 10.0.ToTimescale();
                        m_Buoyancy = 0;
                        m_LinearDeflectionEfficiency = 1;
                        m_LinearDeflectionTimescale = 1.0.ToTimescale();
                        m_AngularDeflectionEfficiency = 0;
                        m_AngularDeflectionTimescale = 10.0.ToTimescale();
                        m_VerticalAttractionEfficiency = 1;
                        m_VerticalAttractionTimescale = 1000.0.ToTimescale();
                        m_BankingEfficiency = 0;
                        m_BankingMix = 1;
                        m_BankingTimescale = 10.0.ToTimescale();
                        m_ReferenceFrame = Quaternion.Identity;
                        m_AngularWindEfficiency = Vector3.Zero;
                        m_LinearWindEfficiency = Vector3.Zero;

                        m_InvertedBankingModifier = 1.0;
                        m_BankingMix = 1.0;
                        m_BankingTimescale = 10.0.ToTimescale();
                        m_MouselookAltitude = Math.PI / 4.0;
                        m_MouselookAzimuth = Math.PI / 4.0;
                        m_BankingAzimuth = (float)Math.PI / 2.0;
                        m_DisableMotorsAbove = 0.0;
                        m_DisableMotorsAfter = 0.0;

                        Flags = VehicleFlags.NoDeflectionUp | VehicleFlags.LimitRollOnly | VehicleFlags.LimitMotorUp;
                        break;

                    case VehicleType.Car:
                        m_LinearFrictionTimescale = new Vector3(100, 2, 1000).ToTimescale();
                        m_AngularFrictionTimescale = new Vector3(1000, 1000, 1000).ToTimescale();
                        m_LinearMotorDirection = Vector3.Zero;
                        m_LinearMotorTimescale = new Vector3(1, 1, 1).ToTimescale();
                        m_LinearMotorDecayTimescale = new Vector3(60, 60, 60).ToTimescale();
                        m_AngularMotorDirection = Vector3.Zero;
                        m_AngularMotorTimescale = new Vector3(1, 1, 1).ToTimescale();
                        m_AngularMotorDecayTimescale = new Vector3(0.8, 0.8, 0.8).ToTimescale();
                        m_HoverHeight = 0;
                        m_HoverEfficiency = 0;
                        m_HoverTimescale = 1000.0.ToTimescale();
                        m_Buoyancy = 0;
                        m_LinearDeflectionEfficiency = 1;
                        m_LinearDeflectionTimescale = 2.0.ToTimescale();
                        m_AngularDeflectionEfficiency = 0;
                        m_AngularDeflectionTimescale = 10.0.ToTimescale();
                        m_VerticalAttractionEfficiency = 1;
                        m_VerticalAttractionTimescale = 10.0.ToTimescale();
                        m_BankingEfficiency = -0.2;
                        m_BankingMix = 1;
                        m_BankingTimescale = 1.0.ToTimescale();
                        m_ReferenceFrame = Quaternion.Identity;
                        m_AngularWindEfficiency = Vector3.Zero;
                        m_LinearWindEfficiency = Vector3.Zero;

                        m_InvertedBankingModifier = 1.0;
                        m_BankingMix = 1.0;
                        m_BankingTimescale = 1.0.ToTimescale();
                        m_MouselookAltitude = (float)Math.PI / 4.0;
                        m_MouselookAzimuth = (float)Math.PI / 4.0;
                        m_BankingAzimuth = (float)Math.PI / 2.0;
                        m_DisableMotorsAbove = 0.75;
                        m_DisableMotorsAfter = 2.5f;

                        Flags = VehicleFlags.NoDeflectionUp | VehicleFlags.LimitRollOnly | VehicleFlags.HoverUpOnly | VehicleFlags.LimitMotorUp;
                        break;

                    case VehicleType.Boat:
                        m_LinearFrictionTimescale = new Vector3(10, 3, 2).ToTimescale();
                        m_AngularFrictionTimescale = new Vector3(10, 10, 10).ToTimescale();
                        m_LinearMotorDirection = Vector3.Zero;
                        m_LinearMotorTimescale = new Vector3(5, 5, 5).ToTimescale();
                        m_LinearMotorDecayTimescale = new Vector3(60, 60, 60).ToTimescale();
                        m_AngularMotorDirection = Vector3.Zero;
                        m_AngularMotorTimescale = new Vector3(4, 4, 4).ToTimescale();
                        m_AngularMotorDecayTimescale = new Vector3(4, 4, 4).ToTimescale();
                        m_HoverHeight = 0;
                        m_HoverEfficiency = 0.4;
                        m_HoverTimescale = 2.0.ToTimescale();
                        m_Buoyancy = 1;
                        m_LinearDeflectionEfficiency = 0.5;
                        m_LinearDeflectionTimescale = 3.0.ToTimescale();
                        m_AngularDeflectionEfficiency = 0.5;
                        m_AngularDeflectionTimescale = 5.0.ToTimescale();
                        m_VerticalAttractionEfficiency = 0.5;
                        m_VerticalAttractionTimescale = 5.0.ToTimescale();
                        m_BankingEfficiency = -0.3;
                        m_BankingMix = 0.8;
                        m_BankingTimescale = 1.0.ToTimescale();
                        m_ReferenceFrame = Quaternion.Identity;
                        m_AngularWindEfficiency = Vector3.Zero;
                        m_LinearWindEfficiency = Vector3.Zero;

                        m_InvertedBankingModifier = 1.0;
                        m_BankingMix = 0.5;
                        m_BankingTimescale = 0.2.ToTimescale();
                        m_MouselookAltitude = (float)Math.PI / 4.0;
                        m_MouselookAzimuth = (float)Math.PI / 4.0;
                        m_BankingAzimuth = (float)Math.PI / 2.0;
                        m_DisableMotorsAbove = 0.0;
                        m_DisableMotorsAfter = 0.0;

                        Flags = VehicleFlags.NoDeflectionUp | VehicleFlags.HoverWaterOnly | VehicleFlags.HoverUpOnly | VehicleFlags.LimitMotorUp;
                        break;

                    case VehicleType.Airplane:
                        m_LinearFrictionTimescale = new Vector3(200, 10, 5).ToTimescale();
                        m_AngularFrictionTimescale = new Vector3(20, 20, 20).ToTimescale();
                        m_LinearMotorDirection = Vector3.Zero;
                        m_LinearMotorTimescale = new Vector3(2, 2, 2).ToTimescale();
                        m_LinearMotorDecayTimescale = new Vector3(60, 60, 60).ToTimescale();
                        m_AngularMotorDirection = Vector3.Zero;
                        m_AngularMotorTimescale = new Vector3(4, 4, 4).ToTimescale();
                        m_AngularMotorDecayTimescale = new Vector3(8, 8, 8).ToTimescale();
                        m_HoverHeight = 0;
                        m_HoverEfficiency = 0.5;
                        m_HoverTimescale = 1000.0.ToTimescale();
                        m_Buoyancy = 0;
                        m_LinearDeflectionEfficiency = 0.5;
                        m_LinearDeflectionTimescale = 0.5.ToTimescale();
                        m_AngularDeflectionEfficiency = 1;
                        m_AngularDeflectionTimescale = 2.0.ToTimescale();
                        m_VerticalAttractionEfficiency = 0.9;
                        m_VerticalAttractionTimescale = 2.0.ToTimescale();
                        m_BankingEfficiency = 1;
                        m_BankingMix = 0.7;
                        m_BankingTimescale = 2.0.ToTimescale();
                        m_ReferenceFrame = Quaternion.Identity;
                        m_AngularWindEfficiency = Vector3.Zero;
                        m_LinearWindEfficiency = Vector3.Zero;

                        m_InvertedBankingModifier = 1.0;
                        m_BankingMix = 0.7;
                        m_BankingTimescale = 1.0.ToTimescale();
                        m_MouselookAltitude = Math.PI / 4.0;
                        m_MouselookAzimuth = Math.PI / 4.0;
                        m_BankingAzimuth = Math.PI / 2.0;
                        m_DisableMotorsAbove = 0.0;
                        m_DisableMotorsAfter = 0.0;

                        Flags = VehicleFlags.LimitRollOnly | VehicleFlags.TorqueWorldZ;
                        break;

                    case VehicleType.Balloon:
                        m_LinearFrictionTimescale = new Vector3(5, 5, 5).ToTimescale();
                        m_AngularFrictionTimescale = new Vector3(10, 10, 10).ToTimescale();
                        m_LinearMotorDirection = Vector3.Zero;
                        m_LinearMotorTimescale = new Vector3(5, 5, 5).ToTimescale();
                        m_LinearMotorDecayTimescale = new Vector3(60, 60, 60).ToTimescale();
                        m_AngularMotorDirection = Vector3.Zero;
                        m_AngularMotorTimescale = new Vector3(6, 6, 6).ToTimescale();
                        m_AngularMotorDecayTimescale = new Vector3(10, 10, 10).ToTimescale();
                        m_HoverHeight = 5;
                        m_HoverEfficiency = 0.8;
                        m_HoverTimescale = 10.0.ToTimescale();
                        m_Buoyancy = 1;
                        m_LinearDeflectionEfficiency = 0;
                        m_LinearDeflectionTimescale = 5.0.ToTimescale();
                        m_AngularDeflectionEfficiency = 0;
                        m_AngularDeflectionTimescale = 5.0.ToTimescale();
                        m_VerticalAttractionEfficiency = 1;
                        m_VerticalAttractionTimescale = 1000.0.ToTimescale();
                        m_BankingEfficiency = 0;
                        m_BankingMix = 0.7;
                        m_BankingTimescale = 5.0.ToTimescale();
                        m_ReferenceFrame = Quaternion.Identity;
                        m_AngularWindEfficiency = new Vector3(0.01, 0.01, 0.01);
                        m_LinearWindEfficiency = new Vector3(0.1, 0.1, 0.1);

                        m_InvertedBankingModifier = 1.0;
                        m_BankingMix = 0.5;
                        m_BankingTimescale = 5.0.ToTimescale();
                        m_MouselookAltitude = (float)Math.PI / 4.0;
                        m_MouselookAzimuth = (float)Math.PI / 4.0;
                        m_BankingAzimuth = (float)Math.PI / 2.0;
                        m_DisableMotorsAbove = 0.0;
                        m_DisableMotorsAfter = 0.0;

                        Flags = VehicleFlags.ReactToWind;
                        break;

                    case VehicleType.Motorcycle:    // Halcyon based vehicle type
                        m_LinearFrictionTimescale = new Vector3(100.0, 0.1, 10.0).ToTimescale();
                        m_AngularFrictionTimescale = new Vector3(3.0, 0.2, 10.0).ToTimescale();
                        m_LinearMotorDirection = Vector3.Zero;
                        m_AngularMotorDirection = Vector3.Zero;
                        m_LinearMotorOffset = new Vector3(0.0, 0.0, -0.1);
                        m_LinearMotorTimescale = new Vector3(0.5, 1.0, 1.0).ToTimescale();
                        m_AngularMotorTimescale = new Vector3(0.1, 0.1, 0.05).ToTimescale();
                        m_LinearMotorDecayTimescale = new Vector3(10.0, 1.0, 1.0).ToTimescale();
                        m_AngularMotorDecayTimescale = new Vector3(0.2, 0.8, 0.1).ToTimescale();
                        m_LinearWindEfficiency = Vector3.Zero;
                        m_AngularWindEfficiency = Vector3.Zero;

                        m_HoverHeight = 0.0;
                        m_HoverEfficiency = 0.0;
                        m_HoverTimescale = 1000.0.ToTimescale();
                        m_Buoyancy = 0.0;
                        m_LinearDeflectionEfficiency = 1.0;
                        m_LinearDeflectionTimescale = 2.0.ToTimescale();
                        m_AngularDeflectionEfficiency = 0.8;
                        m_AngularDeflectionTimescale = 2.0.ToTimescale();
                        m_VerticalAttractionEfficiency = 1.0;
                        m_VerticalAttractionTimescale = 1.0.ToTimescale();
                        m_BankingEfficiency = 0.95;
                        m_ReferenceFrame = Quaternion.Identity;

                        m_InvertedBankingModifier = -0.5;
                        m_BankingMix = 0.5;
                        m_BankingTimescale = 0.1.ToTimescale();
                        m_MouselookAltitude = (float)Math.PI / 4.0;
                        m_MouselookAzimuth = (float)Math.PI / 4.0;
                        m_BankingAzimuth = (float)Math.PI / 2.0;
                        m_DisableMotorsAbove = 1.5;
                        m_DisableMotorsAfter = 2.5;

                        Flags = VehicleFlags.NoDeflectionUp | VehicleFlags.HoverUpOnly | VehicleFlags.LimitMotorUp | VehicleFlags.LimitMotorDown |
                                        VehicleFlags.LimitRollOnly | VehicleFlags.TorqueWorldZ;
                        break;

                    case VehicleType.Sailboat:  // Halcyon-based vehicle type
                        m_LinearFrictionTimescale = new Vector3(200.0, 0.5, 3.0).ToTimescale();
                        m_AngularFrictionTimescale = new Vector3(10.0, 1.0, 0.2).ToTimescale();
                        m_LinearMotorDirection = Vector3.Zero;
                        m_AngularMotorDirection = Vector3.Zero;
                        m_LinearMotorOffset = Vector3.Zero;
                        m_LinearMotorTimescale = new Vector3(1.0, 5.0, 5.0).ToTimescale();
                        m_AngularMotorTimescale = new Vector3(2.0, 2.0, 0.1).ToTimescale();
                        m_LinearMotorDecayTimescale = new Vector3(1.0, 10.0, 10.0).ToTimescale();
                        m_AngularMotorDecayTimescale = new Vector3(0.3, 0.3, 0.1).ToTimescale();
                        m_LinearWindEfficiency = new Vector3(0.02, 0.001, 0.0);
                        m_AngularWindEfficiency = new Vector3(0.1, 0.01, 0.0);

                        m_HoverHeight = 0.0001;
                        m_HoverEfficiency = 0.8;
                        m_HoverTimescale = 0.5.ToTimescale();
                        m_Buoyancy = 0.0;
                        m_LinearDeflectionEfficiency = 0.5;
                        m_LinearDeflectionTimescale = 3.0.ToTimescale();
                        m_AngularDeflectionEfficiency = 0.5;
                        m_AngularDeflectionTimescale = 5.0.ToTimescale();
                        m_VerticalAttractionEfficiency = 0.5;
                        m_VerticalAttractionTimescale = 0.3.ToTimescale();
                        m_BankingEfficiency = 0.8;
                        m_InvertedBankingModifier = -0.2;
                        m_BankingMix = 0.5f;
                        m_BankingTimescale = 0.5.ToTimescale();
                        m_MouselookAltitude = Math.PI / 4.0f;
                        m_MouselookAzimuth = Math.PI / 4.0f;
                        m_BankingAzimuth = Math.PI / 2.0f;
                        m_DisableMotorsAbove = 0.0f;
                        m_DisableMotorsAfter = 0.0f;

                        m_ReferenceFrame = Quaternion.Identity;

                        Flags = VehicleFlags.NoDeflectionUp | VehicleFlags.HoverWaterOnly |
                            VehicleFlags.LimitMotorUp | VehicleFlags.LimitMotorDown |
                            VehicleFlags.ReactToWind | VehicleFlags.ReactToCurrents |
                            VehicleFlags.TorqueWorldZ;
                        break;

                    default:
                        throw new InvalidOperationException();
                }

                m_VehicleType = value;
            }
        }

        public VehicleFlags Flags
        {
            get { return (VehicleFlags)m_FlagsStore; }

            set
            {
                m_FlagsStore = (int)value;
            }
        }

        public void SetFlags(VehicleFlags value)
        {
            int setflags = (int)value;
            int oldFlagsStore = m_FlagsStore;
            int newFlagsStore;
            do
            {
                newFlagsStore = oldFlagsStore | setflags;
                oldFlagsStore = Interlocked.CompareExchange(ref m_FlagsStore, newFlagsStore, oldFlagsStore) | setflags;
            } while (newFlagsStore != oldFlagsStore);
        }

        public void ClearFlags(VehicleFlags value)
        {
            int clrflags = ~(int)value;
            int oldFlagsStore = m_FlagsStore;
            int newFlagsStore;
            do
            {
                newFlagsStore = oldFlagsStore & ~clrflags;
                oldFlagsStore = Interlocked.CompareExchange(ref m_FlagsStore, newFlagsStore, oldFlagsStore) & ~clrflags;
            } while (newFlagsStore != oldFlagsStore);
        }

        public Quaternion this[VehicleRotationParamId id]
        {
            get
            {
                switch (id)
                {
                    case VehicleRotationParamId.ReferenceFrame:
                        return m_ReferenceFrame;

                    default:
                        throw new KeyNotFoundException();
                }
            }
            set
            {
                switch (id)
                {
                    case VehicleRotationParamId.ReferenceFrame:
                        m_ReferenceFrame = value;
                        break;

                    default:
                        throw new KeyNotFoundException();
                }
            }
        }

        public Vector3 this[VehicleVectorParamId id]
        {
            get
            {
                switch (id)
                {
                    case VehicleVectorParamId.AngularFrictionTimescale:
                        return m_AngularFrictionTimescale.Timescale;

                    case VehicleVectorParamId.AngularMotorDirection:
                        return m_AngularMotorDirection;

                    case VehicleVectorParamId.LinearFrictionTimescale:
                        return m_LinearFrictionTimescale.Timescale;

                    case VehicleVectorParamId.LinearMotorDirection:
                        return m_LinearMotorDirection;

                    case VehicleVectorParamId.LinearMotorOffset:
                        return m_LinearMotorOffset;

                    case VehicleVectorParamId.AngularMotorDecayTimescale:
                        return m_AngularMotorDecayTimescale.Timescale;

                    case VehicleVectorParamId.AngularMotorTimescale:
                        return m_AngularMotorTimescale.Timescale;

                    case VehicleVectorParamId.LinearMotorDecayTimescale:
                        return m_LinearMotorDecayTimescale.Timescale;

                    case VehicleVectorParamId.LinearMotorTimescale:
                        return m_LinearMotorTimescale.Timescale;

                    case VehicleVectorParamId.AngularWindEfficiency:
                        return m_AngularWindEfficiency;

                    case VehicleVectorParamId.LinearWindEfficiency:
                        return m_LinearWindEfficiency;

                    default:
                        throw new KeyNotFoundException();
                }
            }
            set
            {
                switch (id)
                {
                    case VehicleVectorParamId.AngularFrictionTimescale:
                        m_AngularFrictionTimescale = value.ToTimescale();
                        break;

                    case VehicleVectorParamId.AngularMotorDirection:
                        m_AngularMotorDirection = value;
                        break;

                    case VehicleVectorParamId.LinearFrictionTimescale:
                        m_LinearFrictionTimescale = value.ToTimescale();
                        break;

                    case VehicleVectorParamId.LinearMotorDirection:
                        m_LinearMotorDirection = value;
                        break;

                    case VehicleVectorParamId.LinearMotorOffset:
                        m_LinearMotorOffset = value;
                        break;

                    case VehicleVectorParamId.AngularMotorDecayTimescale:
                        m_AngularMotorDecayTimescale = value.ToTimescale();
                        break;

                    case VehicleVectorParamId.AngularMotorTimescale:
                        m_AngularMotorTimescale = value.ToTimescale();
                        break;

                    case VehicleVectorParamId.LinearMotorDecayTimescale:
                        m_LinearMotorDecayTimescale = value.ToTimescale();
                        break;

                    case VehicleVectorParamId.LinearMotorTimescale:
                        m_LinearMotorTimescale = value.ToTimescale();
                        break;

                    case VehicleVectorParamId.AngularWindEfficiency:
                        m_AngularWindEfficiency = value;
                        break;

                    case VehicleVectorParamId.LinearWindEfficiency:
                        m_LinearWindEfficiency = value;
                        break;

                    default:
                        throw new KeyNotFoundException();
                }
            }
        }

        public double this[VehicleFloatParamId id]
        {
            get
            {
                switch (id)
                {
                    case VehicleFloatParamId.AngularDeflectionEfficiency:
                        return m_AngularDeflectionEfficiency;

                    case VehicleFloatParamId.AngularDeflectionTimescale:
                        return m_AngularDeflectionTimescale.Timescale;

                    case VehicleFloatParamId.LinearDeflectionTimescale:
                        return m_LinearDeflectionTimescale.Timescale;

                    case VehicleFloatParamId.LinearDeflectionEfficiency:
                        return m_LinearDeflectionEfficiency;

                    case VehicleFloatParamId.BankingEfficiency:
                        return m_BankingEfficiency;

                    case VehicleFloatParamId.BankingMix:
                        return m_BankingMix;

                    case VehicleFloatParamId.BankingTimescale:
                        return m_BankingTimescale.Timescale;

                    case VehicleFloatParamId.Buoyancy:
                        return m_Buoyancy;

                    case VehicleFloatParamId.HoverHeight:
                        return m_HoverHeight;

                    case VehicleFloatParamId.HoverEfficiency:
                        return m_HoverEfficiency;

                    case VehicleFloatParamId.HoverTimescale:
                        return m_HoverTimescale.Timescale;

                    case VehicleFloatParamId.VerticalAttractionEfficiency:
                        return m_VerticalAttractionEfficiency;

                    case VehicleFloatParamId.VerticalAttractionTimescale:
                        return m_VerticalAttractionTimescale.Timescale;

                    case VehicleFloatParamId.MouselookAzimuth:
                        return m_MouselookAzimuth;

                    case VehicleFloatParamId.MouselookAltitude:
                        return m_MouselookAltitude;

                    case VehicleFloatParamId.BankingAzimuth:
                        return m_BankingAzimuth;

                    case VehicleFloatParamId.DisableMotorsAbove:
                        return m_DisableMotorsAbove;

                    case VehicleFloatParamId.DisableMotorsAfter:
                        return m_DisableMotorsAfter;

                    case VehicleFloatParamId.InvertedBankingModifier:
                        return m_InvertedBankingModifier;

                    default:
                        throw new KeyNotFoundException();
                }
            }
            set
            {
                switch (id)
                {
                    case VehicleFloatParamId.AngularDeflectionEfficiency:
                        m_AngularDeflectionEfficiency = value;
                        break;

                    case VehicleFloatParamId.AngularDeflectionTimescale:
                        m_AngularDeflectionTimescale = value.ToTimescale();
                        break;

                    case VehicleFloatParamId.LinearDeflectionEfficiency:
                        m_LinearDeflectionEfficiency = value.Clamp(0f, 1f);
                        break;

                    case VehicleFloatParamId.LinearDeflectionTimescale:
                        m_LinearDeflectionTimescale = value.ToTimescale();
                        break;

                    case VehicleFloatParamId.BankingEfficiency:
                        m_BankingEfficiency = value.Clamp(-1f, 1f);
                        break;

                    case VehicleFloatParamId.BankingMix:
                        m_BankingMix = value.Clamp(0f, 1f);
                        break;

                    case VehicleFloatParamId.BankingTimescale:
                        m_BankingTimescale = value.ToTimescale();
                        break;

                    case VehicleFloatParamId.Buoyancy:
                        m_Buoyancy = value.Clamp(-1f, 1f);
                        break;

                    case VehicleFloatParamId.HoverHeight:
                        m_HoverHeight = value;
                        break;

                    case VehicleFloatParamId.HoverEfficiency:
                        m_HoverEfficiency = value.Clamp(0f, 1f);
                        break;

                    case VehicleFloatParamId.HoverTimescale:
                        m_HoverTimescale = value.ToTimescale();
                        break;

                    case VehicleFloatParamId.VerticalAttractionEfficiency:
                        m_VerticalAttractionEfficiency = value.Clamp(0f, 1f);
                        break;

                    case VehicleFloatParamId.VerticalAttractionTimescale:
                        m_VerticalAttractionTimescale = value.ToTimescale();
                        break;

                    case VehicleFloatParamId.MouselookAzimuth:
                        m_MouselookAzimuth = value;
                        break;

                    case VehicleFloatParamId.MouselookAltitude:
                        m_MouselookAltitude = value;
                        break;

                    case VehicleFloatParamId.BankingAzimuth:
                        m_BankingAzimuth = value;
                        break;

                    case VehicleFloatParamId.DisableMotorsAbove:
                        m_DisableMotorsAbove = value;
                        break;

                    case VehicleFloatParamId.DisableMotorsAfter:
                        m_DisableMotorsAfter = value;
                        break;

                    case VehicleFloatParamId.InvertedBankingModifier:
                        m_InvertedBankingModifier = value;
                        break;

                    default:
                        throw new KeyNotFoundException();
                }
            }
        }
    }
}
