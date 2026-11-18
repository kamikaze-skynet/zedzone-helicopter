using HarmonyLib;

namespace ZedHelicopter;

[HarmonyPatch(typeof(HumanCharacterController), "OnMouseDown_Leven")]
internal static class P_HeliMouseDown
{
	private static bool Prefix(HumanCharacterController __instance)
	{
		if (Heli.Piloting || Heli.Gunning)
		{
			return !((BasicCharacterController)__instance).isPlayer;
		}
		return true;
	}
}
