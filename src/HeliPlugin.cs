using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace ZedHelicopter;

// Standalone helicopter: no power grid or helipad, call one in with a key.
[BepInPlugin(Guid, "Helicopter", Version)]
public class HeliPlugin : BasePlugin
{
	public const string Guid = "girthy_mods.zedzone.helicopter";

	public const string Version = "1.3.0";

	internal const string IndustrialAgeGuid = "zedzone.powerindustry";

	internal static ManualLogSource L;

	internal static ConfigEntry<bool> HeliEnabled;

	internal static ConfigEntry<KeyCode> CallKey;

	internal static ConfigEntry<int> MaxHelicopters;

	internal static ConfigEntry<float> CallRadius;

	internal static ConfigEntry<float> HeliSpeed;

	internal static ConfigEntry<float> HeliLitersPerMinute;

	internal static ConfigEntry<float> HeliGunDamage;

	internal static ConfigEntry<float> HeliGunAim;

	internal static ConfigEntry<float> HeliGunRate;

	internal static ConfigEntry<bool> DoorGuns;

	internal static ConfigEntry<float> HeliGunSpread;

	internal static ConfigEntry<string> GunSound;

	internal static ConfigEntry<bool> ReturnCasings;

	internal static ConfigEntry<bool> ReloadRecipes;

	internal static ConfigEntry<bool> Rockets;

	internal static ConfigEntry<bool> RocketsNeedAmmo;

	internal static ConfigEntry<float> RocketInterval;

	internal static ConfigEntry<float> RocketDamage;

	internal static ConfigEntry<bool> BuildMenuTab;

	internal static ConfigEntry<bool> RocketRecipe;

	internal static ConfigEntry<float> HeliFxScale;

	internal static ConfigEntry<float> HeliVolume;

	internal static ConfigEntry<float> HeliNoise;

	internal static ConfigEntry<string> Language;

	internal static ConfigEntry<bool> Diagnostics;

