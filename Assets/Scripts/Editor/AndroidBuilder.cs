using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CrosswordGame.EditorTools
{
    /// <summary>
    /// Сборка Android-версии игры (реклама Yandex Mobile Ads).
    /// Меню: Tools → Crossword → Android. Версию приложения (bundleVersion / bundleVersionCode)
    /// сборщик не меняет — её ведёт разработчик в Player Settings.
    /// </summary>
    public static class AndroidBuilder
    {
        public const string PackageName = "com.tupuppo.crossword";
        private const string OutputFolder = "Builds/Android";
        private const string GradleFolder = "Assets/Plugins/Android";
        private const string IconFolder = "Assets/Art/Icon";
        private const string IconSourcePath = IconFolder + "/icon_source.png";
        private const int IconSize = 432;
        private const string PendingBuildKey = "Crossword.PendingAndroidBuild";

        private enum BuildKind
        {
            ApkTest,
            ApkRelease,
            AabRelease
        }

        #region Menu

        [MenuItem("Tools/Crossword/Android/Build APK (test)", priority = 300)]
        private static void BuildApkTest() => Build(BuildKind.ApkTest);

        [MenuItem("Tools/Crossword/Android/Build APK (release)", priority = 301)]
        private static void BuildApkRelease() => Build(BuildKind.ApkRelease);

        [MenuItem("Tools/Crossword/Android/Build AAB (release)", priority = 302)]
        private static void BuildAabRelease() => Build(BuildKind.AabRelease);

        [MenuItem("Tools/Crossword/Android/Apply Player Settings", priority = 320)]
        private static void ApplyPlayerSettingsMenu()
        {
            ApplyPlayerSettings();
            EnsureGradleTemplates();
            EnsureIcons();
            Debug.Log("Crossword: Android player settings applied.");
        }

        [MenuItem("Tools/Crossword/Android/Resolve Dependencies", priority = 321)]
        private static void ResolveDependenciesMenu()
        {
            Debug.Log(ResolveAndroidDependencies()
                ? "Crossword: Android dependencies resolved."
                : "Crossword: dependency resolution failed, see messages above.");
        }

        #endregion

        #region Build flow

        private static void Build(BuildKind kind)
        {
            // Никогда не собираем в той же сессии, в которой переключили платформу:
            // скрипты должны перекомпилироваться под Android.
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("Crossword: switching active build target to Android, the build will continue automatically.");
                AndroidDefinesFixer.EnsureAndroidDefines(true);
                SessionState.SetString(PendingBuildKey, kind.ToString());
                EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Android, BuildTarget.Android);
                return;
            }

            BuildNow(kind);
        }

        [InitializeOnLoadMethod]
        private static void ContinuePendingBuild()
        {
            string pending = SessionState.GetString(PendingBuildKey, string.Empty);
            if (string.IsNullOrEmpty(pending)) return;
            SessionState.EraseString(PendingBuildKey);

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android) return;
            if (!Enum.TryParse(pending, out BuildKind kind)) return;

            EditorApplication.delayCall += () => BuildNow(kind);
        }

        private static void BuildNow(BuildKind kind)
        {
            bool release = kind != BuildKind.ApkTest;
            var config = AssetDatabase.LoadAssetAtPath<MobileAdsConfig>($"Assets/Resources/{MobileAdsConfig.ResourcesPath}.asset");

            if (!CheckBeforeBuild(release, config)) return;

            AndroidDefinesFixer.EnsureAndroidDefines(true);
            ApplyPlayerSettings();
            EnsureGradleTemplates();
            EnsureIcons();
            if (!ResolveAndroidDependencies())
            {
                Debug.LogError("Crossword: Android dependency resolution failed, build cancelled.");
                return;
            }

            Directory.CreateDirectory(OutputFolder);
            bool aab = kind == BuildKind.AabRelease;
            EditorUserBuildSettings.buildAppBundle = aab;
            EditorUserBuildSettings.development = false;

            string suffix = kind == BuildKind.ApkTest ? "-test" : string.Empty;
            string extension = aab ? "aab" : "apk";
            string path = $"{OutputFolder}/crossword-{PlayerSettings.bundleVersion}" +
                          $"-{PlayerSettings.Android.bundleVersionCode}{suffix}.{extension}";

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = path,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            Debug.Log($"Crossword: building {kind} -> {path}");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"Crossword: build {summary.result}. Kind: {kind}, package: {PlayerSettings.applicationIdentifier}, " +
                      $"version: {PlayerSettings.bundleVersion} ({PlayerSettings.Android.bundleVersionCode}), " +
                      $"ad units: {(config != null && config.UsesDemoUnits ? "DEMO" : "own")}, " +
                      $"errors: {summary.totalErrors}, warnings: {summary.totalWarnings}, output: {path}");
        }

        private static bool CheckBeforeBuild(bool release, MobileAdsConfig config)
        {
            if (config == null)
            {
                Debug.LogError($"Crossword: Assets/Resources/{MobileAdsConfig.ResourcesPath}.asset not found. " +
                               "Create it: Tools → Crossword → Android → Create Mobile Ads Config.");
                return false;
            }

            if (!release)
            {
                if (!config.UsesDemoUnits)
                    Debug.LogWarning("Crossword: test build uses REAL ad units. Do not click your own ads — " +
                                     "the account can be blocked.");
                return true;
            }

            bool ok = true;
            if (config.UsesDemoUnits)
            {
                Debug.LogError("Crossword: release build with demo ad units. Fill interstitialAdUnitId and " +
                               "rewardedAdUnitId in MobileAdsConfig and switch off Use Demo Ad Units.");
                ok = false;
            }
            if (!PlayerSettings.Android.useCustomKeystore)
            {
                Debug.LogError("Crossword: release build requires your own keystore. " +
                               "Player Settings → Publishing Settings → Custom Keystore.");
                ok = false;
            }
            // Unity не хранит пароли keystore на диске: после перезапуска редактора их вводят заново.
            else if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) ||
                     string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
            {
                Debug.LogError("Crossword: keystore passwords are empty. Enter them again in " +
                               "Player Settings → Publishing Settings (Unity does not store them on disk).");
                ok = false;
            }
            return ok;
        }

        #endregion

        #region Settings

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            // РСЯ требует target API 31+; Auto = самый свежий из установленных.
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.forceInternetPermission = true;   // реклама загружается по сети
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            SplashScreenSetup.Apply(false);
        }

        /// <summary>Плагин РСЯ требует пользовательские gradle-шаблоны, в них EDM4U пишет зависимости.</summary>
        private static void EnsureGradleTemplates()
        {
            Directory.CreateDirectory(GradleFolder);
            string engine = BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.Android, BuildOptions.None);
            string source = Path.Combine(engine, "Tools", "GradleTemplates");

            CopyIfMissing(Path.Combine(source, "mainTemplate.gradle"), $"{GradleFolder}/mainTemplate.gradle");
            CopyIfMissing(Path.Combine(source, "gradleTemplate.properties"), $"{GradleFolder}/gradleTemplate.properties");

            var playerSettings = new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
            SetBool(playerSettings, "useCustomMainGradleTemplate", true);
            SetBool(playerSettings, "useCustomGradlePropertiesTemplate", true);
            playerSettings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.Refresh();
        }

        private static void SetBool(SerializedObject target, string property, bool value)
        {
            var found = target.FindProperty(property);
            if (found == null)
            {
                Debug.LogWarning($"Crossword: player setting '{property}' not found, set it manually in Publishing Settings.");
                return;
            }
            found.boolValue = value;
        }

        private static void CopyIfMissing(string source, string destination)
        {
            if (File.Exists(destination)) return;
            if (!File.Exists(source))
            {
                Debug.LogWarning($"Crossword: gradle template not found: {source}");
                return;
            }
            File.Copy(source, destination);
            AssetDatabase.ImportAsset(destination);
            Debug.Log($"Crossword: gradle template copied to {destination}");
        }

        /// <summary>Резолв зависимостей EDM4U (вызывается рефлексией: пакет может отсутствовать).</summary>
        private static bool ResolveAndroidDependencies()
        {
            AndroidJdkEnvironment.Apply(false);   // gradle требует JAVA_HOME

            var resolver = Type.GetType("GooglePlayServices.PlayServicesResolver, Google.JarResolver", false);
            if (resolver == null)
            {
                Debug.LogWarning("Crossword: External Dependency Manager not found, dependencies are not resolved.");
                return false;
            }

            var method = resolver.GetMethod("ResolveSync", new[] { typeof(bool) });
            if (method == null)
            {
                Debug.LogWarning("Crossword: PlayServicesResolver.ResolveSync(bool) not found.");
                return false;
            }

            // Предупреждения EDM4U про PostProcessAndroidPlayer и версию Android SDK на Unity 6 безвредны.
            return (bool)method.Invoke(null, new object[] { true });
        }

        #endregion

        #region Icons

        [MenuItem("Tools/Crossword/Android/Create Mobile Ads Config", priority = 322)]
        private static void CreateConfig()
        {
            const string path = "Assets/Resources/" + MobileAdsConfig.ResourcesPath + ".asset";
            if (AssetDatabase.LoadAssetAtPath<MobileAdsConfig>(path) != null)
            {
                Debug.Log($"Crossword: {path} already exists.");
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<MobileAdsConfig>(path);
                return;
            }
            Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<MobileAdsConfig>(), path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<MobileAdsConfig>(path);
            Debug.Log($"Crossword: {path} created with demo ad units.");
        }

        /// <summary>
        /// При minSdk 26 Unity поддерживает только адаптивную иконку: два слоя 432×432.
        /// Лаунчер показывает центральные две трети канвы и применяет маску (круг, скруглённый квадрат),
        /// поэтому арт из Assets/Art/Icon/icon_source.png вписывается в видимую зону переднего слоя,
        /// а фоновый слой заливается градиентом в цветах самого арта.
        /// </summary>
        private static void EnsureIcons()
        {
            Directory.CreateDirectory(IconFolder);
            string backgroundPath = $"{IconFolder}/icon_background.png";
            string foregroundPath = $"{IconFolder}/icon_foreground.png";

            var source = LoadReadableTexture(IconSourcePath);
            if (source != null)
            {
                WritePng(backgroundPath, BuildIconBackground(source));
                WritePng(foregroundPath, BuildIconForeground(source));
            }
            else
            {
                Debug.LogWarning($"Crossword: {IconSourcePath} not found, a placeholder icon is generated.");
                WritePng(backgroundPath, BuildPlaceholder(DrawIconBackground));
                WritePng(foregroundPath, BuildPlaceholder(DrawIconForeground));
            }

            ImportIconTexture(backgroundPath);
            ImportIconTexture(foregroundPath);

            var background = AssetDatabase.LoadAssetAtPath<Texture2D>(backgroundPath);
            var foreground = AssetDatabase.LoadAssetAtPath<Texture2D>(foregroundPath);
            if (background == null || foreground == null) return;

            var kinds = PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android);
            var adaptive = kinds.FirstOrDefault(k => k.ToString() == "Adaptive");
            if (adaptive == null)
            {
                Debug.LogWarning("Crossword: adaptive icon kind is not supported by this editor, icon is not set.");
                return;
            }

            var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, adaptive);
            foreach (var icon in icons)
                icon.SetTextures(background, foreground);
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, adaptive, icons);
            Debug.Log($"Crossword: adaptive icon built from {(source != null ? IconSourcePath : "placeholder")}.");
        }

        /// <summary>Фон: вертикальный градиент между средними цветами верхней и нижней кромок арта.</summary>
        private static Color32[] BuildIconBackground(Texture2D source)
        {
            Color top = AverageEdgeColor(source, true);
            Color bottom = AverageEdgeColor(source, false);
            var pixels = new Color32[IconSize * IconSize];
            for (int y = 0; y < IconSize; y++)
            {
                Color row = Color.Lerp(bottom, top, (float)y / (IconSize - 1));
                row.a = 1f;
                for (int x = 0; x < IconSize; x++)
                    pixels[y * IconSize + x] = row;
            }
            return pixels;
        }

        /// <summary>Передний слой: арт, вписанный в видимую зону, с мягко скруглёнными углами.</summary>
        private static Color32[] BuildIconForeground(Texture2D source)
        {
            const float artScale = 0.78f;          // доля канвы под арт
            int art = Mathf.RoundToInt(IconSize * artScale);
            int offset = (IconSize - art) / 2;
            float radius = art * 0.14f;            // скругление углов арта

            var pixels = new Color32[IconSize * IconSize];
            for (int y = 0; y < art; y++)
            for (int x = 0; x < art; x++)
            {
                float u = (x + 0.5f) / art;
                float v = (y + 0.5f) / art;
                Color color = source.GetPixelBilinear(u, v);
                color.a *= RoundedCornerAlpha(x + 0.5f, y + 0.5f, art, radius);
                pixels[(y + offset) * IconSize + x + offset] = color;
            }
            return pixels;
        }

        private static float RoundedCornerAlpha(float x, float y, int size, float radius)
        {
            float cx = Mathf.Clamp(x, radius, size - radius);
            float cy = Mathf.Clamp(y, radius, size - radius);
            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            return Mathf.Clamp01(radius - distance + 0.5f);
        }

        private static Color AverageEdgeColor(Texture2D source, bool top)
        {
            int band = Mathf.Max(1, source.height / 12);
            int startY = top ? source.height - band : 0;
            Color sum = Color.black;
            int count = 0;
            for (int y = startY; y < startY + band; y++)
            for (int x = 0; x < source.width; x += 4)
            {
                sum += source.GetPixel(x, y);
                count++;
            }
            return count > 0 ? sum / count : Color.gray;
        }

        private static Color32[] BuildPlaceholder(Func<int, int, int, Color32> pixel)
        {
            var pixels = new Color32[IconSize * IconSize];
            for (int y = 0; y < IconSize; y++)
            for (int x = 0; x < IconSize; x++)
                pixels[y * IconSize + x] = pixel(x, y, IconSize);
            return pixels;
        }

        private static void WritePng(string path, Color32[] pixels)
        {
            var texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
        }

        private static Texture2D LoadReadableTexture(string path)
        {
            if (!File.Exists(path)) return null;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return null;
            if (!importer.isReadable || importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureType = TextureImporterType.Default;
                importer.isReadable = true;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void ImportIconTexture(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }

        private static Color32 DrawIconBackground(int x, int y, int size)
        {
            float t = (float)y / size;
            Color color = Color.Lerp(UIPalette.AccentDark, UIPalette.Accent, t);
            return color;
        }

        private static Color32 DrawIconForeground(int x, int y, int size)
        {
            // Мотив кроссворда: три белые клетки уголком в центральной части иконки.
            float cell = size * 0.17f;
            float gap = size * 0.022f;
            float originX = size * 0.5f - cell * 1.5f - gap;
            float originY = size * 0.5f - cell * 1.5f - gap;

            bool[,] shape =
            {
                { true, true, true },
                { true, false, false },
                { true, false, false },
            };

            for (int row = 0; row < 3; row++)
            for (int col = 0; col < 3; col++)
            {
                if (!shape[row, col]) continue;
                float left = originX + col * (cell + gap);
                float bottom = originY + (2 - row) * (cell + gap);
                if (x >= left && x <= left + cell && y >= bottom && y <= bottom + cell)
                    return new Color32(255, 255, 255, 255);
            }
            return new Color32(255, 255, 255, 0);
        }

        #endregion
    }
}
