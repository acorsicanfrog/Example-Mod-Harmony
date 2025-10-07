using TMPro;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using LeTai.Asset.TranslucentImage;

/// <summary>
/// Minimal, self-contained example showing how to dynamically build UI at runtime
/// 
/// Notes for modders:
/// - Call StartExample() once to spawn the demo window + button.
/// - This example uses a single shared translucent material to avoid per-object allocations.
/// - You can switch asset loading between embedded (from the DLL) and disk via LOAD_RESOURCES_FROM_DLL.
/// </summary>
class UIExample
{
    // -------------------------------------------------------------------------
    // Configuration
    // -------------------------------------------------------------------------

    /// <summary>Switch to FALSE to load resources from disk instead of from the embedded DLL.</summary>
    private const bool LOAD_RESOURCES_FROM_DLL = true;

    /// <summary>Shader used by LeTai Translucent Image.</summary>
    private const string TRANSLUCENT_SHADER = "UI/TranslucentImage";

    /// <summary>Embedded resource path for the example button/window sprite.</summary>
    private const string EMBEDDED_BUTTON_SPRITE = "ExampleMod.Graphics.UI.Button.png";

    /// <summary>Disk path (relative to the mod install location) to the example sprite.</summary>
    private static readonly string DISK_BUTTON_SPRITE = Path.Combine(ExampleMod.modInstallLocation, "Graphics", "UI", "Buttons", "Button.png");

    /// <summary>Default 9-slice border in pixels (L, B, R, T all the same if using the uniform overload).</summary>
    private const float DEFAULT_SLICE_BORDER_PX = 30f;

    /// <summary>Default pixels-per-unit used when creating sprites.</summary>
    private const float DEFAULT_PPU = 100f;

    /// <summary>Default translucency blending (0..1) for TranslucentImage.</summary>
    private const float DEFAULT_SPRITE_BLENDING = 0.65f;

    /// <summary>Example window default size.</summary>
    private static readonly Vector2 DEFAULT_WINDOW_SIZE = new Vector2(500f, 250f);

    /// <summary>Example button default size.</summary>
    private static readonly Vector2 DEFAULT_BUTTON_SIZE = new Vector2(200f, 50f);

    // Colors used by the base game for interactive UI elements.
    private static readonly Color Grey = new Color(0.588f, 0.6f, 0.611f);
    private static readonly Color Yellow = new Color(1f, 0.8f, 0f);

    // Lifecycle
    private static bool _initialized;

    // Reuse the same material across all translucent images to save allocations & draw calls.
    private static Material _sharedTranslucentMat;

    // -------------------------------------------------------------------------
    // Entry point
    // -------------------------------------------------------------------------

    /// <summary>
    /// Entry point to run the example. Safe to call multiple times.
    /// </summary>
    public static void StartExample()
    {
        Initialize();
        CreateExampleMenu();
    }

    /// <summary>
    /// One-time setup for shared resources (material, etc.).
    /// Must be called before creating any UI that uses the translucent shader.
    /// </summary>
    private static void Initialize()
    {
        if (_initialized) return;

        // Create the shared material once. This avoids per-element material instances.
        _sharedTranslucentMat = new Material(Shader.Find(TRANSLUCENT_SHADER));
        _sharedTranslucentMat.SetFloat("_Vibrancy", 1.8f);

        _initialized = true;
    }

    // -------------------------------------------------------------------------
    // Demo UI
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds a simple window with a single button to demonstrate dynamic UI creation.
    /// </summary>
    private static void CreateExampleMenu()
    {
        // Create a centered window.
        GameObject window = CreateWindow("Example Window", DEFAULT_WINDOW_SIZE);

        // Create a button with localized text.
        Button button = CreateButton("Example Button", DEFAULT_BUTTON_SIZE, "ExampleMod.UI.iamabutton", FontStyles.UpperCase);

        // Parent the button to the window so it sits on top.
        button.transform.SetParent(window.transform, false);

        // Make the background of the button translucent and 9-sliced.
        // NOTE: We add TranslucentImage onto the *button* GameObject itself (background).
        Image buttonBackground = button.gameObject.AddComponent<TranslucentImage>();
        ConfigureTranslucentImage((TranslucentImage)buttonBackground, DEFAULT_SPRITE_BLENDING);
        buttonBackground.raycastTarget = true; // block clicks "through" the button

        // Load the demo sprite (from DLL or disk) and apply it as sliced to the background image.
        Sprite sprite = LoadButtonSprite();
        ApplyToImage(buttonBackground, sprite, p_pixelPerUnitMultiplier: 3f);

        // Hook up the click action.
        button.onClick.AddListener(PerformButtonAction);
    }

