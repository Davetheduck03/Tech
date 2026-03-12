using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to any enemy prefab. Builds a world-space health bar automatically —
/// no prefab or canvas setup required.
///
/// The bar is 1 Unity-unit wide and sits above the enemy. It turns red as health
/// drops and faces the camera every frame.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("Layout")]
    [Tooltip("How far above the pivot the bar floats.")]
    [SerializeField] private float verticalOffset = 2f;
    [Tooltip("Width of the bar in world units.")]
    [SerializeField] private float barWidth = 1f;
    [Tooltip("Height of the bar in world units.")]
    [SerializeField] private float barHeight = 0.12f;

    [Header("Colors")]
    [SerializeField] private Color fullColor    = Color.green;
    [SerializeField] private Color emptyColor   = Color.red;
    [SerializeField] private Color bgColor      = new Color(0.08f, 0.08f, 0.08f, 0.85f);

    // Set via code — the bar stays hidden when health is full to reduce clutter
    [SerializeField] private bool hideWhenFull = true;

    private HealthComponent healthComp;
    private Image fillImage;
    private Canvas canvas;

    private void Awake()
    {
        healthComp = GetComponent<HealthComponent>();

        if (healthComp == null)
        {
            Debug.LogWarning($"[EnemyHealthBar] No HealthComponent found on {gameObject.name}. Bar disabled.");
            enabled = false;
            return;
        }

        BuildCanvas();
    }

    private void BuildCanvas()
    {
        // --- Canvas (world space) ---
        GameObject canvasGO = new GameObject("HealthBar_Canvas");
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, verticalOffset, 0f);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Size the RectTransform so that localScale = 1 equals barWidth × barHeight world units
        // We use a canvas of 100 × 10 canvas-units scaled down.
        float scaleX = barWidth  / 100f;
        float scaleY = barHeight / 10f;
        canvasGO.transform.localScale = new Vector3(scaleX, scaleY, 1f);

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100f, 10f);

        // --- Background ---
        GameObject bgGO = new GameObject("BG");
        bgGO.transform.SetParent(canvasGO.transform, false);

        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = bgColor;

        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // --- Fill ---
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(canvasGO.transform, false);

        fillImage = fillGO.AddComponent<Image>();
        fillImage.color = fullColor;
        fillImage.type  = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0; // left-to-right
        fillImage.fillAmount = 1f;

        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        // 1-unit inset on each side (in canvas units) so it sits inside the bg
        fillRect.offsetMin = new Vector2(1f, 1f);
        fillRect.offsetMax = new Vector2(-1f, -1f);
    }

    private void LateUpdate()
    {
        if (healthComp == null || fillImage == null) return;

        float ratio = healthComp.maxHealth > 0f
            ? Mathf.Clamp01(healthComp.currentHealth / healthComp.maxHealth)
            : 0f;

        // Hide bar when full
        canvas.gameObject.SetActive(!hideWhenFull || ratio < 0.9999f);

        fillImage.fillAmount = ratio;
        fillImage.color = Color.Lerp(emptyColor, fullColor, ratio);

        // Billboard: always face the main camera
        if (Camera.main != null)
        {
            canvas.transform.LookAt(
                canvas.transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up
            );
        }
    }
}