	public override void Load()
	{
		L = Log;
		HeliEnabled = Config.Bind("General", "Enabled", true, "Turn the helicopter mod on or off.");
		CallKey = Config.Bind("General", "CallKey", KeyCode.Home, "Press to call a helicopter. It lands a few metres in front of you (not while driving, and not when one is already close by).");
		MaxHelicopters = Config.Bind("General", "MaxHelicopters", 1, new ConfigDescription("How many helicopters each save can call in. 0 = no limit.", new AcceptableValueRange<int>(0, 20)));
		CallRadius = Config.Bind("General", "NearbyRadius", 40f, new ConfigDescription("You can't call another helicopter while one is parked this close (metres).", new AcceptableValueRange<float>(0f, 500f)));
		Language = Config.Bind("General", "Language", "Auto", "UI language: Auto, zh-TW, zh-CN, en, ko, ja, ru, de, fr, es, pt, pl.");
		HeliSpeed = Config.Bind("Flight", "Speed", 14f, "Top flying speed (tiles per second).");
		HeliLitersPerMinute = Config.Bind("Flight", "LitersPerMinute", 4f, "Fuel burned per real-time minute while flying.");
		HeliNoise = Config.Bind("Flight", "NoiseRadius", 30f, "Zombies within this range hear the rotor and the gun.");
		HeliGunDamage = Config.Bind("MachineGun", "Damage", 400f, "Damage per round (it locks on to the zombie nearest the mouse; a normal zombie has about 1100 HP).");
		HeliGunAim = Config.Bind("MachineGun", "AimRadius", 1.6f, "Lock-on range around the mouse pointer (tiles).");
		HeliGunRate = Config.Bind("MachineGun", "RoundsPerSecond", 8f, "Machine gun fire rate.");
		DoorGuns = Config.Bind("MachineGun", "DoorGunners", true, "Players riding as passengers man the door miniguns (1st passenger left, 2nd right) and aim them with the mouse. The pilot fires them with the right mouse button only while no other player is aboard.");
		GunSound = Config.Bind("MachineGun", "SoundFromWeapon", "M249", "The door guns use the firing sound of the game gun whose name contains this (for example M249, RPK, GM6 Lynx for a .50 cal). Empty = pick a machine gun automatically.");
		ReturnCasings = Config.Bind("MachineGun", "ReturnCasings", true, "Every door gun round gives back its spent casing (5.56x45 Shell or 7.62x39 Shell): onto a stack in your bag, else the helicopter's cargo, else a new spot in your bag.");
		ReloadRecipes = Config.Bind("MachineGun", "ReloadRecipes", true, "If the game (or the Tank mod) has no recipe that reloads 5.56 / 7.62x39 rounds from their casings, add one at the reloader: 10 casings + 1 Gunpowder + 1 Copper Plate = 10 rounds. Takes effect after a restart.");
		Rockets = Config.Bind("Rockets", "Enabled", true, "The pilot fires the rocket pods with the left mouse button (towards the mouse).");
		RocketsNeedAmmo = Config.Bind("Rockets", "NeedAmmo", true, "Each rocket uses one 84mm HE Round from the cargo hold or your backpack.");
		RocketInterval = Config.Bind("Rockets", "SecondsBetweenRockets", 0.5f, new ConfigDescription("Time between rockets while the button is held.", new AcceptableValueRange<float>(0.1f, 5f)));
		RocketDamage = Config.Bind("Rockets", "DirectHitDamage", 1f, new ConfigDescription("Multiplier for a rocket's direct hit damage (the blast is the game's own).", new AcceptableValueRange<float>(0f, 5f)));
		BuildMenuTab = Config.Bind("General", "BuildMenuTab", true, "The helicopter is in the Build menu's VEHICLES tab and can be built from the materials you carry (Iron Plate 80, Metal Component 30, Copper Tube 15, Iron Wire 25, Copper Wire 20).");
		RocketRecipe = Config.Bind("Rockets", "WorkbenchRecipe", true, "84mm HE Rounds can be crafted at a workbench, 2 at a time (Gunpowder 6, Iron Plate 2, Copper Tube 1, Metal Component 1). Takes effect after a restart.");
		HeliGunSpread = Config.Bind("MachineGun", "Spread", 0.6f, "Machine gun spread radius (tiles).");
		HeliFxScale = Config.Bind("Effects", "EffectScale", 2f, "Size of tracers, muzzle flash, impacts and shell casings.");
		HeliVolume = Config.Bind("Effects", "Volume", 0.8f, new ConfigDescription("Rotor and gun volume.", new AcceptableValueRange<float>(0f, 1f)));
		Diagnostics = Config.Bind("Debug", "Diagnostics", false, "Write details and errors to BepInEx\\Helicopter-report.txt.");
		AddComponent<HeliRunner>();
		Log.LogInfo($"Helicopter {Version} loaded. Call key: {CallKey.Value}. Max per save: {MaxHelicopters.Value}.");
	}

	private static bool _patched;

	internal static bool IndustrialAgeLoaded()
	{
		try
		{
			return IL2CPPChainloader.Instance.Plugins.ContainsKey(IndustrialAgeGuid);
		}
		catch
		{
			return false;
		}
	}

	// Patches go in on the first frame, after every plugin has loaded, so we can step
	// aside when Industrial Age (which has its own copy of this helicopter) is installed.
	internal static bool EnsurePatched()
	{
		if (_patched)
		{
			return true;
		}
		_patched = true;
		Harmony h = new Harmony(Guid);
		Type[] types = new Type[]
		{
			typeof(P_HeliMouseDown),
			typeof(P_HeliMouseHold),
			typeof(P_HeliName1),
			typeof(P_HeliName2),
			typeof(P_HeliName3),
			typeof(P_HeliFixed),
			typeof(P_HeliDriving),
			typeof(P_HeliSafety),
			typeof(P_HeliNoExit1),
			typeof(P_HeliNoExit2),
			typeof(P_HeliInspect),
			typeof(P_HeliInspectOpen),
			typeof(P_HeliBuildEnsure),
			typeof(P_HeliBuildOpen),
			typeof(P_HeliBuildTab),
			typeof(P_HeliBuildList),
			typeof(P_HeliBuildAttr),
			typeof(P_HeliBuildCard),
			typeof(P_HeliBuildDetails),
			typeof(P_HeliBuildGo),
			typeof(P_HeliBuildCardClick),
			typeof(P_HeliBuildAttrName),
			typeof(P_HeliBuildAttrDesc),
			typeof(P_HeliBuildAttrCan)
		};
		int safety = HeliSafety.Patch(h);
		int ok = 0;
		foreach (Type t in types)
		{
			try
			{
				h.PatchAll(t);
				ok++;
			}
			catch (Exception ex)
			{
				L.LogWarning("patch " + t.Name + " failed: " + ex.Message);
			}
		}
		int saves = 0;
		foreach (string name in new string[2] { "SaveGameData", "SaveGameDataAsync" })
		{
			foreach (MethodInfo m in AccessTools.GetDeclaredMethods(typeof(GameDataManager)))
			{
				if (m.Name != name)
				{
					continue;
				}
				try
				{
					h.Patch(m, new HarmonyMethod(typeof(HeliPlugin), "OnSave"));
					saves++;
				}
				catch (Exception ex2)
				{
					L.LogWarning("save hook " + name + " failed: " + ex2.Message);
				}
			}
		}
		L.LogInfo($"Patches: {ok}/{types.Length} ok, rocket safety {safety}/6, save hooks {saves}.");
		return true;
	}

