using System;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace ZedHelicopter;

// The helicopter in the game's Build menu, on a "VEHICLES" tab (the same tab the Tank mod uses,
// so with both mods installed the tank and the helicopter sit side by side). It is a made-up
// terrain object entry that is never placed: pressing BUILD takes the materials you carry and
// the helicopter lands a few tiles in front of you.
// Also gives the game's 84mm HE Round (the rocket pods' ammo) a workbench recipe.
internal static class HeliBuild
{
	internal const int AttrId = 881310;
	internal const TechGenre Genre = (TechGenre)6;
	internal const string TabName = "VEHICLES";
	internal const string Name = "UH-1Y Helicopter";
	internal const string Desc = "A flyable helicopter with rocket pods and two door miniguns. Built from the materials you carry: it lands a few tiles in front of you. Runs on gasoline.";
	// Iron Plate, Metal Component, Copper Tube, Iron Wire, Copper Wire
	internal static readonly (int id, int n)[] Recipe = { (8, 80), (62, 30), (11, 15), (12, 25), (13, 20) };
	// 84mm HE Round x2 at a workbench: Gunpowder, Iron Plate, Copper Tube, Metal Component
	internal static readonly (int id, int n)[] RocketRecipe = { (7, 6), (8, 2), (11, 1), (62, 1) };
	internal const int RocketsPerCraft = 2;

	private static TerrainObjectAttr _attr;
	private static Sprite _icon;
	private static bool _logged;

	internal static bool On => HeliPlugin.BuildMenuTab.Value;

	internal static TerrainObjectAttr Attr()
	{
		if ((Object)(object)_attr != (Object)null) return _attr;
		try
		{
			var a = ScriptableObject.CreateInstance<TerrainObjectAttr>();
			((Object)a).name = "zh_helicopter";
			((Object)a).hideFlags = HideFlags.DontUnloadUnusedAsset;
			a.id = AttrId;
			a.englishName = Name;
			a.chineseName = Name;
			a.chineseDescription = Desc;
			a.techGenre = Genre;
			a.unlockByDefault = true;
			a.defaultHP = 4000f;
			a.sortingPriority = 1;
			var r = new RecipeData();
			r.itemId = AttrId;
			r.outputItemNumber = 1;
			r.craftPlatform = CraftPlatform.byhand;
			var items = new Il2CppSystem.Collections.Generic.List<RecipeItemData>();
			foreach (var (id, n) in Recipe) { var ri = new RecipeItemData(); ri.itemId = id; ri.itemNumber = n; items.Add(ri); }
			r.recipeItems = items;
			a.recipeData = r;
			_attr = a;
		}
		catch (Exception e) { HeliPlugin.L.LogWarning("Build menu: making the helicopter entry failed: " + e.Message); }
		return _attr;
	}

	internal static Sprite Icon()
	{
		if ((Object)(object)_icon != (Object)null) return _icon;
		try
		{
			using var st = typeof(HeliBuild).Assembly.GetManifestResourceStream("art.heli_icon.png");
			if (st == null) return null;
			var b = new byte[st.Length];
			int got = 0;
			while (got < b.Length) { int r = st.Read(b, got, b.Length - got); if (r <= 0) break; got += r; }
			var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
			((Object)tex).hideFlags = HideFlags.HideAndDontSave;
			ImageConversion.LoadImage(tex, (Il2CppStructArray<byte>)b);
			_icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
			((Object)_icon).hideFlags = HideFlags.HideAndDontSave;
		}
		catch (Exception e) { HeliPlugin.L.LogWarning("Build menu icon: " + e.Message); }
		return _icon;
	}

