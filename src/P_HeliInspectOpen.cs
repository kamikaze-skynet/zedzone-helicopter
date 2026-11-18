using System;
using HarmonyLib;
using UnityEngine;

namespace ZedHelicopter;

[HarmonyPatch(typeof(VehicleInspectionPanel), "OpenPanel")]
internal static class P_HeliInspectOpen
{
	private static void Postfix(VehicleInspectionPanel __instance, BasicVehicle __0)
	{
		try
		{
			VehicleConditionUI vehicleConditionUI = __instance.vehicleConditionUI;
			VehicleData vd = null;
			try
			{
				vd = (((Object)(object)__0 != (Object)null) ? __0.vehicleData : null);
			}
			catch
			{
			}
			if ((Object)(object)vehicleConditionUI != (Object)null)
			{
				HeliInspect.Apply(vehicleConditionUI, vd, __0, "OpenPanel");
			}
			else
			{
				Report.Note("HELI 檢視面板：OpenPanel 時 vehicleConditionUI 是空的");
			}
		}
		catch (Exception ex)
		{
			Report.Note("heli insp open: " + ex.Message);
		}
	}
}
