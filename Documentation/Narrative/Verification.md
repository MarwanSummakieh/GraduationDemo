# Final verification — 12 October 2026

Unity 6000.5.8f1 compiled the final code with no compilation errors. Existing/deprecated API warnings remain; missing recordings are explicit development diagnostics.

| Requirement | Evidence |
| --- | --- |
| Reuse existing architecture and preserve asset identities | SceneAudit.txt: repeated install, saved scene reload, original character prefab GUIDs and catalogue GUID preserved; existing TrainTour/TrainActor remain active. |
| Complete authored deduction catalogue | EditModeResults.json: all 160 clue–statement pairs and all three suspect responses resolve; asset contains 10 clues, 16 statements and 490 routes. |
| Acquired discoveries separate from catalogue definitions | Acquisition/construction tests and PlayMode interaction/cancellation tests pass; theory construction alone grants no statements or success. |
| Deterministic routing and once-only revelations | Priority/tie-break, fallback, repeat reward and both M-S05 discovery-order tests pass; all 16 statements are reachable. |
| Spoiler-safe concealed spatial knowledge | Initial-interview/confidential-disclosure validation and accusation disclosure rejection tests pass; alternate dialogue correction is coherent with M-S05. |
| Voice/subtitle architecture | CSV voice and script text/speaker equality checked during import; synthetic clip test verifies matching voice ID, AudioSource binding and playback-completion gate. Missing-recording handling and release gate tests pass. |
| Playable story integration | Walkthrough.txt: opening, all interviews, ten reachable physical discoveries, engagement, sighting, speculation, spatial slip and static successful finale executed. |
| Extended train navigation | PlayMode continuous-walking test and live walkthrough pass across all five carriages; original rear safety boundary moved to the new rear. |
| Supported accusation, multiple valid selections, no redundant corroboration | Two physical builds pass; wrong suspect, missing evidence, duplicated theories and shared observations are rejected. No intent-to-kill proposition is required. |
| Editor refresh/debugging and actionable diagnostics | Staged invalid import leaves the asset unchanged; repeated refresh preserves GUID/references/metadata; searchable catalogue, F8 debug tools, release guard and scene validator provided. |
| Player UI | UIReview.txt records native clue/statement selection, combination, theory-source review and three-theory accusation. OpeningUI.png, NotebookUI.png and FinaleUI.png show the final panels. KeyboardReview.txt verifies Space advances exactly one beat. |
| Tests and designer handoff | 20/20 EditMode and 7/7 PlayMode tests passed. README.md, ManualQA.md and ChangedFiles.md provide architecture, import/extension instructions, migrations, conflicts, setup checklist and deferred work. |

The original project had no save system. The progress JSON round-trip is tested; automatic persistence was intentionally not added.

No actual VO recordings were supplied, so actor performance/subtitle matching is not claimed. The catalogue has 216 definitions including four documented canon-corrected recording IDs; development playback is explicitly unvoiced. Production masks, body/evidence art and recordings remain placeholders. Alibi/staging arrest alternatives remain disabled until their source evidence is strengthened, as agreed in the plan.

Unity was left out of Play mode with CarnivalExpress open. The scene is ready to run from its opening.
