using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace ZedHelicopter;

// The pilot's rocket pods fire the game's own rocket: the recoilless rifle round (RecoillessRound).
// The launcher is found among the game's guns by its bullet prefab, so the ammo item,
// speed and blast come from the game data.
internal static class HeliRockets
{
	private static GameObject _prefab;
	private static float _retryAt;
	private static bool _logged;
	private static int _launchLog;
	internal static int AmmoId = -1;
	internal static int LauncherId = -1;
	internal static string AmmoName = "84mm HE Round";
	internal static ItemAttr_RangedWeapon Launcher;
	private static float _speed = 40f;

	internal static bool Find()
	{
		if ((Object)(object)_prefab != (Object)null) return true;
		float now = Time.realtimeSinceStartup;
		if (now < _retryAt) return false;
		_retryAt = now + 10f;
		ItemManager im = ItemManager.instance;
		if (im == null || im.itemList == null) return false;
		var log = new StringBuilder();
		try
		{
			var list = im.itemList;
			for (int i = 0; i < list.Count && (Object)(object)_prefab == (Object)null; i++)
			{
				ItemAttr_RangedWeapon w = list[i]?.TryCast<ItemAttr_RangedWeapon>();
				if (w == null) continue;
				string bp = null;
				try { bp = w.bulletPrefabName; } catch { }
				if (string.IsNullOrEmpty(bp)) continue;
				GameObject go = null;
				try { go = DeployableItem_Turret.LoadBulletPrefab(bp); } catch { }
				if ((Object)(object)go == (Object)null) { try { go = Resources.Load<GameObject>(bp); } catch { } }
				if ((Object)(object)go == (Object)null) continue;
				RecoillessRound rr = go.GetComponent<RecoillessRound>();
				if ((Object)(object)rr == (Object)null) { try { rr = go.GetComponentInChildren<RecoillessRound>(true); } catch { } }
				if ((Object)(object)rr == (Object)null) continue;
				_prefab = go;
				AmmoId = w.ammoId;
				LauncherId = w.itemId;
				Launcher = w;
				if (w.bulletSpeed > 1f) _speed = w.bulletSpeed;
				try { AmmoName = im.FindItemAttrById(AmmoId)?.ItemName_EN ?? AmmoName; } catch { }
				string wn = "?";
				try { wn = w.ItemName_EN; } catch { }
				log.Append($"launcher {LauncherId} '{wn}', prefab '{bp}', ammo {AmmoId} '{AmmoName}', speed {w.bulletSpeed}, range {w.fireRange}");
			}
		}
		catch (Exception e) { log.Append("weapon scan failed: " + e.Message); }
		if (_prefab != null || !_logged)
		{
			_logged = true;
			Report.Note("HELI rockets: " + (log.Length > 0 ? log.ToString() : "no recoilless launcher found yet"));
		}
		return (Object)(object)_prefab != (Object)null;
	}

	internal static bool Launch(HumanCharacterController pc, Vector2 muzzle, Vector2 target)
	{
		if (!Find()) return false;
		try
		{
			Vector2 d = target - muzzle;
			float angle = Mathf.Atan2(d.y, d.x) * 57.29578f;
			GameObject go = Object.Instantiate<GameObject>(_prefab, new Vector3(muzzle.x, muzzle.y, ((Component)pc).transform.position.z), Quaternion.Euler(0f, 0f, angle));
			go.SetActive(true);
			RecoillessRound rr = go.GetComponent<RecoillessRound>() ?? go.GetComponentInChildren<RecoillessRound>(true);
			if ((Object)(object)rr == (Object)null)
			{
				Object.Destroy((Object)(object)go);
				return false;
			}
			try { rr.throwerCharacter = pc; } catch { }
			try { rr.sourceItemId = LauncherId; } catch { }
			try { rr.throwerStandVz = ((BasicCharacterController)pc).characterData.zLevel; } catch { }
			// Pods have no back blast (it would hit the helicopter's own crew).
			try { rr.backBlastDamage = 0f; rr.backBlastForce = 0f; rr.backBlastRange = 0f; rr.backBlastSmokeCount = 0; } catch { }
			try { rr.directHitDamage *= Mathf.Max(0f, HeliPlugin.RocketDamage.Value); } catch { }
			HeliSafety.Mark(rr);
			rr.Init(muzzle, target, _speed);
			if (_launchLog++ < 5)
			{
				Report.Note($"HELI rocket away: {muzzle} -> {target} at {_speed:0.#}, direct hit {rr.directHitDamage:0}, range {rr.maxFlightRange:0.#}");
			}
			return true;
		}
		catch (Exception e)
		{
			Report.Note("HELI rocket launch: " + e.Message);
			return false;
		}
	}
}

