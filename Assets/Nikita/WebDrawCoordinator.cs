using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WebDrawCoordinator : MonoBehaviour
{
    public static WebDrawCoordinator Instance { get; private set; }
    [Header("Boards")]
    public WebDrawLogic main;     // player draws here (drawable = true)
    public WebDrawLogic presetA;  // presets (drawable = false)
    public WebDrawLogic presetB;
    public WebDrawLogic presetC;

    [Header("Preset visuals")]
    public Color32 goodColor = new Color32(0, 255, 0, 255);   // when a line matches main
    public Color32 badColor = new Color32(255, 0, 0, 255);   // default color on presets
    [Range(0f, 1f)] public float badAlpha = 0.5f;             // faded when a drawn line is missing

    [Header("Pattern options")]
    public bool distinctPatterns = true; // try to pick three different patterns if available

    [System.Serializable]
    public struct Pair { public int a; public int b; }

    [System.Serializable]
    public class Pattern
    {
        public string name;
        public Pair[] edges;
    }

    public Text stringAmountText;
    public int strings = 100;
    public int GetStrings()
    {
        return strings;
    }
    public void SetStrings(int value)
    {
        strings = value;
    }

    [Header("Pattern library (fill here or let script auto-fill a default set)")]
    public Pattern[] patterns;

    // we keep just the edges the player has drawn on MAIN
    private readonly HashSet<EdgeKey> _mainEdges = new HashSet<EdgeKey>();

    // pattern sets for presets (what each preset currently displays)
    private HashSet<EdgeKey> _patA, _patB, _patC;

    // canvas groups for fading
    private CanvasGroup _cgA, _cgB, _cgC;

    // normalized undirected edge for our own comparisons
    private struct EdgeKey
    {
        public int a, b; // always (min, max)
        public EdgeKey(int i, int j)
        {
            if (i <= j) { a = i; b = j; } else { a = j; b = i; }
        }
        public override int GetHashCode() { return (a * 397) ^ b; }
        public override bool Equals(object obj)
        {
            if (!(obj is EdgeKey)) return false;
            var o = (EdgeKey)obj;
            return a == o.a && b == o.b;
        }
    }
    private void Start()
    {
        stringAmountText.text = strings.ToString();
        NewRound();
    }
    void Awake()
    {
        // cache CanvasGroups on presets (optional)
        if (presetA) _cgA = presetA.GetComponent<CanvasGroup>();
        if (presetB) _cgB = presetB.GetComponent<CanvasGroup>();
        if (presetC) _cgC = presetC.GetComponent<CanvasGroup>();

        // if no patterns provided in Inspector, build a default library
        if (patterns == null || patterns.Length == 0) BuildDefaultPatterns();
    }

    void OnEnable()
    {
        if (main != null)
        {
            main.OnEdgeCreated += HandleMainEdgeCreated;
            main.OnResetEvent += HandleMainReset;
        }
    }

    void OnDisable()
    {
        if (main != null)
        {
            main.OnEdgeCreated -= HandleMainEdgeCreated;
            main.OnResetEvent -= HandleMainReset;
        }
    }

    void Update()
    {
        // new round
        /*if (Input.GetKeyDown(KeyCode.W))
            NewRound();
        */
        // submit
        /*if (Input.GetKeyDown(KeyCode.S))
            Submit();*/
    }

    // ================== gameplay flow ==================

    public void NewRound()
    {
        // reset all boards
        if (main) main.ResetGraph();
        if (presetA) presetA.ResetGraph();
        if (presetB) presetB.ResetGraph();
        if (presetC) presetC.ResetGraph();

        // main edges memory
        _mainEdges.Clear();

        // set presets to full alpha and red lines
        ResetPresetVisuals(presetA, _cgA);
        ResetPresetVisuals(presetB, _cgB);
        ResetPresetVisuals(presetC, _cgC);

        // pick patterns
        if (patterns == null || patterns.Length == 0) return;

        int count = patterns.Length;

        int iA = Random.Range(0, count);
        int iB = Random.Range(0, count);
        int iC = Random.Range(0, count);

        if (distinctPatterns && count >= 3)
        {
            // make them distinct (simple re-roll approach)
            while (iB == iA) iB = Random.Range(0, count);
            while (iC == iA || iC == iB) iC = Random.Range(0, count);
        }

        // apply patterns
        _patA = ApplyPatternToPreset(presetA, patterns[iA]);
        _patB = ApplyPatternToPreset(presetB, patterns[iB]);
        _patC = ApplyPatternToPreset(presetC, patterns[iC]);
    }

    public void Submit()
    {
        // compare main edges to each preset pattern
        if (Matches(_mainEdges, _patA) || Matches(_mainEdges, _patB) || Matches(_mainEdges, _patC))
        {
            Debug.Log("good");
            NewRound();
        }
        else
        {
            Debug.Log("bad");
        }
    }

    // ================== main board events ==================

    private void HandleMainEdgeCreated(int a, int b)
    {
        var k = new EdgeKey(a, b);
        _mainEdges.Add(k);

        // visual feedback on presets (green if present, else fade/red)
        CheckPresetVisual(presetA, _cgA, a, b);
        CheckPresetVisual(presetB, _cgB, a, b);
        CheckPresetVisual(presetC, _cgC, a, b);

        stringAmountText.text = strings.ToString();
    }

    private void HandleMainReset()
    {
        _mainEdges.Clear();

        // reset preset visuals
        ResetPresetVisuals(presetA, _cgA);
        ResetPresetVisuals(presetB, _cgB);
        ResetPresetVisuals(presetC, _cgC);
        stringAmountText.text = strings.ToString();
    }

    // ================== helpers ==================

    private void CheckPresetVisual(WebDrawLogic preset, CanvasGroup cg, int a, int b)
    {
        if (preset == null) return;

        // does the preset have this exact line already drawn?
        if (preset.TryGetLineImage(a, b, out var img))
        {
            if (img) img.color = goodColor; // mark that specific line green
            if (cg) cg.alpha = 1f;        // keep full alpha
        }
        else
        {
            // not present: make every line red and fade the preset
            preset.SetAllLinesColor(badColor);
            if (cg) cg.alpha = badAlpha;
        }
    }

    private void ResetPresetVisuals(WebDrawLogic preset, CanvasGroup cg)
    {
        if (preset) preset.SetAllLinesColor(badColor);
        if (cg) cg.alpha = 1f;
    }

    private HashSet<EdgeKey> ApplyPatternToPreset(WebDrawLogic preset, Pattern p)
    {
        var set = new HashSet<EdgeKey>();
        if (preset == null || p == null || p.edges == null) return set;

        // build a tuple list to use WebDrawLogic.DrawEdgesInstant without touching its internals
        var tuples = new List<(int a, int b)>(p.edges.Length);
        for (int i = 0; i < p.edges.Length; i++)
        {
            int a = p.edges[i].a;
            int b = p.edges[i].b;
            tuples.Add((a, b));
            set.Add(new EdgeKey(a, b));
        }

        // draw all lines instantly on that preset, set all red
        preset.DrawEdgesInstant(tuples);
        preset.SetAllLinesColor(badColor);

        return set;
    }

    private bool Matches(HashSet<EdgeKey> a, HashSet<EdgeKey> b)
    {
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;

        foreach (var k in a)
            if (!b.Contains(k)) return false;

        return true;
    }

    // ================== default pattern library ==================

    private void BuildDefaultPatterns()
    {
        // All patterns obey your move rules:
        // - center <-> inner
        // - inner neighbors
        // - inner <-> outer on same spoke
        // - outer neighbors

        var list = new List<Pattern>();

        list.Add(Make("InnerChain_12_14", P(24, 12), P(12, 13), P(13, 14)));
        list.Add(Make("InnerChain_16_18", P(24, 16), P(16, 17), P(17, 18)));
        list.Add(Make("InnerV_Out_4", P(24, 16), P(16, 17), P(16, 4)));
        list.Add(Make("InnerV_Out_7", P(24, 19), P(19, 18), P(19, 7)));
        list.Add(Make("OuterArc_0_2", P(24, 12), P(12, 0), P(0, 1), P(1, 2)));
        list.Add(Make("OuterArc_6_8", P(24, 18), P(18, 6), P(6, 7), P(7, 8)));
        list.Add(Make("SpokeCross_15", P(24, 15), P(15, 14), P(15, 3)));
        list.Add(Make("SpokeCross_3", P(24, 3 + 12), P(3 + 12, 4 + 12), P(3 + 12, 3))); // center->inner15, inner15->inner16, inner15->outer3
        list.Add(Make("SmallLoop_12_14", P(24, 12), P(12, 13), P(13, 14), P(14, 24)));

        patterns = list.ToArray();

        // local helpers
        Pattern Make(string name, params Pair[] edges) { return new Pattern { name = name, edges = edges }; }
        Pair P(int a, int b) { return new Pair { a = a, b = b }; }
    }
}
