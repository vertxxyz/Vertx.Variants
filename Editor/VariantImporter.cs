using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Vertx.Variants.Editor
{
	[ScriptedImporter(0, extension, 10)]
	public class VariantImporter : ScriptedImporter
	{
		public const string extension = "assetvariant";

		public string Origin;
		public string Json;

		public override void OnImportAsset(AssetImportContext ctx)
		{
			void CreateFallback()
			{
				ScriptableObjectVariantFallback variantBase = ScriptableObject.CreateInstance<ScriptableObjectVariantFallback>();
				ctx.AddObjectToAsset(nameof(variantBase), variantBase);
				ctx.SetMainObject(variantBase);
			}

			if (Origin == default)
			{
				CreateFallback();
				return;
			}

			string dependencyPath = AssetDatabase.GUIDToAssetPath(Origin);
			if (string.IsNullOrEmpty(dependencyPath))
			{
				CreateFallback();
				return;
			}

			ctx.DependsOnArtifact(new GUID(Origin));
			
			// We need to reload the dependency from the asset database or else it doesn't know we depend on it.
			var dependency = AssetDatabase.LoadAssetAtPath<ScriptableObject>(dependencyPath);

			if (dependency == null)
			{
				CreateFallback();
				return;
			}

			ScriptableObject variant = Instantiate(dependency);

			var overrideData = string.IsNullOrEmpty(Json) ? null : JsonConvert.DeserializeObject<OverrideData>(Json);

			if (overrideData?.Overrides.Count > 0)
			{
				using var so = new SerializedObject(variant);
				List<string> toRemove = null;
				foreach (KeyValuePair<string,JToken> dataOverride in overrideData.Overrides)
				{
					SerializedProperty property = so.FindProperty(dataOverride.Key);
					if (property == null)
					{
						Debug.LogWarning($"{dataOverride.Key} no longer exists in {variant}");
						toRemove ??= new List<string>();
						toRemove.Add(dataOverride.Key);
						continue;
					}
					SetProperty(property, dataOverride.Value);
				}

				if (toRemove != null)
				{
					foreach (string s in toRemove)
						overrideData.Overrides.Remove(s);
					Json = JsonConvert.SerializeObject(overrideData);
					EditorUtility.SetDirty(this);
				}

				so.ApplyModifiedPropertiesWithoutUndo();
			}
			
			variant.name = Path.GetFileNameWithoutExtension(assetPath);
			ctx.AddObjectToAsset(nameof(variant), variant);
			ctx.SetMainObject(variant);
		}

		[MenuItem("Assets/Create/ScriptableObject Variant", true, 91)]
		private static bool ValidateCreateVariant()
		{
			Object target = Selection.activeObject;
			if (target == null)
				return false;
			return target is ScriptableObject;
		}

		[MenuItem("Assets/Create/ScriptableObject Variant", false, 91)]
		private static void CreateVariant()
		{
			Object target = Selection.activeObject;
			if (!(target is ScriptableObject origin))
				return;
			string text = "{}";
			string path = Path.ChangeExtension(AssetDatabase.GetAssetPath(origin), null);
			string pathWithExtension;
			do
			{
				path = $"{path} (Variant)";
				pathWithExtension = Path.ChangeExtension(path, extension);
			} while (File.Exists(pathWithExtension));

			File.WriteAllText(pathWithExtension, text);
			AssetDatabase.ImportAsset(pathWithExtension, ImportAssetOptions.ForceSynchronousImport);
			VariantImporter assetImporter = (VariantImporter) GetAtPath(pathWithExtension);
			assetImporter.Origin = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(origin)).ToString();
			EditorUtility.SetDirty(assetImporter);
			assetImporter.SaveAndReimport();
		}
		
		public static void SetProperty(SerializedProperty property, JToken dataOverrideValue)
		{
			switch (property.propertyType)
			{
				case SerializedPropertyType.Integer:
				case SerializedPropertyType.LayerMask:
					property.intValue = dataOverrideValue.ToObject<int>();
					break;
				case SerializedPropertyType.Boolean:
					property.boolValue = dataOverrideValue.ToObject<bool>();
					break;
				case SerializedPropertyType.Float:
					property.floatValue =  dataOverrideValue.ToObject<float>();
					break;
				case SerializedPropertyType.String:
				case SerializedPropertyType.Character:
					property.stringValue = dataOverrideValue.ToObject<string>();
					break;
				case SerializedPropertyType.Color:
					property.colorValue = (Color) dataOverrideValue.ToObject<Vector4Serializable>();
					break;
				case SerializedPropertyType.ObjectReference:
					property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(dataOverrideValue.ToObject<GUID>()));
					break;
				case SerializedPropertyType.Enum:
					property.enumValueIndex = dataOverrideValue.ToObject<int>();
					break;
				case SerializedPropertyType.Vector2:
					property.vector2Value = (Vector2) dataOverrideValue.ToObject<Vector2Serializable>();
					break;
				case SerializedPropertyType.Vector3:
					property.vector3Value = (Vector3) dataOverrideValue.ToObject<Vector3Serializable>();
					break;
				case SerializedPropertyType.Vector4:
					property.vector4Value = (Vector4) dataOverrideValue.ToObject<Vector4Serializable>();
					break;
				case SerializedPropertyType.Rect:
					property.rectValue = (Rect) dataOverrideValue.ToObject<Vector4Serializable>();
					break;
				case SerializedPropertyType.ArraySize:
					property.arraySize = dataOverrideValue.ToObject<int>();
					break;
				case SerializedPropertyType.AnimationCurve:
					property.animationCurveValue = (AnimationCurve) dataOverrideValue.ToObject<AnimationCurveSerializable>();
					break;
				case SerializedPropertyType.Bounds:
					property.boundsValue = (Bounds) dataOverrideValue.ToObject<BoundsSerializable>();
					break;
				case SerializedPropertyType.Gradient:
					SetGradientValue(property, (Gradient) dataOverrideValue.ToObject<GradientSerializable>());
					break;
				case SerializedPropertyType.Quaternion:
					property.quaternionValue = (Quaternion) dataOverrideValue.ToObject<Vector4Serializable>();
					break;
				case SerializedPropertyType.Vector2Int:
					property.vector2IntValue = (Vector2Int) dataOverrideValue.ToObject<Vector2Serializable>();
					break;
				case SerializedPropertyType.Vector3Int:
					property.vector3IntValue = (Vector3Int) dataOverrideValue.ToObject<Vector3Serializable>();
					break;
				case SerializedPropertyType.RectInt:
					property.rectIntValue = (RectInt) dataOverrideValue.ToObject<Vector4Serializable>();
					break;
				case SerializedPropertyType.BoundsInt:
					property.boundsIntValue = (BoundsInt) dataOverrideValue.ToObject<BoundsSerializable>();
					break;
				case SerializedPropertyType.Hash128:
					property.hash128Value = dataOverrideValue.ToObject<Hash128>();
					break;
				case SerializedPropertyType.ExposedReference:
				case SerializedPropertyType.Generic:
				case SerializedPropertyType.ManagedReference:
				case SerializedPropertyType.FixedBufferSize:
				default:
					Debug.LogWarning($"{property.type} is not handled, sorry!");
					return;
			}
			
			static void SetGradientValue(SerializedProperty property, Gradient gradient)
			{
				PropertyInfo propertyInfo = typeof(SerializedProperty).GetProperty(
					"gradientValue",
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
					null,
					typeof(Gradient),
					Array.Empty<Type>(),
					null
				);
				propertyInfo?.SetValue(property, gradient);
			}
		}
	}
}