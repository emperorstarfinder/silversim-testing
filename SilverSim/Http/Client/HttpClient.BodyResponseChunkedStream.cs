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

namespace SilverSim.Http.Client
{
    public static partial class HttpClient
    {
        public sealed class BodyResponseChunkedStream : HttpReadChunkedBodyStream
        {
            private readonly string m_Scheme;
            private readonly string m_Host;
            private readonly int m_Port;

            public BodyResponseChunkedStream(AbstractHttpStream input, string scheme, string host, int port)
                : base(input)
            {
                m_Scheme = scheme;
                m_Host = host;
                m_Port = port;
            }

            protected override void ReachedEndOfChunked()
            {
                AddStreamForNextRequest((AbstractHttpStream)m_Input, m_Scheme, m_Host, m_Port);
                m_Input = null;
            }
        }
    }
}