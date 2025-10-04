using TMPro;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using LeTai.Asset.TranslucentImage;

class UIExample
{
    // Colors used in the base game, regular grey and yellow for highlight
    static readonly Color Grey = new Color(0.588f, 0.6f, 0.611f);
    static readonly Color Yellow = new Color(1f, 0.8f, 0f);

    static bool _initialized = false;

    // Reusing the same material for every UI elements in order to save performances
    static Material _sharedTransluscentMat;

    public static void StartExample()
    {
        Initialize();

        CreateExampleMenu();
    }

    // Must be called before using anything else, otherwise material will be null
    static void Initialize()
    {
        if (_initialized) return; // Failsafe

        _sharedTransluscentMat = new Material(Shader.Find("UI/TranslucentImage"));
        _sharedTransluscentMat.SetFloat("_Vibrancy", 1.8f);

        _initialized = true;
    }

    static void CreateExampleMenu()
    {
        GameObject window = CreateWindow("Example Window", new Vector2(500f, 250f));

        Button button = CreateButton("Example Button", new Vector2(100f, 35f), "OK");
        button.transform.SetParent(window.transform, false);
        button.onClick.AddListener(delegate { MyButtonAction(); });

        Image image = button.gameObject.AddComponent<TranslucentImage>();

        TranslucentImage translucentImage = (TranslucentImage)image;
        translucentImage.material = _sharedTransluscentMat;
        translucentImage.spriteBlending = 0.65f;
        image.raycastTarget = true;

        string filePath = Path.Combine(ExampleMod.modInstallLocation, "Graphics", "UI", "Buttons", "Button square.png");

        Sprite sprite = LoadPngAsSlicedSprite(filePath, 30f, 100f);

        ApplyToImage(image, sprite, 3f);
    }

    static void MyButtonAction()
    {
        UIManager.ShowMessage("IT HURTS! DON'T DO THAT AGAIN.");
    }

    public static GameObject CreateWindow(string p_objectName, Vector2 p_dimension, bool p_preventClickThrough = true, bool p_useTranslucency = true)
    {
        GameObject gameObject = new GameObject(p_objectName);

        Canvas canvas = GetCanvas();

        gameObject.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = p_dimension;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        Image image;

        if (p_useTranslucency)
        {
            image = gameObject.AddComponent<TranslucentImage>();

            TranslucentImage translucentImage = (TranslucentImage)image;
            translucentImage.material = _sharedTransluscentMat;
            translucentImage.spriteBlending = 0.65f;
        }
        else
        {
            image = gameObject.AddComponent<Image>();
        }

        image.raycastTarget = p_preventClickThrough;

        string filePath = Path.Combine(ExampleMod.modInstallLocation, "Graphics", "UI", "Buttons", "Button square.png");

        Sprite sprite = LoadPngAsSlicedSprite(filePath, 30f, 100f);

        ApplyToImage(image, sprite, 2f);

        return gameObject;
    }

