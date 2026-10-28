using System.Collections.Generic;
using System.Linq;
using GraduationDemo.Narrative;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GraduationDemo.Train
{
    [RequireComponent(typeof(TrainTour), typeof(AudioSource))]
    public sealed class TrainNarrative : MonoBehaviour
    {
        public NarrativeCatalogue catalogue;
        public InvestigationConfiguration configuration;
        public bool developmentUnvoiced = true;
        public bool playOpening = true;
        public InvestigationState State { get; private set; }
        public bool Busy => sequence != null || notebook || choiceSuspect != null || pending != null || confirmDay || QuestionMenuOpen;
        public bool QuestionMenuOpen => questionMenu != null && questionMenu.IsOpen;
        public bool NotebookOpen => notebook;
        public string CurrentSequence => sequence?.id;
        public bool DialogueAudioPlaying => audioSource != null && audioSource.isPlaying;
        public string DialoguePartner => encounter?.route?.suspect ?? (sequence == null ? null : sequence.beats.Select(b => catalogue.Voice(b.voiceId)?.speaker).FirstOrDefault(s => s != null && s != "Burrows"));
        public void ContinueDialogue() => UserAdvance();
        public bool CurrentQuestionDoubtResolved => encounter?.route != null && State.QuestionDoubtResolved(encounter.route.theoryId);
        public string CurrentVoiceId => sequence == null ? null : sequence.beats[beatIndex].voiceId;
        public string IntuitionEntry { get; private set; }
        public string IntuitionFeedback { get; private set; }
        public bool AwaitingIntuition => IntuitionEntry != null && IntuitionFeedback == null;
        public string RouteDiagnostic { get; private set; }
        public string Message { get; private set; }
        public string LogbookNotice { get; private set; }
        public int LogbookNoticeVersion { get; private set; }
        float logbookNoticeUntil;
        TrainTour tour;
        AudioSource audioSource;
        DialogueDefinition sequence;
        RouteSelection encounter;
        int beatIndex;
        int userAdvanceFrame = -1;
        bool notebook, debug;
        string suspect = "Mila";
        readonly HashSet<string> warned = new HashSet<string>();
        Vector2 debugScroll;
        TrainActor talkingActor;
        InvestigationNotebook notebookView;
        SuspectQuestionMenu questionMenu;
        string choiceSuspect, conversationLocation = "unknown";
        ConversationInitialization pending;
        bool confirmDay, debugSequence, authorisedEnding;
        bool ownsConfiguration;
        float barkAgainAt;
        public string BarkText { get; private set; }
        public string CurrentLocation
        {
            get
            {
                if (tour == null || tour.detective == null) return "unknown";
                int nearest = 0; float distance = float.PositiveInfinity;
                for (int i = 0; i < tour.roomViews.Length; i++) { float d = Mathf.Abs(tour.detective.transform.position.x - tour.roomViews[i].position.x); if (d < distance) { nearest = i; distance = d; } }
                return tour.locationIds[Mathf.Clamp(nearest, 0, tour.locationIds.Length - 1)];
            }
        }
        GUIStyle body, heading, notebookButton, panelStyle;
        Texture2D panelTexture;

        void Awake()
        {
            tour = GetComponent<TrainTour>(); audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false; audioSource.spatialBlend = 0;
            if (catalogue == null) { Debug.LogError("TrainNarrative needs an imported catalogue.", this); enabled = false; return; }
            var errors = NarrativeValidation.Errors(catalogue);
            if (errors.Count > 0) { Debug.LogError("Invalid narrative catalogue: " + string.Join("\n", errors), this); enabled = false; return; }
            if (configuration != null && configuration.Errors().Count > 0) { Debug.LogError(string.Join("\n", configuration.Errors()), this); enabled = false; return; }
            if (configuration != null && configuration.doubtLinks.Any(d => catalogue.Theory(d.questionId) == null || catalogue.Clue(d.sourceId) == null && catalogue.Statement(d.sourceId) == null && catalogue.Theory(d.sourceId) == null))
            { Debug.LogError("Authored doubt links reference missing questions or evidence.", this); enabled = false; return; }
            if (configuration != null && configuration.doubts.Any(d => catalogue.Clue(d.clueId) == null || catalogue.Statement(d.statementId) == null))
            { Debug.LogError("Doubt definitions reference missing source evidence.", this); enabled = false; return; }
            // Debug tuning applies to this play session without changing the project asset.
            if (configuration != null) { configuration = Instantiate(configuration); ownsConfiguration = true; }
            State = new InvestigationState(catalogue, null, configuration);
            FocusCoffeeStand.Ensure(this, tour);
            notebookView = GetComponent<InvestigationNotebook>();
            questionMenu = GetComponent<SuspectQuestionMenu>() ?? gameObject.AddComponent<SuspectQuestionMenu>();
            foreach (var binding in FindObjectsByType<NarrativeInteraction>())
                if (!string.IsNullOrEmpty(binding.clueId) && binding.GetComponent<ClueMarker>() == null) binding.gameObject.AddComponent<ClueMarker>();
        }
        void Start()
        {
            if (State == null) return;
            if (GetComponent<DialoguePresentation>() == null) gameObject.AddComponent<DialoguePresentation>();
            if (playOpening) Begin("OPENING — The real corpse");
            else State.Grant("flag:investigation");
        }
        void Update()
        {
            var keys = Keyboard.current;
            if (keys == null) return;
            if ((Application.isEditor || Debug.isDebugBuild) && keys.f8Key.wasPressedThisFrame) debug = !debug;
            var selectedControl = UnityEngine.EventSystems.EventSystem.current == null ? null : UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            bool editingText = selectedControl != null && selectedControl.activeInHierarchy && selectedControl.GetComponent<UnityEngine.UI.InputField>() != null;
            if (keys.nKey.wasPressedThisFrame && !editingText && sequence == null && State.Has("flag:investigation")) ToggleNotebook();
            if (sequence != null && (keys.spaceKey.wasPressedThisFrame || keys.enterKey.wasPressedThisFrame)) UserAdvance();
            if (keys.escapeKey.wasPressedThisFrame)
            {
                if (notebook) notebook = false;
                else if (QuestionMenuOpen) questionMenu.Close();
                else if (sequence == null) { pending = null; choiceSuspect = null; confirmDay = false; }
                notebookView?.SetOpen(notebook);
            }
        }
        public void ToggleNotebook()
        {
            if (sequence != null || pending != null || confirmDay || !State.Has("flag:investigation")) return;
            questionMenu?.Close();
            tour.CloseObservation(); notebook = !notebook;
            notebookView?.SetOpen(notebook);
        }
        public bool Interact(TrainObservation observation)
        {
            if (!tour.CanReach(observation)) { Message = "Approach the marker or suspect before interacting."; return true; }
            var binding = observation.GetComponent<NarrativeInteraction>();
            if (binding == null) { if (observation.speaker != null) { PlayBark(); return true; } return false; }
            if (Busy || !State.Has(binding.requiredFlag)) return true;
            if (!State.InvestigationOpen) { Message = "The investigation has closed. Review your notebook."; if (!string.IsNullOrEmpty(binding.suspectId)) PlayBark(); return true; }
            if (binding.coffeeFocus)
            {
                int before = State.FocusCapacity;
                if (State.ClaimCoffeeFocus())
                {
                    NotifyFocusReward(State.FocusCapacity - before, "COFFEE");
                    observation.description = "A fresh coffee clears your head. +" + (State.FocusCapacity - before) + " focus.\nFocus capacity: " + State.FocusCapacity + ". This stand's coffee reward has been collected.";
                }
                else observation.description = "You've already taken this coffee break.";
                return false;
            }
            if (!string.IsNullOrEmpty(binding.clueId))
            {
                if (!State.CanFindClue(binding.clueId)) { Message = "You have no lead to search here yet."; return true; }
                var definition = catalogue.Clue(binding.clueId);
                if (definition == null) { Debug.LogError("Missing clue binding " + binding.clueId, observation); return true; }
                State.DiscoverClue(definition.id, CurrentLocation); observation.description = definition.fact + "\n\nRecorded in notebook. Press N to review.";
                return false;
            }
            if (!string.IsNullOrEmpty(binding.suspectId))
            {
                suspect = binding.suspectId;
                OpenQuestionMenu(suspect);
                return true;
            }
            return false;
        }
        public bool ObservationAvailable(TrainObservation observation)
        {
            var binding = observation.GetComponent<NarrativeInteraction>();
            if (binding != null && binding.coffeeFocus) return State != null && State.Has(binding.requiredFlag) && State.CoffeeAvailable;
            return binding == null || string.IsNullOrEmpty(binding.clueId) || State != null && State.InvestigationOpen && State.Has(binding.requiredFlag) && State.CanFindClue(binding.clueId) && !State.Has(binding.clueId);
        }
        public bool CanTalkTo(string target)
        {
            if (!NarrativeCatalogue.IsSuspect(target) || State == null || !State.InvestigationOpen) return false;
            var binding = FindObjectsByType<NarrativeInteraction>().FirstOrDefault(b => b.suspectId == target);
            return binding != null && tour.CanReach(binding.GetComponent<TrainObservation>());
        }
        public bool OpenQuestionMenu(string target)
        {
            if (sequence != null || pending != null || !CanTalkTo(target)) { Message = "Approach " + target + " to ask a question."; return false; }
            tour.CloseObservation(); notebook = false; notebookView?.SetOpen(false);
            return questionMenu != null && questionMenu.Open(target);
        }
        static string Interview(string target) => target == "Felix" ? "Felix — Champagne alibi" : target == "Mila" ? "Mila — Solitary rehearsal" : "Bellamy — Corridor witness";
        public bool Begin(string sequenceId, RouteSelection selected = null)
        {
            if (sequence != null) return false;
            var next = catalogue.Sequence(sequenceId);
            if (next == null || next.beats.Count == 0) { Message = "Missing dialogue: " + sequenceId; Debug.LogError(Message, this); return false; }
            if (!CanPlay(next)) return false;
            if (!debugSequence)
            {
                if (sequenceId == "OPENING — The real corpse") { if (State.Has("flag:investigation")) return false; }
                else if (sequenceId == "ENDING — Mila's account") { if (!authorisedEnding) return false; }
                else if (State.progress.reservation == null)
                {
                    string target = new[] { "Felix", "Mila", "Bellamy" }.FirstOrDefault(x => Interview(x) == sequenceId);
                    var alibi = target == null ? null : State.Alibi(target);
                    if (alibi == null || !CanTalkTo(target) || alibi.cost != 0 || !State.TryInitialize(alibi, out var reason)) return false;
                }
                else if (State.progress.reservation.sequenceId != sequenceId) return false;
            }
            conversationLocation = debugSequence ? "unknown" : CurrentLocation;
            tour.CloseObservation(); notebook = false; notebookView?.SetOpen(false); choiceSuspect = null; pending = null;
            questionMenu?.Close();
            sequence = next; encounter = selected; beatIndex = 0; ShowBeat(); return sequence != null;
        }
        bool CanPlay(DialogueDefinition next)
        {
            if (next == null) { Message = "Dialogue definition is unavailable."; return false; }
            foreach (var beat in next.beats)
            {
                var voice = catalogue.Voice(beat.voiceId);
                if (voice == null) { Message = "Missing voice definition: " + beat.voiceId; Debug.LogError(Message, this); return false; }
                if (voice.clip == null && !(developmentUnvoiced && (Application.isEditor || Debug.isDebugBuild)))
                { Message = "Recording unavailable: " + voice.id; Debug.LogError(Message, this); return false; }
            }
            return true;
        }
        void ShowBeat()
        {
            StopSpeaker();
            var voice = catalogue.Voice(CurrentVoiceId);
            if (voice == null) { Message = "Missing voice definition " + CurrentVoiceId; Debug.LogError(Message, this); RecoverPlayback(); return; }
            if (voice.disclosesFallGeometry && !State.Has("M-S05")) State.Grant("flag:geometry_disclosed");
            talkingActor = FindObjectsByType<TrainActor>(FindObjectsSortMode.None).FirstOrDefault(x => x.characterId == voice.speaker);
            if (talkingActor != null) talkingActor.SetTalking(true);
            if (voice.clip != null) { audioSource.clip = voice.clip; audioSource.Play(); }
            else if (developmentUnvoiced && (Application.isEditor || Debug.isDebugBuild))
            {
                if (warned.Add(voice.id)) Debug.LogWarning("UNVOICED DEVELOPMENT LINE: " + voice.id + " / " + voice.subtitle, this);
            }
            else { Message = "Recording unavailable: " + voice.id; Debug.LogError(Message, this); RecoverPlayback(); }
        }
        public void Advance()
        {
            if (sequence == null || audioSource.isPlaying) return;
            if (AwaitingIntuition) return;
            IntuitionEntry = null; IntuitionFeedback = null;
            var reservation = State.progress.reservation;
            if (reservation != null && !debugSequence)
            {
                IntuitionEntry = sequence.beats[beatIndex].grants.FirstOrDefault(id => !State.Has(id) &&
                    !reservation.interpretedEntries.Contains(id) && catalogue.InterpretationTags(id).Any(AccusationReasoning.Roles.Contains));
                if (IntuitionEntry != null) return;
            }
            var notes = new List<string>();
            foreach (var id in sequence.beats[beatIndex].grants.Where(id => catalogue.Clue(id) == null))
            {
                bool acquired = State.Grant(id, conversationLocation);
                if (!acquired) continue;
                var statement = catalogue.Statement(id);
                if (statement != null) notes.Add(statement.speaker + ": " + statement.claim);
            }
            var completedBeat = sequence.beats[beatIndex];
            var speaker = catalogue.Voice(completedBeat.voiceId)?.speaker;
            foreach (var clueId in catalogue.ClueHints(completedBeat))
                if (State.HintClue(clueId, speaker, completedBeat.voiceId)) notes.Add("Clue hint: " + catalogue.Clue(clueId).name + " — " + catalogue.Clue(clueId).hint);
            if (notes.Count > 0)
            {
                LogbookNotice = "NOTED IN LOGBOOK\n" + string.Join("\n", notes);
                LogbookNoticeVersion++; logbookNoticeUntil = Time.unscaledTime + 5;
            }
            beatIndex++;
            if (beatIndex < sequence.beats.Count) { ShowBeat(); return; }
            State.FinishConversation(encounter); StopSpeaker(); sequence = null; encounter = null; debugSequence = false;
            Message = State.Has("flag:resolved") ? "Case resolved. Mila will give her account to the authorities." : "Notebook updated.";
        }
        void UserAdvance() { if (userAdvanceFrame == Time.frameCount) return; userAdvanceFrame = Time.frameCount; Advance(); }
        public bool ChooseIntuition(string role)
        {
            if (!AwaitingIntuition || !AccusationReasoning.Roles.Contains(role)) return false;
            int beforeFocus = State.FocusCapacity;
            bool correct = State.ResolveIntuition(IntuitionEntry, role);
            string interpretation = string.Join(" or ", catalogue.InterpretationTags(IntuitionEntry).Where(AccusationReasoning.Roles.Contains).Select(AccusationReasoning.Label));
            string explanation = interpretation == "Motive" ? "This suggests why someone might have wanted to hurt Odette." :
                interpretation == "Opportunity" ? "This suggests access, presence or a chance to act." : "This account can support either interpretation.";
            IntuitionFeedback = (correct ? "Correct. " : "The interpretation is " + interpretation + ". ") + explanation + "\n" +
                (!State.ActionCostsEnabled ? "Action costs are disabled." : State.progress.reservation.cost == 0 ? "This conversation is free." : "Conversation cost: " + State.progress.reservation.cost + " action(s).");
            if (correct) { Message = IntuitionFeedback; Advance(); if (State.FocusCapacity > beforeFocus) NotifyFocusReward(State.FocusCapacity - beforeFocus, "INTUITION"); }
            return true;
        }
        void NotifyFocusReward(int amount, string source)
        {
            Message = source + " +" + amount + " FOCUS • Capacity " + State.FocusCapacity;
            LogbookNotice = Message + (Time.unscaledTime < logbookNoticeUntil && !string.IsNullOrEmpty(LogbookNotice) ? "\n" + LogbookNotice : "");
            LogbookNoticeVersion++; logbookNoticeUntil = Time.unscaledTime + 5;
        }
        public void CancelDialogue() { if (sequence != null) Message = "Finish this conversation before returning to the investigation."; }
        void RecoverPlayback() { State.RecoverFailedConversation(); StopSpeaker(); sequence = null; encounter = null; debugSequence = false; IntuitionEntry = null; IntuitionFeedback = null; }
        public void DebugPlaySequence(string id) { if (!(Application.isEditor || Debug.isDebugBuild)) return; RecoverPlayback(); debugSequence = true; if (!Begin(id)) debugSequence = false; }
        void StopSpeaker() { audioSource.Stop(); if (talkingActor != null) talkingActor.SetTalking(false); talkingActor = null; }
        public bool Confront(string theoryId, string target)
        {
            if (!State.QuestionSecure(theoryId)) { Message = "This question is insecure. Defend its doubt connection in Reasoning before asking a suspect."; return false; }
            if (!NarrativeCatalogue.IsSuspect(target)) return false;
            var actor = FindObjectsByType<TrainActor>(FindObjectsSortMode.None).FirstOrDefault(x => x.characterId == target);
            if (actor == null || tour.exterior || Vector3.Distance(tour.detective.transform.position + Vector3.up, actor.transform.position) > 3.3f)
            { Message = "Approach " + target + " before presenting a question."; return false; }
            return OfferConversation(State.Presentation(theoryId, target));
        }
        public bool OfferConversation(ConversationInitialization prompt)
        {
            if (prompt != null && prompt.kind == ConversationKind.Theory && !State.QuestionSecure(prompt.theoryId)) { Message = "Defend this insecure question in Reasoning first."; return false; }
            if (sequence != null || prompt == null) { if (prompt == null) PlayBark(); return false; }
            if (!CanTalkTo(prompt.suspect)) { Message = "Approach " + prompt.suspect + " first."; return false; }
            if (prompt.cost > State.RemainingActions) { Message = "Not enough actions remain today."; return false; }
            if (!CanPlay(catalogue.Sequence(prompt.sequenceId))) return false;
            questionMenu?.Close();
            if (prompt.cost == 0) return Initialize(prompt);
            pending = prompt; choiceSuspect = null; notebook = false; notebookView?.SetOpen(false); return true;
        }
        bool Initialize(ConversationInitialization prompt)
        {
            if (!CanTalkTo(prompt.suspect)) { Message = "Approach " + prompt.suspect + " first."; return false; }
            var definition = catalogue.Sequence(prompt.sequenceId); if (definition == null || !CanPlay(definition)) return false;
            if (!State.TryInitialize(prompt, out var reason)) { Message = reason; pending = null; return false; }
            RouteDiagnostic = prompt.selection?.explanation ?? "Initial alibi / " + prompt.suspect;
            if (Begin(prompt.sequenceId, prompt.selection)) return true;
            State.RecoverFailedConversation(); return false;
        }
        public bool ConfirmConversation() { var offered = pending; pending = null; return offered != null && Initialize(offered); }
        public void CancelConfirmation() { pending = null; choiceSuspect = null; confirmDay = false; }
        public void RequestEndDay() { if (sequence == null && State.InvestigationOpen) { tour.CloseObservation(); pending = null; choiceSuspect = null; confirmDay = true; notebook = false; notebookView?.SetOpen(false); } }
        public bool ConfirmEndDay() { if (!confirmDay) return false; confirmDay = false; bool result = State.EndDay(out var reason); Message = reason; return result; }
        void PlayBark()
        {
            if (Time.unscaledTime < barkAgainAt || sequence != null) return;
            var voice = catalogue.Voice("BURR_NO_CHAT_01"); if (voice == null) { Debug.LogError("Missing Burrows no-conversation bark.", this); return; }
            BarkText = voice.subtitle; barkAgainAt = Time.unscaledTime + (voice.clip == null ? 2.5f : Mathf.Max(2.5f, voice.clip.length));
            if (voice.clip != null) audioSource.PlayOneShot(voice.clip);
            else if (developmentUnvoiced && (Application.isEditor || Debug.isDebugBuild)) { if (warned.Add(voice.id)) Debug.LogWarning("UNVOICED DEVELOPMENT BARK: " + voice.id, this); }
            else Debug.LogError("Recording unavailable: " + voice.id, this);
        }
        public bool Accuse(string target, IEnumerable<string> theories)
        { return AccuseWithSockets(target, theories, null); }
        public bool AccuseWithSockets(string target, IEnumerable<string> theories, IReadOnlyDictionary<string, string> sockets)
        {
            if (sequence != null) return false;
            var rule = State.EvaluateArrest(target, theories, out var reason, sockets); Message = reason;
            if (rule == null) return false;
            int finaleRoom = tour.roomViews.Length - 1;
            tour.SetView(finaleRoom, true);
            var accused = FindObjectsByType<TrainActor>(FindObjectsSortMode.None).FirstOrDefault(x => x.characterId == target);
            if (accused != null) accused.transform.position = new Vector3(tour.roomViews[finaleRoom].position.x + .1f, 1.12f, 1.7f);
            authorisedEnding = true; bool started = Begin(rule.endingSequence); authorisedEnding = false; return started;
        }
        void OnGUI()
        {
            if (State == null) return;
            if (body == null)
            {
                body = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 17 };
                body.normal.textColor = new Color(.96f, .88f, .70f);
                heading = new GUIStyle(body) { fontStyle = FontStyle.Bold, fontSize = 21 };
                notebookButton = new GUIStyle(GUI.skin.button) { wordWrap = true, fontSize = 16, padding = new RectOffset(8, 8, 7, 7) };
                panelTexture = new Texture2D(1, 1); panelTexture.SetPixel(0, 0, new Color(.055f, .10f, .105f, .98f)); panelTexture.Apply();
                panelStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(12, 12, 12, 12) };
                panelStyle.normal.background = panelTexture;
            }
            float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), .75f, 1.5f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            if (sequence != null && Event.current.type == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Space || Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
            { UserAdvance(); Event.current.Use(); }
            if (sequence == null && !notebook && !QuestionMenuOpen && State.Has("flag:investigation"))
            {
                if (GUI.Button(new Rect(width - 178, 66, 150, 32), "Notebook / N")) ToggleNotebook();
                if (!string.IsNullOrEmpty(Message)) GUI.Label(new Rect(32, 106, width - 220, 60), Message, body);
            }
            if (choiceSuspect != null || pending != null || confirmDay) DrawConversationChoice(width, height);
            if (sequence == null && !notebook && !QuestionMenuOpen && choiceSuspect == null && pending == null && !confirmDay && State.Has("flag:investigation"))
            {
                GUI.Label(new Rect(24, height - 126, 550, 32), State.InvestigationOpen ? "Day " + State.progress.day + "/" + State.Days + "   " + (State.ActionCostsEnabled ? State.RemainingActions + "/" + State.ActionsPerDay + " actions" : "Action costs OFF") : "Investigation closed / Review your accusation", heading);
                if (State.InvestigationOpen && GUI.Button(new Rect(24, height - 164, 170, 30), "End day")) RequestEndDay();
                if (State.InvestigationOpen && State.ActionCostsEnabled) GUI.Label(new Rect(24, height - 194, 430, 26), string.Join("  ", Enumerable.Range(0, State.ActionsPerDay).Select(i => i < State.progress.actionsSpent ? "☾" : "☀")), heading);
                if (Time.unscaledTime < barkAgainAt) GUI.Label(new Rect(width / 2 - 230, height - 224, 460, 40), BarkText, body);
            }
            if (debug && (Application.isEditor || Debug.isDebugBuild)) DrawDebug(width, height);
            if (!notebook && IntuitionEntry == null && Time.unscaledTime < logbookNoticeUntil && !string.IsNullOrEmpty(LogbookNotice))
            {
                GUI.DrawTexture(new Rect(24, 106, Mathf.Min(620, width - 48), 118), panelTexture);
                GUI.Label(new Rect(38, 116, Mathf.Min(590, width - 76), 98), LogbookNotice, body);
            }
            GUI.matrix = Matrix4x4.identity;
        }
        void DrawDebug(float width, float height)
        {
            GUILayout.BeginArea(new Rect(width - 460, 24, 440, height - 48), panelStyle);
            GUILayout.Label("Narrative debug / F8", heading); debugScroll = GUILayout.BeginScrollView(debugScroll);
            if (configuration != null)
            {
                GUILayout.Label("Action pacing (this play session)", heading);
                GUI.enabled = State.progress.reservation == null && pending == null;
                bool wasEnabled = configuration.actionCostsEnabled;
                int budget = configuration.actionsPerDay, alibi = configuration.alibiCost, question = configuration.confrontationCost;
                configuration.actionCostsEnabled = GUILayout.Toggle(configuration.actionCostsEnabled, "Enable action costs");
                configuration.actionsPerDay = PacingValue("Actions per day", budget, 1);
                configuration.alibiCost = PacingValue("Alibi cost", alibi, 0);
                configuration.confrontationCost = PacingValue("Question cost", question, 0);
                configuration.intuitionMistakeCost = PacingValue("Intuition mistake cost", configuration.intuitionMistakeCost, 0);
                if (wasEnabled != configuration.actionCostsEnabled || budget != configuration.actionsPerDay || alibi != configuration.alibiCost || question != configuration.confrontationCost)
                    State.RefreshConfiguration();
                GUI.enabled = true;
                GUILayout.Label("Persistent defaults: InvestigationPacing asset. Days still end manually.", body);
            }
            foreach (var c in catalogue.clues) if (GUILayout.Button("Grant clue " + c.name)) State.Grant(c.id);
            foreach (var s in catalogue.statements) if (GUILayout.Button("Grant statement " + s.id + " / " + s.claim)) State.Grant(s.id);
            if (GUILayout.Button("Construct every acquired pair")) foreach (var c in State.progress.clues) foreach (var s in State.progress.statements) State.Construct(c, s);
            GUILayout.Label("Flags: " + string.Join(", ", State.progress.flags));
            GUILayout.Label("Suspect: " + suspect); GUILayout.Label(RouteDiagnostic ?? "No confrontation selected.", body);
            foreach (var s in catalogue.sequences) if (GUILayout.Button("Play: " + s.id)) DebugPlaySequence(s.id);
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        static int PacingValue(string label, int value, int minimum)
        {
            GUILayout.BeginHorizontal(); GUILayout.Label(label + ": " + value);
            if (GUILayout.Button("-", GUILayout.Width(32))) value = Mathf.Max(minimum, value - 1);
            if (GUILayout.Button("+", GUILayout.Width(32))) value++;
            GUILayout.EndHorizontal(); return value;
        }
        void OnDestroy() { if (panelTexture != null) Destroy(panelTexture); if (ownsConfiguration) Destroy(configuration); }
        void DrawConversationChoice(float width, float height)
        {
            GUILayout.BeginArea(new Rect(width / 2 - 280, 125, 560, height - 225), panelStyle);
            if (confirmDay)
            {
                GUILayout.Label(State.progress.day >= State.Days ? "Close the final investigation day?" : "End this day?", heading);
                GUILayout.Label((State.ActionCostsEnabled ? "Unused actions: " + State.RemainingActions + ". They will be forfeited." : "Action costs are disabled.") + (State.progress.day >= State.Days ? " Interviews and collection close. You can still construct questions from collected evidence and build an accusation." : ""), body);
                if (GUILayout.Button("Confirm end of day")) ConfirmEndDay();
            }
            else if (pending != null)
            {
                GUILayout.Label((State.ActionCostsEnabled ? "Reserve " + pending.cost + " action(s) for this conversation?" : "Start this conversation? (Action costs OFF)"), heading);
                if (catalogue.Sequence(pending.sequenceId).beats.SelectMany(b => b.grants).Any(id => !State.Has(id) && catalogue.InterpretationTags(id).Any(AccusationReasoning.Roles.Contains)))
                    GUILayout.Label("A correct intuition makes this conversation free. An incorrect intuition costs at least " + configuration.intuitionMistakeCost + " action(s) when costs are enabled.", body);
                GUILayout.Label(catalogue.Theory(pending.theoryId)?.text ?? "Ask for an alibi.", body);
                GUILayout.Label((State.ActionCostsEnabled ? "Actions remaining today: " + State.RemainingActions : "Unlimited conversations; each prompt is still used once."), body);
                if (notebookView != null && notebookView.Assistance == ReasoningAssistance.Directed)
                    GUILayout.Label(ReasoningGuidance.InterviewNote(catalogue, State.progress, pending), body);
                if (GUILayout.Button("Confirm and speak to " + pending.suspect)) ConfirmConversation();
            }
            if (GUILayout.Button("Cancel")) CancelConfirmation();
            GUILayout.EndArea();
        }
    }
}
