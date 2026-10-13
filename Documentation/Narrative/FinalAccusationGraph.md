# Final accusation graph

**Current model:** [Means, Motive and Opportunity](MeansMotiveOpportunity.md). Each of the three theory tethers now attaches to a labelled socket before joining the central accusation. The earlier direct-to-accusation interaction remains an automatic role-placement shortcut for compatibility.

Ending the final day closes interviews and evidence collection, but **theory construction stays available** using collected clues and statements. No new discoveries or action spending are introduced. After the case is resolved, new construction closes.

The Reasoning tab automatically opens the accusation graph when the final day ends. **Reasoning / Accusation** switches between the ordinary graph and accusation graph. Search, filtering, pan, zoom, hover details and the Removed restore list remain available.

## Constructing the accusation

1. Click a collected clue and statement to construct any additional theory you need.
2. Click a theory, then the central hexagonal **Accusation** node to connect an accusation-tether.
3. Repeat until three distinct theories are attached. Repeat a theory-to-accusation connection to untether it.
4. Click the accusation node and then a circular suspect node to tether the case to that suspect. The third theory's tether also arms this next step.
5. Review the network and click **Confirm accusation: [suspect]**. Tethering alone never submits the case.

Every suspect node is linked to that suspect's collected statements. Hover a suspect for the known claims. Click accusation then a different suspect to change the target; repeat the current suspect connection to untether it.

Clues and statements are smaller and faded, but retain click-to-construct and full hover details. Theory diamonds, the accusation node, suspect nodes and gold accusation-tethers are emphasized. Solid parent links and case-guidance links remain in the background. The accusation and theory tooltips explain the tether workflow regardless of assistance setting.

## Editing and validation

The draft requires three active theories and one suspect tether before confirmation is available. A fourth theory is rejected until another is untethered. Right-click still archives a theory; archiving a tethered theory prunes its accusation edge and clears the suspect target when fewer than three theories remain. Restoring it does not automatically reattach it or replay dialogue.

Tethered theories and their parents stay visible under search/filtering, so a draft's links cannot silently disappear. Unsupported accusations keep the graph editable without reopening collection or interviews. Theory construction also remains possible after an unsupported accusation. Existing independent-evidence evaluation and the supported Mila finale remain in use.

The draft is notebook-session UI state, matching the previous accusation selection. No automatic save/load system was added.

## Verification

The accusation update passed 53 EditMode and 15 PlayMode tests, including construction after final closure, filtering, tether limits, removal/restoration, unsupported-case editing and the supported finale. A real mouse walkthrough exercised faded-source hover, three new final-stage theories and both tether types without changing discoveries or action spending. Submitting that graph played the supported Mila finale and resolved the case.

Evidence: `AccusationGraphEditModeResults.json`, `AccusationGraphPlayModeResults.json`, `AccusationGraphInteractionReview.json`, `AccusationGraphFinaleReview.json`, and `FinalAccusationGraph.png`.