    /// <summary>
    /// Example action for the demo button.
    /// </summary>
    private static void PerformButtonAction()
    {
        UIManager.ShowMessage("IT HURT!\nDON'T DO THAT AGAIN.");
    }

    // -------------------------------------------------------------------------
    // UI Factories
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a centered window GameObject with an (optional) translucent, 9-sliced background.
    /// </summary>
    /// <param name="p_objectName">Name for the GameObject.</param>
    /// <param name="p_dimension">Width/height in local canvas units.</param>
    /// <param name="p_preventClickThrough">If true, blocks raycasts to UI behind the window.</param>
    /// <param name="p_useTranslucency">If true, uses TranslucentImage for the background.</param>
    public static GameObject CreateWindow(string p_objectName, Vector2 p_dimension, bool p_preventClickThrough = true, bool p_useTranslucency = true)
    {
        Canvas canvas = GetCanvas();

        if (canvas == null)
        {
            Debug.LogWarning("UIExample.CreateWindow: No Canvas found. Aborting.");
            return null;
        }

        GameObject go = new GameObject(p_objectName);
        go.transform.SetParent(canvas.transform, false);

        // Setup RectTransform and center it.
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = p_dimension;
        CenterInParent(rt);

        // Background image (translucent or normal).
        Image bg;

        if (p_useTranslucency)
        {
            var translucent = go.AddComponent<TranslucentImage>();
            ConfigureTranslucentImage(translucent, DEFAULT_SPRITE_BLENDING);
            bg = translucent;
        }
        else
        {
            bg = go.AddComponent<Image>();
        }

        bg.raycastTarget = p_preventClickThrough;

        // Apply the sliced sprite.
        Sprite sprite = LoadButtonSprite();
        ApplyToImage(bg, sprite, p_pixelPerUnitMultiplier: 2f);

        return go;
    }

    /// <summary>
    /// Create a button with a text label. The button is parented to the active Canvas by default.
    /// </summary>
    public static Button CreateButton(string p_objectName, Vector2 p_dimension, string p_localizationKey, FontStyles p_fontStyle = FontStyles.Normal)
    {
        Button button = CreateButton(p_objectName, p_dimension);

        // Child that stretches to fill the button for the label.
        GameObject textGO = new GameObject($"{p_objectName}:text");
        textGO.transform.SetParent(button.transform, false);

        RectTransform rt = textGO.AddComponent<RectTransform>();
        StretchToFill(rt);

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.enableAutoSizing = true; // Optional, disable if you want a fixed text size
        tmp.fontSize = 20f;
        tmp.fontSizeMin = 10f;
        tmp.fontSizeMax = 20f;
        tmp.color = Color.white;
        tmp.fontStyle = p_fontStyle;
        tmp.margin = new Vector4(10f, 5f, 10f, 5f); // LEFT, TOP, RIGHT, BOTTOM
        tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
        tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
        tmp.text = LocalizationManager.Translate(p_localizationKey);

        textGO.AddComponent<LocalizedText>().key = p_localizationKey;

        // IMPORTANT:
        // targetGraphic controls which Graphic gets tinted by the Button's ColorBlock during state transitions.
        // Setting it to the text makes the label highlight on hover/press.
        // If you want the background to tint instead, set this to the background Image you add elsewhere.
        button.targetGraphic = tmp;

        return button;
    }

    /// <summary>
    /// Create a button with an icon (Image) child. The button is parented to the active Canvas by default.
    /// </summary>
    public static Button CreateButton(string p_objectName, Vector2 p_dimension, Sprite p_buttonIcon)
    {
        Button button = CreateButton(p_objectName, p_dimension);

        GameObject iconGO = new GameObject($"{p_objectName}:image");
        iconGO.transform.SetParent(button.transform, false);

        RectTransform rt = iconGO.AddComponent<RectTransform>();
        StretchToFill(rt);

        Image icon = iconGO.AddComponent<Image>();
        icon.color = Color.white;
        icon.sprite = p_buttonIcon;

        // See note above re: which graphic is tinted on hover/press.
        button.targetGraphic = icon;

        return button;
    }