	internal static void Register()
	{
		TerrainObjectAttr a = Attr();
		if ((Object)(object)a == (Object)null) return;
		try
		{
			GameController gc = GameController.instance;
			if ((Object)(object)gc != (Object)null)
			{
				var dic = gc.terrainObjectAttrDic;
				if (dic != null && string.IsNullOrEmpty(a.spriteName))
				{
					foreach (var kv in dic)
					{
						TerrainObjectAttr o = kv.Value;
						if ((Object)(object)o == (Object)null || kv.Key == AttrId || string.IsNullOrEmpty(o.spriteName) || o.techGenre != TechGenre.Production) continue;
						a.spriteName = o.spriteName; a.spriteCount = o.spriteCount;
						break;
					}
				}
				if (dic != null && !dic.ContainsKey(AttrId)) dic.Add(AttrId, a);
				var gd = gc.terrainObjectAttrTechGenreDic;
				if (gd != null)
				{
					if (!gd.ContainsKey(Genre)) gd.Add(Genre, new Il2CppSystem.Collections.Generic.List<TerrainObjectAttr>());
					var l = gd[Genre];
					if (!l.Contains(a)) l.Add(a);
				}
			}
			var order = ConstructionPanel.GENRE_ORDER;
			bool has = false;
			if (order != null) foreach (TechGenre g in order) if (g == Genre) has = true;
			if (!has)
			{
				int n = order != null ? order.Length : 0;
				var arr = new Il2CppStructArray<TechGenre>(n + 1);
				for (int i = 0; i < n; i++) arr[i] = order[i];
				arr[n] = Genre;
				ConstructionPanel.GENRE_ORDER = arr;
			}
			if (!_logged) { _logged = true; HeliPlugin.L.LogInfo($"Build menu: {Name} added to the '{TabName}' tab"); }
		}
		catch (Exception e) { if (!_logged) { _logged = true; HeliPlugin.L.LogWarning("Build menu: " + e.Message); } }
	}

	private static bool _tickWarned;
	private static bool _rocketDone;
	private static float _rocketRetry;

	internal static void Tick()
	{
		RocketRecipeOnce();
		if (!On || ((Object)(object)_icon == (Object)null && (Object)(object)Icon() == (Object)null)) return;
		try
		{
			ConstructionPanel p = ConstructionPanel._instance;
			if ((Object)(object)p == (Object)null || !p.built || (Object)(object)p.windowRT == (Object)null || !((Component)p.windowRT).gameObject.activeInHierarchy) return;
			var cards = p.cards;
			if (cards != null)
			{
				for (int i = 0; i < cards.Count; i++)
				{
					var c = cards[i];
					if ((Object)(object)c == (Object)null || c.isVoxelCard || c.terrainObjectId != AttrId || (Object)(object)c.iconImage == (Object)null) continue;
					if ((Object)(object)c.iconImage.sprite == (Object)null || c.iconImage.sprite.Pointer != _icon.Pointer) { c.iconImage.sprite = _icon; c.iconImage.preserveAspect = true; }
				}
			}
			if (p.selectedId == AttrId && !p.voxelTabSelected && (Object)(object)p.detailIcon != (Object)null && ((Object)(object)p.detailIcon.sprite == (Object)null || p.detailIcon.sprite.Pointer != _icon.Pointer))
			{ p.detailIcon.sprite = _icon; p.detailIcon.preserveAspect = true; }
		}
		catch (Exception e) { if (!_tickWarned) { _tickWarned = true; HeliPlugin.L.LogWarning("Build menu icon: " + e.Message); } }
	}

	// The game's 84mm HE Round gets a workbench recipe (unless the game already has one).
	private static void RocketRecipeOnce()
	{
		if (_rocketDone || !HeliPlugin.RocketRecipe.Value) return;
		float now = Time.realtimeSinceStartup;
		if (now < _rocketRetry) return;
		_rocketRetry = now + 5f;
		ItemManager im = ItemManager.instance;
		if (im == null || im.itemList == null || im.itemList.Count == 0) return;
		if (!HeliRockets.Find() || HeliRockets.AmmoId <= 0) return;
		_rocketDone = true;
		try
		{
			ItemAttr a = im.FindItemAttrById(HeliRockets.AmmoId);
			if (a == null) { HeliPlugin.L.LogWarning("Rocket recipe: ammo item not found"); return; }
			var have = im.GetRecipeDatasById(HeliRockets.AmmoId);
			if (have != null && have.Length > 0)
			{
				HeliPlugin.L.LogInfo($"Rocket recipe: {HeliRockets.AmmoName} already has {have.Length} recipe(s) in the game, platform {have[0].craftPlatform}");
				return;
			}
			var r = new RecipeData();
			r.itemId = HeliRockets.AmmoId;
			r.outputItemNumber = RocketsPerCraft;
			r.craftPlatform = CraftPlatform.workbench;
			var items = new Il2CppSystem.Collections.Generic.List<RecipeItemData>();
			foreach (var (id, n) in RocketRecipe) { var ri = new RecipeItemData(); ri.itemId = id; ri.itemNumber = n; items.Add(ri); }
			r.recipeItems = items;
			var arr = new Il2CppReferenceArray<RecipeData>(1);
			arr[0] = r;
			a.recipeDataAry = arr;
			im.RegisterModRecipes(a);
			var check = im.GetRecipeDatasById(HeliRockets.AmmoId);
			HeliPlugin.L.LogInfo($"Rocket recipe: {HeliRockets.AmmoName} x{RocketsPerCraft} at the workbench (Gunpowder 6, Iron Plate 2, Copper Tube 1, Metal Component 1); recipes now {(check != null ? check.Length : 0)}");
		}
		catch (Exception e) { HeliPlugin.L.LogWarning("Rocket recipe: " + e.Message); }
	}

