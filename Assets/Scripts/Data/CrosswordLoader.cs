using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace CrosswordGame
{
    /// <summary>
    /// Загрузка текстовых файлов из StreamingAssets. На WebGL и Android StreamingAssets доступны
    /// только через UnityWebRequest, поэтому используется единый асинхронный путь для всех платформ.
    /// </summary>
    public static class CrosswordLoader
    {
        public const string Folder = "Crosswords";

        public static string GetLevelFileName(int levelNumber) => $"level_{levelNumber}.json";

        public static string GetLevelPath(int levelNumber) => Path.Combine(Application.streamingAssetsPath, Folder, GetLevelFileName(levelNumber));

        public static IEnumerator LoadText(string path, Action<string> onSuccess, Action<string> onError)
        {
            string url = path;
            if (!url.Contains("://"))
                url = new Uri(path).AbsoluteUri; // file:///...

            using (var request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke(request.error);
                    yield break;
                }
                onSuccess?.Invoke(request.downloadHandler.text);
            }
        }

        public static CrosswordData Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonUtility.FromJson<CrosswordData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Crossword JSON parse error: {e.Message}");
                return null;
            }
        }
    }
}
