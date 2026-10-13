# Theory layout, removal and logbook cues

Theories now start at the intersection of the straight clue–statement connection with the centre of the theories column. Colliding icons/captions move to the nearest free location in the central band, using deterministic horizontal lanes and free vertical intervals. They are no longer packed into rows from the top of the page. Existing animation, pan, zoom, filtering and source connections remain available.

Right-click a theory icon or caption (no prior selection required) to remove its node and connections from the active graph. **Removed** opens the restore list. Removal preserves physical clues, statements, acquisition locations and consumed conversation prompts; restoration cannot replay an encounter. During investigation the same pair can also be reconstructed. During final accusation review, previously removed theories can be restored without reopening investigation or creating new discoveries. Removed theories are cleared from pending accusation selections.

When a completed dialogue beat acquires a new clue or statement, a five-second **NOTED IN LOGBOOK** notice identifies the entry. It does not appear for flags or already-known discoveries, does not change spoken subtitles, and does not label testimony as verified. The notice stays clear of the notebook's controls.

## Verification

- 41/41 EditMode tests passed, including exact parent-line placement without collisions, non-overlapping placement for all 160 theories, removal/reconstruction, final-phase restoration and preservation of consumed conversations.
- 13/13 PlayMode tests passed, including statement and clue reward notices, suppression of duplicate notices, and graph-node removal/restoration.
- Live scene review acquired Mila's alibi and displayed the recording notice; removed/restored a selected theory while retaining both sources.
- Evidence: TheoryRefinementEditModeResults.json, TheoryRefinementPlayModeResults.json, TheoryRefinementReview.txt, LogbookRecordingCue.png and TheoryConnectionsAndRemoval.png.

The dialogue-clue test temporarily grants the existing champagne receipt from Felix's receipt line and restores the fixture afterwards; it does not alter canonical source data. Existing action/day rules and the successful accusation scene remain in use. No new recordings or animations are required for the visual recording cue.
