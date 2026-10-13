# Reasoning guidance and right-click removal

Right-click any active theory icon or its caption to remove that theory and its parent connections. No prior selection is required. Left-click still selects it. The removal instruction appears in the hover tooltip; the separate remove button is gone. **Removed** restores theories without resetting discoveries or consumed conversations.

The Reasoning toolbar has a **Guidance** button that cycles **Off → Subtle → Guided → Directed**. Subtle remains the default. Choose Directed for help deciding whom to confront and how interviews feed the final case. This is a notebook assistance preference for the current play session; the starting setting is editable in `Assets/Narrative/Data/InvestigationPacing.asset`.

- **Off:** no guidance marks, links or explanations.
- **Subtle:** marks nodes only, without counterpart identities or dotted lines.
- **Guided:** adds dotted connections and authored questions/explanations, but does not recommend a recipient.
- **Directed:** also names the recipient of an interview lead, highlights the matching presentation button, explains the investigation workflow, and labels reaction-only conversations before spending confirmation.

**? / violet links** are interview leads. Construct that exact clue–statement pairing, select the theory, and approach a suspect; Directed names the recommended recipient. **+ / green links** are case-building connections. They suggest reviewing a theory as injury, presence, contradictory testimony or context; they explicitly do not suggest another interview. Orange/`!` remains available for authored contradiction hints. Solid parent connections still show constructed theories.

Selected theory details and tooltips use only hints matching that exact pair, not every hint sharing one of its ingredients. Directed puts the recipient or case-review advice first, above long source descriptions. Unhinted theories explain that no new interview lead is identified. All four settings preserve ordinary player choices, action confirmation and one-use conversations.

## Reported generic-response theory

`T_C09_FS04` — **Mila was near the private rehearsal despite the prohibition** — has only generic confrontation reactions in the supplied catalogue. It is a presence theory for the final case; it has no bespoke follow-up revelation. This was a guidance/progression coverage gap, not a missed dialogue import. Its new + connection explicitly directs the player toward case review, and Directed confirmation warns that further confrontations are reaction-only. No new canonical dialogue or rewards were invented.

## Authoring

Expand **Reasoning Hints** in the investigation pacing asset. Each entry has a stable ID, two discovery IDs, a kind, purpose (**Interview** or **CaseReview**), recipient, prompt and optional **Resolved By** list. Recipient is shown only in Directed mode. IDs may refer to clues, statements or active constructed theories. Both discoveries must have been acquired, and both endpoints must be visible in the filtered graph. Archived theories do not count as active endpoints. Learning a Resolved By discovery retires a lead, including via an alternative route. Interview leads are hidden after investigation closes; case-review connections remain available.

There are **10 interview leads and 13 case-review connections**. The leads cover nine unique revelations. Every newly revealed statement has a follow-on connection to an acquired clue; M-S04 also opens the two spatial-slip interview leads. The receipt/bar-alibi hint remains removed because it only leads to a generic reaction. Lower assistance levels leave the recipient choice to the player; Directed recommends the authored first-disclosure recipient. No mode reveals future testimony or guarantees a theory's truth.

| Pair | Inquiry | Retires on |
| --- | --- | --- |
| C06 + F-S02 | Rings and champagne celebration | F-S03 |
| C09 + F-S01 | Private rehearsals and activity elsewhere | F-S04 |
| C08 + F-S01 | Preparation activities and costume exchanges | F-S05 |
| C09 + B-S02 | Privacy rule and disagreement | B-S03 |
| C02 + B-S02 | Clock damage and a misunderstood performance | B-S04 |
| C08 + B-S01 | Stage materials and the reported crash | B-S05 |
| C06 + M-S03 | Rings and Odette's reported happiness | M-S06 |
| C02 + M-S03 | Happy mood versus the clock's unexplained damage | M-S04 |
| C07 + M-S04 | Desk damage versus ordinary clumsiness | M-S05 |
| C10 + M-S04 | Clock position versus ordinary clumsiness | M-S05 |

These coverage/reward details are authoring notes, not player-facing hints. No hint uses hidden truth labels, unlocks testimony, changes accusation support, spends an action, or resets conversations. Prompts are unvoiced notebook annotations, not new dialogue attributed to Burrows.

This prototype requires authored relationships. It does not infer contradictions from arbitrary prose or automatically assess every possible theory.

## Verification

49 EditMode tests and 13 PlayMode tests pass. Tests cover recipients, exact pair matching, the reported reaction-only theory, acquired-only follow-on connections for all nine revelations, and a complete Directed path: all nine useful interviews within the default nine-action budget, followed by three independently supported case theories and a valid final accusation. Existing discovery, pacing and conversation checks remain in place. Live UI evidence checks mode cycling, recipient visibility, theory tooltips, exact-pair details and follow-on links.

Reports: `ReasoningGuidanceEditModeResults.json`, `ReasoningGuidancePlayModeResults.json`, `DirectedGuidanceInteractionReview.json`; visual evidence: `DirectedGuidance.png`, `DirectedInterview.png`, `DirectedReactionWarning.png`.
