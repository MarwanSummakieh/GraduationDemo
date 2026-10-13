using System.Linq;
using GraduationDemo.Editor;
using NUnit.Framework;
using UnityEngine;

namespace GraduationDemo.Narrative.Tests
{
    public class AccusationAssessmentTests
    {
        NarrativeCatalogue catalogue;
        InvestigationState state;
        [SetUp] public void Setup()
        {
            catalogue = NarrativeImporter.Build(NarrativeImporter.Source); state = new InvestigationState(catalogue);
            foreach (var c in catalogue.clues) state.Grant(c.id); foreach (var s in catalogue.statements) state.Grant(s.id);
            while (state.InvestigationOpen) state.EndDay(out _);
        }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(catalogue); }
        string[] Construct(params string[] ids) { foreach (var id in ids) { var t = catalogue.Theory(id); state.Construct(t.clueId, t.statementId); } return ids; }
        [Test] public void ReportedCaseSupportsAMeansMotiveOpportunityHypothesis()
        {
            var ids = Construct("T_C03_BS01", "T_C06_MS06", "T_C09_FS04");
            var assessment = state.AssessAccusation("Mila", ids);
            Assert.That(assessment.Supported, Is.True);
            foreach (var role in AccusationReasoning.Roles) Assert.That(assessment.checks.Single(c => c.key == role).satisfied, Is.True);
            Assert.That(assessment.DisplayText, Does.Contain("not proof of intent"));
            Assert.That(state.EvaluateArrest("Mila", ids, out _), Is.Not.Null);
            var missingMotive = Construct("T_C03_BS01", "T_C10_MS05", "T_C09_FS04");
            Assert.That(state.AssessAccusation("Mila", missingMotive).DisplayText, Does.Contain("[Missing] Motive"));
            Assert.That(state.EvaluateArrest("Mila", missingMotive, out _), Is.Null);
        }
        [Test] public void DiagnosticsAlsoExplainMissingObservationsAndDisclosedGeometryWithoutLeakingClaims()
        {
            var ids = Construct("T_C03_BS01", "T_C06_MS06", "T_C09_FS04");
            state.progress.statements.Remove("M-S06");
            var assessment = state.AssessAccusation("Mila", ids);
            Assert.That(assessment.Supported, Is.False); Assert.That(assessment.DisplayText, Does.Contain("missing its collected source evidence"));
            Assert.That(assessment.DisplayText, Does.Not.Contain(catalogue.Statement("M-S06").claim));
            state.progress.statements.Add("M-S06"); state.Grant("flag:geometry_disclosed");
            Assert.That(state.AssessAccusation("Mila", ids).Supported, Is.True);
            Assert.That(state.EvaluateArrest("Mila", ids, out _), Is.Not.Null);
        }
        [Test] public void PreviewDoesNotResolveWithoutTargetOrAcceptDuplicateTheories()
        {
            var ids = Construct("T_C03_BS01", "T_C06_MS06", "T_C09_FS04");
            Assert.That(state.AssessAccusation(null, ids).Supported, Is.False);
            Assert.That(state.AssessAccusation(null, ids).DisplayText, Does.Contain("Tether the accusation to a suspect"));
            Assert.That(state.AssessAccusation("Mila", new[] { ids[0], ids[0], ids[2] }).Supported, Is.False);
            Assert.That(state.AssessAccusation("Felix", ids).Supported, Is.False);
        }
        [Test] public void SocketAssignmentsMustMatchRolesAndCannotReuseOneTheory()
        {
            var ids = Construct("T_C03_BS01", "T_C06_MS06", "T_C09_FS04");
            var sockets = AccusationReasoning.Allocate(catalogue, ids);
            Assert.That(state.AssessAccusation("Mila", ids, sockets).Supported, Is.True);
            sockets[AccusationReasoning.Means] = ids[1]; sockets[AccusationReasoning.Motive] = ids[0];
            Assert.That(state.AssessAccusation("Mila", ids, sockets).Supported, Is.False);
            Assert.That(state.EvaluateArrest("Mila", ids, out _, sockets), Is.Null);
            sockets[AccusationReasoning.Motive] = ids[1];
            Assert.That(state.AssessAccusation("Mila", ids, sockets).Supported, Is.False);
        }
    }
}
