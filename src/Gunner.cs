using System;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using ZedZone.Net;

namespace ZedHelicopter;

// Door miniguns. Each player riding as a passenger mans one door gun:
// the first passenger gets the left gun, the second the right gun.
// They aim with the mouse inside their door's arc and fire with the left mouse button.
// The pilot fires the rocket pods with the left mouse button, and the door gun on the mouse side
// with the right mouse button while no other player is aboard.
internal static partial class Heli
{
	private static Sprite _gun;

	// Pintle in the art: x +0.10 m, y +/-1.52 m from the mast; 47.04 px per metre, 166 px per unit.
	private const float UnitsPerMetre = 47.04f / 166f;

	private static readonly Vector2 GunPivot = new Vector2(0.5f / 2.4f, 0.5f);

	private static readonly Vector2[] GunMount = new Vector2[2]
	{
		new Vector2(0.10f, 1.52f) * UnitsPerMetre,
		new Vector2(0.10f, -1.52f) * UnitsPerMetre
	};

	private const float ArcMin = 12f;

	private const float ArcMax = 168f;

	private const float GunRange = 18f;

	// Side the local player is manning this frame (-1 = none).
	private static int _gunSide = -1;

	public static bool Gunning => _gunSide >= 0;

	private static Vector2 Rot(Vector2 p, float deg)
	{
		float r = deg * ((float)Math.PI / 180f);
		float c = Mathf.Cos(r), s = Mathf.Sin(r);
		return new Vector2(p.x * c - p.y * s, p.x * s + p.y * c);
	}

	private static void PlaceGuns(Vis vis, float heading)
	{
		for (int i = 0; i < 2; i++)
		{
			SpriteRenderer g = vis.Gun[i];
			if ((Object)(object)g == (Object)null)
			{
				continue;
			}
			Vector2 p = Rot(GunMount[i], heading);
			((Component)g).transform.localPosition = new Vector3(p.x, p.y, 0f);
			((Component)g).transform.localRotation = Quaternion.Euler(0f, 0f, heading + vis.GunRel[i]);
		}
	}

	private static float ClampArc(int side, float rel)
	{
		rel = Mathf.DeltaAngle(0f, rel);
		if (side == 0)
		{
			return Mathf.Clamp(rel, ArcMin, ArcMax);
		}
		return Mathf.Clamp(rel, -ArcMax, -ArcMin);
	}

	private static float Heading(BasicVehicle v, out float alt)
	{
		alt = 0f;
		if (_air.TryGetValue(((Il2CppObjectBase)v).Pointer, out var a))
		{
			alt = a.Alt;
			return a.Heading;
		}
		return VehAng(v) + _fwd;
	}

	// World position of a door gun on the ground plane (shots and hits are worked out there).
	private static Vector2 GunGround(BasicVehicle v, int side, float heading)
	{
		return VehPos(v) + Rot(GunMount[side], heading) * Scale;
	}

	private static bool MouseWorld(out Vector2 m)
	{
		m = Vector2.zero;
		Camera main = Camera.main;
		if ((Object)(object)main == (Object)null)
		{
			return false;
		}
		Vector3 w = main.ScreenToWorldPoint(Input.mousePosition);
		m = new Vector2(w.x, w.y);
		return true;
	}

	// Seat order from the game's own vehicle seat sync: index in the passenger list, -1 if unknown.
	private static int SeatIndex(BasicVehicle v, out int playerPassengers)
	{
		playerPassengers = -1;
		try
		{
			if (!NetworkManager.isHost && !NetworkManager.isClient)
			{
				return -1;
			}
			NetVehicleSync ns = NetVehicleSync.instance;
			NetworkManager nm = NetworkManager.instance;
			if ((Object)(object)ns == (Object)null || (Object)(object)nm == (Object)null)
			{
				return -1;
			}
			int id;
			if (NetworkManager.isHost)
			{
				if (v.vehicleData == null || !ns.m_idByData.TryGetValue(v.vehicleData, out id))
				{
					return -1;
				}
			}
			else if (!ns.ClientTryGetNetId(v, out id))
			{
				return -1;
			}
			NetVehicleSync.SeatState seats = ns.GetSeats(id);
			var list = seats?.passengers;
			if (list == null)
			{
				return -1;
			}
			playerPassengers = list.Count;
			return list.IndexOf(nm.localPlayerId);
		}
		catch
		{
			return -1;
		}
	}

	private static int SideFromMouse(BasicVehicle v, float heading, Vector2 mouse)
	{
		Vector2 d = mouse - VehPos(v);
		float rel = Mathf.DeltaAngle(heading, Mathf.Atan2(d.y, d.x) * 57.29578f);
		return (rel >= 0f) ? 0 : 1;
	}

	// Turns the gun towards the mouse, returns where the shot would go.
	private static Vector2 Aim(BasicVehicle v, int side, float heading, Vector2 mouse, out Vector2 gunPos)
	{
		gunPos = GunGround(v, side, heading);
		Vector2 d = mouse - gunPos;
		float rel = ClampArc(side, Mathf.DeltaAngle(heading, Mathf.Atan2(d.y, d.x) * 57.29578f));
		if (_vis.TryGetValue(((Il2CppObjectBase)v).Pointer, out var vis))
		{
			vis.GunRel[side] = rel;
		}
		float ang = (heading + rel) * ((float)Math.PI / 180f);
		float dist = Mathf.Clamp(d.magnitude, 1f, GunRange);
		return gunPos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
	}

