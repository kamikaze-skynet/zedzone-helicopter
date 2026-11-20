using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using HarmonyLib;
using LiteNetLib;
using UnityEngine;
using ZedZone.Net;

namespace ZedHelicopter;

// Co-op: shows each player's door gun and rocket shots to the other players.
// The hits themselves already go through the game (a guest's OnHit is sent to the host),
// but the tracers, muzzle flashes and gun sounds were only drawn on the shooter's screen.
//
// Shots ride on the game's co-op chat (MsgChat), marked with a prefix and hidden from the
// chat box. A guest sends its shots to the host; the host plays them and passes them on to the
// other players. Shots are bundled about 8 times a second.
//   @@HL1|<sender>|G,x,y,tx,ty,hit,heading,alt;R,x,y,tx,ty;...
internal static class HeliNet
{
	internal const string Mark = "@@HL1|";

	private static readonly string _me = UnityEngine.Random.Range(100000, 999999).ToString(CultureInfo.InvariantCulture);

	private static readonly StringBuilder _out = new StringBuilder();

	private static int _queued;

	private static float _sendAt;

	private static int _errLog;

	internal static bool Is(string s) => s != null && s.StartsWith(Mark, StringComparison.Ordinal);

	private static bool Online
	{
		get { try { return NetworkManager.isOnline; } catch { return false; } }
	}

	private static bool IsGuest
	{
		get { try { return NetworkManager.isOnline && NetworkManager.isClient; } catch { return false; } }
	}

	private static string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);

	private static float P(string s) => float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

	internal static void Gun(Vector2 muzzle, Vector2 target, bool hit, float heading, float alt)
	{
		if (!Online || !HeliPlugin.ShareShots.Value) return;
		Add($"G,{F(muzzle.x)},{F(muzzle.y)},{F(target.x)},{F(target.y)},{(hit ? 1 : 0)},{F(heading)},{F(alt)}");
	}

	internal static void Rocket(Vector2 muzzle, Vector2 target)
	{
		if (!Online || !HeliPlugin.ShareShots.Value) return;
		Add($"R,{F(muzzle.x)},{F(muzzle.y)},{F(target.x)},{F(target.y)}");
	}

	private static void Add(string shot)
	{
		if (_queued >= 6) return;  // keep a message small (8 shots a second at most anyway)
		if (_queued > 0) _out.Append(';');
		_out.Append(shot);
		_queued++;
	}

	// Called every frame: sends what's queued about 8 times a second.
	internal static void Tick()
	{
		if (_queued == 0) return;
		float now = Time.realtimeSinceStartup;
		if (now < _sendAt) return;
		_sendAt = now + 0.12f;
		string body = Mark + _me + "|" + _out.ToString();
		_out.Clear();
		_queued = 0;
		if (!Online) return;
		try
		{
			if (IsGuest)
			{
				NetworkManager.instance?.SendToHost(new MsgChat { senderId = 0, senderName = "", text = body }, DeliveryMethod.ReliableOrdered, 0);
			}
			else
			{
				NetworkManager.instance?.BroadcastToJoined(new MsgChat { senderId = 0, senderName = "", text = body }, null, DeliveryMethod.ReliableOrdered, 0);
			}
		}
		catch (Exception e)
		{
			if (_errLog++ < 5) HeliPlugin.L.LogWarning("Sharing shots failed: " + e.Message);
		}
	}

	// Host: a guest's shots. Show them here and pass them on to the other players.
	internal static void OnHostReceive(string text, NetPeer from)
	{
		Play(text);
		try
		{
			NetworkManager.instance?.BroadcastToJoined(new MsgChat { senderId = 0, senderName = "", text = text }, from, DeliveryMethod.ReliableOrdered, 0);
		}
		catch (Exception e)
		{
			if (_errLog++ < 5) HeliPlugin.L.LogWarning("Passing shots on failed: " + e.Message);
		}
	}

	internal static void OnClientReceive(string text) => Play(text);

	private static void Play(string text)
	{
		try
		{
			string[] head = text.Substring(Mark.Length).Split(new[] { '|' }, 2);
			if (head.Length < 2 || head[0] == _me) return;  // our own shots, sent back to us
			if (!HeliPlugin.ShareShots.Value) return;
			foreach (string s in head[1].Split(';'))
			{
				string[] a = s.Split(',');
				if (a[0] == "G" && a.Length >= 8)
				{
					Heli.RemoteGun(new Vector2(P(a[1]), P(a[2])), new Vector2(P(a[3]), P(a[4])), a[5] == "1", P(a[6]), P(a[7]));
				}
				else if (a[0] == "R" && a.Length >= 5)
				{
					Heli.RemoteRocket(new Vector2(P(a[1]), P(a[2])), new Vector2(P(a[3]), P(a[4])));
				}
			}
		}
		catch (Exception e)
		{
			if (_errLog++ < 5) HeliPlugin.L.LogWarning("Bad shot message: " + e.Message);
		}
	}
}

// Never show our lines in the chat box or as speech bubbles, and handle them ourselves.
[HarmonyPatch(typeof(NetChatSync), nameof(NetChatSync.Append))]
internal static class P_HeliNetAppend
{
	private static bool Prefix(string text) => !HeliNet.Is(text);
}

[HarmonyPatch(typeof(NetChatSync), nameof(NetChatSync.OnHostReceive))]
internal static class P_HeliNetHost
{
	private static bool Prefix(MsgChat msg, NetPeer peer)
	{
		string text = msg?.text;
		if (!HeliNet.Is(text)) return true;
		HeliNet.OnHostReceive(text, peer);
		return false;
	}
}

[HarmonyPatch(typeof(NetChatSync), nameof(NetChatSync.OnClientReceive))]
internal static class P_HeliNetClient
{
	private static bool Prefix(MsgChat msg)
	{
		string text = msg?.text;
		if (!HeliNet.Is(text)) return true;
		HeliNet.OnClientReceive(text);
		return false;
	}
}
