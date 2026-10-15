# Third-person train

Open `Assets/Scenes/ThirdPersonTrain.unity` and press Play. It is the first scene in Build Settings.

- **WASD / arrows:** move relative to the active perspective camera.
- **C / camera button:** toggle fixed corners and the detective follow camera.
- **Hold right mouse button in follow mode:** orbit the detective.
- **E:** talk or inspect; **1 / 2 / 3:** visit the three carriages.
- **N:** open the shared investigation logbook; **Escape:** close it.
- **Space / Enter:** advance the opening and other dialogue.

There are exactly seven Camera components: left and right corners for each of the lounge, dining saloon and private suite, plus one shared detective camera. Fixed mode starts enabled. Left/right means position along the train's X axis relative to the carriage centre: the left half selects the right corner, and the right half selects the left corner. A small centre dead zone prevents flickering. Carriage transitions have their own dead zone. Fixed cameras never track or rotate. Only the selected camera and its AudioListener are enabled.

Movement keeps its camera-relative direction while a key is held across a fixed-camera cut; releasing movement adopts the new view. Follow mode remains enabled when entering another cart. The follow camera uses a collision-tested boom and right-drag orbit. The carriage walls and roofs stay enabled in both modes. Dialogue and the notebook pause movement; the notebook also blocks camera controls and carriage shortcuts.

The scene uses the colleague-authored `TrainNarrative`, `InvestigationState` and `InvestigationNotebook` for clues, testimony, reasoning, conversations and accusations. The older `DetectiveNotebook` / `DetectiveMemory` implementation, its validators and the `InterrogationMemory` scene have been removed. Existing player JSON files are not deleted or converted.

The source `CarnivalExpress` investigation has five carriages and remains available. This three-carriage version consolidates its ten clues into the existing lounge, dining saloon and suite; Mila, Felix and Bellamy occupy their original three-carriage seats. The logbook's train view uses this scene's three rooms, and finale staging uses the last carriage. Narrative text, rewards and deduction rules are shared with the colleague's implementation. Source dialogue still refers to the story's original compartments.

Use **Graduation Demo > Create Third Person Train Scene** to create the scene if missing, or open it without overwriting edits if present. The saved scene is fully authored, with editable camera placements and rig settings; nothing is generated on entering Play mode.

Use **Graduation Demo > Validate Third Person Train** to run the play-mode checks. Results and nine camera renders are written to `Documentation/ThirdPersonTrain/`. The batch entry point is `GraduationDemo.Editor.ThirdPersonTrainValidation.Run` (omit `-quit`; the harness exits on completion).

Validated on Unity 6000.5.8f1: all 59 existing edit-mode tests and 19 existing play-mode tests passed. The new scene's dedicated play-mode validation also passed, including all seven cameras, input isolation, physical traversal, inventory interactions, the three-room notebook view, and finale completion. Fixed and following camera renders were visually inspected.
