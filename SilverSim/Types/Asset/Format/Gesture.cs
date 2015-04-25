﻿/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SilverSim.Types.Asset.Format
{
    public class Gesture : IReferencesAccessor
    {
        public enum StepType : int
        {
            Animation = 0,
            Sound = 1,
            Chat = 2,
            Wait = 3,
            EndOfGesture = 4
        };

        #region Steps
        public interface Step
        {
            StepType Type { get; }
            string Serialize();
        }

        public class StepAnimation : Step
        {
            public StepType Type
            {
                get
                {
                    return StepType.Animation;
                }
            }

            public bool AnimationStart = true;
            public UUID AssetID = UUID.Zero;
            public string Name;

            public string Serialize()
            {
                return string.Format("{0}\n{1}\n{2}\n{3}\n",
                    (int)Type, Name, AssetID, AnimationStart ? "1" : "0");
            }

            public StepAnimation()
            {

            }

            public StepAnimation(string name, UUID assetID, bool animStart)
            {
                Name = name;
                AssetID = assetID;
                AnimationStart = animStart;
            }
        }

        public class StepSound : Step
        {
            public StepType Type
            {
                get
                {
                    return StepType.Sound;
                }
            }

            public UUID AssetID = UUID.Zero;
            public string Name;

            public string Serialize()
            {
                return string.Format("{0}\n{1}\n{2}\n",
                    (int)Type, Name, AssetID);
            }

            public StepSound()
            {

            }

            public StepSound(string name, UUID assetID)
            {
                Name = name;
                AssetID = assetID;
            }
        }

        public class StepChat : Step
        {
            public StepType Type
            {
                get
                {
                    return StepType.Chat;
                }
            }

            public string Text;

            public string Serialize()
            {
                return string.Format("{0}\n{1}\n0\n",
                    (int)Type, Text);
            }

            public StepChat()
            {

            }

            public StepChat(string text)
            {
                Text = text;
            }
        }

        public class StepWait : Step
        {
            public StepType Type
            {
                get
                {
                    return StepType.Wait;
                }
            }

            public bool WaitForAnimation;
            public bool WaitForTime;
            public float WaitTime;


            public string Serialize()
            {
                int waitFlags = 0;
                if(WaitForTime)
                {
                    waitFlags |= 0x01;
                }
                if(WaitForAnimation)
                {
                    waitFlags |= 0x02;
                }
                return string.Format(CultureInfo.InvariantCulture, "{0}\n{1:0.000000}\n{2}\n",
                    (int)Type, WaitTime, waitFlags);
            }

            public StepWait()
            {

            }

            public StepWait(float waitTime, bool waitForAnim, bool waitForTime)
            {
                WaitTime = waitTime;
                WaitForAnimation = waitForAnim;
                WaitForTime = waitForTime;
            }
        }

        public class StepEndOfGesture : Step
        {
            public StepType Type
            {
                get
                {
                    return StepType.EndOfGesture;
                }
            }

            public string Serialize()
            {
                return ((int)Type).ToString();
            }

            public StepEndOfGesture()
            {

            }
        }

        #endregion

        #region Fields
        public byte TriggerKey;
        public uint TriggerKeyMask;
        public string Trigger;
        public string ReplaceWith;
        public List<Step> Sequence = new List<Step>();
        #endregion

        #region Constructors
        public Gesture()
        {

        }

        public Gesture(AssetData asset)
        {
            string input = UTF8NoBOM.GetString(asset.Data);
            input = input.Replace('\t', ' ');
            List<string> lines = new List<string>(input.Split('\n'));
            List<string>.Enumerator e = lines.GetEnumerator();
            if(!e.MoveNext())
            {
                throw new NotAGestureFormat();
            }

            if(int.Parse(e.Current) != 2)
            {
                throw new NotAGestureFormat();
            }

            if (!e.MoveNext())
            {
                throw new NotAGestureFormat();
            }
            TriggerKey = byte.Parse(e.Current);

            if (!e.MoveNext())
            {
                throw new NotAGestureFormat();
            }
            TriggerKeyMask = byte.Parse(e.Current);

            if (!e.MoveNext())
            {
                throw new NotAGestureFormat();
            }
            Trigger = e.Current;

            if (!e.MoveNext())
            {
                throw new NotAGestureFormat();
            }
            ReplaceWith = e.Current;

            if (!e.MoveNext())
            {
                throw new NotAGestureFormat();
            }
            int count = int.Parse(e.Current);

            if(count < 0)
            {
                throw new NotAGestureFormat();
            }

            for(int idx = 0; idx < count; ++idx)
            {
                if (!e.MoveNext())
                {
                    throw new NotAGestureFormat();
                }
                StepType type = (StepType)int.Parse(e.Current);

                switch(type)
                {
                    case StepType.EndOfGesture:
                        Sequence.Add(new StepEndOfGesture());
                        return;

                    case StepType.Animation:
                        {
                            StepAnimation step = new StepAnimation();
                            if (e.MoveNext())
                            {
                                step.Name = e.Current;
                            }
                            if (e.MoveNext())
                            {
                                step.AssetID = UUID.Parse(e.Current);
                            }
                            if (e.MoveNext())
                            {
                                step.AnimationStart = int.Parse(e.Current) != 0;
                            }
                            Sequence.Add(step);
                        }
                        break;

                    case StepType.Sound:
                        {
                            StepSound step = new StepSound();
                            if (e.MoveNext())
                            {
                                step.Name = e.Current;
                            }
                            if (e.MoveNext())
                            {
                                step.AssetID = UUID.Parse(e.Current);
                            }
                            Sequence.Add(step);
                        }
                        break;

                    case StepType.Chat:
                        {
                            StepChat step = new StepChat();
                            if (e.MoveNext())
                            {
                                step.Text = e.Current;
                            }
                            e.MoveNext();
                            Sequence.Add(step);
                        }
                        break;

                    case StepType.Wait:
                        {
                            StepWait step = new StepWait();
                            if (e.MoveNext())
                            {
                                step.WaitTime = float.Parse(e.Current, CultureInfo.InvariantCulture);
                            }
                            if (e.MoveNext())
                            {
                                int flags = int.Parse(e.Current);
                                step.WaitForTime = (flags & 0x01) != 0;
                                step.WaitForAnimation = (flags & 0x02) != 0;
                            }
                            Sequence.Add(step);
                        }
                        break;

                    default:
                        throw new NotAGestureFormat();
                }
            }
        }
        #endregion

        #region References interface
        public List<UUID> References
        {
            get
            {
                List<UUID> refs = new List<UUID>();
                foreach(Step step in Sequence)
                {
                    if(step is StepSound)
                    {
                        if (!refs.Contains(((StepSound)step).AssetID))
                        {
                            refs.Add(((StepSound)step).AssetID);
                        }
                    }
                    else if(step is StepAnimation)
                    {
                        if (!refs.Contains(((StepAnimation)step).AssetID))
                        {
                            refs.Add(((StepAnimation)step).AssetID);
                        }
                    }
                }
                return refs;
            }
        }
        #endregion

        #region Operators

        public AssetData Asset()
        {
            return (AssetData)this;
        }

        public static implicit operator AssetData(Gesture v)
        {
            AssetData asset = new AssetData();
            StringBuilder sb = new StringBuilder();
            sb.Append("2\n");
            sb.Append(v.TriggerKey + "\n");
            sb.Append(v.TriggerKeyMask + "\n");
            sb.Append(v.Trigger + "\n");
            sb.Append(v.ReplaceWith + "\n");

            int count = 0;

            if(v.Sequence != null)
            {
                count = v.Sequence.Count;
                sb.Append(count + "\n");
            }

            for (int i = 0; i < count; ++i)
            {
                Step s = v.Sequence[i];
                sb.Append(s.Serialize());
            }

            asset.Data = UTF8NoBOM.GetBytes(sb.ToString());
            asset.Type = AssetType.Gesture;
            asset.Name = "Gesture";
            return asset;
        }

        static UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);
        #endregion
    }
}
