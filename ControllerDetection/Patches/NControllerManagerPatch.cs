using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace SaveManager.Patches;

[HarmonyPatch(typeof(NControllerManager))]
public static class NControllerManagerPatch
{
    [HarmonyPatch("CheckForMouseInput")]
    [HarmonyPrefix]
    static bool CheckForMouseInputPatch(ref InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            // Prevent mouse movement checks
            case InputEventMouseMotion:
                return false;
            // Replace key press with mouse press to trigger input change.
            case InputEventKey:
                inputEvent = new InputEventMouseButton();
                break;
        }

        return true;
    }
}