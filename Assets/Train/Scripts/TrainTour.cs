using UnityEngine;
using UnityEngine.InputSystem;

namespace GraduationDemo.Train
{
    public sealed class TrainTour : MonoBehaviour
    {
        public Camera viewCamera;
        public TrainCameraRig cameraRig;
        public string[] locationIds = { "odette", "sleeping_a", "bar", "dining", "burrows" };
        public CharacterController detective;
        public GameObject[] cutawayShells;
        public Transform[] roomViews;
        public Transform exteriorView;
        public Renderer[] foregroundScenery;
        public float walkSpeed = 3.6f;
        public float interiorSize = 4.1f;
        public float exteriorSize = 21;
        public string[] roomNames = { "01 / THE VELVET LOUNGE", "02 / THE DINING SALOON", "03 / THE PRIVATE SUITE" };
        public bool exterior;
        public int currentRoom;
        public TrainNarrative narrative;
        public TrainObservation Nearby => nearby;
        public TrainObservation ActiveConversation => activeConversation;
        public string CurrentResponse => observation;
        public float CameraFollowX { get; private set; }
        private string observation;
        private TrainObservation nearby, activeConversation;
        private TrainActor currentSpeaker;
        private GUIStyle titleStyle, smallStyle, bodyStyle, buttonStyle, nameStyle;
        private Texture2D panel;

        private void Start() { narrative = GetComponent<TrainNarrative>(); if (narrative == null) SetView(0, false); SnapCamera(); }

        private void Update()
        {
            FindNearby();
            if (narrative != null && narrative.Busy) { cameraRig?.ResetMovement(); UpdateCamera(); return; }
            var keys = Keyboard.current;
            if (keys != null)
            {
                bool reading = !string.IsNullOrEmpty(observation);
                if (keys.escapeKey.wasPressedThisFrame) CloseObservation();
                if (cameraRig != null && keys.cKey.wasPressedThisFrame) cameraRig.ToggleFollow();
                if (cameraRig == null && keys.tabKey.wasPressedThisFrame) SetExterior(!exterior);
                if (!reading)
                {
                    if (keys.digit1Key.wasPressedThisFrame) SetView(0, true);
                    if (keys.digit2Key.wasPressedThisFrame) SetView(1, true);
                    if (keys.digit3Key.wasPressedThisFrame) SetView(2, true);
                    if (keys.digit4Key.wasPressedThisFrame && roomViews.Length > 3) SetView(3, true);
                    if (keys.digit5Key.wasPressedThisFrame && roomViews.Length > 4) SetView(4, true);
                }
                else if (activeConversation != null)
                {
                    if (keys.digit1Key.wasPressedThisFrame) SelectTopic(0);
                    if (keys.digit2Key.wasPressedThisFrame) SelectTopic(1);
                    if (keys.digit3Key.wasPressedThisFrame) SelectTopic(2);
                }
                if (!exterior && string.IsNullOrEmpty(observation))
                {
                    float h = (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0);
                    float v = (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0);
                    Vector3 direction = cameraRig != null ? cameraRig.Movement(new Vector2(h, v)) : Vector3.ClampMagnitude(new Vector3(h, 0, v), 1);
                    detective.Move((direction * walkSpeed + Vector3.down * 9.8f) * Time.deltaTime);
                    if (direction.sqrMagnitude > .01f) detective.transform.rotation = Quaternion.Slerp(detective.transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 12);
                    float nearestRoom = float.PositiveInfinity;
                    for (int i = 0; i < roomViews.Length; i++)
                    {
                        float distance = Mathf.Abs(detective.transform.position.x - roomViews[i].position.x);
                        if (distance < nearestRoom) { nearestRoom = distance; currentRoom = i; }
                    }
                    FindNearby();
                    if (keys.eKey.wasPressedThisFrame && nearby != null) OpenObservation(nearby);
                }
            }
            UpdateCamera();
        }

        private void FindNearby()
        {
            nearby = null;
            if (exterior) return;
            float best = float.PositiveInfinity;
            foreach (var item in TrainObservation.Active)
            {
                if (narrative != null && !narrative.ObservationAvailable(item)) continue;
                float distance = Vector3.Distance(detective.transform.position + Vector3.up, item.transform.position);
                if (distance <= item.interactionRange)
                {
                    float score = distance - (item.speaker != null ? .8f : 0);
                    if (score < best) { best = score; nearby = item; }
                }
            }
        }

