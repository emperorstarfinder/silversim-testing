﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;

namespace SilverSim.Http.Client
{
    public static partial class HttpClient
    {
        private static string BuildQueryString(IDictionary<string, string> parameters)
        {
            StringBuilder outStr = new StringBuilder();
            foreach(KeyValuePair<string, string> kvp in parameters)
            {
                if(outStr.Length != 0)
                {
                    outStr.Append("&");
                }

                string[] names = kvp.Key.Split('?');
                outStr.Append(HttpUtility.UrlEncode(names[0]));
                outStr.Append("=");
                outStr.Append(HttpUtility.UrlEncode(kvp.Value));
            }

            return outStr.ToString();
        }

        #region Synchronous calls
        /**********************************************************************/
        /* synchronous calls */
        public static Stream DoStreamGetRequest(string url, IDictionary<string, string> getValues, int timeoutms)
        {
            return DoStreamRequest("GET", url, getValues, string.Empty, string.Empty, false, timeoutms);
        }

        /*---------------------------------------------------------------------*/
        public static Stream DoStreamHeadRequest(string url, IDictionary<string, string> getvalues, int timeoutms)
        {
            return DoStreamRequest("HEAD", url, getvalues, string.Empty, string.Empty, false, timeoutms);
        }

        /*---------------------------------------------------------------------*/
        public static Stream DoStreamPostRequest(string url, IDictionary<string, string> getValues, IDictionary<string, string> postValues, bool compressed, int timeoutms)
        {
            string post = BuildQueryString(postValues);
            return DoStreamRequest("POST", url, getValues, "application/x-www-form-urlencoded", post, compressed, timeoutms);
        }

        /**********************************************************************/
        /* synchronous calls */
        public static string DoGetRequest(string url, IDictionary<string, string> getValues, int timeoutms)
        {
            return DoRequest("GET", url, getValues, string.Empty, string.Empty, false, timeoutms);
        }

        /*---------------------------------------------------------------------*/
        public static string DoHeadRequest(string url, IDictionary<string, string> getvalues, int timeoutms)
        {
            return DoRequest("HEAD", url, getvalues, string.Empty, string.Empty, false, timeoutms);
        }

        /*---------------------------------------------------------------------*/
        public static string DoPostRequest(string url, IDictionary<string, string> getValues, IDictionary<string, string> postValues, bool compressed, int timeoutms)
        {
            string post = BuildQueryString(postValues);
            return DoRequest("POST", url, getValues, "application/x-www-form-urlencoded", post, compressed, timeoutms);
        }

        /*---------------------------------------------------------------------*/
        public static string DoRequest(string method, string url, IDictionary<string, string> getValues, string content_type, string post, bool compressed, int timeoutms)
        {
            using (Stream responseStream = DoStreamRequest(method, url, getValues, content_type, post, compressed, timeoutms))
            {
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /*---------------------------------------------------------------------*/
        public static string DoRequest(string method, string url, IDictionary<string, string> getValues, string content_type, int content_length, Action<Stream> postdelegate, bool compressed, int timeoutms)
        {
            using (Stream responseStream = DoStreamRequest(method, url, getValues, content_type, content_length, postdelegate, compressed, timeoutms))
            {
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        #endregion
    }
}