using System.Net;
using System.Net.Sockets;
using System.Reflection;
using DB;
using HarmonyLib;
using Manager;
using PartyLink;
using Process;
using Manager.Party.Party;
using MAI2.Util;
using Mai2.Mai2Cue;
using static Process.MusicSelectProcess;
using Monitor;
using TMPro;
using UnityEngine;
using Object = System.Object;
#if !UNITY_2018_1_OR_NEWER
using MelonLoader.TinyJSON;
using System.Threading;
#endif

public static class Futari
{
#if UNITY_2018_1_OR_NEWER
    public static bool Enabled => AquaMai.Enabled("Mods.WorldLink");
#endif

    private static readonly Dictionary<NFSocket, FutariSocket> redirect = new();
    private static FutariClient client;
    private static bool checkAuthCalled = false;
    private static bool isInit = false;
    public static bool stopping = false;
    private static int onlineUserCount = 0;

    private static MethodBase packetWriteUInt;
    private static System.Type StartUpStateType;

    // Thread management
    private static System.Threading.Thread onlineUserCountThread;
    private static System.Threading.Thread recruitListThread;

    public static bool Debug => WorldLink.Core.Config.Debug;

    #region Init

    private static readonly MethodInfo SetRecruitData = typeof(MusicSelectProcess).GetProperty("RecruitData")!.SetMethod;
    public static void OnBeforePatch()
    {
        Log.Info("Starting NyanLink patch...");

        packetWriteUInt = typeof(Packet).GetMethod("write_uint", BindingFlags.NonPublic | BindingFlags.Static, null,
            new[] { typeof(PacketType), typeof(int), typeof(uint) }, null);
        if (packetWriteUInt == null) Log.Error("write_uint not found");

        StartUpStateType = typeof(StartupProcess).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance)!.FieldType;
        if (StartUpStateType == null) Log.Error("StartUpStateType not found");

        // Send HTTP request to get the futari client address
        client = new FutariClient("A1234567890", "", 20101);

        // Check if RelayUrl is configured
        if (!string.IsNullOrEmpty(FutariClient.RELAY_BASE))
        {
            // Use configured RelayUrl directly
            try
            {
                var relayUri = new Uri(FutariClient.RELAY_BASE);
                client.host = relayUri.Host;
                client.port = relayUri.Port != -1 ? relayUri.Port : 20101; // Default to 20101 if no port specified
                Log.Info($"Using configured relay server: {client.host}:{client.port}");
            }
            catch (Exception ex)
            {
                Log.Error($"Invalid RelayUrl format: {FutariClient.RELAY_BASE}. Error: {ex.Message}");
                Log.Info("Falling back to lobby server relay info");
                // Fall back to lobby server method
                GetRelayInfoFromLobby();
            }
        }
        else
        {
            // Fall back to getting relay info from lobby server
            GetRelayInfoFromLobby();
        }
    }

    // Entrypoint
    // private void CheckAuth_Proc()
    [HarmonyPrefix]
    [HarmonyPatch(typeof(OperationManager), "CheckAuth_Proc")]
    public static bool CheckAuth_Proc()
    {
        // Prevent multiple calls
        if (checkAuthCalled) return PrefixRet.RUN_ORIGINAL;
        checkAuthCalled = true;

        if (isInit) return PrefixRet.RUN_ORIGINAL;
        Log.Info("CheckAuth_Proc");

        // Randomize keychip
        var keychip = "W9" + new System.Random().Next(100000000, 999999999);

        // Wait until the client is initialized
        new System.Threading.Thread(() =>
        {
            while (client.host == "" && !stopping) Thread.Sleep(100);
            if (client.host == "") return;
            client.keychip = keychip;
            client.ConnectAsync();
            isInit = true;

            // Wait a bit for game systems to fully initialize
            Thread.Sleep(2000);

            // Fetch initial online user count
            FetchOnlineUserCount();

            // Start periodic online user count fetching with proper thread management
            onlineUserCountThread = 30000.Interval(() => FetchOnlineUserCount(), name: "OnlineUserCountThread");
        }).Start();

        return PrefixRet.RUN_ORIGINAL;
    }

    #endregion

    #region Misc

    //Force Enable LanAvailable
