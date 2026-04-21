using ControllerDetection.SoftDependencies;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace SaveManager.Patches;

[HarmonyPatch(typeof(NControllerManager))]
public static class NControllerManagerPatch
{
    private static SceneTreeTimer? _blockTimer;
    private static bool _hasTriggeredOnce;

    public static float InitialSettleDelay = ModConfigBridge.GetValue("delay", 1.1f);
    
    [HarmonyPatch("CheckForMouseInput")]
    [HarmonyPrefix]
    static bool CheckForMouseInputPrefix(ref InputEvent inputEvent)
    {
        // Block mouse check if timer is running
        if (_blockTimer?.TimeLeft > 0)
        {
            return false; 
        }

        return true;
    }
    
    [HarmonyPatch("ControlModeChanged")]
    [HarmonyPostfix]
    static void ControlModeChangedPostfix(NControllerManager __instance)
    {
        // Start timer on input change
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