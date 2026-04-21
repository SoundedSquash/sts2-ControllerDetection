using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace SaveManager.Patches;

[HarmonyPatch(typeof(NControllerManager))]
public static class NControllerManagerPatch
{
    private static SceneTreeTimer? _blockTimer;
    private static bool _hasTriggeredOnce;

    private const float InitialSettleDelay = 1.1f;
    
    [HarmonyPatch("CheckForMouseInput")]
    [HarmonyPrefix]
    static bool CheckForMouseInputPrefix(ref InputEvent inputEvent)
    {
        if (_blockTimer != null && _blockTimer.TimeLeft > 0)
        {
            return false; 
        }

        return true;
    }
    
    [HarmonyPatch("ControlModeChanged")]
    [HarmonyPostfix]
    static void EmitSignalControllerDetectedPostfix(NControllerManager __instance)
    {
        _hasTriggeredOnce = false;
        TriggerInitialSettle(__instance);
    }
    
    private static void TriggerInitialSettle(NControllerManager instance)
    {
        if (!_hasTriggeredOnce)
        {
            _blockTimer = instance.GetTree().CreateTimer(InitialSettleDelay);
            _hasTriggeredOnce = true;
        }
    }
}