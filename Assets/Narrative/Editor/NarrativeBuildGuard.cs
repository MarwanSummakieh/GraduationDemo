using System.Linq;
using GraduationDemo.Narrative;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace GraduationDemo.Editor
{
    public sealed class NarrativeBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report)
        {
            var c = AssetDatabase.LoadAssetAtPath<NarrativeCatalogue>(NarrativeImporter.AssetPath);
            ValidateForBuild(c, (report.summary.options & BuildOptions.Development) != 0);
        }
        public static void ValidateForBuild(NarrativeCatalogue c, bool development)
        {
            if (c == null) throw new BuildFailedException("Import the narrative catalogue before building.");
            var errors = NarrativeValidation.Errors(c);
            if (errors.Count > 0) throw new BuildFailedException(string.Join("\n", errors));
            if (development) return;
            var used = c.sequences.SelectMany(x => x.beats).Select(x => x.voiceId).Concat(new[] { "BURR_NO_CHAT_01" }).Distinct();
            var missing = used.Where(id => c.Voice(id)?.clip == null).ToArray();
            if (missing.Length > 0) throw new BuildFailedException("Release build blocked: missing matching recordings for " + string.Join(", ", missing));
        }
    }
}
