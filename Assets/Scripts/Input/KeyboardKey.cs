using System;
using UnityEngine;
using UnityEngine.UI;

namespace CrosswordGame
{
    public enum KeyType
    {
        Letter,
        Backspace,
        Clear,
        Submit
    }

    /// <summary>Клавиша экранной клавиатуры. Нажатие через Button — работает и с мышью, и с touch.</summary>
    public class KeyboardKey : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Text label;
        [SerializeField] private Image icon;   // используется вместо текста (например, Backspace)
        [SerializeField] private LayoutElement layoutElement;

        public KeyType Type { get; private set; }
        public char Letter { get; private set; }
        public float WidthMultiplier { get; private set; } = 1f;
        public Button Button => button;

        public event Action<KeyboardKey> Pressed;

        private void Awake()
        {
            button.onClick.AddListener(() => Pressed?.Invoke(this));
        }

        public void Setup(KeyType type, char letter, string text, float widthMultiplier, Sprite iconSprite = null)
        {
            // Иконка вместо символа: не все Unicode-символы есть в шрифте, а в WebGL нет системных шрифтов для подстановки.
            bool hasIcon = iconSprite != null && icon != null;
            if (icon != null)
            {
                icon.gameObject.SetActive(hasIcon);
                icon.sprite = iconSprite;
            }
            label.gameObject.SetActive(!hasIcon);

            Type = type;
            Letter = letter;
            WidthMultiplier = widthMultiplier;
            label.text = text;
            name = type == KeyType.Letter ? $"Key_{letter}" : $"Key_{type}";

            switch (type)
            {
                case KeyType.Letter:
                    background.color = UIPalette.KeyNormal;
                    label.color = UIPalette.TextPrimary;
                    break;
                case KeyType.Submit:
                    background.color = UIPalette.KeyDone;
                    label.color = Color.white;
                    break;
                default:
                    background.color = UIPalette.KeySpecial;
                    label.color = UIPalette.TextPrimary;
                    break;
            }
            if (icon != null) icon.color = label.color;
        }

        public void SetSize(float unitWidth, float height)
        {
            layoutElement.preferredWidth = unitWidth * WidthMultiplier;
            layoutElement.preferredHeight = height;
            int fontSize = Mathf.RoundToInt(Mathf.Min(unitWidth * 0.55f, height * 0.5f));
            label.fontSize = Type == KeyType.Letter ? fontSize : Mathf.RoundToInt(fontSize * 0.72f);
        }
    }
}