    /// <summary>
    /// Create a bare button (no background sprite, no text/icon). Parents to active Canvas.
    /// </summary>
    public static Button CreateButton(string p_objectName, Vector2 p_dimension)
    {
        Canvas canvas = GetCanvas();

        if (canvas == null)
        {
            Debug.LogWarning("UIExample.CreateButton: No Canvas found. Aborting.");
            return null;
        }

        GameObject go = new GameObject(p_objectName);
        go.transform.SetParent(canvas.transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = p_dimension;
        CenterInParent(rt);

        Button button = go.AddComponent<Button>();

        // Setup button state colors (matches base game style).
        ColorBlock cb = new ColorBlock
        {
            colorMultiplier = 1f,
            normalColor = Grey,
            selectedColor = Grey,
            highlightedColor = Yellow,
            pressedColor = Grey,
            disabledColor = Grey * 0.5f,
            fadeDuration = 0.1f
        };

        button.colors = cb;

        return button;
    }

    // -------------------------------------------------------------------------
    // Canvas discovery
    // -------------------------------------------------------------------------

    /// <summary>
    /// Find and return the appropriate Canvas based on the currently active scene.
    /// Scene indices:
    /// 0 - Main Menu, 1 - Game, 2 - Scenario Editor
    /// </summary>
    private static Canvas GetCanvas()
    {
        Scene scene = SceneManager.GetActiveScene();

        // Main Menu
        if (scene.buildIndex == 0)
            return MainMenu.instance.UI_parent.GetComponent<Canvas>();

        // Game
        if (scene.buildIndex == 1)
            return UIManager.instance.mainCanvas;

        // Scenario Editor
        if (scene.buildIndex == 2)
            return GameObject.FindObjectOfType<Canvas>();

        Debug.LogWarning("UIExample.GetCanvas: No canvas found for this scene.");

        return null;
    }

    // -------------------------------------------------------------------------
    // Sprite + Image utilities (9-slicing helpers)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Create a 9-sliced Sprite directly from a Texture2D.
    /// borderPx is (left, bottom, right, top) in pixels.
    /// </summary>
    public static Sprite CreateSlicedSprite(Texture2D p_texture2D, Vector4 p_borderPx, float p_pixelsPerUnit = DEFAULT_PPU, Vector2? p_pivot = null)
    {
        if (p_texture2D == null) throw new ArgumentNullException(nameof(p_texture2D));

        Rect rect = new Rect(0, 0, p_texture2D.width, p_texture2D.height);

        // Clamp borders so they never exceed half of width/height to avoid invalid slice regions.
        p_borderPx = ClampBorder(p_borderPx, p_texture2D.width, p_texture2D.height);

        Sprite sprite = Sprite.Create(
            texture: p_texture2D,
            rect: rect,
            pivot: p_pivot ?? new Vector2(0.5f, 0.5f),
            pixelsPerUnit: p_pixelsPerUnit,
            extrude: 0,
            meshType: SpriteMeshType.FullRect,
            border: p_borderPx,
            generateFallbackPhysicsShape: false
        );

        sprite.name = p_texture2D.name;

        return sprite;
    }

    /// <summary>
    /// Clone an existing Sprite but add a 9-slice border without copying pixels.
    /// </summary>
    public static Sprite CloneWithBorder(Sprite p_original, Vector4 p_borderPx)
    {
        if (p_original == null) throw new ArgumentNullException(nameof(p_original));

        Texture2D tex = p_original.texture;
        Rect rect = p_original.rect;

        // Clamp borders to the sprite's sub-rect.
        p_borderPx = ClampBorder(p_borderPx, rect.width, rect.height);

        Sprite clone = Sprite.Create(
            texture: tex,
            rect: rect,
            pivot: p_original.pivot / p_original.rect.size,
            pixelsPerUnit: p_original.pixelsPerUnit,
            extrude: 0,
            meshType: SpriteMeshType.FullRect,
            border: p_borderPx,
            generateFallbackPhysicsShape: false
        );

        clone.name = p_original.name + "_sliced";

        return clone;
    }

    /// <summary>
    /// Assign a sliced sprite to a UI Image and configure its type.
    /// </summary>
    public static void ApplyToImage(Image p_image, Sprite p_slicedSprite, float p_pixelPerUnitMultiplier = 1f, bool p_preserveAspect = false, bool p_fillCenter = true)
    {
        if (p_image == null) throw new ArgumentNullException(nameof(p_image));
        if (p_slicedSprite == null) throw new ArgumentNullException(nameof(p_slicedSprite));

        p_image.type = Image.Type.Sliced;
        p_image.fillCenter = p_fillCenter;
        p_image.preserveAspect = p_preserveAspect;
        p_image.sprite = p_slicedSprite;
        p_image.pixelsPerUnitMultiplier = p_pixelPerUnitMultiplier;

        // Mark for layout + visual rebuild.
        p_image.SetAllDirty();
    }

    /// <summary>
    /// Load a PNG from disk and return as a 9-sliced Sprite.
    /// </summary>
    public static Sprite LoadPNGasSlicedSprite(string p_filePath, float p_uniformBorderPx, float p_pixelsPerUnit = DEFAULT_PPU)
        => LoadPNGasSlicedSprite(p_filePath, new Vector4(p_uniformBorderPx, p_uniformBorderPx, p_uniformBorderPx, p_uniformBorderPx), p_pixelsPerUnit);

    /// <summary>
    /// Load a PNG from disk and return as a 9-sliced Sprite.
    /// </summary>
    public static Sprite LoadPNGasSlicedSprite(string p_filePath, Vector4 p_borderPx, float p_pixelsPerUnit = DEFAULT_PPU)
    {
        if (!File.Exists(p_filePath)) throw new FileNotFoundException(p_filePath);

        byte[] bytes = File.ReadAllBytes(p_filePath);

        // For UI textures, mipmaps are usually unnecessary; set mipChain:false to save memory.
        Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
        texture2D.name = p_filePath;

        // We don't need pixel reads later, so markNonReadable:true to free CPU-side memory.
        texture2D.LoadImage(bytes, markNonReadable: true);

        return CreateSlicedSprite(texture2D, p_borderPx, p_pixelsPerUnit);
    }

    /// <summary>
    /// Load a PNG embedded as a resource in the mod DLL and return as a 9-sliced Sprite.
    /// </summary>
    public static Sprite LoadEmbeddedPNGasSlicedSprite(string p_filePath, float p_uniformBorderPx, float p_pixelsPerUnit = DEFAULT_PPU)
    {
        byte[] bytes = EmbeddedResourceLoader.LoadResourceBytes(p_filePath);

        Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
        texture2D.LoadImage(bytes, markNonReadable: true);
        texture2D.name = p_filePath;

        return CreateSlicedSprite(texture2D, Vector4.one * p_uniformBorderPx, p_pixelsPerUnit);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Load the example sprite either from DLL or disk based on LOAD_RESOURCES_FROM_DLL.
    /// </summary>
    private static Sprite LoadButtonSprite()
    {
        if (LOAD_RESOURCES_FROM_DLL)
            return LoadEmbeddedPNGasSlicedSprite(EMBEDDED_BUTTON_SPRITE, DEFAULT_SLICE_BORDER_PX, DEFAULT_PPU);

        return LoadPNGasSlicedSprite(DISK_BUTTON_SPRITE, DEFAULT_SLICE_BORDER_PX, DEFAULT_PPU);
    }

    /// <summary>
    /// Configure a TranslucentImage component with the shared material and a blending factor.
    /// </summary>
    private static void ConfigureTranslucentImage(TranslucentImage p_image, float p_blending)
    {
        p_image.material = _sharedTranslucentMat;
        p_image.spriteBlending = p_blending;
    }

    /// <summary>
    /// Clamp 9-slice border values so they don't exceed half the width/height.
    /// </summary>
    private static Vector4 ClampBorder(Vector4 p_border, float p_width, float p_height)
    {
        float maxX = Mathf.Max(0, p_width / 2f);
        float maxY = Mathf.Max(0, p_height / 2f);

        // border = (L, B, R, T)
        p_border.x = Mathf.Clamp(p_border.x, 0, maxX);
        p_border.z = Mathf.Clamp(p_border.z, 0, maxX);
        p_border.y = Mathf.Clamp(p_border.y, 0, maxY);
        p_border.w = Mathf.Clamp(p_border.w, 0, maxY);

        return p_border;
    }

    /// <summary>
    /// Center a RectTransform within its parent using middle anchors and zeroed offsets.
    /// </summary>
    private static void CenterInParent(RectTransform p_rectTransform)
    {
        p_rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        p_rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        p_rectTransform.anchoredPosition = Vector2.zero; // exact center
    }

    /// <summary>
    /// Make a RectTransform stretch to fill its parent (useful for text/icon children).
    /// </summary>
    private static void StretchToFill(RectTransform p_rectTransform)
    {
        p_rectTransform.anchorMin = Vector2.zero;
        p_rectTransform.anchorMax = Vector2.one;
        p_rectTransform.anchoredPosition = Vector2.zero;
        p_rectTransform.offsetMin = Vector2.zero;
        p_rectTransform.offsetMax = Vector2.zero;
    }
}