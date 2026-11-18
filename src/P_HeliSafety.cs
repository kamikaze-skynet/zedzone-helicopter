using HarmonyLib;

namespace ZedHelicopter;

[HarmonyPatch(typeof(BasicVehicle), "EnforcePhysicsSafety")]
internal static class P_HeliSafety
{
	private static bool Prefix(BasicVehicle __instance)
	{
		return !Heli.Airborne(__instance);
	}
}
