﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Script
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public sealed class ScriptApiName : Attribute
    {
        public string Name { get; private set; }

        public ScriptApiName(string name)
        {
            Name = name;
        }
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public sealed class ScriptEngineUsage : Attribute
    {
        public string Name { get; private set; }

        public ScriptEngineUsage(string name)
        {
            Name = name;
        }
    }

    public interface IScriptApi
    {
    }
}
