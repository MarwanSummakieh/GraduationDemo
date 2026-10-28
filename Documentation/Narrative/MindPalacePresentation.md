# The train and the mind palace

The train remains the tangible investigation: masked guests, compartments, props and testimony. Reasoning, intuition and doubt defense use an inner visual language of branching neurons, violet depth, cyan electrical impulses and coral intrusive doubts. The distinction is about the detective's experience, not an additional source of evidence or a guarantee that an interpretation is correct.

## Reasoning and defense

The graph viewport and defense board share a drifting, procedural neural field. Graph nodes retain their clue / statement / question shapes and interpretation badges, with dendrites around their cores. Their parent links carry jagged electrical paths and travelling impulses; doubt connections use coral. The existing graph coordinates, draggable nodes, midpoint doubt buttons, source filtering and resolved indicators continue to work.

Defense constructs use neural cores and dendrites, linked by violet axons. Shots travel as cyan electrical discharges with a bright impulse head. Incoming doubts have an unstable coral neuron silhouette. The central question reads as the thought being protected. Category placement, continuous positions, valid wall topology, routing, focus, pressure, hazards and success conditions retain their existing behavior.

`NeuralInk` supplies shared geometry, `NeuralFieldGraphic` supplies the backdrop, and `NotebookStrokeGraphic` / `DoubtBoardGraphic` supply state-specific connections and impulses. Backgrounds never receive input. Link subdivisions are bounded so the complete current case fits uGUI's mesh vertex limit.

## Dialogue experiment

The dialogue presentation uses original angular cutouts, a warm white text hierarchy, coral edges for spoken conversation, and cyan / violet for internal thought. It takes broad inspiration from the theatrical contrast and composition of Persona 5 rather than using its artwork or UI assets. The real train stays visible above the dialogue.

Each of Burrows, Felix, Mila and Bellamy has a distinct procedural placeholder portrait. The active speaker occupies the large left cutout; a subdued listener appears on the right. A short entrance, subtle breathing / tilt, blinking and a small mouth animation indicate the active speaker. Text appears in full, without a typewriter gate.

For a detective's inner thought, the listener disappears, a neural emblem appears beside Burrows, and the frame / context label switch to **INNER VOICE / THE DETECTIVE'S MIND**. This happens for solo detective sequences and for explicitly authored thought lines within mixed conversations. The opening `BURR_001` line is marked as a thought. `VoiceDefinition.innerThought` can be edited in the catalogue; reimport preserves the author's choices. Subtitles, clips, rewards and story routes are unchanged.

Intuition uses an upper neural card, separate from the portraits and spoken subtitle. Motive / Opportunity choices and correction continue through the existing narrative API. A wrong answer leaves the correction visible until Continue; a correct answer retains the existing focus reward and progression. The old IMGUI dialogue block has been replaced by uGUI; debug tools and other investigation overlays remain separate.

`DialoguePresentation` follows the current voice and conversation state and builds the uGUI layout. `DialoguePortraitGraphic` owns only placeholder artwork and light animation, so finished portraits can replace that presentation without changing dialogue rules. Continue respects voice playback and the existing input guard. The dialogue layer hides while the notebook is open and when the sequence ends.

## Motion and tuning

**Reduced Presentation Motion** in `InvestigationPacing` freezes decorative neural movement, electrical jitter / travelling graph pulses and portrait movement / blinking, as well as the existing defense motion reduction. Static links, silhouettes, text and combat feedback remain. Defense volume and the runtime sound toggle retain their existing settings.

These are code-generated placeholder visuals with no external images, fonts, audio licences or animation packages. The current game's built-in runtime font is retained.

## Review

Unity 6000.5.8f1 verification passed **105/105 EditMode tests** and **34/34 PlayMode tests**: [rules and importer](MindPalaceEditModeResults.xml), [gameplay and presentation](MindPalacePlayModeResults.xml). The gameplay suite checks all four speaking portraits, solo thought and authored thought inside a mixed scene, reduced motion, subtitle fidelity, hiding at the end of dialogue, both playable scene integrations, intuition correction / focus rewards, a full-case graph mesh, and real defense impulses. Existing interview, accusation, exploration, focus and defense tests remain part of the regression pass. All Narrative / Train C# sources match the tested snapshot.

Reviewed previews:

- [Burrows's inner voice](InteractionPreviews/mind-palace-inner-voice.png)
- [Felix](InteractionPreviews/mind-palace-dialogue-Felix.png), [Mila](InteractionPreviews/mind-palace-dialogue-Mila.png), [Bellamy](InteractionPreviews/mind-palace-dialogue-Bellamy.png)
- [Authored opening thought in ThirdPersonTrain](InteractionPreviews/mind-palace-dialogue-ThirdPersonTrain.png)
- [Intuition choice](InteractionPreviews/mind-palace-intuition-choice.png), [correction](InteractionPreviews/mind-palace-intuition-correction.png)
- [Synaptic defense discharge](InteractionPreviews/mind-palace-synaptic-discharge-CarnivalExpress.png), [ThirdPersonTrain](InteractionPreviews/mind-palace-synaptic-discharge-ThirdPersonTrain.png)
