﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.CmdIO;
using SilverSim.Main.Common.HttpServer;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Timers;
using ThreadedClasses;

namespace SilverSim.WebIF.Admin
{
    #region Service Implementation
    [Description("Administration Web-Interface")]
    public class AdminWebIF : IPlugin, IPluginShutdown, IPostLoadStep
    {
        private static readonly ILog m_Log = LogManager.GetLogger("ADMIN WEB IF");

        ServerParamServiceInterface m_ServerParams;
        BaseHttpServer m_HttpServer;
        BaseHttpServer m_HttpsServer;
        readonly string m_BasePath;
        const string JsonContentType = "application/json";

        class SessionInfo
        {
            public int LastSeenTickCount;
            public bool IsAuthenticated;
            public string UserName = string.Empty;
            public string ExpectedResponse = string.Empty;
            public List<string> Rights = new List<string>();

            public SessionInfo()
            {
                LastSeenTickCount = Environment.TickCount;
                IsAuthenticated = false;
            }
        }

        readonly RwLockedDictionary<string, SessionInfo> m_Sessions = new RwLockedDictionary<string, SessionInfo>();
        readonly Timer m_Timer = new Timer(1);

        #region Helpers
        public static void SuccessResponse(HttpRequest req, Map m)
        {
            m.Add("success", true);
            using (HttpResponse res = req.BeginResponse(JsonContentType))
            {
                using (Stream o = res.GetOutputStream())
                {
                    Json.Serialize(m, o);
                }
            }
        }

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class RequiredRightAttribute : Attribute
        {
            public string Right { get; private set; }

            public RequiredRightAttribute(string right)
            {
                Right = right;
            }
        }

        public enum ErrorResult
        {
            NotLoggedIn = 1,
            NotFound = 2,
            InsufficientRights = 3,
            InvalidRequest = 4,
            AlreadyExists = 5,
            NotPossible = 6,
            InUse = 7,
            MissingSessionId = 8,
            MissingMethod = 9,
            InvalidSession = 10,
            InvalidUserAndOrPassword = 11,
            UnknownMethod = 12,
            AlreadyStarted = 13,
            FailedToStart = 14,
            NotRunning = 15,
            IsRunning = 16
        };

        public static void ErrorResponse(HttpRequest req, ErrorResult reason)
        {
            Map m = new Map();
            m.Add("success", false);
            m.Add("reason", (int)reason);
            using (HttpResponse res = req.BeginResponse(JsonContentType))
            {
                using (Stream o = res.GetOutputStream())
                {
                    Json.Serialize(m, o);
                }
            }
        }
        #endregion

        public AdminWebIF(string basepath)
        {
            m_BasePath = basepath;
            m_Timer.Elapsed += HandleTimer;
            JsonMethods.Add("webif.admin.user.grantright", GrantRight);
            JsonMethods.Add("webif.admin.user.revokeright", RevokeRight);
            JsonMethods.Add("webif.admin.user.delete", DeleteUser);
            JsonMethods.Add("session.validate", HandleSessionValidateRequest);
        }

        public ShutdownOrder ShutdownOrder
        {
            get
            {
                return ShutdownOrder.Any;
            }
        }

        void HandleTimer(object o, EventArgs args)
        {
            List<string> removeList = new List<string>();
            foreach(KeyValuePair<string, SessionInfo> kvp in m_Sessions)
            {
                if(Environment.TickCount - kvp.Value.LastSeenTickCount > 1000 * 600) /* 10 minutes */
                {
                    removeList.Add(kvp.Key);
                }
            }

            foreach(string sessionKey in removeList)
            {
                m_Sessions.Remove(sessionKey);
            }
        }

        public readonly RwLockedDictionary<string, Action<HttpRequest, Map>> JsonMethods = new RwLockedDictionary<string, Action<HttpRequest, Map>>();

        const string AdminUserReference = "WebIF.Admin.User.admin.";