	private static void Gunner(HumanCharacterController pc, BasicVehicle v, float now)
	{
		if (!HeliPlugin.DoorGuns.Value || !MouseWorld(out var mouse))
		{
			return;
		}
		float alt;
		float heading = Heading(v, out alt);
		int seat = SeatIndex(v, out _);
		int side = (seat >= 0) ? (seat % 2) : SideFromMouse(v, heading, mouse);
		_gunSide = side;
		Vector2 gunPos;
		Vector2 target = Aim(v, side, heading, mouse, out gunPos);
		NearStatus = L.F("機槍手（{0}）：滑鼠瞄準，按住左鍵射擊", (side == 0) ? L.T("左門") : L.T("右門"));
		if (Input.GetMouseButton(0) && now >= _fireAt)
		{
			Fire(pc, v, heading, alt, gunPos, target, now);
		}
	}

	// Pilot: left mouse fires the rocket pods towards the mouse; right mouse fires the door
	// gun on the mouse side, but only while no other player is aboard to man the guns.
	private static void PilotGun(HumanCharacterController pc, BasicVehicle v, Air a, Vector2 pos, float now)
	{
		if (!MouseWorld(out var mouse))
		{
			return;
		}
		if (HeliPlugin.Rockets.Value && Input.GetMouseButton(0) && now >= _rocketAt)
		{
			FireRocket(pc, v, a, mouse, now);
		}
		if (!HeliPlugin.DoorGuns.Value)
		{
			return;
		}
		SeatIndex(v, out int players);
		if (players > 0)
		{
			return;   // gunners aboard: the guns are theirs
		}
		int side = SideFromMouse(v, a.Heading, mouse);
		Vector2 gunPos;
		Vector2 target = Aim(v, side, a.Heading, mouse, out gunPos);
		if (Input.GetMouseButton(1) && now >= _fireAt)
		{
			Fire(pc, v, a.Heading, a.Alt, gunPos, target, now);
		}
	}

	private static float _rocketAt;

	private static int _pod;

	// Front of the rocket pods: x +0.05 m, y +/-1.60 m from the mast.
	private static readonly Vector2[] PodMount = new Vector2[2]
	{
		new Vector2(0.05f, 1.60f) * UnitsPerMetre,
		new Vector2(0.05f, -1.60f) * UnitsPerMetre
	};

	private static void FireRocket(HumanCharacterController pc, BasicVehicle v, Air a, Vector2 mouse, float now)
	{
		_rocketAt = now + Mathf.Max(0.1f, HeliPlugin.RocketInterval.Value);
		if (!HeliRockets.Find())
		{
			Msg(L.T("找不到火箭彈"));
			_rocketAt = now + 3f;
			return;
		}
		if (HeliPlugin.RocketsNeedAmmo.Value && HeliRockets.AmmoId > 0)
		{
			var boxes = new System.Collections.Generic.List<InventoryData>();
			try
			{
				if (v.vehicleData?.inventoryData != null)
				{
					boxes.Add(v.vehicleData.inventoryData);
				}
			}
			catch
			{
			}
			boxes.AddRange(Carried(pc));
			if (AutoIO.Take(boxes, HeliRockets.AmmoId, 1f) < 1f)
			{
				Msg(L.F("沒有火箭彈：貨艙或背包裡要有 {0}", HeliRockets.AmmoName));
				_rocketAt = now + 1f;
				return;
			}
		}
		_pod ^= 1;
		Vector2 muzzle = VehPos(v) + Rot(PodMount[_pod], a.Heading) * Scale;
		Vector2 d = mouse - muzzle;
		if (d.magnitude < 3f)
		{
			// Mouse right on the helicopter: fire straight ahead.
			float h = a.Heading * ((float)Math.PI / 180f);
			d = new Vector2(Mathf.Cos(h), Mathf.Sin(h)) * 3f;
		}
		// Start the rocket clear of the helicopter's body, in the direction it flies.
		Vector2 dir = d.normalized;
		muzzle += dir * 2.5f;
		Vector2 target = muzzle + dir * Mathf.Max(1f, d.magnitude - 2.5f);
		if (HeliRockets.Launch(pc, muzzle, target))
		{
			HeliAudio.Rocket(muzzle);
			HeliNet.Rocket(muzzle, target);
			try
			{
				GameController.instance.PlayerNoise(target, HeliPlugin.HeliNoise.Value, false, true, ((BasicCharacterController)pc).characterData.zLevel, (BasicCharacterController)null);
			}
			catch
			{
			}
		}
	}

	// Anyone flying or riding in a helicopter, or standing right on one.
	internal static bool InOrByHeli(HumanCharacterController h)
	{
		try
		{
			if ((Object)(object)h.drivingVehicle != (Object)null && IsHeli(h.drivingVehicle))
			{
				return true;
			}
			Vector2 p = ((Component)h).transform.position;
			var live = BasicVehicle.s_liveVehicles;
			if (live == null)
			{
				return false;
			}
			for (int i = 0; i < live.Count; i++)
			{
				BasicVehicle v = live[i];
				if ((Object)(object)v != (Object)null && IsHeli(v) && (VehPos(v) - p).sqrMagnitude < 9f)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static BasicVehicle NetPassengerHeli()
	{
		try
		{
			if (!NetworkManager.isHost && !NetworkManager.isClient)
			{
				return null;
			}
			var live = BasicVehicle.s_liveVehicles;
			if (live == null)
			{
				return null;
			}
			for (int i = 0; i < live.Count; i++)
			{
				BasicVehicle v = live[i];
				if ((Object)(object)v != (Object)null && IsHeli(v) && SeatIndex(v, out _) >= 0)
				{
					if (_seatLog++ < 3)
					{
						Report.Note("HELI door gunner found through the seat list (drivingVehicle was empty)");
					}
					return v;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static int _seatLog;
}
