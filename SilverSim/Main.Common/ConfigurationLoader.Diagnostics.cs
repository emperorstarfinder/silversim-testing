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

using SilverSim.Http.Client;
using SilverSim.Main.Common.Caps;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.Statistics;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Grid;
using SilverSim.Types.StructuredData.XmlRpc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace SilverSim.Main.Common
{
    public partial class ConfigurationLoader
    {
        #region List TCP Ports
        public Dictionary<int, string> KnownTcpPorts
        {
            get
            {
                var tcpPorts = new Dictionary<int, string>
                {
                    [(int)HttpServer.Port] = "HTTP Server"
                };
                BaseHttpServer httpsServer;
                if(TryGetHttpsServer(out httpsServer))
                {
                    tcpPorts.Add((int)httpsServer.Port, "HTTPS Server");
                }
                return tcpPorts;
            }
        }
        #endregion

        private void ShowHttpHandlersCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            BaseHttpServer http = HttpServer;
            var sb = new StringBuilder("HTTP Handlers: (" + http.ServerURI + ")\n----------------------------------------------\n");
            ListHttpHandlers(sb, http);
            BaseHttpServer https;
            if(TryGetHttpsServer(out https))
            {
                sb.AppendFormat("\nHTTPS Handlers: ({0})\n----------------------------------------------\n", https.ServerURI);
                ListHttpHandlers(sb, https);
            }
            io.Write(sb.ToString());
        }

        private void ListHttpHandlers(StringBuilder sb, BaseHttpServer server)
        {
            foreach (KeyValuePair<string, Action<HttpRequest>> kvp in server.UriHandlers)
            {
                sb.AppendFormat("URL: {0}\n", kvp.Key);
            }
            foreach (KeyValuePair<string, Action<HttpRequest>> kvp in server.StartsWithUriHandlers)
            {
                sb.AppendFormat("URL: {0}*\n", kvp.Key);
            }
            foreach (KeyValuePair<string, Action<HttpRequest>> kvp in server.RootUriContentTypeHandlers)
            {
                sb.AppendFormat("Content-Type: {0}\n", kvp.Key);
            }
        }

        private void ShowThreadsCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (UUID.Zero != limitedToScene)
            {
                io.Write("Not allowed on limited console");
            }
            else if (args[0] == "help")
            {
                io.Write("Show existing threads");
            }
            else
            {
                var sb = new StringBuilder("Threads:\n----------------------------------------------\n");
                foreach (Thread t in ThreadManager.Threads)
                {
                    sb.AppendFormat("Thread({0}): {1}\n", t.ManagedThreadId, t.Name);
                    sb.AppendFormat("- State: {0}\n", t.ThreadState.ToString());
                }
                io.Write(sb.ToString());
            }
        }

        private void ShowXmlRpcHandlersCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            var sb = new StringBuilder("XMLRPC Handlers:\n----------------------------------------------\n");
            HttpXmlRpcHandler server = XmlRpcServer;
            foreach (KeyValuePair<string, Func<XmlRpc.XmlRpcRequest, XmlRpc.XmlRpcResponse>> kvp in server.XmlRpcMethods)
            {
                sb.AppendFormat("Method: {0}\n", kvp.Key);
            }
            io.Write(sb.ToString());
        }

        private void ShowJson20RpcHandlersCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            var sb = new StringBuilder("JSON2.0RPC Handlers:\n----------------------------------------------\n");
            HttpJson20RpcHandler server = Json20RpcServer;
            foreach (KeyValuePair<string, Func<string, IValue, IValue>> kvp in server.Json20RpcMethods)
            {
                sb.AppendFormat("Method: {0}\n", kvp.Key);
            }
            io.Write(sb.ToString());
        }

        private void ShowCapsHandlersCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            var sb = new StringBuilder("Caps Handlers:\n----------------------------------------------\n");
            CapsHttpRedirector redirector = CapsRedirector;
            foreach (KeyValuePair<string, RwLockedDictionary<UUID, Action<HttpRequest>>> kvp in redirector.Caps)
            {
                sb.AppendFormat("Capability: {0}\n", kvp.Key);
                foreach (KeyValuePair<UUID, Action<HttpRequest>> kvpInner in kvp.Value)
                {
                    sb.AppendFormat("- ID: {0}\n", kvpInner.Key);
                }
                sb.AppendLine();
            }
            io.Write(sb.ToString());
        }

        #region Show Port allocations
        private readonly GridServiceInterface m_RegionStorage;
        private void ShowPortAllocationsCommand(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            var sb = new StringBuilder("TCP Ports:\n----------------------------------------------\n");
            foreach (KeyValuePair<int, string> kvp in KnownTcpPorts)
            {
                sb.AppendFormat("{0}:\n- Port: {1}\n", kvp.Value, kvp.Key);
            }
            if (m_RegionStorage != null)
            {
                sb.Append("\nUDP Ports:\n----------------------------------------------\n");
                foreach (RegionInfo region in m_RegionStorage.GetAllRegions())
                {
                    string status = Scenes.ContainsKey(region.ID) ? "online" : "offline";
                    sb.AppendFormat("Region \"{0}\" ({1})\n- Port: {2}\n- Status: ({3})\n", region.Name, region.ID, region.ServerPort, status);
                }
            }
            io.Write(sb.ToString());
        }
        #endregion

        public void ShowIssuesCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (UUID.Zero != limitedToScene)
            {
                io.Write("show issues not allowed on limited console");
            }
            else if (args[0] == "help")
            {
                io.Write("show issues");
            }
            else
            {
                io.Write("Known Configuration Issues:\n" + string.Join("\n", KnownConfigurationIssues));
            }
        }

        private static void ShowMemoryCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (args[0] == "help")
            {
                io.Write("Shows current memory usage by simulator");
            }
            else
            {
                Process p = Process.GetCurrentProcess();
                FormattedListBuilder b = new FormattedListBuilder();
                const long MB_DIV = 1048576;

                b.AddColumn("", 20);
                b.AddColumn("Current (MB)", 15);
                b.AddColumn("Peak (MB)", 15);
                b.AddHeader();
                b.AddSeparator();
                b.AddData("GC Heap", (GC.GetTotalMemory(false) + MB_DIV - 1) / MB_DIV, "");
                b.AddData("Process memory", (p.WorkingSet64 + MB_DIV - 1) / MB_DIV, (p.PeakWorkingSet64 + MB_DIV - 1) / MB_DIV);

                io.WriteFormatted(b.ToString());
            }
        }

        private void ShowModulesCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (args[0] == "help")
            {
                io.Write("show modules [<searchstring>] - Show currently loaded modules");
            }
            else
            {
                string searchstring = string.Empty;
                if (args.Count > 2)
                {
                    searchstring = args[2].ToLower();
                }

                var output = new StringBuilder("Module List:\n----------------------------------------------");
                if (!string.IsNullOrEmpty(searchstring))
                {
                    output.AppendFormat("\n<limited to modules containing \"{0}\">\n", searchstring);
                }
                foreach (KeyValuePair<string, IPlugin> moduledesc in PluginInstances)
                {
                    var desc = (DescriptionAttribute)Attribute.GetCustomAttribute(moduledesc.Value.GetType(), typeof(DescriptionAttribute));
                    if (!string.IsNullOrEmpty(searchstring) &&
                        !moduledesc.Key.ToLower().Contains(searchstring))
                    {
                        continue;
                    }

                    output.AppendFormat("\nModule {0}:", moduledesc.Key);
                    if (desc != null)
                    {
                        output.Append("\n   Description: ");
                        output.Append(desc.Description);
                    }
                    foreach (KeyValuePair<Type, string> kvp in FeaturesTable)
                    {
                        if (kvp.Key.IsInterface)
                        {
                            if (moduledesc.Value.GetType().GetInterfaces().Contains(kvp.Key))
                            {
                                output.Append("\n  - ");
                                output.Append(kvp.Value);
                            }
                        }
                        else if (kvp.Key.IsAssignableFrom(moduledesc.Value.GetType()))
                        {
                            output.Append("\n  - ");
                            output.Append(kvp.Value);
                        }
                    }
                    output.Append("\n");
                }
                io.Write(output.ToString());
            }
        }

        private static void ShowThreadCountCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (args[0] == "help")
            {
                io.Write("Show current thread count");
            }
            else
            {
                int maxWorkerThreads;
                int maxCompletionThreads;
                int availWorkerThreads;
                int availCompletionThreads;
                ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxCompletionThreads);
                ThreadPool.GetAvailableThreads(out availWorkerThreads, out availCompletionThreads);

                io.WriteFormatted("Threads: {0}\n\nSystem Threadpool:\nActive worker threads: {1} of {2}\nActive completion port threads: {3} of {4}",
                    Process.GetCurrentProcess().Threads.Count, 
                    maxWorkerThreads - availWorkerThreads, 
                    maxWorkerThreads, 
                    maxCompletionThreads - availCompletionThreads, 
                    maxCompletionThreads);
            }
        }

        private static void ShowTimeSourcesCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (args[0] == "help")
            {
                io.Write("Show time sources");
            }
            else
            {
                FormattedListBuilder fb = new FormattedListBuilder()
                    .AddColumn("Source", 20)
                    .AddColumn("Elapsed time (s)", 20)
                    .AddColumn("Ticks per secs", 20)
                    .AddHeader()
                    .AddSeparator();
                fb.AddData("Stopwatch", TimeProvider.StopWatch.TicksToSecs(TimeProvider.StopWatch.TickCount).ToString("N3"), TimeProvider.StopWatch.Frequency);
                fb.AddData("Environment", TimeProvider.Environment.TicksToSecs(TimeProvider.Environment.TickCount).ToString("N3"), TimeProvider.Environment.Frequency);
                io.Write(fb.ToString());
            }
        }

        private void ShowQueuesCommand(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if (args[0] == "help")
            {
                io.Write("Show queue stats on instance");
            }
            else if (UUID.Zero != limitedToScene)
            {
                io.Write("Not allowed on limited console");
            }
            else
            {
                var sb = new StringBuilder("Queue List:\n----------------------------------------------");
                foreach (KeyValuePair<string, IQueueStatsAccess> kvp in GetServices<IQueueStatsAccess>())
                {
                    foreach (QueueStatAccessor accessors in kvp.Value.QueueStats)
                    {
                        QueueStat stat = accessors.GetData();
                        sb.AppendFormat("\n{0}: {1}:\n- Status: {2}\n- Count: {3}\n- Processed: {4}\n", kvp.Key, accessors.Name, stat.Status, stat.Count, stat.Processed);
                    }
                }
                io.Write(sb.ToString());
            }
        }

        private void ShowConnectivity(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            if(args[0] == "help")
            {
                io.Write("Show network connectivity");
            }
            else
            {
                string hostname = ExternalHostNameService.ExternalHostName;
                IPAddress address;

                var sb = new StringBuilder($"Hostname={hostname}");
                try
                {
                    address = DnsNameCache.GetHostAddresses(hostname, true)[0];
                    sb.Append("\nIP=").Append(address);
                }
                catch
                {
                    sb.Append("\nExternallyAccessible=False");
                    io.Write(sb.ToString());
                    return;
                }

                bool overallsuccess = true;
                if(SystemIPv4Service.IsAddressOnInterface(address))
                {
                    sb.Append("\nNAT=False");
                    if (SystemIPv4Service.IsPrivateIPAddress(address) || IPAddress.IsLoopback(address))
                    {
                        sb.Append("\nPublicIP=False\nExternallyAccessible=False");
                    }
                    else if(SystemIPv4Service.IsCGNAT(address))
                    {
                        sb.Append("\nCGNAT=True\nPublicIP=False\nExternallyAccessible=False");
                    }
                    else
                    {
                        sb.Append("\nPublicIP=True\nExternallyAccessible=True");
                    }
                }
                else
                {
                    bool success;
                    sb.Append("\nNAT=True");

                    if (SystemIPv4Service.IsPrivateIPAddress(address) || IPAddress.IsLoopback(address))
                    {
                        sb.Append("\nPublicIP=False");
                        overallsuccess = false;
                    }
                    else if (SystemIPv4Service.IsCGNAT(address))
                    {
                        sb.Append("\nCGNAT=True\nPublicIP=False");
                        overallsuccess = false;
                    }
                    else
                    {
                        sb.Append("\nPublicIP=True");
                    }

                    try
                    {
                        new HttpClient.Head($"http://{address}:{HttpServer.Port}/diag-ping-external")
                        {
                            ConnectionMode = HttpClient.ConnectionModeEnum.SingleRequest
                        }.ExecuteRequest();
                        success = true;
                        if (m_DetectedCallerIP != address.ToString())
                        {
                            sb.Append("\nHTTPOnAddressDetectedCaller=").Append(m_DetectedCallerIP);
                            success = false;
                        }
                    }
                    catch
                    {
                        success = false;
                    }

                    sb.Append("\nHTTPOnAddress=").Append(success);
                    overallsuccess = overallsuccess && success;

                    try
                    {
                        new HttpClient.Head($"http://{HttpServer.ExternalHostName}:{HttpServer.Port}/diag-ping-external")
                        {
                            ConnectionMode = HttpClient.ConnectionModeEnum.SingleRequest
                        }.ExecuteRequest();
                        success = true;
                        if (m_DetectedCallerIP != address.ToString())
                        {
                            sb.Append("\nHTTPOnHostnameDetectedCaller=").Append(m_DetectedCallerIP);
                            success = false;
                        }
                    }
                    catch
                    {
                        success = false;
                    }

                    sb.Append("\nHTTPOnHostname=").Append(success);
                    overallsuccess = overallsuccess && success;

                    BaseHttpServer https;
                    if(TryGetHttpsServer(out https))
                    {
                        try
                        {
                            new HttpClient.Head($"https://{address}:{https.Port}/diag-ping-external")
                            {
                                ConnectionMode = HttpClient.ConnectionModeEnum.SingleRequest
                            }.ExecuteRequest();
                            success = true;
                            if (m_DetectedCallerIP != address.ToString())
                            {
                                sb.Append("\nHTTPSOnAddressDetectedCaller=").Append(m_DetectedCallerIP);
                                success = false;
                            }
                        }
                        catch (Exception e)
                        {
                            success = false;
                            sb.Append("\nHTTPSOnAddressError=").Append(e.Message);
                        }

                        sb.Append("\nHTTPSOnAddress=").Append(success);
                        overallsuccess = overallsuccess && success;

                        try
                        {
                            new HttpClient.Head($"https://{https.ExternalHostName}:{https.Port}/diag-ping-external")
                            {
                                ConnectionMode = HttpClient.ConnectionModeEnum.SingleRequest
                            }.ExecuteRequest();
                            success = true;
                            if (m_DetectedCallerIP != address.ToString())
                            {
                                sb.Append("\nHTTPSOnHostnameDetectedCaller=").Append(m_DetectedCallerIP);
                                success = false;
                            }
                        }
                        catch (Exception e)
                        {
                            success = false;
                            sb.Append("\nHTTPSOnHostnameError=").Append(e.Message);
                        }

                        sb.Append("\nHTTPSOnHostname=").Append(success);
                        overallsuccess = overallsuccess && success;
                    }

                    lock (m_NetTestLock)
                    {
                        var circuits = new Dictionary<int, IUDPCircuitsManager>();
                        m_ReceivedAddressesOnUDP.Clear();
                        foreach (SceneInterface scene in Scenes.Values)
                        {
                            IUDPCircuitsManager circuit = scene.UDPServer;
                            circuits.Add(circuit.LocalPort, circuit);
                            circuit.OnNetTest += NetTestResponse;
                        }

                        foreach (IUDPCircuitsManager circuit in circuits.Values)
                        {
                            circuit.SendNetTest(address);
                        }

                        Thread.Sleep(1000);

                        foreach(IUDPCircuitsManager circuit in circuits.Values)
                        {
                            circuit.OnNetTest -= NetTestResponse;
                        }

                        foreach(IUDPCircuitsManager circuit in circuits.Values)
                        {
                            IPAddress detectedSender;
                            if(m_ReceivedAddressesOnUDP.TryGetValue(circuit.LocalPort, out detectedSender))
                            {
                                if(detectedSender.Equals(address))
                                {
                                    sb.Append("\nUDP").Append(circuit.LocalPort).Append("=True");
                                }
                                else
                                {
                                    sb.Append("\nUDP").Append(circuit.LocalPort).Append("=False\nUDP").Append(circuit.LocalPort).Append("DetectedAddress=").Append(detectedSender);
                                }
                            }
                            else
                            {
                                sb.Append("\nUDP").Append(circuit.LocalPort).Append("=False");
                                overallsuccess = false;
                            }
                        }
                    }

                    sb.Append("\nExternallyAccessible=").Append(overallsuccess);
                }
                io.Write(sb.ToString());
            }
        }

        private readonly object m_NetTestLock = new object();
        private readonly RwLockedDictionary<int, IPAddress> m_ReceivedAddressesOnUDP = new RwLockedDictionary<int, IPAddress>();

        private void NetTestResponse(IPAddress address, int port)
        {
            m_ReceivedAddressesOnUDP[port] = address;
        }

        private string m_DetectedCallerIP;

        private void HandleDiagPingExternal(HttpRequest req)
        {
            m_DetectedCallerIP = req.CallerIP;
            req.EmptyResponse();
        }
    }
}
