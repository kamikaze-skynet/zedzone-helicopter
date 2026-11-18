using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ZedHelicopter;

internal static partial class Heli
{
	private class Vis
	{
		public GameObject Root;

		public GameObject ShadowGo;

		public SpriteRenderer Body;

		public SpriteRenderer Shadow;

		public SpriteRenderer Rotor;

		public SpriteRenderer Rotor2;

		// Door miniguns: [0] left, [1] right. GunRel is the aim relative to the nose, in degrees.
		public SpriteRenderer[] Gun = new SpriteRenderer[2];

		public float[] GunRel = new float[2] { 90f, -90f };

		public float RotorAng;

		public float RotorSpeed;

		public AudioSource Sound;

		public readonly List<Renderer> Hidden = new List<Renderer>();

		public float NextHide;
	}

	private class Air
	{
		public float Alt;

		public Vector2 Vel;

		public int Ph;

		public readonly List<Collider2D> Off = new List<Collider2D>();

		public RigidbodyType2D BodyType;

		public float Heading;

		public Behaviour Handler;
	}

	private class Fx
	{
		public GameObject Go;

		public float Until;
	}

	public const int NameId = 88030;

	private const float Scale = 1.3f;

	private const float Lift = 1.8f;

	private static readonly int[] AmmoIds = new int[2] { 102, 104 };

	private static readonly CultureInfo CI = CultureInfo.InvariantCulture;

	private static readonly Dictionary<IntPtr, Vis> _vis = new Dictionary<IntPtr, Vis>();

	private static readonly Dictionary<IntPtr, Air> _air = new Dictionary<IntPtr, Air>();

	private static readonly HashSet<string> _pads = new HashSet<string>();

	private static readonly HashSet<string> _mil = new HashSet<string>();

	private static readonly List<(Vector2 pos, float fuelL, float ang)> _pending = new List<(Vector2, float, float)>();

	private static int _called;

	private static long _saveId = -1L;

	private static bool _dirty;

	private static float _nextSave;

	private static float _scanAt;

	private static float _noiseAt;

	private static float _fireAt;

	private static float _msgUntil;

	private static float _calibAt;

	private static float _fwd = 0f;

	private static bool _fwdKnown;

	private static IntPtr _gdPtr;

	private static int _baseId = -1;

	private static float _migrateWait;

	private static float _padWait;

	private static int _ver;

	private static int _visLog;

	private static readonly HashSet<string> _verify = new HashSet<string>();

	private static Sprite _body;

	private static Sprite _rotor;

	private static Sprite _white;

	private static bool _spriteTried;

	private static int _sortLayer;

	private static bool _sortSet;

	public static string NearStatus;

	private static string _msg;

	private static BasicVehicle _mine;

	private static readonly List<Fx> _fx = new List<Fx>();

	private static readonly List<Vector2> _savedAt = new List<Vector2>();

	private static int _spawnLog;

	private static readonly List<Vector2> _known = new List<Vector2>();

	private static readonly Dictionary<IntPtr, int> _knownIdx = new Dictionary<IntPtr, int>();

	private static int _retagLog;

	private static int _walkLog;

	private static int _calibLog;

	private static int _calibTry;

	private static int _noLoadLog;

	private static readonly HashSet<string> _typeLogged = new HashSet<string>();

	private static int _hitLog;

	public static bool Piloting => (Object)(object)_mine != (Object)null;

	private static string FilePath => Path.Combine(Paths.ConfigPath, $"Helicopter.{_saveId}.txt");

	private static string OldFilePath => Path.Combine(Paths.ConfigPath, $"ZedPowerIndustry.heli.{_saveId}.txt");

