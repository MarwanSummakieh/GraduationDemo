using System;
using System.Collections.Generic;
using GraduationDemo.Train;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GraduationDemo.Editor
{
    public static class TrainCharacterInstaller
    {
        const string Package = "Assets/Hodaart/CasualCharacterCollection3/";
        const string CastName = "Passengers / Casual Character Collection 3";

        [MenuItem("Graduation Demo/Seat Passengers and Set Side Camera")]
        public static void ApplySeatedLayout()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before editing the layout.");
            var tour = UnityEngine.Object.FindAnyObjectByType<TrainTour>();
            if (tour == null) throw new InvalidOperationException("Open CarnivalExpress.unity first.");
            ConfigureTour(tour);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            CarnivalTrainBuilder.ValidateAndCapture();
            Debug.Log("SEATED_TRAIN_LAYOUT_OK: Seated passengers, topic conversations and side-scrolling camera.");
        }

        [MenuItem("Graduation Demo/Install Package Characters")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before installing characters.");
            var tour = UnityEngine.Object.FindAnyObjectByType<TrainTour>();
            if (tour == null) throw new InvalidOperationException("Open CarnivalExpress.unity first.");
            Vector3 position = tour.detective.transform.position;
            Quaternion rotation = tour.detective.transform.rotation;
            var oldDetective = tour.detective.gameObject;
            tour.detective = CreateDetective();
            tour.detective.transform.SetPositionAndRotation(position, rotation);
            UnityEngine.Object.DestroyImmediate(oldDetective);
            CreateNpcs(tour);
            if (tour.GetComponent<TrainNarrative>() != null) NarrativeSceneInstaller.Install();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            CarnivalTrainBuilder.ValidateAndCapture();
            Debug.Log("TRAIN_CHARACTERS_INSTALLED: Package detective and three animated NPCs.");
        }

        public static CharacterController CreateDetective()
        {
            var actor = CreateActor("Detective", 6, new Vector3(0, 1.12f, -.7f), null);
            var controller = actor.gameObject.AddComponent<CharacterController>();
            controller.height = 1.85f; controller.radius = .23f; controller.center = new Vector3(0, .925f, 0); controller.stepOffset = .2f;
            actor.locomotion = controller;
            PrefabUtility.SaveAsPrefabAssetAndConnect(actor.gameObject, "Assets/Train/Prefabs/Detective.prefab", InteractionMode.AutomatedAction);
            return controller;
        }

        public static void CreateNpcs(TrainTour tour)
        {
            var old = GameObject.Find(CastName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var cast = new GameObject(CastName).transform;
            CreateNpc(cast, tour, "Bubbly passenger", 3, new Vector3(-3.55f, 1.12f, 1.60f), false,
                "Oh, a detective! What a marvellous travelling companion. I do hope the carnival is as colourful as this train.");
            CreateNpc(cast, tour, "City boy", 2, new Vector3(13.45f, 1.12f, 2.12f), false,
                "You're the detective? Nice. I'm just here for the carnival. And perhaps a second helping from the dining car.");
            CreateNpc(cast, tour, "Businessman", 4, new Vector3(26.1f, 1.12f, 1.70f), false,
                "Good afternoon, detective. I trust we can reach the carnival with a minimum of interruptions.");
            ConfigureTour(tour);
        }

        public static void ConfigureTour(TrainTour tour)
        {
            if (tour.GetComponent<TrainNarrative>() != null) return;
            EnsureBenches();
            foreach (var actor in UnityEngine.Object.FindObjectsByType<TrainActor>())
            {
                if (actor.locomotion != null) continue;
                actor.seated = true; actor.seatHeight = .60f; actor.secondaryIdle = false;
                actor.transform.rotation = Quaternion.Euler(0, 180, 0);
                actor.detective = tour.detective.transform;
                int role = actor.role == "Bubbly passenger" ? 0 : actor.role == "City boy" ? 1 : 2;
                actor.transform.position = new[] { new Vector3(-3.55f, 1.12f, 1.60f), new Vector3(13.45f, 1.12f, 2.12f), new Vector3(26.1f, 1.12f, 1.70f) }[role];
                PoseForEditor(actor.bodyAnimator.gameObject);
                actor.ApplySeatedPose();
                var collider = actor.GetComponent<CapsuleCollider>();
                collider.height = 1.5f; collider.center = Vector3.up * .75f;
                var item = actor.GetComponentInChildren<TrainObservation>();
                item.description = role == 0 ? "Oh, a detective! Please, come over. This is an excellent bench for watching the world go by." : role == 1 ? "Hey, detective. Best seat in the dining car. What do you want to know?" : "Good afternoon, detective. I can spare a moment. What would you like to discuss?";
                item.interactionRange = 3.25f;
                item.topics = new[] { "Introduce yourself", "The carnival", "This carriage" };
                item.responses = role == 0 ? new[] {
                    "I came for a little adventure and a great deal of spectacle. A train full of colourful strangers is a splendid start, wouldn't you say?",
                    "The music, the costumes, the impossible hats! I intend to see everything. Twice, if the programme allows it.",
                    "The velvet lounge is my favourite. A comfortable seat, a warm cup, and the countryside doing all the rushing." }
                    : role == 1 ? new[] {
                    "Just a city boy taking a break. No grand title. You can call me the passenger with excellent taste in seats.",
                    "I'm going for the games and the food. Mostly the food. Please don't put that in an official report.",
                    "This booth is perfect. Close to the service counter, far from anyone who expects me to look busy." }
                    : new[] {
                    "I prefer a quiet journey and a well-kept schedule. A comfortable compartment is a welcome bonus.",
                    "An extravagant occasion, by all accounts. I shall reserve judgment until I have seen it for myself.",
                    "The suite offers a little peace. You are welcome to ask your questions, provided we keep the conversation civil." };
                PrefabUtility.SaveAsPrefabAssetAndConnect(actor.gameObject, "Assets/Train/Prefabs/" + actor.role + ".prefab", InteractionMode.AutomatedAction);
                actor.detective = tour.detective.transform;
            }
            for (int i = 0; i < tour.roomViews.Length; i++)
            {
                tour.roomViews[i].position = new Vector3(i * 13, 5.65f, -16);
                tour.roomViews[i].LookAt(new Vector3(i * 13, 2.15f, 0));
            }
            tour.interiorSize = 4.1f;
            var scenery = GameObject.Find("Travelling countryside/Passing trees");
            var foreground = new List<Renderer>();
            if (scenery != null) foreach (Transform tree in scenery.transform)
                if (tree.position.z < -5) foreground.AddRange(tree.GetComponentsInChildren<Renderer>());
            tour.foregroundScenery = foreground.ToArray();
            tour.SetView(0, false); tour.SnapCamera();
        }

        static void EnsureBenches()
        {
            var train = GameObject.Find("THE CARNIVAL EXPRESS");
            foreach (var child in train.GetComponentsInChildren<Transform>(true))
                if (child.name == "Velvet seat")
                {
                    Vector3 position = child.localPosition; position.y = .37f; child.localPosition = position;
                    Vector3 scale = child.localScale; scale.y = .2f; child.localScale = scale;
                }
            Transform dining = train.transform.Find("02 Dining Saloon/Furniture and dressing");
            if (dining.Find("Conversation banquette") != null) return;
            var bench = new GameObject("Conversation banquette").transform; bench.SetParent(dining, false); bench.localPosition = new Vector3(0, 1.12f, 2.22f);
            Material teal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Train/Materials/Petrol enamel.mat");
            Material gold = AssetDatabase.LoadAssetAtPath<Material>("Assets/Train/Materials/Brushed brass.mat");
            SeatPart(bench, "Brass plinth", new Vector3(0, .18f, 0), new Vector3(2.3f, .2f, .9f), gold);
            SeatPart(bench, "Velvet seat", new Vector3(0, .37f, 0), new Vector3(2.3f, .2f, .9f), teal);
            SeatPart(bench, "Upholstered back", new Vector3(0, .95f, .35f), new Vector3(2.3f, .9f, .2f), teal);
            foreach (int side in new[] { -1, 1 }) SeatPart(bench, "Armrest", new Vector3(side * 1.13f, .65f, 0), new Vector3(.15f, .5f, .9f), teal);
        }

        static void SeatPart(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube); part.name = name; part.transform.SetParent(parent, false); part.transform.localPosition = position; part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        static void CreateNpc(Transform parent, TrainTour tour, string role, int character, Vector3 position, bool alternateIdle, string greeting)
        {
            var actor = CreateActor(role, character, position, parent);
            actor.detective = tour.detective.transform; actor.secondaryIdle = alternateIdle;
            actor.bodyAnimator.SetBool("Idle 2", alternateIdle);
            var collider = actor.gameObject.AddComponent<CapsuleCollider>(); collider.height = 1.85f; collider.center = Vector3.up * .925f; collider.radius = .28f;
            var interaction = new GameObject("Conversation / " + role).AddComponent<TrainObservation>();
            interaction.transform.SetParent(actor.transform, false); interaction.transform.localPosition = Vector3.up;
            interaction.title = "Talk to the " + role.ToLowerInvariant(); interaction.description = role + "\n\n\"" + greeting + "\""; interaction.speaker = actor;
            PrefabUtility.SaveAsPrefabAssetAndConnect(actor.gameObject, "Assets/Train/Prefabs/" + role + ".prefab", InteractionMode.AutomatedAction);
            // Keep the reusable NPC prefab independent of the detective in this scene.
            actor.detective = tour.detective.transform;
        }

        static TrainActor CreateActor(string role, int number, Vector3 position, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Package + "Prefabs/Characters/Character " + number.ToString("00") + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Missing installed character " + number);
            var root = new GameObject(role); root.transform.SetParent(parent, false); root.transform.position = position; root.transform.rotation = Quaternion.Euler(0, 180, 0);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            model.name = "Character " + number.ToString("00") + " / package model";
            var actor = root.AddComponent<TrainActor>(); actor.role = role; actor.bodyAnimator = model.GetComponent<Animator>();
            foreach (var animator in model.GetComponentsInChildren<Animator>(true))
            {
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (animator != actor.bodyAnimator) actor.facialAnimator = animator;
            }
            PoseForEditor(model);
            // Use the actual deformed vertices rather than padded skinning bounds to align feet.
            Bounds bounds = default; bool first = true;
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var baked = new Mesh(); renderer.BakeMesh(baked);
                foreach (Vector3 vertex in baked.vertices)
                {
                    Vector3 point = root.transform.InverseTransformPoint(renderer.transform.TransformPoint(vertex));
                    if (first) { bounds = new Bounds(point, Vector3.zero); first = false; } else bounds.Encapsulate(point);
                }
                UnityEngine.Object.DestroyImmediate(baked);
            }
            if (first || bounds.size.y < .1f) throw new InvalidOperationException("Character mesh is empty: " + role);
            float scale = 1.85f / bounds.size.y;
            model.transform.localScale *= scale; model.transform.localPosition = Vector3.up * (-bounds.min.y * scale);
            return actor;
        }

        static void PoseForEditor(GameObject model)
        {
            // Save an idle pose in the scene so characters also look natural outside Play mode.
            if (AnimationMode.InAnimationMode()) return;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Package + "Animations/Characters_Animations/A_Idle A.anim");
            var bones = model.GetComponentsInChildren<Transform>(true);
            var positions = new Vector3[bones.Length]; var rotations = new Quaternion[bones.Length];
            AnimationMode.StartAnimationMode();
            try
            {
                AnimationMode.BeginSampling(); AnimationMode.SampleAnimationClip(model, clip, .2f); AnimationMode.EndSampling();
                for (int i = 0; i < bones.Length; i++) { positions[i] = bones[i].localPosition; rotations[i] = bones[i].localRotation; }
            }
            finally { AnimationMode.StopAnimationMode(); }
            for (int i = 1; i < bones.Length; i++) { bones[i].localPosition = positions[i]; bones[i].localRotation = rotations[i]; }
        }
    }
}