        private void UpdateCamera()
        {
            if (cameraRig != null) return;
            Transform target = exterior ? exteriorView : roomViews[currentRoom];
            Vector3 position = target.position;
            if (!exterior)
            {
                float playerX = detective.transform.position.x;
                if (Mathf.Abs(playerX - CameraFollowX) > 1.7f)
                    CameraFollowX = playerX - Mathf.Sign(playerX - CameraFollowX) * 1.7f;
                CameraFollowX = Mathf.Clamp(CameraFollowX, MinimumRoomX(), MaximumRoomX());
                position.x = CameraFollowX;
            }
            float blend = 1 - Mathf.Exp(-Time.deltaTime * 5);
            viewCamera.transform.SetPositionAndRotation(Vector3.Lerp(viewCamera.transform.position, position, blend), Quaternion.Slerp(viewCamera.transform.rotation, target.rotation, blend));
            viewCamera.orthographicSize = Mathf.Lerp(viewCamera.orthographicSize, exterior ? exteriorSize : InteriorCameraSize(), blend);
        }

        public float InteriorCameraSize() => Mathf.Max(interiorSize, 6.8f / Mathf.Max(.4f, viewCamera.aspect));

        public void SnapCamera()
        {
            if (cameraRig != null) { cameraRig.Refresh(true); return; }
            Transform target = exterior ? exteriorView : roomViews[currentRoom];
            CameraFollowX = Mathf.Clamp(detective.transform.position.x, MinimumRoomX(), MaximumRoomX());
            Vector3 position = target.position;
            if (!exterior) position.x = CameraFollowX;
            viewCamera.transform.SetPositionAndRotation(position, target.rotation);
            viewCamera.orthographicSize = exterior ? exteriorSize : InteriorCameraSize();
        }

        public void SetView(int room, bool moveDetective)
        {
            currentRoom = Mathf.Clamp(room, 0, roomViews.Length - 1);
            SetExterior(false);
            if (moveDetective)
            {
                detective.enabled = false;
                detective.transform.position = new Vector3(roomViews[currentRoom].position.x, 1.12f, -.7f);
                detective.enabled = true;
                CameraFollowX = roomViews[currentRoom].position.x;
            }
            if (cameraRig != null) { cameraRig.ResetMovement(); cameraRig.Refresh(true); }
        }

        public void SetExterior(bool value)
        {
            if (cameraRig != null) { exterior = false; return; }
            exterior = value;
            foreach (GameObject shell in cutawayShells) shell.SetActive(value);
            if (foregroundScenery != null) foreach (var renderer in foregroundScenery) if (renderer != null) renderer.enabled = value;
            detective.gameObject.SetActive(!value);
            CloseObservation();
        }

        public bool TryTalkTo(TrainObservation item)
        {
            if (exterior || item == null || item.speaker == null) return false;
            if (Vector3.Distance(detective.transform.position + Vector3.up, item.transform.position) > item.interactionRange) return false;
            OpenObservation(item); return true;
        }
        public bool CanReach(TrainObservation item) => item != null && detective != null && !exterior &&
            Vector3.Distance(detective.transform.position + Vector3.up, item.transform.position) <= item.interactionRange;

        public void OpenObservation(TrainObservation item)
        {
            if (!CanReach(item) || (narrative != null && narrative.Busy)) return;
            if (narrative != null && narrative.Interact(item)) return;
            CloseObservation(); observation = item.description;
            currentSpeaker = item.speaker;
            if (currentSpeaker != null) { activeConversation = item; currentSpeaker.SetTalking(true); }
        }

        public void SelectTopic(int index)
        {
            if (activeConversation == null || activeConversation.responses == null || index < 0 || index >= activeConversation.responses.Length) return;
            observation = activeConversation.responses[index];
        }

        public void CloseObservation()
        {
            if (currentSpeaker != null) currentSpeaker.SetTalking(false);
            currentSpeaker = null; activeConversation = null; observation = null;
        }

        private void Styles()
        {
            if (panel != null) return;
            panel = new Texture2D(1, 1); panel.SetPixel(0, 0, new Color(.055f, .10f, .105f, .96f)); panel.Apply();
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 23, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = new Color(.96f, .88f, .70f);
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            smallStyle.normal.textColor = new Color(.79f, .81f, .75f);
            bodyStyle = new GUIStyle(titleStyle) { fontSize = 18, fontStyle = FontStyle.Normal, wordWrap = true };
            nameStyle = new GUIStyle(titleStyle) { fontSize = 17 };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, padding = new RectOffset(12, 12, 8, 8) };
        }

