using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CharacterGallery
{
    public static string Capture()
    {
        var original = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            var camera = new GameObject("Gallery camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 1.05f, -22); camera.orthographic = true; camera.orthographicSize = 2.6f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.3f,.35f,.38f);
            camera.cullingMask = 1 << 30;
            var lamp = new GameObject("Gallery key").AddComponent<Light>(); lamp.type = LightType.Directional; lamp.intensity = 1.7f; lamp.transform.rotation = Quaternion.Euler(40,-25,0); lamp.cullingMask = 1 << 30;
            for(int i=1;i<=10;i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Hodaart/CasualCharacterCollection3/Prefabs/Characters/Character " + i.ToString("00") + ".prefab");
                var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                model.transform.position = new Vector3((i-5.5f)*2.0f, 0,0); model.transform.rotation = Quaternion.Euler(0,180,0);
                foreach(var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=30;
                var animator=model.GetComponent<Animator>(); animator.applyRootMotion=false; animator.Rebind(); animator.Update(0);
                var label = new GameObject("Label",typeof(TextMesh)); label.layer=30; label.transform.position = model.transform.position+new Vector3(0,-.25f,0);
                var text=label.GetComponent<TextMesh>(); text.text=i.ToString("00"); text.anchor=TextAnchor.MiddleCenter; text.characterSize=.1f; text.fontSize=50;
                text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.GetComponent<Renderer>().sharedMaterial=text.font.material;
            }
            var rt=new RenderTexture(2200,520,24); camera.targetTexture=rt; camera.Render();camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=rt;
            var png=new Texture2D(2200,520,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,2200,520),0,0);png.Apply();
            Directory.CreateDirectory("Documentation/TrainPreviews"); File.WriteAllBytes("Documentation/TrainPreviews/character-gallery.png",png.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(png);
            return "Documentation/TrainPreviews/character-gallery.png";
        }
        finally {EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(original);}
    }
}
