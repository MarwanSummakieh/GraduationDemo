# Live accusation support

**Historical rule:** the injury/presence/contradiction model below has been superseded by [Means, Motive and Opportunity sockets](MeansMotiveOpportunity.md). The live assessment/checklist infrastructure remains in use with the new criteria.

The current case rule is retained: **real injury, presence at the scene, and contradictory testimony**, each supported by a separate observation, plus the required collected accounts/physical observations and usable confidential geometry. This is not a means/motive/opportunity rule. The existing accidental-death ending does not require proving a deliberate motive.

While constructing an accusation, the sidebar and accusation-node tooltip show a live **CASE SUPPORT** checklist. It updates whenever theories or the suspect tether change. Missing corroboration, required observations/accounts and invalidated confidential knowledge are explained. Uncollected statement text and hidden truth/reliability labels are not revealed.

The checklist and final submission use the same assessment function, so a visually supported draft cannot fail because of an undisplayed validator requirement. Feedback is part of the case-building interface in every assistance setting; hint lines and recipient suggestions still follow their selected guidance level. Unsupported submissions remain possible and receive detailed feedback without reopening investigation.

## Reported combination

- `T_C03_BS01`: supplies real-injury support.
- `T_C09_FS04`: supplies presence support.
- `T_C06_MS06`: suggests a possible feeling/motive, but supplies no corroborated contradiction or third independent evidence line.

The displayed gap is **contradictory testimony / independent corroboration**. The displacement theory also gets a short explanation distinguishing an inferred motive from a supported fact. Replacing it with supported spatial-account evidence can complete the case, provided the required discoveries were collected and the geometry was not previously disclosed.

No story dialogue, theory-support metadata or winning conditions were changed.

## Verification

58 EditMode and 17 PlayMode tests pass. Coverage includes the exact reported combination, replacing its motive theory with supported contradictory testimony, required accounts, confidentiality conditions, missing targets and duplicate selections. Existing supported/unsupported cases and independent-evidence checks remain passing. The live UI was checked with the reported draft and then updated to supported by replacing one tethered theory, without spending actions.

Evidence: `AccusationFeedbackEditModeResults.json`, `AccusationFeedbackPlayModeResults.json`, `AccusationFeedbackReportedCaseReview.json`, `AccusationFeedbackReplacementReview.json`, and `AccusationMissingEvidence.png`.
