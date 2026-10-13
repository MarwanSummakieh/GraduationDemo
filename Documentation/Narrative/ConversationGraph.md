# Suspect conversation selection

Interacting with a suspect opens a graph of that suspect's currently available constructed theories. Each theory keeps its clue and statement parent links. Sources are greyed and disabled for selection, but hover still shows their full details. Select a theory, then **Present theory to [suspect]**. Availability, proximity, recordings and action cost are checked through the existing conversation rules; paid presentations still require confirmation. Cancel consumes neither an action nor a prompt.

An available **Ask [suspect] for an alibi** button remains separate. Used or ineligible theory conversations are omitted from the graph. Unaffordable options may be reviewed when another free option permits conversation selection, but cannot be presented. Close the conversation view and reopen the ordinary notebook to construct additional theories. Changing to Map or Train leaves conversation selection.

## Guidance lines

Dotted guidance connections represent pairs not yet constructed. Creating or restoring the associated theory hides its dotted connection; removing the theory restores it if the underlying hint is still eligible. Solid theory-parent links remain. Recipient advice and hint explanations remain available on constructed theories, so creating a theory does not erase the next step.

Hints retired by a learned revelation stay retired, even if their theory is removed. This prevents guiding players toward a consumed conversation. In the conversation graph, only available theories and their sources appear; construction guidance for archived pairs remains available in the ordinary Reasoning view.

## Verification

55 EditMode and 16 PlayMode tests passed, including the accusation graph regressions. A real mouse check verifies construction hides dotted hint geometry, removal restores it and restoration hides it again; greyed source hover works; selecting and presenting a theory opens action confirmation; cancelling preserves the budget and prompt.

Evidence: `ConversationGraphEditModeResults.json`, `ConversationGraphPlayModeResults.json`, `ConversationGraphInteractionReview.json`, and `SuspectConversationGraph.png`.
