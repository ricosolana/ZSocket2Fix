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
    //[NetworkCompatibility(CompatibilityLevel.ServerMustHaveMod, VersionStrictness.Minor)]
    internal class ZSocket2Fix : BaseUnityPlugin
    {
        // BepInEx' plugin metadata
        public const string PluginGUID = "com.crzi.ZSocket2Fix";
        public const string PluginName = "ZSocket2Fix";
        public const string PluginVersion = "1.0.0";

        Harmony _harmony;

        static bool m_disableSteam = true;

        private void Awake()
        {
            Game.isModded = true;

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);

            ZLog.LogWarning("Loaded ZSocketFix2");
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
                if (m_disableSteam)
                {
                    return false;
                }
                return true;
            }

            /*
            [HarmonyPrefix]
            [HarmonyPatch(nameof(DLCMan.IsDLCInstalled))]
            static bool IsDLCInstalledPrefix(ref DLCMan __instance, ref bool __result)
            {
                if (m_disableSteam)
                {
                    __result = false;
                    return false;
                }
                return true;
            }*/
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
                    return false;
                }
                return true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(FejdStartup.AwakePlayFab))]
            static bool AwakePlayfabPrefix(ref bool __result)
            {
                if (m_disableSteam)
                {
                    __result = true;
                    return false;
                }
                return true;
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
                if (m_disableSteam)
                {
                    // disable i guess?

                    return false;
                }
                return true;
            }

            //this.UpdateServerListGui(false);
            [HarmonyPrefix]
            [HarmonyPatch(nameof(ServerList.OnServerFilterChanged))]
            static bool OnServerFilterChangedPrefix(ref ServerList __instance)
            {
                if (m_disableSteam)
                {
                    // disable i guess?
                    __instance.UpdateServerListGui(false);

                    return false;
                }
                return true;
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
                    return false;
                }
                return true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(SteamManager.Initialized), MethodType.Getter)]
            static bool InitializedPrefix(ref bool __result)
            {
                if (m_disableSteam)
                {
                    __result = true;
                    return false;
                }
                return true;
            }

            /*
            [HarmonyTranspiler]
            [HarmonyPatch(nameof(FejdStartup.InitializeSteam))]
            static IEnumerable<CodeInstruction> InitializeSteamTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                if (m_disableSteam)
                {
                    return new CodeMatcher(instructions)
                        .MatchForward(useEnd: false, new CodeMatch(OpCodes.Call, typeof(SteamFriends).GetMethod(nameof(SteamFriends.GetPersonaName))))
                        .RemoveInstructions
                        
                        .Advance(-1)
                        .RemoveInstructions(4) // removes steam instructions (hopefully)
                        .InstructionEnumeration();
                }
                return instructions;
            }*/
        }

        //public static bool CanAccessOnlineMultiplayer
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

                    return false;
                }
                return true;
            }
        }


        /*
        [HarmonyPatch(typeof(SteamFriends))]
        class SteamFriendsPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(SteamFriends.GetPersonaName))]
            static bool GetPersonaNamePrefix(ref string __result)
            {
                if (m_disableSteam)
                {
                    __result = "nosteam";
                    return false;
                }
                return true;
            }
        }*/


        [HarmonyPatch(typeof(ZNet))]
        class ZNetPatch
        {
            //[HarmonyPostfix]
            //[HarmonyPatch(nameof(ZNet.SetServerHost))]
            //static void SetServerHostPostfix()
            //{
            //    ZNet.m_onlineBackend = OnlineBackendType.CustomSocket;
            //}

            [HarmonyTranspiler]
            [HarmonyPatch(nameof(ZNet.Awake))]
            static IEnumerable<CodeInstruction> AwakeTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                if (m_disableSteam)
                {
                    //foreach (var inst in instructions) {
                    //    ZLog.LogWarning(inst.opcode + " " + inst.operand);
                    //}

                    //typeof()


                    //var mymatch = new CodeMatch(OpCodes.Call, typeof(SteamFriends).GetMethod(nameof(SteamFriends.GetPersonaName)));

                    //var method = typeof(SteamFriends)
                    //    .GetMethod(nameof(SteamFriends.GetPersonaName), 
                    //    BindingFlags.Static | BindingFlags.Public);
                    //
                    //ZLog.LogWarning("method: " + (method != null ? method.ToString() : "no"));

                    //ZLog.LogWarning("match: " + mymatch.operand);

                    //var inst26 = instructions.ToArray()[26];

                    //ZLog.LogWarning("inst26: " + inst26.operand);

                    //ZLog.LogWarning(.operand.ToString());

                    var pos = new CodeMatcher(instructions)
                        //.MatchForward(useEnd: false, new CodeMatch(OpCodes.Call, typeof(SteamManager).GetMethod(nameof(SteamManager.Initialize), BindingFlags.Static)))
                        //.Advance(2)
                        //.SetAndAdvance(OpCodes.Ldstr, "nosteam")

                        // Remove SteamFriends.GetPersonaName

                        // try to find method by just name?
                        // name seems to be missing from
                        //.MatchForward(useEnd: false, new CodeMatch(OpCodes.Call, typeof(SteamFriends).GetMethod(nameof(SteamFriends.GetPersonaName), BindingFlags.Static | BindingFlags.Public)))
                        .MatchForward(useEnd: false, new CodeMatch(inst => inst?.operand?.ToString().Contains("GetPersonaName") ?? false))
                        .Pos;

                    ZLog.LogWarning("Pos: " + pos);


                    return new CodeMatcher(instructions)
                        //.MatchForward(useEnd: false, new CodeMatch(OpCodes.Call, typeof(SteamManager).GetMethod(nameof(SteamManager.Initialize), BindingFlags.Static)))
                        //.Advance(2)
                        //.SetAndAdvance(OpCodes.Ldstr, "nosteam")

                        // Remove SteamFriends.GetPersonaName
                        //.MatchForward(useEnd: false, new CodeMatch(OpCodes.Call, typeof(SteamFriends).GetMethod(nameof(SteamFriends.GetPersonaName), BindingFlags.Static | BindingFlags.Public)))
                        //.MatchForward(useEnd: false, new CodeMatch(inst => inst.operand.Equals(OpCodes.Call) && inst.operand.ToString().Contains("GetPersonaName")))
                        //.MatchForward(useEnd: false, new CodeMatch(inst => inst.operand != null && inst.operand.ToString().Contains("GetPersonaName")))
                        .MatchForward(useEnd: false, new CodeMatch(inst => inst?.operand?.ToString().Contains("GetPersonaName") ?? false))
                        .SetAndAdvance(OpCodes.Ldstr, "nosteam")
                        
                        // Prevent openserver clause
                        .MatchForward(useEnd: false, new CodeMatch(OpCodes.Ldsfld, typeof(ZNet).GetField(nameof(ZNet.m_openServer))))
                        .SetAndAdvance(OpCodes.Ldc_I4_0, null)
                        
                        // Add postfix to 
                        .End()
                        .Insert(new CodeInstruction(
                            OpCodes.Call,
                            Transpilers.EmitDelegate<Action>(ZNetAwakePostfixDelegate).operand)
                        )
                        // Finish
                        .InstructionEnumeration();
                }
                return instructions;
            }



            [HarmonyTranspiler]
            [HarmonyPatch(nameof(ZNet.StopAll))]
            static IEnumerable<CodeInstruction> StopAllTranspiler(IEnumerable<CodeInstruction> instructions) {
                if (m_disableSteam)
                {
                    return new CodeMatcher(instructions)
                        .MatchForward(useEnd: false, new CodeMatch(OpCodes.Callvirt, typeof(ZSteamMatchmaking).GetMethod(nameof(ZSteamMatchmaking.ReleaseSessionTicket))))
                        .Advance(-1)
                        .RemoveInstructions(4) // removes steam instructions (hopefully)
                        .InstructionEnumeration();
                }
                return instructions;
            }

            /*
            [HarmonyPrefix]
            [HarmonyPatch(nameof(ZNet.Awake))]
            static void AwakePrefix(ref ZNet __instance)
            {
                // Close only if already opened
                if (ZNet.m_isServer && m_disableSteam)
                {
                    // this avoids more steam setup
                    ZNet.m_openServer = false;
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(ZNet.Awake))]
            static void AwakePostfix(ref ZNet __instance)
            {
                // Close only if already opened
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
            }*/

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
                // Close only if already opened
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
                if (m_disableSteam)
                {
                    return false;
                }
                return true;
            }
        }
    }

}