        private void OnGUI()
        {
            if (narrative != null && narrative.Busy) return;
            Styles();
            float scale = Mathf.Max(.1f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            GUI.DrawTexture(new Rect(24, 20, 360, 78), panel);
            GUI.Label(new Rect(40, 27, 340, 32), narrative != null ? "MURDER MOST INCONVENIENT" : "THE CARNIVAL EXPRESS", titleStyle);
            GUI.Label(new Rect(41, 65, 340, 24), exterior ? "A MOST PECULIAR JOURNEY" : roomNames[currentRoom], smallStyle);
            if (cameraRig != null)
            {
                if (GUI.Button(new Rect(width - 248, 24, 220, 35), cameraRig.Following ? "C / Use fixed cameras" : "C / Follow detective", buttonStyle)) cameraRig.ToggleFollow();
            }
            else if (GUI.Button(new Rect(width - 178, 24, 150, 35), exterior ? "Enter the train" : "View exterior", buttonStyle)) SetExterior(!exterior);
            if (!exterior && string.IsNullOrEmpty(observation))
                foreach (var item in TrainObservation.Active)
                {
                    if (item.speaker == null) continue;
                    Vector3 point = viewCamera.WorldToScreenPoint(item.speaker.transform.position + Vector3.up * 1.6f);
                    if (point.z < 0 || point.x < 0 || point.x > Screen.width) continue;
                    float x = point.x / scale, y = (Screen.height - point.y) / scale;
                    bool inRange = Vector3.Distance(detective.transform.position + Vector3.up, item.transform.position) <= item.interactionRange;
                    GUI.enabled = inRange;
                    if (GUI.Button(new Rect(x - 85, y - 38, 170, 30), inRange ? "Talk / " + item.speaker.role : item.speaker.role, buttonStyle)) TryTalkTo(item);
                    GUI.enabled = true;
                }
            if (string.IsNullOrEmpty(observation))
            {
                GUI.DrawTexture(new Rect(24, height - 62, width - 48, 38), panel);
                GUI.Label(new Rect(40, height - 53, width - 80, 28), cameraRig != null
                    ? "WASD / Arrows  Move     E  Talk / Inspect     1–3  Carriages     N  Logbook     C  Camera     Hold RMB  Look around in follow mode"
                    : "A / D  Walk along train     W / S  Move closer     E  Talk / Inspect     1–5  Carriages     N  Notebook     TAB  Exterior", smallStyle);
                if (nearby != null && GUI.Button(new Rect(width / 2 - 180, height - 112, 360, 36), "E  /  " + nearby.title, buttonStyle)) OpenObservation(nearby);
            }
            else
            {
                float top = height - (activeConversation != null ? 224 : 170);
                GUI.DrawTexture(new Rect(24, top, width - 48, height - top - 24), panel);
                GUI.Label(new Rect(44, top + 12, width - 230, 28), currentSpeaker != null ? currentSpeaker.role.ToUpperInvariant() : "OBSERVATION", nameStyle);
                GUI.Label(new Rect(44, top + 48, width - 88, 76), observation, bodyStyle);
                if (activeConversation != null && activeConversation.topics != null)
                {
                    float buttonWidth = (width - 112) / 3;
                    for (int i = 0; i < Mathf.Min(3, activeConversation.topics.Length); i++)
                        if (GUI.Button(new Rect(44 + i * (buttonWidth + 12), top + 137, buttonWidth, 42), (i + 1) + "  " + activeConversation.topics[i], buttonStyle)) SelectTopic(i);
                }
                if (GUI.Button(new Rect(width - 178, top + 10, 130, 30), "Goodbye / Esc", buttonStyle)) CloseObservation();
            }
        }

        private void OnDestroy() { if (panel != null) Destroy(panel); }
        private float MinimumRoomX() { float value = float.PositiveInfinity; foreach (var room in roomViews) value = Mathf.Min(value, room.position.x); return value; }
        private float MaximumRoomX() { float value = float.NegativeInfinity; foreach (var room in roomViews) value = Mathf.Max(value, room.position.x); return value; }
    }
}
