using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GraduationDemo.Train
{
    [RequireComponent(typeof(TrainTour))]
    [DefaultExecutionOrder(-100)]
    public sealed class DetectiveNotebook : MonoBehaviour
    {
        public string saveFileName = "carnival-interrogation-memory.json";
        public bool IsOpen { get; private set; }
        public DetectiveMemory Memory { get; private set; }
        private TrainTour tour;
        private DetectiveMemoryEntry selected;
        private Vector2 listScroll, detailScroll;
        private int filter;
        private string search = "", note = "", toast;
        private float toastUntil;
        private bool textFocused, clearFocus;
        private GUIStyle heading, body, muted, button, row, selectedRow, field;
        private Texture2D neutralSurface, hoverSurface, accentSurface, inputSurface;
        private readonly Color ink = new Color(.12f, .20f, .21f);
        private readonly Color paper = new Color(.95f, .92f, .84f);

        private void Awake() { Initialize(); }

        private void Initialize()
        {
            tour = GetComponent<TrainTour>();
            tour.notebook = this;
            if (Memory == null) Memory = new DetectiveMemory(Path.Combine(Application.persistentDataPath, saveFileName));
        }

        private void OnEnable()
        {
            Initialize();
            selected = null; note = ""; IsOpen = false;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            // Typing J into search or notes must not close the notebook.
            if (IsOpen && keyboard.escapeKey.wasPressedThisFrame) Close();
            else if (keyboard.jKey.wasPressedThisFrame && (!IsOpen || !textFocused))
                Toggle();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else { tour.CloseObservation(); IsOpen = true; }
            clearFocus = true;
        }

        public void Close()
        {
            CommitNote();
            IsOpen = false;
            clearFocus = true;
        }

        public void Record(TrainObservation item, int topicIndex = -1)
        {
            if (item == null) return;
            bool testimony = item.speaker != null;
            string source = testimony ? item.speaker.role : item.title;
            string topic = testimony ? "First conversation" : item.title;
            string text = item.description;
            if (topicIndex >= 0)
            {
                if (item.responses == null || topicIndex >= item.responses.Length) return;
                text = item.responses[topicIndex];
                topic = item.topics != null && topicIndex < item.topics.Length ? item.topics[topicIndex] : "Question " + (topicIndex + 1);
            }
            int room = Mathf.Clamp(Mathf.FloorToInt((item.transform.position.x + 6.5f) / 13), 0, tour.roomNames.Length - 1);
            string identity = item.memoryId;
            if (string.IsNullOrEmpty(identity))
            {
                identity = item.name;
                for (Transform parent = item.transform.parent; parent != null; parent = parent.parent)
                    identity = parent.name + "/" + identity;
            }
            if (Memory.Remember(identity + "/" + topic, source, topic, text, tour.roomNames[room], testimony))
            {
                toast = testimony ? "Testimony remembered" : "Observation recorded";
                toastUntil = Time.unscaledTime + 3.5f;
            }
        }

        private void CommitNote()
        {
            if (selected != null) Memory.SetNote(selected, note);
        }

        private void Choose(DetectiveMemoryEntry entry)
        {
            CommitNote();
            selected = entry;
            note = entry.note ?? "";
            detailScroll = Vector2.zero;
            clearFocus = true;
        }

        public bool Matches(DetectiveMemoryEntry entry)
        {
            if ((filter == 1 && !entry.testimony) || (filter == 2 && entry.testimony) || (filter == 3 && !entry.pinned)) return false;
            string haystack = entry.source + " " + entry.topic + " " + entry.text + " " + entry.carriage + " " + entry.note;
            return haystack.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void Styles()
        {
            if (heading != null) return;
            heading = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, wordWrap = true, richText = false };
            LabelColor(heading, ink);
            body = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true, richText = false };
            LabelColor(body, ink);
            muted = new GUIStyle(body) { fontSize = 14 };
            LabelColor(muted, new Color(.30f, .37f, .36f));
            button = new GUIStyle(GUI.skin.button) { fontSize = 16, padding = new RectOffset(16, 16, 10, 10) };
            neutralSurface = Surface(new Color(.86f, .86f, .79f));
            hoverSurface = Surface(new Color(.79f, .83f, .77f));
            accentSurface = Surface(new Color(.16f, .31f, .32f));
            inputSurface = Surface(new Color(.99f, .97f, .91f));
            State(button.normal, neutralSurface, ink);
            State(button.hover, hoverSurface, ink);
            State(button.focused, hoverSurface, ink);
            State(button.active, accentSurface, paper);
            State(button.onNormal, accentSurface, paper);
            State(button.onHover, accentSurface, paper);
            State(button.onFocused, accentSurface, paper);
            State(button.onActive, accentSurface, paper);
            row = new GUIStyle(button) { alignment = TextAnchor.MiddleLeft, wordWrap = true, richText = false };
            selectedRow = new GUIStyle(row) { fontStyle = FontStyle.Bold };
            State(selectedRow.normal, accentSurface, paper);
            State(selectedRow.hover, accentSurface, paper);
            State(selectedRow.focused, accentSurface, paper);
            field = new GUIStyle(GUI.skin.textArea) { fontSize = 17, wordWrap = true, padding = new RectOffset(12, 12, 10, 10), richText = false };
            State(field.normal, inputSurface, ink);
            State(field.hover, inputSurface, ink);
            State(field.focused, hoverSurface, ink);
            State(field.active, inputSurface, ink);
        }

        private static Texture2D Surface(Color color)
        {
            var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color); texture.Apply();
            return texture;
        }

        private static void State(GUIStyleState state, Texture2D texture, Color text)
        {
            state.background = texture; state.textColor = text;
        }

        private static void LabelColor(GUIStyle style, Color color)
        {
            foreach (var state in new[] { style.normal, style.hover, style.active, style.focused,
                style.onNormal, style.onHover, style.onActive, style.onFocused }) state.textColor = color;
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void OnGUI()
        {
            Styles();
            if (clearFocus) { GUI.FocusControl(null); textFocused = false; clearFocus = false; }
            Matrix4x4 previous = GUI.matrix;
            int depth = GUI.depth;
            Color cursor = GUI.skin.settings.cursorColor;
            Color selection = GUI.skin.settings.selectionColor;
            GUI.skin.settings.cursorColor = ink;
            GUI.skin.settings.selectionColor = new Color(.69f, .79f, .75f);
            GUI.depth = -20;
            float scale = Mathf.Max(.1f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            if (!IsOpen)
            {
                if (GUI.Button(new Rect(width - 410, 24, 220, 35), "Notebook / J  (" + Memory.Entries.Count + ")", button)) Toggle();
                if (Time.unscaledTime < toastUntil || Memory.Error != null)
                {
                    Rect feedback = new Rect(width / 2 - 280, 106, 560, 52);
                    Fill(feedback, paper);
                    GUI.Label(new Rect(feedback.x + 16, feedback.y + 9, feedback.width - 32, 40), Memory.Error ?? toast, muted);
                }
            }
            else DrawNotebook(width, height);
            textFocused = !string.IsNullOrEmpty(GUI.GetNameOfFocusedControl());
            GUI.matrix = previous;
            GUI.depth = depth;
            GUI.skin.settings.cursorColor = cursor;
            GUI.skin.settings.selectionColor = selection;
        }

        private void DrawNotebook(float width, float height)
        {
            Fill(new Rect(0, 0, width, height), new Color(.035f, .075f, .08f, .88f));
            Rect page = new Rect(40, 32, width - 80, height - 64);
            Fill(page, paper);
            GUI.Label(new Rect(64, 52, 500, 42), "Detective's notebook", heading);
            if (GUI.Button(new Rect(width - 224, 52, 160, 40), "Return / Esc", button)) Close();
            filter = GUI.Toolbar(new Rect(64, 112, 530, 40), filter, new[] { "All", "Testimony", "Observations", "Pinned" }, button);
            GUI.Label(new Rect(width - 458, 117, 62, 32), "Search", muted);
            GUI.SetNextControlName("Notebook search");
            search = GUI.TextField(new Rect(width - 390, 112, 326, 40), search, 200, field);
            Fill(new Rect(64, 174, width - 128, 1), new Color(.70f, .73f, .68f));

            float leftWidth = 358;
            Rect list = new Rect(64, 194, leftWidth, height - 268);
            int count = 0;
            foreach (var entry in Memory.Entries) if (Matches(entry)) count++;
            listScroll = GUI.BeginScrollView(list, listScroll, new Rect(0, 0, leftWidth - 20, Mathf.Max(list.height, count * 90)));
            int index = 0;
            foreach (var entry in Memory.Entries)
            {
                if (!Matches(entry)) continue;
                string label = (entry.pinned ? "Pinned / " : "") + entry.source + "\n" + entry.topic;
                if (GUI.Button(new Rect(0, index++ * 90, leftWidth - 24, 80), label, selected == entry ? selectedRow : row)) Choose(entry);
            }
            if (count == 0)
                GUI.Label(new Rect(0, 8, leftWidth - 32, 150), Memory.Entries.Count == 0
                    ? "No memories yet.\n\nReturn to the train and talk to a passenger or inspect an object with E."
                    : "No matching entries.\nChange the filter or search.", body);
            GUI.EndScrollView();

            Rect detail = new Rect(454, 194, width - 518, height - 268);
            if (selected != null && Matches(selected)) DrawEntry(detail);
            else GUI.Label(new Rect(detail.x, detail.y + 12, detail.width, 80), count > 0 ? "Select a memory to review it." : "Only conversations and objects you encounter are recorded.", body);
            GUI.Label(new Rect(64, height - 60, width - 128, 26), Memory.Error ?? (Memory.Entries.Count + " memories  /  Saved automatically"), muted);
        }

        private void DrawEntry(Rect area)
        {
            float contentWidth = area.width - 24;
            float quoteHeight = body.CalcHeight(new GUIContent(selected.text), contentWidth - 32);
            float topicHeight = heading.CalcHeight(new GUIContent(selected.topic), contentWidth);
            float quoteTop = 88 + topicHeight;
            float noteTop = quoteTop + quoteHeight + 48;
            float noteHeight = Mathf.Max(150, field.CalcHeight(new GUIContent(note), contentWidth) + 24);
            detailScroll = GUI.BeginScrollView(area, detailScroll, new Rect(0, 0, contentWidth, Mathf.Max(area.height, noteTop + noteHeight + 60)));
            GUI.Label(new Rect(0, 0, contentWidth - 160, 26), selected.testimony ? "Testimony / unverified" : "Inspected observation", muted);
            if (GUI.Button(new Rect(contentWidth - 148, 0, 148, 38), selected.pinned ? "Unpin memory" : "Pin memory", button))
                Memory.SetPinned(selected, !selected.pinned);
            GUI.Label(new Rect(0, 50, contentWidth, topicHeight + 8), selected.topic, heading);
            GUI.Label(new Rect(0, 56 + topicHeight, contentWidth, 28), selected.source + "  /  " + selected.carriage, muted);
            Fill(new Rect(0, quoteTop, 3, quoteHeight + 16), new Color(.54f, .40f, .23f));
            GUI.Label(new Rect(16, quoteTop + 4, contentWidth - 32, quoteHeight + 16), selected.text, body);
            GUI.Label(new Rect(0, noteTop, contentWidth, 28), "Detective's note", body);
            GUI.SetNextControlName("Detective note");
            string edited = GUI.TextArea(new Rect(0, noteTop + 36, contentWidth, noteHeight), note, 2000, field);
            if (edited != note) { note = edited; CommitNote(); }
            GUI.EndScrollView();
        }

        private void OnDisable()
        {
            if (Memory != null) CommitNote();
            IsOpen = false;
            ReleaseTextures();
        }
        private void OnApplicationPause(bool paused) { if (paused && Memory != null) CommitNote(); }
        private void OnApplicationQuit() { if (Memory != null) CommitNote(); }
        private void OnDestroy() { ReleaseTextures(); }
        private void ReleaseTextures()
        {
            foreach (var texture in new[] { neutralSurface, hoverSurface, accentSurface, inputSurface })
                if (texture != null) Destroy(texture);
            neutralSurface = hoverSurface = accentSurface = inputSurface = null;
            heading = null;
        }
    }
}
