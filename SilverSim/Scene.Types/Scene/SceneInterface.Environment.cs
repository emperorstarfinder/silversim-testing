﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.WindLight;
using SilverSim.Types;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Generic;
using SilverSim.Viewer.Messages.LayerData;
using SilverSim.Viewer.Messages.Region;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        public EnvironmentController Environment;

        EnvironmentSettings m_EnvironmentSettings;

        public EnvironmentSettings EnvironmentSettings
        {
            get
            {
                EnvironmentSettings envSettings = m_EnvironmentSettings;
                if (envSettings == null)
                {
                    return null;
                }
                return new EnvironmentSettings(envSettings);
            }
            set
            {
                m_EnvironmentSettings = (null != value) ?
                    new EnvironmentSettings(m_EnvironmentSettings) :
                    null;
            }
        }

        public class EnvironmentController
        {
            private const int BASE_REGION_SIZE = 256;

            public struct WLVector4
            {
                public double X;
                public double Y;
                public double Z;
                public double W;

                public WLVector4(Quaternion q)
                {
                    X = q.X;
                    Y = q.Y;
                    Z = q.Z;
                    W = q.Z;
                }

                public static implicit operator Quaternion(WLVector4 v)
                {
                    return new Quaternion(v.X, v.Y, v.Z, v.W);
                }
            }

            [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
            public struct WindlightSkyData
            {
                public WLVector4 Ambient;
                public WLVector4 CloudColor;
                public double CloudCoverage;
                public WLVector4 BlueDensity;
                public Vector3 CloudDetailXYDensity;
                public double CloudScale;
                public double CloudScrollX;
                public bool CloudScrollXLock;
                public double CloudScrollY;
                public bool CloudScrollYLock;
                public Vector3 CloudXYDensity;
                public double DensityMultiplier;
                public double DistanceMultiplier;
                public bool DrawClassicClouds;
                public double EastAngle;
                public double HazeDensity;
                public double HazeHorizon;
                public WLVector4 Horizon;
                public int MaxAltitude;
                public double SceneGamma;
                public double StarBrightness;
                public double SunGlowFocus;
                public double SunGlowSize;
                public WLVector4 SunMoonColor;
                public double SunMoonPosition;
            }

            [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
            public struct WindlightWaterData
            {
                public Vector3 BigWaveDirection;
                public Vector3 LittleWaveDirection;
                public double BlurMultiplier;
                public double FresnelScale;
                public double FresnelOffset;
                public UUID NormalMapTexture;
                public Vector3 ReflectionWaveletScale;
                public double RefractScaleAbove;
                public double RefractScaleBelow;
                public double UnderwaterFogModifier;
                public Color Color;
                public double FogDensityExponent;
            }

            [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
            public struct SunData
            {
                public UInt64 UsecSinceStart;
                public UInt32 SecPerDay;
                public UInt32 SecPerYear;
                public Vector3 SunDirection;
                public double SunPhase;
                public Vector3 SunAngVelocity;
                public bool IsSunFixed;
            }

            public IWindModel Wind
            {
                get; private set;
            }

            bool m_WindlightValid;
            WindlightSkyData m_SkyWindlight = new WindlightSkyData();
            WindlightWaterData m_WaterWindlight = new WindlightWaterData();
            SunData m_SunData = new SunData();
            readonly SceneInterface m_Scene;
            readonly System.Timers.Timer m_Timer = new System.Timers.Timer(10000);

            public Vector3 SunDirection
            {
                get
                {
                    lock(this)
                    {
                        return m_SunData.SunDirection;
                    }
                }
                set
                {
                    lock(this)
                    {
                        m_SunData.SunDirection = value;
                    }
                }
            }

            public EnvironmentController(SceneInterface scene)
            {
                m_Scene = scene;
                Wind = new NoWindModel();
                m_SunData.SunDirection = new Vector3();
                m_SunData.SecPerDay = 4 * 60 * 60;
                m_SunData.SecPerYear = 11 * m_SunData.SecPerDay;
            }

            public void Start()
            {
                lock(this)
                {
                    if(!m_Timer.Enabled)
                    {
                        m_Timer.Elapsed += EnvironmentTimer;
                        m_Timer.Start();
                    }
                }
            }

            public void Stop()
            {
                lock(this)
                {
                    if(m_Timer.Enabled)
                    {
                        m_Timer.Stop();
                        m_Timer.Elapsed -= EnvironmentTimer;
                    }
                }
            }

            private void EnvironmentTimer(object sender, System.Timers.ElapsedEventArgs e)
            {
                UpdateSunDirection();
                if(null != Wind)
                {
                    Wind.UpdateModel(m_SunData);
                }
            }

            #region Update of sun direction
            /* source of algorithm is secondlifescripters mailing list */
            double AverageSunTilt = -0.25 * Math.PI;
            double SeasonalSunTilt = 0.03 * Math.PI;
            double SunNormalizedOffset = 0.45;

            public void UpdateSunDirection()
            {
                double DailyOmega;
                double YearlyOmega;
                lock (this)
                {
                    DailyOmega = 2 / m_SunData.SecPerDay;
                    YearlyOmega = 2 / (m_SunData.SecPerYear);
                }
                ulong utctime = Date.GetUnixTime();
                bool sunFixed = m_SunData.IsSunFixed;
                if(sunFixed)
                {
                    utctime = 0;
                }

                double daily_phase = DailyOmega * utctime;
                double sun_phase = daily_phase % (2 * Math.PI);
                double yearly_phase = YearlyOmega * utctime;
                double tilt = AverageSunTilt + SeasonalSunTilt * Math.Sin(yearly_phase);

                Vector3 sunDirection = new Vector3(Math.Cos(-sun_phase), Math.Sin(-sun_phase), 0);
                Quaternion tiltRot = new Quaternion(tilt, 1, 0, 0);

                sunDirection *= tiltRot;
                Vector3 sunVelocity = new Vector3(0, 0, DailyOmega);
                if(sunFixed)
                {
                    sunVelocity = Vector3.Zero;
                }
                sunVelocity *= tiltRot;
                sunDirection.Z += SunNormalizedOffset;
                double radius = sunDirection.Length;
                sunDirection = sunDirection.Normalize();
                sunVelocity *= (1 / radius);
                lock (this)
                {
                    m_SunData.SunDirection = sunDirection;
                    m_SunData.SunAngVelocity = sunVelocity;
                    m_SunData.UsecSinceStart = utctime * 1000000;
                }
            }
            #endregion

            #region Update of Wind Data
            private List<LayerData> CompileWindData(Vector3 basepos)
            {
                List<LayerData> mlist = new List<LayerData>();
                List<LayerPatch> patchesList = new List<LayerPatch>();
                LayerPatch patchX = new LayerPatch();
                LayerPatch patchY = new LayerPatch();

                /* round to nearest low pos */
                bool rX = basepos.X % 256 >= 128;
                bool rY = basepos.Y % 256 >= 128;
                basepos.X = Math.Floor(basepos.X / 256) * 256;
                basepos.Y = Math.Floor(basepos.Y / 256) * 256;

                for (int y = 0; y < 16; ++y)
                {
                    for(int x = 0; x < 16; ++x)
                    {
                        Vector3 actpos = basepos;
                        actpos.X += x * 4;
                        actpos.Y += y * 4;
                        if(rX && x < 8)
                        {
                            actpos.X += 128;
                        }
                        if (rY && y < 8)
                        {
                            actpos.Y += 128;
                        }
                        Vector3 w = Wind[actpos];
                        patchX[x, y] = (float)w.X;
                        patchY[x, y] = (float)w.Y;
                    }
                }

                patchesList.Add(patchX);
                patchesList.Add(patchY);

                LayerData.LayerDataType layerType = LayerData.LayerDataType.Wind;

                if (BASE_REGION_SIZE < m_Scene.RegionData.Size.X || BASE_REGION_SIZE < m_Scene.RegionData.Size.Y)
                {
                    layerType = LayerData.LayerDataType.WindExtended;
                }
                int offset = 0;
                while (offset < patchesList.Count)
                {
                    int remaining = Math.Min(patchesList.Count - offset, LayerCompressor.MESSAGES_PER_WIND_LAYER_PACKET);
                    int actualused;
                    mlist.Add(LayerCompressor.ToLayerMessage(patchesList, layerType, offset, remaining, out actualused));
                    offset += actualused;
                }
                return mlist;
            }

            public void UpdateWindDataToSingleClient(IAgent agent)
            {
                List<LayerData> mlist = CompileWindData(agent.GlobalPosition);
                foreach (LayerData m in mlist)
                {
                    agent.SendMessageAlways(m, m_Scene.ID);
                }
            }

            private void UpdateWindDataToClients()
            {
                foreach (IAgent agent in m_Scene.Agents)
                {
                    List<LayerData> mlist = CompileWindData(agent.GlobalPosition);
                    foreach (LayerData m in mlist)
                    {
                        agent.SendMessageAlways(m, m_Scene.ID);
                    }
                }
            }
            #endregion

            #region Update of Windlight Data
            private void UpdateWindlightProfileToClients()
            {
                GenericMessage m;

                m = (m_WindlightValid) ?
                    CompileWindlightSettings(m_SkyWindlight, m_WaterWindlight) :
                    CompileResetWindlightSettings();

                SendToAllClients(m);
            }

            public void UpdateWindlightProfileToClient(IAgent agent)
            {
                GenericMessage m;

                m = (m_WindlightValid) ?
                    CompileWindlightSettings(m_SkyWindlight, m_WaterWindlight) :
                    CompileResetWindlightSettings();

                agent.SendMessageAlways(m, m_Scene.ID);
            }
            #endregion

            #region Viewer time message update
            private void SendSimulatorTimeMessageToAllClients()
            {
                SimulatorViewerTimeMessage m = new SimulatorViewerTimeMessage();
                m.SunPhase = m_SunData.SunPhase;
                m.UsecSinceStart = m_SunData.UsecSinceStart;
                m.SunDirection = m_SunData.SunDirection;
                m.SunAngVelocity = m_SunData.SunAngVelocity;
                m.SecPerYear = m_SunData.SecPerYear;
                m.SecPerDay = m_SunData.SecPerDay;
                SendToAllClients(m);
            }

            private void SendSimulatorTimeMessageToClient(IAgent agent)
            {
                SimulatorViewerTimeMessage m = new SimulatorViewerTimeMessage();
                m.SunPhase = m_SunData.SunPhase;
                m.UsecSinceStart = m_SunData.UsecSinceStart;
                m.SunDirection = m_SunData.SunDirection;
                m.SunAngVelocity = m_SunData.SunAngVelocity;
                m.SecPerYear = m_SunData.SecPerYear;
                m.SecPerDay = m_SunData.SecPerDay;
                agent.SendMessageAlways(m, m_Scene.ID);
            }
            #endregion

            private void SendToAllClients(Message m)
            {
                foreach (IAgent agent in m_Scene.Agents)
                {
                    agent.SendMessageAlways(m, m_Scene.ID);
                }
            }

            #region Windlight message compiler
            private GenericMessage CompileResetWindlightSettings()
            {
                GenericMessage m = new GenericMessage();
                m.Method = "WindlightReset";
                m.ParamList.Add(new byte[0]);
                return m;
            }

            private GenericMessage CompileWindlightSettings(WindlightSkyData skyWindlight, WindlightWaterData waterWindlight)
            {
                GenericMessage m = new GenericMessage();
                m.Method = "Windlight";
                byte[] mBlock = new byte[249];
                int pos = 0;
                AddToCompiledWL(waterWindlight.Color, ref mBlock, ref pos);
                AddToCompiledWL(waterWindlight.FogDensityExponent, ref mBlock, ref pos);
                AddToCompiledWL(waterWindlight.UnderwaterFogModifier, ref mBlock, ref pos);
                AddToCompiledWL(waterWindlight.ReflectionWaveletScale, ref mBlock, ref pos);
                AddToCompiledWL(waterWindlight.FresnelScale, ref mBlock, ref pos);
                AddToCompiledWL(waterWindlight.FresnelOffset, ref mBlock, ref pos);
                AddToCompiledWL(waterWindlight.RefractScaleAbove, ref mBlock, ref pos);
                AddToCompiledWL(waterWindlight.RefractScaleBelow, ref mBlock, ref pos);
                AddToCompiledWL(waterWindlight.BlurMultiplier, ref mBlock, ref pos);
                AddToCompiledWL(waterWindlight.BigWaveDirection, ref mBlock, ref pos);
                AddToCompiledWL(waterWindlight.LittleWaveDirection, ref mBlock, ref pos);
                AddToCompiledWL(waterWindlight.NormalMapTexture, ref mBlock, ref pos);

                AddToCompiledWL(skyWindlight.Horizon, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.HazeHorizon, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.BlueDensity, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.HazeDensity, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.DensityMultiplier, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.DistanceMultiplier, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.SunMoonColor, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.SunMoonPosition, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.Ambient, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.EastAngle, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.SunGlowFocus, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.SunGlowSize, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.SceneGamma, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.StarBrightness, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.CloudColor, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.CloudXYDensity, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.CloudCoverage, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.CloudScale, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.CloudDetailXYDensity, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.CloudScrollX, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.CloudScrollY, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.MaxAltitude, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.CloudScrollXLock, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.CloudScrollYLock, ref mBlock, ref pos);
                AddToCompiledWL(skyWindlight.DrawClassicClouds, ref mBlock, ref pos);
                m.ParamList.Add(mBlock);
                return m;
            }

            private void AddToCompiledWL(bool v, ref byte[] mBlock, ref int pos)
            {
                mBlock[pos++] = v ?
                    (byte)1 :
                    (byte)0;
            }

            private void AddToCompiledWL(Vector3 v, ref byte[] mBlock, ref int pos)
            {
                AddToCompiledWL(v.X, ref mBlock, ref pos);
                AddToCompiledWL(v.Y, ref mBlock, ref pos);
                AddToCompiledWL(v.Z, ref mBlock, ref pos);
            }

            private void AddToCompiledWL(WLVector4 v, ref byte[] mBlock, ref int pos)
            {
                AddToCompiledWL(v.X, ref mBlock, ref pos);
                AddToCompiledWL(v.Y, ref mBlock, ref pos);
                AddToCompiledWL(v.Z, ref mBlock, ref pos);
                AddToCompiledWL(v.W, ref mBlock, ref pos);
            }

            private void AddToCompiledWL(Color v, ref byte[] mBlock, ref int pos)
            {
                AddToCompiledWL(v.R, ref mBlock, ref pos);
                AddToCompiledWL(v.G, ref mBlock, ref pos);
                AddToCompiledWL(v.B, ref mBlock, ref pos);
            }

            private void AddToCompiledWL(UUID v, ref byte[] mBlock, ref int pos)
            {
                v.ToBytes(mBlock, pos);
                pos += 16;
            }

            private void AddToCompiledWL(double v, ref byte[] mBlock, ref int pos)
            {
                byte[] b = BitConverter.GetBytes((float)v);
                if(!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(b);
                }
                Buffer.BlockCopy(b, 0, mBlock, pos, b.Length);
                pos += b.Length;
            }

            private void AddToCompiledWL(int v, ref byte[] mBlock, ref int pos)
            {
                byte[] b = BitConverter.GetBytes(v);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(b);
                }
                Buffer.BlockCopy(b, 0, mBlock, pos, b.Length);
                pos += b.Length;
            }
            #endregion
        }
    }
}
