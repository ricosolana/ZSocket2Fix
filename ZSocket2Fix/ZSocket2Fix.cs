using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
//using Jotunn.Utils;

namespace ZSocket2Fix
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    internal class ZSocket2Fix : BaseUnityPlugin
    {
        // BepInEx' plugin metadata
        public const string PluginGUID = "com.crzi.ZSocket2Fix";
        public const string PluginName = "ZSocket2Fix";
        public const string PluginVersion = "1.0.1";

        Harmony _harmony;

        static bool m_disableSteam = true;

        static bool m_isDedicated = false;

        private void Awake()
        {
            Game.isModded = true;

            ZLog.LogWarning("ZSocket2Fix Checking for -nosteam arg");

            string[] commandLineArgs = Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArgs.Length; i++)
            {
                string text3 = commandLineArgs[i].ToLower();
                if (text3 == "-nosteam")
                {
                    m_disableSteam = true;
                } else if (text3 == "-password")
                {
                    m_isDedicated = true;
                }
            }

            if (m_disableSteam && !m_isDedicated)
            {
                ZLog.LogWarning("ZSocket2Fix disabling steam integration...");
            }

            if (m_isDedicated)
                m_disableSteam = false;

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);

            ZLog.LogWarning("Loaded ZSocket2Fix");
        }

        private void Destroy()
        {
            _harmony?.UnpatchSelf();
        }

        [HarmonyPatch(typeof(DLCMan))]
        class DLCManPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(DLCMan.CheckDLCsSTEAM))]
            static bool CheckDLCsSTEAMPrefix(ref DLCMan __instance)
            {
                return !m_disableSteam;
            }
        }

        [HarmonyPatch(typeof(FejdStartup))]
        class FejdStartupPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(FejdStartup.TransitionToMainScene))]
            static void TransitionToMainScenePrefix(ref FejdStartup __instance)
            {
                ZNet.m_onlineBackend = OnlineBackendType.CustomSocket;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(FejdStartup.InitializeSteam))]
            static bool InitializeSteamPrefix(ref bool __result)
            {
                if (m_disableSteam)
                {
                    __result = true;
                }
                return !m_disableSteam;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(FejdStartup.AwakePlayFab))]
            static bool AwakePlayfabPrefix(ref bool __result)
            {
                if (m_disableSteam)
                {
                    __result = true;
                }
                return !m_disableSteam;
            }
        }

        [HarmonyPatch(typeof(FileHelpers))]
        class FileHelpersPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(FileHelpers.UpdateCloudEnabledStatus))]
            static bool UpdateCloudEnabledStatusPrefix()
            {
                return !m_disableSteam;
            }
        }

        [HarmonyPatch(typeof(ServerList))]
        class ServerListPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(ServerList.RequestServerList))]
            static bool RequestServerListPrefix(ref ServerList __instance)
            {
                return !m_disableSteam;
            }

            //this.UpdateServerListGui(false);
            [HarmonyPrefix]
            [HarmonyPatch(nameof(ServerList.OnServerFilterChanged))]
            static bool OnServerFilterChangedPrefix(ref ServerList __instance)
            {
                if (m_disableSteam)
                {
                    __instance.UpdateServerListGui(false);
                }
                return !m_disableSteam;
            }
        }

        [HarmonyPatch(typeof(SteamManager))]
        class SteamManagerPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(SteamManager.Initialize))]
            static bool InitializePrefix(ref bool __result)
            {
                if (m_disableSteam) {
                    __result = true;
                }
                return !m_disableSteam;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(SteamManager.Initialized), MethodType.Getter)]
            static bool InitializedPrefix(ref bool __result)
            {
                if (m_disableSteam)
                {
                    __result = true;
                }
                return !m_disableSteam;
            }
        }

        [HarmonyPatch(typeof(PrivilegeManager))]
        class PrivilegeManagerPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(PrivilegeManager.CanAccessOnlineMultiplayer), MethodType.Getter)]
            static bool CanAccessOnlineMultiplayerPrefix(ref bool __result)
            {
                if (m_disableSteam)
                {
                    __result = true;
                }
                return !m_disableSteam;
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

                if (m_disableSteam)
                {
                    // only valid with client?
                    match = match
                        // Remove SteamFriends.GetPersonaName
                        .MatchForward(useEnd: false, new CodeMatch(inst => inst?.operand?.ToString().Contains("GetPersonaName") ?? false));
                    //.SetAndAdvance(OpCodes.Ldstr, "nosteam");

                    if (match.IsValid)
                        match = match.SetAndAdvance(OpCodes.Ldstr, "nosteam");
                    else
                        match = match.Start();

                    match = match
                        // Prevent openserver clause
                        .MatchForward(useEnd: false, new CodeMatch(OpCodes.Ldsfld, typeof(ZNet).GetField(nameof(ZNet.m_openServer))))
                        .SetAndAdvance(OpCodes.Ldc_I4_0, null);
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
            static IEnumerable<CodeInstruction> StopAllTranspiler(IEnumerable<CodeInstruction> instructions) {
                if (m_disableSteam)
                {
                    return new CodeMatcher(instructions)
                        .MatchForward(useEnd: false, new CodeMatch(OpCodes.Callvirt, typeof(ZSteamMatchmaking).GetMethod(nameof(ZSteamMatchmaking.ReleaseSessionTicket))))
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
                if (m_disableSteam)
                {
                    return new CodeMatcher(instructions)
                        .MatchForward(useEnd: false, new CodeMatch(OpCodes.Callvirt, typeof(ZSteamMatchmaking).GetMethod(nameof(ZSteamMatchmaking.RequestSessionTicket))))
                        .Advance(-2)
                        .RemoveInstructions(3)

                        // instantiate an empty array in place of RequestSessionTicket call
                        .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldc_I4_0, null),
                            new CodeInstruction(OpCodes.Newarr, typeof(byte))
                        )

                        .InstructionEnumeration();
                }
                return instructions;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(ZNet.Start))]
            static void TransitionToMainScenePrefix(ref ZNet __instance)
            {
                ZNet.m_onlineBackend = OnlineBackendType.CustomSocket;
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(ZNet.Update))]
            static void UpdatePostfix(ref ZNet __instance)
            {
                if (!ZNet.m_isServer)
                {
                    __instance.UpdateClientConnector(Time.deltaTime);
                }
            }
        }

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
                socket.StartHost(2456);
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
                return !m_disableSteam;
            }
        }
    }

}