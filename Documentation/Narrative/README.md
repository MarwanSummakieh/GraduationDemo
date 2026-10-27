# Murder Most Inconvenient

**Detectives' intuition:** [Motive/Opportunity choices during revealing dialogue, corrections and action costs](DetectivesIntuition.md).

**Accusation update:** collected reasoning nodes show Motive/Opportunity badges. The ACCUSE button is available throughout investigation, including day one; unsuccessful accusations leave investigation open.

**Current environment and dialogue interactions:** [positioned clue markers, spoken clue hints and the dedicated suspect question menu](ClueInteractions.md).

**Current flow:** [Questions, interpretation tags, Motive and Opportunity accusations, and optional action costs](QuestionsAndAccusation.md). This supersedes the older three-theory/Means model. Combine clues and statements into questions for interviews; assign two collected entries as motive and opportunity to a suspect at final accusation. Action costs start disabled. Use F8 for session tuning or edit InvestigationPacing for saved defaults.

The earlier design and verification records remain in this folder as historical notes. The controls and integration notes below predate the current question and accusation flow.
Open `Assets/Scenes/CarnivalExpress.unity` and press Play. The opening begins in Odette's compartment with Burrows and Bellamy. All supplied recordings are pending, so the prototype explicitly displays **DEVELOPMENT — unvoiced** and logs each missing voice ID once per play session.

## Controls and investigation

- A/D and W/S: move through the connected train. E or an inspection button: collect physical evidence or interview a nearby suspect.
- Space/Enter or Continue: advance the spoken dialogue beat. A recorded beat must finish playback before advancing.
- N: notebook. Click a discovered clue and an acquired statement to construct a theory. Right-click a theory to remove it; **Removed** restores it. **Guidance** cycles Off/Subtle/Guided/Directed. Approach a suspect to present an available theory.
- 1–5: Odette, Felix/Mila, bar/lounge, dining, Burrows. Tab: exterior. Escape: close observations/notebook or cancel ordinary dialogue. Opening and finale finish through Continue.
- F8 in the editor/development builds: grant discoveries, construct available pairs, inspect route decisions and flags, and play named sequences.

Construction never confirms a theory or unlocks testimony. A statement enters the notebook only after its supporting dialogue beat completes. Repeated combinations, statements and revelation rewards are deduplicated.

## Architecture and assets

`Assets/Narrative/Source` contains six untouched CSV exports, the original XLSX and `theories.csv` extracted from its Theory Matrix. `Assets/Narrative/Data/Investigation.asset` holds 10 clues, 16 statements, 160 theories, 490 routes and 217 voice definitions. The catalogue inspector offers searchable sections with readable text.

`Assets/Narrative/Runtime` contains immutable serializable definitions, the ScriptableObject catalogue, pure `InvestigationState` rules and structural validation. `InvestigationProgress` holds discoveries, constructed theories, completed encounters and flags. It supports JSON round trips, but this prototype has no save/load UI or automatic persistence because the original project had no save system.

`TrainNarrative` adapts these rules to the existing `TrainTour`, `TrainObservation` and `TrainActor` components. It owns one progress instance and one AudioSource, presents dialogue/notebook UI and reuses character talking animation. `NarrativeInteraction` binds a physical observation to a clue or suspect. Movement, camera, scenery, humanoid rigs and existing environmental observations remain in use.

Confrontations select eligible routes by descending numeric priority, then ordinal route ID. First-disclosure routes have priority 100; the 480 base theory/suspect routes have priority 0. An already disclosed shared revelation skips its first-disclosure route. Completed encounters use the suspect's reusable response without new rewards. Exact CSV generic responses resolve to catalogue voice IDs; missing route definitions use a suspect-specific fallback. F8 shows the selected route and rejected prerequisites.

Each `DialogueBeat` references one canonical `VoiceDefinition`: ID, speaker, exact subtitle, AudioClip, expression cue, MoSCoW priority and recording status. Notebook claims are separate from spoken subtitles. The current rigs support talking/blinking; additional portrait expressions remain metadata for future art integration.

## Refresh and extend

1. Update the writer's ZIP and XLSX together. Use Python with openpyxl to run `AgentScripts/RefreshNarrativeSource.py --zip <export.zip> --workbook <matrix.xlsx>`. This refreshes the versioned source copies and extracts the authored Theory Matrix.
2. In Unity use **Graduation Demo > Narrative > Refresh Catalogue**. The importer stages definitions and validates them before updating the existing asset. Duplicate IDs/pairs, unsupported reward/condition annotations, unresolved references, unreachable statements and invalid routing reject the refresh. The catalogue GUID stays stable.
3. Review `Documentation/Narrative/Validation.txt` and the Console. Use **Validate Scene Bindings** to check the active playable scene.
4. Assign clips in the catalogue's Voices section by stable voice ID. Use the displayed exact subtitle as the recording script. Refresh preserves original and canon-corrected clip assignments, theory proposition metadata, observation groups and arrest rules.
5. Use **Install Playable Investigation** for scene binding/geometry changes. It updates named generated content without replacing existing rooms. Reinstalling preserves the catalogue GUID and character prefab identities.

Source clue, statement, theory, route and dialogue definitions are regenerated; edit their source CSVs rather than relying on Inspector changes to those imported fields. The separate proposition/observation-group metadata and arrest rules are intentionally retained on refresh. Conflicting duplicate routes are rejected instead of resolved silently.

