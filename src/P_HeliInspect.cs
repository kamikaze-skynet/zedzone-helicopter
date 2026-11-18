using HarmonyLib;

namespace ZedHelicopter;

[HarmonyPatch(typeof(VehicleConditionUI), "Init")]
internal static class P_HeliInspect
{
	private static void Postfix(VehicleConditionUI __instance, VehicleData __0)
	{
		HeliInspect.Apply(__instance, __0, null, "Init");
	}
}