// Our own rockets must not hurt the people in the helicopter. The game applies blast damage in
// BasicExplosion.ExplodeDamage (a coroutine), so while that runs for one of our rockets,
// hits on anyone in or right by a helicopter are dropped.
internal static class HeliSafety
{
	private static bool _active;
	private static readonly List<Vector3> _blasts = new List<Vector3>();
	private static readonly HashSet<IntPtr> _ours = new HashSet<IntPtr>();
	private static int _logged;
	private static bool _blastHook;

	// Without the blast hook: one of our rockets went off near here in the last moment.
	private static bool RecentBlastNear(Vector2 p)
	{
		float now = Time.time;
		foreach (Vector3 b in _blasts)
			if (now - b.z < 1.5f && ((Vector2)b - p).sqrMagnitude < 64f) return true;
		return false;
	}

	internal static int Patch(Harmony h)
	{
		int ok = 0;
		try
		{
			var mn = ExplodeCo.MoveNext();
			if (mn != null) { h.Patch(mn, new HarmonyMethod(typeof(HeliSafety), nameof(MoveNextPre)), new HarmonyMethod(typeof(HeliSafety), nameof(Post))); ok++; _blastHook = true; HeliPlugin.L.LogInfo("rocket safety: blast hook on " + ExplodeCo.Type.Name); }
			else HeliPlugin.L.LogWarning("rocket safety: ExplodeDamage coroutine not found (using the detonation time instead)");
		}
		catch (Exception e) { HeliPlugin.L.LogWarning("rocket safety (blast): " + e.Message); }
		try
		{
			var hit = AccessTools.Method(typeof(HumanCharacterController), nameof(HumanCharacterController.OnHit));
			if (hit != null) { h.Patch(hit, new HarmonyMethod(typeof(HeliSafety), nameof(OnHitPre))); ok++; }
		}
		catch (Exception e) { HeliPlugin.L.LogWarning("rocket safety (hit): " + e.Message); }
		try
		{
			var det = AccessTools.Method(typeof(RecoillessRound), nameof(RecoillessRound.Detonate));
			if (det != null) { h.Patch(det, new HarmonyMethod(typeof(HeliSafety), nameof(DetonatePre))); ok++; }
		}
		catch (Exception e) { HeliPlugin.L.LogWarning("rocket safety (detonate): " + e.Message); }
		try
		{
			var dh = AccessTools.Method(typeof(BasicGrenade), nameof(BasicGrenade.ApplyDirectHit));
			if (dh != null) { h.Patch(dh, new HarmonyMethod(typeof(HeliSafety), nameof(DirectPre)), new HarmonyMethod(typeof(HeliSafety), nameof(Post))); ok++; }
		}
		catch (Exception e) { HeliPlugin.L.LogWarning("rocket safety (direct): " + e.Message); }
		try
		{
			var imp = AccessTools.Method(typeof(RecoillessRound), nameof(RecoillessRound.IsValidImpact));
			if (imp != null) { h.Patch(imp, new HarmonyMethod(typeof(HeliSafety), nameof(ImpactPre))); ok++; }
		}
		catch (Exception e) { HeliPlugin.L.LogWarning("rocket safety (impact): " + e.Message); }
		try
		{
			var vh = AccessTools.Method(typeof(BasicVehicle), nameof(BasicVehicle.OnHit));
			if (vh != null) { h.Patch(vh, new HarmonyMethod(typeof(HeliSafety), nameof(VehicleHitPre))); ok++; }
		}
		catch (Exception e) { HeliPlugin.L.LogWarning("rocket safety (vehicle): " + e.Message); }
		return ok;
	}

