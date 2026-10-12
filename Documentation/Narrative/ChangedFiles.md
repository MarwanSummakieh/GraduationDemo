# Delivery file inventory

## Subsequent pacing/notebook update

- New `InvestigationConfiguration.cs` and `InvestigationPacing.asset`: editable days, action counts/costs and journey stops.
- `InvestigationState.cs`: consumed initialization/sequence keys, atomic spending/reservations, failure recovery, day phases, accusation gating and first-discovery provenance.
- New `ReasoningGraphLayout.cs`, `InvestigationNotebook.cs` and `NotebookGraphics.cs`: three-tab uGUI notebook, deterministic animated graph, pan/zoom/filter/hover, between-stop map marker and live carriage/provenance overview.
- `TrainNarrative.cs`: suspect prompt choice, paid confirmation, compulsory completion, Burrows bark, day HUD/confirmation; obsolete notebook list UI removed.
- Importer/build guard/scene installer: stable bark definition, release recording coverage and pacing/notebook assignments without replacing the existing catalogue/prefab identities.
- New `PacingAndNotebookTests.cs`, expanded PlayMode tests and `PacingWalkthrough.cs`: repeat/action/day/provenance/graph coverage and supported/unsupported three-day walkthroughs.
- `PacingAndNotebook.md`, pacing test reports, walkthrough report and graph/map/train/HUD images: current setup and QA. The inventory below describes the original narrative integration.

## Existing files adapted

- `Assets/Scenes/CarnivalExpress.unity`: two sleeping carriages, connected vestibules, relocated rear boundary, five camera views, evidence bindings, suspect identity/prefab overrides, narrative controller and AudioSource.
- `Assets/Train/Scripts/TrainTour.cs`: narrative interaction adapter, modal dialogue/notebook movement pause, five-carriage navigation and camera bounds; existing interaction/camera behavior retained.
- `Assets/Train/Scripts/TrainActor.cs`: stable narrative character ID, retaining the existing rig and talking/blinking behavior.
- `Assets/Train/Editor/CarnivalTrainBuilder.cs`: installs the playable narrative after a generated environment rebuild.
- `Assets/Train/Editor/TrainCharacterInstaller.cs`: protects narrative layout from ambient-topic resets and reinstalls narrative bindings after cast replacement.
- `Documentation/CarnivalExpress.md`: points readers to current investigation instructions.

## New runtime and editor files

- `Assets/Narrative/Runtime/NarrativeCatalogue.cs`: narrative definitions, canonical voice text, routing and editable accusation rules.
- `Assets/Narrative/Runtime/InvestigationState.cs`: separate player progress, acquisition, deterministic theory construction, route selection and independent-evidence evaluation.
- `Assets/Narrative/Runtime/NarrativeValidation.cs`: IDs, complete pair/suspect coverage, rewards, prerequisites, reachability, confidential information and accusation references.
- `Assets/Narrative/Editor/NarrativeImporter.cs`: staged CSV import, explicit source corrections and reference-preserving refresh.
- `Assets/Narrative/Editor/NarrativeCatalogueInspector.cs`: searchable designer-facing catalogue sections.
- `Assets/Narrative/Editor/NarrativeBuildGuard.cs`: structural checks and missing-recording release gate.
- `Assets/Train/Scripts/TrainNarrative.cs`: ordered dialogue/audio presentation, acquisition beats, notebook, confrontation/accusation UI and development debugging.
- `Assets/Train/Scripts/NarrativeInteraction.cs`: existing observation-to-evidence/suspect bindings.
- `Assets/Train/Editor/NarrativeSceneInstaller.cs`: idempotent scene integration and scene-reference validation.
- Narrative runtime/editor assembly definitions isolate pure deduction and import code from the existing default train assembly.

## Data, tests and verification

- `Assets/Narrative/Source`: original CSV exports/XLSX plus workbook-derived `theories.csv`.
- `Assets/Narrative/Data/Investigation.asset`: generated immutable catalogue; clip assignments and evidence metadata survive refresh.
- `Assets/Narrative/Tests/Editor/InvestigationTests.cs` and its assembly definition: acquisition, pair coverage, routing, revelations, independent accusations, invalid/refreshed imports, confidentiality and release-recording guard tests.
- `Assets/Narrative/Tests/PlayMode/PlayableInvestigationTests.cs` and its assembly definition: opening, continuous walking route, interactions, cancellation, full ending and audio handling.
- `AgentScripts/RefreshNarrativeSource.py`: refreshes source copies and extracts the workbook theory matrix.
- `AgentScripts/NarrativePipeline.py`: local Unity automation requests; reads the local token privately.
- `AgentScripts/NarrativeSceneAudit.cs`: repeated-install, GUID and scene-reload validation.
- `AgentScripts/NarrativeWalkthrough.cs`: live reachable-evidence/interview/revelation/finale walkthrough and actual Game view UI captures.
- `Documentation/Narrative`: architecture/setup, manual QA, source conflicts, test reports, validation reports, scene audit, live walkthrough and visual captures.
- Unity-generated `.meta` files accompany all new assets, scripts and folders.

Existing unrelated Hodaart/package-lock/local editor changes were present before this task and were not reverted. Original character and environment prefab GUIDs were preserved; migration is implemented with stable IDs and scene overrides.
