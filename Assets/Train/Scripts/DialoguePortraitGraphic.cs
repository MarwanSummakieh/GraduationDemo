using UnityEngine;
using UnityEngine.UI;

namespace GraduationDemo.Train
{
    // Original geometric stand-ins; replaceable independently of dialogue and speaker state.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DialoguePortraitGraphic : MaskableGraphic
    {
        public string speaker = "Burrows";
        public bool speaking, innerVoice, reducedMotion;
        public float AnimationPhase { get; private set; }
        Vector2 P(float x, float y) => new Vector2(rectTransform.rect.xMin + x * rectTransform.rect.width, rectTransform.rect.yMin + y * rectTransform.rect.height);
        void Poly(VertexHelper vh, Color c, params Vector2[] points)
        {
            int n = vh.currentVertCount; foreach (var p in points) vh.AddVert(P(p.x, p.y), c, Vector2.zero);
            for (int i = 1; i < points.Length - 1; i++) vh.AddTriangle(n, n + i, n + i + 1);
        }
        void Update() { AnimationPhase = reducedMotion ? 0 : Time.unscaledTime; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var cream = new Color(.97f, .9f, .78f); var black = new Color(.045f, .035f, .07f);
            var accent = innerVoice ? NeuralInk.Cyan : speaker == "Mila" ? new Color(.92f, .36f, .5f) : speaker == "Felix" ? new Color(.95f, .67f, .28f) : speaker == "Bellamy" ? new Color(.55f, .66f, .9f) : new Color(.78f, .8f, .73f);
            if (!speaking) { accent *= .55f; cream *= .6f; }
            Poly(vh, accent, new Vector2(.02f, .02f), new Vector2(.12f, .9f), new Vector2(.88f, 1), new Vector2(.98f, .13f));
            Poly(vh, black, new Vector2(.07f, .04f), new Vector2(.17f, .88f), new Vector2(.83f, .96f), new Vector2(.92f, .15f));
            Poly(vh, accent, new Vector2(.12f, .06f), new Vector2(.23f, .3f), new Vector2(.43f, .36f), new Vector2(.61f, .36f), new Vector2(.8f, .28f), new Vector2(.89f, .08f));
            Poly(vh, cream, new Vector2(.43f, .38f), new Vector2(.59f, .38f), new Vector2(.63f, .28f), new Vector2(.52f, .17f), new Vector2(.38f, .29f));
            Poly(vh, cream, new Vector2(.3f, .71f), new Vector2(.4f, .84f), new Vector2(.67f, .8f), new Vector2(.73f, .65f), new Vector2(.65f, .43f), new Vector2(.52f, .36f), new Vector2(.37f, .45f));
            Poly(vh, new Color(.64f, .53f, .49f), new Vector2(.57f, .78f), new Vector2(.67f, .8f), new Vector2(.73f, .65f), new Vector2(.65f, .43f), new Vector2(.52f, .36f));
            if (speaker == "Burrows")
            {
                Poly(vh, black, new Vector2(.26f, .77f), new Vector2(.35f, .93f), new Vector2(.65f, .94f), new Vector2(.76f, .78f));
                Poly(vh, cream, new Vector2(.19f, .74f), new Vector2(.79f, .79f), new Vector2(.83f, .73f), new Vector2(.24f, .69f));
                Poly(vh, black, new Vector2(.39f, .49f), new Vector2(.52f, .52f), new Vector2(.65f, .46f), new Vector2(.5f, .46f));
            }
            else if (speaker == "Mila")
            {
                Poly(vh, accent, new Vector2(.32f, .77f), new Vector2(.27f, .98f), new Vector2(.41f, .92f), new Vector2(.46f, .76f));
                Poly(vh, accent, new Vector2(.57f, .78f), new Vector2(.64f, .99f), new Vector2(.74f, .94f), new Vector2(.7f, .74f));
                Poly(vh, black, new Vector2(.27f, .72f), new Vector2(.36f, .85f), new Vector2(.62f, .87f), new Vector2(.76f, .73f), new Vector2(.48f, .76f));
            }
            else
                Poly(vh, black, new Vector2(.27f, .72f), new Vector2(.34f, .88f), new Vector2(.67f, speaker == "Felix" ? .94f : .86f), new Vector2(.76f, .7f), new Vector2(.51f, .79f));
            if (speaker != "Burrows") Poly(vh, accent, new Vector2(.29f, .66f), new Vector2(.71f, .69f), new Vector2(.68f, .56f), new Vector2(.52f, .61f), new Vector2(.34f, .55f));
            bool blink = Mathf.Repeat(AnimationPhase + (speaker == "Burrows" ? .7f : 0), 4.3f) > 4.15f;
            NeuralInk.Line(vh, P(.37f, .63f), P(.46f, blink ? .63f : .65f), black, 3);
            NeuralInk.Line(vh, P(.57f, blink ? .64f : .66f), P(.66f, .64f), black, 3);
            float mouth = speaking && !reducedMotion ? Mathf.Max(0, Mathf.Sin(AnimationPhase * 12)) * .015f : 0;
            NeuralInk.Line(vh, P(.47f, .43f), P(.58f, .44f - mouth), black, speaking ? 3 : 2);
            if (innerVoice) NeuralInk.Neuron(vh, P(.82f, .37f), rectTransform.rect.width * .08f, NeuralInk.Cyan, .4f);
        }
    }
}
