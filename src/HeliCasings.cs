using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ZedHelicopter;

// Spent casings come back: every door gun round gives back its brass (5.56x45 Shell or
// 7.62x39 Shell, the game's own items). If nothing in the game (or the Tank mod) reloads those
// rounds from their casings yet, a reloader recipe is added: 10 casings + powder + copper = 10 rounds.
internal static class HeliCasings
{
	// round -> its casing (game items)
	internal static readonly Dictionary<int, int> Brass = new Dictionary<int, int> { { 102, 1002 }, { 104, 1004 } };

	private const int Gunpowder = 7;

	private const int CopperPlate = 9;

	private static bool _done;

	private static float _retry;

	private static bool _warned;

	internal static void Check()
	{
		if (_done || !HeliPlugin.ReloadRecipes.Value)
		{
			return;
		}
		float now = Time.realtimeSinceStartup;
		if (now < _retry)
		{
			return;
		}
		_retry = now + 5f;
		ItemManager im = ItemManager.instance;
		if (im == null || im.itemList == null || im.itemList.Count == 0)
		{
			return;
		}
		// give the Tank mod (which adds the same recipes) its turn first
		if (now < 20f)
		{
			return;
		}
		_done = true;
		var sb = new StringBuilder("Casings:");
		foreach (var kv in Brass)
		{
			AddReload(im, kv.Key, kv.Value, sb);
		}
		HeliPlugin.L.LogInfo(sb.ToString());
	}

	private static bool _rocketDone;

	private static float _rocketRetry;

	// Reloader: 1 Rocket Case + 4 Gunpowder + 1 Iron Plate + 1 Metal Component = 1 84mm HE Round.
	internal static void CheckRocket()
	{
		if (_rocketDone || !HeliPlugin.ReloadRecipes.Value)
		{
			return;
		}
		float now = Time.realtimeSinceStartup;
		if (now < _rocketRetry)
		{
			return;
		}
		_rocketRetry = now + 5f;
		ItemManager im = ItemManager.instance;
		if (im == null || im.itemList == null || im.itemList.Count == 0)
		{
			return;
		}
		int caseId = HeliItems.CaseId();
		if (caseId <= 0)
		{
			if (now > 120f)
			{
				_rocketDone = true;
				HeliPlugin.L.LogWarning("Rocket case item never loaded: no rocket reload recipe");
			}
			return;
		}
		int round = HeliRockets.AmmoId;
		if (round <= 0)
		{
			HeliRockets.Find();
			round = HeliRockets.AmmoId;
			if (round <= 0)
			{
				return;
			}
		}
		_rocketDone = true;
		var sb = new StringBuilder("Rocket case:");
		AddRecipe(im, round, caseId, 1, new (int, int)[4] { (caseId, 1), (Gunpowder, 4), (IronPlate, 1), (MetalComponent, 1) }, sb);
		HeliPlugin.L.LogInfo(sb.ToString());
	}

	private const int IronPlate = 8;

	private const int MetalComponent = 62;

	private static void AddReload(ItemManager im, int round, int brass, StringBuilder sb)
	{
		AddRecipe(im, round, brass, 10, new (int, int)[3] { (brass, 10), (Gunpowder, 1), (CopperPlate, 1) }, sb);
	}