For a new bespoke encounter, add exact lines to `voice_lines.csv`, ordered beats to `spoken_script.csv`, and a route to `routes.csv`. Add state conditions in `unlock_rules.csv` when necessary. Conditions currently accept statement `collected`/`not collected` clauses separated by semicolons. Reward cells accept a statement ID, clue ID or `flag:<name>`, optionally followed by `(first time only)`; initial-interview/opening/finale editorial labels are explicitly mapped. Definition lists also support required/forbidden flags and multiple rewards for future expansion.

Keep acquisition rewards on the spoken beat that supports them, and use shared statement IDs for alternative discovery routes. Preserve the distinction between the catalogue theory and constructed notebook theory. Do not assign a different spoken text to an existing recording ID. New canonical wording gets a new voice ID and recording.

## Accusation rules

The enabled **Physical MVP** rule requires exactly three distinct constructed theories supporting real injury, Mila's presence and contradictory testimony through three independent observation groups. It also requires the injury/clock/desk observations, Mila's original denial, Felix's sighting and Mila's spatial statement. Confidential geometry disclosed prematurely invalidates the knowledge argument. Only Mila can meet the enabled rule; intent to kill is not a proposition.

Theory support is editable metadata, and the evaluator assigns each required proposition to a different observation group. Multiple theories repeating Felix's sighting or Mila's slip do not provide independent corroboration. Examples of supported selections:

- `T_C03_BS01` + `T_C04_FS04` + `T_C10_MS05`.
- `T_C02_BS01` + `T_C09_FS04` + `T_C07_MS05`.

Both play the same supplied static finale, including Mila's admission of the accidental death. Unsupported selections and other suspects receive clearly labeled provisional feedback and return to investigation. Alibi/staging configurations remain disabled pending narrative strengthening.

## Migrations and source issues

- Existing detective and NPC prefabs keep their file paths/GUIDs. Scene overrides map Detective to Basil Burrows, Bubbly passenger to Mila Novak, City boy to Felix Mercer, and Businessman to Edgar Bellamy. Stable `characterId` fields replace role strings as narrative keys. Existing actor objects/rigs are retained.
- Two sleeping carriages are appended behind the original train. Sleeping A houses Felix's luggage and Mila's rehearsal space; Sleeping B houses Odette's body and desk. The original suite remains Burrows's compartment. Camera bounds and five room shortcuts now follow authored room positions. Generated evidence/body/furniture are development placeholders; the clock-position marker refers to the same clock, not another clock.
- All 480 CSV route rows contain statement labels in `Burrows automatic theory`, rather than their theory wording. The importer deliberately uses the workbook's 160 authored texts exported to `theories.csv`.
- The unlock-rule title `A fall she shouldn’t picture` uses a curly apostrophe; the spoken script uses `A fall she shouldn't picture`. An explicit title alias resolves this source inconsistency.
- The alternate desk revelation originally describes a different spatial claim than M-S05. `MILA_CANON_SPATIAL_01` aligns that spoken beat with the shared notebook statement; `BURR_CANON_SPATIAL_01` and `MILA_CANON_SPATIAL_02` update its immediate question and evasive reply to remain coherent. Original lines/IDs stay in the catalogue for audit.
- The supplied finale says Odette's head struck the desk; writer's truth specifies the heavy desk clock. `MILA_CANON_FINALE_01` supplies matching revised subtitle wording. All four corrected IDs need new recordings; old IDs/clips are not reused for changed text.
- Legacy routes occasionally list their own input statement as a reward. Such grants are not treated as a new revelation; dedicated first-disclosure routes make the actual revelations reachable.
- The PDF's example performances differ from the operational voice script. The imported exact voice catalogue supplies the dialogue wording; documented canon overrides resolve the two material conflicts above.
- No audio clips were provided. Structural voice/subtitle references were checked; matching recorded performances cannot be verified until recordings exist. Development builds allow explicitly unvoiced lines; release builds are blocked while any referenced voice has no assigned clip.
- The CSV has no explicit right-hand dagger-placement observation. A successful staging accusation is consequently disabled; no additional canonical evidence was invented.

## Inspector/setup checklist

- The scene's Train exploration object must have TrainTour, TrainNarrative and AudioSource with the imported catalogue assigned. Keep exactly one narrative progress owner.
- Five room cameras and all cutaway shells must remain assigned; four character IDs and ten clue bindings must remain unique.
- Replace Odette's body/evidence placeholders with production art while retaining NarrativeInteraction IDs and reachable inspection positions. The existing human character models are provisional casting; final animal masks are still art work.
- Assign exact recorded takes to their voice IDs, including the four canon-corrected IDs. Check recordings against the subtitle text, then disable development unvoiced mode when production coverage is complete.
- Test cancellation, repeat encounters, both M-S05 discovery routes and supported/unsupported accusations after source updates.
- Narrative sources expose writer-only metadata in editor tools. Do not display truth/reliability/proposition tags as correctness indicators in the player's notebook.

## Verification and deferred work

Unity EditMode/PlayMode results are saved alongside this document. The live walkthrough report and opening/notebook/finale screenshots record the inspected prototype state. Synthetic silent clips test AudioSource binding/advance timing; they do not establish that a real actor's performance matches a subtitle.

Deferred production work: actual VO recording and listening QA, finished masks/body/evidence art, richer portrait-expression presentation, action-budget playtesting, additional endings and stronger alibi/staging accusation data. No new cinematics, procedural dialogue, historical jurisdiction assertions or persistence system were added.
