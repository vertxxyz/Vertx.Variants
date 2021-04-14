using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Vertx.Variants.Editor
{
	internal class OverrideData
	{
		[JsonProperty("o")]
		public Dictionary<string, JToken> Overrides = new Dictionary<string, JToken>();
	}
	
	public partial class VariantImporterInspector
	{
		private OverrideData overrideData;

		public void ModifiedProperty(string path, SerializedProperty property)
		{
			object data;
			switch (property.propertyType)
			{
				case SerializedPropertyType.Integer:
				case SerializedPropertyType.LayerMask:
					data = property.intValue;
					break;
				case SerializedPropertyType.Boolean:
					data = property.boolValue;
					break;
				case SerializedPropertyType.Float:
					data = property.floatValue;
					break;
				case SerializedPropertyType.String:
				case SerializedPropertyType.Character:
					data = property.stringValue;
					break;
				case SerializedPropertyType.Color:
					data = new Vector4Serializable(property.colorValue);
					break;
				case SerializedPropertyType.ObjectReference:
					data = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(property.objectReferenceValue));
					break;
				case SerializedPropertyType.Enum:
					data = property.enumValueIndex;
					break;
				case SerializedPropertyType.Vector2:
					data = new Vector2Serializable(property.vector2Value);
					break;
				case SerializedPropertyType.Vector3:
					data = new Vector3Serializable(property.vector3Value);
					break;
				case SerializedPropertyType.Vector4:
					data = new Vector4Serializable(property.vector4Value);
					break;
				case SerializedPropertyType.Rect:
					data = new Vector4Serializable(property.rectValue);
					break;
				case SerializedPropertyType.ArraySize:
					data = property.arraySize;
					break;
				case SerializedPropertyType.AnimationCurve:
					data = new AnimationCurveSerializable(property.animationCurveValue);
					break;
				case SerializedPropertyType.Bounds:
					data = new BoundsSerializable(property.boundsValue);
					break;
				case SerializedPropertyType.Gradient:
					data = new GradientSerializable(GetGradientValue(property));
					break;
				case SerializedPropertyType.Quaternion:
					data = new Vector4Serializable(property.quaternionValue);
					break;
				case SerializedPropertyType.Vector2Int:
				    data = new Vector2Serializable(property.vector2IntValue);
					break;
				case SerializedPropertyType.Vector3Int:
					data = new Vector3Serializable(property.vector3IntValue);
					break;
				case SerializedPropertyType.RectInt:
					data = new Vector4Serializable(property.rectIntValue);
					break;
				case SerializedPropertyType.BoundsInt:
					data = new BoundsSerializable(property.boundsIntValue);
					break;
				case SerializedPropertyType.Hash128:
					data = property.hash128Value;
					break;
				case SerializedPropertyType.FixedBufferSize:
					// Although it may appear we can handle fixed buffer size, it cannot be deserialized.
					// data = property.fixedBufferSize;
				case SerializedPropertyType.ExposedReference:
				case SerializedPropertyType.Generic:
				case SerializedPropertyType.ManagedReference:
				default:
					Debug.LogWarning($"{property.propertyType} is not handled, sorry! ({property.propertyPath})");
					return;
			}

			overrideData ??= new OverrideData();
			overrideData.Overrides[path] = JToken.FromObject(data);
			serializedObject.FindProperty(nameof(VariantImporter.Json)).stringValue = JsonConvert.SerializeObject(overrideData);
			serializedObject.ApplyModifiedProperties();
		}
		
		private static Gradient GetGradientValue(SerializedProperty property)
		{
			PropertyInfo propertyInfo = typeof(SerializedProperty).GetProperty(
				"gradientValue",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
				null,
				typeof(Gradient),
				Array.Empty<Type>(),
				null
			);
			return propertyInfo?.GetValue(property, null) as Gradient;
		}

		private void RemovePropertyModification(SerializedProperty property)
		{
			overrideData?.Overrides.Remove(property.propertyPath);
			
			var variantImporter = (VariantImporter) target;
			
			// Apply changes to the importer (and request a reimport)
			serializedObject.FindProperty(nameof(VariantImporter.Json)).stringValue = JsonConvert.SerializeObject(overrideData);
			serializedObject.ApplyModifiedProperties();
			AssetDatabase.ImportAsset(variantImporter.assetPath, ImportAssetOptions.ForceSynchronousImport);
			
			// Ensure the temporary variant (our fake inspector) is also reverted to the origin's data.
			if (string.IsNullOrEmpty(variantImporter.Origin))
				return;
			string path = AssetDatabase.GUIDToAssetPath(variantImporter.Origin);
			if (string.IsNullOrEmpty(path))
				return;
			using var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>(path));
			SerializedProperty prop = so.FindProperty(property.propertyPath);
			temporaryVariantEditor.serializedObject.CopyFromSerializedPropertyIfDifferent(prop);
			temporaryVariantEditor.serializedObject.ApplyModifiedProperties();
			Repaint();
		}
	}
}