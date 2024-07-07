using HarmonyLib;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ZSocket2Fix
{
    internal class PatchServerBind
    {

        [HarmonyPatch(typeof(SteamManager))]
        class SteamManagerPatch
        {
            // TODO change / impl this properly NOW
            [HarmonyTranspiler]
            [HarmonyPatch(nameof(SteamManager.Awake))]
            static IEnumerable<CodeInstruction> AwakeTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                if (!ZSocket2Fix.m_disableSteamSetting.Value && ZSocket2Fix.m_isDedicated && ZSocket2Fix.m_serverBindAddressIPv4 != 0)
                {
                    // we patch steam server 
                    //nameof(ZRpc.Invoke)

                    //ZLog.LogWarning("Original Awake:");
                    //ZSocket2Fix.dump_instructions(instructions);

                    //((MethodInfo)null).Module.Name

                    //if (true)
                    //return instructions;

                    var patched = new CodeMatcher(instructions)
                        // find the method call to Init
                        //////.MatchForward(useEnd: false, new CodeMatch(
                        //////    OpCodes.Call, 
                        //////    typeof(GameServer).GetMethod(nameof(GameServer.Init), BindingFlags.Static | BindingFlags.Public)))


                        // seems to possibly work
                        .MatchForward(useEnd: false,
                            new CodeMatch(ci => ci.opcode.Equals(OpCodes.Call) && ci.operand.ToString().Contains("Init")))

                        // OR directly load the int
                        //  this will be less resilient to code changes
                        .MatchBack(useEnd: false, new CodeMatch(OpCodes.Ldc_I4_0))
                        //.SetAndAdvance(OpCodes.Ldsfld, typeof(ZSocket2Fix).GetField("m_serverBindAddressIPv4", BindingFlags.Static | BindingFlags.Public))
                        //.SetAndAdvance(OpCodes.Ldsfld, typeof(ZSocket2Fix).GetField(nameof(m_serverBindAddressIPv4), BindingFlags.Static | BindingFlags.Public))
                        .SetAndAdvance(OpCodes.Ldsfld, AccessTools.Field(typeof(ZSocket2Fix), nameof(ZSocket2Fix.m_serverBindAddressIPv4)))
                        //.InsertAndAdvance(new CodeInstruction(OpCodes.Conv_I4)) // Looks like an unsigned is being pushed, so convert instead? weird
                        //nameof(ZSocket2Fix.m_serverBindAddressIPv4)
                        // this is more resistant to code changes; it directly targets the Init() call                        
                        //.SetAndAdvance(OpCodes.Call,
                        //    Transpilers.EmitDelegate<Func<uint, ushort, ushort, ushort, EServerMode, string, bool>>(AwakeDelegate).operand)
                        .InstructionEnumeration();

                    //ZLog.LogWarning("Patched Awake:");

                    //ZSocket2Fix.dump_instructions(patched);

                    return patched;
                }
                return instructions;
            }
        }

        // TODO fix / patch this
        [HarmonyPatch(typeof(ZSocket2))]
        class ZSocket2Patch
        {
            //[HarmonyTranspiler]
            //[HarmonyPatch(nameof(ZSocket2.BindSocket))]
            //static IEnumerable<CodeInstruction> BindSocketTranspiler(IEnumerable<CodeInstruction> instructions)
            //{
            //    // required equivalent code to generate:
            //    //SteamNetworkingIPAddr addr = default;
            //    //addr.SetIPv4(m_serverBindAddressIPv4, (ushort)ZSteamSocket.m_steamDataPort);
            //
            //    // if dedicated server,
            //    //  and if on custom bind
            //    if (ZSocket2Fix.m_serverBindAddressIPv4 != 0)
            //    {
            //        ZLog.LogWarning("Original BindSocket:");
            //        ZSocket2Fix.dump_instructions(instructions);
            //        var patched = new CodeMatcher(instructions)
            //            .MatchForward(useEnd: false, new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(IPAddress), nameof(IPAddress.Any))))
            //            .Set(OpCodes.Ldsfld, AccessTools.Field(typeof(ZSocket2Fix), nameof(ZSocket2Fix.m_serverBindIpAddress)))
            //            .InstructionEnumeration();
            //
            //        ZLog.LogWarning("Patched BindSocket:");
            //        ZSocket2Fix.dump_instructions(patched);
            //
            //        return patched;
            //    }
            //    return instructions;
            //}

            // Just replace the bind for simplicity
            [HarmonyPrefix]
            [HarmonyPatch(nameof(ZSocket2.StartHost))]
            static bool StartHostPrefix(ref bool __result, ref ZSocket2 __instance, int port)
            {                
                if (ZSocket2Fix.m_isDedicated && ZSocket2Fix.m_serverBindAddressIPv4 != 0)
                {
                    try
                    {
                        if (__instance.m_listner != null)
                        {
                            __instance.m_listner.Stop();
                            __instance.m_listner = null;
                        }
                        __instance.m_listner = new TcpListener(ZSocket2Fix.m_serverBindIpAddress, port);
                        __instance.m_listner.Start();
                        __instance.m_listenPort = port;

                        __result = true;
                    }
                    catch {
                        __result = false;
                    }

                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ZSteamSocket))]
        class ZSteamSocketPatch
        {
            [HarmonyTranspiler]
            [HarmonyPatch(nameof(ZSteamSocket.StartHost))]
            static IEnumerable<CodeInstruction> StartHostTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                // required equivalent code to generate:
                //SteamNetworkingIPAddr addr = default;
                //addr.SetIPv4(m_serverBindAddressIPv4, (ushort)ZSteamSocket.m_steamDataPort);

                // if dedicated server,
                //  and if on custom bind
                if (!ZSocket2Fix.m_disableSteamSetting.Value && ZSocket2Fix.m_isDedicated && ZSocket2Fix.m_serverBindAddressIPv4 != 0)
                {
                    //ZLog.LogWarning("Original StartHost:");
                    //ZSocket2Fix.dump_instructions(instructions);
                    var patched = new CodeMatcher(instructions)
                        .MatchForward(useEnd: false, new CodeMatch(ci => ci.opcode.Equals(OpCodes.Stfld) && ci.operand.ToString().Contains(nameof(SteamNetworkingIPAddr.m_port))))
                        //.Advance(-4), 
                        .Advance(-2)
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ZSocket2Fix), nameof(ZSocket2Fix.m_serverBindAddressIPv4))))
                        .Advance(2)
                        // load SetIPv4 method (replacing m_port assignment)
                        .Set(OpCodes.Callvirt, AccessTools.Method(typeof(SteamNetworkingIPAddr), nameof(SteamNetworkingIPAddr.SetIPv4)))
                        .InstructionEnumeration();

                    //ZLog.LogWarning("Patched StartHost:");
                    //ZSocket2Fix.dump_instructions(patched);

                    return patched;
                }
                return instructions;
            }
        }

    }
}
