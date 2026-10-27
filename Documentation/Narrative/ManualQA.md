# Current environment-interaction QA

See [clue markers, hints and suspect menu](ClueInteractions.md) for authoring and the three hidden-clue experiments.

1. Play each scene. Approach a visible square question-mark marker and inspect with E or its button. Check that distant markers cannot collect evidence and nearby labels stay separate around Odette's clustered clues, with lines pointing to their world positions. Collected markers disappear.
2. Find Felix and open his questioning menu. Check that the unasked alibi appears, and that constructed questions use the same diamonds and numbers as Reasoning, with their source icons. Opening/closing consumes nothing. Used prompts disappear; unaffordable choices remain disabled when costs are enabled.
3. Complete Felix's receipt line. Check that a clue-hint notice appears, C05 becomes a grey anticipated node, and its marker becomes available at the bar. The receipt fact and evidence remain unavailable until inspection. Collect it and check that the same node becomes normal, with actual collection provenance.
4. Repeat with Mila's fake-blood line (C08) and her clock/fall detail (C10). Check that each lead points to its authored search area and cannot be combined or used for accusation while unfound.
5. Run the save/load regression test for progress after a hint, and finish a day in normal play. Check that the lead survives serialization and an empty conversation placeholder does not block progression. Repeated dialogue cannot duplicate the hint.

The earlier question/accusation checklist follows.
# Current question and accusation QA

1. Play either train scene. Check **Action costs OFF** in the HUD. Open F8; confirm Enable action costs is unchecked. Ask several different questions beyond the configured daily budget; conversations remain free, and consumed prompts stay unavailable.
2. Combine rings C06 and champagne account F-S02 in Reasoning. Check that the result appears as a question ending in `?`, and ask Felix about it. Check that the authored dialogue still grants the marriage account.
3. Obtain Felix's corridor sighting F-S04 through the C09/F-S01 question, and Mila's reaction M-S06 through C06/M-S03. Check purple M and teal O badges on collected clues, statements and questions, the notebook legend and interpretation details. Dual-role entries show both badges; anticipated clues show neither.
4. On day one, press ACCUSE. Select C06 then Motive, and F-S04 then Opportunity. Choose Mila and confirm; the finale resolves early. Repeat with constructed questions T_C06_MS06 and T_C09_FS04 instead, then repeat after closing the final day. Exactly two slots should appear, with no Means requirement.
5. Choose Felix or Bellamy, swap the motive/opportunity assignments, or leave either empty. Confirm that the draft cannot resolve successfully and remains available for correction. After an unsuccessful early accusation, return to Reasoning, collect another clue and question a suspect; the day remains open. Moving an entry between slots must clear its old assignment.
6. In a fresh session, enable action costs in F8 and adjust the budget and both conversation costs. Confirm spending, insufficient-action rejection, free costs when set to zero, refund on playback failure, and daily replenishment. Disable costs to continue at an exhausted budget. Leave Play mode and check the saved asset still starts disabled.
7. Check narrow and wide Game views: question labels, interpretation details, two accusation slots, suspect links and confirmation should remain accessible. Check both train scenes.

See [design and tuning](QuestionsAndAccusation.md). The checklist below is historical.
# Minimum playable investigation QA

For the current one-use conversation and daily pacing flow, follow [the updated QA additions](PacingAndNotebook.md). The original checklist below predates action confirmations and final-day accusation gating.

1. Open CarnivalExpress and press Play. Check that Bellamy and Burrows discover Odette in Sleeping B. Advance all 17 opening beats; no testimony or writer-only truth should appear early.
2. Inspect the dagger, injury, programme, desk, broken clock, clock position and no-interruption instructions. Confirm each enters the notebook once. Move toward the desk to reach the concealed-clock inspection; nearby undiscovered evidence has separate clickable buttons.
3. Visit the bar/lounge, dining and Sleeping A. Collect the receipt, syrup materials and rings in Felix's luggage. Confirm all five carriages connect through a continuous walkable aisle.
4. Interview Felix at the bar, Bellamy near Odette and Mila in Sleeping A. Finish their lines. Confirm seven initial statements. Cancel Mila's interview before her first reply and confirm that the unspoken alibi is not awarded.
5. In the notebook combine C06 rings + F-S02 champagne account. Approach Felix, select him and present the theory. Confirm F-S03 marriage is acquired after his dialogue, with no automatic alibi confirmation.
6. Combine C09 rule + F-S01 bar alibi and confront Felix. Confirm F-S04 corridor sighting; it places Mila nearby, not at the moment of the fatal act.
7. Combine C02 broken clock + M-S03 excitement and confront Mila. Confirm M-S04 speculation. No M-S05 should exist merely from constructing this theory.
8. Combine C07 disturbed desk + M-S04 and confront Mila. Confirm her spatial slip is spoken before M-S05 appears. Repeat with C10 clock position + M-S04: it must use a repeat reaction and grant no duplicate. In a fresh session, use C10 first to check the alternative discovery route.
9. Construct C03 injury + B-S01 crash, C04 programme + F-S04 sighting, and C10 clock position + M-S05 spatial slip. Open Build accusation and select these three theories.
10. Accuse Felix: provisional feedback should preserve the investigation. Accuse Mila: the single static finale should play in Burrows's compartment, explaining the unintended death before resolving the case.
11. In another session use C02+B-S01, C09+F-S04 and C07+M-S05. Check that this alternative physical build reaches the same finale. Duplicate/redundant theories and missing statements must not pass.
12. Press F8 to inspect route decisions and flags. Test grant/construct/play controls. Close debug tools and confirm ordinary controls work.
13. With an assigned recording, check exact subtitle text and matching speaker. Continue must wait until playback finishes. With no clip, development mode must display the unvoiced ID and warn; disabling that mode must stop the missing line safely. A release build with missing referenced clips must be rejected.
14. Refresh Catalogue twice. Verify its GUID and assigned AudioClips stay stable. Install Playable Investigation twice and verify it does not add duplicate rooms, evidence or characters. Validate Catalogue and Validate Scene Bindings should report no structural errors.