#if !UNITY_2018_1_OR_NEWER
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AMDaemon.Network), "IsLanAvailable", MethodType.Getter)]
    private static bool PreIsLanAvailable(ref bool __result)
    {
        __result = true;
        return false;
    }
#endif

    // Fetch online user count from server
    private static void FetchOnlineUserCount()
    {
        if (stopping) return; // Don't fetch if we're shutting down

        try
        {
            // Check if the lobby URL is available
            if (string.IsNullOrEmpty(FutariClient.LOBBY_BASE))
            {
                Log.Debug("Lobby URL not available yet, skipping online user count fetch");
                return;
            }

            var response = $"{FutariClient.LOBBY_BASE}/online".Get();
            if (string.IsNullOrEmpty(response))
            {
                Log.Debug("Empty response from online endpoint");
                return;
            }

            var onlineInfo = JsonUtility.FromJson<OnlineUserInfo>(response);
            if (onlineInfo != null)
            {
                onlineUserCount = onlineInfo.totalUsers;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to fetch online user count: {ex.Message}");
            onlineUserCount = 0;
        }
    }

    //Online Display
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CommonMonitor), "ViewUpdate")]
    public static void CommonMonitorViewUpdate(CommonMonitor __instance, TextMeshProUGUI ____buildVersionText, GameObject ____developmentBuildText)
    {
        ____buildVersionText.transform.position = ____developmentBuildText.transform.position;
        ____buildVersionText.gameObject.SetActive(true);
        switch (client.StatusCode)
        {
            case -1:
                ____buildVersionText.text = $"NyanLink Offline";
                ____buildVersionText.color = Color.red;
                break;
            case 0:
                ____buildVersionText.text = $"NyanLink Disconnect";
                ____buildVersionText.color = Color.gray;
                break;
            case 1:
                ____buildVersionText.text = $"NyanLink Connecting";
                ____buildVersionText.color = Color.yellow;
                break;
            case 2:
                var recruitCount = PartyMan == null ? 0 : PartyMan.GetRecruitList().Count;
                if (onlineUserCount > 0)
                {
                    ____buildVersionText.text = $"NyanLink Room: {recruitCount} Online: {onlineUserCount}";
                    ____buildVersionText.color = new Color(172 / 256f, 181 / 256f, 250 / 256f);
                }
                else if (onlineUserCount == 0)
                {
                    ____buildVersionText.text = $"NyanLink Online: 0";
                    ____buildVersionText.color = Color.gray;
                }
                break;
        }
    }

    // Block irrelevant packets
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SocketBase), "sendClass", typeof(ICommandParam))]
    public static bool sendClass(SocketBase __instance, ICommandParam info)
    {
        switch (info)
        {
            // Block AdvocateDelivery, SettingHostAddress
            case AdvocateDelivery or Setting.SettingHostAddress:
                return PrefixRet.BLOCK_ORIGINAL;

            // If it's a Start/FinishRecruit message, send it using http instead
            case StartRecruit or FinishRecruit:
                var inf = info is StartRecruit o ? o.RecruitInfo : ((FinishRecruit)info).RecruitInfo;
                var start = info is StartRecruit ? "start" : "finish";
                var msg = JsonUtility.ToJson(new RecruitRecord
                {
                    Keychip = client.keychip,
                    RecruitInfo = inf
                }, false);
                $"{FutariClient.LOBBY_BASE}/recruit/{start}".PostAsync(msg);
                Log.Info($"Sent {start} recruit message: {msg}");
                return PrefixRet.BLOCK_ORIGINAL;

            // Log the actual type of info and the actual type of this class
            default:
                Log.Debug($"SendClass: {Log.BRIGHT_RED}{info.GetType().Name}{Log.RESET} from {__instance.GetType().Name}");
                return PrefixRet.RUN_ORIGINAL;
        }
    }

    // Patch for error logging
    // SocketBase:: protected void error(string message, int no)
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SocketBase), "error", typeof(string), typeof(int))]
    public static bool preError(string message, int no)
    {
        Log.Error($"Error: {message} ({no})");
        return PrefixRet.RUN_ORIGINAL;
    }

    // Force isSameVersion to return true
    // Packet:: public bool isSameVersion()
