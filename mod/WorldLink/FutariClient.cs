using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using HarmonyLib;
#if !UNITY_2018_1_OR_NEWER
using System.Threading;
using AMDaemon;
using PartyLink;
using static Manager.Accounting;
#endif

public class FutariClient
{
    public static string LOBBY_BASE => AquaMai.ReadString("Mods.WorldLink.LobbyUrl");
    // public const string LOBBY_BASE = "https://aquadx.net/aqua/mai2-futari";
    public static FutariClient Instance { get; private set; }

    public FutariClient(string keychip, string host, int port, int _)
    {
        this.host = host;
        this.port = port;
        this.keychip = keychip;
    }

    public FutariClient(string keychip, string host, int port) : this(keychip, host, port, 0)
    {
        Instance = this;
    }

    public string keychip { get; set; }

    private TcpClient _tcpClient;
    private StreamWriter _writer;
    private StreamReader _reader;

    public readonly ConcurrentQueue<FutariMsg> sendQ = new();
    // <Port + Stream ID, Message Queue>
    public readonly ConcurrentDictionary<int, ConcurrentQueue<FutariMsg>> tcpRecvQ = new();
    // <Port, Message Queue>
    public readonly ConcurrentDictionary<int, ConcurrentQueue<FutariMsg>> udpRecvQ = new();
    // <Port, Accept Queue>
    public readonly ConcurrentDictionary<int, ConcurrentQueue<FutariMsg>> acceptQ = new();
    // <Port + Stream ID, Callback>
    public readonly ConcurrentDictionary<int, Action<FutariMsg>> acceptCallbacks = new();

    private System.Threading.Thread _sendThread;
    private System.Threading.Thread _recvThread;
     
    private bool _reconnecting = false;

    private readonly Stopwatch _heartbeat = new Stopwatch().Also(it => it.Start());
    private readonly long[] _delayWindow = new int[20].Select(_ => -1L).ToArray();
    public int _delayIndex = 0;
    public long _delayAvg = 0;
    
    public string host { get; set; }
    public int port { get; set; }

    public IPAddress StubIP => FutariExt.KeychipToStubIp(keychip).ToIP();

    /// <summary>
    /// -1: Failed to connect
    /// 0: Not connect
    /// 1: Connecting
    /// 2: Connected
    /// </summary>
    public int StatusCode { get; private set; } = 0;
    public string ErrorMsg { get; private set; } = "";

    public void ConnectAsync() => new System.Threading.Thread(Connect) { IsBackground = true }.Start();

    private void Connect()
    {
        _tcpClient = new TcpClient();

        try
        {
            StatusCode = 1;
            _tcpClient.Connect(host, port);
            StatusCode = 2;
        }
        catch (Exception ex)
        {
            StatusCode = -1;
            ErrorMsg = ex.Message;
            Log.Error($"Error connecting to server:\nHost:{host}:{port}\n{ex.Message}");
            ConnectAsync();
            return;
        }
        var networkStream = _tcpClient.GetStream();
        _writer = new StreamWriter(networkStream, Encoding.UTF8) { AutoFlush = true };
        _reader = new StreamReader(networkStream, Encoding.UTF8);
        _reconnecting = false;

        // Register
        Send(new FutariMsg { FutariCmd = FutariCmd.CTL_START, data = keychip });
        Log.Info($"Connected to server at {host}:{port}");

        // Start communication and message receiving in separate threads
        _sendThread = 10.Interval(() =>
        {
            if (_heartbeat.ElapsedMilliseconds > 1000)
            {
                _heartbeat.Restart();
                Send(new FutariMsg { FutariCmd = FutariCmd.CTL_HEARTBEAT });
            }

            // Send any data in the send queue
            while (sendQ.TryDequeue(out var msg)) Send(msg);

        }, final: Reconnect, name: "SendThread", stopOnError: true);
        
        _recvThread = 10.Interval(() =>
        {
            var line = _reader.ReadLine();
            if (line == null) return;

            var message = FutariMsg.FromString(line);
            HandleIncomingMessage(message);
        
        }, final: Reconnect, name: "RecvThread", stopOnError: true);
    }

