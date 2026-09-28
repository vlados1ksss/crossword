using System.IO;
using UnityEditor;
using UnityEngine;


namespace CrosswordGame.EditorTools
{
    /// <summary>
    /// Экран загрузки: сначала стандартный логотип Unity, затем логотип студии с той же анимацией.
    /// Настройки splash общие для всех платформ, поэтому логотип виден и в WebGL-версии, и в Android.
    /// Логотип берётся из Assets/Art/Splash/splash_logo.png.
    /// </summary>
    public static class SplashScreenSetup
    {
        public const string LogoPath = "Assets/Art/Splash/splash_logo.png";
        private const float LogoDuration = 2f;   // минимальная длительность одного логотипа в Unity

        [MenuItem("Tools/Crossword/Apply Splash Screen", priority = 210)]
        public static void ApplyMenu()
        {
            if (Apply(true)) Debug.Log("Crossword: splash screen configured.");
        }

        public static bool Apply(bool log)
        {
            var logo = LoadLogoSprite();
            if (logo == null)
            {
                if (log) Debug.LogWarning($"Crossword: {LogoPath} not found, splash screen is left as is.");
                return false;
            }

            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = true;
            // Логотипы идут друг за другом: сначала Unity, потом студия.
            PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
            PlayerSettings.SplashScreen.backgroundColor = Color.black;   // под тёмный фон логотипа студии
            PlayerSettings.SplashScreen.logos = new[]
            {
                PlayerSettings.SplashScreenLogo.CreateWithUnityLogo(),
                PlayerSettings.SplashScreenLogo.Create(LogoDuration, logo),
            };

            AssetDatabase.SaveAssets();
            if (log) Debug.Log($"Crossword: splash logos = Unity + {LogoPath}, {LogoDuration}s each.");
            return true;
        }

        private static Sprite LoadLogoSprite()
        {
            if (!File.Exists(LogoPath)) return null;

            var importer = AssetImporter.GetAtPath(LogoPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
        }
    }
}
