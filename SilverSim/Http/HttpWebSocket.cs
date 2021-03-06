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
using System;
using System.IO;

namespace SilverSim.Http
{
    [Serializable]
    public class WebSocketClosedException : Exception
    {
        public WebSocketClosedException()
        {
        }

        public WebSocketClosedException(string message) : base(message)
        {
        }

        public WebSocketClosedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected WebSocketClosedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }

    public class HttpWebSocket : IDisposable
    {
        public enum CloseReason
        {
            NormalClosure = 1000,
            GoingAway = 1001,
            ProtocolError = 1002,
            UnsupportedData = 1003,
            NoStatusReceived = 1005,
            InvalidFramePayloadData = 1007,
            PolicyViolation = 1008,
            MessageTooBig = 1009,
            MandatoryExtension = 1010,
            InternalError = 1011,
            ServiceRestart = 1012,
            TryAgainLater = 1013,
            BadGateway = 1014,
        }

        [Serializable]
        public class MessageTimeoutException : Exception
        {
            public MessageTimeoutException()
            {
            }

            public MessageTimeoutException(string message) : base(message)
            {
            }

            public MessageTimeoutException(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected MessageTimeoutException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
            {
            }
        }

        private static readonly Random Random = new Random();
        private static byte[] MaskingKey
        {
            get
            {
                int mask;
                lock(Random)
                {
                    mask = Random.Next(int.MinValue, int.MaxValue);
                }
                return BitConverter.GetBytes(mask);
            }
        }

        protected readonly Stream m_WebSocketStream;
        private bool m_IsClosed;
        private readonly object m_SendLock = new object();
        private bool m_IsDisposed;
        private CloseReason m_CloseReason = CloseReason.NormalClosure;

        public HttpWebSocket(Stream o)
        {
            m_WebSocketStream = o;
        }

        public void Close(CloseReason reason = CloseReason.NormalClosure)
        {
            m_CloseReason = reason;
            Dispose();
        }

        public void Dispose()
        {
            if (!m_IsDisposed)
            {
                try
                {
                    SendClose(m_CloseReason);
                }
                catch
                {
                    /* intentionally ignore errors */
                }
            }
            m_WebSocketStream.Dispose();
            m_IsDisposed = true;
        }

        private enum OpCode
        {
            Continuation = 0,
            Text = 1,
            Binary = 2,
            Close = 8,
            Ping = 9,
            Pong = 10,
        }

        public enum MessageType
        {
            Text = 1,
            Binary = 2
        }

        public struct Message
        {
            public MessageType Type;
            public bool IsLastSegment;
            public byte[] Data;
        }

        public Message Receive()
        {
            m_WebSocketStream.ReadTimeout = 1000;
            for (;;)
            {
                var hdr = new byte[2];
                if (m_IsClosed)
                {
                    throw new WebSocketClosedException();
                }
                try
                {
                    if (2 != m_WebSocketStream.Read(hdr, 0, 2))
                    {
                        m_IsClosed = true;
                        throw new WebSocketClosedException();
                    }
                }
                catch(IOException)
                {
                    throw new MessageTimeoutException();
                }
                catch(HttpStream.TimeoutException)
                {
                    throw new MessageTimeoutException();
                }

                var opcode = (OpCode)(hdr[0] & 0xF);
                if (opcode == OpCode.Close)
                {
                    m_IsClosed = true;
                    throw new WebSocketClosedException();
                }
                int payloadlen = hdr[1] & 0x7F;
                if (payloadlen == 127)
                {
                    var leninfo = new byte[8];
                    if (8 != m_WebSocketStream.Read(leninfo, 0, 8))
                    {
                        m_IsClosed = true;
                        throw new WebSocketClosedException();
                    }
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(leninfo);
                    }
                    payloadlen = (int)BitConverter.ToUInt64(leninfo, 0);
                }
                else if (payloadlen == 126)
                {
                    var leninfo = new byte[2];
                    if (2 != m_WebSocketStream.Read(leninfo, 0, 2))
                    {
                        m_IsClosed = true;
                        throw new WebSocketClosedException();
                    }
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(leninfo);
                    }
                    payloadlen = BitConverter.ToUInt16(leninfo, 0);
                }
                else
                {
                    payloadlen = hdr[1] & 0x7F;
                }
                var maskingkey = new byte[4] { 0, 0, 0, 0 };
                if ((hdr[1] & 128) != 0 && 4 != m_WebSocketStream.Read(maskingkey, 0, 4))
                {
                    m_IsClosed = true;
                    throw new WebSocketClosedException();
                }

