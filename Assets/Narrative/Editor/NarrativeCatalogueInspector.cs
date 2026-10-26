using System.Linq;
using GraduationDemo.Narrative;
using UnityEditor;
using UnityEngine;

namespace GraduationDemo.Editor
{
    [CustomEditor(typeof(NarrativeCatalogue))]
    public sealed class NarrativeCatalogueInspector : UnityEditor.Editor
    {
        int tab;
        string search = "";
        readonly string[] tabs = { "Clues", "Statements", "Questions", "Dialogue", "Voices", "Routes", "Arrest" };
        readonly string[] fields = { "clues", "statements", "theories", "sequences", "voices", "routes", "arrestRules" };
        public override void OnInspectorGUI()
        {
            var c = (NarrativeCatalogue)target;
            EditorGUILayout.LabelField("Murder Most Inconvenient", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Immutable narrative definitions. Player discoveries live in TrainNarrative.State. Refresh preserves audio assignments and question/arrest metadata; source definitions are regenerated.", MessageType.Info);
            if (GUILayout.Button("Validate catalogue")) NarrativeImporter.Validate();
            tab = GUILayout.SelectionGrid(tab, tabs, 4);
            search = EditorGUILayout.TextField("Find text / character / ID", search);
            serializedObject.Update();
            var list = serializedObject.FindProperty(fields[tab]);
            EditorGUILayout.LabelField(list.arraySize + " definitions");
            for (int i = 0; i < list.arraySize; i++)
            {
                var value = list.GetArrayElementAtIndex(i);
                string label = value.FindPropertyRelative("id")?.stringValue ?? "Definition " + i;
                foreach (var field in new[] { "name", "speaker", "claim", "text", "subtitle", "suspect", "sequenceId" })
                {
                    var property = value.FindPropertyRelative(field);
                    if (property != null && !string.IsNullOrEmpty(property.stringValue)) label += " / " + property.stringValue;
                }
                if (!string.IsNullOrEmpty(search) && label.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                string compact = label.Length > 110 ? label.Substring(0, 107) + "..." : label;
                value.isExpanded = EditorGUILayout.Foldout(value.isExpanded, compact, true);
                if (value.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    var child = value.Copy(); var end = value.GetEndProperty();
                    if (child.NextVisible(true)) do { if (SerializedProperty.EqualContents(child, end)) break; EditorGUILayout.PropertyField(child, true); } while (child.NextVisible(false));
                    EditorGUI.indentLevel--;
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
