using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CrosswordGame.EditorTools
{
    /// <summary>Вспомогательные методы для генерации UI в редакторе (используются CrosswordSceneBuilder).</summary>
    public static class CrosswordUIFactory
    {
        public const string ArtFolder = "Assets/Art/UI";
        public const string FontRegularPath = "Assets/Fonts/PTSans-Regular.ttf";
        public const string FontBoldPath = "Assets/Fonts/PTSans-Bold.ttf";

        public static Font FontRegular => AssetDatabase.LoadAssetAtPath<Font>(FontRegularPath);
        public static Font FontBold => AssetDatabase.LoadAssetAtPath<Font>(FontBoldPath);

        public static Sprite RoundedLarge => LoadSprite("rounded_large.png");
        public static Sprite RoundedSmall => LoadSprite("rounded_small.png");
        public static Sprite Circle => LoadSprite("circle.png");
        public static Sprite BackspaceIcon => LoadSprite("icon_backspace.png");

        private static Sprite LoadSprite(string file) => AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{file}");

        #region Sprites

        /// <summary>Генерирует простые сглаженные спрайты для 9-slice (без внешних ассетов).</summary>
        public static void EnsureSprites()
        {
            Directory.CreateDirectory(ArtFolder);
            CreateRoundedSprite("rounded_large.png", 96, 28);
            CreateRoundedSprite("rounded_small.png", 48, 10);
            CreateRoundedSprite("circle.png", 64, 32);
            CreateBackspaceIcon("icon_backspace.png", 128);
        }

        /// <summary>Белая иконка «стереть»: стрелка влево с вырезанным крестиком. Цвет задаётся через Image.color.</summary>
        private static void CreateBackspaceIcon(string file, int size)
        {
            string path = $"{ArtFolder}/{file}";
            if (!File.Exists(path))
            {
                const int samples = 4; // сглаживание краёв
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int covered = 0;
                    for (int sy = 0; sy < samples; sy++)
                    for (int sx = 0; sx < samples; sx++)
                    {
                        float u = (x + (sx + 0.5f) / samples) / size;
                        float v = (y + (sy + 0.5f) / samples) / size;
                        if (IsBackspaceShape(u, v)) covered++;
                    }
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * covered / (samples * samples)));
                }
                texture.SetPixels32(pixels);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static bool IsBackspaceShape(float u, float v)
        {
            const float left = 0.06f, neck = 0.34f, right = 0.94f, halfHeight = 0.28f;
            float dy = Mathf.Abs(v - 0.5f);
            if (u < left || u > right || dy > halfHeight) return false;
            if (u < neck && dy > (u - left) / (neck - left) * halfHeight) return false; // остриё стрелки

            // Прозрачный крестик внутри прямоугольной части.
            float px = u - 0.63f, py = v - 0.5f;
            const float arm = 0.12f, thickness = 0.06f;
            bool inCross = Mathf.Abs(px) <= arm && Mathf.Abs(py) <= arm &&
                           (Mathf.Abs(px - py) <= thickness || Mathf.Abs(px + py) <= thickness);
            return !inCross;
        }

        private static void CreateRoundedSprite(string file, int size, int radius)
        {
            string path = $"{ArtFolder}/{file}";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float cx = Mathf.Clamp(px, radius, size - radius);
                    float cy = Mathf.Clamp(py, radius, size - radius);
                    float distance = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
                texture.SetPixels32(pixels);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spriteBorder = new Vector4(radius, radius, radius, radius);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        #endregion

        #region Objects

        public static RectTransform Create(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            if (parent != null) rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform Stretch(RectTransform rect, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        public static RectTransform Anchor(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static Image AddImage(RectTransform rect, Sprite sprite, Color color, bool raycast = true)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        public static Text AddText(RectTransform rect, string text, int size, Color color, TextAnchor alignment, bool bold = false)
        {
            var label = rect.gameObject.AddComponent<Text>();
            label.font = bold ? FontBold : FontRegular;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.supportRichText = true;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        public static Text CreateText(string name, Transform parent, string text, int size, Color color, TextAnchor alignment, bool bold = false)
        {
            return AddText(Create(name, parent), text, size, color, alignment, bold);
        }

        public static Text CreateLocalizedText(string name, Transform parent, string key, int size, Color color, TextAnchor alignment, bool bold = false)
        {
            var text = CreateText(name, parent, LocalizationManager.Get(key), size, color, alignment, bold);
            var localized = text.gameObject.AddComponent<LocalizedText>();
            SetField(localized, "key", key);
            return text;
        }

        public static Button CreateButton(string name, Transform parent, string labelKey, Color background, Color labelColor, int fontSize, out Text label)
        {
            var rect = Create(name, parent);
            var image = AddImage(rect, RoundedLarge, background);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            SetupColors(button);
            rect.gameObject.AddComponent<ButtonPressAnimation>();

            label = labelKey != null
                ? CreateLocalizedText("Label", rect, labelKey, fontSize, labelColor, TextAnchor.MiddleCenter, true)
                : CreateText("Label", rect, string.Empty, fontSize, labelColor, TextAnchor.MiddleCenter, true);
            Stretch(label.rectTransform, 12, 4, 12, 4);
            return button;
        }

        public static void SetupColors(Selectable selectable)
        {
            var colors = selectable.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.6f);
            colors.fadeDuration = 0.08f;
            selectable.colors = colors;
            var navigation = selectable.navigation;
            navigation.mode = Navigation.Mode.None;
            selectable.navigation = navigation;
        }

        public static T AddLayoutElement<T>(RectTransform rect) where T : Component => rect.gameObject.AddComponent<T>();

        #endregion

        #region Serialized fields

        public static void SetField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"Field '{field}' not found on {target.GetType().Name}");
                return;
            }
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetField(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"Field '{field}' not found on {target.GetType().Name}");
                return;
            }
            property.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion
    }
}
