using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrosswordGame
{
    [Serializable]
    public class LocalizationEntry
    {
        public string key;
        public string value;
    }

    /// <summary>Файл языка: Resources/Localization/{lang}.json. Содержит тексты UI и раскладку клавиатуры.</summary>
    [Serializable]
    public class LanguageFile
    {
        public string language;
        public string alphabet;
        public string[] keyboardRows;
        public string physicalKeysLatin;
        public string physicalKeysLocal;
        public List<LocalizationEntry> entries = new List<LocalizationEntry>();
    }

    /// <summary>
    /// Простая локализация без сторонних пакетов. Чтобы добавить английский язык, достаточно
    /// создать Resources/Localization/en.json и добавить "en" в SupportedLanguages.
    /// </summary>
    public static class LocalizationManager
    {
        public const string DefaultLanguage = "ru";
        public static readonly string[] SupportedLanguages = { "ru" };

        public static event Action LanguageChanged;

        public static string CurrentLanguage { get; private set; }
        public static LanguageFile Current { get; private set; }

        private static readonly Dictionary<string, string> Strings = new Dictionary<string, string>();

        public static bool IsInitialized => Current != null;

        /// <summary>
        /// Выбирает язык по коду из SDK платформы: поддерживаемый язык используется как есть,
        /// русскоязычные регионы (be, kk, uk, uz) получают русский, остальные — язык по умолчанию.
        /// </summary>
        public static void Initialize(string sdkLanguage)
        {
            string code = string.IsNullOrEmpty(sdkLanguage) ? string.Empty : sdkLanguage.Trim().ToLowerInvariant();
            string lang = DefaultLanguage;
            if (Array.IndexOf(SupportedLanguages, code) >= 0)
                lang = code;
            else if (Array.IndexOf(RussianRegionLanguages, code) >= 0 && Array.IndexOf(SupportedLanguages, "ru") >= 0)
                lang = "ru";

            Debug.Log($"[Loc] SDK language '{sdkLanguage}' -> {lang}");
            SetLanguage(lang);
        }

        private static readonly string[] RussianRegionLanguages = { "ru", "be", "kk", "uk", "uz" };

        public static void SetLanguage(string lang)
        {
            var asset = Resources.Load<TextAsset>($"Localization/{lang}");
            if (asset == null && lang != DefaultLanguage)
            {
                Debug.LogWarning($"Localization '{lang}' not found, fallback to '{DefaultLanguage}'.");
                lang = DefaultLanguage;
                asset = Resources.Load<TextAsset>($"Localization/{lang}");
            }
            if (asset == null)
            {
                Debug.LogError("Localization file is missing: Resources/Localization/ru.json");
                return;
            }

            Current = JsonUtility.FromJson<LanguageFile>(asset.text);
            CurrentLanguage = lang;
            Strings.Clear();
            foreach (var entry in Current.entries)
                Strings[entry.key] = entry.value;

            LanguageChanged?.Invoke();
        }

        public static void EnsureInitialized()
        {
            if (!IsInitialized) SetLanguage(DefaultLanguage);
        }

        public static string Get(string key)
        {
            EnsureInitialized();
            return Strings.TryGetValue(key, out var value) ? value : key;
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }

        public static string Alphabet
        {
            get
            {
                EnsureInitialized();
                return Current.alphabet;
            }
        }

        /// <summary>Преобразует символ с физической клавиатуры в букву игры (учитывает латинскую раскладку).</summary>
        public static char MapPhysicalChar(char c)
        {
            EnsureInitialized();
            char upper = char.ToUpperInvariant(c);
            if (upper == 'Ё') upper = 'Е';
            if (Current.alphabet.IndexOf(upper) >= 0) return upper;

            int index = Current.physicalKeysLatin.IndexOf(upper);
            if (index >= 0 && index < Current.physicalKeysLocal.Length) return Current.physicalKeysLocal[index];

            // Символы, которые в латинской раскладке зависят от Shift: { } : " < >
            switch (c)
            {
                case '{': return MapPhysicalChar('[');
                case '}': return MapPhysicalChar(']');
                case ':': return MapPhysicalChar(';');
                case '"': return MapPhysicalChar('\'');
                case '<': return MapPhysicalChar(',');
                case '>': return MapPhysicalChar('.');
            }
            return '\0';
        }
    }
}
