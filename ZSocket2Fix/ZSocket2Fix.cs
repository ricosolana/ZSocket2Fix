﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
//using Jotunn.Utils;
using Jotunn.Managers;
using UnityEngine.UI;
using TMPro;

namespace ZSocket2Fix
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    internal class ZSocket2Fix : BaseUnityPlugin
    {
        // BepInEx' plugin metadata
        public const string PluginGUID = "com.crzi.ZSocket2Fix";
        public const string PluginName = "ZSocket2Fix";
        public const string PluginVersion = "1.0.2";

        Harmony _harmony;

        public static ConfigEntry<bool> m_disableSteamSetting;
        public static ConfigEntry<string> m_serverBindAddressSetting;

        public static uint m_serverBindAddressIPv4 = 0;
        public static IPAddress m_serverBindIpAddress = null;
        public static bool m_connectUsingTCP = false;

        //static bool m_disableSteam = true;

        public static bool m_isDedicated = false;

        public static GameObject ZS2Button;

        public static BepInEx.Logging.ManualLogSource LOGGER;

        private void Awake()
        {
            Game.isModded = true;

            m_disableSteamSetting = Config.Bind(
                "Client",
                "DisableSteam",
                false,
                "Whether to disable steam integration"
            );

            m_serverBindAddressSetting = Config.Bind(
                "Server",
                "ServerBindAddress",
                "0.0.0.0",
                "Custom address to bind server listen socket on"
            );

            // woodpanel_serverlist
            //GUIManager.

            // call servergui creation within awake of ServerList

            LOGGER = Logger;

            //GUIManager.Instance.CreateButton("Connect with TCP")

            //ZS2Button = new GameObject("ZS2", typeof(Button));
            //ZS2Button.transform.SetParent(ServerList.instance.m_serverListRoot);

            // Determine whether environment is a dedicated headless server
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArgs.Length; i++)
            {
                string text3 = commandLineArgs[i].ToLower();
                if (text3 == "-password")
                {
                    m_isDedicated = true;
                }
            }

            if (m_disableSteamSetting.Value && !m_isDedicated)
            {
                ZLog.LogWarning("ZSocket2Fix disabling steam integration...");
            }
            
            if (m_isDedicated)
            {
                m_disableSteamSetting.Value = false;
                // ensure ip is valid

                m_serverBindIpAddress = IPAddress.Parse(m_serverBindAddressSetting.Value);

                var bytes = m_serverBindIpAddress.GetAddressBytes();

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }

                // TODO test this:
                //  m_serverBindAddressIPv4 being set to something besides 0 when bytes is not {0, 0, 0, 0}

                m_serverBindAddressIPv4 = BitConverter.ToUInt32(bytes, 0);

                ZLog.LogWarning("Bytes + ip converted:::");
                ZLog.LogWarning(m_serverBindAddressSetting.Value);
                ZLog.LogWarning(BitConverter.ToString(bytes));
                ZLog.LogWarning(m_serverBindAddressIPv4);
            }

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);

            ZLog.LogWarning("Loaded ZSocket2Fix");
        }

        // Update "Connect with TCP" button
        [HarmonyPatch(typeof(ServerList))]
        class ServerListPatch
        {
            [HarmonyPostfix]
            //[HarmonyPatch(nameof(ZNet.Awake))]
            [HarmonyPatch(nameof(ServerList.OnEnable))]
            public static void OnEnablePostfix(ServerList __instance)
            {
                //LOGGER.LogError("ServerList Awake");

                if (ZS2Button != null)
                {
                    // already initialized
                    return;
                }

                if (GUIManager.Instance == null)
                {
                    LOGGER.LogError("GUIManager instance is null");
                    return;
                }



                m_connectUsingTCP = false;

                var joinButton = ServerList.instance.m_joinGameButton;

                // Scale it down first and move up
                var rect = joinButton.GetComponent<RectTransform>();

                var delta = rect.sizeDelta; //.y *= 0.5;
                rect.sizeDelta = new Vector2(delta.x, delta.y * 0.55f);

                joinButton.transform.position += new Vector3(0, 17);

                // TODO create a copy of 
                ZS2Button = Instantiate(joinButton.gameObject, joinButton.transform.parent);
                ZS2Button.GetComponent<Button>().onClick.AddListener(() =>
                {
                    // connect using TCP
                    m_connectUsingTCP = true;
                    FejdStartup.instance.OnJoinStart();
                });

                ZS2Button.transform.position = new Vector2(rect.position.x, rect.position.y - rect.rect.height);

                ZS2Button.SetActive(true);

                ZS2Button.name = "ZS2Button";

                //ZS2Button.GetComponentInChildren<Text>().text = "Connect with TCP";

                ZS2Button.GetComponentInChildren<TextMeshProUGUI>().text = "Connect with TCP";


                /*
                ZLog.LogError("joinGameButton: "
                    + rect.anchorMin + ", "
                    + rect.anchorMax + ", "
                    + rect.position + ", "
                    + rect.sizeDelta + ", "
                    + joinButton.transform.parent.name + ", "
                    //+ rect.parent.name
                    + joinButton.transform.parent.parent?.name
                );

                ZS2Button = GUIManager.Instance.CreateButton(
                    text: "Connect with TCP",
                    parent: joinButton.transform.parent, // ServerList.instance.m_serverListRoot.transform,
                    anchorMin: rect.anchorMin, //new Vector2(0.5f, 0.5f),
                    anchorMax: rect.anchorMax, // + new Vector2(0, 0.05f), //new Vector2(0.5f, 0.5f),
                    position: new Vector2(0, 0), // new Vector2(rect.position.x, rect.position.y), // - rect.rect.height - 3),
                    //width: 250f,
                    //height: 60f
                    width: rect.rect.width,
                    height: rect.rect.height
                );
                */

                //ZS2Button.GetComponent<Button>().onClick.AddListener(() =>
                //{
                //    // connect using TCP
                //    m_connectUsingTCP = true;
                //    FejdStartup.instance.OnJoinStart();
                //});
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(ServerList.UpdateButtons))]
            static void UpdateButtonsPostfix(ref ServerList __instance)
            {
                int selectedServer = __instance.GetSelectedServer();
                bool isSelected = selectedServer >= 0;

                ZS2Button.GetComponent<Button>().interactable = isSelected;
            }
        }

        [HarmonyPatch(typeof(DLCMan))]
        class DLCManPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(DLCMan.CheckDLCsSTEAM))]
            static bool CheckDLCsSTEAMPrefix(ref DLCMan __instance)
            {
                return !m_disableSteamSetting.Value;
            }
        }

        [HarmonyPatch(typeof(FejdStartup))]
        class FejdStartupPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(FejdStartup.TransitionToMainScene))]
            static void TransitionToMainScenePrefix(ref FejdStartup __instance)
            {
                if (ZNet.m_isServer || m_connectUsingTCP)
                {
                    ZNet.m_onlineBackend = OnlineBackendType.CustomSocket;
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(FejdStartup.InitializeSteam))]
            static bool InitializeSteamPrefix(ref bool __result)
            {
                if (m_disableSteamSetting.Value)
                {
                    __result = true;
                }
                return !m_disableSteamSetting.Value;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(FejdStartup.AwakePlayFab))]
            static bool AwakePlayfabPrefix(ref bool __result)
            {
                if (m_disableSteamSetting.Value)
                {
                    __result = true;
                }
                return !m_disableSteamSetting.Value;
            }
        }

        [HarmonyPatch(typeof(FileHelpers))]
        class FileHelpersPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(FileHelpers.UpdateCloudEnabledStatus))]
            static bool UpdateCloudEnabledStatusPrefix()
            {
                return !m_disableSteamSetting.Value;
            }
        }

        [HarmonyPatch(typeof(SteamManager))]
        class SteamManagerPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(SteamManager.Initialize))]
            static bool InitializePrefix(ref bool __result)
            {
                if (m_disableSteamSetting.Value)
                {
                    __result = true;
                }
                return !m_disableSteamSetting.Value;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(SteamManager.Initialized), MethodType.Getter)]
            static bool InitializedPrefix(ref bool __result)
            {
                if (m_disableSteamSetting.Value)
                {
                    __result = true;
                }
                return !m_disableSteamSetting.Value;
            }

        }

        public static void dump_instructions(IEnumerable<CodeInstruction> instructions)
        {
            for (int i = 0; i < instructions.Count(); i++)
            {
                var inst = instructions.ElementAt(i);
                var opr = inst.operand;

                string result = opr?.GetType()?.ToString() ?? "";

                var info = opr as MethodInfo;
                if (info != null)
                {
                    result += ", " + info.Name + ", " + info.Module?.FullyQualifiedName;
                }

                ZLog.Log(i + ": " + inst.opcode + " | "
                    + opr + " ||| " + result);

                //ZLog.LogWarning(i + ": " + inst.opcode + " | " 
                //    + opr + ", " + opr?.GetType()?.ToString() + " | " + (opr?.GetType()?.IsSubclassOf(typeof(MethodInfo)) ? opr. : ""));
            }
        }

        //static void my_test_caller_do_not_use()
        //{
        //    bool status = GameServer.Init(m_serverBindAddressIPv4, 0, 2456, 2456 + 1, EServerMode.eServerModeNoAuthentication, "1.0.0.0");
        //    if (!status)
        //        throw new Exception("bad");
        //}
        //
        //// TODO detemine whether return is required to be passed 
        //static bool AwakeDelegate(uint unIP, ushort usSteamPort, ushort usGamePort, ushort usQueryPort, EServerMode eServerMode, string pchVersionString)
        //{
        //    return GameServer.Init(m_serverBindAddressIPv4, usSteamPort, usGamePort, usQueryPort, eServerMode, pchVersionString);
        //}

        [HarmonyPatch(typeof(PrivilegeManager))]
        class PrivilegeManagerPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(PrivilegeManager.CanAccessOnlineMultiplayer), MethodType.Getter)]
            static bool CanAccessOnlineMultiplayerPrefix(ref bool __result)
            {
                if (m_disableSteamSetting.Value)
                {
                    __result = true;
                }
                return !m_disableSteamSetting.Value;
            }
        }

        [HarmonyPatch(typeof(ZNet))]
        class ZNetPatch
        {
            [HarmonyTranspiler]
            [HarmonyPatch(nameof(ZNet.Awake))]
            static IEnumerable<CodeInstruction> AwakeTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                var match = new CodeMatcher(instructions);

                //ZLog.Log("Original ZNet.Awake");
                //dump_instructions(instructions);

                if (m_disableSteamSetting.Value)
                {
                    // Force remove any client match for GetPersonaName
                    match = match
                        // Removes SteamFriends.GetPersonaName
                        .MatchForward(useEnd: false, new CodeMatch(ci => ci.opcode.Equals(OpCodes.Call) && ci.operand.ToString().Contains(nameof(Steamworks.SteamFriends.GetPersonaName))));
                    
                    if (match.IsValid)
                        match = match.SetAndAdvance(OpCodes.Ldstr, "nosteam");
                    else
                        match = match.Start();

                    match = match
                        // Prevent openserver clause
                        .MatchForward(useEnd: false, new CodeMatch(OpCodes.Ldsfld, typeof(ZNet).GetField(nameof(ZNet.m_openServer))))
                        .SetInstruction(new CodeInstruction(OpCodes.Ldc_I4_0));
                }
                return match
                    // postfix
                    .End()
                    .Insert(new CodeInstruction(
                        OpCodes.Call,
                        Transpilers.EmitDelegate<Action>(ZNetAwakePostfixDelegate).operand)
                    )
                    .InstructionEnumeration();
            }

            [HarmonyTranspiler]
            [HarmonyPatch(nameof(ZNet.StopAll))]
            static IEnumerable<CodeInstruction> StopAllTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                if (m_disableSteamSetting.Value)
                {
                    // TODO 
                    // More precise, testing needed
                    //return new CodeMatcher(instructions)
                    //    .MatchForward(useEnd: false, 
                    //        new CodeMatch(
                    //            OpCodes.Call,   
                    //            typeof(ZSteamMatchmaking).GetMethod(nameof(ZSteamMatchmaking.instance))),
                    //        new CodeMatch(
                    //            OpCodes.Callvirt,
                    //            typeof(ZSteamMatchmaking).GetMethod(nameof(ZSteamMatchmaking.ReleaseSessionTicket))),
                    //        new CodeMatch(
                    //            OpCodes.Call,
                    //            typeof(ZSteamMatchmaking).GetMethod(nameof(ZSteamMatchmaking.instance))),
                    //        new CodeMatch(
                    //            OpCodes.Callvirt,
                    //            typeof(ZSteamMatchmaking).GetMethod(nameof(ZSteamMatchmaking.UnregisterServer))))
                    //    .RemoveInstructions(4)
                    //    .InstructionEnumeration();

                    return new CodeMatcher(instructions)
                        .MatchForward(useEnd: false, new CodeMatch(OpCodes.Callvirt, typeof(ZSteamMatchmaking).GetMethod(nameof(ZSteamMatchmaking.ReleaseSessionTicket))))
                        // TODO add a MatchBackward to remove, instead of flat removing
                        .Advance(-1)
                        .RemoveInstructions(4)
                        .InstructionEnumeration();
                }
                return instructions;
            }

            [HarmonyTranspiler]
            [HarmonyPatch(nameof(ZNet.SendPeerInfo))]
            static IEnumerable<CodeInstruction> SendPeerInfoTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                if (m_disableSteamSetting.Value)
                {
                    return new CodeMatcher(instructions)
                        .MatchForward(useEnd: false, new CodeMatch(OpCodes.Callvirt, typeof(ZSteamMatchmaking).GetMethod(nameof(ZSteamMatchmaking.RequestSessionTicket))))
                        // TODO add a MatchBackward to target get_instance and remove from there, instead of removing flat instructions
                        .Advance(-2)
                        .RemoveInstructions(3)

                        // instantiate an empty array in place of RequestSessionTicket call
                        .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldc_I4_0), // Of size 0
                            new CodeInstruction(OpCodes.Newarr, typeof(byte)) // new byte[]
                        )

                        .InstructionEnumeration();
                }
                return instructions;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(ZNet.Start))]
            static void StartPrefix(ref ZNet __instance)
            {
                if (ZNet.m_isServer || m_connectUsingTCP)
                {
                    ZNet.m_onlineBackend = OnlineBackendType.CustomSocket;
                }
            }

            // Client TCP connect ONLY
            [HarmonyPostfix]
            [HarmonyPatch(nameof(ZNet.Update))]
            static void UpdatePostfix(ref ZNet __instance)
            {
                if (!ZNet.m_isServer)
                {
                    if (m_connectUsingTCP)
                    {
                        __instance.UpdateClientConnector(Time.deltaTime);
                    }
                }
            }
        }

        // Patches TCP server ONLY
        static void ZNetAwakePostfixDelegate()
        {
            ZNet __instance = ZNet.instance;
            if (ZNet.m_isServer)
            {
                if (__instance.m_hostSocket != null)
                {
                    ZLog.LogWarning("Closing host socket: " + __instance.GetType().Name);
                    __instance.m_hostSocket.Close();
                    __instance.m_hostSocket.Dispose();
                }

                ZSocket2 socket = new ZSocket2();
                socket.StartHost(ZSteamSocket.m_steamDataPort);
                __instance.m_hostSocket = socket;
                ZLog.LogWarning("Started TCP Custom Socket");
            }
        }

        [HarmonyPatch(typeof(ZSteamMatchmaking))]
        class ZSteamMatchmakingPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(ZSteamMatchmaking.Initialize))]
            static bool InitializePrefix()
            {
                return !m_disableSteamSetting.Value;
            }
        }        
    }
}