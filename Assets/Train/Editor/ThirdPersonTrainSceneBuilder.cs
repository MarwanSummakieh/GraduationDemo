using System;
using System.Collections.Generic;
using System.Linq;
using GraduationDemo.Train;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GraduationDemo.Editor
{
    public static class ThirdPersonTrainSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/ThirdPersonTrain.unity";

        [MenuItem("Graduation Demo/Create Third Person Train Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                EditorSceneManager.OpenScene(ScenePath);
                return;
            }
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/CarnivalExpress.unity");
            // Unpack this copy only, keeping the existing train and narrative scene independent.
            foreach (var root in scene.GetRootGameObjects())
                if (PrefabUtility.IsPartOfPrefabInstance(root)) PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var tour = UnityEngine.Object.FindAnyObjectByType<TrainTour>();
            var train = GameObject.Find("THE CARNIVAL EXPRESS").transform;
            foreach (string name in new[] { "Sleeping A / Felix and Mila", "Sleeping B / Odette", "Investigation vestibule -6.5", "Investigation vestibule -19.5" })
            {
                var extra = train.Find(name);
                if (extra != null) UnityEngine.Object.DestroyImmediate(extra.gameObject);
            }
            foreach (var boundary in train.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Invisible rear safety barrier"))
                boundary.position = new Vector3(-6.15f, 2.3f, 0);
            tour.cutawayShells = tour.cutawayShells.Where(s => s != null).ToArray();
            foreach (var shell in tour.cutawayShells) shell.SetActive(true);
            foreach (var renderer in tour.foregroundScenery) if (renderer != null) renderer.enabled = true;
            foreach (var actor in UnityEngine.Object.FindObjectsByType<TrainActor>(FindObjectsSortMode.None))
            {
                if (actor.characterId == "Mila") actor.transform.position = new Vector3(-3.55f, 1.12f, 1.60f);
                if (actor.characterId == "Felix") actor.transform.position = new Vector3(13.45f, 1.12f, 2.12f);
                if (actor.characterId == "Bellamy") actor.transform.position = new Vector3(26.1f, 1.12f, 1.70f);
            }
            // Consolidate the source scene's evidence into the requested three existing carriages.
            foreach (var binding in UnityEngine.Object.FindObjectsByType<NarrativeInteraction>(FindObjectsSortMode.None))
            {
                if (string.IsNullOrEmpty(binding.clueId)) continue;
                Vector3 p = binding.transform.position;
                if (p.x < -19.5f) p.x += 52;
                else if (p.x < -6.5f) p.x += 26;
                binding.transform.position = p;
            }
            tour.roomNames = new[] { "01 / THE VELVET LOUNGE", "02 / THE DINING SALOON", "03 / THE PRIVATE SUITE" };
            tour.locationIds = new[] { "bar", "dining", "burrows" };
            foreach (Transform child in tour.transform.Cast<Transform>().ToArray())
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            tour.roomViews = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                var anchor = new GameObject("Carriage centre / " + tour.roomNames[i]).transform;
                anchor.SetParent(tour.transform); anchor.position = new Vector3(i * 13, 1.12f, 0);
                tour.roomViews[i] = anchor;
            }
            tour.exteriorView = tour.roomViews[0];
            foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
            var rig = tour.gameObject.AddComponent<TrainCameraRig>();
            tour.cameraRig = rig; rig.tour = tour;
            rig.leftCorners = new Camera[3]; rig.rightCorners = new Camera[3];
            var cameras = new GameObject("Seven perspective cameras").transform;
            for (int i = 0; i < 3; i++)
            {
                rig.leftCorners[i] = CameraAt(cameras, $"Cart {i + 1} / Left corner", new Vector3(i * 13 - 5.5f, 4.05f, -2.45f), new Vector3(i * 13 + 1.8f, 1.85f, -.3f));
                rig.rightCorners[i] = CameraAt(cameras, $"Cart {i + 1} / Right corner", new Vector3(i * 13 + 5.5f, 4.05f, -2.45f), new Vector3(i * 13 - 1.8f, 1.85f, -.3f));
            }
            rig.detectiveCamera = CameraAt(cameras, "Detective / Third person follow", new Vector3(-3, 3.7f, -.7f), new Vector3(0, 2.6f, -.7f));
            tour.name = "Train exploration / three carriage perspective";
            tour.exterior = false;
            tour.detective.gameObject.SetActive(true);
            tour.detective.transform.rotation = Quaternion.Euler(0, 90, 0);
            tour.SetView(0, true);
            rig.Refresh(true);
            AddInteriorCeilings();
            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            scenes.AddRange(EditorBuildSettings.scenes.Where(s => s.path != ScenePath && s.path != "Assets/Scenes/InterrogationMemory.unity"));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("THIRD_PERSON_SCENE_OK: Three carts, six fixed cameras, one detective camera and the shared investigation notebook.");
        }

        // The old cutaway roof mesh has outward faces only. Add a visible, collidable interior.
        public static void AddInteriorCeilings()
        {
            var train = GameObject.Find("THE CARNIVAL EXPRESS").transform;
            foreach (string name in new[] { "01 Velvet Lounge", "02 Dining Saloon", "03 Private Suite" })
            {
                var car = train.Find(name);
                if (car.Find("Interior ceiling") != null) continue;
                var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceiling.name = "Interior ceiling"; ceiling.transform.SetParent(car, false);
                ceiling.transform.localPosition = new Vector3(0, 4.53f, 0);
                ceiling.transform.localScale = new Vector3(11.8f, .08f, 5.65f);
                ceiling.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Train/Materials/Warm ivory.mat");
            }
        }

        public static void FinishInterior()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            AddInteriorCeilings();
            // Restore the source evidence depth after consolidating the extra carriages.
            foreach (var binding in UnityEngine.Object.FindObjectsByType<NarrativeInteraction>())
            {
                if (string.IsNullOrEmpty(binding.clueId)) continue;
                var p = binding.transform.position;
                switch (binding.clueId)
                {
                    case "C01": p.z = 1; break;
                    case "C02": case "C10": p.z = 2.3f; break;
                    case "C03": p.z = 1.1f; break;
                    case "C06": p.z = 1.3f; break;
                    case "C07": p.z = 1.6f; break;
                    default: p.z = 1.2f; break;
                }
                binding.transform.position = p;
            }
            EditorSceneManager.SaveScene(scene);
        }

        static Camera CameraAt(Transform parent, string name, Vector3 position, Vector3 target)
        {
            var camera = new GameObject(name, typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.transform.SetParent(parent); camera.transform.position = position; camera.transform.LookAt(target);
            camera.orthographic = false; camera.fieldOfView = 65; camera.nearClipPlane = .05f; camera.farClipPlane = 220;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.74f, .81f, .82f);
            camera.allowHDR = true; camera.allowMSAA = true;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            camera.enabled = false; camera.GetComponent<AudioListener>().enabled = false;
            return camera;
        }
    }
}
