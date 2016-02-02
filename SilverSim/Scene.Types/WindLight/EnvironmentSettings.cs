﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace SilverSim.Scene.Types.WindLight
{
    [SuppressMessage("Gendarme.Rules.Concurrency", "DoNotLockOnThisOrTypesRule")]
    public class EnvironmentSettings
    {
        public readonly RwLockedList<KeyValuePair<double, string>> DayCycle = new RwLockedList<KeyValuePair<double, string>>();
        public readonly RwLockedDictionary<string, SkyEntry> SkySettings = new RwLockedDictionary<string, SkyEntry>();
        WaterEntry m_WaterSettings = new WaterEntry();
        readonly object m_WaterSettingsLock = new object();

        public WaterEntry WaterSettings
        {
            get
            {
                lock(m_WaterSettingsLock)
                {
                    return m_WaterSettings;
                }
            }
            set
            {
                lock(m_WaterSettingsLock)
                {
                    m_WaterSettings = value;
                }
            }
        }

        public EnvironmentSettings()
        {
        }

        public EnvironmentSettings(EnvironmentSettings env)
        {
            m_WaterSettings = env.WaterSettings;
            DayCycle = new RwLockedList<KeyValuePair<double, string>>(env.DayCycle);
            SkySettings = new RwLockedDictionary<string, SkyEntry>(env.SkySettings);
        }

        public void Serialize(Stream s, UUID regionID)
        {
            using(XmlTextWriter writer = s.UTF8XmlTextWriter())
            {
                Serialize(writer, regionID);
            }
        }

        public void Serialize(XmlTextWriter writer, UUID regionID)
        {
            writer.WriteStartElement("llsd");
            {
                writer.WriteStartElement("array");
                {
                    writer.WriteStartElement("map");
                    {
                        writer.WriteNamedValue("key", "messageID");
                        writer.WriteNamedValue("uuid", UUID.Zero);
                        writer.WriteNamedValue("key", "regionID");
                        writer.WriteNamedValue("uuid", regionID);
                    }
                    writer.WriteEndElement();

                    writer.WriteStartElement("array");
                    foreach(KeyValuePair<double, string> kvp in DayCycle)
                    {
                        writer.WriteNamedValue("real", kvp.Key);
                        writer.WriteNamedValue("string", kvp.Value);
                    }
                    writer.WriteEndElement();

                    writer.WriteStartElement("map");
                    foreach (KeyValuePair<string, SkyEntry> entry in SkySettings)
                    {
                        writer.WriteNamedValue("key", entry.Key);
                        entry.Value.Serialize(writer);
                    }
                    writer.WriteEndElement();

                    WaterSettings.Serialize(writer);
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        [Serializable]
        public class EnvironmentSettingsSerializationException : Exception
        {
            public EnvironmentSettingsSerializationException()
            {

            }

            public EnvironmentSettingsSerializationException(string message)
                : base(message)
            {

            }

            protected EnvironmentSettingsSerializationException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public EnvironmentSettingsSerializationException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }

        public static EnvironmentSettings Deserialize(Stream input)
        {
            EnvironmentSettings env = new EnvironmentSettings();
            AnArray a = LlsdXml.Deserialize(input) as AnArray;
            if(null == a)
            {
                throw new EnvironmentSettingsSerializationException();
            }

            AnArray dayCycleArray = a[1] as AnArray;
            Map skyArray = a[2] as Map;
            Map waterSettings = a[3] as Map;

            if (dayCycleArray != null && skyArray != null)
            {
                for (int i = 0; i < dayCycleArray.Count - 1; i += 2)
                {
                    env.DayCycle.Add(new KeyValuePair<double, string>(dayCycleArray[i + 0].AsReal, dayCycleArray[i + 1].ToString()));
                }

                foreach (KeyValuePair<string, IValue> kvp in skyArray)
                {
                    if (kvp.Value is Map)
                    {
                        env.SkySettings.Add(kvp.Key, new SkyEntry((Map)kvp.Value));
                    }
                }
            }

            if (waterSettings != null)
            {
                env.WaterSettings = new WaterEntry(waterSettings);
            }

            return env;
        }
    }
}
