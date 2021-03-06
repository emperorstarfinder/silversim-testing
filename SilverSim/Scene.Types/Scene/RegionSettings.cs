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

using SilverSim.Types;
using SilverSim.Types.Estate;
using System;
using System.IO;

namespace SilverSim.Scene.Types.Scene
{
    public class RegionSettings
    {
        public RegionSettings()
        {
        }

        public RegionSettings(RegionSettings src)
        {
            BlockTerraform = src.BlockTerraform;
            BlockFly = src.BlockFly;
            AllowDamage = src.AllowDamage;
            BlockDwell = src.BlockDwell;
            RestrictPushing = src.RestrictPushing;
            AllowLandResell = src.AllowLandResell;
            AllowLandJoinDivide = src.AllowLandJoinDivide;
            BlockShowInSearch = src.BlockShowInSearch;
            AgentLimit = src.AgentLimit;
            ObjectBonus = src.ObjectBonus;
            DisableScripts = src.DisableScripts;
            DisableCollisions = src.DisableCollisions;
            DisablePhysics = src.DisablePhysics;
            BlockFlyOver = src.BlockFlyOver;
            SunPosition = src.SunPosition;
            IsSunFixed = src.IsSunFixed;
            UseEstateSun = src.UseEstateSun;
            ResetHomeOnTeleport = src.ResetHomeOnTeleport;
            AllowLandmark = src.AllowLandmark;
            AllowDirectTeleport = src.AllowDirectTeleport;
            m_TerrainTexture1 = src.m_TerrainTexture1;
            m_TerrainTexture2 = src.m_TerrainTexture2;
            m_TerrainTexture3 = src.m_TerrainTexture3;
            m_TerrainTexture4 = src.m_TerrainTexture4;
            Elevation1NW = src.Elevation1NW;
            Elevation2NW = src.Elevation2NW;
            Elevation1NE = src.Elevation1NE;
            Elevation2NE = src.Elevation2NE;
            Elevation1SE = src.Elevation1SE;
            Elevation2SE = src.Elevation2SE;
            Elevation1SW = src.Elevation1SW;
            Elevation2SW = src.Elevation2SW;
            WaterHeight = src.WaterHeight;
            TerrainRaiseLimit = src.TerrainRaiseLimit;
            TerrainLowerLimit = src.TerrainLowerLimit;
            Sandbox = src.Sandbox;
            TelehubObject = src.TelehubObject;
            MaxBasePrims = src.MaxBasePrims;
            WalkableCoefficientsUnderwater = new WalkingCoefficients(src.WalkableCoefficientsUnderwater);
            WalkableCoefficientsTerrain0 = new WalkingCoefficients(src.WalkableCoefficientsTerrain0);
            WalkableCoefficientsTerrain1 = new WalkingCoefficients(src.WalkableCoefficientsTerrain1);
            WalkableCoefficientsTerrain2 = new WalkingCoefficients(src.WalkableCoefficientsTerrain2);
            WalkableCoefficientsTerrain3 = new WalkingCoefficients(src.WalkableCoefficientsTerrain3);
        }

        public class WalkingCoefficients
        {
            private double m_Avatar = 1;
            private double m_A = 1;
            private double m_B = 1;
            private double m_C = 1;
            private double m_D = 1;

            public double Avatar
            {
                get
                {
                    return m_Avatar;
                }
                set
                {
                    m_Avatar = Math.Min(0, value);
                }
            }

            public double A
            {
                get
                {
                    return m_A;
                }
                set
                {
                    m_A = Math.Min(0, value);
                }
            }

            public double B
            {
                get
                {
                    return m_B;
                }
                set
                {
                    m_B = Math.Min(0, value);
                }
            }

            public double C
            {
                get
                {
                    return m_C;
                }
                set
                {
                    m_C = Math.Min(0, value);
                }
            }

            public double D
            {
                get
                {
                    return m_D;
                }
                set
                {
                    m_D = Math.Min(0, value);
                }
            }

            public WalkingCoefficients()
            {
            }

            public WalkingCoefficients(WalkingCoefficients src)
            {
                Avatar = src.Avatar;
                A = src.A;
                B = src.B;
                C = src.C;
                D = src.D;
            }

            public void CopyFrom(WalkingCoefficients src)
            {
                Avatar = src.Avatar;
                A = src.A;
                B = src.B;
                C = src.C;
                D = src.D;
            }
        }

