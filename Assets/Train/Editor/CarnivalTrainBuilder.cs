using System;
using System.Collections.Generic;
using System.IO;
using GraduationDemo.Train;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace GraduationDemo.Editor
{
    /// <summary>Builds authored, editable assets. No geometry is generated during gameplay.</summary>
    public static class CarnivalTrainBuilder
    {
        const string Root = "Assets/Train";
        const string ScenePath = "Assets/Scenes/CarnivalExpress.unity";
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        static readonly List<GameObject> Shells = new List<GameObject>();
        static Material wine, teal, cream, gold, wood, dark, glass, pink, carpet, glow, paper, green;
        static Transform train;

        [MenuItem("Graduation Demo/Build Carnival Express")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before rebuilding the train.");
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Meshes");
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory("Documentation/TrainPreviews");
            AssetDatabase.Refresh();
            Materials.Clear(); Shells.Clear();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            wine = Mat("Oxblood lacquer", "672E43", .32f, .25f);
            teal = Mat("Petrol enamel", "23575B", .25f, .25f);
            cream = Mat("Warm ivory", "EDD9B6");
            gold = Mat("Brushed brass", "C69A55", .65f, .45f);
            wood = Mat("Walnut", "674735");
            dark = Mat("Charcoal iron", "26363C", .5f, .2f);
            glass = Mat("Evening window glass", "759FA4", .15f, .5f, .22f);
            pink = Mat("Dusty rose velvet", "B86676");
            carpet = Mat("Burgundy runner", "803C50");
            glow = Mat("Lamplight", "FFE1A0", 0, .1f, 1.4f);
            paper = Mat("Stationery", "FFF0CE");
            green = Mat("Sage foliage", "597567");
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("A5BBC1");
            RenderSettings.ambientEquatorColor = Hex("9C9493");
            RenderSettings.ambientGroundColor = Hex("4D555E");
            RenderSettings.ambientIntensity = 1;
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            var sun = new GameObject("Late afternoon / soft key").AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = Hex("FFE3BB"); sun.intensity = 1.8f;
            sun.transform.rotation = Quaternion.Euler(42, -30, 0); sun.shadows = LightShadows.Soft;
            var fill = new GameObject("Cool sky / fill").AddComponent<Light>();
            fill.type = LightType.Directional; fill.color = Hex("A6CED9"); fill.intensity = .65f;
            fill.transform.rotation = Quaternion.Euler(30, 145, 0);

            train = Group("THE CARNIVAL EXPRESS", null);
            for (int i = 0; i < 3; i++) Carriage(i);
            Locomotive();
            for (int i = 0; i < 3; i++) Gangway(i * 13 + 6.5f);
            World();

            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 4.1f;
            camera.nearClipPlane = .1f; camera.farClipPlane = 220;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Hex("BDCED0");
            camera.allowHDR = true; camera.allowMSAA = true;
            camera.gameObject.AddComponent<AudioListener>();
            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var volume = new GameObject("Colour and lamplight").AddComponent<Volume>();
            volume.isGlobal = true;
            string profilePath = Root + "/TrainAtmosphere.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, profilePath); }
            foreach (var component in profile.components.ToArray()) { profile.Remove(component.GetType()); UnityEngine.Object.DestroyImmediate(component, true); }
            var bloom = profile.Add<Bloom>(); bloom.intensity.Override(.18f); bloom.threshold.Override(1.1f);
            var tonemapping = profile.Add<Tonemapping>(); tonemapping.mode.Override(TonemappingMode.Neutral);
            volume.sharedProfile = profile;

            var tour = new GameObject("Train exploration / fixed camera").AddComponent<TrainTour>();
            tour.viewCamera = camera; tour.cutawayShells = Shells.ToArray();
            tour.roomViews = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                var view = Group("Camera composition / " + tour.roomNames[i], tour.transform);
                view.position = new Vector3(i * 13, 5.65f, -16);
                view.LookAt(new Vector3(i * 13, 2.15f, 0)); tour.roomViews[i] = view;
            }
            tour.exteriorView = Group("Camera composition / exterior", tour.transform);
            tour.exteriorView.position = new Vector3(45, 30, -47);
            tour.exteriorView.LookAt(new Vector3(17, 1.3f, 0));
            tour.detective = Detective();
            TrainCharacterInstaller.CreateNpcs(tour);
            camera.transform.SetPositionAndRotation(tour.roomViews[0].position, tour.roomViews[0].rotation);

            // Save the complete enclosed train as a reusable prefab, then use its instance in the scene.
            foreach (var shell in Shells) shell.SetActive(true);
            PrefabUtility.SaveAsPrefabAssetAndConnect(train.gameObject, Root + "/Prefabs/CarnivalExpress.prefab", InteractionMode.AutomatedAction);
            foreach (var shell in Shells) shell.SetActive(false);
            EditorSceneManager.SaveScene(scene, ScenePath);
            var buildScenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var existing in EditorBuildSettings.scenes) if (existing.path != ScenePath) buildScenes.Add(existing);
            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
            ValidateAndCapture();
            Selection.activeGameObject = train.gameObject;
            Debug.Log("CARNIVAL_TRAIN_BUILD_OK: Editable train, prefab, scene, constrained exploration and previews created.");
        }

        static void Carriage(int index)
        {
            float x = index * 13;
            var car = Group(new[] { "01 Velvet Lounge", "02 Dining Saloon", "03 Private Suite" }[index], train);
            car.localPosition = new Vector3(x, 0, 0);
            var structure = Group("Structure and running gear", car);
            Box("Underframe", structure, new Vector3(0, .45f, 0), new Vector3(12.1f, .4f, 5.9f), dark);
            Box("Parquet floor", structure, new Vector3(0, .96f, 0), new Vector3(12, .24f, 5.7f), wood);
            for (int p = 0; p < 30; p++)
                Box("Floorboard seam", structure, new Vector3(-5.8f + p * .4f, 1.086f, 0), new Vector3(.018f, .008f, 5.6f), dark, false);
            Box("Aisle runner", structure, new Vector3(0, 1.10f, -.5f), new Vector3(11.8f, .025f, 1.35f), carpet, false);
            for (int s = -1; s <= 1; s += 2)
            {
                Box("Runner gold border", structure, new Vector3(0, 1.12f, -.5f + s * .61f), new Vector3(11.7f, .012f, .035f), gold, false);
                for (int b = -1; b <= 1; b += 2)
                {
                    Box("Bogie", structure, new Vector3(b * 3.8f, .15f, 0), new Vector3(2.3f, .4f, 4.6f), dark);
                    for (int w = -1; w <= 1; w += 2)
                    {
                        var wheel = Cylinder("Flanged wheel", structure, new Vector3(b * 3.8f + w * .75f, .0f, s * 2.4f), new Vector3(.93f, .15f, .93f), dark);
                        wheel.localRotation = Quaternion.Euler(90, 0, 0);
                        var hub = Cylinder("Brass axle cap", structure, wheel.localPosition + new Vector3(0, 0, s * .17f), new Vector3(.25f, .03f, .25f), gold);
                        hub.localRotation = wheel.localRotation;
                    }
                }
            }
            var back = Group("Window wall / rear", car);
            Wall(back, 1, index);
            var front = Group("Cutaway / near window wall", car);
            Wall(front, -1, index); Shells.Add(front.gameObject);
            var roof = Group("Cutaway / barrel roof", car);
            var roofObject = new GameObject("Curved teal roof", typeof(MeshFilter), typeof(MeshRenderer));
            roofObject.transform.SetParent(roof, false);
            roofObject.GetComponent<MeshFilter>().sharedMesh = RoofMesh(); roofObject.GetComponent<MeshRenderer>().sharedMaterial = teal;
            for (int end = -1; end <= 1; end += 2)
            {
                Box("Roof brass rim", roof, new Vector3(end * 6, 4.53f, 0), new Vector3(.09f, .11f, 5.95f), gold, false);
                var cap = Cylinder("Roof ventilator", roof, new Vector3(end * 3.6f, 5.38f, 0), new Vector3(.5f, .1f, .5f), gold, false);
            }
            Shells.Add(roof.gameObject);
            // Open vestibules; the continuous route is deliberately never gated.
            for (int end = -1; end <= 1; end += 2)
            {
                var wall = Group("Open vestibule " + end, car);
                for (int side = -1; side <= 1; side += 2)
                    Box("End wall", wall, new Vector3(end * 5.95f, 2.65f, side * 1.9f), new Vector3(.16f, 3.1f, 1.9f), teal);
                Box("Door lintel", wall, new Vector3(end * 5.95f, 4.03f, 0), new Vector3(.2f, .4f, 1.9f), cream);
                for (int side = -1; side <= 1; side += 2)
                    Box("Brass doorway trim", wall, new Vector3(end * 5.83f, 2.65f, side * .91f), new Vector3(.05f, 3.0f, .07f), gold, false);
            }
            var interior = Group("Furniture and dressing", car);
            if (index == 0) Lounge(interior);
            if (index == 1) Dining(interior);
            if (index == 2) Suite(interior);
            for (int j = -1; j <= 1; j++)
            {
                Pendant(interior, j * 3.6f, 0);
                // Hanging details stay above the player's head in the cutaway.
                for (int k = 0; j != 0 && k < 8; k++)
                    Pennant(interior, new Vector3(j * 3.6f - 1.5f + k * .4f, 4.0f + .14f * Mathf.Abs(k - 3.5f), 2.55f), k % 2 == 0 ? pink : gold);
            }
            Sign(interior, new Vector3(0, 3.88f, 2.71f), new[] { "LE SALON", "LE RESTAURANT", "LA SUITE" }[index], .28f, gold);
            Sign(front, new Vector3(-2.3f, 2.2f, -3.01f), "CARNIVAL EXPRESS  /  0" + (index + 1), .21f, gold);
        }

        static void Wall(Transform parent, int side, int index)
        {
            Material paint = index == 1 ? wine : teal;
            Box("Lacquered lower body", parent, new Vector3(0, 1.55f, side * 2.9f), new Vector3(12, 1.0f, .16f), paint);
            Box("Ivory frieze", parent, new Vector3(0, 4.13f, side * 2.9f), new Vector3(12, .75f, .16f), cream);
            foreach (float y in new[] { 1.15f, 1.95f, 3.75f, 4.48f })
                Box("Continuous brass moulding", parent, new Vector3(0, y, side * 2.99f), new Vector3(12.08f, .055f, .055f), gold, false);
            for (int i = 0; i < 6; i++)
            {
                float x = -5 + i * 2;
                Box("Window pier", parent, new Vector3(x - .96f, 2.9f, side * 2.9f), new Vector3(.15f, 1.8f, .18f), cream);
                Box("Evening window", parent, new Vector3(x, 2.88f, side * 2.92f), new Vector3(1.7f, 1.5f, .045f), glass, false);
                foreach (int s in new[] { -1, 1 })
                {
                    Box("Window frame", parent, new Vector3(x + s * .84f, 2.88f, side * 2.85f), new Vector3(.055f, 1.58f, .08f), gold, false);
                    Box("Curtain", parent, new Vector3(x + s * .67f, 2.95f, side * 2.69f), new Vector3(.25f, 1.55f, .15f), pink, false);
                    Box("Curtain tie", parent, new Vector3(x + s * .67f, 2.5f, side * 2.58f), new Vector3(.28f, .07f, .08f), gold, false);
                    Box("Window top / sill", parent, new Vector3(x, 2.88f + s * .76f, side * 2.84f), new Vector3(1.75f, .06f, .1f), gold, false);
                }
                Box("Window mullion", parent, new Vector3(x, 2.9f, side * 2.82f), new Vector3(.045f, 1.5f, .06f), gold, false);
                Box("Inset lower panel", parent, new Vector3(x, 1.55f, side * 2.8f), new Vector3(1.6f, .55f, .04f), wood, false);
            }
        }

        static void Lounge(Transform p)
        {
            for (int i = -1; i <= 1; i++)
            {
                Sofa(p, new Vector3(i * 3.55f, 1.12f, 1.7f), 2.6f, pink);
                Table(p, new Vector3(i * 3.55f, 1.12f, .4f), 1.7f, .75f, wood);
                Cup(p, new Vector3(i * 3.55f + .35f, 2.03f, .4f));
                if (i != 0) Vase(p, new Vector3(i * 3.55f - .35f, 2.03f, .4f));
            }
            Suitcase(p, new Vector3(-4.7f, 1.12f, -2.0f), wine);
            Suitcase(p, new Vector3(-4.55f, 1.67f, -2.0f), wood);
            Plant(p, new Vector3(4.8f, 1.12f, 2.0f));
            var invite = Box("Carnival invitation", p, new Vector3(0, 2.04f, .37f), new Vector3(.38f, .02f, .28f), paper, false);
            invite.localRotation = Quaternion.Euler(0, 15, 0);
            Inspect(invite, "Carnival invitation", "An embossed invitation to the carnival. Even the envelope is dressed for the occasion.");
            Mask(p, new Vector3(-3.6f, 3.1f, 2.63f), "Fox", gold);
            Mask(p, new Vector3(3.6f, 3.1f, 2.63f), "Rabbit", cream);
        }

        static void Dining(Transform p)
        {
            for (int i = -1; i <= 1; i++)
            {
                float x = i * 3.5f;
                Table(p, new Vector3(x, 1.12f, 1.25f), 1.9f, 1.65f, cream);
                Box("Rose table runner", p, new Vector3(x, 2.025f, 1.25f), new Vector3(.42f, .025f, 1.68f), pink, false);
                foreach (int side in new[] { -1, 1 })
                {
                    var chair = Group("Dining chair", p); chair.localPosition = new Vector3(x + side * 1.25f, 1.12f, 1.25f);
                    chair.localRotation = Quaternion.Euler(0, side * -90, 0);
                    Chair(chair);
                    Cylinder("Porcelain plate", p, new Vector3(x + side * .54f, 2.04f, 1.25f), new Vector3(.48f, .018f, .48f), paper, false);
                    Cup(p, new Vector3(x + side * .55f, 2.05f, 1.7f));
                    Box("Brass cutlery", p, new Vector3(x + side * .55f, 2.05f, .88f), new Vector3(.32f, .015f, .035f), gold, false);
                }
                Vase(p, new Vector3(x, 2.05f, 1.25f));
            }
            Box("Service cabinet", p, new Vector3(4.85f, 1.65f, -2.0f), new Vector3(1.2f, 1.1f, 1.1f), teal);
            Box("Service marble top", p, new Vector3(4.85f, 2.24f, -2.0f), new Vector3(1.3f, .1f, 1.2f), cream);
            var menu = Box("Printed menu", p, new Vector3(4.8f, 2.31f, -1.75f), new Vector3(.38f, .03f, .5f), paper, false);
            Inspect(menu, "Dining menu", "Tonight's menu promises a grand supper before the carnival. The dining saloon has a flair for ceremony.");
            Plant(p, new Vector3(-5, 1.12f, -2.0f));
        }

        static void Suite(Transform p)
        {
            Box("Bed base", p, new Vector3(3.6f, 1.45f, 1.25f), new Vector3(3.4f, .65f, 2.2f), wood);
            Box("Ivory mattress", p, new Vector3(3.6f, 1.88f, 1.25f), new Vector3(3.35f, .22f, 2.15f), cream);
            Box("Velvet coverlet", p, new Vector3(3.3f, 2.03f, 1.25f), new Vector3(2.6f, .13f, 2.2f), wine);
            Box("Upholstered headboard", p, new Vector3(5.25f, 2.3f, 1.25f), new Vector3(.18f, 2.0f, 2.4f), pink);
            Box("Pillow", p, new Vector3(4.8f, 2.1f, 1.25f), new Vector3(.65f, .19f, 1.55f), paper, false);
            for (int j = 0; j < 4; j++) Sphere("Headboard button", p, new Vector3(5.13f, 2.7f, .45f + j * .53f), Vector3.one * .09f, gold, false);
            Table(p, new Vector3(-3.6f, 1.12f, 1.9f), 2.7f, 1.15f, wood);
            var chair = Group("Writing chair", p); chair.localPosition = new Vector3(-3.6f, 1.12f, .8f); chair.localRotation = Quaternion.Euler(0, 180, 0); Chair(chair);
            var letter = Box("Unwritten correspondence", p, new Vector3(-3.7f, 2.04f, 1.8f), new Vector3(.5f, .02f, .38f), paper, false);
            Inspect(letter, "Writing desk", "A private writing desk, ready for correspondence. What happened in this compartment remains for the case's writer to decide.");
            Cylinder("Ink bottle", p, new Vector3(-3.0f, 2.13f, 1.9f), new Vector3(.16f, .09f, .16f), teal, false);
            Suitcase(p, new Vector3(-4.8f, 1.12f, -2.1f), wine);
            Cylinder("Hatbox", p, new Vector3(-4.8f, 1.95f, -2.1f), new Vector3(.85f, .25f, .85f), pink);
            Cylinder("Hatbox lid", p, new Vector3(-4.8f, 2.23f, -2.1f), new Vector3(.9f, .035f, .9f), gold, false);
            Sofa(p, new Vector3(.1f, 1.12f, 1.8f), 2.0f, teal);
            Mask(p, new Vector3(.1f, 3.15f, 2.6f), "Owl", cream);
            Plant(p, new Vector3(4.9f, 1.12f, -2));
        }

        static void Locomotive()
        {
            var p = Group("04 Brass and petrol locomotive", train); p.localPosition = new Vector3(39, 0, 0);
            Box("Locomotive chassis", p, new Vector3(0, .6f, 0), new Vector3(12, .45f, 5.4f), dark);
            Box("Footplate", p, new Vector3(0, 1, 0), new Vector3(11.6f, .2f, 5.9f), gold);
            var boiler = Cylinder("Enamel boiler", p, new Vector3(1.5f, 2.5f, 0), new Vector3(3.1f, 3.25f, 3.1f), teal);
            boiler.localRotation = Quaternion.Euler(0, 0, 90);
            for (int i = 0; i < 4; i++)
            {
                var band = Cylinder("Boiler brass band", p, new Vector3(-.8f + i * 1.75f, 2.5f, 0), new Vector3(3.16f, .06f, 3.16f), gold, false);
                band.localRotation = boiler.localRotation;
            }
            var face = Cylinder("Smokebox face", p, new Vector3(4.8f, 2.5f, 0), new Vector3(3.18f, .12f, 3.18f), dark);
            face.localRotation = boiler.localRotation;
            Cylinder("Chimney", p, new Vector3(3.2f, 4.3f, 0), new Vector3(.85f, .8f, .85f), dark);
            Cylinder("Chimney crown", p, new Vector3(3.2f, 5.1f, 0), new Vector3(1.25f, .13f, 1.25f), gold);
            Cylinder("Steam dome", p, new Vector3(.1f, 4.05f, 0), new Vector3(1.0f, .38f, 1), gold);
            Sphere("Dome cap", p, new Vector3(.1f, 4.4f, 0), new Vector3(1, .35f, 1), gold);
            Box("Cab lower body", p, new Vector3(-4.1f, 1.9f, 0), new Vector3(3.2f, 1.65f, 5.4f), wine);
            Box("Cab roof", p, new Vector3(-4.1f, 4.7f, 0), new Vector3(3.7f, .24f, 5.9f), teal);
            foreach (int s in new[] { -1, 1 })
            {
                Box("Cab window", p, new Vector3(-4.1f, 3.5f, s * 2.65f), new Vector3(2.6f, 1.6f, .12f), glass);
                foreach (float x in new[] { -5.6f, -2.6f }) Box("Cab pillar", p, new Vector3(x, 3.5f, s * 2.65f), new Vector3(.18f, 2.2f, .2f), gold);
                for (int i = 0; i < 3; i++)
                {
                    var wheel = Cylinder("Driving wheel", p, new Vector3(-.8f + i * 2, .1f, s * 2.55f), new Vector3(1.7f, .16f, 1.7f), wine);
                    wheel.localRotation = Quaternion.Euler(90, 0, 0);
                    var hub = Cylinder("Drive hub", p, wheel.localPosition + Vector3.forward * s * .18f, new Vector3(.4f, .06f, .4f), gold);
                    hub.localRotation = wheel.localRotation;
                    for (int spoke = 0; spoke < 8; spoke++)
                    {
                        var bar = Box("Wheel spoke", p, wheel.localPosition + Vector3.forward * s * .17f, new Vector3(.07f, 1.5f, .06f), gold, false);
                        bar.localRotation = Quaternion.Euler(0, 0, spoke * 22.5f);
                    }
                }
                Box("Connecting rod", p, new Vector3(1.2f, .1f, s * 2.83f), new Vector3(4.35f, .14f, .12f), gold, false);
                var wheelSmall = Cylinder("Cab wheel", p, new Vector3(-4.3f, .05f, s * 2.45f), new Vector3(1, .14f, 1), dark);
                wheelSmall.localRotation = Quaternion.Euler(90, 0, 0);
            }
            var headlamp = Cylinder("Headlamp brass housing", p, new Vector3(5.0f, 3.4f, 0), new Vector3(.75f, .2f, .75f), gold);
            headlamp.localRotation = Quaternion.Euler(0, 0, 90);
            var lens = Cylinder("Headlamp lens", p, new Vector3(5.23f, 3.4f, 0), new Vector3(.57f, .025f, .57f), glow, false);
            lens.localRotation = headlamp.localRotation;
            for (int i = -3; i <= 3; i++)
            {
                var slat = Box("Cowcatcher rib", p, new Vector3(5.75f, 1.0f, i * .7f), new Vector3(.16f, 1.4f, .12f), gold);
                slat.localRotation = Quaternion.Euler(0, 0, -35);
            }
            var steam = new GameObject("Soft steam", typeof(ParticleSystem)); steam.transform.SetParent(p, false); steam.transform.localPosition = new Vector3(3.2f, 5.2f, 0);
            var particles = steam.GetComponent<ParticleSystem>(); var main = particles.main;
            main.startLifetime = 3.5f; main.startSpeed = 1.1f; main.startSize = 1.3f; main.startColor = new Color(.87f, .88f, .84f, .25f); main.maxParticles = 35;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission; emission.rateOverTime = 5;
            var velocity = particles.velocityOverLifetime; velocity.enabled = true; velocity.x = -2; velocity.y = 1.0f; velocity.space = ParticleSystemSimulationSpace.World;
            var size = particles.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .4f, 1, 2));
            var fade = particles.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Hex("DEE1DA"), 0), new GradientColorKey(Hex("DEE1DA"), 1) }, new[] { new GradientAlphaKey(.2f, 0), new GradientAlphaKey(0, 1) }); fade.color = gradient;
            var smokeMat = Mat("Steam", "DEE1DA"); smokeMat.shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            smokeMat.SetFloat("_Surface", 1); smokeMat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); smokeMat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); smokeMat.SetFloat("_ZWrite", 0); smokeMat.renderQueue = 3000;
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = smokeMat;
        }

        static void Gangway(float x)
        {
            var p = Group("Open connecting vestibule", train);
            Box("Walkable bridge", p, new Vector3(x, .97f, 0), new Vector3(1.4f, .26f, 1.8f), dark);
            for (int s = -1; s <= 1; s += 2)
            {
                Box("Gangway guard", p, new Vector3(x, 1.8f, s * .94f), new Vector3(1.4f, .1f, .08f), gold);
                Box("Gangway lower rail", p, new Vector3(x, 1.45f, s * .94f), new Vector3(1.4f, .08f, .08f), dark);
                for (int j = -1; j <= 1; j += 2) Box("Guard post", p, new Vector3(x + j * .55f, 1.45f, s * .94f), new Vector3(.08f, .9f, .08f), gold);
            }
        }

        static CharacterController Detective()
        {
            return TrainCharacterInstaller.CreateDetective();
        }

        static void World()
        {
            var p = Group("Travelling countryside", null);
            var ground = Mat("Sage countryside", "859D85");
            Box("Ground", p, new Vector3(15, -1.1f, 0), new Vector3(210, .3f, 180), ground);
            Box("Track ballast", p, new Vector3(15, -.85f, 0), new Vector3(160, .24f, 6.8f), Mat("Track gravel", "777B78"));
            foreach (int s in new[] { -1, 1 }) Box("Steel rail", p, new Vector3(15, -.52f, s * 1.95f), new Vector3(160, .16f, .12f), dark);
            var sleepers = Group("Moving sleepers", p); sleepers.gameObject.AddComponent<PassingLandscape>().speed = 5;
            for (int i = 0; i < 68; i++) Box("Sleeper", sleepers, new Vector3(-30 + i * 1.5f, -.65f, 0), new Vector3(.24f, .14f, 5), wood);
            var trees = Group("Passing trees", p); trees.gameObject.AddComponent<PassingLandscape>().speed = 5;
            for (int i = 0; i < 24; i++)
            {
                var tree = Group("Poplar " + i, trees); tree.position = new Vector3(-28 + i * 4.25f, -.8f, i % 2 == 0 ? 10 + i % 4 : -14 - i % 3);
                Cylinder("Trunk", tree, new Vector3(0, 1.15f, 0), new Vector3(.25f, 1.2f, .25f), wood, false);
                Sphere("Faceted crown", tree, new Vector3(0, 3, 0), new Vector3(1.8f, 4.8f, 1.8f), green, false);
            }
            var hills = Mat("Distant blue hills", "77979C");
            for (int i = 0; i < 11; i++) Sphere("Rolling distant hill", p, new Vector3(-65 + i * 16, -2, 37 + i % 3 * 6), new Vector3(30, 16 + i % 3 * 7, 21), hills, false);
            var carnival = Group("Carnival on the horizon", p); carnival.position = new Vector3(35, -.9f, 25);
            // A silhouette of the destination: a striped tent and a little Ferris wheel.
            Box("Carnival tent", carnival, new Vector3(-6, 1.5f, 0), new Vector3(5, 3, 4), cream, false);
            for (int i = 0; i < 5; i++) Box("Tent stripe", carnival, new Vector3(-8 + i, 1.5f, -2.03f), new Vector3(.48f, 3, .04f), pink, false);
            var tentRoof = new GameObject("Tent peak", typeof(MeshFilter), typeof(MeshRenderer)); tentRoof.transform.SetParent(carnival, false); tentRoof.transform.localPosition = new Vector3(-6, 3, 0);
            tentRoof.GetComponent<MeshFilter>().sharedMesh = PyramidMesh(); tentRoof.GetComponent<MeshRenderer>().sharedMaterial = wine;
            Beam("Flagpole", carnival, new Vector3(-6, 5.6f, 0), new Vector3(-6, 7, 0), .06f, gold);
            Pennant(carnival, new Vector3(-5.65f, 6.8f, 0), pink);
            Vector3 wheelCenter = new Vector3(3, 6, 0);
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI * 2 / 16, b = (i + 1) * Mathf.PI * 2 / 16;
                Vector3 point = wheelCenter + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 4.3f;
                Vector3 next = wheelCenter + new Vector3(Mathf.Cos(b), Mathf.Sin(b), 0) * 4.3f;
                Beam("Wheel rim", carnival, point, next, .08f, cream);
                Beam("Wheel spoke", carnival, wheelCenter, point, .04f, gold);
                if (i % 2 == 0) Box("Gondola", carnival, point + Vector3.down * .35f, new Vector3(.55f, .6f, .45f), i % 4 == 0 ? wine : teal, false);
            }
            Beam("Wheel support", carnival, new Vector3(0, 0, 0), wheelCenter, .2f, teal);
            Beam("Wheel support", carnival, new Vector3(6, 0, 0), wheelCenter, .2f, teal);
            // Stop exploration at the passenger train's two ends.
            Box("Invisible rear safety barrier", train, new Vector3(-6.15f, 2.3f, 0), new Vector3(.1f, 3, 2), dark).GetComponent<Renderer>().enabled = false;
            Box("Invisible engine safety barrier", train, new Vector3(32.25f, 2.3f, 0), new Vector3(.1f, 3, 2), dark).GetComponent<Renderer>().enabled = false;
            // Collision remains when the near wall is cut away for the camera.
            for (int i = 0; i < 3; i++)
                Box("Invisible cutaway safety boundary", train, new Vector3(i * 13, 2.4f, -2.87f), new Vector3(12, 3, .12f), dark).GetComponent<Renderer>().enabled = false;
        }

        static void Sofa(Transform p, Vector3 position, float width, Material upholstery)
        {
            var s = Group("Tufted velvet banquette", p); s.localPosition = position;
            Box("Brass plinth", s, new Vector3(0, .18f, 0), new Vector3(width, .2f, .9f), gold);
            Box("Velvet seat", s, new Vector3(0, .48f, 0), new Vector3(width, .4f, 1), upholstery);
            Box("High back", s, new Vector3(0, 1.05f, .42f), new Vector3(width, 1.0f, .25f), upholstery);
            foreach (int side in new[] { -1, 1 }) Box("Armrest", s, new Vector3(side * width * .49f, .75f, 0), new Vector3(.18f, .55f, 1.05f), upholstery);
            for (int i = 0; i < 5; i++) Sphere("Tuft button", s, new Vector3(-width * .36f + i * width * .18f, 1.13f, .275f), Vector3.one * .065f, gold, false);
        }
        static void Chair(Transform p)
        {
            Box("Seat cushion", p, new Vector3(0, .58f, 0), new Vector3(.78f, .18f, .78f), wine);
            Box("Upholstered back", p, new Vector3(0, 1.05f, .35f), new Vector3(.78f, .9f, .15f), teal);
            foreach (int x in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
                Box("Chair leg", p, new Vector3(x * .29f, .28f, z * .29f), new Vector3(.07f, .56f, .07f), gold);
        }
        static void Table(Transform p, Vector3 position, float width, float depth, Material top)
        {
            var t = Group("Table", p); t.localPosition = position;
            Box("Tabletop", t, new Vector3(0, .86f, 0), new Vector3(width, .12f, depth), top);
            Cylinder("Pedestal", t, new Vector3(0, .4f, 0), new Vector3(.15f, .4f, .15f), gold);
            Box("Pedestal foot", t, new Vector3(0, .05f, 0), new Vector3(width * .65f, .1f, depth * .55f), dark);
        }
        static void Cup(Transform p, Vector3 position)
        {
            Cylinder("Saucer", p, position, new Vector3(.27f, .012f, .27f), paper, false);
            Cylinder("Teacup", p, position + Vector3.up * .09f, new Vector3(.17f, .07f, .17f), cream, false);
            Cylinder("Tea surface", p, position + Vector3.up * .165f, new Vector3(.135f, .005f, .135f), wood, false);
        }
        static void Vase(Transform p, Vector3 position)
        {
            Cylinder("Bud vase", p, position + Vector3.up * .14f, new Vector3(.16f, .14f, .16f), teal, false);
            Beam("Flower stem", p, position, position + Vector3.up * .55f, .025f, green);
            Sphere("Rose", p, position + Vector3.up * .55f, Vector3.one * .2f, pink, false);
        }
        static void Plant(Transform p, Vector3 position)
        {
            Cylinder("Brass planter", p, position + Vector3.up * .3f, new Vector3(.6f, .3f, .6f), gold);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72 * Mathf.Deg2Rad;
                var leaf = Sphere("Sculptural leaf", p, position + new Vector3(Mathf.Sin(a) * .25f, 1.05f, Mathf.Cos(a) * .25f), new Vector3(.28f, 1.15f, .36f), green, false);
                leaf.localRotation = Quaternion.Euler(Mathf.Cos(a) * 25, 0, Mathf.Sin(a) * 25);
            }
        }
        static void Suitcase(Transform p, Vector3 pos, Material leather)
        {
            Box("Leather travelling trunk", p, pos + Vector3.up * .25f, new Vector3(1.15f, .5f, .7f), leather);
            foreach (int s in new[] { -1, 1 })
            {
                Box("Trunk brass band", p, pos + new Vector3(s * .38f, .25f, -.36f), new Vector3(.065f, .48f, .025f), gold, false);
                Box("Trunk strap", p, pos + new Vector3(s * .38f, .51f, 0), new Vector3(.065f, .025f, .7f), gold, false);
            }
            Box("Trunk handle", p, pos + new Vector3(0, .28f, -.39f), new Vector3(.26f, .06f, .07f), dark, false);
        }
        static void Pendant(Transform p, float x, float z)
        {
            Cylinder("Pendant stem", p, new Vector3(x, 4.2f, z), new Vector3(.04f, .35f, .04f), gold, false);
            Sphere("Opaline pendant", p, new Vector3(x, 3.8f, z), new Vector3(.5f, .45f, .5f), glow, false);
            Cylinder("Pendant collar", p, new Vector3(x, 4.02f, z), new Vector3(.22f, .035f, .22f), gold, false);
            var light = new GameObject("Warm pendant light").AddComponent<Light>(); light.transform.SetParent(p, false); light.transform.localPosition = new Vector3(x, 3.65f, z);
            light.type = LightType.Point; light.color = Hex("FFDA99"); light.intensity = 3.5f; light.range = 5; light.shadows = LightShadows.None;
        }
        static void Mask(Transform p, Vector3 position, string kind, Material material)
        {
            var mask = Group(kind + " carnival mask", p); mask.localPosition = position;
            Sphere("Mask face", mask, Vector3.zero, new Vector3(.58f, .65f, .13f), material, false);
            foreach (int side in new[] { -1, 1 })
            {
                Sphere("Eye opening", mask, new Vector3(side * .15f, .05f, -.085f), new Vector3(.16f, .1f, .04f), dark, false);
                var ear = Sphere("Mask ear", mask, new Vector3(side * .22f, kind == "Rabbit" ? .46f : .27f, 0), new Vector3(.16f, kind == "Rabbit" ? .65f : .3f, .12f), material, false);
                ear.localRotation = Quaternion.Euler(0, 0, side * -18);
            }
            Sphere("Nose / beak", mask, new Vector3(0, -.1f, -.12f), new Vector3(.12f, .16f, .17f), gold, false);
        }
        static void Pennant(Transform p, Vector3 pos, Material material)
        {
            var go = new GameObject("Carnival pennant", typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(p, false); go.transform.localPosition = pos;
            string path = Root + "/Meshes/Pennant.asset"; var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = new Mesh { name = "Two-sided pennant" }; AssetDatabase.CreateAsset(mesh, path); }
            // Each face needs its own vertices so opposite normals cannot cancel out.
            mesh.Clear();
            mesh.vertices = new[] { new Vector3(-.17f, 0, 0), new Vector3(.17f, 0, 0), new Vector3(0, -.38f, 0), new Vector3(-.17f, 0, 0), new Vector3(.17f, 0, 0), new Vector3(0, -.38f, 0) };
            mesh.triangles = new[] { 0, 1, 2, 5, 4, 3 }; mesh.RecalculateNormals(); EditorUtility.SetDirty(mesh);
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
        static void Sign(Transform p, Vector3 pos, string text, float size, Material material)
        {
            var go = new GameObject(text, typeof(TextMesh)); go.transform.SetParent(p, false); go.transform.localPosition = pos;
            var label = go.GetComponent<TextMesh>(); label.text = text; label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = 60; label.characterSize = size / 6;
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.color = material.color;
            go.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
        }
        static void Inspect(Transform p, string title, string description) { var item = p.gameObject.AddComponent<TrainObservation>(); item.title = title; item.description = description; }
        static Transform Group(string name, Transform parent) { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }
        static Transform Box(string name, Transform p, Vector3 pos, Vector3 size, Material m, bool solid = true) { return Primitive(PrimitiveType.Cube, name, p, pos, size, m, solid); }
        static Transform Sphere(string name, Transform p, Vector3 pos, Vector3 size, Material m, bool solid = true) { return Primitive(PrimitiveType.Sphere, name, p, pos, size, m, solid); }
        static Transform Capsule(string name, Transform p, Vector3 pos, Vector3 size, Material m, bool solid = true) { return Primitive(PrimitiveType.Capsule, name, p, pos, size, m, solid); }
        static Transform Cylinder(string name, Transform p, Vector3 pos, Vector3 size, Material m, bool solid = true) { return Primitive(PrimitiveType.Cylinder, name, p, pos, size, m, solid); }
        static Transform Primitive(PrimitiveType type, string name, Transform p, Vector3 pos, Vector3 size, Material m, bool solid)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(p, false); go.transform.localPosition = pos; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = m;
            if (!solid) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go.transform;
        }
        static void Beam(string name, Transform p, Vector3 a, Vector3 b, float diameter, Material m)
        {
            var t = Cylinder(name, p, (a + b) * .5f, new Vector3(diameter, Vector3.Distance(a, b) * .5f, diameter), m, false);
            t.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
        }
        static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out Color result); return result; }
        static Material Mat(string name, string color, float metallic = 0, float smoothness = .12f, float emission = 0)
        {
            if (Materials.TryGetValue(name, out Material known)) return known;
            string path = Root + "/Materials/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.name = name; mat.color = Hex(color); mat.SetFloat("_Metallic", metallic); mat.SetFloat("_Smoothness", smoothness);
            if (emission > 0) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", Hex(color) * emission); }
            EditorUtility.SetDirty(mat); Materials[name] = mat; return mat;
        }
        static Mesh RoofMesh()
        {
            const string path = Root + "/Meshes/BarrelRoof.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (existing != null) return existing;
            var verts = new List<Vector3>(); var tris = new List<int>();
            for (int i = 0; i <= 20; i++)
            {
                float a = i * Mathf.PI / 20;
                Vector3 point = new Vector3(0, 4.48f + Mathf.Sin(a) * .85f, Mathf.Cos(a) * 3.02f);
                verts.Add(point + Vector3.left * 6.12f); verts.Add(point + Vector3.right * 6.12f);
                if (i < 20) { int n = i * 2; tris.AddRange(new[] { n, n + 1, n + 2, n + 1, n + 3, n + 2 }); }
            }
            var mesh = new Mesh { name = "Barrel roof", vertices = verts.ToArray(), triangles = tris.ToArray() }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        static Mesh PyramidMesh()
        {
            const string path = Root + "/Meshes/TentRoof.asset"; var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (mesh != null) return mesh;
            mesh = new Mesh { name = "Tent roof", vertices = new[] { new Vector3(-2.6f, 0, -2.1f), new Vector3(2.6f, 0, -2.1f), new Vector3(2.6f, 0, 2.1f), new Vector3(-2.6f, 0, 2.1f), new Vector3(0, 2.7f, 0) }, triangles = new[] { 0, 4, 1, 1, 4, 2, 2, 4, 3, 3, 4, 0 } };
            mesh.RecalculateNormals(); AssetDatabase.CreateAsset(mesh, path); return mesh;
        }

        [MenuItem("Graduation Demo/Validate and Capture Train")]
        public static void ValidateAndCapture()
        {
            var tour = UnityEngine.Object.FindFirstObjectByType<TrainTour>();
            if (tour == null || tour.roomViews.Length != 3 || tour.cutawayShells.Length != 6) throw new Exception("Train tour is incomplete.");
            var missing = new List<string>();
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                    if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null) missing.Add(renderer.name);
            if (missing.Count > 0) throw new Exception("Missing materials: " + string.Join(", ", missing));
            // Check a physically traversable path in both directions through both gangways.
            Physics.SyncTransforms();
            for (float x = -5.5f; x <= 31.5f; x += .25f)
                if (Physics.CheckCapsule(new Vector3(x, 1.5f, -.5f), new Vector3(x, 2.6f, -.5f), .23f, ~0, QueryTriggerInteraction.Ignore))
                {
                    // Exclude only the detective's own controller at the starting position.
                    var hits = Physics.OverlapCapsule(new Vector3(x, 1.5f, -.5f), new Vector3(x, 2.6f, -.5f), .23f);
                    foreach (var hit in hits) if (!hit.transform.IsChildOf(tour.detective.transform)) throw new Exception("Aisle obstructed at x=" + x + " by " + hit.name);
                }
            for (int i = 0; i < 3; i++)
            {
                tour.SetView(i, false);
                tour.viewCamera.transform.SetPositionAndRotation(tour.roomViews[i].position, tour.roomViews[i].rotation); tour.viewCamera.orthographicSize = Mathf.Max(tour.interiorSize, 6.8f / (1600f / 900));
                Capture(tour.viewCamera, "Documentation/TrainPreviews/0" + (i + 1) + ".png");
            }
            tour.SetExterior(true); tour.viewCamera.transform.SetPositionAndRotation(tour.exteriorView.position, tour.exteriorView.rotation); tour.viewCamera.orthographicSize = 21;
            Capture(tour.viewCamera, "Documentation/TrainPreviews/04-exterior.png");
            tour.SetView(0, false); tour.SnapCamera();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            File.WriteAllText("Documentation/TrainPreviews/validation.txt", "PASS: Three furnished carriages, six cutaway shells, assigned materials, open collision-tested aisle and gangways.\nPASS: Four camera renders captured.\n");
            Debug.Log("CARNIVAL_TRAIN_VALIDATION_OK");
        }
        static void Capture(Camera camera, string path)
        {
            var texture = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32); texture.antiAliasing = 2;
            // Warm the new camera/renderer before the exported frame.
            camera.targetTexture = texture; camera.Render(); camera.Render(); var previous = RenderTexture.active; RenderTexture.active = texture;
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG()); camera.targetTexture = null; RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
