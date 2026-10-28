# Authored doubts and accumulated pressure

The current visual presentation is described in [The train and the mind palace](MindPalacePresentation.md): electrical reasoning links and defense discharges, surreal neural backgrounds, and animated portrait dialogue.

## Story-authored connections

A doubt connects one **existing question** to one **collected clue, statement or active question** that presents a specific potential contradiction. Question–question links appear only when both questions have actually been constructed, in either order. They are directional: a counter-question can challenge another question without itself becoming insecure. No generic doubt is generated for every question or every pairing. The writer authors `id`, `questionId`, `sourceId`, `reason` and optional positive `weight` in **Doubt Links** on `Assets/Narrative/Data/InvestigationPacing.asset`. A link cannot target itself.

The reasoning graph draws the contradiction as a pink connection, with its selectable doubt node exactly at the midpoint between question and source. Hover or select it to read the actual conflict and defend the question. Search preserves the contradicting source whenever the question is visible, so filtering cannot hide the route to defending an insecure question.

Current authored cases use existing story material:

| Question | Counter-source | Potential contradiction |
|---|---|---|
| T_C02_MS04: could Odette have broken the clock herself? | C10 | The clock is displaced behind the desk rather than in its usual position |
| T_C02_MS04 | M-S05 | Mila says Odette could not reach it from where she fell |
| T_C02_MS04 | C07 | Impact damage and displaced furniture challenge a routine accident explanation |
| T_C09_MS01: Mila's solitary rehearsal alibi | F-S04 | Felix places her near Odette's compartment at 20:45 |
| T_C04_FS04: accepting Mila's presence near the scene | M-S01 | Mila says she was rehearsing alone elsewhere |
| T_C03_MS04: could clumsiness explain the injury? | C07 | A fall alone may not explain the disturbance at the desk |
| T_C01_BS04: mistaking death for theatrical stabbing | C03 | The actual head/neck injury needs explaining separately from the staged stabbing |
| T_C06_BS03: linking Bellamy's argument to the marriage | M-S06 | Mila's professed goodwill raises whose reaction this line of inquiry establishes |
| T_C02_MS04: accidental clock damage | T_C02_BS01 | Ordinary clumsiness versus the sharp noise and crash: can one sequence explain both? |
| T_C09_MS01: solitary rehearsal | T_C04_FS04 | Private rehearsal versus presence near the scene before discovery |
| T_C03_MS04: accidental injury | T_C02_MS05 | Does an accident explain Mila's detailed knowledge of the fall? |

These are reasons to examine a question, not automatically true conclusions. Witness claims remain witness claims. The rules deliberately avoid requiring a revelation to defend the very interview needed to obtain that revelation. No new dialogue or recordings are required.

## Interview readiness

A question with an encountered, unresolved authored doubt is **insecure**. Its suspect-menu card remains visible but disabled with **Needs doubt defense in Reasoning**. Notebook details explain the lock. Presentation, initialization and TrainNarrative entry points all check readiness; a stale paid confirmation cannot bypass a new contradiction or spend actions. Alibis and unrelated questions remain available.

A defense succeeds only when every invader is intercepted: zero breaches. Success automatically unlocks affected questions with no reflection step. Failure, acknowledgement, revision or merely finishing a run does not unlock them. A defended question has a green **OK** badge in Reasoning (the legend explains **doubt resolved**), a matching status in its selected details, and a green **Doubt resolved** label in the suspect menu. The active conversation also displays its resolved status. Untouched secure questions do not claim to have been defended. A new authored counter-source collected later makes the question insecure again and removes the resolved indication. Early accusation and its optional review retain their existing behavior; this readiness gate controls suspect questioning.

## One accumulated challenge

**Doubt pressure** is the sum of weights of encountered authored connections that have not been defended. It rises only when both a target question and its counter-source are known. Unrelated evidence does not add arbitrary pressure. The notebook header and board show the total.

Selecting any unresolved connection opens a defense of that question, but **one run handles the current accumulated backlog together**. On Test defense, the run captures all pending link IDs and its difficulty. A successful run clears that captured batch across its affected questions. It does not clear later contradictions that were outside the batch. This avoids a mandatory separate encounter for every question or link.

