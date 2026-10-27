# World clue markers, clue hints and suspect questioning

Both CarnivalExpress and ThirdPersonTrain use the same interaction system.

## Player flow

- A square **?** marker is anchored to each available clue's inspection point. It says **Approach to inspect** until the detective is within that observation's interaction range. Walk to it and press **E**, or click the marker when it becomes available. Nearby marker labels spread apart to remain selectable; thin lines keep them attached to the actual inspection points. Collection requires proximity at the interaction boundary; the former screen-side list of clue buttons has been removed.
- Talk to a nearby suspect to open a dedicated questioning menu. It lists their unasked alibi and eligible constructed questions, with action costs. Question diamonds and numbers match the reasoning graph; small square clue and circular statement icons identify the source pairing. Scrolling handles a long question list. Close/Escape returns to exploration.
- Used prompts disappear. A new authored follow-up on an already-presented question may appear when its conditions become eligible. Opening/closing the menu does not spend or consume anything. Paid choices retain confirmation; free choices start immediately.
- Some clues start hidden. A completed spoken dialogue beat can reveal a **clue hint**, which activates that clue's world marker and adds a grey **not yet found** square to Reasoning. The entry shows the search context and who mentioned it, but does not reveal the observed fact. It cannot form a question or support an accusation until physically found. Inspection upgrades the same node to collected evidence and records its actual location/day.

## Initial experiment

| Hidden clue | Dialogue cue | Search context |
| --- | --- | --- |
| C05 — Champagne receipt | Felix: “I've got the receipt.” | His place at the bar |
| C08 — Stage blood materials | Mila mentions preparing sticky fake blood | Preparation materials in the dining saloon |
| C10 — Clock behind desk | Mila describes what Odette could reach from where she fell | Behind the desk in Odette's compartment |

The third lead is accessible through the damaged-desk question without needing the hidden clock-position clue first. Dialogue hints award knowledge of a search lead, never physical evidence. Hints deduplicate and survive progress serialization; collecting the clue retains its hint history. Legacy dialogue and route rewards containing clue IDs are also treated as hints, so dialogue cannot bypass physical collection. Restoring a save clears an empty JSON conversation placeholder while retaining a valid in-progress reservation.

## Authoring

- Add a `NarrativeInteraction` with a valid clue ID and a `TrainObservation` at the intended inspection point, then add `ClueMarker`. Set the observation's **Interaction Range** and the marker's world offset. Marker artwork is generic and shared; no clue-specific sprite is required. Existing scene positions and art remain editable.
- In `Assets/Narrative/Source/clues.csv`, set **Hidden until hinted** to `yes` and write **Clue hint** search context. Use `no` for immediately available clues.
- In `spoken_script.csv`, **Clue hints** accepts semicolon-separated clue IDs on the exact spoken beat that reveals them. It does not alter the spoken subtitle or recording ID. Inspector-authored dialogue beats expose the same list.
- **Graduation Demo > Narrative > Refresh Interaction Scenes** imports the catalogue and installs missing components in both scenes, preserving their geometry and inspection positions. Save any open scene edits before running it. **Install Clue Markers and Question Menu** changes only the open scene. Scene builders and runtime initialization also supply missing components.
- Validation rejects unknown hint IDs, missing search context, hidden clues without cues, and circular clue-hint dependencies.

## Verification

Use **Graduation Demo > Narrative > Verify Interactions - EditMode / PlayMode** in the existing editor. Results are written to `ClueInteractionsEditModeResults.xml` and `ClueInteractionsPlayModeResults.xml` in this directory. PlayMode coverage includes both scenes, out-of-range interaction rejection, exact dialogue timing, anticipated-node upgrade, and menu icon/consumption/budget behavior.


### Verified result

- **69/69 EditMode tests passed**, including hidden-clue reachability, hint-only knowledge, legacy reward conversion, and save/load progression.
- **21/21 PlayMode tests passed**, including both scenes, actual suspect-menu initiation, marker-button collection, distance rejection, clustered-marker separation, and grey-node upgrade.
- The current code, source CSVs and scene files matched the tested snapshot by SHA-256 (49 files); catalogue game data also matched.
- Nine UI previews were rendered and reviewed. See [questioning menu](InteractionPreviews/suspect-question-menu.png), [clustered markers](InteractionPreviews/clustered-clue-markers-ThirdPersonTrain.png), and [anticipated clue](InteractionPreviews/anticipated-clue-ThirdPersonTrain.png).