	internal static float Count(System.Collections.Generic.List<InventoryData> boxes, int id)
	{
		float n = 0f;
		foreach (InventoryData box in boxes)
		{
			try
			{
				var list = box.itemList;
				for (int i = 0; i < list.Count; i++)
				{
					ItemData it = list[i];
					if (it != null && it.itemId == id) n += Mathf.Max(1f, it.itemNumberFloat);
				}
			}
			catch { }
		}
		return n;
	}

	internal static bool Afford(HumanCharacterController pc, out string missing)
	{
		missing = null;
		var boxes = Heli.CarriedBoxes(pc);
		foreach (var (id, n) in Recipe)
		{
			float c = Count(boxes, id);
			if (c < n)
			{
				string nm = null;
				try { nm = TextManager.instance?.GetText(1000000 + id); } catch { }
				missing = $"{nm ?? ("item " + id)} {c:0}/{n}";
				return false;
			}
		}
		return true;
	}

	internal static void Build(ConstructionPanel panel)
	{
		HumanCharacterController pc = GameController.instance?.playerCharacter;
		if ((Object)(object)pc == (Object)null) return;
		if (!HeliRunner.Active) return;
		if (!Afford(pc, out string missing)) { Heli.Message("Not enough materials for the helicopter: " + missing); return; }
		string r = Heli.CallIn(pc, true);
		if (r == null || r.StartsWith("!")) return;
		var boxes = Heli.CarriedBoxes(pc);
		foreach (var (id, n) in Recipe)
		{
			float took = AutoIO.Take(boxes, id, n);
			if (took < n) HeliPlugin.L.LogWarning($"Build menu: only took {took}/{n} of item {id}");
		}
		try { panel.ClosePanel(); } catch { }
	}
}

[HarmonyPatch(typeof(ConstructionPanel), "EnsureBuilt")]
internal static class P_HeliBuildEnsure
{
	private static void Prefix() { if (HeliBuild.On) HeliBuild.Register(); }
}

[HarmonyPatch(typeof(ConstructionPanel), "OpenPanel")]
internal static class P_HeliBuildOpen
{
	private static void Prefix() { if (HeliBuild.On) HeliBuild.Register(); }
}

[HarmonyPatch(typeof(ConstructionPanel), "BuildGenreTab")]
internal static class P_HeliBuildTab
{
	private static void Postfix(ConstructionPanel __instance, TechGenre genre)
	{
		if (genre != HeliBuild.Genre || !HeliBuild.On) return;
		try
		{
			var tabs = __instance.tabs;
			if (tabs == null) return;
			for (int i = 0; i < tabs.Count; i++)
				if (tabs[i] != null && tabs[i].genre == HeliBuild.Genre && tabs[i].nameText != null) tabs[i].nameText.text = HeliBuild.TabName;
		}
		catch (Exception e) { HeliPlugin.L.LogWarning("Build menu tab: " + e.Message); }
	}
}

[HarmonyPatch(typeof(GameController), "GetAvailableTerrainObjectAttrsByTechGenre")]
internal static class P_HeliBuildList
{
	private static void Postfix(TechGenre m_techGenre, ref Il2CppSystem.Collections.Generic.List<TerrainObjectAttr> __result)
	{
		if (m_techGenre != HeliBuild.Genre || !HeliBuild.On) return;
		TerrainObjectAttr a = HeliBuild.Attr();
		if ((Object)(object)a == (Object)null) return;
		if (__result == null) __result = new Il2CppSystem.Collections.Generic.List<TerrainObjectAttr>();
		if (!__result.Contains(a)) __result.Add(a);
	}
}

