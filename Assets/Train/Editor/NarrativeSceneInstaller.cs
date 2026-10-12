using System;
using System.Collections.Generic;
using System.Linq;
using GraduationDemo.Narrative;
using GraduationDemo.Train;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GraduationDemo.Editor
{
    public static class NarrativeSceneInstaller
    {
        const string ScenePath = "Assets/Scenes/CarnivalExpress.unity";
        [MenuItem("Graduation Demo/Narrative/Install Playable Investigation")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before installing narrative content.");
            if (SceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
            NarrativeImporter.Refresh();
            var tour = UnityEngine.Object.FindAnyObjectByType<TrainTour>();
            if (tour == null) throw new InvalidOperationException("CarnivalExpress scene has no TrainTour.");
            var train = GameObject.Find("THE CARNIVAL EXPRESS").transform;
            var narrative = tour.GetComponent<TrainNarrative>() ?? tour.gameObject.AddComponent<TrainNarrative>();
            narrative.catalogue = AssetDatabase.LoadAssetAtPath<NarrativeCatalogue>(NarrativeImporter.AssetPath);
            const string pacingPath = "Assets/Narrative/Data/InvestigationPacing.asset";
            var pacing = narrative.configuration != null ? narrative.configuration : AssetDatabase.LoadAssetAtPath<InvestigationConfiguration>(pacingPath);
            if (pacing == null) { pacing = ScriptableObject.CreateInstance<InvestigationConfiguration>(); AssetDatabase.CreateAsset(pacing, pacingPath); }
            narrative.configuration = pacing;
            if (tour.GetComponent<InvestigationNotebook>() == null) tour.gameObject.AddComponent<InvestigationNotebook>();
            narrative.developmentUnvoiced = true; tour.narrative = narrative;
            var suite = train.Find("03 Private Suite");
            var a = SleepingCar(train, suite, "Sleeping A / Felix and Mila", -13);
            var b = SleepingCar(train, suite, "Sleeping B / Odette", -26);
            var shells = new List<GameObject>(tour.cutawayShells.Where(x => x != null));
            foreach (var car in new[] { a, b })
                foreach (Transform child in car) if (child.name.StartsWith("Cutaway /") && !shells.Contains(child.gameObject)) shells.Add(child.gameObject);
            tour.cutawayShells = shells.ToArray();
            Bridge(train, -6.5f); Bridge(train, -19.5f);
            // The old rear boundary is now an interior vestibule. Keep its safety function at the new rear.
            foreach (var boundary in train.GetComponentsInChildren<Transform>(true).Where(x => x.name == "Invisible rear safety barrier"))
            {
                var position = boundary.position; position.x = -32.15f; boundary.position = position;
                PrefabUtility.RecordPrefabInstancePropertyModifications(boundary);
            }
            var views = new List<Transform>();
            foreach (var value in new[] { -26f, -13f, 0f, 13f, 26f })
            {
                var view = Child(tour.transform, "Investigation camera " + value);
                view.position = new Vector3(value, 5.65f, -16); view.rotation = Quaternion.Euler(12.5f, 0, 0); views.Add(view);
            }
            tour.roomViews = views.ToArray();
            tour.roomNames = new[] { "01 / ODETTE’S COMPARTMENT", "02 / FELIX AND MILA’S COMPARTMENTS", "03 / THE BAR AND LOUNGE", "04 / THE DINING SALOON", "05 / BURROWS’S COMPARTMENT" };
            tour.exteriorView.position = new Vector3(12, 42, -70); tour.exteriorView.LookAt(new Vector3(5, 1.5f, 0)); tour.exteriorSize = 35;
            InstallCast(tour);
            var content = Child(train, "Investigation evidence");
            Evidence(content, narrative.catalogue, "C01", new Vector3(-28.5f, 1.65f, 1.0f), PrimitiveType.Cube, new Vector3(.8f, .08f, .1f));
            Evidence(content, narrative.catalogue, "C02", new Vector3(-24.7f, 1.35f, 2.3f), PrimitiveType.Cylinder, new Vector3(.45f, .17f, .45f));
            var body = Evidence(content, narrative.catalogue, "C03", new Vector3(-26.7f, 1.3f, 1.1f), PrimitiveType.Capsule, new Vector3(.55f, .9f, .55f));
            body.rotation = Quaternion.Euler(0, 0, 90);
            Evidence(content, narrative.catalogue, "C04", new Vector3(-29.8f, 1.7f, 1.2f), PrimitiveType.Cube, new Vector3(.5f, .04f, .6f));
            Evidence(content, narrative.catalogue, "C05", new Vector3(-1.5f, 1.7f, 1.2f), PrimitiveType.Cube, new Vector3(.35f, .03f, .5f));
            Evidence(content, narrative.catalogue, "C06", new Vector3(-16.7f, 1.7f, 1.3f), PrimitiveType.Cube, new Vector3(.7f, .35f, .45f));
            Evidence(content, narrative.catalogue, "C07", new Vector3(-24.7f, 1.7f, 1.6f), PrimitiveType.Cube, new Vector3(1.2f, .2f, .65f));
            Evidence(content, narrative.catalogue, "C08", new Vector3(15.8f, 1.7f, 1.2f), PrimitiveType.Cylinder, new Vector3(.5f, .15f, .5f));
            Evidence(content, narrative.catalogue, "C09", new Vector3(-21.5f, 1.7f, 1.2f), PrimitiveType.Cube, new Vector3(.5f, .04f, .6f));
            // A distinct inspectable location marker describes the same clock, not a second physical clock.
            var clockPosition = Evidence(content, narrative.catalogue, "C10", new Vector3(-24.7f, 1.35f, 2.3f), PrimitiveType.Cube, new Vector3(.15f, .04f, .15f));
            clockPosition.GetComponent<Renderer>().enabled = false;
            tour.SetView(0, true); tour.SnapCamera();
            EditorUtility.SetDirty(tour); EditorUtility.SetDirty(narrative);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
            ValidateScene();
            Debug.Log("NARRATIVE_SCENE_OK: Five connected carriages, three suspect interviews, ten evidence bindings and opening/finale.");
        }
        static Transform Child(Transform parent, string name)
        {
            var found = DirectChild(parent, name); if (found != null) return found;
            var value = new GameObject(name).transform; value.SetParent(parent, false); return value;
        }
        static Transform SleepingCar(Transform train, Transform template, string name, float x)
        {
            var found = DirectChild(train, name); if (found != null) return found;
            var clone = UnityEngine.Object.Instantiate(template.gameObject, train); clone.name = name;
            if (PrefabUtility.IsPartOfPrefabInstance(clone)) PrefabUtility.UnpackPrefabInstance(clone, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            clone.transform.localPosition = new Vector3(x, 0, 0);
            var furniture = clone.transform.Find("Furniture and dressing");
            UnityEngine.Object.DestroyImmediate(furniture.gameObject);
            furniture = Child(clone.transform, "Compartment furniture");
            for (int side = -1; side <= 1; side += 2)
            {
                Prop(furniture, side < 0 ? "Felix / sleeping compartment" : "Mila / rehearsal compartment", PrimitiveType.Cube, new Vector3(side * 3.7f, 1.45f, 1.8f), new Vector3(2.6f, .65f, 1.3f));
                Prop(furniture, "Upholstered back " + side, PrimitiveType.Cube, new Vector3(side * 3.7f, 2.2f, 2.4f), new Vector3(2.6f, 1, .25f));
            }
            Prop(furniture, "Compartment partition", PrimitiveType.Cube, new Vector3(0, 2.1f, 1.8f), new Vector3(.12f, 2, 1.5f));
            foreach (var observation in clone.GetComponentsInChildren<TrainObservation>(true)) UnityEngine.Object.DestroyImmediate(observation);
            return clone.transform;
        }
        static void Bridge(Transform train, float x)
        {
            string name = "Investigation vestibule " + x; if (train.Find(name) != null) return;
            Prop(train, name, PrimitiveType.Cube, new Vector3(x, .97f, 0), new Vector3(1.4f, .26f, 1.8f));
        }
        static Transform Prop(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale)
        {
            var part = DirectChild(parent, name);
            if (part == null) { part = GameObject.CreatePrimitive(type).transform; part.name = name; part.SetParent(parent, false); }
            part.localPosition = position; part.localScale = scale;
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Train/Materials/Brushed brass.mat");
            if (part.GetComponent<Renderer>() != null) part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }
        static Transform DirectChild(Transform parent, string name) { foreach (Transform child in parent) if (child.name == name) return child; return null; }
        static Transform Evidence(Transform content, NarrativeCatalogue catalogue, string id, Vector3 position, PrimitiveType type, Vector3 scale)
        {
            var c = catalogue.Clue(id); var prop = Prop(content, id + " / " + c.name, type, position, scale);
            var collider = prop.GetComponent<Collider>(); if (collider != null) collider.enabled = false;
            var observation = prop.GetComponent<TrainObservation>() ?? prop.gameObject.AddComponent<TrainObservation>();
            observation.title = "Inspect " + c.name; observation.description = c.fact; observation.interactionRange = 3.1f;
            var binding = prop.GetComponent<NarrativeInteraction>() ?? prop.gameObject.AddComponent<NarrativeInteraction>(); binding.clueId = id;
            return prop;
        }
        static void InstallCast(TrainTour tour)
        {
            foreach (var actor in UnityEngine.Object.FindObjectsByType<TrainActor>(FindObjectsSortMode.None))
            {
                string id = actor.locomotion != null ? "Burrows" : actor.characterId;
                if (string.IsNullOrEmpty(id)) id = actor.role == "Bubbly passenger" ? "Mila" : actor.role == "City boy" ? "Felix" : actor.role == "Businessman" ? "Bellamy" : null;
                if (id == null) continue;
                actor.characterId = id; actor.role = id == "Burrows" ? "Basil Burrows" : id == "Mila" ? "Mila Novak" : id == "Felix" ? "Felix Mercer" : "Edgar Bellamy";
                // Keep object and prefab identities stable; readable identity comes from the component.
                if (id == "Burrows") { EditorUtility.SetDirty(actor); PrefabUtility.RecordPrefabInstancePropertyModifications(actor); continue; }
                actor.transform.position = id == "Mila" ? new Vector3(-9.3f, 1.12f, 1.65f) : id == "Felix" ? new Vector3(-3.55f, 1.12f, 1.60f) : new Vector3(-29.7f, 1.12f, 1.65f);
                var observation = actor.GetComponentInChildren<TrainObservation>();
                observation.title = "Talk to " + actor.role; observation.description = "Ask about the evening."; observation.topics = new string[0]; observation.responses = new string[0];
                var binding = observation.GetComponent<NarrativeInteraction>() ?? observation.gameObject.AddComponent<NarrativeInteraction>(); binding.suspectId = id;
                EditorUtility.SetDirty(actor); EditorUtility.SetDirty(observation);
                PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
                PrefabUtility.RecordPrefabInstancePropertyModifications(actor.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(observation);
            }
        }
        [MenuItem("Graduation Demo/Narrative/Validate Scene Bindings")]
        public static void ValidateScene()
        {
            var tour = UnityEngine.Object.FindAnyObjectByType<TrainTour>(); var narrative = tour == null ? null : tour.GetComponent<TrainNarrative>();
            if (narrative == null || narrative.catalogue == null) throw new InvalidOperationException("Missing TrainNarrative catalogue binding.");
            if (narrative.configuration == null || narrative.configuration.Errors().Count > 0 || tour.GetComponent<InvestigationNotebook>() == null) throw new InvalidOperationException("Missing/invalid investigation pacing or notebook component.");
            if (tour.viewCamera == null || tour.detective == null || tour.exteriorView == null || tour.cutawayShells.Any(x => x == null)) throw new InvalidOperationException("Missing train camera, detective or cutaway reference.");
            if (tour.roomViews.Length != 5 || tour.roomNames.Length != 5 || tour.roomViews.Any(x => x == null)) throw new InvalidOperationException("Missing five-carriage camera bindings.");
            var bindings = UnityEngine.Object.FindObjectsByType<NarrativeInteraction>(FindObjectsSortMode.None);
            foreach (var clue in narrative.catalogue.clues) if (bindings.Count(x => x.clueId == clue.id) != 1) throw new InvalidOperationException("Clue scene binding must be unique: " + clue.id);
            foreach (var suspect in new[] { "Felix", "Mila", "Bellamy" }) if (bindings.Count(x => x.suspectId == suspect) != 1) throw new InvalidOperationException("Suspect scene binding must be unique: " + suspect);
            var actors = UnityEngine.Object.FindObjectsByType<TrainActor>(FindObjectsSortMode.None);
            foreach (var id in new[] { "Burrows", "Felix", "Mila", "Bellamy" }) if (actors.Count(x => x.characterId == id) != 1) throw new InvalidOperationException("Character identity must be unique: " + id);
            foreach (var actor in actors) if (actor.bodyAnimator == null) throw new InvalidOperationException("Missing character animation reference: " + actor.role);
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0) throw new InvalidOperationException("Missing scene script: " + transform.name);
            foreach (var binding in bindings) if (binding.GetComponent<TrainObservation>() == null) throw new InvalidOperationException("Interaction has no TrainObservation: " + binding.name);
        }
    }
}
