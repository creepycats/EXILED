// -----------------------------------------------------------------------
// <copyright file="StaminaUsage.cs" company="Exiled Team">
// Copyright (c) Exiled Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.Patches.Generic
{
#pragma warning disable SA1313
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection.Emit;

    using API.Features;
    using API.Features.Pools;

    using HarmonyLib;

    using PlayerRoles.Voice;
    using UnityEngine;
    using VoiceChat;
    using VoiceChat.Codec;
    using VoiceChat.Networking;
    using VoiceChat.Playbacks;

    using static HarmonyLib.AccessTools;

    /// <summary>
    /// Patches <see cref="VoiceTransceiver.ServerReceiveMessage"/>.
    /// Implements <see cref="Player.Loudness"/>, using <see cref="StandardVoiceModule.GlobalChatLoudness"/> and <see cref="VoiceMessage"/>.
    /// </summary>
    [HarmonyPatch(typeof(VoiceTransceiver), nameof(VoiceTransceiver.ServerReceiveMessage))]
    internal static class PlayerVolume
    {
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

            Label skip = generator.DefineLabel();
            Label loopcheck = generator.DefineLabel();
            Label loopstart = generator.DefineLabel();

            LocalBuilder plr = generator.DeclareLocal(typeof(Player));
            LocalBuilder svm = generator.DeclareLocal(typeof(StandardVoiceModule));
            LocalBuilder decoded = generator.DeclareLocal(typeof(float[]));
            LocalBuilder pos = generator.DeclareLocal(typeof(int));

            const int offset = 4;
            int index = newInstructions.FindIndex(i => i.Calls(Method(typeof(VoiceModuleBase), nameof(VoiceModuleBase.ValidateSend), new[] { typeof(VoiceChatChannel) }))) + offset;

            newInstructions[index].WithLabels(skip);

            newInstructions.InsertRange(index, new List<CodeInstruction>()
            {
                // Player plr = Player.Get(msg.Speaker)
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldfld, Field(typeof(VoiceMessage), nameof(VoiceMessage.Speaker))),
                new(OpCodes.Call, Method(typeof(Player), nameof(Player.Get), new[] { typeof(ReferenceHub) })),
                new(OpCodes.Stloc_S, plr),

                // if (plr.VoiceModule is StandardVoiceModule svm)
                new(OpCodes.Ldloc_S, plr),
                new(OpCodes.Callvirt, PropertyGetter(typeof(Player), nameof(Player.VoiceModule))),
                new(OpCodes.Isinst, typeof(StandardVoiceModule)),
                new(OpCodes.Stloc_S, svm),
                new(OpCodes.Ldloc_S, svm),
                new(OpCodes.Brfalse_S, skip),

                // float[] decoded = new float[48000]
                new(OpCodes.Ldc_I4, 48000),
                new(OpCodes.Newarr, typeof(float)),
                new(OpCodes.Stloc_S, decoded),

                // plr.VoiceModule.Decoder.Decode(msg.Data, msg.DataLength, decoded);
                new(OpCodes.Ldloc_S, plr),
                new(OpCodes.Callvirt, PropertyGetter(typeof(Player), nameof(Player.VoiceModule))),
                new(OpCodes.Callvirt, PropertyGetter(typeof(VoiceModuleBase), nameof(VoiceModuleBase.Decoder))),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldfld, Field(typeof(VoiceMessage), nameof(VoiceMessage.Data))),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldfld, Field(typeof(VoiceMessage), nameof(VoiceMessage.DataLength))),
                new(OpCodes.Ldloc_S, decoded),
                new(OpCodes.Callvirt, Method(typeof(OpusDecoder), nameof(OpusDecoder.Decode), new[] { typeof(byte[]), typeof(int), typeof(float[]) })),
                new(OpCodes.Pop),

                // for (int i = 0; i < array.Length; i++)
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Stloc_S, pos),
                // decoded[i] = Mathf.Abs(decoded[i])
                new(OpCodes.Br_S, loopcheck),

                // loop start
                new(OpCodes.Ldloc_S, decoded),
                new(OpCodes.Ldloc_S, pos),
                new(OpCodes.Ldloc_S, decoded),
                new(OpCodes.Ldloc_S, pos),
                new(OpCodes.Ldelem_R4),
                new(OpCodes.Call, Method(typeof(Mathf), nameof(Mathf.Abs), new[] { typeof(float) })),
                new(OpCodes.Stelem_R4),

                // i++
                new(OpCodes.Ldloc_S, pos),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Add),
                new(OpCodes.Stloc_S, pos),

                // i < array.Length
                new(OpCodes.Ldloc_S, pos),
                new(OpCodes.Ldloc_S, decoded),
                new(OpCodes.Ldlen),
                new(OpCodes.Conv_I4),
                new(OpCodes.Blt_S, loopstart),

                // end loop

                // standardVoiceModule.GlobalPlayback.Loudness = array.Sum() / (float)msg.DataLength;
                new(OpCodes.Ldloc_S, svm),
                new(OpCodes.Ldfld, Field(typeof(StandardVoiceModule), nameof(StandardVoiceModule.GlobalPlayback))),
                new(OpCodes.Ldloc_S, decoded),
                new(OpCodes.Call, Method(typeof(Enumerable), nameof(Enumerable.Sum), new[] { typeof(IEnumerable<float>) })),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldfld, Field(typeof(VoiceMessage), nameof(VoiceMessage.DataLength))),
                new(OpCodes.Conv_R4),
                new(OpCodes.Div),
                new(OpCodes.Callvirt, PropertySetter(typeof(VoiceChatPlaybackBase), nameof(VoiceChatPlaybackBase.Loudness))),
            });

            newInstructions[index + 26].WithLabels(loopstart);
            newInstructions[index + 37].WithLabels(loopcheck);

            foreach (CodeInstruction instruction in newInstructions)
                yield return instruction;

            ListPool<CodeInstruction>.Pool.Return(newInstructions);
        }
    }
}