        #region Initialization
        public void Startup(ConfigurationLoader loader)
        {
            m_ServerParams = loader.GetServerParamStorage();
            m_HttpServer = loader.HttpServer;
            m_HttpServer.StartsWithUriHandlers.Add("/admin", HandleUnsecureHttp);
            try
            {
                m_HttpsServer = loader.HttpsServer;
            }
            catch(ConfigurationLoader.ServiceNotFoundException)
            {
                m_HttpsServer = null;
            }

            if(null != m_HttpsServer)
            {
                m_HttpsServer.StartsWithUriHandlers.Add("/admin", HandleHttp);
            }
            CommandRegistry.Commands.Add("admin-webif", AdminWebIFCmd);
        }

        public void PostLoad()
        {
            string res;
            if (!m_ServerParams.TryGetValue(UUID.Zero, AdminUserReference + "PassCode", out res))
            {
                res = UUID.Random.ToString();
                m_Log.InfoFormat("<<admin>> created password: {0}", res);
                using (SHA1 sha1 = SHA1.Create())
                {
                    byte[] str = UTF8NoBOM.GetBytes(res);

                    res = BitConverter.ToString(sha1.ComputeHash(str)).Replace("-", "").ToLower();
                }
                m_ServerParams[UUID.Zero, AdminUserReference + "PassCode"] = res;
                m_ServerParams[UUID.Zero, AdminUserReference + "Rights"] = "admin.all";
            }
        }

        public void Shutdown()
        {
            JsonMethods.Clear();
            m_HttpServer.StartsWithUriHandlers.Remove("/admin");
            if (null != m_HttpsServer)
            {
                m_HttpsServer.StartsWithUriHandlers.Remove("/admin");
            }
            m_Timer.Elapsed -= HandleTimer;
            m_Timer.Dispose();
        }
        #endregion

        #region User Logic
        void FindUser(SessionInfo sessionInfo, UUID challenge)
        {
            string userRef = "WebIF.Admin.User." + sessionInfo.UserName + ".";
            string pass_sha1;
            string rights;

            if (m_ServerParams.TryGetValue(UUID.Zero, userRef + "PassCode", out pass_sha1) &&
                m_ServerParams.TryGetValue(UUID.Zero, userRef + "Rights", out rights))
            {
                using (SHA1 sha1 = SHA1.Create())
                {
                    byte[] str = UTF8NoBOM.GetBytes(challenge.ToString().ToLower() + "+" + pass_sha1.ToLower());

                    sessionInfo.ExpectedResponse = BitConverter.ToString(sha1.ComputeHash(str)).Replace("-", "").ToLower();
                    sessionInfo.Rights = new List<string>(rights.ToLower().Split(','));
                }
            }
        }
        #endregion

        static readonly UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);

        #region Core Json API handler (login, challenge, logout + method lookup)
        public void HandleUnsecureHttp(HttpRequest req)
        {
            if(null == m_HttpsServer || m_ServerParams.GetBoolean(UUID.Zero, "WebIF.Admin.EnableHTTP", m_HttpsServer == null))
            {
                HandleHttp(req);
            }
            else
            {
                req.ErrorResponse(HttpStatusCode.Forbidden, "Not Allowed");
            }
        }

        void HandleSessionValidateRequest(HttpRequest req, Map jsonreq)
        {
            SuccessResponse(req, new Map());
        }

        void HandleLoginRequest(HttpRequest req, Map jsonreq)
        {
            if(!jsonreq.ContainsKey("sessionid") || !jsonreq.ContainsKey("user") || !jsonreq.ContainsKey("response"))
            {
                ErrorResponse(req, ErrorResult.InvalidRequest);
                return;
            }

            string sessionKey = req.CallerIP + "+" + jsonreq["sessionid"].ToString();
            SessionInfo sessionInfo;
            if(!m_Sessions.TryGetValue(sessionKey, out sessionInfo) || sessionInfo.IsAuthenticated)
            {
                ErrorResponse(req, ErrorResult.InvalidSession);
            }
            else if(sessionInfo.ExpectedResponse.Length == 0)
            {
                ErrorResponse(req, ErrorResult.InvalidUserAndOrPassword);
            }
            else
            {
                if(jsonreq["response"].ToString().ToLower() == sessionInfo.ExpectedResponse)
                {
                    sessionInfo.LastSeenTickCount = Environment.TickCount;
                    sessionInfo.IsAuthenticated = true;
                    sessionInfo.ExpectedResponse = string.Empty;
                    Map res = new Map();
                    AnArray rights = new AnArray();
                    foreach(string right in sessionInfo.Rights)
                    {
                        rights.Add(right);
                    }
                    res.Add("rights", rights);
                    res.Add("success", true);
                    using (HttpResponse httpres = req.BeginResponse(JsonContentType))
                    {
                        httpres.Headers["Set-Cookie"] = "sessionid=" + jsonreq["sessionid"].ToString() +";path=/admin";
                        using (Stream o = httpres.GetOutputStream())
                        {
                            Json.Serialize(res, o);
                        }
                    }
                }
                else
                {
                    m_Sessions.Remove(sessionKey);
                    ErrorResponse(req, ErrorResult.InvalidUserAndOrPassword);
                }
            }
        }