#if !UNITY_2018_1_OR_NEWER
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Packet), "isSameVersion")]
    public static void postIsSameVersion(ref bool __result)
    {
        Log.Debug($"isSameVersion (original): {__result}, forcing true");
        __result = true;
    }
#endif

    // Patch my IP address to a stub
    // public static IPAddress MyIpAddress(int mockID)
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PartyLink.Util), "MyIpAddress", typeof(int))]
    public static bool preMyIpAddress(int mockID, ref IPAddress __result)
    {
        __result = FutariExt.MyStubIP().ToIP();
        return PrefixRet.BLOCK_ORIGINAL;
    }

    #endregion

    #region Recruit Information

    private static readonly MethodInfo RStartRecruit = typeof(Client)
        .GetMethod("RecvStartRecruit", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo RFinishRecruit = typeof(Client)
        .GetMethod("RecvFinishRecruit", BindingFlags.NonPublic | BindingFlags.Instance);
    private static Dictionary<string, RecruitInfo> lastRecruits = new();

    private static string Identity(this RecruitInfo x) => $"{x.IpAddress} : {x.MusicID}";

    // Client Constructor
    // Client:: public Client(string name, PartyLink.Party.InitParam initParam)
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Client), MethodType.Constructor, typeof(string), typeof(PartyLink.Party.InitParam))]
    public static void postClientConstruct(Client __instance, string name, PartyLink.Party.InitParam initParam)
    {
        Log.Debug($"new Client({name}, {initParam})");
        recruitListThread = 10000.Interval(() =>
        {
            if (stopping) return; // Don't process if we're shutting down

            try
            {
                // Add null checks to prevent crashes
                if (__instance == null || lastRecruits == null)
                {
                    Log.Debug("Client or lastRecruits is null, skipping recruit list update");
                    return;
                }

                var response = $"{FutariClient.LOBBY_BASE}/recruit/list".Get();
                if (string.IsNullOrEmpty(response))
                {
                    Log.Debug("Empty response from recruit list endpoint");
                    return;
                }

                var recruitRecords = response.Trim().Split('\n')
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Select(JsonUtility.FromJson<RecruitRecord>)
                    .Where(x => x != null && x.RecruitInfo != null) // Filter out null records
                    .ToList();

                // Process finished recruits
                var currentIds = recruitRecords.Select(x => x.RecruitInfo.Identity()).ToList();
                var finishedRecruits = lastRecruits.Keys.Where(key => !currentIds.Contains(key)).ToList();

                foreach (var key in finishedRecruits)
                {
                    try
                    {
                        if (lastRecruits.ContainsKey(key) && lastRecruits[key] != null)
                        {
                            var packet = new Packet(lastRecruits[key].IpAddress);
                            packet.encode(new FinishRecruit(lastRecruits[key]));
                            RFinishRecruit?.Invoke(__instance, new object[] { packet });
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error processing finished recruit {key}: {ex.Message}");
                    }
                }

                // Process new recruits
                foreach (var record in recruitRecords)
                {
                    try
                    {
                        if (record.RecruitInfo != null)
                        {
                            var packet = new Packet(record.RecruitInfo.IpAddress);
                            packet.encode(new StartRecruit(record.RecruitInfo));
                            RStartRecruit?.Invoke(__instance, new object[] { packet });
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error processing new recruit: {ex.Message}");
                    }
                }

                // Update lastRecruits dictionary
                lastRecruits = recruitRecords
                    .Where(x => x.RecruitInfo != null)
                    .ToDictionary(x => x.RecruitInfo.Identity(), x => x.RecruitInfo);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in recruit list update: {ex.Message}");
            }
        }, name: "RecruitListThread");
    }

    // Block start recruit if the song is not available
    // Client:: private void RecvStartRecruit(Packet packet)
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Client), "RecvStartRecruit", typeof(Packet))]
    public static bool preRecvStartRecruit(Packet packet)
    {
        var inf = packet.getParam<StartRecruit>().RecruitInfo;
        Log.Info($"RecvStartRecruit: {JsonUtility.ToJson(inf)}");
        if (Singleton<DataManager>.Instance.GetMusic(inf.MusicID) == null)
        {
            Log.Error($"Recruit received but music {inf.MusicID} is not available.");
            Log.Error($"If you want to play with {string.Join(" and ", inf.MechaInfo.UserNames)},");
            Log.Error("make sure you have the same game version and option packs installed.");
            return PrefixRet.BLOCK_ORIGINAL;
        }
        return PrefixRet.RUN_ORIGINAL;
    }

    #endregion

    private static IManager PartyMan => Manager.Party.Party.Party.Get();

    //Skip StartupNetworkChecker
    [HarmonyPostfix]
    [HarmonyPatch(nameof(StartupProcess), nameof(StartupProcess.OnUpdate))]
    public static void postStartupOnUpdate(ref byte ____state, string[] ____statusMsg, string[] ____statusSubMsg)
    {
        var msgCount = ____statusMsg.Length;
        // Status code
        ____statusMsg[msgCount - 3] = "NyanLink";
        ____statusSubMsg[msgCount - 3] = client.StatusCode switch
        {
            -1 => "BAD",
            0 => "Not Connect",
            1 => "Connecting",
            2 => "GOOD",
            _ => "Waiting..."
        };

        // Delay
        ____statusMsg[msgCount - 2] = "PING";
        ____statusSubMsg[msgCount - 2] = client._delayAvg == 0 ? "N/A" : $"{client._delayAvg} ms";

        var isThursday = DateTime.Now.DayOfWeek == DayOfWeek.Thursday;

        ____statusMsg[msgCount - 1] = isThursday ? "CRAZY THURSDAY" : "CAT :3";
        ____statusSubMsg[msgCount - 1] = client._delayIndex % 2 == 0 ? "" : (isThursday ? "V50" : "MEOW");

        // If it is in the wait link delivery state, change to ready immediately
        if (____state != 0x04) return;
        ____state = 0x08;

        // Start the services that would have been started by the StartupNetworkChecker
        DeliveryChecker.get().start(true);
        Setting.get().setData(new Setting.Data().Also(x => x.set(false, 4)));
        Setting.get().setRetryEnable(true);
        Advertise.get().initialize(MachineGroupID.ON);
        PartyMan.Start(MachineGroupID.ON);
        Log.Info("Skip Startup Network Check");
    }

    #region NFSocket

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NFSocket), MethodType.Constructor, typeof(AddressFamily), typeof(SocketType), typeof(ProtocolType), typeof(int))]
    public static void postNFCreate(NFSocket __instance, AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, int mockID)
    {
        Log.Debug($"new NFSocket({addressFamily}, {socketType}, {protocolType}, {mockID})");
        if (mockID == 3939) return; // Created in redirected NFAccept as a stub
        var futari = new FutariSocket(addressFamily, socketType, protocolType, mockID);
        redirect.Add(__instance, futari);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NFSocket), MethodType.Constructor, typeof(Socket))]
    public static void postNFCreate2(NFSocket __instance, Socket nfSocket)
    {
        Log.Error("new NFSocket(Socket) -- We shouldn't get here.");
        throw new NotImplementedException();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NFSocket), "Poll")]
    public static bool preNFPoll(NFSocket socket, SelectMode mode, ref bool __result)
    {
        __result = FutariSocket.Poll(redirect[socket], mode);
        return PrefixRet.BLOCK_ORIGINAL;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NFSocket), "Send")]
    public static bool preNFSend(NFSocket __instance, byte[] buffer, int offset, int size, SocketFlags socketFlags, ref int __result)
    {
        __result = redirect[__instance].Send(buffer, offset, size, socketFlags);
        return PrefixRet.BLOCK_ORIGINAL;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NFSocket), "SendTo")]
    public static bool preNFSendTo(NFSocket __instance, byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP, ref int __result)
    {
        __result = redirect[__instance].SendTo(buffer, offset, size, socketFlags, remoteEP);
        return PrefixRet.BLOCK_ORIGINAL;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NFSocket), "Receive")]
    public static bool preNFReceive(NFSocket __instance, byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode, ref int __result)
    {
        __result = redirect[__instance].Receive(buffer, offset, size, socketFlags, out errorCode);
        return PrefixRet.BLOCK_ORIGINAL;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NFSocket), "ReceiveFrom")]
    public static bool preNFReceiveFrom(NFSocket __instance, byte[] buffer, SocketFlags socketFlags, ref EndPoint remoteEP, ref int __result)
    {
        __result = redirect[__instance].ReceiveFrom(buffer, socketFlags, ref remoteEP);
        return PrefixRet.BLOCK_ORIGINAL;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NFSocket), "Bind")]
    public static bool preNFBind(NFSocket __instance, EndPoint localEndP)
    {
        Log.Debug("NFBind");
        redirect[__instance].Bind(localEndP);
        return PrefixRet.BLOCK_ORIGINAL;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NFSocket), "Listen")]
    public static bool preNFListen(NFSocket __instance, int backlog)
    {
        Log.Debug("NFListen");
        redirect[__instance].Listen(backlog);
        return PrefixRet.BLOCK_ORIGINAL;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NFSocket), "Accept")]
    public static bool preNFAccept(NFSocket __instance, ref NFSocket __result)
    {
        Log.Debug("NFAccept");
        var futariSocket = redirect[__instance].Accept();
        var mockSocket = new NFSocket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp, 3939);
        redirect[mockSocket] = futariSocket;
        __result = mockSocket;
        return PrefixRet.BLOCK_ORIGINAL;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NFSocket), "ConnectAsync")]
    public static bool preNFConnectAsync(NFSocket __instance, SocketAsyncEventArgs e, int mockID, ref bool __result)
    {
        Log.Debug("NFConnectAsync");
        __result = redirect[__instance].ConnectAsync(e, mockID);
        return PrefixRet.BLOCK_ORIGINAL;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NFSocket), "SetSocketOption")]
    public static bool preNFSetSocketOption(NFSocket __instance, SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue)
    {
        redirect[__instance].SetSocketOption(optionLevel, optionName, optionValue);
        return PrefixRet.BLOCK_ORIGINAL;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NFSocket), "Close")]
    public static bool preNFClose(NFSocket __instance)
    {
        Log.Debug("NFClose");
        redirect[__instance].Close();
        return PrefixRet.BLOCK_ORIGINAL;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NFSocket), "Shutdown")]
    public static bool preNFShutdown(NFSocket __instance, SocketShutdown how)
    {
        Log.Debug("NFShutdown");
        redirect[__instance].Shutdown(how);
        return PrefixRet.BLOCK_ORIGINAL;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NFSocket), "RemoteEndPoint", MethodType.Getter)]
    public static bool preNFGetRemoteEndPoint(NFSocket __instance, ref EndPoint __result)
    {
        Log.Debug("NFGetRemoteEndPoint");
        __result = redirect[__instance].RemoteEndPoint;
        return PrefixRet.BLOCK_ORIGINAL;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NFSocket), "LocalEndPoint", MethodType.Getter)]
    public static bool preNFGetLocalEndPoint(NFSocket __instance, ref EndPoint __result)
    {
        Log.Debug("NFGetLocalEndPoint");
        __result = redirect[__instance].LocalEndPoint;
        return PrefixRet.BLOCK_ORIGINAL;
    }

    #endregion

    #region Packet codec

    // Disable encryption
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Packet), "encrypt")]
    public static bool prePacketEncrypt(Packet __instance, PacketType ____encrypt, PacketType ____plane)
    {
        ____encrypt.ClearAndResize(____plane.Count);
        Array.Copy(____plane.GetBuffer(), 0, ____encrypt.GetBuffer(), 0, ____plane.Count);
        ____encrypt.ChangeCount(____plane.Count);
        packetWriteUInt.Invoke(null, new Object[] { ____plane, 0, (uint)____plane.Count });
        return PrefixRet.BLOCK_ORIGINAL;
    }

    // Disable decryption
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Packet), "decrypt")]
    public static bool prePacketDecrypt(Packet __instance, PacketType ____encrypt, PacketType ____plane)
    {
        ____plane.ClearAndResize(____encrypt.Count);
        Array.Copy(____encrypt.GetBuffer(), 0, ____plane.GetBuffer(), 0, ____encrypt.Count);
        ____plane.ChangeCount(____encrypt.Count);
        packetWriteUInt.Invoke(null, new Object[] { ____plane, 0, (uint)____plane.Count });
        return PrefixRet.BLOCK_ORIGINAL;
    }

    #endregion

    #region Recruit UI

    private static int musicIdSum;
    private static bool sideMessageFlag;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MusicSelectProcess), "OnStart")]
    public static bool preMusicSelectProcessOnStart(MusicSelectProcess __instance)
    {
        // 每次重新进入选区菜单之后重新初始化变量
        musicIdSum = 0;
        sideMessageFlag = false;
        return PrefixRet.RUN_ORIGINAL;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MusicSelectProcess), "PartyExec")]
    public static void postPartyExec(MusicSelectProcess __instance)
    {
        // 检查联机房间是否有更新，如果更新的话设置 IsConnectingMusic=false 然后刷新列表
        var checkDiff = PartyMan.GetRecruitListWithoutMe().Sum(item => item.MusicID);
        if (musicIdSum != checkDiff)
        {
            musicIdSum = checkDiff;
            __instance.IsConnectingMusic = false;
        }

        if (__instance.IsConnectingMusic && __instance.RecruitData != null && __instance.IsConnectionFolder())
        {
            // 设置房间信息显示
            var info = __instance.RecruitData.MechaInfo;
            var players = "NyanLink Room! Players: " +
                string.Join(" and ", info.UserNames.Where((_, i) => info.FumenDifs[i] != -1));

            __instance.MonitorArray.Where((_, i) => __instance.IsEntry(i))
                .Each(x => x.SetSideMessage(players));

            sideMessageFlag = true;
        }
        else if (!__instance.IsConnectionFolder() && sideMessageFlag)
        {
            __instance.MonitorArray.Where((_, i) => __instance.IsEntry(i))
                .Each(x => x.SetSideMessage(CommonMessageID.Scroll_Music_Select.GetName()));

            sideMessageFlag = false;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MusicSelectProcess), "RecruitData", MethodType.Getter)]
    public static void postRecruitData(MusicSelectProcess __instance, ref RecruitInfo __result)
    {
        // 开歌时设置当前选择的联机数据
        if (!__instance.IsConnectionFolder() || __result == null) return;

        var list = PartyMan.GetRecruitListWithoutMe();
        if (list == null) return;
        if (__instance.CurrentMusicSelect >= 0 && __instance.CurrentMusicSelect < list.Count)
        {
            __result = list[__instance.CurrentMusicSelect];
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MusicSelectProcess), "IsConnectStart")]
    public static bool preIsConnectStart(
        MusicSelectProcess __instance,
        List<CombineMusicSelectData> ____connectCombineMusicDataList,
        SubSequence[] ____currentPlayerSubSequence,
        ref bool __result)
    {
        __result = false;

        // 修正 SetConnectData 触发条件，阻止原有 IP 判断重新设置
        var recruits = PartyMan.GetRecruitListWithoutMe();
        if (!__instance.IsConnectingMusic && recruits.Count > 0)
        {
            // var recruit = recruits.Count > 0 ? recruits[0] : null;
            var recruit = recruits[0];
            Log.Debug($"MusicSelectProcess::IsConnectStart recruit data has been set to {recruit}");
            SetRecruitData.Invoke(__instance, new Object[] { recruit });
            preSetConnectData(__instance, ____connectCombineMusicDataList, ____currentPlayerSubSequence);
            __result = true;
        }
        return PrefixRet.BLOCK_ORIGINAL;
    }

    public static readonly MethodInfo SetConnectCategoryEnable = typeof(MusicSelectProcess).GetProperty("IsConnectCategoryEnable")!.SetMethod;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MusicSelectProcess), "SetConnectData")]
    public static bool preSetConnectData(
        MusicSelectProcess __instance,
        List<CombineMusicSelectData> ____connectCombineMusicDataList,
        SubSequence[] ____currentPlayerSubSequence)
    {
        ____connectCombineMusicDataList.Clear();
        SetConnectCategoryEnable.Invoke(__instance, new Object[] { false });

        // 遍历所有房间并且显示
        foreach (var item in PartyMan.GetRecruitListWithoutMe())
        {
            var musicID = item.MusicID;
            var combineMusicSelectData = new CombineMusicSelectData();
            var music = Singleton<DataManager>.Instance.GetMusic(musicID);
            var notesList = Singleton<NotesListManager>.Instance.GetNotesList()[musicID].NotesList;

            switch (musicID)
            {
                case < 10000:
                    combineMusicSelectData.existStandardScore = true;
                    break;
                case > 10000 and < 20000:
                    combineMusicSelectData.existDeluxeScore = true;
                    break;
            }

            for (var i = 0; i < 2; i++)
            {
                combineMusicSelectData.musicSelectData.Add(new MusicSelectData(music, notesList, 0));
            }
            ____connectCombineMusicDataList.Add(combineMusicSelectData);
            try
            {
                var thumbnailName = music.thumbnailName;
                for (var j = 0; j < __instance.MonitorArray.Length; j++)
                {
                    if (!__instance.IsEntry(j)) continue;

                    __instance.MonitorArray[j].SetRecruitInfo(thumbnailName);
                    SoundManager.PlaySE(Cue.SE_INFO_NORMAL, j);
                }
            }
            catch
            { /* 防止有可能的空 */
            }

            __instance.IsConnectingMusic = true;
        }

        // No data available, add a dummy entry
        if (PartyMan.GetRecruitListWithoutMe().Count == 0)
        {
            ____connectCombineMusicDataList.Add(new CombineMusicSelectData
            {
                musicSelectData = new List<MusicSelectData> { null, null },
                isWaitConnectScore = true
            });
            __instance.IsConnectingMusic = false;
        }

        if (__instance.MonitorArray == null) return PrefixRet.BLOCK_ORIGINAL;

        for (var l = 0; l < __instance.MonitorArray.Length; l++)
        {
            if (____currentPlayerSubSequence[l] != SubSequence.Music) continue;

            __instance.MonitorArray[l].SetDeployList(false);
            if (!__instance.IsConnectionFolder(0)) continue;

            __instance.ChangeBGM();
            if (!__instance.IsEntry(l)) continue;

            __instance.MonitorArray[l].SetVisibleButton(__instance.IsConnectingMusic, InputManager.ButtonSetting.Button04);
        }
        return PrefixRet.BLOCK_ORIGINAL;
    }

    #endregion

    // Helper method to get relay info from lobby server
    private static void GetRelayInfoFromLobby()
    {
        $"{FutariClient.LOBBY_BASE}/info".GetAsync((sender, e) =>
        {
            if (e.Error != null)
            {
                Log.Error($"Failed to get NyanLink server address: {e.Error}");
                return;
            }
            // Response Format: {"relayHost": "google.com", "relayPort": 20101}
            var info = JsonUtility.FromJson<ServerInfo>(e.Result);
            client.host = info.relayHost;
            client.port = info.relayPort;
            Log.Info($"NyanLink server address: {info.relayHost}:{info.relayPort}");
        });
    }
}