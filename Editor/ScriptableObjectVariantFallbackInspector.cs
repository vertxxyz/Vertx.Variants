using UnityEditor;

namespace Vertx.Variants.Editor
{
	[CustomEditor(typeof(ScriptableObjectVariantFallback))]
	public class ScriptableObjectVariantFallbackInspector : UnityEditor.Editor
	{
		public override void OnInspectorGUI() => EditorGUILayout.HelpBox("The original asset this variant depends on has been deleted!", MessageType.Error);
	}
}