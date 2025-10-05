using System.Collections.Generic;
using TMPro;
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
    public TextMeshProUGUI presetAText;  // presets (drawable = false)
    public TextMeshProUGUI presetBText;
    public TextMeshProUGUI presetCText;
    [Header("Preset visuals")]
    public Color32 goodColor = new Color32(0, 255, 0, 255);   // when a line matches main
    public Color32 badColor = new Color32(255, 0, 0, 255);   // default color on presets
    [Range(0f, 1f)] public float badAlpha = 0.5f;             // faded when a drawn line is missing

    [Header("Pattern options")]
    public bool distinctPatterns = true; // try to pick three different patterns if available

    public Settings settings;

    private List<Settings.Upgrade> availableUpgrades;
    [System.Serializable]
    public struct Pair { public int a; public int b; }

    [System.Serializable]
    public class Pattern
    {
        
        public Pair[] edges;
    }

    public TextMeshProUGUI bloodDropsAmountText;

    public Settings.Upgrade[] tutorialUpgrades;
    public bool tutorialComplete = false;
    /*[Header("Pattern library (fill here or let script auto-fill a default set)")]
    public Pattern[] patterns;*/

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
        main.Init();
        presetA.Init();
        presetB.Init();
        presetC.Init();
        availableUpgrades = new List<Settings.Upgrade>(settings.UpgradeArray); // shallow copy
        bloodDropsAmountText.text = GameManager.Instance.blood.ToString();
        /*GameManager.Instance.blood++;
        NewRound();*/
        Tutorial();
        
    }

    private void Tutorial()
    {
        _mainEdges.Clear();
        Debug.Log("Use blood to draw symbol on the web");
        GameManager.Instance.blood+=3;

        if (main) main.ResetGraph(false);
        if (presetA) presetA.ResetGraph();
        if (presetB) presetB.ResetGraph();
        if (presetC) presetC.ResetGraph();

        ResetPresetVisuals(presetA, _cgA);
        ResetPresetVisuals(presetB, _cgB);
        ResetPresetVisuals(presetC, _cgC);

        // pick patterns
        /*if (patterns == null || patterns.Length == 0) return;*/

        

        iA = 0;
        iB = 1;
        iC = 2;

        // apply patterns
        _patA = ApplyPatternToPreset(presetA, tutorialUpgrades[iA].pattern);
        presetAText.text = tutorialUpgrades[iA].description;
        _patB = ApplyPatternToPreset(presetB, tutorialUpgrades[iB].pattern);
        presetBText.text = tutorialUpgrades[iB].description;
        _patC = ApplyPatternToPreset(presetC, tutorialUpgrades[iC].pattern);
        presetCText.text = tutorialUpgrades[iC].description;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // cache CanvasGroups on presets (optional)
        if (presetA) _cgA = presetA.GetComponent<CanvasGroup>();
        if (presetB) _cgB = presetB.GetComponent<CanvasGroup>();
        if (presetC) _cgC = presetC.GetComponent<CanvasGroup>();

        // if no patterns provided in Inspector, build a default library
        /*if (patterns == null || patterns.Length == 0) BuildDefaultPatterns();*/
    }

    void OnEnable()
    {
        if (main != null)
        {
            main.OnEdgeCreated += HandleMainEdgeCreated;
            main.OnResetEvent += HandleMainReset;
        }
        bloodDropsAmountText.text = GameManager.Instance.blood.ToString();

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
    int iA;
    int iB;
    int iC;
    public void NewRound(bool stringReuse = true)
    {
        if (GameManager.Instance.blood <= 0 && stringReuse)
        {
            return;
        }
        if (!tutorialComplete)
        {
            
            Debug.Log("you can reroll templates to find better upgrade\nbut each reroll cost 1 blood");
            return;
        }
        if (stringReuse)
        {
            GameManager.Instance.blood--;
        }


        GameManager.Instance.UIManager.UpdateBloodText();
        bloodDropsAmountText.text = GameManager.Instance.blood.ToString();

        // reset all boards
        if (main) main.ResetGraph(stringReuse);
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
        /*if (patterns == null || patterns.Length == 0) return;*/

        int count = availableUpgrades.Count;

        iA = Random.Range(0, count);
        iB = Random.Range(0, count);
        iC = Random.Range(0, count);

        if (distinctPatterns && count >= 3)
        {
            // make them distinct (simple re-roll approach)
            while (iB == iA) iB = Random.Range(0, count);
            while (iC == iA || iC == iB) iC = Random.Range(0, count);
        }

        // apply patterns
        _patA = ApplyPatternToPreset(presetA, availableUpgrades[iA].pattern );
        presetAText.text = availableUpgrades[iA].description;
        _patB = ApplyPatternToPreset(presetB, availableUpgrades[iB].pattern);
        presetBText.text = availableUpgrades[iB].description;
        _patC = ApplyPatternToPreset(presetC, availableUpgrades[iC].pattern);
        presetCText.text = availableUpgrades[iC].description;
    }

    public void Submit()
    {
        
        // compare main edges to each preset pattern
        if (Matches(_mainEdges, _patA) )
        {
            if (tutorialComplete) {
                UpgradeManager.Instance.TryUpgrade(availableUpgrades[iA].upgradeType);
                Debug.Log($"you obtained upgade: {availableUpgrades[iA].upgradeType}");
                if (availableUpgrades[iA].isUnique) availableUpgrades.RemoveAt(iA);

            }
            else
            {
                Debug.Log($"you obtained upgade: {tutorialUpgrades[iA].description}");
                tutorialComplete = true;
            }
            NewRound(false);

            
        }
        else if(Matches(_mainEdges, _patB))
        {
            if (tutorialComplete)
            {
                UpgradeManager.Instance.TryUpgrade(availableUpgrades[iB].upgradeType);
                Debug.Log($"you obtained upgade: {availableUpgrades[iB].upgradeType}");
                if (availableUpgrades[iB].isUnique) availableUpgrades.RemoveAt(iB);

            }
            else
            {
                Debug.Log($"you obtained upgade: {tutorialUpgrades[iB].description}");
                tutorialComplete = true;

            }
            NewRound(false);
        }
        else if(Matches(_mainEdges, _patC))
        {
            if (tutorialComplete)
            {
                UpgradeManager.Instance.TryUpgrade(availableUpgrades[iC].upgradeType);
                Debug.Log($"you obtained upgade: {availableUpgrades[iC].upgradeType}");
                if (availableUpgrades[iC].isUnique) availableUpgrades.RemoveAt(iC);

            }
            else
            {
                Debug.Log($"you obtained upgade: {tutorialUpgrades[iC].description}");
                tutorialComplete = true;

            }
            NewRound(false);
        }
        else
        {
            Debug.Log("incorect drawing\ntry again");
        }
    }

    // ================== main board events ==================

    private void HandleMainEdgeCreated(int a, int b)
    {
        var k = new EdgeKey(a, b);
        _mainEdges.Add(k);
        if (!tutorialComplete)
        {
            Debug.Log("if line is correct you will see green line\nelse you can start from beginning by pressing button");
        }
        // visual feedback on presets (green if present, else fade/red)
        CheckPresetVisual(presetA, _cgA, a, b);
        CheckPresetVisual(presetB, _cgB, a, b);
        CheckPresetVisual(presetC, _cgC, a, b);

        bloodDropsAmountText.text = GameManager.Instance.blood.ToString();
    }

    private void HandleMainReset()
    {
        _mainEdges.Clear();

        // reset preset visuals
        ResetPresetVisuals(presetA, _cgA);
        ResetPresetVisuals(presetB, _cgB);
        ResetPresetVisuals(presetC, _cgC);
        bloodDropsAmountText.text = GameManager.Instance.blood.ToString();
    }

    // ================== helpers ==================

    private void CheckPresetVisual(WebDrawLogic preset, CanvasGroup cg, int a, int b)
    {
        if (preset == null) return;
        if (preset.drawingCanceled) return;
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
            preset.drawingCanceled = true;
        }
    }

    private void ResetPresetVisuals(WebDrawLogic preset, CanvasGroup cg)
    {
        if (preset) preset.SetAllLinesColor(badColor);
        if (cg) cg.alpha = 1f;


        preset.drawingCanceled = false;
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

    /*private void BuildDefaultPatterns()
    {
        // All patterns obey your move rules:
        // - center <-> inner
        // - inner neighbors
        // - inner <-> outer on same spoke
        // - outer neighbors

        var list = new List<Pattern>();

        list.Add(Make(P(24, 12), P(12, 13), P(13, 14)));
        list.Add(Make(P(24, 16), P(16, 17), P(17, 18)));
        list.Add(Make(P(24, 16), P(16, 17), P(16, 4)));
        list.Add(Make(P(24, 19), P(19, 18), P(19, 7)));
        list.Add(Make( P(24, 12), P(12, 0), P(0, 1), P(1, 2)));
        list.Add(Make( P(24, 18), P(18, 6), P(6, 7), P(7, 8)));
        list.Add(Make( P(24, 15), P(15, 14), P(15, 3)));
        list.Add(Make(P(24, 3 + 12), P(3 + 12, 4 + 12), P(3 + 12, 3))); // center->inner15, inner15->inner16, inner15->outer3
        list.Add(Make( P(24, 12), P(12, 13), P(13, 14), P(14, 24)));

        patterns = list.ToArray();

        // local helpers
        Pattern Make(params Pair[] edges) { return new Pattern { edges = edges }; }
        Pair P(int a, int b) { return new Pair { a = a, b = b }; }
    }*/
}
