using HarmonyLib;
using Timberborn.ModManagerScene;
using UnityEngine;

namespace Calloatti.ShaftBuildOrder
{
  public class ModStarter : IModStarter
  {
    public void StartMod(IModEnvironment modEnvironment)
    {
      new Harmony("Calloatti.ShaftBuildOrder").PatchAll();
      Debug.Log("[ShaftBuildOrder] Loaded successfully.");
    }
  }
}