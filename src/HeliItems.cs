using System;
using System.IO;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace ZedHelicopter;

// The 84mm Rocket Case is a game item mod (.zedmod) embedded in Helicopter.dll. At start-up it's
// written to BepInEx\cache\Helicopter and handed to the game's own mod loader, so every install
// route (local Mods folder or Workshop) gets the item with nothing else to copy.
internal static class HeliItems
{
	internal const string CaseGuid = "e6350b16-245b-4a66-84d3-b7fc36e98fbd";

	private static bool _done;

	private static float _next;

	internal static void Tick()
	{
		if (_done) return;
		float now = Time.realtimeSinceStartup;
		if (now < _next) return;
		_next = now + 2f;
		try
		{
			ItemManager im = ItemManager.instance;
			if (im == null || im.itemList == null || im.itemList.Count == 0) return;
			var handlers = ModLoader.handlers;
			if (handlers == null || handlers.Count == 0) return;
			_done = true;
			Load(handlers);
		}
		catch (Exception e)
		{
			_done = true;
			HeliPlugin.L.LogWarning("Rocket case item: " + e.Message);
		}
	}

	private static void Load(Il2CppSystem.Collections.Generic.Dictionary<string, IModHandler> handlers)
	{
		var loaded = ModLoader.loadedMods;
		if (loaded != null && loaded.ContainsKey(CaseGuid))
		{
			HeliPlugin.L.LogInfo($"Rocket case item already loaded, id {CaseId()}");
			return;
		}
		IModHandler handler = null;
		foreach (var kv in handlers)
		{
			var h = kv.Value;
			if (h == null) continue;
			string hs = null, ht = null;
			try { hs = h.SubFolder; ht = h.ModType; } catch { }
			if (string.Equals(hs, "Ammo", StringComparison.OrdinalIgnoreCase) || string.Equals(ht, "Ammo", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(kv.Key, "Ammo", StringComparison.OrdinalIgnoreCase)) { handler = h; break; }
		}
		if (handler == null)
		{
			HeliPlugin.L.LogWarning("Rocket case item: the game has no item handler for Ammo");
			return;
		}
		string dir = Path.Combine(Paths.CachePath, "Helicopter");
		Directory.CreateDirectory(dir);
		string file = Path.Combine(dir, CaseGuid + ".zedmod");
		using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("items." + CaseGuid + ".zedmod"))
		{
			if (s == null) { HeliPlugin.L.LogWarning("Rocket case item: not embedded in the DLL"); return; }
			using FileStream f = File.Create(file);
			s.CopyTo(f);
		}
		bool ok = ModLoader.TryLoadZedmod(file, handler);
		if (ok) { try { ModLocaleManager.ReapplyAllLocales(); } catch { } }
		HeliPlugin.L.LogInfo(ok ? $"Rocket case item loaded, id {CaseId()}" : "Rocket case item: the game refused it");
	}

	/// <summary>Runtime item id of the 84mm Rocket Case, or -1 if it isn't loaded.</summary>
	internal static int CaseId()
	{
		try
		{
			var mods = ModLoader.loadedMods;
			if (mods != null && mods.ContainsKey(CaseGuid))
			{
				var info = mods[CaseGuid];
				if (info != null && info.resolvedRuntimeId > 0) return info.resolvedRuntimeId;
			}
		}
		catch { }
		return -1;
	}
}