	private static void AddRecipe(ItemManager im, int round, int brass, int output, (int, int)[] parts, StringBuilder sb)
	{
		try
		{
			ItemAttr a = im.FindItemAttrById(round);
			if (a == null || im.FindItemAttrById(brass) == null)
			{
				sb.Append($" {round}: item missing;");
				return;
			}
			var have = im.GetRecipeDatasById(round);
			int n = (have != null) ? have.Length : 0;
			if (have != null)
			{
				foreach (RecipeData rd in have)
				{
					var items = rd?.recipeItems;
					if (items == null)
					{
						continue;
					}
					for (int i = 0; i < items.Count; i++)
					{
						if (items[i] != null && items[i].itemId == brass)
						{
							sb.Append($" {Name(a)} already reloads from its casings ({rd.craftPlatform});");
							return;
						}
					}
				}
			}
			var r = new RecipeData();
			r.itemId = round;
			r.outputItemNumber = output;
			r.craftPlatform = CraftPlatform.reloader;
			var list = new Il2CppSystem.Collections.Generic.List<RecipeItemData>();
			foreach (var (id, num) in parts)
			{
				var ri = new RecipeItemData();
				ri.itemId = id;
				ri.itemNumber = num;
				list.Add(ri);
			}
			r.recipeItems = list;
			var old = a.recipeDataAry;
			var only = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<RecipeData>(1);
			only[0] = r;
			a.recipeDataAry = only;
			im.RegisterModRecipes(a);
			int ol = (old != null) ? old.Length : 0;
			var all = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<RecipeData>(ol + 1);
			for (int i = 0; i < ol; i++)
			{
				all[i] = old[i];
			}
			all[ol] = r;
			a.recipeDataAry = all;
			var check = im.GetRecipeDatasById(round);
			sb.Append($" {Name(a)}: reloader recipe added ({n} -> {((check != null) ? check.Length : 0)});");
		}
		catch (Exception e)
		{
			sb.Append($" {round}: {e.Message};");
		}
	}

	private static string Name(ItemAttr a)
	{
		try
		{
			return a.ItemName_EN ?? a.itemId.ToString();
		}
		catch
		{
			return a.itemId.ToString();
		}
	}

	// One casing back to the shooter: onto a stack they carry, else onto one in the helicopter's
	// cargo, else as a new item (the game drops it at their feet if the bag is full).
	internal static void Give(HumanCharacterController pc, BasicVehicle v, int round)
	{
		if (!HeliPlugin.ReturnCasings.Value || !Brass.TryGetValue(round, out int id))
		{
			return;
		}
		GiveId(pc, v, id);
	}

	// One 84mm Rocket Case back for every rocket fired.
	internal static void GiveRocketCase(HumanCharacterController pc, BasicVehicle v)
	{
		if (!HeliPlugin.ReturnRocketCases.Value)
		{
			return;
		}
		int id = HeliItems.CaseId();
		if (id > 0)
		{
			GiveId(pc, v, id);
		}
	}

	private static void GiveId(HumanCharacterController pc, BasicVehicle v, int id)
	{
		try
		{
			ItemManager im = ItemManager.instance;
			ItemAttr a = im?.FindItemAttrById(id);
			if (a == null)
			{
				return;
			}
			float max = Mathf.Max(1, a.stackNumber);
			var boxes = Heli.CarriedBoxes(pc);
			try
			{
				if (v?.vehicleData?.inventoryData != null)
				{
					boxes.Add(v.vehicleData.inventoryData);
				}
			}
			catch
			{
			}
			foreach (InventoryData box in boxes)
			{
				var l = box.itemList;
				if (l == null)
				{
					continue;
				}
				for (int i = 0; i < l.Count; i++)
				{
					ItemData it = l[i];
					if (it == null || it.itemId != id)
					{
						continue;
					}
					float have = Mathf.Max(1f, it.itemNumberFloat);
					if (have + 1f > max + 0.001f)
					{
						continue;
					}
					it.itemNumberFloat = have + 1f;
					try
					{
						box.RefreshTotalItemWeight();
					}
					catch
					{
					}
					return;
				}
			}
			ItemData d = im.GetRandomDataByAttr(a, false, false);
			if (d == null)
			{
				return;
			}
			d.itemNumberFloat = 1f;
			d.durability = InventoryData.MAX_ITEM_DURABILITY;
			GameController.instance.AddItemToPlayer(d, true);
		}
		catch (Exception e)
		{
			if (!_warned)
			{
				_warned = true;
				HeliPlugin.L.LogWarning("casings: " + e.Message);
			}
		}
	}
}
