using System.Collections.Generic;
using System.Linq;

namespace GraduationDemo.Narrative
{
    public static class AccusationReasoning
    {
        public const string Means = "P_MEANS", Motive = "P_MOTIVE", Opportunity = "P_OPPORTUNITY";
        public static readonly string[] Roles = { Means, Motive, Opportunity };
        public static string Label(string role) => role == Means ? "Means" : role == Motive ? "Motive" : role == Opportunity ? "Opportunity" : "Evidence";
        public static string SocketId(string role) => "socket/" + role;
        public static Dictionary<string, string> Allocate(NarrativeCatalogue catalogue, IEnumerable<string> theories)
        {
            var result = new Dictionary<string, string>();
            foreach (var id in theories)
            {
                var theory = catalogue.Theory(id); if (theory == null) continue;
                var role = Roles.FirstOrDefault(r => !result.ContainsKey(r) && theory.propositions.Contains(r)) ?? Roles.FirstOrDefault(r => !result.ContainsKey(r));
                if (role != null) result[role] = id;
            }
            return result;
        }
        public static void MigrateReviewedDefaults(NarrativeCatalogue catalogue)
        {
            foreach (var theory in catalogue.theories)
            {
                if (theory.propositions.Remove("P1_REAL_INJURY")) theory.propositions.Add(Means);
                if (theory.propositions.Remove("P3_MILA_PRESENCE")) theory.propositions.Add(Opportunity);
                if (theory.propositions.Remove("P4_CONTRADICTORY_ACCOUNT")) theory.propositions.Add(Means);
                if (theory.id == "T_C06_MS06" && !theory.propositions.Contains(Motive))
                { theory.propositions.Add(Motive); theory.observationGroup = "relationship_and_reaction"; }
            }
            foreach (var rule in catalogue.arrestRules.Where(r => r.id == "Physical MVP"))
            {
                rule.id = "Means, Motive and Opportunity";
                rule.propositions = new List<string>(Roles);
                rule.requiredStatements.Clear(); rule.requiredClues = new List<string> { "C03" };
                rule.forbiddenFlags = new List<string> { "flag:resolved" };
            }
        }
    }
}
