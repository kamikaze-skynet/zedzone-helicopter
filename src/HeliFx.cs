using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZedHelicopter;

internal static class HeliFx
{
	private enum Kind
	{
		Tracer,
		Flash,
		Puff,
		Casing
	}

	private class Fx
	{
		public GameObject Go;

		public SpriteRenderer Sr;

		public Kind K;

		public float T0;

		public float Dur;

		public Vector2 A;

		public Vector2 B;

		public Vector2 Vel;

		public float Rot;

		public float Spin;

		public float S0;

		public float S1;

		public float Fall;

		public Color C;
	}

	private static readonly List<Fx> _fx = new List<Fx>();

	private static Sprite _streak;

	private static Sprite _flash;

	private static Sprite _puff;

	private static Sprite _casing;

	private static bool _made;

	private static Sprite Make(int w, int h, Func<int, int, Color> px, Vector2 pivot)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(w, h, (TextureFormat)4, false)
		{
			filterMode = (FilterMode)0,
			wrapMode = (TextureWrapMode)1
		};
		((Object)val).hideFlags = (HideFlags)61;
		for (int i = 0; i < h; i++)
		{
			for (int j = 0; j < w; j++)
			{
				val.SetPixel(j, i, px(j, i));
			}
		}
		val.Apply();
		Sprite obj = Sprite.Create(val, new Rect(0f, 0f, (float)w, (float)h), pivot, 32f);
		((Object)obj).hideFlags = (HideFlags)61;
		return obj;
	}

	private static void Ensure()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		if (_made)
		{
			return;
		}
		_made = true;
		_streak = Make(16, 3, delegate(int x, int y)
		{
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			float num7 = (float)x / 15f;
			float num8 = num7 * num7;
			if (y != 1)
			{
				num8 *= 0.4f;
			}
			Color result2 = Color.Lerp(new Color(1f, 0.45f, 0.1f), new Color(1f, 0.97f, 0.75f), num7);
			result2.a = num8;
			return result2;
		}, new Vector2(1f, 0.5f));
		_flash = Make(15, 15, delegate(int x, int y)
		{
			//IL_0113: Unknown result type (might be due to invalid IL or missing references)
			int num3 = x - 4;
			int num4 = y - 7;
			float num5 = Mathf.Sqrt((float)(num3 * num3 + num4 * num4));
			float num6 = 0f;
			Color result = default(Color);
			result = new Color(1f, 0.85f, 0.35f);
			if (num5 <= 1.6f)
			{
				num6 = 1f;
				result = new Color(1f, 1f, 0.9f);
			}
			else if (num4 == 0 && num3 > 0 && num3 <= 10)
			{
				num6 = 1f - (float)num3 / 11f;
			}
			else if (num4 == 0 && num3 < 0 && num3 >= -3)
			{
				num6 = 0.7f - (float)(-num3) / 5f;
			}
			else if (num3 == 0 && Math.Abs(num4) <= 4)
			{
				num6 = 0.9f - (float)Math.Abs(num4) / 5f;
			}
			else if (Math.Abs(num3) == Math.Abs(num4) && num3 > 0 && num3 <= 4)
			{
				num6 = 0.75f - (float)num3 / 5f;
			}
			else if (num5 <= 2.6f)
			{
				num6 = 0.55f;
				result = new Color(1f, 0.6f, 0.2f);
			}
			result.a = Mathf.Clamp01(num6);
			return result;
		}, new Vector2(0.3f, 0.5f));
		_puff = Make(7, 7, delegate(int x, int y)
		{
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			float num = Mathf.Sqrt((float)((x - 3) * (x - 3) + (y - 3) * (y - 3)));
			float num2 = ((num <= 2f) ? 1f : ((num <= 3.2f) ? 0.45f : 0f));
			return new Color(1f, 1f, 1f, num2);
		}, new Vector2(0.5f, 0.5f));
		_casing = Make(3, 2, (int x, int y) => (x != 0) ? ((y != 1) ? new Color(0.85f, 0.66f, 0.26f) : new Color(0.98f, 0.84f, 0.45f)) : new Color(0.62f, 0.45f, 0.16f), new Vector2(0.5f, 0.5f));
	}

	private static Fx Add(Kind k, Sprite sp, int layer, int order, float t0, float dur)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		GameObject val = new GameObject("zpi_fx");
		SpriteRenderer val2 = val.AddComponent<SpriteRenderer>();
		val2.sprite = sp;
		((Renderer)val2).sortingLayerID = layer;
		((Renderer)val2).sortingOrder = order;
		if (t0 > Time.realtimeSinceStartup)
		{
			((Renderer)val2).enabled = false;
		}
		Fx fx = new Fx
		{
			Go = val,
			Sr = val2,
			K = k,
			T0 = t0,
			Dur = dur
		};
		_fx.Add(fx);
		return fx;
	}

	public static void Shot(Vector2 muzzle, Vector2 target, bool hit, float heading, float altOffset, int layer, Color? tint = null, bool casing = true)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		Ensure();
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		float num = Mathf.Clamp(HeliPlugin.HeliFxScale.Value, 0.3f, 5f);
		Vector2 val = target - muzzle;
		float magnitude = val.magnitude;
		float num2 = Mathf.Atan2(val.y, val.x) * 57.29578f;
		float num3 = Mathf.Clamp(magnitude / 220f, 0.035f, 0.09f);
		Fx fx = Add(Kind.Tracer, _streak, layer, 32016, realtimeSinceStartup, num3);
		fx.A = muzzle;
		fx.B = target;
		fx.Rot = num2;
		fx.S0 = 2.2f * num;
		fx.S1 = num;
		fx.C = (tint ?? Color.white);
		Fx fx2 = Add(Kind.Flash, _flash, layer, 32020, realtimeSinceStartup, 0.05f);
		fx2.A = muzzle;
		fx2.Rot = num2 + Random.Range(-8f, 8f);
		fx2.S0 = Random.Range(0.85f, 1.25f) * num;
		fx2.C = (tint ?? Color.white);
		int num4 = (hit ? 4 : 3);
		for (int i = 0; i < num4; i++)
		{
			Fx fx3 = Add(Kind.Puff, _puff, layer, 32005, realtimeSinceStartup + num3, Random.Range(0.25f, 0.42f));
			fx3.A = target + Random.insideUnitCircle * 0.12f * num;
			fx3.Vel = (-val.normalized * 0.6f + Random.insideUnitCircle) * (hit ? 1.6f : 1.1f) * Mathf.Sqrt(num);
			fx3.S0 = (hit ? 0.45f : 0.55f) * num;
			fx3.S1 = (hit ? 0.75f : 1.35f) * num;
			fx3.C = (hit ? new Color(0.62f, 0.06f, 0.05f, 0.95f) : new Color(0.66f, 0.6f, 0.5f, 0.8f));
		}
		if (casing && Random.value < 0.7f)
		{
			float num5 = heading * ((float)Math.PI / 180f);
			Vector2 val2 = default(Vector2);
			val2 = new Vector2(Mathf.Sin(num5), 0f - Mathf.Cos(num5));
			Vector2 val3 = default(Vector2);
			val3 = new Vector2(Mathf.Cos(num5), Mathf.Sin(num5));
			Fx fx4 = Add(Kind.Casing, _casing, layer, 32014, realtimeSinceStartup, Random.Range(0.45f, 0.6f));
			fx4.A = muzzle - val3 * 0.6f;
			fx4.Vel = val2 * Random.Range(1.6f, 2.6f) + val3 * Random.Range(-0.4f, 0.2f);
			fx4.Rot = Random.Range(0f, 360f);
			fx4.Spin = Random.Range(600f, 1100f) * ((Random.value < 0.5f) ? (-1f) : 1f);
			fx4.S0 = 1.2f * num;
			fx4.Fall = altOffset;
			fx4.C = Color.white;
		}
	}

	public static void Tick()
	{
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		for (int num = _fx.Count - 1; num >= 0; num--)
		{
			Fx fx = _fx[num];
			float num2 = (realtimeSinceStartup - fx.T0) / Mathf.Max(0.001f, fx.Dur);
			if (num2 >= 1f || (Object)(object)fx.Go == (Object)null)
			{
				try
				{
					if ((Object)(object)fx.Go != (Object)null)
					{
						Object.Destroy((Object)(object)fx.Go);
					}
				}
				catch
				{
				}
				_fx.RemoveAt(num);
			}
			else if (!(num2 < 0f))
			{
				try
				{
					if (!((Renderer)fx.Sr).enabled)
					{
						((Renderer)fx.Sr).enabled = true;
					}
					Transform transform = fx.Go.transform;
					float num3 = realtimeSinceStartup - fx.T0;
					Color c = fx.C;
					switch (fx.K)
					{
					case Kind.Tracer:
						transform.position = ((Vector2)(Vector2.Lerp(fx.A, fx.B, num2)));
						transform.rotation = Quaternion.Euler(0f, 0f, fx.Rot);
						transform.localScale = new Vector3(fx.S0, fx.S1, 1f);
						break;
					case Kind.Flash:
					{
						transform.position = ((Vector2)(fx.A));
						transform.rotation = Quaternion.Euler(0f, 0f, fx.Rot);
						float num6 = fx.S0 * (1f - num2 * 0.3f);
						transform.localScale = new Vector3(num6, num6, 1f);
						c.a *= 1f - num2;
						break;
					}
					case Kind.Puff:
					{
						float num4 = 1f - num2;
						transform.position = ((Vector2)(fx.A + fx.Vel * num3 * (0.6f + 0.4f * num4)));
						float num5 = Mathf.Lerp(fx.S0, fx.S1, Mathf.Sqrt(num2));
						transform.localScale = new Vector3(num5, num5, 1f);
						c.a *= num4 * num4;
						break;
					}
					case Kind.Casing:
					{
						Vector2 val = fx.A + fx.Vel * num3 * (1f - num2 * 0.5f) - new Vector2(0f, fx.Fall * num2 * num2);
						transform.position = ((Vector2)(val));
						transform.rotation = Quaternion.Euler(0f, 0f, fx.Rot + fx.Spin * num3);
						transform.localScale = new Vector3(fx.S0, fx.S0, 1f);
						if (num2 > 0.7f)
						{
							c.a *= (1f - num2) / 0.3f;
						}
						break;
					}
					}
					fx.Sr.color = c;
				}
				catch
				{
				}
			}
		}
	}
}
