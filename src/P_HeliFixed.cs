using HarmonyLib;

namespace ZedHelicopter;

[HarmonyPatch(typeof(BasicVehicle), "FixedUpdate")]
internal static class P_HeliFixed
{
	private static bool Prefix(BasicVehicle __instance)
	{
		return !Heli.Airborne(__instance);
	}
}
