# Interrogation memory

Open `Assets/Scenes/InterrogationMemory.unity` and press Play. This is a separate copy of the Carnival Express train with the detective's notebook attached. The original scene remains available.

- Walk with **WASD / arrow keys**, then press **E** to talk or inspect. Passenger nameplates also work.
- During conversation, use **1 / 2 / 3** or the topic buttons to ask a question.
- Press **J** or click **Notebook** to review what the detective remembers. **Escape / Return** closes it. J also closes it when you are not typing in a field.
- Filter **All / Testimony / Observations / Pinned**, or search by speaker, topic, response, carriage, or note.
- Select a memory to see its exact response and source. **Pin memory** highlights entries for later review. Write in **Detective's note** to add your own interpretation.

The notebook begins empty. Opening a conversation records its greeting; selecting a topic records that question and response. Inspecting an object records its description. Unasked topics are never added. Repeating the same interaction does not duplicate the entry; changed responses are retained separately. Testimony is labelled unverified. The three existing environmental observations are available as clues, without introducing a culprit or a new crime narrative.

Opening the notebook ends the current conversation and pauses detective movement, carriage shortcuts, and world interaction. The train scenery continues animating. Notebook fields accept normal text input without triggering movement or dialogue keys.

Entries, pins, and notes save automatically to `carnival-interrogation-memory.json` in `Application.persistentDataPath`. This file belongs to the memory scene's investigation. It persists after stopping Play mode and restarting the game. A previous-save `.bak` is kept when replacing a save. Read/write errors appear in the notebook; an unreadable save is preserved before starting an empty notebook. To start a fresh investigation during development, close the game and move this JSON file out of the save directory.

`DetectiveMemory` owns storage and deduplication. `DetectiveNotebook` records interactions and draws the UI. `TrainTour` forwards only the greeting, inspected object, and selected answer. `TrainObservation.memoryId` supplies a stable authored identifier; other scenes can leave it empty to use the hierarchy path. Keep IDs unique per passenger/object. Add `DetectiveNotebook` to a `TrainTour` object to enable the same system in another scene; use a different `saveFileName` for an independent investigation.

Use **Graduation Demo > Create Interrogation Memory Scene** to generate the scene if missing. If it already exists, the command opens it without replacing edits. Use **Graduation Demo > Validate Interrogation Memory** for storage and interaction checks. Validation uses temporary save files and does not change the player's notebook.
