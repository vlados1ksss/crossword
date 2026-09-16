using UnityEngine;

namespace CrosswordGame
{
    /// <summary>Цвета интерфейса в одном месте.</summary>
    public static class UIPalette
    {
        public static readonly Color Background = Hex("EEF2F7");
        public static readonly Color Panel = Hex("FFFFFF");
        public static readonly Color TextPrimary = Hex("1F2937");
        public static readonly Color TextSecondary = Hex("6B7280");
        public static readonly Color Accent = Hex("3B82F6");
        public static readonly Color AccentDark = Hex("2563EB");
        public static readonly Color Success = Hex("22A45D");
        public static readonly Color Error = Hex("E5484D");
        public static readonly Color Warning = Hex("F2A93B");

        public static readonly Color CellBorder = Hex("94A3B8");
        public static readonly Color CellEmpty = Hex("FFFFFF");
        public static readonly Color CellWord = Hex("DBEAFE");
        public static readonly Color CellSelected = Hex("93C5FD");
        public static readonly Color CellCorrect = Hex("DCF5E6");
        public static readonly Color CellHint = Hex("FFF1C9");
        public static readonly Color CellLetter = Hex("1F2937");
        public static readonly Color CellLetterCorrect = Hex("15803D");
        public static readonly Color CellLetterHint = Hex("A16207");

        public static readonly Color SlotEmpty = Hex("FFFFFF");
        public static readonly Color SlotLocked = Hex("E5E7EB");
        public static readonly Color SlotCursor = Hex("BFDBFE");
        public static readonly Color SlotCorrect = Hex("86EFAC");
        public static readonly Color SlotWrong = Hex("FCA5A5");

        public static readonly Color KeyNormal = Hex("FFFFFF");
        public static readonly Color KeySpecial = Hex("CBD5E1");
        public static readonly Color KeyDone = Hex("3B82F6");

        public static readonly Color QuestionActive = Hex("DBEAFE");
        public static readonly Color QuestionNormal = new Color(1f, 1f, 1f, 0f);

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }
    }
}
