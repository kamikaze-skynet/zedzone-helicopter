using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace ZedHelicopter;

// Gasoline for the standalone helicopter. It burns the game's vehicle fuel
// (VehicleFuelTransfer.fuelLiquidItemId, gasoline), so a fuel can that fills a car fills it too.
internal static partial class Heli
{
	internal static int FuelLiquidId
	{
		get
		{
			try
			{
				return VehicleFuelTransfer.fuelLiquidItemId;
			}
			catch
			{
				return -1;
			}
		}
	}

	// Shift + call key next to a landed helicopter: pour every fuel can you carry into it.
	internal static string Refuel(HumanCharacterController pc)
	{
		BasicVehicle v = null;
		try
		{
			v = pc.drivingVehicle;
		}
		catch
		{
		}
		if (!IsHeli(v))
		{
			v = NearestHeli(((Vector2)(((Component)pc).transform.position)), 5f);
		}
		if ((Object)(object)v == (Object)null || Airborne(v))
		{
			return Fail(L.T("附近沒有停著的直升機"));
		}
		float cap = Cap(v.vehicleData);
		float before = Fuel(v);
		if (cap - before < 0.05f)
		{
			return Fail(L.T("油箱已經滿了"));
		}
		VehiclePartData tank = null;
		try
		{
			tank = v.fuelTankData ?? FuelPart(v.vehicleData);
		}
		catch
		{
		}
		if (tank == null)
		{
			Report.Note("HELI refuel: no fuel tank part");
			return Fail(L.T("背包裡沒有裝汽油的油桶"));
		}
		float added = 0f;
		foreach (InventoryData inv in Carried(pc))
		{
			var items = inv.itemList;
			if (items == null)
			{
				continue;
			}
			for (int i = 0; i < items.Count && Fuel(v) < cap - 0.05f; i++)
			{
				ItemData it = items[i];
				if (it == null)
				{
					continue;
				}
				try
				{
					if (!VehicleFuelTransfer.IsFuelContainer(it))
					{
						continue;
					}
					float got = VehicleFuelTransfer.AddFuelFromContainer(v.vehicleData, tank, it);
					if (got > 0f)
					{
						added += got;
						try
						{
							inv.RefreshTotalItemWeight();
						}
						catch
						{
						}
					}
				}
				catch (Exception ex)
				{
					Report.Note("HELI refuel: " + ex.Message);
				}
			}
		}
		float after = Fuel(v);
		Report.Note($"HELI refuel: game said {added:0.##}, tank {before:0.##} -> {after:0.##} / {cap:0}");
		if (after - before < 0.01f)
		{
			return Fail(L.T("背包裡沒有裝汽油的油桶"));
		}
		return Ok(L.F("加了 {0:0.#} L 汽油（{1:0.#} / {2:0} L）", after - before, after, cap));
	}

	private static BasicVehicle NearestHeli(Vector2 pos, float r)
	{
		BasicVehicle best = null;
		float bestD = r * r;
		try
		{
			var list = BasicVehicle.s_liveVehicles;
			if (list == null)
			{
				return null;
			}
			for (int i = 0; i < list.Count; i++)
			{
				BasicVehicle v = list[i];
				if (!IsHeli(v))
				{
					continue;
				}
				float d = (((Vector2)(((Component)v).transform.position)) - pos).sqrMagnitude;
				if (d <= bestD)
				{
					bestD = d;
					best = v;
				}
			}
		}
		catch
		{
		}
		return best;
	}

	// For Spawn Menu's kit: full cans of gasoline. Picks the container the game itself
	// spawns with gasoline in it (largest one), then makes sure it is full.
	internal static string GiveFuelCans(int count)
	{
		GameController gc = GameController.instance;
		ItemManager im = ItemManager.instance;
		int fuelId = FuelLiquidId;
		if (gc == null || im == null || fuelId <= 0)
		{
			return "!No fuel: not in a game";
		}
		ItemAttr_Container best = null;
		try
		{
			var e = im.itemAttrDic.GetEnumerator();
			while (e.MoveNext())
			{
				ItemAttr a = e.Current.Value;
				if (a == null)
				{
					continue;
				}
				ItemAttr_Container c = a.TryCast<ItemAttr_Container>();
				if (c == null || c.defaultLiquidIdAry == null)
				{
					continue;
				}
				bool gas = false;
				for (int i = 0; i < c.defaultLiquidIdAry.Length; i++)
				{
					if (c.defaultLiquidIdAry[i] == fuelId)
					{
						gas = true;
						break;
					}
				}
				if (gas && (best == null || c.totalCapacity > best.totalCapacity))
				{
					best = c;
				}
			}
		}
		catch (Exception ex)
		{
			Report.Note("HELI fuel can search: " + ex.Message);
		}
		if (best == null)
		{
			return "!No fuel: couldn't find a gasoline can item";
		}
		ItemAttr liquidAttr = null;
		try
		{
			liquidAttr = im.FindItemAttrById(fuelId);
		}
		catch
		{
		}
		int given = 0;
		string canName = "?";
		try
		{
			canName = best.ItemName_EN;
		}
		catch
		{
		}
		for (int n = 0; n < count; n++)
		{
			try
			{
				ItemData can = im.GetRandomDataByAttr(best, false, false);
				if (can == null)
				{
					break;
				}
				can.durability = InventoryData.MAX_ITEM_DURABILITY;
				Fill(can, best, liquidAttr, fuelId, im);
				gc.AddItemToPlayer(can, true);
				given++;
			}
			catch (Exception ex2)
			{
				Report.Note("HELI fuel can: " + ex2.Message);
				break;
			}
		}
		Report.Note($"HELI gave {given} x {canName} (item {best.itemId}, capacity {best.totalCapacity}) of liquid {fuelId}");
		return given > 0 ? $"{given} full {canName}" : "!No fuel: couldn't make a fuel can";
	}

	private static void Fill(ItemData can, ItemAttr_Container attr, ItemAttr liquidAttr, int fuelId, ItemManager im)
	{
		InventoryData inv = can.inventoryData;
		if (inv == null)
		{
			Report.Note("HELI fuel can has no inventory");
			return;
		}
		float cap = attr.totalCapacity;
		var items = inv.itemList;
		ItemData gas = null;
		for (int i = items.Count - 1; i >= 0; i--)
		{
			ItemData it = items[i];
			if (it == null)
			{
				continue;
			}
			if (it.itemId == fuelId && gas == null)
			{
				gas = it;
			}
			else
			{
				try
				{
					inv.RemoveItem(it, true);
				}
				catch
				{
				}
			}
		}
		if (gas == null && liquidAttr != null)
		{
			gas = im.GetRandomDataByAttr(liquidAttr, false, false);
			if (gas != null)
			{
				gas.itemNumberFloat = cap;
				inv.AddItem(gas, true);
			}
		}
		else if (gas != null)
		{
			gas.itemNumberFloat = cap;
		}
		try
		{
			inv.RefreshTotalItemWeight();
		}
		catch
		{
		}
	}
}