    public static Button CreateButton(string p_objectName, Vector2 p_dimension, string p_buttonText)
    {
        Button button = CreateButton(p_objectName, p_dimension);

        GameObject textObject = new GameObject($"{p_objectName}:text");
        textObject.transform.SetParent(button.transform, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        TextMeshProUGUI textMeshProUGUI = textObject.AddComponent<TextMeshProUGUI>();
        textMeshProUGUI.fontSize = 20f;
        textMeshProUGUI.color = Color.white;
        textMeshProUGUI.verticalAlignment = VerticalAlignmentOptions.Middle;
        textMeshProUGUI.horizontalAlignment = HorizontalAlignmentOptions.Center;
        textMeshProUGUI.margin = new Vector4(5f, 5f, 5f, 5f);
        textMeshProUGUI.text = p_buttonText;

        button.targetGraphic = textMeshProUGUI;

        return button;
    }

    public static Button CreateButton(string p_objectName, Vector2 p_dimension, Sprite p_buttonIcon)
    {
        Button button = CreateButton(p_objectName, p_dimension);

        GameObject textObject = new GameObject($"{p_objectName}:image");
        textObject.transform.SetParent(button.transform, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = textObject.AddComponent<Image>();
        image.color = Color.white;
        image.sprite = p_buttonIcon;

        button.targetGraphic = image;

        return button;
    }

    public static Button CreateButton(string p_objectName, Vector2 p_dimension)
    {
        GameObject buttonObject = new GameObject(p_objectName);

        Canvas canvas = GetCanvas();

        buttonObject.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = p_dimension;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0.5f, 0.5f);

        Button button = buttonObject.AddComponent<Button>();

        ColorBlock colorBlock = new ColorBlock();
        colorBlock.colorMultiplier = 1f;
        colorBlock.normalColor = Grey;
        colorBlock.selectedColor = Grey;
        colorBlock.highlightedColor = Yellow;
        colorBlock.pressedColor = Grey;
        colorBlock.disabledColor = Grey * 0.5f;

        button.colors = colorBlock;

        return button;
    }

    /// <summary>
    /// Find and return the appropriate Canvas based on what scene is currently active
    /// </summary>
    /// <returns></returns>
    static Canvas GetCanvas()
    {
        Scene scene = SceneManager.GetActiveScene();

        // Main Menu scene
        if (scene.buildIndex == 0)
        {
            return MainMenu.instance.UI_parent.GetComponent<Canvas>();
        }
        // Game scene
        else if (scene.buildIndex == 1)
        {
            return UIManager.instance.mainCanvas;
        }
        // Scenario Editor scene
        else if (scene.buildIndex == 2)
        {
            return GameObject.FindObjectOfType<Canvas>();
        }

        Debug.LogWarning("No canvas was found.");

        return null;
    }

    /// <summary>
    /// Create a 9-sliced Sprite directly from a Texture2D.
    /// borderPx = (left, bottom, right, top) in *pixels*.
    /// </summary>
    public static Sprite CreateSlicedSprite(Texture2D tex, Vector4 borderPx, float pixelsPerUnit = 100f, Vector2? pivot = null)
    {
        if (tex == null) throw new ArgumentNullException(nameof(tex));

        Rect rect = new Rect(0, 0, tex.width, tex.height);

        // Clamp borders so they never exceed half of width/height.
        borderPx = ClampBorder(borderPx, tex.width, tex.height);

        return Sprite.Create(
            texture: tex,
            rect: rect,
            pivot: pivot ?? new Vector2(0.5f, 0.5f),
            pixelsPerUnit: pixelsPerUnit,
            extrude: 0,
            meshType: SpriteMeshType.FullRect,
            border: borderPx,
            generateFallbackPhysicsShape: false
        );
    }

    /// <summary>
    /// Clone an existing Sprite but add a 9-slice border.
    /// </summary>
    public static Sprite CloneWithBorder(Sprite original, Vector4 borderPx)
    {
        if (original == null) throw new ArgumentNullException(nameof(original));

        Texture2D tex = original.texture;
        Rect rect = original.rect;

        // Clamp borders to sub-rect, not full texture.
        borderPx = ClampBorder(borderPx, rect.width, rect.height);

        Sprite clone = Sprite.Create(
            texture: tex,
            rect: rect,
            pivot: original.pivot / original.rect.size,
            pixelsPerUnit: original.pixelsPerUnit,
            extrude: 0,
            meshType: SpriteMeshType.FullRect,
            border: borderPx,
            generateFallbackPhysicsShape: false
        );

        clone.name = original.name + "_sliced";

        return clone;
    }

    /// <summary>
    /// Assign a sliced sprite to a UI Image and configure its type.
    /// </summary>
    public static void ApplyToImage(Image image, Sprite slicedSprite, float pixelPerUnitMultiplier = 1f, bool preserveAspect = false, bool fillCenter = true)
    {
        if (image == null) throw new ArgumentNullException(nameof(image));
        if (slicedSprite == null) throw new ArgumentNullException(nameof(slicedSprite));

        image.type = Image.Type.Sliced;
        image.fillCenter = fillCenter;
        image.preserveAspect = preserveAspect;
        image.sprite = slicedSprite;
        image.pixelsPerUnitMultiplier = pixelPerUnitMultiplier;

        image.SetAllDirty();
    }

    /// <summary>
    /// Convenience: load a PNG from disk and return a sliced Sprite.
    /// Accepts either a uniform border (all sides the same) or per-side.
    /// </summary>
    public static Sprite LoadPngAsSlicedSprite(string filePath, float uniformBorderPx, float pixelsPerUnit = 100f)
        => LoadPngAsSlicedSprite(filePath, new Vector4(uniformBorderPx, uniformBorderPx, uniformBorderPx, uniformBorderPx), pixelsPerUnit);

    public static Sprite LoadPngAsSlicedSprite(string filePath, Vector4 borderPx, float pixelsPerUnit = 100f)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException(filePath);

        byte[] bytes = File.ReadAllBytes(filePath);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: true);

        tex.LoadImage(bytes, markNonReadable: false); // keep readable = false if you don't need to access pixels

        return CreateSlicedSprite(tex, borderPx, pixelsPerUnit);
    }

    // --- helpers ---
    private static Vector4 ClampBorder(Vector4 border, float width, float height)
    {
        float maxX = Mathf.Max(0, width / 2f);
        float maxY = Mathf.Max(0, height / 2f);

        // border = (L, B, R, T)
        border.x = Mathf.Clamp(border.x, 0, maxX);
        border.z = Mathf.Clamp(border.z, 0, maxX);
        border.y = Mathf.Clamp(border.y, 0, maxY);
        border.w = Mathf.Clamp(border.w, 0, maxY);

        return border;
    }
}