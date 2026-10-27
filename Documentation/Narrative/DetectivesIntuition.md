# Detectives' intuition

When an interview first reveals a statement tagged Motive or Opportunity, continuing from that spoken line opens an intuition choice. The player chooses **Motive** or **Opportunity**. Tagged testimony is recorded only after the choice is resolved. Existing notebook badges then show its authored interpretation.

A correct answer continues immediately and refunds the conversation's reserved action cost. An incorrect answer pauses to explain the correct interpretation; the player must continue before the dialogue proceeds. The conversation costs the greater of its original cost and **Intuition Mistake Cost** (default 1), charged once for the entire conversation. Any incorrect answer keeps this cost even if another revelation is interpreted correctly. Both choices are accepted for entries authored with both tags.

The global **Action Costs Enabled** toggle still starts disabled. Intuition choices and corrections remain active, but spend no actions while costs are off. Set the penalty in `Assets/Narrative/Data/InvestigationPacing.asset`, or use F8's **Intuition mistake cost** control for the current play session. Normal interview confirmation and affordability checks remain in effect. A mistake may exhaust the remaining budget; it never blocks the current dialogue. Playback recovery refunds the conversation's actual charged cost.

Currently authored reveals are Felix's corridor sighting F-S04 (**Opportunity**) and Mila's response about the happy couple M-S06 (**Motive**). Other statement reveals use their catalogue interpretation tags automatically. Ordinary untagged dialogue, already collected testimony, the opening, the ending and debug sequence previews do not trigger intuition. Reveals are detected from the dialogue beat's grants, preserving the recorded line and its timing.

Manual QA:

1. Enable action costs. Ask Felix the C09/F-S01 question. At his corridor account, continue and check that ordinary Space/Continue cannot skip the Motive/Opportunity choice.
2. Choose Motive. Check the Opportunity explanation, charged cost, and that testimony is recorded only after acknowledging the correction. Complete the conversation and verify its cost remains spent.
3. In a fresh session, ask Mila C06/M-S03. Choose Motive at M-S06. Check immediate continuation and the refunded conversation cost.
4. Repeat with costs disabled; answers and corrections still appear, with no spending. Repeat after acquiring the testimony through another route; known statements must not trigger another choice.
5. Change Intuition Mistake Cost in F8 and verify a wrong answer uses the new value. Leave Play mode; the asset defaults remain unchanged.

Verification (27 October 2026): Unity 6000.5.8f1 passed 76/76 EditMode tests and 25/25 PlayMode tests (`IntuitionEditModeResults.xml`, `IntuitionPlayModeResults.xml`). Coverage includes correct/incorrect answers, feedback blocking, duplicate-answer rejection, dual-role entries, mixed outcomes, disabled costs, exhausted budgets and playback refunds.
