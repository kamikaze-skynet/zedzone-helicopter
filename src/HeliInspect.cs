using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.UI;

namespace ZedHelicopter;

internal static class HeliInspect
{
	private class Orig
	{
		public RectTransform Rt;

		public Image Img;

		public Sprite Sprite;

		public Vector3 Pos;

		public Vector2 Pivot;

		public Vector2 Size;

		public Vector3 Scale;

		public bool Aspect;

		public bool Ray;
	}

	private static readonly Dictionary<IntPtr, Orig> _orig = new Dictionary<IntPtr, Orig>();

	private static readonly Dictionary<string, Sprite> _sp = new Dictionary<string, Sprite>();

	private static bool _applied;

	private static int _log;

	private const float VW = 360f;

	private const float VH = 560f;

	private static IntPtr _polled;

	private static float _pollAt;

	private static GameObject _hub;

	private static int _callLog;

	private static Sprite Sp(string name)
	{
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Expected O, but got Unknown
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		if (_sp.TryGetValue(name, out var value))
		{
			return value;
		}
		value = null;
		try
		{
			using Stream stream = typeof(HeliInspect).Assembly.GetManifestResourceStream("art." + name + ".png");
			if (stream != null)
			{
				byte[] array = new byte[stream.Length];
				int num;
				for (int i = 0; i < array.Length; i += num)
				{
					num = stream.Read(array, i, array.Length - i);
					if (num <= 0)
					{
						break;
					}
				}
				Texture2D val = new Texture2D(2, 2, (TextureFormat)4, false);
				((Object)val).hideFlags = (HideFlags)61;
				ImageConversion.LoadImage(val, ((Il2CppStructArray<byte>)(array)));
				((Texture)val).filterMode = (FilterMode)0;
				value = Sprite.Create(val, new Rect(0f, 0f, (float)((Texture)val).width, (float)((Texture)val).height), new Vector2(0.5f, 0.5f), 100f);
				((Object)value).hideFlags = (HideFlags)61;
			}
		}
		catch (Exception ex)
		{
			Report.Note("heli insp sprite: " + ex.Message);
		}
		_sp[name] = value;
		return value;
	}

	private static Image ImgOf(VehiclePartConditionUI ui)
	{
		try
		{
			if ((Object)(object)ui == (Object)null)
			{
				return null;
			}
			return ((Component)ui).GetComponent<Image>() ?? ((Component)ui).GetComponentInChildren<Image>(true);
		}
		catch
		{
			return null;
		}
	}

	private static RectTransform RtOf(Component c)
	{
		try
		{
			return ((Object)(object)c != (Object)null) ? c.GetComponent<RectTransform>() : null;
		}
		catch
		{
			return null;
		}
	}

