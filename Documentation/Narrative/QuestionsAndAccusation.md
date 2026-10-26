# Questions, interpretation and accusation

The Unity scenes `Assets/Scenes/ThirdPersonTrain.unity` and `Assets/Scenes/CarnivalExpress.unity` share the narrative catalogue and investigation notebook.

Combine an acquired clue with a collected statement in **Reasoning** to form a **question**. Select the question and approach a suspect to ask it. The existing conditional interview routes and one-use encounter rules remain in effect. Questions are tentative leads, not conclusions; the question catalogue has been rewritten accordingly.

After manually ending the final investigation day, use **Accusation** to select a collected clue, statement or question, then assign it to **Motive** or **Opportunity**. Fill both slots, choose a suspect and confirm. Means is no longer an accusation requirement. The case-support checklist checks the chosen interpretation, collected sources and separate supporting observations. Unsupported accusations leave the case open for revision. Switch back to Reasoning to form more questions from already collected material.

Interpretation tags appear in the selected entry's details. Tags express investigative significance rather than proof. The ring clue, Mila's relationship/reaction statement and the associated question carry Motive; Felix's corridor sighting and associated questions carry Opportunity. Tags for clues and statements are editable in the catalogue Inspector; questions retain their proposition metadata. Refresh preserves these metadata and audio assignments. Role IDs are `P_MOTIVE` and `P_OPPORTUNITY`.

## Testing action pacing

Select `Assets/Narrative/Data/InvestigationPacing.asset` in Unity:

- **Action Costs Enabled** starts unchecked.
- **Actions Per Day** sets the daily budget (default 3).
- **Alibi Cost** defaults to 0.
- **Question Cost** is the cost of asking a constructed question (default 1).
- **Investigation Days** defaults to 3.

In Play mode, press **F8** for a session-only toggle and +/- controls for the budget and both costs. Changes are disabled while a conversation is pending or playing. Play mode uses a cloned configuration, so these controls do not overwrite the asset. Re-enable costs to restore spending and insufficient-action checks; ending a day replenishes the budget. Turning costs off preserves previously spent actions and makes new conversations free. It does not reset consumed interview prompts. Days still end manually and the final day still closes evidence collection.

## Data compatibility

`TheoryDefinition`, `theories`, `theories.csv`, `T_...` IDs and `theory/...` encounter keys are retained internally to preserve scene references, saved discovery IDs and authored routing. The CSV is the current question source; the original XLSX is a historical authoring reference and should not overwrite the updated CSVs. Use **Graduation Demo > Narrative > Refresh Catalogue** after editing CSV content. Existing recordings are retained; changing notebook question text requires no rerecording.

The enabled Mila resolution now requires a motive and an opportunity. Other suspect choices can be tried, but the prototype still has one authored successful ending.

## Verification (26 October 2026)

- Unity 6000.5.8f1 catalogue refresh: 0 structural errors; 160 questions and 490 interview routes. Existing unvoiced development dialogue remains available.
- EditMode: 63/63 passed (`QuestionsEditModeResults.xml`). Includes question text, collected-entry accusation support, source requirements, interpretation-tag refresh preservation, and enabled/disabled action budgets.
- PlayMode: 20/20 passed (`QuestionsPlayModeResults.xml`). Includes question interviews, paid confirmation and cancellation when costs are enabled, two-slot graph assignment/movement, raw clue/statement assignments, and the successful finale.
- `git diff --check` passed. Manual narrow/wide visual QA steps are in ManualQA.md.

Free conversations start immediately, as free alibis did previously; a paid conversation still asks for confirmation before spending.
