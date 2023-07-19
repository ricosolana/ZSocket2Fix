using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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

        private void Awake()
        {
            Game.isModded = true;

            Directory.CreateDirectory("./dumped");

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);

            ZLog.Log("Loading ZSocketFix2");
        }

        private void Destroy()
        {
            _harmony?.UnpatchSelf();
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
        }

        [HarmonyPatch(typeof(ZNet))]
        class ZNetPatch
        {
            //[HarmonyPostfix]
            //[HarmonyPatch(nameof(ZNet.SetServerHost))]
            //static void SetServerHostPostfix()
            //{
            //    ZNet.m_onlineBackend = OnlineBackendType.CustomSocket;
            //}

            [HarmonyPostfix]
            [HarmonyPatch(nameof(ZNet.Awake))]
            static void AwakePostFix(ref ZNet __instance)
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
                // Close only if already opened
                if (!ZNet.m_isServer)
                {
                    __instance.UpdateClientConnector(Time.deltaTime);
                }
            }
        }
    }

}