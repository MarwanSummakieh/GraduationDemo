using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace GraduationDemo.Train.Editor
{
    // Allows verification in an already-open editor, without a second editor taking the project lock.
    [InitializeOnLoad]
    public static class InteractionVerification
    {
        static readonly TestRunnerApi api;
        const string ModeKey = "GraduationDemo.InteractionVerification.Mode";
        static InteractionVerification()
        {
            api = ScriptableObject.CreateInstance<TestRunnerApi>(); api.RegisterCallbacks(new Results());
        }
        [MenuItem("Graduation Demo/Narrative/Verify Interactions - EditMode")]
        public static void EditMode() => Run(TestMode.EditMode, "GraduationDemo.Narrative.Tests");
        [MenuItem("Graduation Demo/Narrative/Verify Interactions - PlayMode")]
        public static void PlayMode() => Run(TestMode.PlayMode, "GraduationDemo.Narrative.PlayTests");
        static void Run(TestMode mode, string assembly)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || !string.IsNullOrEmpty(SessionState.GetString(ModeKey, "")))
                throw new System.InvalidOperationException("Wait for compilation, play mode or the active test run to finish.");
            SessionState.SetString(ModeKey, mode.ToString());
            api.Execute(new ExecutionSettings(new Filter { testMode = mode, assemblyNames = new[] { assembly } }));
        }
        sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { Debug.Log("CLUE_TEST_START: " + test.FullName); }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                var mode = SessionState.GetString(ModeKey, ""); if (string.IsNullOrEmpty(mode)) return;
                SessionState.EraseString(ModeKey); Directory.CreateDirectory("Documentation/Narrative");
                TestRunnerApi.SaveResultToFile(result, Path.GetFullPath("Documentation/Narrative/ClueInteractions" + mode + "Results.xml"));
                Debug.Log("CLUE_INTERACTION_TESTS: " + mode + "; " + result.PassCount + " passed, " + result.FailCount + " failed.");
            }
        }
    }
}
