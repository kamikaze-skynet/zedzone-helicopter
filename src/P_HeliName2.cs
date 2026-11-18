using System;
using HarmonyLib;

namespace ZedHelicopter;

[HarmonyPatch(typeof(TextManager), "GetText", new Type[]
{
	typeof(int),
	typeof(GameLanguage)
})]
internal static class P_HeliName2
{
	private static void Postfix(int textid, ref string __result)
	{
		if (textid == 88030)
		{
			__result = L.T("直升機");
		}
	}
}
