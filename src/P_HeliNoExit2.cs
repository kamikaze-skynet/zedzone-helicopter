using System;
using HarmonyLib;

namespace ZedHelicopter;

[HarmonyPatch(typeof(HumanCharacterController), "GetOutVehicle", new Type[] { typeof(float) })]
internal static class P_HeliNoExit2
{
	private static bool Prefix(HumanCharacterController __instance)
	{
		return Heli.CanExit(__instance);
	}
}
