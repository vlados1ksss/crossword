using System;
using UnityEngine;
using UnityEngine.UI;

namespace CrosswordGame
{
    /// <summary>Одна клетка строки ввода над клавиатурой.</summary>
    public class AnswerSlot : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Text letterText;
        [SerializeField] private RectTransform content;
        [SerializeField] private LayoutElement layoutElement;

        public int Index { get; set; }
        public Image Background => background;
        public RectTransform Content => content;

        public event Action<AnswerSlot> Clicked;

        private void Awake()
        {
            button.onClick.AddListener(() => Clicked?.Invoke(this));
        }

        public void SetSize(float size)
        {
            layoutElement.preferredWidth = size;
            layoutElement.preferredHeight = size;
            letterText.fontSize = Mathf.RoundToInt(size * 0.58f);
        }

        public void SetLetter(char letter, bool locked, bool isCursor)
        {
            letterText.text = letter == '\0' ? string.Empty : letter.ToString();
            letterText.color = locked ? UIPalette.TextSecondary : UIPalette.TextPrimary;
            background.color = GetColor(locked, isCursor);
        }

        public static Color GetColor(bool locked, bool isCursor)
        {
            if (isCursor) return UIPalette.SlotCursor;
            return locked ? UIPalette.SlotLocked : UIPalette.SlotEmpty;
        }

        public void ResetVisual()
        {
            content.localScale = Vector3.one;
        }
    }
}
