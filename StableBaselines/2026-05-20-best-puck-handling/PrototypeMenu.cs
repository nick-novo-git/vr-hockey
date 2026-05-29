using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace HockeyStickhandling
{
    public enum PlayerHandedness
    {
        Left,
        Right
    }

    public static class GameSettings
    {
        public static PlayerHandedness Handedness { get; set; } = PlayerHandedness.Left;
    }

    public sealed class PrototypeMenu : MonoBehaviour
    {
        [Header("Placement")]
        [SerializeField] private float distanceFromPlayer = 1.75f;
        [SerializeField] private float verticalOffsetFromEyes = -0.08f;
        [SerializeField] private Vector2 menuSize = new Vector2(860.0f, 560.0f);
        [SerializeField] private float worldScale = 0.0014f;

        [Header("Style")]
        [SerializeField] private Color panelColor = new Color(0.08f, 0.1f, 0.13f, 0.78f);
        [SerializeField] private Color buttonColor = new Color(0.16f, 0.2f, 0.26f, 0.92f);
        [SerializeField] private Color buttonHoverColor = new Color(0.24f, 0.32f, 0.42f, 0.96f);
        [SerializeField] private Color buttonSelectedColor = new Color(0.03f, 0.55f, 0.42f, 0.96f);
        [SerializeField] private Color textColor = new Color(0.94f, 0.97f, 1.0f, 1.0f);
        [SerializeField] private Color mutedTextColor = new Color(0.72f, 0.78f, 0.86f, 1.0f);

        private Canvas canvas;
        private RectTransform root;
        private RectTransform mainPage;
        private RectTransform handednessPage;
        private ButtonView playButton;
        private ButtonView leftButton;
        private ButtonView rightButton;
        private Text statusText;
        private Camera mainCamera;
        private InputDevice rightController;
        private bool pressWasHeld;

        public void Initialize()
        {
            mainCamera = Camera.main;
            CreateMenu();
            PlaceInFrontOfPlayer();
            ShowMainPage();
        }

        private void Update()
        {
            var ray = GetInteractionRay();
            var hovered = Physics.Raycast(ray, out var hit, 10.0f) ? hit.collider.GetComponent<MenuButtonTarget>() : null;
            UpdateButtonState(playButton, hovered);
            UpdateButtonState(leftButton, hovered);
            UpdateButtonState(rightButton, hovered);

            var pressed = IsPressed();
            if (pressed && !pressWasHeld && hovered != null)
            {
                hovered.Invoke();
            }

            pressWasHeld = pressed;
        }

        private void CreateMenu()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;
            gameObject.AddComponent<GraphicRaycaster>();

            root = canvas.GetComponent<RectTransform>();
            root.sizeDelta = menuSize;
            root.localScale = Vector3.one * worldScale;

            var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            var panelSprite = CreateRoundedSprite(64, 28, Color.white);
            var buttonSprite = CreateRoundedSprite(64, 24, Color.white);

            var panel = CreateImage("Panel", root, menuSize, Vector2.zero, panelColor, panelSprite);
            panel.raycastTarget = false;

            mainPage = CreatePage("Main Page");
            handednessPage = CreatePage("Handedness Page");

            CreateText("Main Title", mainPage, "Hockey VR", 64, FontStyle.Bold, textColor, new Vector2(0.0f, 155.0f), new Vector2(760.0f, 90.0f), font);
            playButton = CreateButton("Play", mainPage, "Play", new Vector2(0.0f, -40.0f), new Vector2(560.0f, 112.0f), buttonSprite, font, ShowHandednessPage);

            CreateText("Handedness Title", handednessPage, "Choose Your Handedness", 46, FontStyle.Bold, textColor, new Vector2(0.0f, 176.0f), new Vector2(760.0f, 76.0f), font);
            leftButton = CreateButton("Left", handednessPage, "Left", new Vector2(0.0f, 42.0f), new Vector2(620.0f, 104.0f), buttonSprite, font, () => SaveHandedness(PlayerHandedness.Left));
            rightButton = CreateButton("Right", handednessPage, "Right", new Vector2(0.0f, -86.0f), new Vector2(620.0f, 104.0f), buttonSprite, font, () => SaveHandedness(PlayerHandedness.Right));
            CreateStickIcon("Left Stick Icon", leftButton.Root, true);
            CreateStickIcon("Right Stick Icon", rightButton.Root, false);
            statusText = CreateText("Status", handednessPage, "Selection is saved for future gameplay setup.", 26, FontStyle.Normal, mutedTextColor, new Vector2(0.0f, -200.0f), new Vector2(740.0f, 50.0f), font);
        }

        private void PlaceInFrontOfPlayer()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                transform.position = new Vector3(0.0f, 1.45f, 1.75f);
                transform.rotation = Quaternion.identity;
                return;
            }

            var forward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f)
            {
                forward = mainCamera.transform.forward;
            }

            transform.position = mainCamera.transform.position + forward * distanceFromPlayer + Vector3.up * verticalOffsetFromEyes;
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        private RectTransform CreatePage(string objectName)
        {
            var page = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
            page.SetParent(root, false);
            page.anchorMin = Vector2.zero;
            page.anchorMax = Vector2.one;
            page.offsetMin = Vector2.zero;
            page.offsetMax = Vector2.zero;
            return page;
        }

        private static Image CreateImage(string objectName, Transform parent, Vector2 size, Vector2 position, Color color, Sprite sprite)
        {
            var image = new GameObject(objectName, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.rectTransform.sizeDelta = size;
            image.rectTransform.anchoredPosition = position;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        private ButtonView CreateButton(
            string objectName,
            Transform parent,
            string label,
            Vector2 position,
            Vector2 size,
            Sprite sprite,
            Font font,
            Action onClick)
        {
            var image = CreateImage($"{objectName} Button", parent, size, position, buttonColor, sprite);
            var collider = image.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(size.x, size.y, 8.0f);
            collider.center = Vector3.zero;

            var target = image.gameObject.AddComponent<MenuButtonTarget>();
            target.Initialize(onClick);

            var text = CreateText($"{objectName} Label", image.transform, label, 42, FontStyle.Bold, textColor, Vector2.zero, size, font);
            return new ButtonView(image.rectTransform, image, text);
        }

        private Text CreateText(
            string objectName,
            Transform parent,
            string value,
            int fontSize,
            FontStyle fontStyle,
            Color color,
            Vector2 position,
            Vector2 size,
            Font font)
        {
            var text = new GameObject(objectName, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.rectTransform.sizeDelta = size;
            text.rectTransform.anchoredPosition = position;
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private void CreateStickIcon(string objectName, Transform parent, bool leftHanded)
        {
            var iconRoot = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
            iconRoot.SetParent(parent, false);
            iconRoot.sizeDelta = new Vector2(130.0f, 74.0f);
            iconRoot.anchoredPosition = new Vector2(leftHanded ? -210.0f : 210.0f, 0.0f);
            iconRoot.localRotation = Quaternion.Euler(0.0f, 0.0f, leftHanded ? 18.0f : -18.0f);

            var shaft = CreateImage("Shaft", iconRoot, new Vector2(92.0f, 9.0f), Vector2.zero, textColor, CreateRoundedSprite(32, 8, Color.white));
            shaft.raycastTarget = false;
            var blade = CreateImage("Blade", iconRoot, new Vector2(38.0f, 12.0f), new Vector2(leftHanded ? -54.0f : 54.0f, -15.0f), textColor, CreateRoundedSprite(32, 8, Color.white));
            blade.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, leftHanded ? -32.0f : 32.0f);
            blade.raycastTarget = false;
        }

        private void ShowMainPage()
        {
            mainPage.gameObject.SetActive(true);
            handednessPage.gameObject.SetActive(false);
        }

        private void ShowHandednessPage()
        {
            mainPage.gameObject.SetActive(false);
            handednessPage.gameObject.SetActive(true);
        }

        private void SaveHandedness(PlayerHandedness handedness)
        {
            GameSettings.Handedness = handedness;
            statusText.text = $"Saved: {handedness}";
            UpdateButtonState(leftButton, null);
            UpdateButtonState(rightButton, null);
        }

        private void UpdateButtonState(ButtonView button, MenuButtonTarget hovered)
        {
            if (button == null)
            {
                return;
            }

            var target = button.Root.GetComponent<MenuButtonTarget>();
            var selected =
                (button == leftButton && GameSettings.Handedness == PlayerHandedness.Left) ||
                (button == rightButton && GameSettings.Handedness == PlayerHandedness.Right);
            button.Image.color = selected ? buttonSelectedColor : hovered == target ? buttonHoverColor : buttonColor;
        }

        private Ray GetInteractionRay()
        {
            if (!rightController.isValid)
            {
                rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            }

            if (rightController.TryGetFeatureValue(CommonUsages.devicePosition, out var controllerPosition) &&
                rightController.TryGetFeatureValue(CommonUsages.deviceRotation, out var controllerRotation))
            {
                return new Ray(controllerPosition, controllerRotation * Vector3.forward);
            }

            return mainCamera != null
                ? new Ray(mainCamera.transform.position, mainCamera.transform.forward)
                : new Ray(Vector3.zero, Vector3.forward);
        }

        private bool IsPressed()
        {
            if (!rightController.isValid)
            {
                rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            }

            return (rightController.TryGetFeatureValue(CommonUsages.triggerButton, out var triggerPressed) && triggerPressed) ||
                   (rightController.TryGetFeatureValue(CommonUsages.primaryButton, out var primaryPressed) && primaryPressed);
        }

        private static Sprite CreateRoundedSprite(int size, int radius, Color color)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Rounded UI Sprite";
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Max(radius - x, 0, x - (size - radius - 1));
                    var dy = Mathf.Max(radius - y, 0, y - (size - radius - 1));
                    var inside = dx * dx + dy * dy <= radius * radius;
                    texture.SetPixel(x, y, inside ? color : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0.0f, 0.0f, size, size), new Vector2(0.5f, 0.5f), 100.0f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        private sealed class ButtonView
        {
            public ButtonView(RectTransform root, Image image, Text label)
            {
                Root = root;
                Image = image;
                Label = label;
            }

            public RectTransform Root { get; }
            public Image Image { get; }
            public Text Label { get; }
        }

        private sealed class MenuButtonTarget : MonoBehaviour
        {
            private Action onClick;

            public void Initialize(Action clickCallback)
            {
                onClick = clickCallback;
            }

            public void Invoke()
            {
                onClick?.Invoke();
            }
        }
    }
}
