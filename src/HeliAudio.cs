using System;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;

namespace ZedHelicopter;

internal static class HeliAudio
{
	private static bool _init;

	private static bool _rotorIsEngine;

	private static AudioClip _rotor;

	private static AudioClip _start;

	private static AudioClip _gun;

	private static AudioMixerGroup _group;

	private static AudioSource _shots;

	private static float Vol => Mathf.Clamp01(HeliPlugin.HeliVolume.Value);

	internal static AudioMixerGroup SfxGroup
	{
		get
		{
			Init();
			return _group;
		}
	}

	private static void Init()
	{
		if (_init)
		{
			return;
		}
		_init = true;
		StringBuilder stringBuilder = new StringBuilder("HELI 音效：");
		StringBuilder stringBuilder3;
		StringBuilder.AppendInterpolatedStringHandler handler;
		try
		{
			StringBuilder stringBuilder2 = new StringBuilder();
			foreach (AudioClip item in Resources.FindObjectsOfTypeAll<AudioClip>())
			{
				if ((Object)(object)item == (Object)null)
				{
					continue;
				}
				string text = (((Object)item).name ?? "").ToLowerInvariant();
				if (text.Contains("heli") || text.Contains("rotor") || text.Contains("chopper") || text.Contains("propel"))
				{
					stringBuilder2.Append(((Object)item).name).Append(' ');
					if (!text.Contains("crash") && !text.Contains("explo") && ((Object)(object)_rotor == (Object)null || text == "helicopter" || text.Contains("loop")))
					{
						_rotor = item;
					}
				}
			}
			stringBuilder3 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder3;
			handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder3);
			handler.AppendLiteral("旋翼候選［");
			handler.AppendFormatted(stringBuilder2);
			handler.AppendLiteral("］");
			stringBuilder4.Append(ref handler);
		}
		catch (Exception ex)
		{
			stringBuilder.Append("clips " + ex.Message + "；");
		}
		try
		{
			foreach (Engine item2 in Resources.FindObjectsOfTypeAll<Engine>())
			{
				if (!((Object)(object)item2 == (Object)null))
				{
					if ((Object)(object)_start == (Object)null && (Object)(object)item2.startUp != (Object)null)
					{
						_start = item2.startUp;
					}
					if ((Object)(object)_rotor == (Object)null && (Object)(object)item2.engineLoopClip != (Object)null)
					{
						_rotor = item2.engineLoopClip;
						_rotorIsEngine = true;
					}
				}
			}
		}
		catch (Exception ex2)
		{
			stringBuilder.Append("engine " + ex2.Message + "；");
		}
		try
		{
			int num = -1;
			StringBuilder stringBuilder5 = new StringBuilder();
			foreach (BasicRangedWeapon item3 in Resources.FindObjectsOfTypeAll<BasicRangedWeapon>())
			{
				if (!((Object)(object)item3 == (Object)null) && !((Object)(object)item3.fireClip == (Object)null))
				{
					string text2 = (((Object)item3).name + "|" + ((Object)item3.fireClip).name).ToLowerInvariant();
					if (stringBuilder5.Length < 500)
					{
						stringBuilder5.Append(((Object)item3).name).Append('/').Append(((Object)item3.fireClip).name)
							.Append(' ');
					}
					int num2 = 1;
					if (text2.Contains("pistol") || text2.Contains("shotgun") || text2.Contains("bow") || text2.Contains("flare") || text2.Contains("launcher") || text2.Contains("revolver"))
					{
						num2 = 0;
					}
					if (text2.Contains("ak") || text2.Contains("m4") || text2.Contains("rifle") || text2.Contains("556") || text2.Contains("762") || text2.Contains("ar15") || text2.Contains("scar"))
					{
						num2 = 2;
					}
					if (text2.Contains("m249") || text2.Contains("mg") || text2.Contains("machine") || text2.Contains("minigun") || text2.Contains("lmg") || text2.Contains("m60") || text2.Contains("pkm") || text2.Contains("rpk"))
					{
						num2 = 3;
					}
					if (num2 > num)
					{
						num = num2;
						_gun = item3.fireClip;
					}
				}
			}
			stringBuilder3 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder3;
			handler = new StringBuilder.AppendInterpolatedStringHandler(4, 1, stringBuilder3);
			handler.AppendLiteral("｜槍［");
			handler.AppendFormatted(stringBuilder5);
			handler.AppendLiteral("］");
			stringBuilder6.Append(ref handler);
		}
		catch (Exception ex3)
		{
			stringBuilder.Append("guns " + ex3.Message + "；");
		}
		try
		{
			StringBuilder stringBuilder7 = new StringBuilder();
			int num3 = -1;
			foreach (AudioSource item4 in Resources.FindObjectsOfTypeAll<AudioSource>())
			{
				AudioMixerGroup val = ((item4 != null) ? item4.outputAudioMixerGroup : null);
				if (!((Object)(object)val == (Object)null))
				{
					string text3 = (((Object)val).name ?? "").ToLowerInvariant();
					if (stringBuilder7.Length < 200 && stringBuilder7.ToString().IndexOf(((Object)val).name) < 0)
					{
						stringBuilder7.Append(((Object)val).name).Append(' ');
					}
					int num4 = 1;
					if (text3.Contains("music") || text3.Contains("bgm") || text3.Contains("ui") || text3.Contains("voice") || text3.Contains("ambient"))
					{
						num4 = 0;
					}
					if (text3.Contains("sfx") || text3.Contains("effect") || text3.Contains("sound") || text3.Contains("world"))
					{
						num4 = 2;
					}
					if (num4 > num3)
					{
						num3 = num4;
						_group = val;
					}
				}
			}
			stringBuilder3 = stringBuilder;
			StringBuilder stringBuilder8 = stringBuilder3;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 2, stringBuilder3);
			handler.AppendLiteral("｜混音群組［");
			handler.AppendFormatted(stringBuilder7);
			handler.AppendLiteral("］→ ");
			handler.AppendFormatted(((Object)(object)_group != (Object)null) ? ((Object)_group).name : "無");
			stringBuilder8.Append(ref handler);
		}
		catch (Exception ex4)
		{
			stringBuilder.Append("mixer " + ex4.Message);
		}
		stringBuilder3 = stringBuilder;
		StringBuilder stringBuilder9 = stringBuilder3;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 4, stringBuilder3);
		handler.AppendLiteral("｜選用 旋翼=");
		AudioClip rotor = _rotor;
		handler.AppendFormatted((rotor != null) ? ((Object)rotor).name : null);
		handler.AppendFormatted(_rotorIsEngine ? "（引擎聲）" : "");
		handler.AppendLiteral(" 啟動=");
		AudioClip start = _start;
		handler.AppendFormatted((start != null) ? ((Object)start).name : null);
		handler.AppendLiteral(" 槍=");
		AudioClip gun = _gun;
		handler.AppendFormatted((gun != null) ? ((Object)gun).name : null);
		stringBuilder9.Append(ref handler);
		Report.Note(stringBuilder.ToString());
	}

	private static AudioSource NewSource(GameObject host)
	{
		AudioSource val = host.AddComponent<AudioSource>();
		val.playOnAwake = false;
		val.spatialBlend = 0f;
		val.dopplerLevel = 0f;
		if ((Object)(object)_group != (Object)null)
		{
			val.outputAudioMixerGroup = _group;
		}
		return val;
	}

	private static AudioSource Shots()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		if ((Object)(object)_shots != (Object)null)
		{
			return _shots;
		}
		GameObject val = new GameObject("zpi_heli_audio");
		Object.DontDestroyOnLoad((Object)val);
		((Object)val).hideFlags = (HideFlags)61;
		_shots = NewSource(val);
		return _shots;
	}

	private static float Falloff(float dist)
	{
		return Mathf.Clamp01(1f - dist / 45f);
	}

	private static float DistToPlayer(Vector2 p)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Vector2 val = ((Vector2)(((Component)GameController.instance.playerCharacter).transform.position)) - p;
			return val.magnitude;
		}
		catch
		{
			return 0f;
		}
	}

	public static void Start(Vector2 at)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		Init();
		try
		{
			if ((Object)(object)_start != (Object)null)
			{
				Shots().PlayOneShot(_start, Vol * 0.9f * Falloff(DistToPlayer(at)));
			}
		}
		catch
		{
		}
	}

	public static void Stop(Vector2 at)
	{
	}

	// Weapon sounds are loaded on demand by the game (AudioController.LoadRangedWeaponClip), so
	// they are not around yet when the rotor sound is set up. They are fetched by the weapon's
	// item data the first time a gun or rocket fires.
	private static AudioClip _gunClip;

	private static AudioClip _rocketClip;

	private static bool _gunTried;

	private static bool _rocketTried;

	private static AudioSource _gunSrc;

	internal static AudioClip LoadWeaponClip(ItemAttr_RangedWeapon w)
	{
		if (w == null)
		{
			return null;
		}
		string name = null;
		try
		{
			name = w.fireClip;
		}
		catch
		{
		}
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		AudioClip c = null;
		foreach (string slot in new string[3] { "fire", "Fire", "fireClip" })
		{
			try
			{
				c = AudioController.instance?.LoadRangedWeaponClip(w, slot, name);
			}
			catch
			{
			}
			if ((Object)(object)c != (Object)null)
			{
				return c;
			}
		}
		try
		{
			foreach (AudioClip a in Resources.FindObjectsOfTypeAll<AudioClip>())
			{
				if ((Object)(object)a != (Object)null && string.Equals(((Object)a).name, name, StringComparison.OrdinalIgnoreCase))
				{
					return a;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static int ScoreGun(string t, bool auto)
	{
		string want = (HeliPlugin.GunSound.Value ?? "").Trim().ToLowerInvariant();
		if (want.Length > 0 && t.Contains(want))
		{
			return 100;
		}
		if (t.Contains("flame") || t.Contains("laser") || t.Contains("water") || t.Contains("nail") || t.Contains("grenade") || t.Contains("rocket") || t.Contains("taser") || t.Contains("bow") || t.Contains("flare") || t.Contains("launcher") || t.Contains("recoilless") || t.Contains("submachine"))
		{
			return -1;
		}
		int s = 1;
		if (t.Contains("m249") || t.Contains("machine gun") || t.Contains("rpk") || t.Contains("m60") || t.Contains("pkm") || t.Contains("minigun"))
		{
			s = 7;
		}
		else if (t.Contains("ak") || t.Contains("rifle"))
		{
			s = 2;
		}
		return s + (auto ? 1 : 0);
	}

	private static AudioClip GunClip()
	{
		if ((Object)(object)_gunClip != (Object)null || _gunTried)
		{
			return _gunClip;
		}
		_gunTried = true;
		ItemAttr_RangedWeapon best = null;
		int bs = -1;
		try
		{
			var list = ItemManager.instance.itemList;
			for (int i = 0; i < list.Count; i++)
			{
				ItemAttr_RangedWeapon w = list[i]?.TryCast<ItemAttr_RangedWeapon>();
				if (w == null || string.IsNullOrEmpty(w.fireClip))
				{
					continue;
				}
				int s = ScoreGun(((w.ItemName_EN ?? "") + "|" + w.fireClip).ToLowerInvariant(), w.fullAuto);
				if (s > bs)
				{
					bs = s;
					best = w;
				}
			}
		}
		catch (Exception e)
		{
			Report.Note("HELI gun sound search: " + e.Message);
		}
		_gunClip = LoadWeaponClip(best);
		if ((Object)(object)_gunClip == (Object)null)
		{
			_gunClip = _gun;
		}
		string bn = "?";
		try
		{
			bn = best?.ItemName_EN;
		}
		catch
		{
		}
		HeliPlugin.L.LogInfo($"Door gun sound from '{bn}' -> {(((Object)(object)_gunClip != (Object)null) ? ((Object)_gunClip).name : "not found")}");
		return _gunClip;
	}

	private static AudioClip RocketClip()
	{
		if ((Object)(object)_rocketClip != (Object)null || _rocketTried)
		{
			return _rocketClip;
		}
		_rocketTried = true;
		_rocketClip = LoadWeaponClip(HeliRockets.Launcher);
		HeliPlugin.L.LogInfo($"Rocket sound -> {(((Object)(object)_rocketClip != (Object)null) ? ((Object)_rocketClip).name : "not found")}");
		return _rocketClip;
	}

	// One game audio source for the guns: every round starts its own copy of the clip, so a held
	// trigger sounds like a real machine gun (the game's PlayAt skips a clip that is still playing).
	private static void PlayShot(AudioClip c, Vector2 at, float vol)
	{
		if ((Object)(object)c == (Object)null)
		{
			return;
		}
		try
		{
			if ((Object)(object)_gunSrc == (Object)null)
			{
				_gunSrc = AudioController.instance?.CreatePersistentAudioSource();
				if ((Object)(object)_gunSrc != (Object)null)
				{
					_gunSrc.playOnAwake = false;
					_gunSrc.loop = false;
					_gunSrc.volume = 1f;
				}
			}
			if ((Object)(object)_gunSrc != (Object)null)
			{
				((Component)_gunSrc).transform.position = new Vector3(at.x, at.y, 0f);
				_gunSrc.pitch = Random.Range(0.96f, 1.04f);
				_gunSrc.PlayOneShot(c, vol * Vol);
				return;
			}
		}
		catch
		{
		}
		try
		{
			Shots().PlayOneShot(c, vol * Vol * Falloff(DistToPlayer(at)));
		}
		catch
		{
		}
	}

	public static void Gun(Vector2 at)
	{
		Init();
		PlayShot(GunClip(), at, 0.7f);
	}

	public static void Rocket(Vector2 at)
	{
		Init();
		AudioClip c = RocketClip();
		if ((Object)(object)c == (Object)null)
		{
			return;
		}
		try
		{
			AudioController.instance.PlayAt(c, new Vector3(at.x, at.y, 0f), Vol, AudioSpatial.Medium, 0f, AudioCategory.Sfx);
		}
		catch
		{
			PlayShot(c, at, 1f);
		}
	}

	public static void Rotor(GameObject host, ref AudioSource src, float rpm, float dist)
	{
		Init();
		if ((Object)(object)_rotor == (Object)null || (Object)(object)host == (Object)null)
		{
			return;
		}
		try
		{
			if ((Object)(object)src == (Object)null)
			{
				src = NewSource(host);
				src.clip = _rotor;
				src.loop = true;
			}
			float num = Vol * Mathf.Clamp01(rpm * 1.2f) * Falloff(dist) * (_rotorIsEngine ? 0.8f : 0.7f);
			if (num < 0.01f)
			{
				if (src.isPlaying)
				{
					src.Stop();
				}
				return;
			}
			src.volume = num;
			src.pitch = (_rotorIsEngine ? Mathf.Lerp(0.35f, 0.62f, rpm) : Mathf.Lerp(0.6f, 1f, rpm));
			if (!src.isPlaying)
			{
				src.Play();
			}
		}
		catch
		{
		}
	}
}
