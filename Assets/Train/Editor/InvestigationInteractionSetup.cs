using System;
using System.Linq;
using GraduationDemo.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GraduationDemo.Train.Editor
{
    public static class InvestigationInteractionSetup
    {
        [MenuItem("Graduation Demo/Narrative/Install Clue Markers and Question Menu")]
        public static void InstallInOpenScene()
        {
            var owner = UnityEngine.Object.FindAnyObjectByType<TrainNarrative>();
            if (owner == null) throw new InvalidOperationException("Open an investigation scene first.");
            if (owner.GetComponent<SuspectQuestionMenu>() == null) Undo.AddComponent<SuspectQuestionMenu>(owner.gameObject);
            foreach (var binding in UnityEngine.Object.FindObjectsByType<NarrativeInteraction>().Where(b => !string.IsNullOrEmpty(b.clueId)))
                if (binding.GetComponent<ClueMarker>() == null) Undo.AddComponent<ClueMarker>(binding.gameObject);
            EditorSceneManager.MarkSceneDirty(owner.gameObject.scene);
        }
        // Batch entry point: update only interaction components, preserving scene geometry and placement.
        [MenuItem("Graduation Demo/Narrative/Refresh Interaction Scenes")]
        public static void RefreshAndInstall()
        {
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                if (EditorSceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scene edits before refreshing interaction scenes.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            NarrativeImporter.Refresh();
            foreach (var path in new[] { "Assets/Scenes/CarnivalExpress.unity", "Assets/Scenes/ThirdPersonTrain.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path); InstallInOpenScene();
                EditorSceneManager.SaveScene(scene);
                var markers = UnityEngine.Object.FindObjectsByType<ClueMarker>();
                if (markers.Length != 10) throw new InvalidOperationException("Expected ten positioned clue markers in " + path);
                Debug.Log("INTERACTION_SETUP_OK: " + path + "; " + markers.Length + " markers and dedicated question menu.");
            }
            AssetDatabase.SaveAssets();
            if (!Application.isBatchMode && setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }
}
