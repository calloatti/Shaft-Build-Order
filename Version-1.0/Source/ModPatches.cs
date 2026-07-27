using HarmonyLib;
using Timberborn.ConstructionSites;

namespace Calloatti.ShaftBuildOrder
{
  [HarmonyPatch(typeof(ConstructionSite), "ReadyToBuild", MethodType.Getter)]
  public static class ConstructionSite_ReadyToBuild_Patch
  {
    public static void Postfix(ConstructionSite __instance, ref bool __result)
    {
      // If the vanilla game already says it's not ready, leave it alone.
      if (!__result) return;

      ShaftBuildOrderBlocker blocker = __instance.GetComponent<ShaftBuildOrderBlocker>();

      // If it's one of our shafts and it's out of order...
      if (blocker != null && blocker.EnforceBuildOrder && blocker.ShouldBlockBuilders())
      {
        // ...we force the engine to think it's not ready for hammering.
        // Haulers will still see it as a valid delivery target!
        __result = false;
      }
    }
  }
}