        public bool BlockTerraform;
        public bool BlockFly;
        public bool AllowDamage;
        public bool BlockDwell = true;
        public bool RestrictPushing;
        public bool AllowLandResell;
        public bool AllowLandJoinDivide;
        public bool BlockShowInSearch;
        public int AgentLimit = 40;
        public double ObjectBonus = 1.0;
        public bool DisableScripts;
        public bool DisableCollisions;
        public bool DisablePhysics;
        public bool BlockFlyOver;
        public double SunPosition;
        public bool IsSunFixed;
        public bool UseEstateSun;
        public bool ResetHomeOnTeleport;
        public bool AllowLandmark;
        public bool AllowDirectTeleport;
        public int MaxBasePrims = 45000;
        public readonly WalkingCoefficients WalkableCoefficientsUnderwater = new WalkingCoefficients { Avatar = 0, A = 0, B = 0, C = 0, D = 0 };
        public readonly WalkingCoefficients WalkableCoefficientsTerrain0 = new WalkingCoefficients();
        public readonly WalkingCoefficients WalkableCoefficientsTerrain1 = new WalkingCoefficients();
        public readonly WalkingCoefficients WalkableCoefficientsTerrain2 = new WalkingCoefficients();
        public readonly WalkingCoefficients WalkableCoefficientsTerrain3 = new WalkingCoefficients();

        private static void WriteDouble(Stream s, double v)
        {
            byte[] d = BitConverter.GetBytes(v);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(d);
            }
            s.Write(d, 0, d.Length);
        }

