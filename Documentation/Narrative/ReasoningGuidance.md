# Reasoning guidance and right-click removal

Right-click any active theory icon or its caption to remove that theory and its parent connections. No prior selection is required. Left-click still selects it. The removal instruction appears in the hover tooltip; the separate remove button is gone. **Removed** restores theories without resetting discoveries or consumed conversations.

The Reasoning toolbar has a **Guidance** button that cycles **Off → Subtle → Guided**. Subtle is the default. This is a notebook assistance preference for the current play session; the starting setting is editable in `Assets/Narrative/Data/InvestigationPacing.asset`.

- **Off:** no guidance marks, links or explanations.
- **Subtle:** `!` marks a potential contradiction and `?` marks a relevant ambiguity on nodes only. No dotted links or counterpart identities are revealed.
- **Guided:** adds dotted connections between related discoveries, their identities, and the authored question/explanation in hover and scrollable notebook details. Selecting a theory includes notes relevant to its parents.

Orange dashed links identify potential contradictions; violet dashed links identify ambiguities. Symbols and readable labels also communicate the distinction. These links are reasoning notes, distinct from the solid parent connections of constructed theories.

## Authoring

Expand **Reasoning Hints** in the investigation pacing asset. Each entry has a stable ID, two discovery IDs, a kind, and a Guided prompt. IDs may refer to clues, statements or active constructed theories. Both discoveries must have been acquired before the hint is available; both endpoints must also be visible in the current filtered graph. Archived theories do not count as active hint endpoints.

The four initial entries concern Mila's rehearsal/visit accounts versus Felix's 20:45 sighting, the 20:10 receipt versus Felix's approximately 20:30 alibi, and the uncertain cause of the broken clock. They flag questions to examine, not proven lies or guilt. No hint uses hidden truth labels, unlocks testimony, changes accusation support, spends an action, or resets conversations. Prompts are unvoiced notebook annotations, not new dialogue attributed to Burrows.

This prototype requires authored relationships. It does not infer contradictions from arbitrary prose or automatically assess every possible theory.

## Verification

44 EditMode tests and 13 PlayMode tests pass. The new tests cover discovery prerequisites, assistance tiers, unchanged progress/action state, and active versus archived theory endpoints. A live scene check additionally verifies actual Input System right-click input, icon/caption event routing, restoration, the missing remove button, Guided details, Off suppression, and unrevealed-testimony gating. The captured Guided view was checked for readable links and separated sidebar controls.

Reports: `ReasoningGuidanceEditModeResults.json`, `ReasoningGuidancePlayModeResults.json`, `ReasoningGuidanceInteractionReview.json`; visual evidence: `ReasoningGuidance.png`.
