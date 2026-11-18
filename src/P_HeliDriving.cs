using HarmonyLib;

namespace ZedHelicopter;

[HarmonyPatch(typeof(BasicVehicle), "UpdateDriving")]
internal static class P_HeliDriving
{
	private static bool Prefix(BasicVehicle __instance)
	{
		return !Heli.Airborne(__instance);
	}
}
