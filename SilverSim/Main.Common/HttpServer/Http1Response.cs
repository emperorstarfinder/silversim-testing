﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Http;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace SilverSim.Main.Common.HttpServer
{
    public sealed class Http1Response : HttpResponse
    {
        readonly Stream m_Output;
        private bool m_IsHeaderSent;
        private Stream ResponseBody;
        readonly bool IsChunkedAccepted;

        [SuppressMessage("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule")]
        public Http1Response(Stream output, HttpRequest request, HttpStatusCode statusCode, string statusDescription)
            : base(request, statusCode, statusDescription)
        {
            Headers["Content-Type"] = "text/html";
            m_Output = output;
            MajorVersion = request.MajorVersion;
            MinorVersion = request.MinorVersion;
            IsCloseConnection = HttpConnectionMode.Close == request.ConnectionMode;
            StatusCode = statusCode;
            StatusDescription = statusDescription;
            IsChunkedAccepted = request.ContainsHeader("TE");
            if (MinorVersion >= 1 || MajorVersion > 1)
            {
                IsChunkedAccepted = true;
            }
        }

        protected override void SendHeaders()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (TextWriter w = ms.UTF8StreamWriter())
                {
                    w.Write(string.Format("HTTP/{0}.{1} {2} {3}\r\n", MajorVersion, MinorVersion, (uint)StatusCode, StatusDescription.Replace("\n", string.Empty).Replace("\r", string.Empty)));
                    foreach (KeyValuePair<string, string> kvp in Headers)
                    {
                        w.Write(string.Format("{0}: {1}\r\n", kvp.Key.Replace("\r", string.Empty).Replace("\n", string.Empty), kvp.Value.Replace("\r", string.Empty).Replace("\n", string.Empty)));
                    }
                    w.Write("\r\n");
                    w.Flush();
                    byte[] b = ms.ToArray();
                    m_Output.Write(b, 0, b.Length);
                    m_Output.Flush();
                }
            }
            m_IsHeaderSent = true;
        }

        public override void Close()
        {
            if (!m_IsHeaderSent)
            {
                Headers["Content-Length"] = "0";
                SendHeaders();
            }
            if (ResponseBody != null)
            {
                ResponseBody.Close();
                ResponseBody = null;
            }
            m_Output.Flush();

            if (IsCloseConnection)
            {
                throw new ConnectionCloseException();
            }
        }

        public override Stream GetOutputStream(long contentLength)
        {
            if (!m_IsHeaderSent)
            {
                Headers["Content-Length"] = contentLength.ToString();
                SendHeaders();
            }
            else
            {
                throw new InvalidOperationException();
            }

            return ResponseBody = new HttpResponseBodyStream(m_Output, contentLength);
        }

        public override Stream GetOutputStream(bool disableCompression = false)
        {
            bool gzipEnable = false;
            if (!m_IsHeaderSent)
            {
                IsCloseConnection = true;
                Headers["Connection"] = "close";
                Headers.Remove("Content-Length");
                if (!disableCompression && AcceptedEncodings != null && AcceptedEncodings.Contains("gzip"))
                {
                    gzipEnable = true;
                    Headers["Content-Encoding"] = "gzip";
                }
                SendHeaders();
            }
            else
            {
                throw new InvalidOperationException();
            }

            /* we never give out the original stream because Close is working recursively according to .NET specs */
            if (gzipEnable)
            {
                return new GZipStream(new HttpResponseBodyStream(m_Output), CompressionMode.Compress);
            }
            return new HttpResponseBodyStream(m_Output);
        }

        public override Stream GetChunkedOutputStream()
        {
            if (IsChunkedAccepted)
            {
                if (!m_IsHeaderSent)
                {
                    Headers["Transfer-Encoding"] = "chunked";
                    SendHeaders();
                }
                else
                {
                    throw new InvalidOperationException();
                }

                return ResponseBody = new HttpWriteChunkedBodyStream(m_Output);
            }
            else
            {
                return GetOutputStream();
            }
        }

        public override void Dispose()
        {
            if (null != ResponseBody)
            {
                ResponseBody.Dispose();
            }
            Close();
            if (null != m_Output)
            {
                m_Output.Dispose();
            }
        }
    }
}
