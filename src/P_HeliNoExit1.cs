using System;
using HarmonyLib;

namespace ZedHelicopter;

[HarmonyPatch(typeof(HumanCharacterController), "GetOutVehicle", new Type[] { })]
internal static class P_HeliNoExit1
{
	private static bool Prefix(HumanCharacterController __instance)
	{
		return Heli.CanExit(__instance);
	}
}
