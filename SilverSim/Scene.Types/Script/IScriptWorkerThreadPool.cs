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
using System.Runtime.Serialization;

namespace SilverSim.Scene.Types.Script
{
    [Serializable]
    public sealed class ScriptAbortException : Exception
    {
        public ScriptAbortException()
        {
        }

        public ScriptAbortException(string message)
            : base(message)
        {
        }

        private ScriptAbortException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public ScriptAbortException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public sealed class ScriptReportData
    {
        private double m_Score;
        private readonly object m_ScoreLock = new object();

        public double Score
        {
            get
            {
                lock(m_ScoreLock)
                {
                    return m_Score;
                }
            }
        }

        public void AddScore(double f)
        {
            lock(m_ScoreLock)
            {
                m_Score += f;
            }
        }
    }

    public struct ScriptInfo
    {
        public UUID PartID;
        public UUID ItemID;
        public UUID AssetID;
        public UUID ObjectID;
        public string PartName;
        public string ObjectName;
        public int LinkNumber;
        public string ItemName;
    }

    public interface IScriptWorkerThreadPool
    {
        void PostScript(ScriptInstance script);
        void AbortScript(ScriptInstance script);
        void Shutdown();
        void Sleep(int milliseconds);
        void Sleep(TimeSpan timespan);
        RwLockedDictionary<uint /* localids */, ScriptReportData> GetExecutionTimes();
        void IncrementScriptEventCounter();
        int ScriptEventCounter { get; }
        double ScriptEventsPerSec { get; }
        double ScriptTimeMsPerSec { get; }
        int ExecutingScripts { get; }

        int MinimumThreads { get; set; }
        int MaximumThreads { get; set; }
        IList<ScriptInfo> ExecutingScriptsList { get; }
    }
}