	private static string Key(Vector2 p)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		return $"{Mathf.Round(p.x):0},{Mathf.Round(p.y):0}";
	}

	private static void Msg(string s)
	{
		_msg = s;
		_msgUntil = Time.realtimeSinceStartup + 4f;
	}

	public static string Message()
	{
		if (!(Time.realtimeSinceStartup < _msgUntil))
		{
			return null;
		}
		return _msg;
	}

	private static float F(string s)
	{
		return float.Parse(s, NumberStyles.Float, CI);
	}

	public static bool IsHeli(BasicVehicle v)
	{
		try
		{
			if ((Object)(object)v == (Object)null)
			{
				return false;
			}
			if (v.vehicleNameTextId == 88030)
			{
				return true;
			}
			VehicleData vehicleData = v.vehicleData;
			return vehicleData != null && vehicleData.vehicleNameTextId == 88030;
		}
		catch
		{
			return false;
		}
	}

	public static bool Airborne(BasicVehicle v)
	{
		try
		{
			return (Object)(object)v != (Object)null && _air.ContainsKey(((Il2CppObjectBase)v).Pointer);
		}
		catch
		{
			return false;
		}
	}

	public static bool CanExit(HumanCharacterController h)
	{
		try
		{
			BasicVehicle drivingVehicle = h.drivingVehicle;
			if ((Object)(object)drivingVehicle != (Object)null && Airborne(drivingVehicle))
			{
				if (((BasicCharacterController)h).isPlayer)
				{
					Msg(L.T("空中不能下機，先按空白鍵降落"));
				}
				return false;
			}
		}
		catch
		{
		}
		return true;
	}

	private static void Load()
	{
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0276: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		_called = 0;
		_pads.Clear();
		_mil.Clear();
		_pending.Clear();
		_verify.Clear();
		_savedAt.Clear();
		_ver = 0;
		ClearVis();
		_air.Clear();
		_mine = null;
		_known.Clear();
		_knownIdx.Clear();
		try
		{
			string path = File.Exists(FilePath) ? FilePath : OldFilePath;
			if (!File.Exists(path))
			{
				return;
			}
			string[] array = File.ReadAllLines(path);
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split('|');
				if (array2[0] == "H" && array2.Length >= 8)
				{
					_pending.Add((new Vector2(F(array2[2]), F(array2[3])), F(array2[5]) / 1000f, F(array2[4])));
				}
				else if (array2[0] == "P" && array2.Length >= 2)
				{
					_pads.Add(array2[1]);
				}
				else if (array2[0] == "M" && array2.Length >= 2)
				{
					_mil.Add(array2[1]);
				}
				else if (array2[0] == "R" && array2.Length >= 2)
				{
					_fwd = F(array2[1]);
					_fwdKnown = true;
				}
				else if (array2[0] == "K" && array2.Length >= 3)
				{
					_known.Add(new Vector2(F(array2[1]), F(array2[2])));
				}
				else if (array2[0] == "S" && array2.Length >= 3)
				{
					_savedAt.Add(new Vector2(F(array2[1]), F(array2[2])));
				}
				else if (array2[0] == "C" && array2.Length >= 2)
				{
					_called = int.Parse(array2[1]);
				}
				else if (array2[0] == "V" && array2.Length >= 2)
				{
					_ver = int.Parse(array2[1]);
				}
			}
			if (_ver < 2)
			{
				foreach (string pad in _pads)
				{
					_verify.Add(pad);
				}
				_ver = 2;
				_dirty = true;
			}
			foreach (Vector2 item in _savedAt)
			{
				bool flag = false;
				foreach (Vector2 item2 in _known)
				{
					Vector2 val = item2 - item;
					if (val.sqrMagnitude < 1f)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					_known.Add(item);
				}
			}
			if (_pending.Count > 0)
			{
				Report.Note($"HELI 有 {_pending.Count} 架舊版直升機，等區塊載入後換成車輛");
			}
		}
		catch (Exception ex)
		{
			Report.Note("heli load: " + ex.Message);
		}
	}

	private static void Save()
	{
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (var item in _pending)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 4, stringBuilder2);
				handler.AppendLiteral("H|0|");
				handler.AppendFormatted(item.pos.x.ToString("0.##", CI));
				handler.AppendLiteral("|");
				handler.AppendFormatted(item.pos.y.ToString("0.##", CI));
				handler.AppendLiteral("|");
				handler.AppendFormatted(item.ang.ToString("0", CI));
				handler.AppendLiteral("|");
				handler.AppendFormatted((item.fuelL * 1000f).ToString("0", CI));
				handler.AppendLiteral("|0|old");
				stringBuilder3.AppendLine(ref handler);
			}
			foreach (string pad in _pads)
			{
				stringBuilder.AppendLine("P|" + pad);
			}
			foreach (string item2 in _mil)
			{
				stringBuilder.AppendLine("M|" + item2);
			}
			if (_fwdKnown)
			{
				stringBuilder.AppendLine("R|" + _fwd.ToString("0", CI));
			}
			stringBuilder.AppendLine("C|" + _called);
			stringBuilder.AppendLine("V|" + _ver);
			List<Vector2> list = new List<Vector2>();
			foreach (Vector2 item3 in _known)
			{
				bool flag = false;
				foreach (Vector2 item4 in list)
				{
					Vector2 val = item4 - item3;
					if (val.sqrMagnitude < 1f)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					list.Add(item3);
					StringBuilder stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder4 = stringBuilder2;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(3, 2, stringBuilder2);
					handler.AppendLiteral("K|");
					handler.AppendFormatted(item3.x.ToString("0.##", CI));
					handler.AppendLiteral("|");
					handler.AppendFormatted(item3.y.ToString("0.##", CI));
					stringBuilder4.AppendLine(ref handler);
				}
			}
			foreach (Vector2 item5 in _savedAt)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder5 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(3, 2, stringBuilder2);
				handler.AppendLiteral("S|");
				handler.AppendFormatted(item5.x.ToString("0.##", CI));
				handler.AppendLiteral("|");
				handler.AppendFormatted(item5.y.ToString("0.##", CI));
				stringBuilder5.AppendLine(ref handler);
			}
			File.WriteAllText(FilePath, stringBuilder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			_dirty = false;
		}
		catch (Exception ex)
		{
			Report.Note("heli save: " + ex.Message);
		}
	}

	public static void OnGameSave()
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (_saveId < 0 || !HeliPlugin.HeliEnabled.Value)
			{
				return;
			}
			var s_liveVehicles = BasicVehicle.s_liveVehicles;
			if (s_liveVehicles == null)
			{
				return;
			}
			_savedAt.Clear();
			for (int i = 0; i < s_liveVehicles.Count; i++)
			{
				BasicVehicle val = s_liveVehicles[i];
				if ((Object)(object)val != (Object)null && IsHeli(val))
				{
					_savedAt.Add(VehPos(val));
				}
			}
			Save();
			Report.Note($"HELI 存檔當下記下 {_savedAt.Count} 架直升機的位置");
		}
		catch (Exception ex)
		{
			Report.Note("heli onsave: " + ex.Message);
		}
	}

	private static int BaseId()
	{
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Invalid comparison between Unknown and I4
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		if (_baseId >= 0)
		{
			return _baseId;
		}
		GameController instance = GameController.instance;
		StringBuilder stringBuilder = new StringBuilder();
		int num = -1;
		float num2 = -1f;
		int num3 = 64;
		try
		{
			if (instance.vehiclePrefabs != null)
			{
				num3 = Mathf.Max(((Il2CppArrayBase<GameObject>)(object)instance.vehiclePrefabs).Length, 1);
			}
		}
		catch
		{
		}
		for (int i = 0; i < num3 + 4; i++)
		{
			try
			{
				if (!instance.IsValidVehicleId(i))
				{
					continue;
				}
				BasicVehicle vehiclePrefabConfig = instance.GetVehiclePrefabConfig(i);
				if (!((Object)(object)vehiclePrefabConfig == (Object)null))
				{
					string value = null;
					try
					{
						TextManager instance2 = TextManager.instance;
						value = ((instance2 != null) ? instance2.GetText(vehiclePrefabConfig.vehicleNameTextId) : null);
					}
					catch
					{
					}
					Vector2Int inventorySize = vehiclePrefabConfig.inventorySize;
					float num4 = (float)(inventorySize.x * inventorySize.y) + (float)vehiclePrefabConfig.passengerSeatLimit * 2f;
					bool num5 = (int)vehiclePrefabConfig.powerType == 0;
					if (stringBuilder.Length < 900)
					{
						StringBuilder stringBuilder2 = stringBuilder;
						StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 6, stringBuilder2);
						handler.AppendFormatted(i);
						handler.AppendLiteral("「");
						handler.AppendFormatted(value);
						handler.AppendLiteral("」貨艙");
						handler.AppendFormatted(inventorySize.x);
						handler.AppendLiteral("x");
						handler.AppendFormatted(inventorySize.y);
						handler.AppendLiteral(" 座");
						handler.AppendFormatted(vehiclePrefabConfig.passengerSeatLimit);
						handler.AppendLiteral(" ");
						handler.AppendFormatted<VehiclePowerType>(vehiclePrefabConfig.powerType);
						handler.AppendLiteral("；");
						stringBuilder2.Append(ref handler);
					}
					if (num5 && i != GameController.forkliftVehicleId && num4 > num2)
					{
						num2 = num4;
						num = i;
					}
				}
			}
			catch
			{
			}
		}
		Report.Note($"HELI 車款：{stringBuilder}→ 選用 {num}");
		_baseId = num;
		return num;
	}

	private static BasicChunk ChunkAt(Vector2 pos)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			MapController instance = MapController.instance;
			return instance.GetChunkByCoordinate(instance.GetChunkCoordinateByPosition(pos));
		}
		catch
		{
			return null;
		}
	}

	private static int Dedupe(ChunkData cd, Vector2 pos)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		try
		{
			var val = ((cd != null) ? cd.vehicleDataList : null);
			if (val == null)
			{
				return 0;
			}
			for (int num2 = val.Count - 1; num2 >= 0; num2--)
			{
				VehicleData val2 = val[num2];
				if (val2 != null && val2.vehicleNameTextId == 88030)
				{
					Vector2 val3 = val2.worldCoordinate - pos;
					if (!(val3.sqrMagnitude > 9f) && !((Object)(object)BasicVehicle.FindRuntimeVehicle(val2) != (Object)null))
					{
						val.RemoveAt(num2);
						num++;
					}
				}
			}
		}
		catch
		{
		}
		if (num > 0)
		{
			Report.Note($"HELI 清掉 {num} 筆生成失敗留下的直升機資料");
		}
		return num;
	}

	private static bool HeliNear(Vector2 pos, float r)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			var s_liveVehicles = BasicVehicle.s_liveVehicles;
			if (s_liveVehicles != null)
			{
				for (int i = 0; i < s_liveVehicles.Count; i++)
				{
					if (IsHeli(s_liveVehicles[i]))
					{
						Vector2 val = ((Vector2)(((Component)s_liveVehicles[i]).transform.position)) - pos;
						if (val.sqrMagnitude < r * r)
						{
							return true;
						}
					}
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static BasicVehicle Spawn(Vector2 pos, float fuelL, float heading)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		GameController instance = GameController.instance;
		int num = BaseId();
		if (num < 0)
		{
			Report.Note("HELI 找不到可用的底車");
			return null;
		}
		BasicChunk val = ChunkAt(pos);
		ChunkData val2 = ((val != null) ? val.chunkData : null);
		if (val2 == null)
		{
			if (_spawnLog++ < 6)
			{
				Report.Note($"HELI 生成：{pos} 的區塊還沒載入");
			}
			return null;
		}
		Dedupe(val2, pos);
		VehicleData val3 = null;
		try
		{
			val3 = instance.GetPristineVehicleData(num, heading);
		}
		catch (Exception ex)
		{
			Report.Note("HELI GetPristineVehicleData: " + ex.Message);
			return null;
		}
		if (val3 == null)
		{
			return null;
		}
		val3.vehicleNameTextId = 88030;
		try
		{
			val3.worldCoordinate = pos;
		}
		catch
		{
		}
		try
		{
			val3.coordinateX = pos.x;
			val3.coordinateY = pos.y;
		}
		catch
		{
		}
		try
		{
			val3.eularAngleZ = heading;
		}
		catch
		{
		}
		try
		{
			val3.zLevel = ((BasicCharacterController)GameController.instance.playerCharacter).characterData.zLevel;
		}
		catch
		{
		}
		SetFuel(val3, fuelL);
		try
		{
			if (val2.vehicleDataList != null && !val2.vehicleDataList.Contains(val3))
			{
				val2.vehicleDataList.Add(val3);
			}
		}
		catch
		{
		}
		BasicVehicle val4 = null;
		string text = "";
		try
		{
			InGameController instance2 = InGameController.instance;
			if ((Object)(object)instance2 != (Object)null)
			{
				val4 = instance2.AddVehicle(val3, val);
				text = "AddVehicle";
			}
		}
		catch (Exception ex2)
		{
			text = "AddVehicle 例外 " + ex2.Message + "；";
		}
		if ((Object)(object)val4 == (Object)null)
		{
			try
			{
				val4 = val2.SpawnVehicleOnLayer(val3);
				text += "→SpawnVehicleOnLayer";
			}
			catch (Exception ex3)
			{
				text = text + "SpawnVehicleOnLayer 例外 " + ex3.Message;
			}
		}
		if ((Object)(object)val4 == (Object)null)
		{
			try
			{
				val2.vehicleDataList.Remove(val3);
			}
			catch
			{
			}
		}
		else
		{
			try
			{
				val4.vehicleNameTextId = 88030;
			}
			catch
			{
			}
			try
			{
				((Component)val4).transform.position = new Vector3(pos.x, pos.y, ((Component)val4).transform.position.z);
				Rigidbody2D rigidbody = val4.m_rigidbody;
				if ((Object)(object)rigidbody != (Object)null)
				{
					rigidbody.position = pos;
				}
			}
			catch
			{
			}
			try
			{
				var vehicleList = val.vehicleList;
				if (vehicleList != null && !vehicleList.Contains(val4))
				{
					vehicleList.Add(val4);
				}
			}
			catch
			{
			}
			try
			{
				if (!val4.initExecutedFlag)
				{
					val4.Init();
				}
			}
			catch
			{
			}
			try
			{
				VehiclePartData val5 = val4.fuelTankData ?? FuelPart(val4.vehicleData);
				if (val5 != null)
				{
					val5.fuel = Mathf.Clamp(fuelL, 0f, Cap(val4.vehicleData));
				}
			}
			catch
			{
			}
		}
		if (_spawnLog++ < 8)
		{
			string value = "?";
			string text2 = "";
			try
			{
				value = $"{((Component)val4).transform.position.x:0.#},{((Component)val4).transform.position.y:0.#}";
			}
			catch
			{
			}
			try
			{
				int value2 = 0;
				var s_liveVehicles = BasicVehicle.s_liveVehicles;
				if (s_liveVehicles != null)
				{
					for (int i = 0; i < s_liveVehicles.Count; i++)
					{
						if ((Object)(object)s_liveVehicles[i] != (Object)null && ((Il2CppObjectBase)s_liveVehicles[i]).Pointer == ((Il2CppObjectBase)val4).Pointer)
						{
							value2 = 1;
						}
					}
				}
				text2 = $"油量={Fuel(val4):0.#}/{Cap(val4.vehicleData):0} L 啟用={((Component)val4).gameObject.activeInHierarchy} 清單中={value2} 圖層隱藏={val4.layerHiddenFlag} 已初始化={val4.initExecutedFlag} 標記={IsHeli(val4)}";
			}
			catch
			{
			}
			Report.Note($"HELI 生成直升機（底車 {num}，{text}）目標 {pos.x:0.#},{pos.y:0.#} → 實際 {value}｜區塊 {val2.coordinateX},{val2.coordinateY}｜{(((Object)(object)val4 != (Object)null) ? ("成功 " + text2) : "失敗")}");
		}
		return val4;
	}

	private static VehiclePartData FuelPart(VehicleData vd)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		try
		{
			var val = ((vd != null) ? vd.vehiclePartDatas : null);
			if (val != null)
			{
				for (int i = 0; i < val.Count; i++)
				{
					if (val[i] != null && (int)val[i].vehiclePartType == 4)
					{
						return val[i];
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static void SetFuel(VehicleData vd, float liters)
	{
		VehiclePartData val = FuelPart(vd);
		if (val != null)
		{
			val.fuel = Mathf.Clamp(liters, 0f, Cap(vd));
		}
	}

	private static float Cap(VehicleData vd)
	{
		float result = 60f;
		try
		{
			if (vd.vehicleFuelTankSize > 1f)
			{
				result = vd.vehicleFuelTankSize;
			}
		}
		catch
		{
		}
		return result;
	}

	private static float Fuel(BasicVehicle v)
	{
		try
		{
			VehiclePartData val = v.fuelTankData ?? FuelPart(v.vehicleData);
			return (val != null) ? val.fuel : 0f;
		}
		catch
		{
			return 0f;
		}
	}

	private static void AddFuel(BasicVehicle v, float d)
	{
		try
		{
			VehiclePartData val = v.fuelTankData ?? FuelPart(v.vehicleData);
			if (val != null)
			{
				val.fuel = Mathf.Clamp(val.fuel + d, 0f, Cap(v.vehicleData));
			}
		}
		catch
		{
		}
	}

	private static void DetectForward(BasicVehicle v)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		if (_fwdKnown)
		{
			return;
		}
		try
		{
			Vector2 val = Vector2.zero;
			int num = 0;
			Light2D[] array = (Light2D[])(object)new Light2D[2] { v.headlight_Left, v.headlight_Right };
			foreach (Light2D val2 in array)
			{
				if ((Object)(object)val2 != (Object)null)
				{
					Vector3 val3 = ((Component)v).transform.InverseTransformPoint(((Component)val2).transform.position);
					val += new Vector2(val3.x, val3.y);
					num++;
				}
			}
			if (num == 0)
			{
				Vector2 headLightLocalOffset = v.headLightLocalOffset;
				if (headLightLocalOffset.sqrMagnitude > 0.01f)
				{
					val = headLightLocalOffset;
					num = 1;
				}
			}
			if (num != 0 && !(val.sqrMagnitude < 0.0001f))
			{
				Vector2 val4 = val / (float)num;
				_fwd = Mathf.Round(Mathf.Atan2(val4.y, val4.x) * 57.29578f / 90f) * 90f;
				if (_fwd <= -180f)
				{
					_fwd += 360f;
				}
				_fwdKnown = true;
				_dirty = true;
				Report.Note($"HELI 車頭方向（依車燈位置 {val4.x:0.##},{val4.y:0.##}）：本地 {_fwd:0} 度");
			}
		}
		catch (Exception ex)
		{
			Report.Note("heli fwd: " + ex.Message);
		}
	}

	private static bool EngineOn(BasicVehicle v)
	{
		try
		{
			return (Object)(object)v.engine != (Object)null && v.engine.engineRunningFlag;
		}
		catch
		{
			return false;
		}
	}

	private static Sprite LoadSprite(string name)
	{
		return LoadSprite(name, new Vector2(0.5f, 0.5f));
	}

	private static Sprite LoadSprite(string name, Vector2 pivot)
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		using Stream stream = typeof(Heli).Assembly.GetManifestResourceStream("art." + name + ".png");
		if (stream == null)
		{
			return null;
		}
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
		// Large textures are the rendered UH-1Y art (about 47 px per metre); small ones are pixel art.
		bool hiRes = ((Texture)val).width > 300 || name == "heli_minigun";
		((Texture)val).filterMode = hiRes ? (FilterMode)1 : (FilterMode)0;
		Sprite obj = Sprite.Create(val, new Rect(0f, 0f, (float)((Texture)val).width, (float)((Texture)val).height), pivot, hiRes ? 166f : 32f);
		((Object)obj).hideFlags = (HideFlags)61;
		return obj;
	}

	private static bool Sprites()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		if (!_spriteTried)
		{
			_spriteTried = true;
			try
			{
				_body = LoadSprite("heli_body");
				_rotor = LoadSprite("heli_rotor");
				_gun = LoadSprite("heli_minigun", GunPivot);
				Texture2D val = new Texture2D(1, 1, (TextureFormat)4, false);
				val.SetPixel(0, 0, Color.white);
				val.Apply();
				((Object)val).hideFlags = (HideFlags)61;
				_white = Sprite.Create(val, new Rect(0f, 0f, 1f, 1f), new Vector2(0f, 0.5f), 1f);
				((Object)_white).hideFlags = (HideFlags)61;
			}
			catch (Exception ex)
			{
				Report.Note("heli sprite: " + ex.Message);
			}
		}
		if ((Object)(object)_body != (Object)null)
		{
			return (Object)(object)_rotor != (Object)null;
		}
		return false;
	}

	private static SpriteRenderer NewSr(string name, Sprite sp, Transform parent, int order)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		GameObject val = new GameObject(name);
		if ((Object)(object)parent != (Object)null)
		{
			val.transform.SetParent(parent, false);
		}
		SpriteRenderer obj = val.AddComponent<SpriteRenderer>();
		obj.sprite = sp;
		((Renderer)obj).sortingLayerID = _sortLayer;
		((Renderer)obj).sortingOrder = order;
		return obj;
	}

	private static Vis EnsureVis(BasicVehicle v)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		if (_vis.TryGetValue(((Il2CppObjectBase)v).Pointer, out var value) && (Object)(object)value.Root != (Object)null)
		{
			return value;
		}
		value = new Vis();
		value.Root = new GameObject("zpi_heli");
		value.Body = NewSr("body", _body, value.Root.transform, 32009);
		if ((Object)(object)_gun != (Object)null)
		{
			value.Gun[0] = NewSr("gunL", _gun, value.Root.transform, 32010);
			value.Gun[1] = NewSr("gunR", _gun, value.Root.transform, 32010);
		}
		value.Rotor = NewSr("rotor", _rotor, value.Root.transform, 32012);
		value.Rotor2 = NewSr("rotor2", _rotor, value.Root.transform, 32011);
		value.Rotor2.color = new Color(1f, 1f, 1f, 0f);
		value.Shadow = NewSr("shadow", _body, null, 32000);
		value.ShadowGo = ((Component)value.Shadow).gameObject;
		value.Shadow.color = new Color(0.05f, 0.07f, 0.04f, 0.38f);
		value.Root.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
		value.ShadowGo.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
		_vis[((Il2CppObjectBase)v).Pointer] = value;
		if (_visLog++ < 5)
		{
			try
			{
				Report.Note($"HELI 外觀套上直升機 @{((Component)v).transform.position.x:0.#},{((Component)v).transform.position.y:0.#} z={((Component)v).transform.position.z:0.##}｜車身圖像 {((Component)v).GetComponentsInChildren<Renderer>(true).Length} 個｜排序圖層 {_sortLayer}｜啟用={((Component)v).gameObject.activeInHierarchy}");
			}
			catch
			{
			}
		}
		return value;
	}

	private static void ClearVis()
	{
		foreach (Vis value in _vis.Values)
		{
			try
			{
				if ((Object)(object)value.Root != (Object)null)
				{
					Object.Destroy((Object)(object)value.Root);
				}
				if ((Object)(object)value.ShadowGo != (Object)null)
				{
					Object.Destroy((Object)(object)value.ShadowGo);
				}
			}
			catch
			{
			}
		}
		_vis.Clear();
	}

	private static void HideCar(BasicVehicle v, Vis x, float now)
	{
		try
		{
			MeshRenderer vehicleQuadRenderer = v.vehicleQuadRenderer;
			if ((Object)(object)vehicleQuadRenderer != (Object)null && ((Renderer)vehicleQuadRenderer).enabled)
			{
				((Renderer)vehicleQuadRenderer).enabled = false;
			}
		}
		catch
		{
		}
		try
		{
			MeshRenderer vehicleMeshRenderer = v.vehicleMeshRenderer;
			if ((Object)(object)vehicleMeshRenderer != (Object)null && ((Renderer)vehicleMeshRenderer).enabled)
			{
				((Renderer)vehicleMeshRenderer).enabled = false;
			}
		}
		catch
		{
		}
		try
		{
			GameObject vehicleQuadObj = v.vehicleQuadObj;
			if ((Object)(object)vehicleQuadObj != (Object)null)
			{
				foreach (Renderer componentsInChild in vehicleQuadObj.GetComponentsInChildren<Renderer>(true))
				{
					if ((Object)(object)componentsInChild != (Object)null && componentsInChild.enabled)
					{
						componentsInChild.enabled = false;
					}
				}
			}
		}
		catch
		{
		}
		try
		{
			Il2CppReferenceArray<MeshRenderer> damageTintRenderers = v.damageTintRenderers;
			if (damageTintRenderers != null)
			{
				for (int i = 0; i < ((Il2CppArrayBase<MeshRenderer>)(object)damageTintRenderers).Length; i++)
				{
					if ((Object)(object)((Il2CppArrayBase<MeshRenderer>)(object)damageTintRenderers)[i] != (Object)null && ((Renderer)((Il2CppArrayBase<MeshRenderer>)(object)damageTintRenderers)[i]).enabled)
					{
						((Renderer)((Il2CppArrayBase<MeshRenderer>)(object)damageTintRenderers)[i]).enabled = false;
					}
				}
			}
		}
		catch
		{
		}
		if (now < x.NextHide)
		{
			foreach (Renderer item in x.Hidden)
			{
				if ((Object)(object)item != (Object)null && item.enabled)
				{
					item.enabled = false;
				}
			}
			return;
		}
		x.NextHide = now + 0.5f;
		x.Hidden.Clear();
		try
		{
			foreach (Renderer componentsInChild2 in ((Component)v).GetComponentsInChildren<Renderer>(true))
			{
				if (!((Object)(object)componentsInChild2 == (Object)null) && !((Object)(object)((Component)componentsInChild2).GetComponentInParent<BasicCharacterController>() != (Object)null))
				{
					if (componentsInChild2.enabled)
					{
						componentsInChild2.enabled = false;
					}
					x.Hidden.Add(componentsInChild2);
				}
			}
		}
		catch
		{
		}
	}

	public static void Tick(float dt)
	{
		if (!HeliPlugin.HeliEnabled.Value)
		{
			return;
		}
		GameController instance = GameController.instance;
		if ((Object)(object)instance == (Object)null)
		{
			return;
		}
		long createdDateTime;
		try
		{
			if (instance._gameData == null)
			{
				return;
			}
			createdDateTime = instance._gameData.createdDateTime;
		}
		catch
		{
			return;
		}
		HumanCharacterController playerCharacter = instance.playerCharacter;
		if ((Object)(object)playerCharacter == (Object)null)
		{
			return;
		}
		IntPtr intPtr = IntPtr.Zero;
		try
		{
			intPtr = ((Il2CppObjectBase)instance._gameData).Pointer;
		}
		catch
		{
		}
		if (createdDateTime != _saveId || intPtr != _gdPtr)
		{
			_saveId = createdDateTime;
			_gdPtr = intPtr;
			Load();
		}
		if (!_sortSet)
		{
			try
			{
				SpriteRenderer componentInChildren = ((Component)playerCharacter).GetComponentInChildren<SpriteRenderer>();
				if ((Object)(object)componentInChildren != (Object)null)
				{
					_sortLayer = ((Renderer)componentInChildren).sortingLayerID;
					_sortSet = true;
				}
			}
			catch
			{
			}
		}
		if (!Sprites())
		{
			return;
		}
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (realtimeSinceStartup >= _scanAt)
		{
			_scanAt = realtimeSinceStartup + 0.5f;
			try
			{
				Track();
				ScanPads(playerCharacter);
				Migrate(playerCharacter);
			}
			catch (Exception ex)
			{
				Report.Note("heli pads: " + ex.Message);
				_scanAt = realtimeSinceStartup + 5f;
			}
		}
		_gunSide = -1;
		BasicVehicle val = null;
		try
		{
			val = playerCharacter.drivingVehicle;
		}
		catch
		{
		}
		bool flag = (Object)(object)val != (Object)null && IsHeli(val) && SameDriver(val, playerCharacter);
		if ((Object)(object)_mine != (Object)null && (!flag || ((Il2CppObjectBase)val).Pointer != ((Il2CppObjectBase)_mine).Pointer))
		{
			ForceLand(_mine);
			_mine = null;
		}
		if (flag)
		{
			try
			{
				Pilot(playerCharacter, val, dt, realtimeSinceStartup);
			}
			catch (Exception ex2)
			{
				Report.Note("heli pilot: " + ex2);
			}
		}
		else if ((Object)(object)val != (Object)null && IsHeli(val))
		{
			try
			{
				Gunner(playerCharacter, val, realtimeSinceStartup);
			}
			catch (Exception exG)
			{
				Report.Note("heli gunner: " + exG.Message);
			}
		}
		else if ((Object)(object)val == (Object)null)
		{
			// In co-op a passenger may not have drivingVehicle set: find the heli whose seat list has us.
			BasicVehicle seatHeli = NetPassengerHeli();
			if ((Object)(object)seatHeli != (Object)null)
			{
				try
				{
					Gunner(playerCharacter, seatHeli, realtimeSinceStartup);
				}
				catch (Exception exG2)
				{
					Report.Note("heli gunner: " + exG2.Message);
				}
			}
			else
			{
				NearHint(playerCharacter);
			}
		}
		try
		{
			Draw(playerCharacter, dt, realtimeSinceStartup);
		}
		catch (Exception ex3)
		{
			Report.Note("heli draw: " + ex3.Message);
		}
		HeliInspect.Poll();
		TickFx(realtimeSinceStartup);
		if (_dirty && realtimeSinceStartup > _nextSave)
		{
			_nextSave = realtimeSinceStartup + 5f;
			Save();
		}
	}

	private static bool SameDriver(BasicVehicle v, HumanCharacterController pc)
	{
		try
		{
			HumanCharacterController driverCharacterController = v.driverCharacterController;
			return (Object)(object)driverCharacterController != (Object)null && ((Il2CppObjectBase)driverCharacterController).Pointer == ((Il2CppObjectBase)pc).Pointer;
		}
		catch
		{
			return false;
		}
	}

	private static void NearHint(HumanCharacterController pc)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		NearStatus = null;
		try
		{
			var s_liveVehicles = BasicVehicle.s_liveVehicles;
			if (s_liveVehicles == null)
			{
				return;
			}
			Vector2 val = ((Vector2)(((Component)pc).transform.position));
			for (int i = 0; i < s_liveVehicles.Count; i++)
			{
				BasicVehicle val2 = s_liveVehicles[i];
				if (IsHeli(val2))
				{
					Vector2 val3 = ((Vector2)(((Component)val2).transform.position)) - val;
					if (!(val3.sqrMagnitude > 25f))
					{
						NearStatus = L.F("直升機：油 {0:0.#} / {1:0} L｜上機後發動引擎，按空白鍵起飛", Fuel(val2), Cap(val2.vehicleData)) + "｜" + L.F("Shift+{0} 用油桶加油", HeliPlugin.CallKey.Value);
						break;
					}
				}
			}
		}
		catch
		{
		}
	}

	private static void Track()
	{
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		var s_liveVehicles = BasicVehicle.s_liveVehicles;
		if (s_liveVehicles == null)
		{
			return;
		}
		HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
		for (int i = 0; i < s_liveVehicles.Count; i++)
		{
			if ((Object)(object)s_liveVehicles[i] != (Object)null)
			{
				hashSet.Add(((Il2CppObjectBase)s_liveVehicles[i]).Pointer);
			}
		}
		List<IntPtr> list = new List<IntPtr>();
		foreach (IntPtr key in _knownIdx.Keys)
		{
			if (!hashSet.Contains(key))
			{
				list.Add(key);
			}
		}
		foreach (IntPtr item in list)
		{
			_knownIdx.Remove(item);
		}
		HashSet<int> hashSet2 = new HashSet<int>(_knownIdx.Values);
		for (int j = 0; j < s_liveVehicles.Count; j++)
		{
			BasicVehicle val = s_liveVehicles[j];
			if ((Object)(object)val == (Object)null || !IsHeli(val))
			{
				continue;
			}
			Vector2 val2 = VehPos(val);
			if (!_knownIdx.TryGetValue(((Il2CppObjectBase)val).Pointer, out var value))
			{
				value = Nearest(val2, 3f, hashSet2);
				if (value < 0)
				{
					_known.Add(val2);
					value = _known.Count - 1;
					_dirty = true;
				}
				_knownIdx[((Il2CppObjectBase)val).Pointer] = value;
				hashSet2.Add(value);
			}
			Vector2 val3 = _known[value] - val2;
			if (val3.sqrMagnitude > 0.25f)
			{
				_known[value] = val2;
				_dirty = true;
			}
		}
		int num = ((_baseId >= 0) ? _baseId : BaseId());
		for (int k = 0; k < s_liveVehicles.Count; k++)
		{
			BasicVehicle val4 = s_liveVehicles[k];
			if ((Object)(object)val4 == (Object)null || IsHeli(val4) || _knownIdx.ContainsKey(((Il2CppObjectBase)val4).Pointer))
			{
				continue;
			}
			VehicleData val5 = null;
			try
			{
				val5 = val4.vehicleData;
			}
			catch
			{
			}
			if (val5 == null || val5.vehicleId != num)
			{
				continue;
			}
			Vector2 val6 = VehPos(val4);
			int num2 = Nearest(val6, 2.5f, hashSet2);
			if (num2 >= 0)
			{
				try
				{
					val4.vehicleNameTextId = 88030;
					val5.vehicleNameTextId = 88030;
				}
				catch
				{
				}
				_knownIdx[((Il2CppObjectBase)val4).Pointer] = num2;
				hashSet2.Add(num2);
				if (_retagLog++ < 8)
				{
					Report.Note($"HELI 讀檔後重新標記直升機 @{val6.x:0.#},{val6.y:0.#}");
				}
			}
		}
	}

	private static int Nearest(Vector2 p, float r, HashSet<int> skip)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		int result = -1;
		float num = r * r;
		for (int i = 0; i < _known.Count; i++)
		{
			if (!skip.Contains(i))
			{
				Vector2 val = _known[i] - p;
				float sqrMagnitude = val.sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = i;
				}
			}
		}
		return result;
	}

	private static Vector2 PadCenter(TerrainObject o)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		return ((Vector2)(((Component)o).transform.position)) + new Vector2(0f, 1.5f);
	}

	private static string PadId(TerrainObject o, Vector2 c)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			TerrainObjectData objectData = o.objectData;
			string text = ((objectData != null) ? objectData.terrainObejctGUID : null);
			if (!string.IsNullOrEmpty(text))
			{
				return "g:" + text;
			}
		}
		catch
		{
		}
		return Key(c);
	}

	private static void Walkable(TerrainObject o)
	{
		try
		{
			int num = 0;
			foreach (Collider2D componentsInChild in ((Component)o).GetComponentsInChildren<Collider2D>(true))
			{
				if ((Object)(object)componentsInChild != (Object)null && !componentsInChild.isTrigger)
				{
					componentsInChild.isTrigger = true;
					num++;
				}
			}
			if (num > 0 && _walkLog++ < 3)
			{
				Report.Note($"HELI 停機坪改成可通行：{num} 個碰撞體設為觸發器");
			}
		}
		catch
		{
		}
	}

	// Standalone: no helipad building or power grid. The call key puts a
	// helicopter a few metres in front of the player instead.
	private static float _callWait;

	private static void ScanPads(HumanCharacterController pc)
	{
		KeyCode key = HeliPlugin.CallKey.Value;
		if ((int)key == 0 || !Input.GetKeyDown(key))
		{
			return;
		}
		if (Time.realtimeSinceStartup < _callWait)
		{
			return;
		}
		_callWait = Time.realtimeSinceStartup + 2f;
		if (Input.GetKey((KeyCode)304) || Input.GetKey((KeyCode)303))
		{
			Refuel(pc);
			return;
		}
		CallIn(pc, ignoreLimit: false);
	}

	// Calls a helicopter in a few metres in front of the player. Also used by HeliApi
	// (Spawn Menu's helicopter kit), which skips the per-save limit for testing.
	internal static string CallIn(HumanCharacterController pc, bool ignoreLimit)
	{
		if ((Object)(object)pc == (Object)null || Piloting)
		{
			return null;
		}
		try
		{
			if ((Object)(object)pc.drivingVehicle != (Object)null)
			{
				return Fail(L.T("下車後才能叫直升機"));
			}
		}
		catch
		{
		}
		if (!Sprites())
		{
			return Fail("Helicopter art failed to load (see BepInEx\\LogOutput.log)");
		}
		Vector2 here = ((Vector2)(((Component)pc).transform.position));
		if (HeliNear(here, HeliPlugin.CallRadius.Value))
		{
			return Fail(L.T("附近已經有一架直升機了"));
		}
		int max = HeliPlugin.MaxHelicopters.Value;
		if (!ignoreLimit && max > 0 && _called >= max)
		{
			return Fail(L.F("這個存檔已經叫過 {0} 架直升機（設定 MaxHelicopters）", max));
		}
		Vector2 spot = here + new Vector2(0f, -4f);
		if ((Object)(object)Spawn(spot, float.MaxValue, 90f) != (Object)null)
		{
			_called++;
			_dirty = true;
			Save();
			Report.Note($"HELI called in at {spot} ({_called}/{max}, ignoreLimit={ignoreLimit})");
			return Ok(L.T("直升機到了"));
		}
		return Fail(L.T("這裡叫不到直升機，換個空地再試"));
	}

	internal static System.Collections.Generic.List<InventoryData> CarriedBoxes(HumanCharacterController pc)
	{
		return Carried(pc);
	}

	internal static void Message(string m)
	{
		Msg(m);
	}

	private static string Ok(string m)
	{
		Msg(m);
		return m;
	}

	private static string Fail(string m)
	{
		Msg(m);
		return "!" + m;
	}

	private static void Migrate(HumanCharacterController pc)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		if (_pending.Count == 0 || Time.realtimeSinceStartup < _migrateWait)
		{
			return;
		}
		Vector2 val = ((Vector2)(((Component)pc).transform.position));
		for (int num = _pending.Count - 1; num >= 0; num--)
		{
			(Vector2, float, float) item = _pending[num];
			Vector2 val2 = item.Item1 - val;
			if (!(val2.sqrMagnitude > 1600f))
			{
				if (!HeliNear(item.Item1, 2f) && !((Object)(object)Spawn(item.Item1, item.Item2, item.Item3) != (Object)null))
				{
					_pending.RemoveAt(num);
					_pending.Add(item);
					_migrateWait = Time.realtimeSinceStartup + 10f;
					break;
				}
				_pending.RemoveAt(num);
				_dirty = true;
				Save();
			}
		}
	}

	private static List<(ItemData liq, InventoryData holder)> FuelSources(List<InventoryData> invs)
	{
		List<(ItemData, InventoryData)> list = new List<(ItemData, InventoryData)>();
		HashSet<int> hashSet = FuelGen.FuelIds();
		foreach (InventoryData inv in invs)
		{
			if (((inv != null) ? inv.itemList : null) == null)
			{
				continue;
			}
			for (int i = 0; i < inv.itemList.Count; i++)
			{
				ItemData val = inv.itemList[i];
				if (val == null)
				{
					continue;
				}
				if (hashSet.Contains(val.itemId))
				{
					if (val.itemNumberFloat > 0.01f)
					{
						list.Add((val, inv));
					}
					continue;
				}
				InventoryData inventoryData = val.inventoryData;
				if (((inventoryData != null) ? inventoryData.itemList : null) == null)
				{
					continue;
				}
				for (int j = 0; j < inventoryData.itemList.Count; j++)
				{
					ItemData val2 = inventoryData.itemList[j];
					if (val2 != null && hashSet.Contains(val2.itemId) && val2.itemNumberFloat > 0.01f)
					{
						list.Add((val2, inventoryData));
					}
				}
			}
		}
		return list;
	}

	private static float Burn(List<(ItemData liq, InventoryData holder)> srcs, float ml)
	{
		float num = 0f;
		foreach (var (val, val2) in srcs)
		{
			if (num >= ml)
			{
				break;
			}
			float num2 = 0f;
			try
			{
				num2 = val.itemNumberFloat;
			}
			catch
			{
				continue;
			}
			if (num2 <= 0.0001f)
			{
				continue;
			}
			float num3 = Mathf.Min(num2, ml - num);
			try
			{
				if (num2 - num3 <= 0.0001f)
				{
					val2.RemoveItem(val, true);
				}
				else
				{
					val.itemNumberFloat = num2 - num3;
					try
					{
						val2.RefreshTotalItemWeight();
					}
					catch
					{
					}
				}
				num += num3;
			}
			catch
			{
			}
		}
		return num;
	}

	private static List<InventoryData> Carried(HumanCharacterController pc)
	{
		List<InventoryData> list = new List<InventoryData>();
		try
		{
			var carriedInventoryList = ((BasicCharacterController)pc).characterData.GetCarriedInventoryList();
			if (carriedInventoryList != null)
			{
				for (int i = 0; i < carriedInventoryList.Count; i++)
				{
					if (carriedInventoryList[i] != null)
					{
						list.Add(carriedInventoryList[i]);
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private static void Pilot(HumanCharacterController pc, BasicVehicle v, float dt, float now)
	{
		Air value;
		bool flag = _air.TryGetValue(((Il2CppObjectBase)v).Pointer, out value);
		LightForward(v);
		bool flag2 = Input.GetKey((KeyCode)119) || Input.GetKey((KeyCode)97) || Input.GetKey((KeyCode)115) || Input.GetKey((KeyCode)100) || Input.GetKey((KeyCode)273) || Input.GetKey((KeyCode)274) || Input.GetKey((KeyCode)276) || Input.GetKey((KeyCode)275);
		if (Input.GetKeyDown((KeyCode)32))
		{
			if (!flag)
			{
				if (!EngineOn(v))
				{
					Msg(L.T("先發動引擎才能起飛"));
				}
				else
				{
					TakeOff(v);
				}
			}
			else if (value.Ph != 3)
			{
				value.Ph = 3;
			}
			flag = _air.TryGetValue(((Il2CppObjectBase)v).Pointer, out value);
		}
		else if (!flag && flag2 && EngineOn(v))
		{
			TakeOff(v);
			flag = _air.TryGetValue(((Il2CppObjectBase)v).Pointer, out value);
		}
		if (!flag)
		{
			NearStatus = L.T("直升機：引擎發動後按空白鍵起飛");
			return;
		}
		_mine = v;
		Fly(pc, v, value, dt, now);
	}

	private static float VehAng(BasicVehicle v)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Rigidbody2D rigidbody = v.m_rigidbody;
			if ((Object)(object)rigidbody != (Object)null)
			{
				return rigidbody.rotation;
			}
		}
		catch
		{
		}
		return ((Component)v).transform.eulerAngles.z;
	}

	private static Vector2 VehPos(BasicVehicle v)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Rigidbody2D rigidbody = v.m_rigidbody;
			if ((Object)(object)rigidbody != (Object)null)
			{
				return rigidbody.position;
			}
		}
		catch
		{
		}
		return ((Vector2)(((Component)v).transform.position));
	}

	private static void LightForward(BasicVehicle v)
	{
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		if (_fwdKnown)
		{
			return;
		}
		try
		{
			Light2D val = v.headlight_Left ?? v.headlight_Right;
			if (!((Object)(object)val == (Object)null))
			{
				Vector3 up = ((Component)val).transform.up;
				float num = Mathf.Atan2(up.y, up.x) * 57.29578f;
				_fwd = Mathf.Round(Mathf.DeltaAngle(VehAng(v), num) / 5f) * 5f;
				_fwdKnown = true;
				_dirty = true;
				bool value = false;
				try
				{
					value = (Object)(object)v.m_rigidbody != (Object)null && ((Il2CppObjectBase)((Component)v.m_rigidbody).gameObject).Pointer == ((Il2CppObjectBase)((Component)v).gameObject).Pointer;
				}
				catch
				{
				}
				Report.Note($"HELI 車頭方向（依車燈照射方向）：剛體角度 {VehAng(v):0}、車燈 {num:0} → 本地 {_fwd:0} 度｜剛體在根物件上={value}｜根物件角度 {((Component)v).transform.eulerAngles.z:0}");
			}
		}
		catch (Exception ex)
		{
			Report.Note("heli light fwd: " + ex.Message);
		}
	}

	private static void Sane(BasicVehicle v)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Vector2 val = VehPos(v);
			v.lastSanePosition = new Vector3(val.x, val.y, ((Component)v).transform.position.z);
			v.lastSanePositionInit = true;
			v.physicsAnomalyStreak = 0;
		}
		catch
		{
		}
		try
		{
			VehicleData vehicleData = v.vehicleData;
			if (vehicleData != null)
			{
				vehicleData.worldCoordinate = VehPos(v);
				vehicleData.eularAngleZ = VehAng(v);
			}
		}
		catch
		{
		}
	}

	private static void TakeOff(BasicVehicle v)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		if (_air.ContainsKey(((Il2CppObjectBase)v).Pointer))
		{
			return;
		}
		Air a = new Air
		{
			Ph = 1,
			Alt = 0f,
			Vel = Vector2.zero
		};
		a.Heading = VehAng(v) + _fwd;
		try
		{
			Off((Collider2D)(object)v.vehicleCollider2D);
		}
		catch
		{
		}
		try
		{
			Rigidbody2D rigidbody = v.m_rigidbody;
			if ((Object)(object)rigidbody != (Object)null && rigidbody.attachedColliderCount > 0)
			{
				Il2CppReferenceArray<Collider2D> val = new Il2CppReferenceArray<Collider2D>((long)rigidbody.attachedColliderCount);
				int attachedColliders = rigidbody.GetAttachedColliders(val);
				for (int i = 0; i < attachedColliders; i++)
				{
					Off(((Il2CppArrayBase<Collider2D>)(object)val)[i]);
				}
			}
		}
		catch (Exception ex)
		{
			Report.Note("heli colliders: " + ex.Message);
		}
		try
		{
			foreach (Collider2D componentsInChild in ((Component)v).GetComponentsInChildren<Collider2D>(true))
			{
				Off(componentsInChild);
			}
		}
		catch
		{
		}
		try
		{
			VehicleCollisionHandler vehicleCollisionHandler = v.vehicleCollisionHandler;
			if ((Object)(object)vehicleCollisionHandler != (Object)null && ((Behaviour)vehicleCollisionHandler).enabled)
			{
				((Behaviour)vehicleCollisionHandler).enabled = false;
				a.Handler = (Behaviour)(object)vehicleCollisionHandler;
			}
		}
		catch
		{
		}
		try
		{
			Rigidbody2D rigidbody2 = v.m_rigidbody;
			if ((Object)(object)rigidbody2 != (Object)null)
			{
				a.BodyType = rigidbody2.bodyType;
				rigidbody2.velocity = Vector2.zero;
				rigidbody2.angularVelocity = 0f;
				rigidbody2.bodyType = (RigidbodyType2D)1;
			}
		}
		catch
		{
		}
		_air[((Il2CppObjectBase)v).Pointer] = a;
		Report.Note($"HELI 起飛：關閉 {a.Off.Count} 個碰撞，車頭 {a.Heading:0} 度");
		void Off(Collider2D c)
		{
			try
			{
				if ((Object)(object)c != (Object)null && ((Behaviour)c).enabled && (Object)(object)((Component)c).GetComponentInParent<BasicCharacterController>() == (Object)null && !a.Off.Contains(c))
				{
					((Behaviour)c).enabled = false;
					a.Off.Add(c);
				}
			}
			catch
			{
			}
		}
	}

	private static void Landed(BasicVehicle v, Air a)
	{
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		Sane(v);
		foreach (Collider2D item in a.Off)
		{
			try
			{
				if ((Object)(object)item != (Object)null)
				{
					((Behaviour)item).enabled = true;
				}
			}
			catch
			{
			}
		}
		try
		{
			if ((Object)(object)a.Handler != (Object)null)
			{
				a.Handler.enabled = true;
			}
		}
		catch
		{
		}
		try
		{
			Rigidbody2D rigidbody = v.m_rigidbody;
			if ((Object)(object)rigidbody != (Object)null)
			{
				rigidbody.bodyType = a.BodyType;
				rigidbody.velocity = Vector2.zero;
			}
		}
		catch
		{
		}
		try
		{
			Vector2 worldCoordinate = VehPos(v);
			VehicleData vehicleData = v.vehicleData;
			if (vehicleData != null)
			{
				vehicleData.worldCoordinate = worldCoordinate;
				vehicleData.eularAngleZ = VehAng(v);
			}
		}
		catch
		{
		}
		_air.Remove(((Il2CppObjectBase)v).Pointer);
		if ((Object)(object)_mine != (Object)null && ((Il2CppObjectBase)_mine).Pointer == ((Il2CppObjectBase)v).Pointer)
		{
			_mine = null;
		}
	}

	private static void ForceLand(BasicVehicle v)
	{
		try
		{
			if (_air.TryGetValue(((Il2CppObjectBase)v).Pointer, out var value))
			{
				Landed(v, value);
			}
		}
		catch
		{
			_air.Clear();
		}
	}

	private static void Fly(HumanCharacterController pc, BasicVehicle v, Air a, float dt, float now)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0438: Unknown result type (might be due to invalid IL or missing references)
		//IL_0434: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_043d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0445: Unknown result type (might be due to invalid IL or missing references)
		//IL_0457: Unknown result type (might be due to invalid IL or missing references)
		//IL_045c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0461: Unknown result type (might be due to invalid IL or missing references)
		//IL_0463: Unknown result type (might be due to invalid IL or missing references)
		//IL_0468: Unknown result type (might be due to invalid IL or missing references)
		//IL_0518: Unknown result type (might be due to invalid IL or missing references)
		//IL_0519: Unknown result type (might be due to invalid IL or missing references)
		//IL_055d: Unknown result type (might be due to invalid IL or missing references)
		//IL_047e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0485: Unknown result type (might be due to invalid IL or missing references)
		//IL_048e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0365: Unknown result type (might be due to invalid IL or missing references)
		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
		//IL_036d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0372: Unknown result type (might be due to invalid IL or missing references)
		//IL_0377: Unknown result type (might be due to invalid IL or missing references)
		//IL_037e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0384: Unknown result type (might be due to invalid IL or missing references)
		//IL_0390: Unknown result type (might be due to invalid IL or missing references)
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0346: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bf: Unknown result type (might be due to invalid IL or missing references)
		float value = HeliPlugin.HeliSpeed.Value;
		Rigidbody2D rigidbody = v.m_rigidbody;
		Vector2 val = (((Object)(object)rigidbody != (Object)null) ? rigidbody.position : ((Vector2)(((Component)v).transform.position)));
		if (a.Ph == 1)
		{
			a.Alt = Mathf.MoveTowards(a.Alt, 1f, dt / 1.6f);
			if (a.Alt >= 1f)
			{
				a.Ph = 2;
			}
		}
		else if (a.Ph == 3)
		{
			a.Vel = Vector2.MoveTowards(a.Vel, Vector2.zero, value * 2f * dt);
			a.Alt = Mathf.MoveTowards(a.Alt, 0f, dt / 1.6f);
			if (a.Alt <= 0f && a.Vel.sqrMagnitude < 0.01f)
			{
				try
				{
					if ((Object)(object)rigidbody != (Object)null)
					{
						rigidbody.velocity = Vector2.zero;
					}
				}
				catch
				{
				}
				Landed(v, a);
				return;
			}
		}
		Vector2 zero = Vector2.zero;
		if (a.Ph != 3 && a.Alt > 0.5f)
		{
			if (Input.GetKey((KeyCode)119) || Input.GetKey((KeyCode)273))
			{
				zero.y += 1f;
			}
			if (Input.GetKey((KeyCode)115) || Input.GetKey((KeyCode)274))
			{
				zero.y -= 1f;
			}
			if (Input.GetKey((KeyCode)100) || Input.GetKey((KeyCode)275))
			{
				zero.x += 1f;
			}
			if (Input.GetKey((KeyCode)97) || Input.GetKey((KeyCode)276))
			{
				zero.x -= 1f;
			}
			if (zero.sqrMagnitude > 1f)
			{
				zero.Normalize();
			}
		}
		float num = value;
		try
		{
			MapController instance = MapController.instance;
			if ((Object)(object)instance != (Object)null && a.Vel.sqrMagnitude > 1f)
			{
				Vector2 val2 = val + a.Vel.normalized * Mathf.Max(8f, a.Vel.magnitude * 1.2f);
				if ((Object)(object)instance.GetChunkByCoordinate(instance.GetChunkCoordinateByPosition(val2)) == (Object)null)
				{
					num = Mathf.Min(num, 3f);
					if (_noLoadLog++ < 3)
					{
						Report.Note("HELI 前方區塊尚未載入，減速");
					}
				}
			}
		}
		catch
		{
		}
		if (a.Ph != 3)
		{
			a.Vel = Vector2.MoveTowards(a.Vel, zero * num, value * ((zero.sqrMagnitude > 0.01f) ? 1.1f : 0.8f) * dt);
		}
		if (a.Vel.magnitude > num)
		{
			a.Vel = Vector2.MoveTowards(a.Vel, a.Vel.normalized * num, value * 2f * dt);
		}
		if (a.Vel.sqrMagnitude > 0.25f)
		{
			a.Heading = Mathf.MoveTowardsAngle(a.Heading, Mathf.Atan2(a.Vel.y, a.Vel.x) * 57.29578f, 140f * dt);
		}
		float num2 = a.Heading - _fwd;
		try
		{
			if ((Object)(object)rigidbody != (Object)null)
			{
				rigidbody.velocity = a.Vel;
				rigidbody.angularVelocity = 0f;
				rigidbody.rotation = num2;
			}
			else
			{
				val += a.Vel * dt;
				((Component)v).transform.position = new Vector3(val.x, val.y, ((Component)v).transform.position.z);
			}
			if ((Object)(object)rigidbody == (Object)null)
			{
				((Component)v).transform.rotation = Quaternion.Euler(0f, 0f, num2);
			}
			if ((Object)(object)v.engine != (Object)null)
			{
				v.engine.rpm = Mathf.Lerp(v.engine.maxRpm * 0.55f, v.engine.maxRpm * 0.85f, a.Vel.magnitude / Mathf.Max(1f, value));
			}
		}
		catch
		{
		}
		Sane(v);
		try
		{
			Vector2 val3 = (((Object)(object)rigidbody != (Object)null) ? rigidbody.position : val);
			float z = ((Component)pc).transform.position.z;
			Vector2 val4 = ((Vector2)(((Component)pc).transform.position)) - val3;
			if (val4.sqrMagnitude > 0.04f)
			{
				((Component)pc).transform.position = new Vector3(val3.x, val3.y, z);
				Rigidbody2D rigidbody2 = ((BasicCharacterController)pc).m_rigidbody;
				if ((Object)(object)rigidbody2 != (Object)null)
				{
					rigidbody2.position = val3;
				}
			}
		}
		catch
		{
		}
		AddFuel(v, (0f - HeliPlugin.HeliLitersPerMinute.Value) / 60f * dt);
		if (Fuel(v) <= 0f && a.Ph != 3)
		{
			a.Ph = 3;
			Msg(L.T("油用完了，自動降落"));
		}
		else if (!EngineOn(v) && a.Ph != 3)
		{
			// Engine switched off in the air: no lift, so it comes down.
			a.Ph = 3;
			Msg(L.T("引擎熄火，自動降落"));
		}
		if (now >= _noiseAt)
		{
			_noiseAt = now + 3f;
			try
			{
				GameController.instance.PlayerNoise(((Vector2)(val)), HeliPlugin.HeliNoise.Value, false, true, ((BasicCharacterController)pc).characterData.zLevel, (BasicCharacterController)null);
			}
			catch
			{
			}
		}
		if (a.Ph == 2)
		{
			PilotGun(pc, v, a, val, now);
		}
		SeatIndex(v, out int gunners);
		NearStatus = (gunners > 0)
			? L.F("飛行中：WASD 移動、左鍵火箭、空白鍵降落（機槍由乘客操作）｜高度 {0:0}%", a.Alt * 100f)
			: L.F("飛行中：WASD 移動、左鍵火箭、右鍵機槍、空白鍵降落｜高度 {0:0}%", a.Alt * 100f);
	}

	internal static bool Hostile(BasicCharacterController ch)
	{
		try
		{
			if (BasicCharacterController.IsPlayerSideCharacter(ch))
			{
				return false;
			}
			GOAPAgent component = ((Component)ch).GetComponent<GOAPAgent>();
			if ((Object)(object)component != (Object)null && component.IsPlayerTeammate())
			{
				return false;
			}
			if ((Object)(object)((Component)ch).GetComponent<ZombieController>() != (Object)null)
			{
				return true;
			}
			bool flag = false;
			try
			{
				flag = BasicCharacterController.IsHostileCamp(ch.characterData);
			}
			catch
			{
			}
			bool flag2 = (Object)(object)((Component)ch).GetComponent<HumanCharacterController>() != (Object)null;
			bool flag3 = flag || !flag2 || ((Object)(object)component != (Object)null && component.isBanditAgent);
			string name = ((Il2CppSystem.Reflection.MemberInfo)((Il2CppSystem.Object)ch).GetIl2CppType()).Name;
			string item = name + (flag ? "+敵對" : "");
			if (_typeLogged.Add(item) && _typeLogged.Count < 24)
			{
				int value = -1;
				try
				{
					value = ch.characterData.camp;
				}
				catch
				{
				}
				Report.Note($"敵我判斷：{name}（陣營 {value}、敵對陣營 {flag}、強盜 {(Object)(object)component != (Object)null && component.isBanditAgent}）→ {(flag3 ? "會打" : "不打")}，HP {ch.hp:0}");
			}
			return flag3;
		}
		catch
		{
			return false;
		}
	}

	private static void Fire(HumanCharacterController pc, BasicVehicle v, float heading, float alt, Vector2 gunPos, Vector2 target, float now)
	{
		//IL_0446: Unknown result type (might be due to invalid IL or missing references)
		//IL_0448: Unknown result type (might be due to invalid IL or missing references)
		//IL_0412: Unknown result type (might be due to invalid IL or missing references)
		//IL_041d: Unknown result type (might be due to invalid IL or missing references)
		//IL_041f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		_fireAt = now + 1f / Mathf.Max(1f, HeliPlugin.HeliGunRate.Value);
		List<InventoryData> list = new List<InventoryData>();
		try
		{
			VehicleData vehicleData = v.vehicleData;
			if (((vehicleData != null) ? vehicleData.inventoryData : null) != null)
			{
				list.Add(v.vehicleData.inventoryData);
			}
		}
		catch
		{
		}
		list.AddRange(Carried(pc));
		bool flag = false;
		int[] ammoIds = AmmoIds;
		foreach (int id in ammoIds)
		{
			if (AutoIO.Take(list, id, 1f) >= 1f)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			Msg(L.T("沒有子彈：貨艙或背包裡要有 5.56mm 或 7.62x39mm 子彈"));
			_fireAt = now + 1f;
			return;
		}
		Vector2 val2 = target;
		Vector2 val5 = gunPos;
		Vector2 muzzle = val5 + new Vector2(0f, alt * 1.8f);
		int num2 = 0;
		BasicCharacterController val7 = null;
		CharacterBodyPart val8 = null;
		try
		{
			float num3 = float.MaxValue;
			Vector2 val10;
			foreach (Collider2D item in (Il2CppArrayBase<Collider2D>)(object)Physics2D.OverlapCircleAll(val2, HeliPlugin.HeliGunAim.Value))
			{
				if ((Object)(object)item == (Object)null)
				{
					continue;
				}
				CharacterBodyPart component = ((Component)item).GetComponent<CharacterBodyPart>();
				BasicCharacterController val9 = (((Object)(object)component != (Object)null) ? component.character : ((Component)item).GetComponentInParent<BasicCharacterController>());
				if (!((Object)(object)val9 == (Object)null) && !(((Il2CppObjectBase)val9).Pointer == ((Il2CppObjectBase)pc).Pointer) && !val9.deadFlag && Hostile(val9))
				{
					val10 = ((Vector2)(((Component)val9).transform.position)) - val2;
					float sqrMagnitude = val10.sqrMagnitude;
					if (sqrMagnitude < num3)
					{
						num3 = sqrMagnitude;
						val7 = val9;
						val8 = component;
					}
				}
			}
			if ((Object)(object)val7 != (Object)null && Random.value < 0.85f)
			{
				if ((Object)(object)val8 == (Object)null)
				{
					val8 = ((Component)val7).GetComponentInChildren<CharacterBodyPart>();
				}
				val2 = ((Vector2)(((Component)val7).transform.position)) + Random.insideUnitCircle * 0.2f;
				BasicCharacterController obj2 = val7;
				float value = HeliPlugin.HeliGunDamage.Value;
				Vector2 val11 = val2;
				val10 = val2 - val5;
				obj2.OnHit(value, val11, val10.normalized * 4f, val8, 1f, true, false, true, false);
				num2 = 1;
			}
			else
			{
				val2 += Random.insideUnitCircle * HeliPlugin.HeliGunSpread.Value;
			}
		}
		catch (Exception ex)
		{
			if (_hitLog++ < 5)
			{
				Report.Note("heli hit: " + ex.Message);
			}
		}
		if (num2 > 0 && _hitLog++ < 12)
		{
			float value2 = -1f;
			string value3 = "?";
			try
			{
				value2 = val7.hp;
				value3 = ((Il2CppSystem.Reflection.MemberInfo)((Il2CppSystem.Object)val7).GetIl2CppType()).Name;
			}
			catch
			{
			}
			Report.Note($"HELI 機槍命中 {value3}，傷害 {HeliPlugin.HeliGunDamage.Value:0}，剩 HP {value2:0.#}");
		}
		HeliAudio.Gun(gunPos);
		try
		{
			GameController.instance.PlayerNoise(((Vector2)(val2)), HeliPlugin.HeliNoise.Value, false, true, ((BasicCharacterController)pc).characterData.zLevel, (BasicCharacterController)null);
		}
		catch
		{
		}
		HeliFx.Shot(muzzle, val2, num2 > 0, heading, alt * 1.8f, _sortLayer);
	}

	private static void TickFx(float now)
	{
		HeliFx.Tick();
	}

	private static void Draw(HumanCharacterController pc, float dt, float now)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0318: Unknown result type (might be due to invalid IL or missing references)
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0385: Unknown result type (might be due to invalid IL or missing references)
		//IL_039f: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ac: Unknown result type (might be due to invalid IL or missing references)
		HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
		var s_liveVehicles = BasicVehicle.s_liveVehicles;
		Vector2 val = ((Vector2)(((Component)pc).transform.position));
		if (s_liveVehicles != null)
		{
			Vector3 val4 = default(Vector3);
			for (int i = 0; i < s_liveVehicles.Count; i++)
			{
				BasicVehicle val2 = s_liveVehicles[i];
				if (IsHeli(val2))
				{
					hashSet.Add(((Il2CppObjectBase)val2).Pointer);
					Vis vis = EnsureVis(val2);
					HideCar(val2, vis, now);
					Air value;
					float num = (_air.TryGetValue(((Il2CppObjectBase)val2).Pointer, out value) ? value.Alt : 0f);
					Vector2 val3 = VehPos(val2);
					val4 = new Vector3(val3.x, val3.y, ((Component)val2).transform.position.z);
					float num2 = value?.Heading ?? (VehAng(val2) + _fwd);
					vis.Root.transform.position = new Vector3(val4.x, val4.y + num * 1.8f, val4.z - 0.6f);
					((Component)vis.Body).transform.localRotation = Quaternion.Euler(0f, 0f, num2);
					vis.ShadowGo.transform.position = new Vector3(val4.x + 0.12f + num * 0.5f, val4.y - 0.12f, val4.z - 0.55f);
					vis.ShadowGo.transform.rotation = Quaternion.Euler(0f, 0f, num2);
					float num3 = Mathf.Lerp(1f, 0.85f, num);
					vis.ShadowGo.transform.localScale = new Vector3(1.3f * num3, 1.3f * num3, 1f);
					vis.Shadow.color = new Color(0.05f, 0.07f, 0.04f, Mathf.Lerp(0.38f, 0.22f, num));
					float num4 = (EngineOn(val2) ? 1500f : 0f);
					vis.RotorSpeed = Mathf.MoveTowards(vis.RotorSpeed, num4, ((num4 > vis.RotorSpeed) ? 500f : 260f) * dt);
					vis.RotorAng = (vis.RotorAng + vis.RotorSpeed * dt) % 360f;
					float num5 = num2 * ((float)Math.PI / 180f);
					Vector3 localPosition = Vector3.zero;
					PlaceGuns(vis, num2);
					((Component)vis.Rotor).transform.localPosition = localPosition;
					((Component)vis.Rotor2).transform.localPosition = localPosition;
					((Component)vis.Rotor).transform.localRotation = Quaternion.Euler(0f, 0f, num2 + 15f + vis.RotorAng);
					((Component)vis.Rotor2).transform.localRotation = Quaternion.Euler(0f, 0f, num2 + 15f + vis.RotorAng + 45f);
					float num6 = Mathf.Clamp01(vis.RotorSpeed / 1500f);
					vis.Rotor.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.55f, num6));
					vis.Rotor2.color = new Color(1f, 1f, 1f, 0.45f * num6);
					GameObject root = vis.Root;
					ref AudioSource sound = ref vis.Sound;
					Vector2 val5 = ((Vector2)(val4)) - val;
					HeliAudio.Rotor(root, ref sound, num6, val5.magnitude);
				}
			}
		}
		List<IntPtr> list = new List<IntPtr>();
		foreach (IntPtr key in _vis.Keys)
		{
			if (!hashSet.Contains(key))
			{
				list.Add(key);
			}
		}
		foreach (IntPtr item in list)
		{
			Vis vis2 = _vis[item];
			try
			{
				Object.Destroy((Object)(object)vis2.Root);
				Object.Destroy((Object)(object)vis2.ShadowGo);
			}
			catch
			{
			}
			_vis.Remove(item);
		}
	}
}
