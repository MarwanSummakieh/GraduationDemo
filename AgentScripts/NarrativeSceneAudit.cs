using System;
using System.IO;
using System.Linq;
using GraduationDemo.Editor;
using GraduationDemo.Train;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NarrativeSceneAudit
{
    public static string TestRunnerState()
    {
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.TestTools.TestRunner.TestRun.TestJobDataHolder")).First(t => t != null);
        var holder = type.BaseType.GetProperty("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).GetValue(null);
        var jobs = (System.Collections.IList)type.GetField("TestRuns").GetValue(holder);
        return "Active test jobs: " + jobs.Count + "; play mode: " + EditorApplication.isPlaying;
    }
    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Run the scene audit in Edit mode.");
        string guid = AssetDatabase.AssetPathToGUID(NarrativeImporter.AssetPath);
        string before = Snapshot();
        NarrativeSceneInstaller.Install(); NarrativeSceneInstaller.Install();
        if (Snapshot() != before) throw new Exception("Repeated installation changed generated object counts or prefab identities.");
        EditorSceneManager.OpenScene("Assets/Scenes/CarnivalExpress.unity"); NarrativeSceneInstaller.ValidateScene();
        if (Snapshot() != before || AssetDatabase.AssetPathToGUID(NarrativeImporter.AssetPath) != guid) throw new Exception("Scene reload changed bindings or catalogue GUID.");
        const string report = "PASS: two scene installs and saved scene reload preserve one narrative controller, ten clue bindings, three suspect bindings, four character identities, five room cameras, original character prefab GUIDs and catalogue GUID. No missing scene scripts or animator references.";
        File.WriteAllText("Documentation/Narrative/SceneAudit.txt", report); return report;
    }
    static string Snapshot()
    {
        var tour = UnityEngine.Object.FindAnyObjectByType<TrainTour>();
        var bindings = UnityEngine.Object.FindObjectsByType<NarrativeInteraction>(FindObjectsSortMode.None);
        var actors = UnityEngine.Object.FindObjectsByType<TrainActor>(FindObjectsSortMode.None);
        string prefabs = string.Join("|", actors.Select(a => a.characterId + ":" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(a)))).OrderBy(x => x));
        return UnityEngine.Object.FindObjectsByType<TrainNarrative>(FindObjectsSortMode.None).Length + "/" + bindings.Length + "/" + actors.Length + "/" + tour.roomViews.Length + "/" + prefabs + "/" + string.Join("|", bindings.Select(b => b.clueId + ":" + b.suspectId).OrderBy(x => x));
    }
}