Pressure is persistent, not a daily action expenditure. Archiving a question hides its graph connection and preserves the question/history, but cannot erase already encountered pressure. A batch can clear those archived connections too, so restoring the question does not require replaying an already successful defense.

This also applies to archived counter-questions. Restore one from **Removed** to expose its connection again, or clear the batch through another visible doubt. An insecure question's details explain when its counter-question needs restoring. Search filtering retains counter-questions and their source parents, so their connections remain readable.

## Difficulty and environmental effects

### Focus from exploration and intuition

The bar/lounge has a clearly marked **coffee stand**. Approach it and press E or click its marker to gain **+2 focus**, once per investigation. It costs no daily actions and adds no murder evidence. The prop is created in both playable train scenes when needed; an existing placed `FocusCoffeeStand` is reused for authored scenery. The marker disappears after collection, while the stand remains in the environment.

Each distinct testimony correctly interpreted as Motive or Opportunity also grants **+1 focus**. Wrong answers grant none. Rewards work when action costs are disabled and cannot be duplicated by replaying, retrying or recovering a conversation.

These are persistent additions to the tower capacity: base Focus Budget + earned bonus. They survive day changes and progress serialization; new investigations start fresh. The notebook header and defense board show the resulting capacity, and reward notices explain the gain. Removing a tower still frees its deployment cost.

Tune **Coffee Focus Reward** (default 2) and **Intuition Focus Reward** (default 1) on the pacing asset. Setting an amount to zero disables that reward. Both values must be nonnegative. Serialized `bonusFocus` and `focusRewardClaims` preserve capacity and prevent duplicate claims.

Verification for these rewards: **100/100 EditMode tests** and **29/29 PlayMode tests** passed ([EditMode results](FocusRewardEditModeResults.xml), [PlayMode results](FocusRewardPlayModeResults.xml)). Tests include actual extra tower deployment, coffee reach checks and both train scenes, intuition correctness/recovery, progress restoration and day transitions. Previews: [coffee stand](InteractionPreviews/coffee-focus-CarnivalExpress.png), [increased defense capacity](InteractionPreviews/coffee-focus-board-CarnivalExpress.png).

Planning remains untimed, placement and enemy routes remain continuous, and **Test defense** runs automatically to an outcome. Focus is a separate tower budget: clues are Shot towers (1), statements Slow towers (1), and questions Buff towers (2). Removing towers frees focus. Failed attempts can be rearranged and retried without spending daily actions.

Let `L = min(pressure, Max Difficulty)`. Defaults cap L at 6:

| Pressure level | Waves | Obstructions | Enemy haste areas | Tower weakness areas |
|---|---:|---:|---:|---:|
| 1 | 1 | 0 | 0 | 0 |
| 2 | 1 | 1 | 0 | 1 |
| 3 | 2 | 1 | 1 | 1 |
| 4 | 2 | 2 | 1 | 2 |
| 5 | 3 | 2 | 1 | 2 |
| 6+ | 3 | 3 | 2 | 2 |

Waves are also capped by **Max Waves** (default 4); there are **Enemies Per Wave** (3), with **Spawn Interval** (3 pulses) and **Wave Pause Pulses** (4). Counts are deterministic and capped; pressure never creates an unlimited grind. Repeated invaders represent pressure from the captured contradictions, not additional story revelations.

Grey obstructions block tower placement, walls and enemy movement. Red areas increase invader speed (default ×1.3). Amber areas reduce tower range/effect and projectile damage (default ×0.7). Selected towers show when they are weakened. Environmental geometry is previewed before starting, and navigation/wall validation uses that same geometry. Shortest automatic walls still reject cycles/crossings and preserve paths from every entrance.

The default six-focus budget has a verified winning mixed formation at both ordinary accumulated pressure and the capped maximum. This is a balance baseline, not a requirement to discover one exact placement: all positions remain free and retries are free.

## Controls and tuning

Main board: defended question, a Focus meter, Doubt, live wave/breach counters, three tower categories, Test defense, Details, Remove tower and stop/rearrange. **Shot / Slow / Buff** replace the paginated individual-source inventory. Select a category and click a free position; repeat to place another unused material from that category. Related material is used first, then other collected material in stable ID order. Each collected node still supplies at most one active tower. Cards show costs and remaining materials and disable when unaffordable. Click an existing tower to select it for moving/removal; Details identifies its underlying evidence. Positioning remains continuous, without snapping.

