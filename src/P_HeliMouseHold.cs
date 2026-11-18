using HarmonyLib;

namespace ZedHelicopter;

[HarmonyPatch(typeof(HumanCharacterController), "OnMouseHold_Leven")]
internal static class P_HeliMouseHold
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
