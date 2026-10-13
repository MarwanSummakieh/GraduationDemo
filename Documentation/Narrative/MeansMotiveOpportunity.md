# Means, Motive and Opportunity

This model supersedes the injury/presence/contradiction rule. The final accusation uses exactly three labelled sockets, each holding one distinct constructed theory:

- **Means:** a plausible physical method consistent with the observed injury.
- **Motive:** a plausible reason for conflict, supported as an interpretation of the collected relationship evidence.
- **Opportunity:** evidence connecting the suspect to the scene around the incident.

Click a theory and then its socket. Compatible sockets highlight and theory details name the roles it can support. An occupied socket replaces its prior theory; moving a theory clears its old socket, preventing duplication. Repeating a connection untethers it. After all three, connect the central accusation to a suspect and confirm. Wrong-role placements remain editable and the live checklist explains the missing role.

## Supported hypothesis

The previously reported combination is now supported for Mila:

| Socket | Theory |
| --- | --- |
| Means | C03 + B-S01 — noise and head injury |
| Motive | C06 + M-S06 — possible feeling of displacement |
| Opportunity | C09 + F-S04 — presence despite the rehearsal prohibition |

These form an investigative hypothesis, not proof of murderous intent. The motive is explicitly described as an inference. The existing confession still establishes the slap and accidental impact with the clock; it has not been rewritten into a deliberate killing.

The old mandatory contradiction, private spatial disclosure gate and blanket clock/desk/alibi prerequisites are removed. Attached theories require their collected parents and separate supporting observation groups. The observed injury (C03) is the only additional physical prerequisite, already a parent of the usual Means theory. Other constructed spatial-account theories can strengthen Means/context, but are not mandatory.

## Data and recording migration

The importer migrates reviewed legacy support tags to `P_MEANS`, `P_MOTIVE`, `P_OPPORTUNITY` and replaces the default Physical MVP rule. Existing arbitrary support metadata, clips and catalogue identity are preserved; disabled alternative endings remain disabled. The existing 160 theory texts and investigation routes are unchanged.

The old finale bridge referred to the now-optional fall account. Its ending-only replacement has a new voice ID **BURR_MMO_FINALE_01**: “The injury, your connection to the couple, and the sighting near Odette's compartment warrant an explanation.” It needs a new recording. Original BURR_025 remains intact for its other uses and audit. The catalogue now contains 218 voice definitions.

Mila remains the only authored supported resolution; the socket system is general, but no unsupported Felix/Bellamy confession was invented. Final-day gating, action economy, one-use dialogue and continued final-stage theory construction are retained.

## Verification

59 EditMode and 19 PlayMode tests passed. Coverage includes the reported combination, missing Motive, wrong socket placements, duplicate prevention, independent observations, source ownership and a final accusation without the old contradiction prerequisites. A real mouse walkthrough constructed the three theories, highlighted compatible sockets, attached and moved them, corrected a wrong-role draft and confirmed the hypothesis through the revised bridge to the existing resolved ending. Reasoning spent no actions.

Evidence: `MMOEditModeResults.json`, `MMOPlayModeResults.json`, `MMOInteractionReview.json`, `MMOFinaleReview.json`, and `MMOSockets.png`.
