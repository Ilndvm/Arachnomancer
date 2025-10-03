using UnityEngine;
using UnityEngine.UI;

public class WebDrawLogic : MonoBehaviour
{
    [Header("A thin Image used as the line")]
    public Image linePrefab;

    [Header("Buttons to connect (use at least 2)")]
    public Button[] buttons;

    [Header("Optional: where to spawn lines (e.g., a 'Lines' RectTransform under the Canvas)")]
    public Transform lineParent;

    [Tooltip("Shorten the line on each end (pixels)")]
    public float endPadding = 0f;

    private RectTransform _previousPoint;
    private RectTransform _secondPoint;

    private int _currentIndex;


    private int _half;
    void Start()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];


            int idx = i;
            button.onClick.AddListener(() => OnButtonClicked(idx));
        }
        _currentIndex = buttons.Length-1;
        _previousPoint = buttons[_currentIndex].transform as RectTransform;
        //buttons[_currentIndex].enabled = false;
        _half = ((buttons.Length - 1) / 2);


    }
    bool IsNeighbor(int current, int other, int count)
    {
        int d = (other - current + count) % count;     // ring distance forward
        return d == 1 || d == count - 1;               // next or previous
    }
    void OnButtonClicked(int index)
    {
        Debug.Log($"Clicked button index: {index}");
        Debug.Log(_half);

        if(_currentIndex == buttons.Length-1 && index >= _half ) 
        { _secondPoint = buttons[index].transform as RectTransform;
           
        }

        else if((_currentIndex != buttons.Length - 1))
        {
            if(_currentIndex>= _half  )
            {
                if (_currentIndex - _half == index)
                {
                    _secondPoint = buttons[index].transform as RectTransform;
                    
                }
                else if(IsNeighbor(_currentIndex,index,_half))
                {
                    _secondPoint = buttons[index].transform as RectTransform;
                   
                }
                
            }
            else if (_currentIndex < _half  )
            {
                if (_currentIndex + _half == index)
                {
                    _secondPoint = buttons[index].transform as RectTransform;
                    
                }
                else if (IsNeighbor(_currentIndex, index, _half))
                {
                    _secondPoint = buttons[index].transform as RectTransform;
                    
                }
            }
            

        }

        if (_secondPoint == null) return;
        DrawLineBetween(_previousPoint, _secondPoint);
        _previousPoint = buttons[index].transform as RectTransform;
        _secondPoint = null;
        _currentIndex = index;
        //buttons[_currentIndex].enabled = false;


        // optional: also log its name
        // Debug.Log($"[{index}] {buttons[index].name}");
    }

    void DrawLineBetween(RectTransform from, RectTransform to)
    {
        // Spawn the line under the given parent (fallback: canvas root)
        Transform parent = lineParent;
        if (!parent)
        {
            var canvas = from.GetComponent<Canvas>();
            if (canvas) parent = canvas.transform;
        }

        Image line = Instantiate(linePrefab, parent);
        RectTransform rt = line.rectTransform;

        // World-space centers of the rects
        Vector3 a = from.TransformPoint(from.rect.center);
        Vector3 b = to.TransformPoint(to.rect.center);

        Vector3 dir = b - a;
        float len = dir.magnitude;
        if (len < 0.001f) return;

        // Position at midpoint
        rt.position = a + dir * 0.5f;

        // Rotate to face B
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.rotation = Quaternion.Euler(0, 0, angle);

        // Set line length along X (keep Y as thickness from prefab)
        float adjusted = Mathf.Max(0f, len - endPadding * 2f);
        rt.sizeDelta = new Vector2(adjusted, rt.sizeDelta.y);

        // Make sure it doesn't block button clicks
        line.raycastTarget = false;
    }
}