        private static double ReadDouble(Stream s)
        {
            byte[] d = new byte[8];
            if (s.Read(d, 0, d.Length) != d.Length)
            {
                throw new EndOfStreamException();
            }
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(d);
            }
            return BitConverter.ToDouble(d, 0);
        }

        private static void WriteShort(Stream s, short v)
        {
            byte[] d = BitConverter.GetBytes(v);
            if(!BitConverter.IsLittleEndian)
            {
                Array.Reverse(d);
            }
            s.Write(d, 0, d.Length);
        }

        private static short ReadShort(Stream s)
        {
            byte[] d = new byte[2];
            if(s.Read(d, 0, d.Length) != d.Length)
            {
                throw new EndOfStreamException();
            }
            if(!BitConverter.IsLittleEndian)
            {
                Array.Reverse(d);
            }
            return BitConverter.ToInt16(d, 0);
        }

        private static void WriteCoeffs(Stream s, short index, WalkingCoefficients coeff)
        {
            WriteShort(s, index);
            WriteShort(s, 5);
            WriteDouble(s, coeff.Avatar);
            WriteDouble(s, coeff.A);
            WriteDouble(s, coeff.B);
            WriteDouble(s, coeff.C);
            WriteDouble(s, coeff.D);
        }

        private static short ReadCoeffs(Stream s, out WalkingCoefficients coeff)
        {
            short index = ReadShort(s);
            short count = ReadShort(s);
            coeff = new WalkingCoefficients();

            if(index == -1)
            {
                coeff.Avatar = 0;
                coeff.A = 0;
                coeff.B = 0;
                coeff.C = 0;
                coeff.D = 0;
            }

            for(short i = 0; i < count; ++i)
            {
                switch(i)
                {
                    case 0:
                        coeff.Avatar = ReadDouble(s);
                        break;

                    case 1:
                        coeff.A = ReadDouble(s);
                        break;

                    case 2:
                        coeff.B = ReadDouble(s);
                        break;

                    case 3:
                        coeff.C = ReadDouble(s);
                        break;

                    case 4:
                        coeff.D = ReadDouble(s);
                        break;
                }
            }
            return index;
        }

        public byte[] WalkableCoefficientsSerialization
        {
            get
            {
                using (var ms = new MemoryStream())
                {
                    WriteCoeffs(ms, -1, WalkableCoefficientsUnderwater);
                    WriteCoeffs(ms, 0, WalkableCoefficientsTerrain0);
                    WriteCoeffs(ms, 1, WalkableCoefficientsTerrain1);
                    WriteCoeffs(ms, 2, WalkableCoefficientsTerrain2);
                    WriteCoeffs(ms, 3, WalkableCoefficientsTerrain3);

                    return ms.ToArray();
                }
            }

            set
            {
                using (var ms = new MemoryStream(value))
                {
                    WalkingCoefficients coeffs;
                    while (ms.Length > ms.Position)
                    {
                        switch (ReadCoeffs(ms, out coeffs))
                        {
                            case -1:
                                WalkableCoefficientsUnderwater.CopyFrom(coeffs);
                                break;

                            case 0:
                                WalkableCoefficientsTerrain0.CopyFrom(coeffs);
                                break;

                            case 1:
                                WalkableCoefficientsTerrain1.CopyFrom(coeffs);
                                break;

                            case 2:
                                WalkableCoefficientsTerrain2.CopyFrom(coeffs);
                                break;

                            case 3:
                                WalkableCoefficientsTerrain3.CopyFrom(coeffs);
                                break;
                        }
                    }
                }
            }
        }

        private UUID m_TerrainTexture1 = TextureConstant.DefaultTerrainTexture1;
        public UUID TerrainTexture1
        {
            get { return m_TerrainTexture1; }

            set
            {
                m_TerrainTexture1 = (value == UUID.Zero) ?
                    TextureConstant.DefaultTerrainTexture1 :
                    value;
            }
        }

        private UUID m_TerrainTexture2 = TextureConstant.DefaultTerrainTexture2;
        public UUID TerrainTexture2
        {
            get { return m_TerrainTexture2; }

            set
            {
                m_TerrainTexture2 = (value == UUID.Zero) ?
                    TextureConstant.DefaultTerrainTexture2 :
                    value;
            }
        }

        private UUID m_TerrainTexture3 = TextureConstant.DefaultTerrainTexture3;
        public UUID TerrainTexture3
        {
            get { return m_TerrainTexture3; }

            set
            {
                m_TerrainTexture3 = (value == UUID.Zero) ?
                    TextureConstant.DefaultTerrainTexture3 :
                    value;
            }
        }

        private UUID m_TerrainTexture4 = TextureConstant.DefaultTerrainTexture4;
        public UUID TerrainTexture4
        {
            get { return m_TerrainTexture4; }

            set
            {
                m_TerrainTexture4 = (value == UUID.Zero) ?
                    TextureConstant.DefaultTerrainTexture4 :
                    value;
            }
        }

        public double Elevation1NW = 10;
        public double Elevation2NW = 60;
        public double Elevation1NE = 10;
        public double Elevation2NE = 60;
        public double Elevation1SE = 10;
        public double Elevation2SE = 60;
        public double Elevation1SW = 10;
        public double Elevation2SW = 60;

        public double WaterHeight = 20;
        public double TerrainRaiseLimit = 100;
        public double TerrainLowerLimit = -100;

        public bool Sandbox;

        public UUID TelehubObject = UUID.Zero;

        public int MaxTotalPrims => (int)(MaxBasePrims * ObjectBonus);

        public RegionOptionFlags AsFlags
        {
            get
            {
                RegionOptionFlags flags = 0;
                if (AllowDirectTeleport)
                {
                    flags |= RegionOptionFlags.AllowDirectTeleport;
                }
                if (AllowLandmark)
                {
                    flags |= RegionOptionFlags.AllowLandmark;
                }
                if (ResetHomeOnTeleport)
                {
                    flags |= RegionOptionFlags.ResetHomeOnTeleport;
                }
                if (IsSunFixed)
                {
                    flags |= RegionOptionFlags.SunFixed;
                }
                if (AllowDamage)
                {
                    flags |= RegionOptionFlags.AllowDamage;
                }
                if (BlockTerraform)
                {
                    flags |= RegionOptionFlags.BlockTerraform;
                }
                if (!AllowLandResell)
                {
                    flags |= RegionOptionFlags.BlockLandResell;
                }
                if (BlockDwell)
                {
                    flags |= RegionOptionFlags.BlockDwell;
                }
                if (BlockFlyOver)
                {
                    flags |= RegionOptionFlags.BlockFlyOver;
                }
                if (DisableCollisions)
                {
                    flags |= RegionOptionFlags.DisableAgentCollisions;
                }
                if (DisableScripts)
                {
                    flags |= RegionOptionFlags.DisableScripts;
                }
                if (DisablePhysics)
                {
                    flags |= RegionOptionFlags.DisablePhysics;
                }
                if (BlockFly)
                {
                    flags |= RegionOptionFlags.BlockFly;
                }
                if (RestrictPushing)
                {
                    flags |= RegionOptionFlags.RestrictPushObject;
                }
                if (AllowLandJoinDivide)
                {
                    flags |= RegionOptionFlags.AllowParcelChanges;
                }
                if (BlockShowInSearch)
                {
                    flags |= RegionOptionFlags.BlockParcelSearch;
                }
                if (Sandbox)
                {
                    flags |= RegionOptionFlags.Sandbox;
                }
                if (IsSunFixed)
                {
                    flags |= RegionOptionFlags.SunFixed;
                }

                return flags;
            }
        }
    }
}
