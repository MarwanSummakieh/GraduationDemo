using System;
using GraduationDemo.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace GraduationDemo.Train
{
    public sealed class DialoguePresentation : MonoBehaviour
    {
        TrainNarrative owner;
        GameObject root, intuition;
        RectTransform sheet, portraitRect;
        DialoguePortraitGraphic portrait, listener;
        Text speakerName, context, subtitle, status, insight, feedback;
        Button next, motive, opportunity, intuitionNext;
        Font font;
        string lastVoice;
        float entered;
        public string ActiveSpeaker { get; private set; }
        public bool InnerVoice { get; private set; }
        public bool Visible => root != null && root.activeSelf;
        public string CurrentSubtitle => subtitle == null ? null : subtitle.text;
        void Awake() { owner = GetComponent<TrainNarrative>(); }
        RectTransform Rect(Transform parent, string name, Vector2 at, Vector2 size)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = Vector2.zero; r.pivot = Vector2.zero; r.anchoredPosition = at; r.sizeDelta = size; return r;
        }
        Text Text(Transform parent, string name, Vector2 at, Vector2 size, int fontSize, Color color)
        {
            var r = Rect(parent, name, at, size); var text = r.gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = fontSize; text.color = color; text.raycastTarget = false;
            text.supportRichText = false; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; return text;
        }
        Button Button(Transform parent, string name, string title, Vector2 at, Vector2 size, Action action)
        {
            var r = Rect(parent, name, at, size); var image = r.gameObject.AddComponent<Image>(); image.color = new Color(.2f, .17f, .28f);
            var button = r.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
            var label = Text(r, "Label", new Vector2(10, 0), size - new Vector2(20, 0), 18, Color.white); label.alignment = TextAnchor.MiddleCenter; label.text = title; return button;
        }
        void Build()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            root = new GameObject("Dialogue / train and inner voice", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 90;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            sheet = Rect(root.transform, "Dialogue cutout", new Vector2(0, 18), new Vector2(1200, 220)); sheet.anchorMin = sheet.anchorMax = new Vector2(.5f, 0); sheet.pivot = new Vector2(.5f, 0);
            sheet.gameObject.AddComponent<DialogueCutoutGraphic>().raycastTarget = true;
            portraitRect = Rect(sheet, "Active speaker portrait", new Vector2(4, 16), new Vector2(225, 290));
            portrait = portraitRect.gameObject.AddComponent<DialoguePortraitGraphic>(); portrait.raycastTarget = false;
            var r = Rect(sheet, "Listener portrait", new Vector2(1060, 205), new Vector2(120, 150)); listener = r.gameObject.AddComponent<DialoguePortraitGraphic>(); listener.raycastTarget = false;
            speakerName = Text(sheet, "Dialogue speaker", new Vector2(263, 170), new Vector2(755, 32), 27, Color.white); speakerName.fontStyle = FontStyle.Bold;
            context = Text(sheet, "Dialogue context", new Vector2(268, 142), new Vector2(750, 22), 13, NeuralInk.Cyan);
            subtitle = Text(sheet, "Dialogue subtitle", new Vector2(268, 55), new Vector2(894, 83), 22, new Color(.97f, .94f, .89f));
            subtitle.resizeTextForBestFit = true; subtitle.resizeTextMinSize = 17; subtitle.resizeTextMaxSize = 22;
            status = Text(sheet, "Dialogue readiness", new Vector2(268, 20), new Vector2(540, 30), 14, NeuralInk.Cyan);
            next = Button(sheet, "Dialogue continue", "CONTINUE  /  SPACE  >", new Vector2(866, 14), new Vector2(290, 36), () => owner.ContinueDialogue());
            var panel = Rect(root.transform, "Intuition mind-space", new Vector2(0, 390), new Vector2(1040, 235)); panel.anchorMin = panel.anchorMax = new Vector2(.5f, 0); panel.pivot = new Vector2(.5f, 0);
            intuition = panel.gameObject; panel.gameObject.AddComponent<DialogueCutoutGraphic>().inner = true;
            panel.gameObject.AddComponent<RectMask2D>();
            NeuralFieldGraphic.Attach(panel, owner.configuration != null && owner.configuration.defenseReducedMotion);
            var title = Text(panel, "Intuition title", new Vector2(36, 188), new Vector2(960, 32), 24, NeuralInk.Cyan); title.text = "INTUITION / WHAT DOES THIS REVEAL?";
            insight = Text(panel, "Intuition evidence", new Vector2(36, 130), new Vector2(960, 52), 20, Color.white);
            feedback = Text(panel, "Intuition correction", new Vector2(36, 65), new Vector2(960, 60), 17, Color.white);
            motive = Button(panel, "Intuition motive", "MOTIVE", new Vector2(36, 20), new Vector2(310, 38), () => owner.ChooseIntuition(AccusationReasoning.Motive));
            opportunity = Button(panel, "Intuition opportunity", "OPPORTUNITY", new Vector2(362, 20), new Vector2(310, 38), () => owner.ChooseIntuition(AccusationReasoning.Opportunity));
            intuitionNext = Button(panel, "Intuition continue", "CONTINUE  /  SPACE  >", new Vector2(690, 10), new Vector2(310, 38), () => owner.ContinueDialogue());
        }
        void LateUpdate()
        {
            bool visible = owner.State != null && owner.CurrentVoiceId != null && !owner.NotebookOpen;
            if (!visible) { if (root != null) root.SetActive(false); return; }
            if (root == null) Build(); root.SetActive(true);
            var voice = owner.catalogue.Voice(owner.CurrentVoiceId); if (voice == null) return;
            bool reduced = owner.configuration != null && owner.configuration.defenseReducedMotion;
            if (lastVoice != voice.id) { lastVoice = voice.id; entered = Time.unscaledTime; }
            ActiveSpeaker = voice.speaker; InnerVoice = ActiveSpeaker == "Burrows" && (voice.innerThought || owner.DialoguePartner == null);
            portrait.speaker = ActiveSpeaker; portrait.speaking = true; portrait.innerVoice = InnerVoice; portrait.reducedMotion = reduced;
            listener.gameObject.SetActive(!InnerVoice && owner.DialoguePartner != null);
            listener.speaker = ActiveSpeaker == "Burrows" ? owner.DialoguePartner : "Burrows"; listener.speaking = false; listener.innerVoice = false; listener.reducedMotion = reduced;
            float enter = reduced ? 1 : Mathf.Clamp01((Time.unscaledTime - entered) / .22f);
            portraitRect.anchoredPosition = new Vector2(4 + (1 - enter) * -28, 16 + (reduced ? 0 : Mathf.Sin(Time.unscaledTime * 2.6f) * 2));
            portraitRect.localRotation = Quaternion.Euler(0, 0, reduced ? 0 : Mathf.Sin(Time.unscaledTime * 1.8f) * .6f);
            speakerName.text = ActiveSpeaker == "Burrows" ? "BASIL BURROWS" : ActiveSpeaker == "Felix" ? "FELIX MERCER" : ActiveSpeaker == "Mila" ? "MILA NOVAK" : ActiveSpeaker == "Bellamy" ? "EDGAR BELLAMY" : ActiveSpeaker;
            context.text = InnerVoice ? "INNER VOICE / THE DETECTIVE'S MIND" : "CARNIVAL EXPRESS / QUESTIONING";
            context.color = InnerVoice ? NeuralInk.Cyan : new Color(.95f, .7f, .4f);
            sheet.GetComponent<DialogueCutoutGraphic>().inner = InnerVoice;
            subtitle.text = voice.subtitle;
            status.text = owner.CurrentQuestionDoubtResolved ? "DOUBT RESOLVED / QUESTION READY" : InnerVoice ? "A thought, not a conclusion." : ActiveSpeaker == "Burrows" ? "A question, not an accusation." : "Testimony / interpret with care.";
            next.gameObject.SetActive(owner.IntuitionEntry == null); next.interactable = !owner.DialogueAudioPlaying;
            intuition.SetActive(owner.IntuitionEntry != null);
            if (intuition.activeSelf)
            {
                insight.text = owner.catalogue.EntryText(owner.IntuitionEntry);
                feedback.text = owner.AwaitingIntuition ? "Choose how this matters to your accusation." : owner.IntuitionFeedback;
                motive.gameObject.SetActive(owner.AwaitingIntuition); opportunity.gameObject.SetActive(owner.AwaitingIntuition);
                intuitionNext.gameObject.SetActive(!owner.AwaitingIntuition); intuitionNext.interactable = !owner.DialogueAudioPlaying;
            }
        }
        void OnDestroy() { if (root != null) Destroy(root); }
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DialogueCutoutGraphic : MaskableGraphic
    {
        public bool inner;
        void Update() { SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect; var accent = inner ? NeuralInk.Cyan : new Color(.98f, .34f, .34f);
            Vector2[] points = { new Vector2(r.xMin + 17, r.yMin), new Vector2(r.xMax - 25, r.yMin + 8), new Vector2(r.xMax, r.yMax - 28), new Vector2(r.xMin + 40, r.yMax) };
            for (int i = 0; i < 4; i++) vh.AddVert(points[i], inner ? new Color(.055f, .025f, .12f, .97f) : new Color(.045f, .035f, .055f, .97f), Vector2.zero);
            vh.AddTriangle(0, 1, 2); vh.AddTriangle(0, 2, 3);
            for (int i = 0; i < 4; i++) NeuralInk.Line(vh, points[i], points[(i + 1) % 4], accent, i == 2 ? 5 : 2);
            NeuralInk.Line(vh, points[3] + new Vector2(12, -10), points[2] + new Vector2(-38, -10), new Color(.95f, .93f, .87f), 2);
        }
    }
}
