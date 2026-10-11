using UnityEditor;
using UnityEngine;
public static class InspectCharacterRig
{
    public static string Run()
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Hodaart/CasualCharacterCollection3/Prefabs/Characters/Character 04.prefab"));
        try
        {
            var a=instance.GetComponent<Animator>(); a.applyRootMotion=false; a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            a.Rebind(); a.Update(.2f);
            var state=a.GetCurrentAnimatorStateInfo(0); var clips=a.GetCurrentAnimatorClipInfo(0);
            string result="Human="+a.isHuman+" avatarValid="+a.avatar.isValid+" state="+state.fullPathHash+" clips="+clips.Length;
            foreach(var c in clips) result+=" "+c.clip.name+" humanMotion="+c.clip.humanMotion;
            foreach(var r in instance.GetComponentsInChildren<SkinnedMeshRenderer>()) result+=" mesh="+r.name+" bounds="+r.bounds;
            return result;
        }
        finally {Object.DestroyImmediate(instance);}
    }
}