	// Our rockets fly straight through the helicopter that fired them (and its crew).
	private static bool ImpactPre(RecoillessRound __instance, Collider2D col, ref bool __result)
	{
		try
		{
			if ((Object)(object)__instance == (Object)null || (Object)(object)col == (Object)null || !_ours.Contains(((Il2CppObjectBase)__instance).Pointer)) return true;
			BasicVehicle v = ((Component)col).GetComponentInParent<BasicVehicle>();
			if ((Object)(object)v != (Object)null && Heli.IsHeli(v)) { __result = false; return false; }
			HumanCharacterController hc = ((Component)col).GetComponentInParent<HumanCharacterController>();
			if ((Object)(object)hc != (Object)null && Heli.InOrByHeli(hc)) { __result = false; return false; }
		}
		catch { }
		return true;
	}

	// Our own blasts, or anything the crew does, never damage the helicopter.
	private static bool VehicleHitPre(BasicVehicle __instance, BasicCharacterController attacker)
	{
		try
		{
			if (!Heli.IsHeli(__instance)) return true;
			if (_active) return false;
			if (!_blastHook && RecentBlastNear(((Component)__instance).transform.position)) return false;
			HumanCharacterController hc = (attacker != null) ? attacker.TryCast<HumanCharacterController>() : null;
			if ((Object)(object)hc != (Object)null && Heli.InOrByHeli(hc))
			{
				if (_logged++ < 5) Report.Note("HELI damage to the helicopter from its own crew was blocked");
				return false;
			}
		}
		catch { }
		return true;
	}

	internal static void Mark(RecoillessRound rr)
	{
		try { if ((Object)(object)rr != (Object)null) _ours.Add(((Il2CppObjectBase)rr).Pointer); } catch { }
		if (_ours.Count > 64) _ours.Clear();
	}

	private static void DetonatePre(RecoillessRound __instance)
	{
		try
		{
			if ((Object)(object)__instance == (Object)null || !_ours.Contains(((Il2CppObjectBase)__instance).Pointer)) return;
			Vector3 p = ((Component)__instance).transform.position;
			_blasts.Add(new Vector3(p.x, p.y, Time.time));
			if (_blasts.Count > 16) _blasts.RemoveAt(0);
		}
		catch { }
	}

	private static void MoveNextPre(Il2CppSystem.Object __instance)
	{
		_active = false;
		try
		{
			BasicExplosion ex = ExplodeCo.Owner(__instance);
			if ((Object)(object)ex == (Object)null) return;
			try
			{
				HumanCharacterController who = ex.instigator?.TryCast<HumanCharacterController>();
				if ((Object)(object)who != (Object)null && Heli.InOrByHeli(who)) { _active = true; return; }
			}
			catch { }
			Vector2 p = ((Component)ex).transform.position;
			float now = Time.time;
			foreach (Vector3 b in _blasts)
			{
				if (now - b.z < 3f && ((Vector2)b - p).sqrMagnitude < 4f) { _active = true; break; }
			}
		}
		catch { }
	}

	private static void DirectPre(BasicGrenade __instance)
	{
		_active = false;
		try { if ((Object)(object)__instance != (Object)null && _ours.Contains(((Il2CppObjectBase)__instance).Pointer)) _active = true; } catch { }
	}

	private static void Post()
	{
		_active = false;
	}

	private static bool OnHitPre(HumanCharacterController __instance, ref bool __result)
	{
		bool ours = _active;
		if (!ours && !_blastHook) { try { ours = RecentBlastNear(((Component)__instance).transform.position); } catch { } }
		if (!ours || !Heli.InOrByHeli(__instance)) return true;
		if (_logged++ < 5) Report.Note("HELI a rocket hit on the helicopter crew was blocked");
		__result = false;
		return false;
	}
}
