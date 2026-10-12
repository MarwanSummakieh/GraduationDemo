# One-use conversations, notebook graph and daily pacing

## Play and balance

Open CarnivalExpress and press Play. An alibi or a constructed theory is the only ordinary way to start suspect dialogue. Clicking a nearby suspect lists available prompts; if there is no affordable viable prompt, Burrows gives his no-chit-chat bark. Physical inspections and environmental observations remain separate from suspect dialogue.

Default pacing is **three days, three actions per day**. Alibi interviews, walking, clue collection and theory construction are free. Each new theory confrontation costs one action. Edit `Assets/Narrative/Data/InvestigationPacing.asset` to change days, actions, interview/confrontation costs and the fictional route's stop labels/normalized map positions.

A paid prompt always asks whether to spend the displayed action cost. Cancel spends nothing and consumes no prompt. Confirmation checks eligibility, proximity, playback assets and budget again before reserving the prompt and spending. Once dialogue starts, finish its lines using Continue/Space/Enter; Escape no longer cancels ordinary conversations. Unexpected playback failures release the reservation and refund its action, preserving testimony already heard.

Alibis are one-use per suspect. Theory presentations are one-use per theory, suspect and selected route. A genuinely new conditional authored response may become available later; an additional route cannot replay the same sequence for an already-used pair. Different theories may legitimately use the same exact generic voice recordings. Shared revelations suppress redundant alternative discovery prompts.

At zero actions, the current day stays open for free investigation and reasoning. **End day** asks for confirmation, explains forfeited unused actions and replenishes the next day. Ending early is allowed. Only confirming the end of the final day opens final accusation construction. Afterwards the player may construct new theories from collected clues/statements, review discoveries and revise an accusation, but cannot acquire new evidence or interview suspects. The [accusation graph](FinalAccusationGraph.md) replaces the old selection list: connect three theory-to-accusation tethers, connect the accusation to a suspect node, then confirm. An unsupported accusation remains editable without reopening investigation; a supported one retains the static Mila finale.

The core engagement/sighting/speculation/spatial-slip walkthrough uses four paid encounters and fits the default nine-action budget. All nine unique revelation encounters also fit when actions are not wasted. Tighter settings are permitted for balancing and can intentionally leave an unresolved case.

## Three notebook tabs

- **Map:** a stylized European outline and editable fictional stop route. Day/action progress determines the current connection; the cyan train marker remains inside that connection rather than on a stop. After final closure it becomes an arrival marker. Stops and investigation days remaining are displayed.
- **Reasoning:** acquired clue squares at the left, statements grouped by suspect at the right, and authored theory diamonds in the middle. Click one clue and one statement in either order to create a theory. Each pairing starts fresh, preventing an old selection from creating unintended theories. Hover icons for details; click a theory for its wording and source observations. Drag to pan, scroll to zoom, use Fit for an overview, and search/filter to navigate a large graph.
- **Train:** horizontally scrollable carriage cards show current character/player locations and collected clue/statement icons. Hover for acquisition context and select a carriage to read its collected observations. This view is informational and does not teleport the player. First-acquisition location/day is retained; legacy/debug data with unavailable provenance is explicitly marked unknown.

The graph uses a deterministic layout constrained to the three columns, with suspect groups and theories placed near their parent-line intersection with the central column. Crowded theories shift into nearby free positions without overlapping icons or captions. Existing nodes animate toward their new positions as the network grows. Zoom/pan survive selection and discovery updates; reopening or changing filters starts a fresh framing. At small view sizes the initial zoom favors usable icon hit targets; Fit exposes the complete network and users can zoom back into individual nodes.

## Architecture and migration

`InvestigationConfiguration` is a separate ScriptableObject so imports do not overwrite balancing settings. `InvestigationProgress` now holds phase/day/action state, consumed initialization and sequence keys, a current reservation and discovery provenance. Availability, spending, day closure and accusation gating are enforced in `InvestigationState` rather than only in UI.

`TrainNarrative` adapts these rules to existing train interactions and voice playback. `InvestigationNotebook`, `NotebookGraphics` and `ReasoningGraphLayout` supply the uGUI view, procedural icons/connections and deterministic layout. The superseded notebook list UI was removed. One existing narrative progress owner remains; the notebook adds an Input System UI EventSystem only when the scene has none.

The scene installer reuses the original scene/character/catalogue identities and assigns the pacing asset and notebook component. CSV refresh preserves the new bark's clip assignment. `BURR_NO_CHAT_01` is a new exact-text Burrows voice definition: **“Chit-chat? A waste of valuable time.”** No recording was supplied, so it follows the existing explicit unvoiced-development policy. Release validation includes this bark.

The existing prototype still has no automatic save system. Progress JSON round trips now preserve pacing, consumed prompts and provenance; restored discoveries without location metadata migrate to unknown provenance.

## QA additions

1. Interview each suspect once for free; repeat the click before constructing theories and verify only Burrows's bark plays.
2. Offer a theory, cancel confirmation, then offer again. Verify no spending before confirmation and exactly one deduction after confirming. Finish all lines; the initialization must disappear.
3. Obtain M-S05 through either route; the other discovery route must not replay or spend another action.
4. Spend all daily actions. Verify free inspection/reasoning still works and the day does not advance until confirmed. End a day early and verify unused actions are forfeited.
5. Attempt accusation before the final day closes, then after closure. Only the latter may construct a final accusation. Check insufficient evidence stays unresolved without reopening investigation.
6. Check all three tabs, between-stop marker placement at day boundaries, acquisition-location records, graph hover/select, pan/zoom, search/suspect filtering and a developer-seeded 160-theory graph.
7. Test a missing recording before confirmation: no action or prompt should be consumed. If a recording disappears during playback, verify recovery allows retry without duplicate discoveries.

Current test reports and live evidence are `PacingEditModeResults.json`, `PacingPlayModeResults.json`, `PacingWalkthrough.txt`, and the accompanying graph/map/train/HUD screenshots. Earlier verification documents describe the previous repeat-enabled, ungated notebook behavior and are historical baseline evidence.

## Theory management and recording cues

Right-click a theory icon or caption to take it and its connections out of the active graph. **Removed** opens a restore list. Evidence, statements and consumed conversation prompts are retained; restoring never replays a used encounter. Previously removed theories can also be restored during final accusation review.

When a completed dialogue beat grants a new clue or statement, a five-second **NOTED IN LOGBOOK** notice identifies the new entry. Flags and already-known discoveries do not produce duplicate notices, and the cue does not mark testimony as verified truth.
