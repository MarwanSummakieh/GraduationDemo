# Carnival Express

The current prototype includes the five-carriage Murder Most Inconvenient investigation. See [narrative setup and architecture](Narrative/README.md) and [manual QA](Narrative/ManualQA.md) for current controls and playable content. The notes below describe the original three-carriage environment baseline.

Open `Assets/Scenes/CarnivalExpress.unity` and press Play.

- **A / D or left / right:** walk along the train. **W / S or up / down:** move closer to or away from the benches.
- **1 / 2 / 3:** visit the lounge, dining saloon or private suite.
- **Tab:** switch between the interior and the enclosed exterior.
- **E:** talk to nearby NPCs or inspect set dressing. Click a passenger's **Talk** nameplate or the nearby interaction button as an alternative.
- **1 / 2 / 3 during conversation:** select a dialogue topic. These keys visit carriages when no conversation is open.
- **Escape / Goodbye:** close the conversation or observation.

The complete enclosed train is reusable at `Assets/Train/Prefabs/CarnivalExpress.prefab`. The scene instance opens the near walls and roofs for readable interior compositions; exterior view restores them. The three passenger carriages connect through open, walkable vestibules. The locomotive is scenery rather than an investigation area.

The scene interprets the GDD's whimsical train journey to a carnival, primarily 3D spaces, simplified expressive modeling, theatrical colour, animal-mask motif and constrained camera. Carriage layout, locomotive style, furnishings and palette are proposed art direction because the document does not specify them. No suspect, culprit, murder method or evidence logic has been made canon. The three inspectable objects provide environmental observations rather than collectible case evidence.

Train geometry, materials and lights are editable Unity objects. The detective and three NPCs use the installed **Hodaart Casual Character Collection 3**, including its body and facial animation controllers. Character 06 is the detective; 03 is the bubbly passenger in the lounge; 02 is the city boy in the dining car; 04 is the businessman in the suite. These casting choices and short greetings are provisional. They add no alibis or case evidence.

The detective changes between idle and walking animation with actual movement. The lounge passenger and businessman sit on velvet benches; the city boy sits in a new dining booth. The existing humanoid rigs receive a seated pose with anchored hips, bent knees, feet on the floor and hands on laps. Their upper-body idle and facial controllers remain active, with blinking, head turns and talking animation. Root motion is disabled so animation cannot move characters through furniture or carriage walls. Four reusable character prefabs are saved alongside the train prefab; the original package assets are preserved.

Each NPC has a greeting and three ambient conversation topics. Dialogue pauses detective movement and keeps the passenger visible above a bottom dialogue panel. The dialogue makes no claims about murder alibis or case evidence.

The interior camera uses an orthographic side-on cutaway with a slight downward angle, inspired by the readable dollhouse framing of Neighbours from Hell. It scrolls horizontally with a small dead zone instead of rotating or cutting between diagonal room views. Foreground trees are hidden during exploration. Framing expands for narrow Game views to keep a carriage readable.

The landscape and sleepers loop during Play mode; the train stays at the origin for stable exploration and framing.

To rebuild, use **Graduation Demo > Build Carnival Express**. This replaces the generated scene, prefab, materials and atmosphere profile; save any custom edits separately first. **Graduation Demo > Validate and Capture Train** checks material assignments and the continuous aisle, then writes four renders to `Documentation/TrainPreviews`.

**Graduation Demo > Install Package Characters** updates only the cast in the open train scene. The main train builder also includes the cast on future rebuilds.

**Graduation Demo > Seat Passengers and Set Side Camera** applies the bench layout, topics and side camera to the existing train scene.

This deliverable covers the train environment and exploration, not the GDD's full deduction game, dialogue, action economy, voiced audio or illustrated story sequences.
