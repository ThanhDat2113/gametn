using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public class MinimapMarkerUI : MonoBehaviour
{
    public enum OutOfRangeBehavior { Clamp, Hide }

    // ── Shape enum ────────────────────────────────────────────────────────────
    public enum MarkerShape
    {
        Circle,     // chấm tròn (dùng Texture2D procedural)
        Diamond,    // hình thoi (xoay 45°)
        Square,     // hình vuông
        Triangle,   // tam giác hướng lên (polygon mesh đơn giản)
        Star,       // ngôi sao 4 cánh (cross + diamond overlay)
        Ring,       // vòng tròn rỗng (outline)
    }

    [Header("Out-of-range Behavior")]
    [SerializeField] private OutOfRangeBehavior outOfRangeBehavior = OutOfRangeBehavior.Clamp;

    [Header("Marker Style")]
    [SerializeField] private MarkerShape shape = MarkerShape.Circle;
    [SerializeField] private Color       dotColor  = new Color(1f, 0.85f, 0f, 1f);
    [SerializeField] private Color       rimColor  = new Color(0f, 0f, 0f, 0.6f); // viền ngoài (tuỳ chọn)
    [SerializeField] private float       dotSize   = 14f;
    [Tooltip("Tự động đặt dotSize bằng kích thước playerIcon trên minimap (lấy từ MinimapController). " +
             "Khi bật, giá trị dotSize trong Inspector bị bỏ qua.")]
    [SerializeField] private bool        matchPlayerIconSize = true;
    [Tooltip("Độ dày viền tối (pixel). 0 = không vẽ viền.")]
    [SerializeField] private float       rimWidth  = 1.5f;
    [Tooltip("Chỉ dùng cho Ring: độ dày vành (0‒1 so với radius).")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float       ringThickness = 0.25f;

    [Header("Minimap")]
    [SerializeField] private float edgePadding = 8f;

    // ── Runtime refs ──────────────────────────────────────────────────────────
    private RectTransform     _rectTransform;
    private CanvasGroup       _canvasGroup;
    private QuestMarkerBridge _targetBridge;

    // Một RawImage dùng Texture2D procedural vẽ bằng CPU — không cần asset nào.
    private RawImage  _rawImage;
    private Texture2D _tex;

    // Cache để rebuild texture khi property thay đổi
    private MarkerShape _builtShape;
    private Color       _builtColor;
    private Color       _builtRim;
    private float       _builtSize;
    private float       _builtRimWidth;
    private float       _builtRingThickness;

    public QuestMarkerBridge TargetBridge => _targetBridge;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup   = GetComponent<CanvasGroup>();
        RebuildTexture();
    }

    private void OnValidate()
    {
        // Cho phép xem trước trong Editor khi đổi shape/color/size
        if (Application.isPlaying) RebuildTextureIfDirty();
    }

    private void OnDestroy()
    {
        if (_tex != null) Destroy(_tex);
    }

    // ── Init ──────────────────────────────────────────────────────────────────

    public void InitializeFromBridge(QuestMarkerBridge bridge)
    {
        if (bridge == null) { Debug.LogError("[MinimapMarkerUI] bridge is null"); return; }
        _targetBridge = bridge;
        SyncSizeWithPlayer(); // áp dụng ngay lần đầu
    }

    /// <summary>
    /// Nếu matchPlayerIconSize = true, cập nhật dotSize theo PlayerIconSize của MinimapController.
    /// Gọi mỗi LateUpdate để tự động theo khi playerIcon thay đổi kích thước lúc runtime.
    /// </summary>
    private void SyncSizeWithPlayer()
    {
        if (!matchPlayerIconSize) return;
        if (MinimapController.Instance == null) return;

        float iconSize = MinimapController.Instance.PlayerIconSize;
        if (iconSize > 0f && !Mathf.Approximately(dotSize, iconSize))
            dotSize = iconSize;
    }

    // ── LateUpdate ────────────────────────────────────────────────────────────

    private void LateUpdate()
    {
        SyncSizeWithPlayer();   // đồng bộ kích thước với playerIcon (nếu matchPlayerIconSize = true)
        RebuildTextureIfDirty();

        if (_targetBridge == null || MinimapController.Instance == null) return;

        bool dialogueActive = DialogueBubbleUI.Instance != null && DialogueBubbleUI.Instance.IsShowing;
        if (dialogueActive) { _canvasGroup.alpha = 0f; return; }

        Vector2 mapPos = MinimapController.Instance.WorldToMinimapPosition(
            _targetBridge.MarkerPosition, out bool isWithinRange);

        if (isWithinRange)
        {
            _rectTransform.anchoredPosition = mapPos;
            _canvasGroup.alpha = 1f;
        }
        else
        {
            switch (outOfRangeBehavior)
            {
                case OutOfRangeBehavior.Clamp:
                    _rectTransform.anchoredPosition =
                        MinimapController.Instance.ClampToMinimapEdge(mapPos, edgePadding);
                    _canvasGroup.alpha = 1f;
                    break;
                case OutOfRangeBehavior.Hide:
                    _canvasGroup.alpha = 0f;
                    break;
            }
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void SetDotColor(Color color) { dotColor = color; }
    public void SetShape(MarkerShape s)  { shape    = s;     }
    public void SetDotSize(float size)   { dotSize  = size;  }
    public void SetActive(bool active)   => gameObject.SetActive(active);

    // ── Texture builder ───────────────────────────────────────────────────────

    private void RebuildTextureIfDirty()
    {
        if (_builtShape          == shape          &&
            _builtColor          == dotColor       &&
            _builtRim            == rimColor       &&
            _builtSize           == dotSize        &&
            _builtRimWidth       == rimWidth       &&
            _builtRingThickness  == ringThickness  &&
            _tex != null) return;

        RebuildTexture();
    }

    private void RebuildTexture()
    {
        // Kích thước texture = dotSize * 2 để AA mượt hơn, sau đó scale RawImage xuống.
        int res = Mathf.Max(8, Mathf.RoundToInt(dotSize) * 4);
        if (res % 2 != 0) res++;

        if (_tex != null) Destroy(_tex);
        _tex = new Texture2D(res, res, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp,
            name       = "MinimapMarkerTex"
        };

        Color[] pixels = new Color[res * res];
        float   half   = res * 0.5f;
        float   r      = half - 1f;          // radius vẽ (tính theo pixel texture)
        float   rim    = rimWidth / dotSize * r; // rim tương đối

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float px = x - half + 0.5f;
            float py = y - half + 0.5f;

            float alpha = SampleShape(px, py, r, rim, out bool isRim);
            pixels[y * res + x] = isRim
                ? new Color(rimColor.r, rimColor.g, rimColor.b, rimColor.a * alpha)
                : new Color(dotColor.r, dotColor.g, dotColor.b, dotColor.a * alpha);
        }

        _tex.SetPixels(pixels);
        _tex.Apply();

        // Gắn vào RawImage (tạo nếu chưa có)
        if (_rawImage == null)
        {
            GameObject go = new GameObject("MarkerDot", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(transform, false);
            _rawImage = go.GetComponent<RawImage>();

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
        }

        _rawImage.texture = _tex;
        _rawImage.GetComponent<RectTransform>().sizeDelta = Vector2.one * dotSize;
        _rectTransform.sizeDelta = Vector2.one * dotSize;

        // Cache
        _builtShape         = shape;
        _builtColor         = dotColor;
        _builtRim           = rimColor;
        _builtSize          = dotSize;
        _builtRimWidth      = rimWidth;
        _builtRingThickness = ringThickness;
    }

    /// <summary>
    /// Trả về alpha [0,1] của pixel tại (px,py) tính từ tâm, với anti-alias tự nhiên.
    /// isRim = true nếu pixel này thuộc lớp viền ngoài.
    /// </summary>
    private float SampleShape(float px, float py, float r, float rim, out bool isRim)
    {
        isRim = false;
        float dist; // "khoảng cách đến biên hình" — dương = trong, âm = ngoài

        switch (shape)
        {
            // ── Circle ────────────────────────────────────────────────────────
            case MarkerShape.Circle:
            default:
                dist = r - Mathf.Sqrt(px * px + py * py);
                break;

            // ── Ring (vòng tròn rỗng) ─────────────────────────────────────────
            case MarkerShape.Ring:
            {
                float d   = Mathf.Sqrt(px * px + py * py);
                float th  = r * ringThickness;           // độ dày vành (pixel)
                float inner = r - th;
                // dist dương khi nằm trong vành (r-th ≤ d ≤ r)
                dist = Mathf.Min(r - d, d - inner);
                break;
            }

            // ── Square ────────────────────────────────────────────────────────
            case MarkerShape.Square:
                dist = r - Mathf.Max(Mathf.Abs(px), Mathf.Abs(py));
                break;

            // ── Diamond (hình thoi = Square xoay 45°) ─────────────────────────
            case MarkerShape.Diamond:
                dist = r - (Mathf.Abs(px) + Mathf.Abs(py));
                break;

            // ── Triangle (tam giác hướng lên) ─────────────────────────────────
            case MarkerShape.Triangle:
            {
                // Đỉnh trên = (0, r), hai đáy = (±r, -r)
                // SDF tam giác đều bằng 3 half-plane
                float h = r * 2f;                     // chiều cao
                // Dịch y lên r*0.33 để tam giác canh giữa hơn
                float cy  = py + r * 0.33f;
                float d1  = cy - r;                   // cạnh trên (y ≤ r)
                float d2  =  px * Mathf.Sqrt(3f) * 0.5f - cy * 0.5f + r * 0.5f; // cạnh phải
                float d3  = -px * Mathf.Sqrt(3f) * 0.5f - cy * 0.5f + r * 0.5f; // cạnh trái
                dist = -Mathf.Max(d1, Mathf.Max(-d2, -d3));
                break;
            }

            // ── Star (ngôi sao 4 cánh = circle + diamond lớn hơn một chút) ───
            case MarkerShape.Star:
            {
                float dCircle  = r * 0.55f - Mathf.Sqrt(px * px + py * py);
                float dDiamond = r         - (Mathf.Abs(px) + Mathf.Abs(py));
                dist = Mathf.Max(dCircle, dDiamond);
                break;
            }
        }

        if (dist <= 0f) return 0f;         // ngoài hình

        // Anti-alias: fade 1 pixel ở rìa
        float alpha = Mathf.Clamp01(dist);

        // Viền (rim): 1 lớp mỏng phía trong rìa ngoài cùng
        if (rim > 0.01f && dist <= rim + 1f)
            isRim = true;

        return alpha;
    }
}