using System;
using System.Reflection;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Vertx.Variants.Editor
{
	[CustomEditor(typeof(VariantImporter))]
	public partial class VariantImporterInspector : ScriptedImporterEditor
	{
		public override bool showImportedObject => false;
		protected override bool needsApplyRevert => false;
		protected override bool ShouldHideOpenButton() => true;

		private static readonly GUIContent baseLabel = new GUIContent("Base");

		/// <summary>
		/// A temporary object we instance purely so the editor isn't read-only.
		/// If we use variant, then because it is an imported asset the editor is GUI disabled.
		/// There doesn't seem to be a way around this, disappointingly.
		/// </summary>
		private ScriptableObject temporaryVariant;
		private UnityEditor.Editor temporaryVariantEditor;
		private string assetPath;

		private bool bound;

		protected override void Awake()
		{
			base.Awake();
			Refresh(false);
		}

		private void Refresh(bool repaint = true, bool addToDelegates = true)
		{
			temporaryVariantEditor = null;

			if (assetTarget != null)
			{
				assetPath = AssetDatabase.GetAssetPath(assetTarget);

				if (addToDelegates)
				{
					EditorApplication.contextualPropertyMenu += ContextualPropertyMenu;

					MethodInfo beginProperty = typeof(EditorGUIUtility).GetMethod("add_beginProperty", BindingFlags.Static | BindingFlags.NonPublic);
					beginProperty.Invoke(null, new object[] {new Action<Rect, SerializedProperty>(BeginProperty)});
				}

				temporaryVariant = (ScriptableObject) Instantiate(assetTarget);
				temporaryVariantEditor = CreateEditor(temporaryVariant);

				string json = ((VariantImporter) target).Json;
				if(!string.IsNullOrEmpty(json))
					overrideData = JsonConvert.DeserializeObject<OverrideData>(json);

				VariantImporter.OnImport += OnImport;
				bound = true;
			}
			else
			{
				assetPath = null;
				bound = false;
			}
			
			if(repaint)
				Repaint();
		}

		private void OnImport(string path)
		{
			if (path != assetPath) return;
			EditorApplication.delayCall += () => Refresh(addToDelegates: false);
		}

		private void ContextualPropertyMenu(GenericMenu menu, SerializedProperty property)
		{
			// Do not add context menus for objects this editor is not managing.
			if (property.serializedObject.targetObject != temporaryVariant) return;

			if (overrideData?.Overrides.ContainsKey(property.propertyPath) ?? false)
			{
				menu.AddItem(new GUIContent("Revert"), false, () => RemovePropertyModification(property));
			}
		}

		private void BeginProperty(Rect position, SerializedProperty property)
		{
			// Do not draw property swatches for objects this editor is not managing.
			if (property.serializedObject.targetObject != temporaryVariant) return;

			// Only draw the swatch if there is override data present for this property path.
			if (!overrideData?.Overrides.ContainsKey(property.propertyPath) ?? true)
				return;

			position.x = 0;
			position.width = 2;
			EditorGUI.DrawRect(position, new Color(0.05882353f, 0.5019608f, 0.7450981f));
		}

		public override void OnDisable()
		{
			base.OnDisable();
			if (temporaryVariantEditor != null)
				DestroyImmediate(temporaryVariantEditor, true);
			if (temporaryVariant != null)
				DestroyImmediate(temporaryVariant, true);

			EditorApplication.contextualPropertyMenu -= ContextualPropertyMenu;
			VariantImporter.OnImport -= OnImport;

			if (bound)
			{
				MethodInfo beginProperty = typeof(EditorGUIUtility).GetMethod("remove_beginProperty", BindingFlags.Static | BindingFlags.NonPublic);
				beginProperty.Invoke(null, new object[] {new Action<Rect, SerializedProperty>(BeginProperty)});
				bound = true;
			}
		}

		public override void OnInspectorGUI()
		{
			if (temporaryVariantEditor == null)
				return;

			using var cCS = new EditorGUI.ChangeCheckScope();

			temporaryVariantEditor.OnInspectorGUI();

			if (!cCS.changed)
				return;

			SerializedProperty tempProp = temporaryVariantEditor.serializedObject.GetIterator();
			//SerializedProperty targetProp = variantSerializedObject.GetIterator();
			tempProp.NextVisible(true);
			while (tempProp.NextVisible(true))
			{
				if (tempProp.propertyPath == "m_Script") continue;
				/*targetProp.Next(true);
						if (tempProp.propertyPath != targetProp.propertyPath)
						{
							Debug.LogError("properties have become unsynced.\n" +
							               $"origin: \"{tempProp.propertyPath}\"\n" +
							               $"target: \"{targetProp.propertyPath}\"");
							break;
						}*/
				if (tempProp.propertyType == SerializedPropertyType.Generic) continue;
				if (!assetSerializedObject.CopyFromSerializedPropertyIfDifferent(tempProp)) continue;
				assetSerializedObject.ApplyModifiedProperties();
				ModifiedProperty(tempProp.propertyPath, tempProp);
			}
		}

		protected override void OnHeaderGUI()
		{
			base.OnHeaderGUI();

			// Draws the "base" object field (and extra UI to visibly connect it to the default header)
			Rect controlRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
			Rect bg = new Rect(controlRect.x - 3, controlRect.y - 4, controlRect.width + 6, controlRect.height + 8);
			controlRect.x += 3;
			controlRect.width -= 3;
			controlRect.height = EditorGUIUtility.singleLineHeight;
			EditorGUI.DrawRect(bg, new Color(0.2352941f, 0.2352941f, 0.2352941f));

			GUI.enabled = false;
			SerializedProperty origin = serializedObject.FindProperty(nameof(VariantImporter.Origin));
			string path = AssetDatabase.GUIDToAssetPath(origin.stringValue);
			ScriptableObject asset = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
			float labelWidthTemp = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 50;
			EditorGUI.ObjectField(controlRect, baseLabel, asset, typeof(ScriptableObject), false);
			EditorGUIUtility.labelWidth = labelWidthTemp;
			GUI.enabled = true;

			EditorGUI.DrawRect(new Rect(bg.x, bg.yMax - 3, bg.width, 1), Color.black);
		}
	}
}