        void HandleChallengeRequest(HttpRequest req, Map jsonreq)
        {
            Map resdata = new Map();
            UUID sessionID = UUID.Random;
            UUID challenge = UUID.Random;
            resdata["sessionid"] = sessionID;
            resdata["challenge"] = challenge;

            if(!jsonreq.ContainsKey("user"))
            {
                ErrorResponse(req, ErrorResult.InvalidRequest);
                return;
            }

            SessionInfo sessionInfo = new SessionInfo();
            m_Sessions.Add(req.CallerIP + "+" + sessionID.ToString(), sessionInfo);
            sessionInfo.UserName = jsonreq["user"].ToString().ToLower();
            FindUser(sessionInfo, challenge);
            SuccessResponse(req, resdata);
        }

        public void HandleHttp(HttpRequest req)
        {
            if(req.RawUrl.StartsWith("/admin/json"))
            {
                if(req.Method != "POST")
                {
#if DEBUG
                    m_Log.DebugFormat("Method {0} not allowed to /admin/json", req.Method);
#endif
                    req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                }
                else if(req.ContentType != JsonContentType)
                {
#if DEBUG
                    m_Log.DebugFormat("Content-Type '{0}' not allowed to /admin/json", req.ContentType);
#endif
                    req.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported media type " + req.ContentType);
                }
                else
                {
                    SessionInfo sessionInfo;
                    Map jsondata;
                    try
                    {
                        jsondata = Json.Deserialize(req.Body) as Map;
                    }
                    catch(Json.InvalidJsonSerializationException)
                    {
                        req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                        return;
                    }
                    if(jsondata == null)
                    {
                        ErrorResponse(req, ErrorResult.InvalidRequest);
                        return;
                    }
                    Action<HttpRequest, Map> del;
                    if (!jsondata.ContainsKey("method"))
                    {
                        ErrorResponse(req, ErrorResult.MissingMethod);
                        return;
                    }

                    string methodName = jsondata["method"].ToString();
                    string sessionKey;

#if DEBUG
                    m_Log.DebugFormat("/admin/json Method called {0}", methodName);
#endif

                    switch(methodName)
                    {
                        case "login":
                            HandleLoginRequest(req, jsondata);
                            break;

                        case "challenge":
                            HandleChallengeRequest(req, jsondata);
                            break;

                        default:
                            if(!jsondata.ContainsKey("sessionid"))
                            {
                                ErrorResponse(req, ErrorResult.MissingSessionId);
                                return;
                            }
                            sessionKey = req.CallerIP + "+" + jsondata["sessionid"].ToString();
                            if (!m_Sessions.TryGetValue(sessionKey, out sessionInfo) ||
                                !sessionInfo.IsAuthenticated)
                            {
                                ErrorResponse(req, ErrorResult.NotLoggedIn);
                                return;
                            }
                            else
                            {
                                sessionInfo.LastSeenTickCount = Environment.TickCount;
                            }
                            if (methodName == "logout")
                            {
                                m_Sessions.Remove(sessionKey);
                                SuccessResponse(req, new Map());
                            }
                            else if (!JsonMethods.TryGetValue(methodName, out del))
                            {
                                ErrorResponse(req, ErrorResult.UnknownMethod);
                                return;
                            }
                            else
                            {
                                if (!sessionInfo.Rights.Contains("admin.all"))
                                {
                                    RequiredRightAttribute attr = Attribute.GetCustomAttribute(del.GetType(), typeof(RequiredRightAttribute)) as RequiredRightAttribute;
                                    if (attr != null && !sessionInfo.Rights.Contains(attr.Right))
                                    {
                                        ErrorResponse(req, ErrorResult.InsufficientRights);
                                        return;
                                    }
                                }
                                del(req, jsondata);
                            }
                            break;
                    }
                }
            }
            else if(req.RawUrl.StartsWith("/admin/js/") || req.RawUrl.StartsWith("/admin/css/"))
            {
                string uri = Uri.UnescapeDataString(req.RawUrl).Substring(6);
                if (uri.Contains("..") || uri.Contains("/./") || uri.Contains("\\"))
                {
                    req.ErrorResponse(HttpStatusCode.NotFound, "File Not Found");
                    return;
                }
                ServeFile(req, uri);
            }
            else
            {
                string uri = Uri.UnescapeDataString(req.RawUrl).Substring(6);
                if (uri.Contains("..") || uri.Contains("/./") || uri.Contains("\\"))
                {
                    req.ErrorResponse(HttpStatusCode.NotFound, "File Not Found");
                    return;
                }
                if (0 == uri.Length || uri.EndsWith("/"))
                {
                    uri = "index.html";
                }

                ServeFile(req, m_BasePath + uri);
            }
        }
        #endregion

