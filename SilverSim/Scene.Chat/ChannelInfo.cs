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

using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Threading;
using SilverSim.Types;
using System;

namespace SilverSim.Scene.Chat
{
    internal sealed class ChannelInfo
    {
        public readonly RwLockedList<ChatServiceInterface.Listener> Listeners = new RwLockedList<ChatServiceInterface.Listener>();
        private readonly ChatHandler m_Handler;

        public ChannelInfo(ChatHandler handler)
        {
            m_Handler = handler;
        }

        private void SendToListener(ChatServiceInterface.Listener listener, ListenEvent ev, double maxDistanceSquared)
        {
            Func<UUID> getowner = listener.GetOwner;
            Func<UGI> getGroup = listener.GetGroup;
            if ((ev.GlobalPosition - listener.GetPosition()).LengthSquared > maxDistanceSquared &&
                !listener.IsIgnorePosition)
            {
                /* too far so no listening */
            }
            else if(listener.LimitToSameGroup && getGroup != null && ev.Group != null && ev.Group.Equals(getGroup()))
            {
                /* ignore group mismatch */
            }
            else if(listener.LimitToSameOwner && getowner != null && getowner() != ev.OwnerID)
            {
                /* ignore owner mismatch */
            }
            else if (ev.TargetID == UUID.Zero || 
                    ev.TargetID == listener.GetUUID())
            {
                listener.Send(ev);
            }
        }

        public void Send(ListenEvent ev)
        {
            double sayDistanceSquared = m_Handler.SayDistance * m_Handler.SayDistance;
            double shoutDistanceSquared = m_Handler.ShoutDistance * m_Handler.ShoutDistance;
            double whisperDistanceSquared = m_Handler.WhisperDistance * m_Handler.WhisperDistance;

            foreach(ChatServiceInterface.Listener listener in Listeners)
            {
                try
                {
                    switch (ev.Type)
                    {
                        case ListenEvent.ChatType.Region:
                        case ListenEvent.ChatType.DebugChannel:
                        case ListenEvent.ChatType.Broadcast:
                            listener.Send(ev);
                            break;

                        case ListenEvent.ChatType.Say:
                            SendToListener(listener, ev, sayDistanceSquared);
                            break;

                        case ListenEvent.ChatType.Shout:
                            SendToListener(listener, ev, shoutDistanceSquared);
                            break;

                        case ListenEvent.ChatType.Whisper:
                            SendToListener(listener, ev, whisperDistanceSquared);
                            break;

                        case ListenEvent.ChatType.OwnerSay:
                            if (listener.GetUUID().Equals(ev.TargetID))
                            {
                                listener.Send(ev);
                            }
                            break;

                        case ListenEvent.ChatType.StartTyping:
                        case ListenEvent.ChatType.StopTyping:
                            if (!listener.IsAgent)
                            {
                                /* listener is not an agent, so no typing messages */
                            }
                            else
                            {
                                SendToListener(listener, ev, sayDistanceSquared);
                            }
                            break;

                        default:
                            break;
                    }
                }
                catch
                {
                    /* ignore in the rare case that an object is part died */
                }
            }
        }
    }
}
