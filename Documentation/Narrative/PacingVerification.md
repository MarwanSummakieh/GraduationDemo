# One-use conversations and notebook verification — 12 October 2026

Final Unity results: **36/36 EditMode tests and 10/10 PlayMode tests passed**, with no compilation errors. Existing deprecated API warnings and explicit missing-recording diagnostics remain.

| Requirement | Verified evidence |
| --- | --- |
| One-use alibis and theory initializations | Rule tests cover consumption at initialization, duplicate attempts, shared revelations and genuinely new conditional responses. Repeated routes cannot replay the same sequence for the same theory/suspect. |
| Only viable conversation prompts; Burrows bark otherwise | Live walkthrough interviewed three suspects for free, then repeated interactions without theories and reached the exact no-chit-chat bark definition. Unbound ambient speakers also use the bark path. |
| Confirm every paid action | Rule and PlayMode tests confirm offers/cancellation spend nothing, confirmation revalidates and spends exactly once, and stale/insufficient-budget attempts fail safely. |
| Dialogue must finish | PlayMode verifies ordinary cancellation cannot close the interview; subsequent initialization is unavailable after completion. Playback recovery preserves discoveries and refunds failed reservations. |
| Configurable days/actions/costs | InvestigationPacing.asset exposes these fields. Tests exercise alternative counts/costs and invalid settings; the live four-revelation route fits the default three-day/nine-action budget. |
| Manual day endings and final-day gating | Live case stays open at zero actions for free reasoning, confirms day closure/replenishment, forfeits unused actions when ending early and rejects accusation before final closure. |
| Closed unsupported case | Fresh walkthrough spends an action on an unproductive theory, closes all days, rejects its accusation and does not reopen evidence acquisition/interviews. |
| Supported final case | Live three-day investigation triggers the existing Mila finale only after final closure and resolves the case. Wrong-suspect selection is rejected. |
| Map tab | JourneyMap.png shows the cyan marker inside the current connection. PlayMode asserts the marker differs from stop positions; final closure displays arrival separately. |
| Reasoning graph | Rule tests verify 186 distinct nodes, every theory's two acquired parents, deterministic positions and search/suspect filtering. Live full-graph fixture displays all 160 theories with their sources; Fit exposes the complete overview. |
| Actual graph interaction | GraphPointerReview.txt records Input System mouse events reaching hover, clue/statement Button handlers, wheel zoom and drag panning. ReasoningPacing.png shows the resulting interface. |
| Spatial context | TrainOverviewPacing.png shows actor/player locations and discovery icons. First-acquisition location/day and legacy unknown provenance are tested; occupancy changes refresh the overview. |
| Symbolic day/action HUD | DayActionHud.png shows day 2/3 with two of three actions remaining and moon/sun action symbols. |
| Stable integration | SceneAudit.txt confirms two installs and scene reload preserve original character prefab/catalogue GUIDs, unique bindings and camera references. Final structural validation reports zero errors. |

Evidence files: PacingEditModeResults.json, PacingPlayModeResults.json, PacingWalkthrough.txt, GraphPointerReview.txt, SceneAudit.txt, Validation.txt and the named screenshots, including FullReasoningGraph.png.

The new BURR_NO_CHAT_01 bark has a stable exact subtitle and assignable AudioClip, but no actual recording was provided. It uses the explicit unvoiced development policy; release validation requires its recording. Actual recorded actor performances have not been verified. The existing prototype has no automatic save system; JSON progress round trips preserve the new pacing/prompt/provenance data.

Unity is left out of Play mode with CarnivalExpress ready to run from its opening. Current setup, balancing and manual QA instructions are in PacingAndNotebook.md.
