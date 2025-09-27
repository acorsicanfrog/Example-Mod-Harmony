// ==================================================================================
// Harmony Patch: TurnManager (Popup UI Example)
//
// This patch hooks into `TurnManager.NextTurn` and displays a custom popup window 
// on each turn. The popup demonstrates how to dynamically build UI elements in Unity 
// at runtime with Harmony integration.
//
// Key Features:
// - Postfix on TurnManager.NextTurn:
//      • Finds a valid UI canvas (via UIManager or scene canvases).
//      • Creates a centered popup window when a new turn begins.
// - Popup Window Construction:
//      • Window root with background image and dark theme.
//      • Title ("Ready to run the action?") and description text.
//      • Two buttons created dynamically with styling and layout:
//          ◦ "Run Action" (executes `ValidateFunction` and closes popup).
//          ◦ "Cancel" (executes `CancelFunction` and closes popup).
//      • Ensures only one popup exists at a time by destroying previous instances.
// - Helper Method (MakeButton):
//      • Simplifies dynamic button creation with colors, sizes, labels, and click events.
// - Example Logic Placeholders:
//      • `ValidateFunction()` and `CancelFunction()` provide hooks to insert actual mod logic.
//
// Usage:
// Use this as a template for adding interactive mod UI.  
// Replace the placeholder functions with real actions triggered by the popup.
// ==================================================================================

using TMPro;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

class TurnManagerPatch
{
    [HarmonyPatch(typeof(TurnManager))]
    static class Patch_TurnManager
    {
        // A unique name so we can find/destroy the window if it already exists
        const string PopupName = "ModPopupWindow";

        [HarmonyPostfix]
        [HarmonyPatch(nameof(TurnManager.NextTurn))]
        static void Patch_NextTurn(bool sendRPC)
        {
            Canvas canvas = FindCanvas();

            if (canvas != null)
                CreatePopup(canvas);
        }

        static Canvas FindCanvas()
        {
            if (UIManager.instance == null)
            {
                // Find a suitable canvas
                Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
                if (canvases == null || canvases.Length == 0) return null;
                return canvases[0];
            }
            else
            {
                return UIManager.instance.mainCanvas;
            }
        }

        static void CreatePopup(Canvas p_canvas)
        {
            // If a previous window exists, delete it (avoid stacking multiple)
            Transform existing = p_canvas.transform.Find(PopupName);
            if (existing != null)
                Object.Destroy(existing.gameObject);

            // Root / window
            GameObject popupWindow = new GameObject(PopupName, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            popupWindow.transform.SetParent(p_canvas.transform, false);

            RectTransform windowRT = popupWindow.GetComponent<RectTransform>();
            windowRT.anchorMin = new Vector2(0.5f, 0.5f);
            windowRT.anchorMax = new Vector2(0.5f, 0.5f);
            windowRT.pivot = new Vector2(0.5f, 0.5f);
            windowRT.sizeDelta = new Vector2(360f, 180f);
            windowRT.anchoredPosition = Vector2.zero;

            Image windowBG = popupWindow.GetComponent<Image>();
            windowBG.color = new Color(0.15f, 0.15f, 0.15f, 1f); // dark grey

            // Title text
            GameObject titleGO = new GameObject("Title", typeof(RectTransform));
            titleGO.transform.SetParent(popupWindow.transform, false);
            RectTransform titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 1f);
            titleRT.anchorMax = new Vector2(0.5f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0f, -16f);
            titleRT.sizeDelta = new Vector2(320f, 40f);

            TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "Ready to run the action?";
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.fontSize = 24f;
            titleTMP.enableWordWrapping = true;
            titleTMP.color = Color.white;

            // Description text (optional)
            GameObject descGO = new GameObject("Description", typeof(RectTransform));
            descGO.transform.SetParent(popupWindow.transform, false);
            RectTransform descRT = descGO.GetComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0.5f, 0.5f);
            descRT.anchorMax = new Vector2(0.5f, 0.5f);
            descRT.pivot = new Vector2(0.5f, 0.5f);
            descRT.anchoredPosition = new Vector2(0f, 12f);
            descRT.sizeDelta = new Vector2(320f, 40f);

            TextMeshProUGUI descTMP = descGO.AddComponent<TextMeshProUGUI>();
            descTMP.text = "Click Run to execute your mod logic, or Cancel to dismiss.";
            descTMP.alignment = TextAlignmentOptions.Center;
            descTMP.fontSize = 18f;
            descTMP.enableWordWrapping = true;
            descTMP.color = new Color(1f, 1f, 1f, 0.9f);

            // Button container with horizontal layout (centered)
            GameObject buttonsRoot = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonsRoot.transform.SetParent(popupWindow.transform, false);
            RectTransform buttonsRT = buttonsRoot.GetComponent<RectTransform>();
            buttonsRT.anchorMin = new Vector2(0.5f, 0f);
            buttonsRT.anchorMax = new Vector2(0.5f, 0f);
            buttonsRT.pivot = new Vector2(0.5f, 0f);
            buttonsRT.anchoredPosition = new Vector2(0f, 16f);
            buttonsRT.sizeDelta = new Vector2(320f, 48f);

            HorizontalLayoutGroup hlg = buttonsRoot.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.padding = new RectOffset(0, 0, 0, 0);

            // Helper local to create a button quickly
            Button MakeButton(string name, string label, Color bg, System.Action onClick)
            {
                GameObject btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                btnGO.transform.SetParent(buttonsRoot.transform, false);

                RectTransform r = btnGO.GetComponent<RectTransform>();
                r.sizeDelta = new Vector2(150f, 40f);

                Image img = btnGO.GetComponent<Image>();
                img.color = bg;

                LayoutElement le = btnGO.GetComponent<LayoutElement>();
                le.preferredWidth = 150f;
                le.preferredHeight = 40f;
                le.flexibleWidth = 0f;
                le.flexibleHeight = 0f;

                Button btn = btnGO.GetComponent<Button>();
                btn.transition = Selectable.Transition.ColorTint;
                ColorBlock cb = btn.colors;
                cb.highlightedColor = new Color(bg.r + 0.15f, bg.g + 0.15f, bg.b + 0.15f, 1f);
                cb.pressedColor = new Color(bg.r * 0.8f, bg.g * 0.8f, bg.b * 0.8f, 1f);
                cb.selectedColor = bg;
                cb.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                btn.colors = cb;

                // Label
                GameObject labelGO = new GameObject("Label", typeof(RectTransform));
                labelGO.transform.SetParent(btnGO.transform, false);
                RectTransform lrt = labelGO.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;

                TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
                tmp.text = label;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 20f;
                tmp.color = Color.white;
                tmp.raycastTarget = false;

                btn.onClick.AddListener(() => onClick?.Invoke());
                return btn;
            }

            // Validate (Run) button
            Button validateButton = MakeButton(
                "ValidateButton",
                "Run Action",
                new Color(0.2f, 0.6f, 1f, 1f),
                () =>
                {
                    try { ValidateFunction(); }
                    finally { Object.Destroy(popupWindow); }
                }
            );

            // Cancel button
            Button cancelButton = MakeButton(
                "CancelButton",
                "Cancel",
                new Color(0.8f, 0.2f, 0.2f, 1f),
                () =>
                {
                    try { CancelFunction(); }
                    finally { Object.Destroy(popupWindow); }
                }
            );
        }

        // Your mod logic goes here
        static void ValidateFunction()
        {
            // TODO: Replace with your actual action.
            Debug.Log("[Mod] ValidateFunction executed.");
        }

        static void CancelFunction()
        {
            Debug.Log("[Mod] CancelFunction executed.");
        }
    }
}