	private static void Remember(RectTransform rt, Image img)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)rt == (Object)null) && !_orig.ContainsKey(((Il2CppObjectBase)rt).Pointer))
		{
			Dictionary<IntPtr, Orig> orig = _orig;
			IntPtr pointer = ((Il2CppObjectBase)rt).Pointer;
			Orig obj = new Orig
			{
				Rt = rt,
				Img = img,
				Sprite = (((Object)(object)img != (Object)null) ? img.sprite : null),
				Pos = ((Transform)rt).localPosition,
				Pivot = rt.pivot
			};
			Rect rect = rt.rect;
			obj.Size = rect.size;
			obj.Scale = ((Transform)rt).localScale;
			obj.Aspect = (Object)(object)img != (Object)null && img.preserveAspect;
			obj.Ray = (Object)(object)img == (Object)null || ((Graphic)img).raycastTarget;
			orig[pointer] = obj;
		}
	}

	private static void Restore()
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		if (!_applied)
		{
			return;
		}
		foreach (Orig value in _orig.Values)
		{
			try
			{
				if (!((Object)(object)value.Rt == (Object)null))
				{
					value.Rt.pivot = value.Pivot;
					value.Rt.SetSizeWithCurrentAnchors((RectTransform.Axis)0, value.Size.x);
					value.Rt.SetSizeWithCurrentAnchors((RectTransform.Axis)1, value.Size.y);
					((Transform)value.Rt).localPosition = value.Pos;
					((Transform)value.Rt).localScale = value.Scale;
					if ((Object)(object)value.Img != (Object)null)
					{
						value.Img.sprite = value.Sprite;
						value.Img.preserveAspect = value.Aspect;
						((Graphic)value.Img).raycastTarget = value.Ray;
					}
				}
			}
			catch
			{
			}
		}
		if ((Object)(object)_hub != (Object)null)
		{
			try
			{
				Object.Destroy((Object)(object)_hub);
			}
			catch
			{
			}
			_hub = null;
		}
		_applied = false;
	}

	public static void Poll()
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (realtimeSinceStartup < _pollAt)
		{
			return;
		}
		_pollAt = realtimeSinceStartup + 0.2f;
		try
		{
			VehicleInspectionPanel instance = VehicleInspectionPanel._instance;
			if ((Object)(object)instance == (Object)null || !((Component)instance).gameObject.activeInHierarchy)
			{
				_polled = IntPtr.Zero;
				return;
			}
			BasicVehicle vehicle = instance.vehicle;
			if (!((Object)(object)vehicle == (Object)null) && (!(((Il2CppObjectBase)vehicle).Pointer == _polled) || (!_applied && Heli.IsHeli(vehicle))))
			{
				_polled = ((Il2CppObjectBase)vehicle).Pointer;
				VehicleConditionUI vehicleConditionUI = instance.vehicleConditionUI;
				if ((Object)(object)vehicleConditionUI != (Object)null)
				{
					Apply(vehicleConditionUI, null, vehicle, "Poll");
				}
			}
		}
		catch
		{
		}
	}

	private static void Hub(RectTransform parent, float size)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)_hub != (Object)null)
		{
			try
			{
				Object.Destroy((Object)(object)_hub);
			}
			catch
			{
			}
		}
		_hub = new GameObject("zpi_rotor_hit");
		RectTransform obj2 = _hub.AddComponent<RectTransform>();
		((Transform)obj2).SetParent((Transform)(object)parent, false);
		Vector2 val = default(Vector2);
		val = new Vector2(0.5f, 0.5f);
		obj2.pivot = val;
		Vector2 anchorMin = (obj2.anchorMax = val);
		obj2.anchorMin = anchorMin;
		obj2.sizeDelta = new Vector2(size, size);
		((Transform)obj2).localPosition = Vector3.zero;
		Image obj3 = _hub.AddComponent<Image>();
		((Graphic)obj3).color = new Color(1f, 1f, 1f, 0f);
		((Graphic)obj3).raycastTarget = true;
	}

	public static void Apply(VehicleConditionUI ui, VehicleData vd, BasicVehicle v, string from)
	{
		//IL_04f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_054e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0555: Unknown result type (might be due to invalid IL or missing references)
		//IL_055d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0564: Unknown result type (might be due to invalid IL or missing references)
		//IL_056e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0578: Unknown result type (might be due to invalid IL or missing references)
		//IL_0584: Unknown result type (might be due to invalid IL or missing references)
		//IL_0589: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0596: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0697: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_060e: Unknown result type (might be due to invalid IL or missing references)
		//IL_063a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0641: Unknown result type (might be due to invalid IL or missing references)
		//IL_0653: Unknown result type (might be due to invalid IL or missing references)
		//IL_065d: Unknown result type (might be due to invalid IL or missing references)
		//IL_034e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_0357: Unknown result type (might be due to invalid IL or missing references)
		//IL_035c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_0365: Unknown result type (might be due to invalid IL or missing references)
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		//IL_036e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0372: Unknown result type (might be due to invalid IL or missing references)
		//IL_037e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Unknown result type (might be due to invalid IL or missing references)
		//IL_038f: Unknown result type (might be due to invalid IL or missing references)
		//IL_039b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
		bool flag = false;
		try
		{
			flag = vd != null && vd.vehicleNameTextId == 88030;
		}
		catch
		{
		}
		if (!flag)
		{
			try
			{
				if ((Object)(object)v == (Object)null)
				{
					VehicleInspectionPanel instance = VehicleInspectionPanel._instance;
					v = ((instance != null) ? instance.vehicle : null);
				}
				flag = Heli.IsHeli(v);
			}
			catch
			{
			}
		}
		if (_callLog++ < 6)
		{
			int value = -1;
			try
			{
				value = ((vd != null) ? vd.vehicleNameTextId : (-1));
			}
			catch
			{
			}
			Report.Note($"HELI 檢視面板（{from}）：資料名稱ID={value} 車子={(Object)(object)v != (Object)null} 直升機={flag}");
		}
		if (!flag)
		{
			Restore();
			return;
		}
		try
		{
			List<(VehiclePartConditionUI, string, float, float, float, float, bool)> list = new List<(VehiclePartConditionUI, string, float, float, float, float, bool)>
			{
				(ui.bodyworkUI, "insp_body", 180f, 280f, 240f, 456f, false),
				(ui.transmisionUI, "insp_rotor", 180f, 180f, 360f, 360f, false),
				(ui.engineUI, "insp_engine", 180f, 236f, 88f, 68f, false),
				(ui.fueltankUI, "insp_fuel", 180f, 298f, 92f, 52f, false),
				(ui.batteryUI, "insp_battery", 180f, 106f, 36f, 26f, false),
				(ui.headlight_Left_UI, "insp_light", 148f, 80f, 28f, 28f, false),
				(ui.headlight_Right_UI, "insp_light", 212f, 80f, 28f, 28f, false),
				(ui.tire_frontLeft_UI, "insp_skid", 72f, 150f, 32f, 116f, false),
				(ui.tire_frontRight_UI, "insp_skid", 288f, 150f, 32f, 116f, false),
				(ui.tire_rearLeft_UI, "insp_skid", 72f, 264f, 32f, 116f, false),
				(ui.tire_rearRight_UI, "insp_skid", 288f, 264f, 32f, 116f, false)
			};
			RectTransform val = RtOf((Component)(object)ui.bodyworkUI);
			if ((Object)(object)val == (Object)null)
			{
				return;
			}
			Restore();
			foreach (var item in list)
			{
				Remember(RtOf((Component)(object)item.Item1), ImgOf(item.Item1));
			}
			Image fuelTankCapacityImage = ui.fuelTankCapacityImage;
			if ((Object)(object)fuelTankCapacityImage != (Object)null)
			{
				Remember(RtOf((Component)(object)fuelTankCapacityImage), fuelTankCapacityImage);
			}
			Rect rect = val.rect;
			Vector2 size = rect.size;
			Vector3 localPosition = ((Transform)val).localPosition;
			Vector2 pivot = val.pivot;
			Vector2 val2 = default(Vector2);
			val2 = new Vector2(localPosition.x + (0.5f - pivot.x) * size.x, localPosition.y + (0.5f - pivot.y) * size.y);
			float num = size.y / 456f;
			Transform parent = ((Transform)val).parent;
			StringBuilder stringBuilder = new StringBuilder();
			Vector2 val6 = default(Vector2);
			foreach (var item2 in list)
			{
				RectTransform val3 = RtOf((Component)(object)item2.Item1);
				Image val4 = ImgOf(item2.Item1);
				if ((Object)(object)val3 == (Object)null)
				{
					continue;
				}
				if ((Object)(object)((Transform)val3).parent != (Object)(object)parent && _log < 1)
				{
					StringBuilder stringBuilder2 = stringBuilder;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
					handler.AppendLiteral("[");
					handler.AppendFormatted(item2.Item2);
					handler.AppendLiteral(" 父物件不同] ");
					stringBuilder2.Append(ref handler);
				}
				Sprite val5 = Sp(item2.Item2);
				if ((Object)(object)val4 != (Object)null && (Object)(object)val5 != (Object)null)
				{
					val4.sprite = val5;
					val4.preserveAspect = false;
				}
				if (item2.Item2 == "insp_rotor" && (Object)(object)val4 != (Object)null)
				{
					try
					{
						((Graphic)val4).raycastTarget = false;
						Hub(val3, 90f * num);
					}
					catch (Exception ex)
					{
						Report.Note("heli insp hub: " + ex.Message);
					}
				}
				val3.pivot = new Vector2(0.5f, 0.5f);
				val3.SetSizeWithCurrentAnchors((RectTransform.Axis)0, item2.Item5 * num);
				val3.SetSizeWithCurrentAnchors((RectTransform.Axis)1, item2.Item6 * num);
				val6 = new Vector2((item2.Item3 - 180f) * num, (0f - (item2.Item4 - 280f)) * num);
				((Transform)val3).localPosition = new Vector3(val2.x + val6.x, val2.y + val6.y, ((Transform)val3).localPosition.z);
				Vector3 localScale = ((Transform)val3).localScale;
				((Transform)val3).localScale = new Vector3(item2.Item7 ? (0f - Mathf.Abs(localScale.x)) : Mathf.Abs(localScale.x), localScale.y, localScale.z);
			}
			if ((Object)(object)fuelTankCapacityImage != (Object)null)
			{
				RectTransform val7 = RtOf((Component)(object)fuelTankCapacityImage);
				if ((Object)(object)val7 != (Object)null)
				{
					val7.pivot = new Vector2(0.5f, 0.5f);
					val7.SetSizeWithCurrentAnchors((RectTransform.Axis)0, 70f * num);
					val7.SetSizeWithCurrentAnchors((RectTransform.Axis)1, 10f * num);
					((Transform)val7).localPosition = new Vector3(val2.x, val2.y - 18f * num, ((Transform)val7).localPosition.z);
				}
			}
			_applied = true;
			if (_log++ < 2)
			{
				Report.Note($"HELI 檢視示意圖換成直升機：車架 {size.x:0}x{size.y:0} @({val2.x:0},{val2.y:0})，比例 {num:0.##}｜油量條={(Object)(object)fuelTankCapacityImage != (Object)null} {stringBuilder}");
			}
		}
		catch (Exception ex2)
		{
			Report.Note("heli insp: " + ex2.Message);
		}
	}
}
