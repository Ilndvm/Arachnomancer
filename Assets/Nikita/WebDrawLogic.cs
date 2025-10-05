using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class WebDrawLogic : MonoBehaviour
{
    [Header("A thin Image used as the line")]
    public Image linePrefab;

    [Header("Buttons: 0-11 Outer, 12-23 Inner, 24 Center")]
    public Button[] buttons;

    [Header("Where to spawn line Images (assign a RectTransform under your Canvas)")]
    public Transform lineParent;

    [Tooltip("Shorten the line on each end (pixels)")]
    public float endPadding = 0f;

    [Header("Pooling")]
    [Min(0)] public int initialPoolSize = 24;

    

    [Header("Selection Colors")]
    public Color32 colorIdle = new Color32(255, 0, 0, 255);
    public Color32 colorSelected = new Color32(255, 0, 255, 255);

    private RectTransform _previousPoint;
    private int _currentIndex;

    // layout derived from array
    private int _ringCount;      // 12
    private int _innerStart;     // 12
    private int _centerIndex;    // 24

    // ---------- pooling ----------
    private readonly Queue<Image> _pool = new Queue<Image>();
    private Transform _defaultParent;

    // active lines & edges
    private readonly Dictionary<Edge, Image> _activeLines = new Dictionary<Edge, Image>();
    private readonly HashSet<Edge> _edges = new HashSet<Edge>();

    // blinking guard
    private readonly HashSet<Image> _blinkBusy = new HashSet<Image>();


    public bool drawable;
    public bool drawingCanceled;
    public event System.Action<int, int> OnEdgeCreated; // fired when a new edge is added
    public event System.Action OnResetEvent;            // fired when ResetGraph() completes

    public bool TryGetLineImage(int a, int b, out Image img)
    {
        img = null;
        if (a < 0 || b < 0 || a >= buttons.Length || b >= buttons.Length) return false;
        var e = new Edge(a, b);
        return _activeLines.TryGetValue(e, out img);
    }

    public void SetAllLinesColor(Color32 color)
    {
        foreach (var kv in _activeLines)
            if (kv.Value) kv.Value.color = color;
    }
    // undirected edge normalized as (min,max)
    private readonly struct Edge : System.IEquatable<Edge>
    {
        public readonly int a, b;
        public Edge(int i, int j)
        {
            if (i <= j) { a = i; b = j; } else { a = j; b = i; }
        }
        public bool Equals(Edge other) => a == other.a && b == other.b;
        public override bool Equals(object obj) => obj is Edge e && Equals(e);
        public override int GetHashCode() => (a * 397) ^ b;
        public override string ToString() => "(" + a + "," + b + ")";
    }

    public void Init()
    {
        // Expect: outer(N) + inner(N) + center(1)
        _ringCount = (buttons.Length - 1) / 2;
        _innerStart = _ringCount;
        _centerIndex = buttons.Length - 1;

        // clicks
        for (int i = 0; i < buttons.Length; i++)
        {
            int idx = i; // capture
            
            if (buttons[i])
            {
                buttons[i].enabled = drawable;
                if (!drawable) continue;
                buttons[i].onClick.AddListener(() => OnButtonClicked(idx));
            }
                
        }

        // parent for pooled lines (no GetComponentInParent as requested)
        _defaultParent = lineParent
            ? lineParent
            : (buttons.Length > 0 ? buttons[0].GetComponent<Canvas>()?.transform : null);

        // prewarm pool
        if (linePrefab && initialPoolSize > 0 && _defaultParent)
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                var img = Instantiate(linePrefab, _defaultParent);
                img.raycastTarget = false;
                img.gameObject.SetActive(false);
                _pool.Enqueue(img);
            }
        }

        // start from center and color selection
        //ApplyButtonColor(_centerIndex, colorSelected);
        ResetGraph();
    }

    void Update()
    {/*
        if (!drawable) return;
        
        if (Input.GetKeyDown(KeyCode.R))
            ResetGraph();

        if (Input.GetKeyDown(KeyCode.Space))
            LogEdges();
        */
        
    }

    // ---------- adjacency logic ----------
    enum Ring { Outer, Inner, Center, Invalid }
    Ring GetRing(int index)
    {
        if (index == _centerIndex) return Ring.Center;
        if (index >= 0 && index < _ringCount) return Ring.Outer;
        if (index >= _innerStart && index < _innerStart + _ringCount) return Ring.Inner;
        return Ring.Invalid;
    }

    int Mod(int a, int m) => (a % m + m) % m;
    int RingIndex(int idx) => Mod(idx, _ringCount);           // 0..ringCount-1
    bool IsSameRadial(int a, int b) => RingIndex(a) == RingIndex(b);
    bool IsRingNeighbor(int a, int b)
    {
        if (GetRing(a) != GetRing(b)) return false;
        int d = Mod(RingIndex(b) - RingIndex(a), _ringCount);
        return d == 1 || d == _ringCount - 1; // +-1 (wrap)
    }

    bool CanConnect(int current, int next)
    {
        var rCur = GetRing(current);
        var rNxt = GetRing(next);

        if (rCur == Ring.Center) return rNxt == Ring.Inner; // center -> inner
        if (rCur == Ring.Inner)
        {
            if (rNxt == Ring.Inner && IsRingNeighbor(current, next)) return true; // inner neighbors
            if (rNxt == Ring.Outer && IsSameRadial(current, next)) return true;   // radial out
            if (rNxt == Ring.Center) return true;                                 // back to center
            return false;
        }
        if (rCur == Ring.Outer)
        {
            if (rNxt == Ring.Outer && IsRingNeighbor(current, next)) return true; // outer neighbors
            if (rNxt == Ring.Inner && IsSameRadial(current, next)) return true;   // radial in
            return false;                                                         // no center from outer
        }
        return false;
    }

    // ---------- clicks ----------
    void OnButtonClicked(int index)
    {
        if (index < 0 || index >= buttons.Length) return;
        if (!CanConnect(_currentIndex, index)) return;
        if (GameManager.Instance.blood < 1) return;
        var edge = new Edge(_currentIndex, index);

        // duplicate: blink existing line and do not move selection
        if (_edges.Contains(edge))
        {
            if (_activeLines.TryGetValue(edge, out var existing))
                StartCoroutine(BlinkLine(existing, 0.4f, 2));
            return;
        }

        var from = _previousPoint;
        var to = buttons[index].transform as RectTransform;
        GameManager.Instance.blood--;
        GameManager.Instance.UIManager.UpdateBloodText();
        var line = DrawLineBetween(from, to);
        if (line == null) return;

        _activeLines[edge] = line;
        _edges.Add(edge);
        OnEdgeCreated?.Invoke(edge.a, edge.b);
        // update selection colors
        ApplyButtonColor(_currentIndex, colorIdle);
        ApplyButtonColor(index, colorSelected);

        _previousPoint = to;
        _currentIndex = index;
    }

    // ---------- reset & logging ----------
    public void ResetGraph(bool reuseString = false)
    {
        // recycle active lines
        foreach (var kv in _activeLines)
        {
            if(drawable && reuseString) GameManager.Instance.blood++;
            Recycle(kv.Value);
        }
        _activeLines.Clear();
        _edges.Clear();

        // colors: set all to idle red, then center to selected magenta
        for (int i = 0; i < buttons.Length; i++)
        {
            
            ApplyButtonColor(i, colorIdle);
        }

        drawingCanceled = false;
        // return to center
        _currentIndex = _centerIndex;
        _previousPoint = buttons[_centerIndex].transform as RectTransform;

        OnResetEvent?.Invoke();
        if (drawable)
        {
            ApplyButtonColor(_centerIndex, colorSelected);
        }
    }

    public void LogEdges()
    {
        var list = new List<Edge>(_edges);
        list.Sort((e1, e2) =>
        {
            int c = e1.a.CompareTo(e2.a);
            return c != 0 ? c : e1.b.CompareTo(e2.b);
        });

        var sb = new StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append(list[i].ToString()); // (a,b)
        }
        Debug.Log(sb.Length > 0 ? sb.ToString() : "(empty)");
    }

    // ---------- draw-by-list (Q) ----------
    /*
    void DrawPresetEdges()
    {
        var edges = ParseEdgesString(presetEdges);
        if (edges.Count == 0)
        {
            Debug.Log("No edges parsed from presetEdges.");
            return;
        }
        DrawEdgesInstant(edges);
    }*/

    // Draws edges instantly: no movement checks, does not change selection.
    // Skips duplicates and blinks the existing line instead.
    private void DrawEdgesInstant(IEnumerable<Edge> edges)
    {
        foreach (var e in edges)
        {
            if (_edges.Contains(e))
            {
                if (_activeLines.TryGetValue(e, out var img))
                    StartCoroutine(BlinkLine(img, 0.4f, 2));
                continue;
            }

            if (e.a < 0 || e.a >= buttons.Length || e.b < 0 || e.b >= buttons.Length)
                continue;

            var from = buttons[e.a].transform as RectTransform;
            var to = buttons[e.b].transform as RectTransform;
            if (from == null || to == null) continue;

            var line = DrawLineBetween(from, to);
            if (line == null) continue;

            _activeLines[e] = line;
            _edges.Add(e);
            OnEdgeCreated?.Invoke(e.a, e.b);
        }
    }

    // Parser for strings like: (14,15),(14,24),(15,16)
    

   

    // ---------- pooling + drawing ----------
    Image GetLineFromPool()
    {
        Image line;
        if (_pool.Count > 0)
        {
            line = _pool.Dequeue();
        }
        else
        {
            if (!linePrefab || !_defaultParent)
            {
                Debug.LogWarning("Line pool: missing prefab or parent.");
                return null;
            }
            line = Instantiate(linePrefab, _defaultParent);
        }

        if (lineParent && line.transform.parent != lineParent)
            line.transform.SetParent(lineParent, false);
        line.raycastTarget = false;
        line.gameObject.SetActive(true);
        return line;
    }

    void Recycle(Image img)
    {
        if (!img) return;
        img.gameObject.SetActive(false);
        _pool.Enqueue(img);
    }

    Image DrawLineBetween(RectTransform from, RectTransform to)
    {
        var line = GetLineFromPool();
        if (!line) return null;

        RectTransform rt = line.rectTransform;
        var parentRT = (RectTransform)rt.parent;

        // 1) World positions of rect centers
        Vector3 aWorld = from.TransformPoint(from.rect.center);
        Vector3 bWorld = to.TransformPoint(to.rect.center);

        // 2) Direction in world (for rotation only)
        Vector3 dirWorld = bWorld - aWorld;
        float lenWorld = dirWorld.magnitude;
        if (lenWorld < 0.001f)
        {
            Recycle(line);
            return null;
        }

        // 3) Midpoint & rotation (world space is fine here)
        rt.position = aWorld + dirWorld * 0.5f;
        float angle = Mathf.Atan2(dirWorld.y, dirWorld.x) * Mathf.Rad2Deg;
        rt.rotation = Quaternion.Euler(0, 0, angle);

        // 4) Length in the LINE'S PARENT LOCAL SPACE (this removes double scaling)
        Vector3 aLocal = parentRT.InverseTransformPoint(aWorld);
        Vector3 bLocal = parentRT.InverseTransformPoint(bWorld);
        float lenLocal = (bLocal - aLocal).magnitude;

        // 5) Stretch along X using LOCAL length
        float adjustedLocal = Mathf.Max(0f, lenLocal - endPadding * 2f);
        rt.sizeDelta = new Vector2(adjustedLocal, rt.sizeDelta.y);

        return line;
    }

    // ---------- visuals ----------
    void ApplyButtonColor(int index, Color32 col)
    {
        if (index < 0 || index >= buttons.Length) return;
        var img = buttons[index] ? buttons[index].GetComponent<Image>() : null;
        if (img) img.color = col;
    }

    IEnumerator BlinkLine(Image img, float duration, int pulses)
    {
        if (img == null) yield break;
        if (_blinkBusy.Contains(img)) yield break;

        _blinkBusy.Add(img);
        Color original = img.color;
        float half = duration / (pulses * 2f);

        for (int i = 0; i < pulses; i++)
        {
            img.color = new Color(original.r, original.g, original.b, 0.35f);
            yield return new WaitForSeconds(half);
            img.color = original;
            yield return new WaitForSeconds(half);
        }

        img.color = original;
        _blinkBusy.Remove(img);
    }

    // ---------- optional: public API to draw from tuple list ----------
    // Use this from other scripts if you prefer passing tuples.
    public void DrawEdgesInstant(IEnumerable<(int a, int b)> pairs)
    {
        var edges = new List<Edge>();
        foreach (var p in pairs) edges.Add(new Edge(p.a, p.b));
        DrawEdgesInstant(edges);
    }
}