    public void Bind(int bindPort, ProtocolType proto)
    {
        if (proto == ProtocolType.Tcp) 
            acceptQ.TryAdd(bindPort, new ConcurrentQueue<FutariMsg>());
        else if (proto == ProtocolType.Udp)
            udpRecvQ.TryAdd(bindPort, new ConcurrentQueue<FutariMsg>());
    }

    private void Reconnect()
    {
        Log.Warn("Reconnect Entered");
        if (_reconnecting) return;
        _reconnecting = true;
        
        try { _tcpClient.Close(); }
        catch { /* ignored */ }

        try { _sendThread.Abort(); }
        catch { /* ignored */ }
        
        try { _recvThread.Abort(); }
        catch { /* ignored */ }
        
        _sendThread = null;
        _recvThread = null;
        _tcpClient = null;
        
        // Reconnect
        Log.Warn("Reconnecting...");
        ConnectAsync();
    }

    private void HandleIncomingMessage(FutariMsg futariMsg)
    {
        if (futariMsg.FutariCmd != FutariCmd.CTL_HEARTBEAT)
            Log.Info($"{StubIP} <<< {futariMsg.ToReadableString()}");

        switch (futariMsg.FutariCmd)
        {
            // Heartbeat
            case FutariCmd.CTL_HEARTBEAT:
                var delay = _heartbeat.ElapsedMilliseconds;
                _delayWindow[_delayIndex] = delay;
                _delayIndex = (_delayIndex + 1) % _delayWindow.Length;
                _delayAvg = (long) _delayWindow.Where(x => x != -1).Average();
                Log.Info($"Heartbeat: {delay}ms, Avg: {_delayAvg}ms");
                break;
            
            // UDP message
            case FutariCmd.DATA_SEND or FutariCmd.DATA_BROADCAST when futariMsg is { proto: ProtocolType.Udp, dPort: not null }:
                udpRecvQ.Get(futariMsg.dPort.Value)?.Also(q =>
                {
                    Log.Info($"+ Added to UDP queue, there are {q.Count + 1} messages in queue");
                })?.Enqueue(futariMsg);
                break;
            
            // TCP message
            case FutariCmd.DATA_SEND when futariMsg.proto == ProtocolType.Tcp && futariMsg is { sid: not null, dPort: not null }:
                tcpRecvQ.Get(futariMsg.sid.Value + futariMsg.dPort.Value)?.Also(q =>
                {
                    Log.Info($"+ Added to TCP queue, there are {q.Count + 1} messages in queue for port {futariMsg.dPort}");
                })?.Enqueue(futariMsg);
                break;
            
            // TCP connection request
            case FutariCmd.CTL_TCP_CONNECT when futariMsg.dPort != null:
                acceptQ.Get(futariMsg.dPort.Value)?.Also(q =>
                {
                    Log.Info($"+ Added to Accept queue, there are {q.Count + 1} messages in queue");
                })?.Enqueue(futariMsg);
                break;
            
            // TCP connection accept
            case FutariCmd.CTL_TCP_ACCEPT when futariMsg is { sid: not null, dPort: not null }:
                acceptCallbacks.Get(futariMsg.sid.Value + futariMsg.dPort.Value)?.Invoke(futariMsg);
                break;
        }
    }

    private void Send(FutariMsg futariMsg)
    {
        // Check if msg's destination ip is the same as my local ip. If so, handle it locally
        if (futariMsg.dst == StubIP.ToU32())
        {
            Log.Debug($"Loopback @@@ {futariMsg.ToReadableString()}");
            HandleIncomingMessage(futariMsg);
            return;
        }
        
        _writer.WriteLine(futariMsg);
        if (futariMsg.FutariCmd != FutariCmd.CTL_HEARTBEAT)
            Log.Info($"{StubIP} >>> {futariMsg.ToReadableString()}");
    }
}
