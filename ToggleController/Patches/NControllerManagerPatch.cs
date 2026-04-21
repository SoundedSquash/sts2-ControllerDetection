using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace SaveManager.Patches;

[HarmonyPatch(typeof(NControllerManager))]
public class NControllerManagerPatch
{
    [HarmonyPatch(nameof(NControllerManager._Input))]
    [HarmonyPrefix]
    static bool _InputPatch(InputEvent inputEvent, NControllerManager __instance)
    {
        if (__instance.IsUsingController) return true;
        
        // Simulate a controller button and have it do its normal controller checks/updates.
        inputEvent = new InputEventAction()
        {
            Action = Controller.AllControllerInputs.FirstOrDefault(),
            Pressed = true
        };

        var method = AccessTools.Method(typeof(NControllerManager), "CheckForControllerInput");
        method.Invoke(__instance, [inputEvent]);
        return true;
    }
}
