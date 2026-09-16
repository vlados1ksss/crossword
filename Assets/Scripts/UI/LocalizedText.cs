using UnityEngine;
using UnityEngine.UI;

namespace CrosswordGame
{
    /// <summary>Подставляет в Text строку локализации по ключу. Тексты UI не зашиваются в код и сцену.</summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;

        public string Key
        {
            get => key;
            set
            {
                key = value;
                Refresh();
            }
        }

        private void OnEnable()
        {
            LocalizationManager.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable() => LocalizationManager.LanguageChanged -= Refresh;

        private void Refresh()
        {
            if (!string.IsNullOrEmpty(key))
                GetComponent<Text>().text = LocalizationManager.Get(key);
        }
    }
}
