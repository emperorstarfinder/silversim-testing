﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Scripting.LSL.API.Chat
{
    public partial class Chat_API
    {
        public static int MaxListenerHandles = 64;

        [APILevel(APIFlags.LSL)]
        public void llShout(ScriptInstance Instance, int channel, string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = channel;
            ev.Type = ListenEvent.ChatType.Shout;
            ev.Message = message;
            ev.SourceType = ListenEvent.ChatSourceType.Object;
            ev.OwnerID = getOwner(Instance);
            sendChat(Instance, ev);
        }

        [APILevel(APIFlags.LSL)]
        public void llSay(ScriptInstance Instance, int channel, string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = channel;
            ev.Type = ListenEvent.ChatType.Say;
            ev.Message = message;
            ev.SourceType = ListenEvent.ChatSourceType.Object;
            ev.OwnerID = getOwner(Instance);
            sendChat(Instance, ev);
        }

        [APILevel(APIFlags.LSL)]
        public void llWhisper(ScriptInstance Instance, int channel, string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = channel;
            ev.Type = ListenEvent.ChatType.Whisper;
            ev.Message = message;
            ev.SourceType = ListenEvent.ChatSourceType.Object;
            ev.OwnerID = getOwner(Instance);
            sendChat(Instance, ev);
        }

        [APILevel(APIFlags.LSL)]
        public void llOwnerSay(ScriptInstance Instance, string message)
        {
            lock (Instance)
            {
                ListenEvent ev = new ListenEvent();
                ev.Channel = PUBLIC_CHANNEL;
                ev.Type = ListenEvent.ChatType.OwnerSay;
                ev.Message = message;
                ev.TargetID = Instance.Part.ObjectGroup.Owner.ID;
                ev.SourceType = ListenEvent.ChatSourceType.Object;
                ev.OwnerID = getOwner(Instance);
                sendChat(Instance, ev);
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llRegionSay(ScriptInstance Instance, int channel, string message)
        {
            if (channel != PUBLIC_CHANNEL)
            {
                ListenEvent ev = new ListenEvent();
                ev.Type = ListenEvent.ChatType.Region;
                ev.Channel = channel;
                ev.Message = message;
                ev.OwnerID = getOwner(Instance);
                ev.SourceType = ListenEvent.ChatSourceType.Object;
                sendChat(Instance, ev);
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llRegionSayTo(ScriptInstance Instance, LSLKey target, int channel, string message)
        {
            ListenEvent ev = new ListenEvent();
            ev.Channel = channel;
            ev.Type = ListenEvent.ChatType.Region;
            ev.Message = message;
            ev.TargetID = target;
            ev.OwnerID = getOwner(Instance);
            ev.SourceType = ListenEvent.ChatSourceType.Object;
            sendChat(Instance, ev);
        }

        [APILevel(APIFlags.LSL)]
        public int llListen(ScriptInstance Instance, int channel, string name, LSLKey id, string msg)
        {
            Script script = (Script)Instance;
            lock (script)
            {
                if (script.m_Listeners.Count >= MaxListenerHandles)
                {
                    return new Integer(-1);
                }
                ChatServiceInterface chatservice = Instance.Part.ObjectGroup.Scene.GetService<ChatServiceInterface>();

                int newhandle = 0;
                ChatServiceInterface.Listener l;
                for (newhandle = 0; newhandle < MaxListenerHandles; ++newhandle)
                {
                    if (!script.m_Listeners.TryGetValue(newhandle, out l))
                    {
                        l = chatservice.AddListen(
                            channel,
                            name,
                            id,
                            msg,
                            delegate() { return Instance.Part.ID; },
                            delegate() { return Instance.Part.GlobalPosition; },
                            script.onListen);
                        try
                        {
                            script.m_Listeners.Add(newhandle, l);
                            return new Integer(newhandle);
                        }
                        catch
                        {
                            l.Remove();
                            return new Integer(-1);
                        }
                    }
                }
                return new Integer(-1);
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llListenRemove(ScriptInstance Instance, int handle)
        {
            Script script = (Script)Instance;
            ChatServiceInterface.Listener l;
            lock (script)
            {
                if (script.m_Listeners.Remove(handle, out l))
                {
                    l.Remove();
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llListenControl(ScriptInstance Instance, int handle, int active)
        {
            Script script = (Script)Instance;
            ChatServiceInterface.Listener l;
            lock (script)
            {
                if (script.m_Listeners.TryGetValue(handle, out l))
                {
                    l.IsActive = active != 0;
                }
            }
        }

        #region osListenRegex
        [APILevel(APIFlags.OSSL)]
        public int osListenRegex(ScriptInstance Instance, int channel, string name, LSLKey id, string msg, int regexBitfield)
        {
            Script script = (Script)Instance;
            lock (script)
            {
                if (script.m_Listeners.Count >= MaxListenerHandles)
                {
                    return -1;
                }
                ChatServiceInterface chatservice = Instance.Part.ObjectGroup.Scene.GetService<ChatServiceInterface>();

                int newhandle = 0;
                ChatServiceInterface.Listener l;
                for (newhandle = 0; newhandle < MaxListenerHandles; ++newhandle)
                {
                    if (!script.m_Listeners.TryGetValue(newhandle, out l))
                    {
                        l = chatservice.AddListenRegex(
                            channel,
                            name,
                            id,
                            msg,
                            regexBitfield,
                            delegate() { return Instance.Part.ID; },
                            delegate() { return Instance.Part.GlobalPosition; },
                            script.onListen);
                        try
                        {
                            script.m_Listeners.Add(newhandle, l);
                            return newhandle;
                        }
                        catch
                        {
                            l.Remove();
                            return -1;
                        }
                    }
                }
            }
            return -1;
        }
        #endregion

        [ExecutedOnStateChange]
        public static void ResetListeners(ScriptInstance Instance)
        {
            Script script = (Script)Instance;
            lock (script)
            {
                ICollection<ChatServiceInterface.Listener> coll = script.m_Listeners.Values;
                script.m_Listeners.Clear();
                foreach (ChatServiceInterface.Listener l in coll)
                {
                    l.Remove();
                }
            }
        }
    }
}
