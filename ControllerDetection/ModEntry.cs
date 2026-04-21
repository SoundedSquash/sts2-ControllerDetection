using ControllerDetection.SoftDependencies;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

[ModInitializer("Initialize")]
public class ModEntry
{
    public static void Initialize()
    {
        var harmony = new Harmony("soundedsquash.controllerdetection");
        harmony.PatchAll();

        ModConfigBridge.DeferredRegister();
    }
}