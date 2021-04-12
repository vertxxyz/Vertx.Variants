using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Vertx.Variants.Editor
{
	[Serializable]
	internal struct Vector4Serializable
	{
		public float X;
		public float Y;
		public float Z;
		public float W;

		public Vector4Serializable(Color c)
		{
			X = c.r;
			Y = c.g;
			Z = c.b;
			W = c.a;
		}

		public Vector4Serializable(Vector4 c)
		{
			X = c.x;
			Y = c.y;
			Z = c.z;
			W = c.w;
		}

		public Vector4Serializable(Rect r)
		{
			X = r.x;
			Y = r.y;
			Z = r.width;
			W = r.height;
		}

		public Vector4Serializable(RectInt r)
		{
			X = r.x;
			Y = r.y;
			Z = r.width;
			W = r.height;
		}

		public Vector4Serializable(Quaternion q)
		{
			X = q.x;
			Y = q.y;
			Z = q.z;
			W = q.w;
		}

		public static explicit operator Color(Vector4Serializable v) => new Color(v.X, v.Y, v.Z, v.W);
		public static explicit operator Vector4(Vector4Serializable v) => new Vector4(v.X, v.Y, v.Z, v.W);
		public static explicit operator Rect(Vector4Serializable v) => new Rect(v.X, v.Y, v.Z, v.W);
		public static explicit operator RectInt(Vector4Serializable v) => new RectInt((int) v.X, (int) v.Y, (int) v.Z, (int) v.W);
		public static explicit operator Quaternion(Vector4Serializable v) => new Quaternion(v.X, v.Y, v.Z, v.W);
	}

	[Serializable]
	internal struct Vector3Serializable
	{
		public float X;
		public float Y;
		public float Z;

		public Vector3Serializable(Vector3 c)
		{
			X = c.x;
			Y = c.y;
			Z = c.z;
		}

		public static explicit operator Vector3(Vector3Serializable c) => new Vector3(c.X, c.Y, c.Z);
		public static explicit operator Vector3Int(Vector3Serializable c) => new Vector3Int((int) c.X, (int) c.Y, (int) c.Z);
	}

	[Serializable]
	internal struct Vector2Serializable
	{
		public float X;
		public float Y;

		public Vector2Serializable(Vector2 c)
		{
			X = c.x;
			Y = c.y;
		}

		public static explicit operator Vector2(Vector2Serializable c) => new Vector2(c.X, c.Y);

		public static explicit operator Vector2Int(Vector2Serializable c) => new Vector2Int((int) c.X, (int) c.Y);
	}

	[Serializable]
	internal struct BoundsSerializable
	{
		public Vector3Serializable Center;
		public Vector3Serializable ExtentsOrSize;

		public BoundsSerializable(Bounds b)
		{
			Center = new Vector3Serializable(b.center);
			ExtentsOrSize = new Vector3Serializable(b.size);
		}

		public BoundsSerializable(BoundsInt b)
		{
			Center = new Vector3Serializable(b.center);
			ExtentsOrSize = new Vector3Serializable(b.size);
		}

		public static explicit operator Bounds(BoundsSerializable c) => new Bounds((Vector3) c.Center, (Vector3) c.ExtentsOrSize);

		public static explicit operator BoundsInt(BoundsSerializable c) =>
			new BoundsInt(new Vector3Int((int) c.Center.X, (int) c.Center.Y, (int) c.Center.Z),
				new Vector3Int((int) c.ExtentsOrSize.X, (int) c.ExtentsOrSize.Y, (int) c.ExtentsOrSize.Z));
	}

	[Serializable]
	internal struct GradientSerializable
	{
		public int Mode;
		public GradientAlphaKeySerializable[] AlphaKeys;
		public GradientColorKeySerializable[] ColorKeys;

		public GradientSerializable(Gradient g)
		{
			Mode = (int) g.mode;
			GradientAlphaKey[] gradientAlphaKeys = g.alphaKeys;
			AlphaKeys = new GradientAlphaKeySerializable[gradientAlphaKeys.Length];
			for (int i = 0; i < gradientAlphaKeys.Length; i++)
			{
				var original = gradientAlphaKeys[i];
				AlphaKeys[i] = new GradientAlphaKeySerializable(original.alpha, original.time);
			}

			GradientColorKey[] gradientColorKeys = g.colorKeys;
			ColorKeys = new GradientColorKeySerializable[gradientColorKeys.Length];
			for (int i = 0; i < gradientColorKeys.Length; i++)
			{
				var original = gradientColorKeys[i];
				ColorKeys[i] = new GradientColorKeySerializable(original.color, original.time);
			}
		}

		public static explicit operator Gradient(GradientSerializable g)
		{
			var gradient = new Gradient {mode = (GradientMode) g.Mode};

			var gradientAlphaKeys = g.AlphaKeys;
			var alphaKeys = new GradientAlphaKey[gradientAlphaKeys.Length];
			for (int i = 0; i < gradientAlphaKeys.Length; i++)
			{
				var original = gradientAlphaKeys[i];
				alphaKeys[i] = new GradientAlphaKey(original.Alpha, original.Time);
			}

			var gradientColorKeys = g.ColorKeys;
			var colorKeys = new GradientColorKey[gradientColorKeys.Length];
			for (int i = 0; i < gradientColorKeys.Length; i++)
			{
				var original = gradientColorKeys[i];
				colorKeys[i] = new GradientColorKey((Color) original.Color, original.Time);
			}

			gradient.SetKeys(colorKeys, alphaKeys);
			return gradient;
		}
	}

	[Serializable]
	internal struct GradientAlphaKeySerializable
	{
		public float Alpha;
		public float Time;

		public GradientAlphaKeySerializable(float alpha, float time)
		{
			Alpha = alpha;
			Time = time;
		}
	}

	[Serializable]
	internal struct GradientColorKeySerializable
	{
		public Vector4Serializable Color;
		public float Time;

		public GradientColorKeySerializable(Color col, float time)
		{
			Color = new Vector4Serializable(col);
			Time = time;
		}
	}

	[Serializable]
	internal class AnimationCurveSerializable
	{
		public KeyframeSerializable[] Keyframes;

		public AnimationCurveSerializable(AnimationCurve curve)
		{
			Keyframe[] keys = curve.keys;
			Keyframes = new KeyframeSerializable[keys.Length];
			for (int i = 0; i < keys.Length; i++)
				Keyframes[i] = new KeyframeSerializable(keys[i]);
		}

		public static explicit operator AnimationCurve(AnimationCurveSerializable c)
		{
			var keyframes = c.Keyframes;
			Keyframe[] keys = new Keyframe[keyframes.Length];
			for (int i = 0; i < keyframes.Length; i++)
			{
				keys[i] = keyframes[i];
			}

			return new AnimationCurve(keys);
		}
	}

	[Serializable]
	internal struct KeyframeSerializable
	{
		public float Time;
		public float Value;
		public float InTangent;
		public float OutTangent;
		public int TangentMode;
		public int WeightedMode;
		public float InWeight;
		public float OutWeight;

		public KeyframeSerializable(Keyframe keyframe)
		{
			Time = keyframe.time;
			Value = keyframe.value;
			InTangent = keyframe.inTangent;
			OutTangent = keyframe.outTangent;
			TangentMode = (int) typeof(Keyframe).GetProperty("tangentModeInternal", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(keyframe);
			WeightedMode = (int) keyframe.weightedMode;
			InWeight = keyframe.inWeight;
			OutWeight = keyframe.outWeight;
		}

		public static implicit operator Keyframe(KeyframeSerializable k)
		{
			Keyframe keyframe = new Keyframe(k.Time, k.Value, k.InTangent, k.OutTangent, k.InWeight, k.OutWeight) {weightedMode = (WeightedMode) k.WeightedMode};
			typeof(Keyframe).GetProperty("tangentModeInternal", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(keyframe, (AnimationUtility.TangentMode) k.TangentMode);
			return keyframe;
		}
	}
}