[HarmonyPatch(typeof(GameController), "GetTerrainObjectAttrById")]
internal static class P_HeliBuildAttr
{
	private static void Postfix(int id, ref TerrainObjectAttr __result)
	{
		if (id == HeliBuild.AttrId && (Object)(object)__result == (Object)null) __result = HeliBuild.Attr();
	}
}

[HarmonyPatch(typeof(ConstructionItemCardUI), "Build")]
internal static class P_HeliBuildCard
{
	private static void Prefix(TerrainObjectAttr attr, ref Sprite iconSprite)
	{
		if ((Object)(object)attr != (Object)null && attr.id == HeliBuild.AttrId) iconSprite = HeliBuild.Icon() ?? iconSprite;
	}

	private static void Postfix(ConstructionItemCardUI __instance, TerrainObjectAttr attr)
	{
		if ((Object)(object)attr == (Object)null || attr.id != HeliBuild.AttrId) return;
		try
		{
			if ((Object)(object)__instance.nameText != (Object)null) __instance.nameText.text = HeliBuild.Name;
			if ((Object)(object)__instance.iconImage != (Object)null && (Object)(object)HeliBuild.Icon() != (Object)null) { __instance.iconImage.sprite = HeliBuild.Icon(); __instance.iconImage.preserveAspect = true; }
		}
		catch { }
	}
}

[HarmonyPatch(typeof(ConstructionPanel), "ShowTerrainObjectDetails")]
internal static class P_HeliBuildDetails
{
	private static void Postfix(ConstructionPanel __instance, int m_terrainObjectId)
	{
		if (m_terrainObjectId != HeliBuild.AttrId) return;
		try
		{
			if ((Object)(object)__instance.detailIcon != (Object)null && (Object)(object)HeliBuild.Icon() != (Object)null) { __instance.detailIcon.sprite = HeliBuild.Icon(); __instance.detailIcon.preserveAspect = true; }
			if ((Object)(object)__instance.detailNameText != (Object)null) __instance.detailNameText.text = HeliBuild.Name;
			if ((Object)(object)__instance.detailGenreText != (Object)null) __instance.detailGenreText.text = HeliBuild.TabName;
			if ((Object)(object)__instance.detailDescText != (Object)null) __instance.detailDescText.text = HeliBuild.Desc;
		}
		catch (Exception e) { HeliPlugin.L.LogWarning("Build menu details: " + e.Message); }
	}
}

[HarmonyPatch(typeof(ConstructionPanel), "BuildSelected")]
internal static class P_HeliBuildGo
{
	private static bool Prefix(ConstructionPanel __instance)
	{
		try
		{
			if (__instance.selectedId != HeliBuild.AttrId || __instance.voxelTabSelected) return true;
			HeliBuild.Build(__instance);
			return false;
		}
		catch (Exception e) { HeliPlugin.L.LogWarning("Build menu build: " + e.Message); return false; }
	}
}

[HarmonyPatch(typeof(ConstructionPanel), "OnCardClicked")]
internal static class P_HeliBuildCardClick
{
	private static bool Prefix(ConstructionPanel __instance, ConstructionItemCardUI card, int clickCount)
	{
		try
		{
			if ((Object)(object)card == (Object)null || card.isVoxelCard || card.terrainObjectId != HeliBuild.AttrId || clickCount < 2) return true;
			__instance.SelectItem(HeliBuild.AttrId, true);
			HeliBuild.Build(__instance);
			return false;
		}
		catch { return true; }
	}
}

[HarmonyPatch(typeof(TerrainObjectAttr), "name", MethodType.Getter)]
internal static class P_HeliBuildAttrName
{
	private static void Postfix(TerrainObjectAttr __instance, ref string __result)
	{
		try { if (__instance.id == HeliBuild.AttrId) __result = HeliBuild.Name; } catch { }
	}
}

[HarmonyPatch(typeof(TerrainObjectAttr), "description", MethodType.Getter)]
internal static class P_HeliBuildAttrDesc
{
	private static void Postfix(TerrainObjectAttr __instance, ref string __result)
	{
		try { if (__instance.id == HeliBuild.AttrId) __result = HeliBuild.Desc; } catch { }
	}
}

[HarmonyPatch(typeof(TerrainObjectAttr), "canBeBuilt", MethodType.Getter)]
internal static class P_HeliBuildAttrCan
{
	private static void Postfix(TerrainObjectAttr __instance, ref bool __result)
	{
		try { if (__instance.id == HeliBuild.AttrId) __result = true; } catch { }
	}
}
