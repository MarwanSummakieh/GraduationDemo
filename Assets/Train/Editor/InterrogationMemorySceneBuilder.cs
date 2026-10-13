using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GraduationDemo.Train;

namespace GraduationDemo.Editor
{
    public static class InterrogationMemorySceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/InterrogationMemory.unity";

        [MenuItem("Graduation Demo/Create Interrogation Memory Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before creating the memory scene.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                EditorSceneManager.OpenScene(ScenePath);
                Debug.Log("Memory scene already exists; opened without replacing edits.");
                return;
            }
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/CarnivalExpress.unity");
            var tour = UnityEngine.Object.FindAnyObjectByType<TrainTour>();
            if (tour == null) throw new InvalidOperationException("The source scene needs a TrainTour.");
            tour.notebook = tour.gameObject.AddComponent<DetectiveNotebook>();
            // Author IDs so renaming or moving scene objects later does not duplicate memories.
            foreach (var observation in UnityEngine.Object.FindObjectsByType<TrainObservation>())
            {
                observation.memoryId = observation.speaker != null
                    ? "passenger/" + observation.speaker.role
                    : "object/" + observation.title;
                EditorUtility.SetDirty(observation);
            }
            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("INTERROGATION_MEMORY_SCENE_OK: " + ScenePath);
        }
    }
}