Long contradiction text, source claims, rejected-wall reasons and optional related-question suggestions stay in Details. After a result, **Back to reasoning** closes the defense and returns directly to the normal graph. There is no reflection form or free-text field. Successful defenses are already recorded automatically.

### Presentation and feedback

The board uses procedural uGUI geometry for animated projectile trails, impact rings and fragments, placement bursts, invader defeat bursts, spawn pulses, a breathing central shield, and a colored completion burst. Invaders interpolate between simulation pulses; attack events remain tied to actual combat effects. Slow/buff towers show subtle animated area rings, distinct silhouettes, and their existing range/link previews. Environmental hazards have additional cross/arrow markings. Breaches briefly shake board geometry. These transient effects do not change combat or saved progress.

`DefenseFeedback` owns a separate 2D AudioSource and lazily generated, cached synthesized sounds for placement, removal, launch, shots, spawning, defeats, breaches, invalid placement and outcomes. No external audio or texture assets are required. Dialogue retains its original AudioSource. The board has a **Sound: on/off** toggle; **Defense Sound Volume** in the pacing asset controls level (zero disables it), and **Reduced Presentation Motion** disables decorative movement, interpolation, fragments and shake while retaining static markings and fading feedback. It also reduces neural and portrait animation. The runtime mute preference lasts for the current controller session.

The walkthrough checks the return button, absence of the reflection form, resolved badges in both scenes, suspect-menu and active-dialogue status, and removal of the badge when new doubt appears. Previews: [reasoning indicator](InteractionPreviews/resolved-question-graph-CarnivalExpress.png), [dialogue menu](InteractionPreviews/resolved-question-dialogue-menu.png).

In **Doubt Tuning**, adjust focus, wall radius, shot range/damage, slow/buff ranges and strength, invader health/speed, automatic pulse interval (**Step Seconds**, default 0.22), waves, pauses, difficulty cap and environmental multipliers. Each new session uses a cloned configuration. **Doubt Links** is the current authoring list; older hidden question-only template data is retained for compatibility and no longer generates doubts.

## Persistence and verification

Unity 6000.5.8f1 verification: **104/104 EditMode tests** and **31/31 PlayMode tests** passed. Results: [EditMode](DefensePresentationEditModeResults.xml), [PlayMode](DefensePresentationPlayModeResults.xml). The PlayMode suite exercises both train scenes, the real disabled/enabled interview cards, category placement with distinct collected materials, exact free coordinates, independent dialogue/defense audio sources, sound muting, an automatic winning formation, and filtered question–question connections. EditMode also covers either construction order, saved progress, and restoring an archived counter-question. Presentation is transient and has no influence on the deterministic combat outcome.

Presentation previews: [category palette and placement effects](InteractionPreviews/defense-categories-and-effects.png), [animated run](InteractionPreviews/defense-presentation-running.png), [successful defense](InteractionPreviews/defense-category-victory.png).

Reviewed previews: [accumulated defense](InteractionPreviews/authored-doubt-planning-CarnivalExpress.png), [narrow board](InteractionPreviews/authored-doubt-planning-narrow-CarnivalExpress.png), [automatic run](InteractionPreviews/authored-doubt-running-CarnivalExpress.png), [locked interview](InteractionPreviews/authored-doubt-locked-interview.png), [successful unlock](InteractionPreviews/authored-doubt-success-CarnivalExpress.png).

InvestigationProgress saves encountered/defended link IDs, pressure inputs, drafts, captured batch IDs, difficulty, environment, exact tower/enemy coordinates and reflections. Restore through the existing InvestigationState progress boundary. Older run geometry returns to planning while retaining placements and history. Configuration changes do not invent missing evidence.

Tests cover authored eligibility and source IDs, midpoint graph links, unrelated questions remaining free, dialogue gates and stale paid confirmations, failed defenses, batch unlocks across multiple questions, new evidence relocking, persistence, archive pressure, actual environmental modifiers and collision geometry, capped difficulty with a winning default-cost formation, and both-scene UI walkthroughs including asking an unlocked question.