        #region HTTP File Serving
        void ServeFile(HttpRequest req, string filepath)
        {
            try
            {
                using (FileStream file = new FileStream("../data/adminpages/" + filepath, FileMode.Open))
                {
                    string contentType = "application/octet-stream";
                    if (filepath.EndsWith(".html") || filepath.EndsWith(".htm"))
                    {
                        contentType = "text/html";
                    }
                    else if (filepath.EndsWith(".js"))
                    {
                        contentType = "text/javascript";
                    }
                    else if (filepath.EndsWith(".jpg") || filepath.EndsWith(".jpeg"))
                    {
                        contentType = "image/jpeg";
                    }
                    else if (filepath.EndsWith(".png"))
                    {
                        contentType = "image/png";
                    }
                    else if (filepath.EndsWith(".gif"))
                    {
                        contentType = "image/gif";
                    }

                    using (HttpResponse res = req.BeginResponse(contentType))
                    {
                        using (Stream o = res.GetOutputStream(file.Length))
                        {
                            byte[] buf = new byte[10240];
                            int blocklen;
                            for (blocklen = file.Read(buf, 0, 10240); 
                                0 != blocklen; 
                                blocklen = file.Read(buf, 0, 10240))
                            {
                                o.Write(buf, 0, blocklen);
                            }
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                using (HttpResponse res = req.BeginResponse(HttpStatusCode.NotFound, "Not Found"))
                {
                    res.ContentType = "text/html";
                    using (Stream o = res.GetOutputStream())
                    {
                        using (StreamWriter w = new StreamWriter(o, UTF8NoBOM))
                        {
                            w.Write("<html><head><title>Not Found</title><body>Not Found</body></html>");
                        }
                    }
                }
            }
        }
        #endregion

        #region Commands
        void DisplayAdminWebIFHelp(Main.Common.CmdIO.TTY io)
        {
            io.Write("admin-webif show users\n" +
                "admin-webif delete user <user>\n" +
                "admin-webif grant <user> <right>\n" +
                "admin-webif revoke <user> <right>\n");
        }

        public void AdminWebIFCmd(List<string> args, Main.Common.CmdIO.TTY io, UUID limitedToScene)
        {
            if(limitedToScene != UUID.Zero)
            {
                io.Write("admin-webif command is not allowed on restricted console");
                return;
            }
            else if(args[0] == "help" || args.Count < 2)
            {
                DisplayAdminWebIFHelp(io);
            }
            else
            {
                switch(args[1])
                {
                    case "show":
                        if(args.Count < 3)
                        {
                            DisplayAdminWebIFHelp(io);
                        }
                        else
                        {
                            switch(args[2])
                            {
                                case "users":
                                    string output = "User List: --------------------";
                                    foreach(string name in m_ServerParams[UUID.Zero])
                                    {
                                        if(name.StartsWith("WebIF.Admin.User.") &&
                                            name.EndsWith(".PassCode"))
                                        {
                                            string username = name.Substring(17, name.Length - 17 - 9);
                                            output += "\n";
                                            output += username;
                                        }
                                    }
                                    io.Write(output);
                                    break;

                                default:
                                    DisplayAdminWebIFHelp(io);
                                    break;
                            }
                        }
                        break;

                    case "delete":
                        break;

                    case "grant":
                        break;

                    case "revoke":
                        break;

                    default:
                        io.Write("Unknown admin-webif operation " + args[1]);
                        break;
                }
            }
        }
        #endregion

        #region WebIF admin functions
        [RequiredRight("webif.admin.users.manage")]
        void GrantRight(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("user") || !jsondata.ContainsKey("right"))
            {
                ErrorResponse(req, ErrorResult.InvalidRequest);
            }
            else
            {
                string userRef = "WebIF.Admin.User." + jsondata["user"].ToString().ToLower() + ".";
                string pass_sha1;
                string rights;

                if (m_ServerParams.TryGetValue(UUID.Zero, userRef + "PassCode", out pass_sha1) &&
                    m_ServerParams.TryGetValue(UUID.Zero, userRef + "Rights", out rights))
                {
                    string[] rightlist = rights.ToLower().Split(',');
                    List<string> rightlistnew = new List<string>();
                    AnArray resdata = new AnArray();
                    foreach(string r in rightlist)
                    {
                        rightlistnew.Add(r.Trim());
                        resdata.Add(r.Trim());
                    }
                    if(!rightlistnew.Contains(jsondata["right"].ToString().ToLower()))
                    {
                        rightlistnew.Add(jsondata["right"].ToString().ToLower());
                        resdata.Add(jsondata["right"].ToString().ToLower());
                    }
                    m_ServerParams[UUID.Zero, userRef + "Rights"] = string.Join(",", rightlistnew);
                    Map m = new Map();
                    m["user"] = jsondata["user"];
                    m["rights"] = resdata;
                    SuccessResponse(req, m);
                }
                else
                {
                    ErrorResponse(req, ErrorResult.NotFound);
                }
            }
        }

        [RequiredRight("webif.admin.users.manage")]
        void RevokeRight(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("user") || !jsondata.ContainsKey("right"))
            {
                ErrorResponse(req, ErrorResult.InvalidRequest);
            }
            else
            {
                string userRef = "WebIF.Admin.User." + jsondata["user"].ToString().ToLower() + ".";
                string pass_sha1;
                string rights;

                if (m_ServerParams.TryGetValue(UUID.Zero, userRef + "PassCode", out pass_sha1) &&
                    m_ServerParams.TryGetValue(UUID.Zero, userRef + "Rights", out rights))
                {
                    string[] rightlist = rights.ToLower().Split(',');
                    List<string> rightlistnew = new List<string>();
                    AnArray resdata = new AnArray();
                    foreach (string r in rightlist)
                    {
                        string trimmed = r.Trim();
                        if (trimmed != jsondata["right"].ToString())
                        {
                            rightlistnew.Add(r.Trim());
                            resdata.Add(r.Trim());
                        }
                    }
                    m_ServerParams[UUID.Zero, userRef + "Rights"] = string.Join(",", rightlistnew);
                    Map m = new Map();
                    m["user"] = jsondata["user"];
                    m["rights"] = resdata;
                    SuccessResponse(req, m);
                }
                else
                {
                    ErrorResponse(req, ErrorResult.NotFound);
                }
            }
        }

        [RequiredRight("webif.admin.users.manage")]
        void DeleteUser(HttpRequest req, Map jsondata)
        {
            if (!jsondata.ContainsKey("user"))
            {
                ErrorResponse(req, ErrorResult.InvalidRequest);
            }
            else
            {
                string userRef = "WebIF.Admin.User." + jsondata["user"].ToString().ToLower() + ".";

                m_ServerParams.Remove(UUID.Zero, userRef + "Rights");
                if (m_ServerParams.Remove(UUID.Zero, userRef + "PassCode"))
                {
                    SuccessResponse(req, new Map());
                }
                else
                {
                    ErrorResponse(req, ErrorResult.NotFound);
                }
            }
        }
        #endregion
    }
    #endregion

    #region Factory
    [PluginName("AdminWebIF")]
    public class AdminWebIFFactory : IPluginFactory
    {
        public AdminWebIFFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new AdminWebIF(ownSection.GetString("BasePath", ""));
        }
    }
    #endregion
}