	private static void OnSave()
	{
		try
		{
			Heli.OnGameSave();
		}
		catch
		{
		}
	}
}

public class HeliRunner : MonoBehaviour
{
	internal static bool Active;

	private bool _checked;

	private bool _off;

	private string _offMsg;

	private float _offUntil;

	private GUIStyle _style;

	private Texture2D _white;

	public HeliRunner(IntPtr ptr)
		: base(ptr)
	{
	}

	private void Update()
	{
		if (!_checked)
		{
			_checked = true;
			if (HeliPlugin.IndustrialAgeLoaded())
			{
				_off = true;
				_offMsg = "Helicopter mod is off: Industrial Age is installed and has its own helicopter. Unsubscribe from one of them.";
				HeliPlugin.L.LogWarning(_offMsg);
			}
			else
			{
				HeliPlugin.EnsurePatched();
			}
		}
		Active = !_off && HeliPlugin.HeliEnabled.Value;
		if (!Active)
		{
			if (_off && _offUntil == 0f && GameController.instance != null && GameController.instance.playerCharacter != null)
			{
				_offUntil = Time.realtimeSinceStartup + 12f;
			}
			return;
		}
		try
		{
			HeliBuild.Tick();
			HeliCasings.Check();
		}
		catch
		{
		}
		try
		{
			Heli.Tick(Time.deltaTime);
		}
		catch (Exception ex)
		{
			Report.Note("heli tick: " + ex.Message);
		}
	}

	private void OnGUI()
	{
		try
		{
			if (Event.current == null || (int)Event.current.type != 7)
			{
				return;
			}
			string text = null;
			if (_off)
			{
				if (Time.realtimeSinceStartup < _offUntil)
				{
					text = _offMsg;
				}
			}
			else if (HeliPlugin.HeliEnabled.Value)
			{
				text = Heli.Message();
				if (text == null && !string.IsNullOrEmpty(Heli.NearStatus))
				{
					text = Heli.NearStatus;
				}
			}
			if (!string.IsNullOrEmpty(text))
			{
				Hint(text);
			}
		}
		catch
		{
		}
	}

	private void Hint(string text)
	{
		if (_style == null)
		{
			_style = new GUIStyle(GUI.skin.label);
			_style.alignment = TextAnchor.MiddleCenter;
			_style.fontSize = 15;
			_style.normal.textColor = Color.white;
			_white = new Texture2D(1, 1);
			_white.SetPixel(0, 0, Color.white);
			_white.Apply();
			((Object)_white).hideFlags = HideFlags.HideAndDontSave;
		}
		float s = Mathf.Clamp(Screen.height / 1080f, 0.75f, 2f);
		Matrix4x4 old = GUI.matrix;
		GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
		float sw = Screen.width / s;
		float sh = Screen.height / s;
		float w = Mathf.Min(Mathf.Max(460f, _style.CalcSize(new GUIContent(text)).x + 40f), sw - 40f);
		float h = 34f;
		float x = (sw - w) / 2f;
		float y = sh * 0.72f - 42f;
		Color c = GUI.color;
		GUI.color = new Color(0.07f, 0.07f, 0.08f, 0.85f);
		GUI.DrawTexture(new Rect(x, y, w, h), _white);
		GUI.color = new Color(0.35f, 0.65f, 1f, 1f);
		GUI.DrawTexture(new Rect(x, y, 3f, h), _white);
		GUI.color = c;
		GUI.Label(new Rect(x, y, w, h), text, _style);
		GUI.matrix = old;
	}
}