                var payload = new byte[payloadlen];
                if (payloadlen != m_WebSocketStream.Read(payload, 0, payloadlen))
                {
                    m_IsClosed = true;
                    throw new WebSocketClosedException();
                }
                for (int offset = 0; offset < payloadlen; ++offset)
                {
                    payload[offset] ^= maskingkey[offset % 4];
                }

                if (opcode == OpCode.Binary)
                {
                    return new Message
                    {
                        Data = payload,
                        Type = MessageType.Binary,
                        IsLastSegment = (hdr[0] & 128) != 0
                    };
                }
                else if (opcode == OpCode.Text)
                {
                    return new Message
                    {
                        Data = payload,
                        Type = MessageType.Text,
                        IsLastSegment = (hdr[0] & 128) != 0
                    };
                }
                else if (opcode == OpCode.Ping)
                {
                    SendFrame(OpCode.Pong, true, payload, 0, payload.Length, true);
                }
            }
        }

        public void WriteText(string text, bool fin = true, bool masked = false)
        {
            byte[] utf8 = text.ToUTF8Bytes();
            SendFrame(OpCode.Text, fin, utf8, 0, utf8.Length, masked);
        }

        public void WriteBinary(byte[] data, int offset, int length, bool fin = true, bool masked = false)
        {
            SendFrame(OpCode.Binary, fin, data, offset, length, masked);
        }

        private void SendClose(CloseReason reason)
        {
            byte[] res = BitConverter.GetBytes((ushort)reason);
            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(res);
            }
            SendFrame(OpCode.Close, true, res, 0, 2);
        }

        private void SendFrame(OpCode opcode, bool fin, byte[] payload, int offset, int length, bool masked = false)
        {
            lock (m_SendLock)
            {
                byte[] frame;
                byte[] maskingkey;

                if (offset < 0 || length < 0 || offset > payload.Length || offset + length > payload.Length || offset + length < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (length < 126)
                {
                    frame = new byte[2];
                    frame[1] = (byte)length;
                }
                else if (payload.Length < 65536)
                {
                    frame = new byte[4];
                    frame[1] = 126;
                    frame[2] = (byte)(payload.Length >> 8);
                    frame[3] = (byte)(payload.Length & 0xFF);
                }
                else
                {
                    frame = new byte[10];
                    frame[1] = 127;
                    frame[2] = 0;
                    frame[3] = 0;
                    frame[4] = 0;
                    frame[5] = 0;
                    frame[6] = (byte)((payload.Length >> 24) & 0xFF);
                    frame[7] = (byte)((payload.Length >> 16) & 0xFF);
                    frame[8] = (byte)((payload.Length >> 8) & 0xFF);
                    frame[9] = (byte)(payload.Length & 0xFF);
                }
                frame[0] = (byte)(int)opcode;
                if (fin)
                {
                    frame[0] |= 128;
                }
                if (masked)
                {
                    frame[1] |= 128;
                    maskingkey = MaskingKey;
                }
                else
                {
                    maskingkey = new byte[4] { 0, 0, 0, 0 };
                }
                m_WebSocketStream.Write(frame, 0, frame.Length);
                if (masked)
                {
                    m_WebSocketStream.Write(maskingkey, 0, maskingkey.Length);
                    byte[] maskedpayload = new byte[payload.Length];
                    for (int i = 0; i < maskedpayload.Length; ++i)
                    {
                        maskedpayload[i] = (byte)(payload[i + offset] ^ maskingkey[i % 4]);
                    }
                    m_WebSocketStream.Write(maskedpayload, 0, maskedpayload.Length);
                }
                else
                {
                    m_WebSocketStream.Write(payload, offset, length);
                }
            }
        }
    }
}
