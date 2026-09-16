using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static CrosswordGame.EditorTools.CrosswordUIFactory;

namespace CrosswordGame.EditorTools
{
    /// <summary>
    /// Генерирует префабы, сцены MainMenu и Game и настраивает проект (Build Settings, WebGL-шаблон, YG2).
    /// Сцены можно дорабатывать вручную; повторная генерация перезапишет их.
    /// Меню: Tools → Crossword → Rebuild Scenes And Prefabs.
    /// </summary>
    public static class CrosswordSceneBuilder
    {
        private const string PrefabFolder = "Assets/Prefabs";
        private const string SceneFolder = "Assets/Scenes";
        private static string MainMenuPath => $"{SceneFolder}/{GameManager.MainMenuScene}.unity";
        private static string GamePath => $"{SceneFolder}/{GameManager.GameScene}.unity";

        [MenuItem("Tools/Crossword/Rebuild Scenes And Prefabs", priority = 100)]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            LocalizationManager.SetLanguage(LocalizationManager.DefaultLanguage);
            EnsureSprites();
            Directory.CreateDirectory(PrefabFolder);
            Directory.CreateDirectory(SceneFolder);

            var prefabs = BuildPrefabs();
            BuildMainMenuScene(prefabs);
            BuildGameScene(prefabs);
            ConfigureProject();

            EditorSceneManager.OpenScene(MainMenuPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Crossword: scenes and prefabs rebuilt.");
        }

        /// <summary>
        /// Префабы загружаются при каждом обращении: EditorSceneManager.NewScene выгружает неиспользуемые ассеты,
        /// и ранее полученные ссылки на компоненты становятся недействительными.
        /// </summary>
        private class Prefabs
        {
            public CrosswordCell Cell => LoadPrefab<CrosswordCell>("CrosswordCell");
            public QuestionItemUI QuestionItem => LoadPrefab<QuestionItemUI>("QuestionItem");
            public KeyboardKey Key => LoadPrefab<KeyboardKey>("KeyboardKey");
            public RectTransform KeyboardRow => LoadPrefab<RectTransform>("KeyboardRow");
            public AnswerSlot AnswerSlot => LoadPrefab<AnswerSlot>("AnswerSlot");
            public LevelButtonUI LevelButton => LoadPrefab<LevelButtonUI>("LevelButton");
        }

        #region Prefabs

        private static Prefabs BuildPrefabs()
        {
            SavePrefab(BuildCell(), "CrosswordCell");
            SavePrefab(BuildQuestionItem(), "QuestionItem");
            SavePrefab(BuildKey(), "KeyboardKey");
            SavePrefab(BuildKeyboardRow(), "KeyboardRow");
            SavePrefab(BuildAnswerSlot(), "AnswerSlot");
            SavePrefab(BuildLevelButton(), "LevelButton");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            return new Prefabs();
        }

        private static void SavePrefab(RectTransform root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, $"{PrefabFolder}/{name}.prefab");
            Object.DestroyImmediate(root.gameObject);
        }

        private static T LoadPrefab<T>(string name) where T : Component
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{name}.prefab");
            var component = go != null ? go.GetComponent<T>() : null;
            if (component == null) Debug.LogError($"Crossword: prefab {name} has no component {typeof(T).Name}");
            return component;
        }

        private static RectTransform BuildCell()
        {
            var root = Create("CrosswordCell", null);
            root.sizeDelta = new Vector2(64, 64);
            AddImage(root, null, new Color(1, 1, 1, 0)); // область нажатия

            var content = Stretch(Create("Content", root));
            // Меньший радиус скругления: клетки бывают мелкими на телефонах.
            AddImage(content, RoundedSmall, UIPalette.CellBorder, false).pixelsPerUnitMultiplier = 2f;
            var fill = Stretch(Create("Fill", content), 2, 2, 2, 2);
            var fillImage = AddImage(fill, RoundedSmall, UIPalette.CellEmpty, false);
            fillImage.pixelsPerUnitMultiplier = 2f;

            var letter = CreateText("Letter", content, "", 36, UIPalette.CellLetter, TextAnchor.MiddleCenter, true);
            Stretch(letter.rectTransform, 0, 0, 0, 0);
            letter.horizontalOverflow = HorizontalWrapMode.Overflow;

            var number = CreateText("Number", content, "", 14, UIPalette.TextSecondary, TextAnchor.UpperLeft);
            number.rectTransform.anchorMin = new Vector2(0, 0.5f);
            number.rectTransform.anchorMax = new Vector2(1, 1);
            number.rectTransform.offsetMin = new Vector2(4, 0);
            number.rectTransform.offsetMax = new Vector2(0, -1);
            number.horizontalOverflow = HorizontalWrapMode.Overflow;

            var cell = root.gameObject.AddComponent<CrosswordCell>();
            SetField(cell, "fill", fillImage);
            SetField(cell, "letterText", letter);
            SetField(cell, "numberText", number);
            SetField(cell, "content", content);
            return root;
        }

        private static RectTransform BuildQuestionItem()
        {
            var root = Create("QuestionItem", null);
            root.sizeDelta = new Vector2(600, 60);
            var background = AddImage(root, RoundedSmall, UIPalette.QuestionNormal);
            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = background;
            SetupColors(button);

            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var number = CreateText("Number", root, "1", 30, UIPalette.Accent, TextAnchor.UpperRight, true);
            var numberLayout = number.gameObject.AddComponent<LayoutElement>();
            numberLayout.minWidth = 44;
            numberLayout.preferredWidth = 44;

            var question = CreateText("Question", root, "Вопрос", 30, UIPalette.TextPrimary, TextAnchor.UpperLeft);
            question.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var item = root.gameObject.AddComponent<QuestionItemUI>();
            SetField(item, "button", button);
            SetField(item, "background", background);
            SetField(item, "numberText", number);
            SetField(item, "questionText", question);
            return root;
        }

        private static RectTransform BuildKey()
        {
            var root = Create("KeyboardKey", null);
            root.sizeDelta = new Vector2(70, 80);
            var image = AddImage(root, RoundedSmall, UIPalette.KeyNormal);
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            SetupColors(button);
            root.gameObject.AddComponent<ButtonPressAnimation>();
            var layoutElement = root.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 70;
            layoutElement.preferredHeight = 80;

            var label = CreateText("Label", root, "А", 36, UIPalette.TextPrimary, TextAnchor.MiddleCenter, true);
            Stretch(label.rectTransform);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;

            var iconRect = Create("Icon", root);
            iconRect.anchorMin = new Vector2(0.15f, 0.15f);
            iconRect.anchorMax = new Vector2(0.85f, 0.85f);
            iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
            var icon = AddImage(iconRect, null, UIPalette.TextPrimary, false);
            icon.preserveAspect = true;
            iconRect.gameObject.SetActive(false);

            var key = root.gameObject.AddComponent<KeyboardKey>();
            SetField(key, "button", button);
            SetField(key, "background", image);
            SetField(key, "label", label);
            SetField(key, "icon", icon);
            SetField(key, "layoutElement", layoutElement);
            return root;
        }

        private static RectTransform BuildKeyboardRow()
        {
            var root = Create("KeyboardRow", null);
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 8;
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
            return root;
        }

        private static RectTransform BuildAnswerSlot()
        {
            var root = Create("AnswerSlot", null);
            root.sizeDelta = new Vector2(80, 80);
            var layoutElement = root.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 80;
            layoutElement.preferredHeight = 80;

            var content = Stretch(Create("Content", root));
            var border = AddImage(content, RoundedSmall, UIPalette.CellBorder);
            var fill = Stretch(Create("Fill", content), 3, 3, 3, 3);
            var background = AddImage(fill, RoundedSmall, UIPalette.SlotEmpty, false);

            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = border;
            SetupColors(button);

            var letter = CreateText("Letter", content, "", 44, UIPalette.TextPrimary, TextAnchor.MiddleCenter, true);
            Stretch(letter.rectTransform);
            letter.horizontalOverflow = HorizontalWrapMode.Overflow;

            var slot = root.gameObject.AddComponent<AnswerSlot>();
            SetField(slot, "button", button);
            SetField(slot, "background", background);
            SetField(slot, "letterText", letter);
            SetField(slot, "content", content);
            SetField(slot, "layoutElement", layoutElement);
            return root;
        }

        private static RectTransform BuildLevelButton()
        {
            var root = Create("LevelButton", null);
            root.sizeDelta = new Vector2(720, 132);
            var background = AddImage(root, RoundedLarge, UIPalette.Panel);
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            SetupColors(button);
            root.gameObject.AddComponent<ButtonPressAnimation>();
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 132;

            var title = CreateText("Title", root, "Уровень 1", 42, UIPalette.TextPrimary, TextAnchor.MiddleLeft, true);
            Anchor(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -16), new Vector2(380, 56));

            var difficulty = CreateText("Difficulty", root, "Очень легко", 28, UIPalette.TextSecondary, TextAnchor.MiddleLeft);
            Anchor(difficulty.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(32, 16), new Vector2(380, 44));

            var dots = Create("Dots", root);
            Anchor(dots, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-32, -30), new Vector2(150, 22));
            var dotsLayout = dots.gameObject.AddComponent<HorizontalLayoutGroup>();
            dotsLayout.spacing = 10;
            dotsLayout.childAlignment = TextAnchor.MiddleRight;
            dotsLayout.childControlWidth = false;
            dotsLayout.childControlHeight = false;
            dotsLayout.childForceExpandWidth = false;
            var dotImages = new List<Object>();
            for (int i = 0; i < 5; i++)
            {
                var dot = Create($"Dot{i + 1}", dots);
                dot.sizeDelta = new Vector2(20, 20);
                dotImages.Add(AddImage(dot, Circle, UIPalette.Warning, false));
            }

            var status = CreateText("Status", root, "Не начат", 28, UIPalette.TextSecondary, TextAnchor.MiddleRight);
            Anchor(status.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-32, 16), new Vector2(320, 44));

            var levelButton = root.gameObject.AddComponent<LevelButtonUI>();
            SetField(levelButton, "button", button);
            SetField(levelButton, "background", background);
            SetField(levelButton, "titleText", title);
            SetField(levelButton, "difficultyText", difficulty);
            SetField(levelButton, "statusText", status);
            SetArray(levelButton, "difficultyDots", dotImages.ToArray());
            return root;
        }

        #endregion

        #region Common scene parts

        private static RectTransform CreateCanvas()
        {
            var cameraGo = new GameObject("Main Camera", typeof(Camera));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UIPalette.Background;
            camera.orthographic = true;
            cameraGo.transform.position = new Vector3(0, 0, -10);

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<AdaptiveCanvasScaler>();

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetAsLastSibling();

            var background = Stretch(Create("Background", canvasGo.transform));
            AddImage(background, null, UIPalette.Background, false);
            return (RectTransform)canvasGo.transform;
        }

        private static RectTransform CreateSafeArea(RectTransform canvas)
        {
            var safeArea = Stretch(Create("SafeArea", canvas));
            safeArea.gameObject.AddComponent<SafeAreaFitter>();
            return safeArea;
        }

        private static ScrollRect CreateScrollView(RectTransform parent, out RectTransform content)
        {
            var scroll = parent.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            scroll.inertia = true;

            var viewport = Stretch(Create("Viewport", parent), 8, 8, 8, 8);
            viewport.gameObject.AddComponent<RectMask2D>();
            AddImage(viewport, null, new Color(1, 1, 1, 0)); // для скролла пальцем по пустому месту

            content = Create("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 12;
            layout.padding = new RectOffset(8, 8, 8, 8);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return scroll;
        }

        private static RectTransform CreateToast(RectTransform canvas, out ToastMessage toast)
        {
            var root = Create("Toast", canvas);
            Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 120));
            var group = root.gameObject.AddComponent<CanvasGroup>();
            AddImage(root, RoundedLarge, new Color(0.12f, 0.16f, 0.22f, 0.92f), false);
            var text = CreateText("Message", root, "", 34, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 24, 8, 24, 8);
            toast = root.gameObject.AddComponent<ToastMessage>();
            SetField(toast, "canvasGroup", group);
            SetField(toast, "messageText", text);
            return root;
        }

        #endregion

        #region Main menu

        private static void BuildMainMenuScene(Prefabs prefabs)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var canvas = CreateCanvas();
            var safeArea = CreateSafeArea(canvas);

            // Main panel: заголовок и кнопки (скрывается при выборе уровня)
            var main = Stretch(Create("MainPanel", safeArea));
            var mainGroup = main.gameObject.AddComponent<CanvasGroup>();

            var header = Create("Header", main);
            Anchor(header, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(1000, 230));
            var title = CreateLocalizedText("Title", header, "game_title", 110, UIPalette.TextPrimary, TextAnchor.MiddleCenter, true);
            Anchor(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(1000, 140));
            var subtitle = CreateLocalizedText("Subtitle", header, "game_subtitle", 40, UIPalette.TextSecondary, TextAnchor.MiddleCenter);
            Anchor(subtitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(1000, 60));

            var buttons = Create("Buttons", main);
            Anchor(buttons, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(680, 420));

            var continueButton = CreateButton("ContinueButton", buttons, null, UIPalette.Accent, Color.white, 50, out var continueLabel);
            Anchor((RectTransform)continueButton.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(680, 170));
            continueLabel.text = LocalizationManager.Get("menu_continue");
            Stretch(continueLabel.rectTransform, 12, 70, 12, 20);
            var continueSub = CreateText("SubLabel", continueButton.transform, "", 32, new Color(1, 1, 1, 0.85f), TextAnchor.MiddleCenter);
            Stretch(continueSub.rectTransform, 12, 22, 12, 100);

            var selectButton = CreateButton("LevelSelectButton", buttons, "menu_select_level", UIPalette.Panel, UIPalette.TextPrimary, 44, out _);
            Anchor((RectTransform)selectButton.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -200), new Vector2(680, 120));

            var loading = CreateLocalizedText("Loading", buttons, "menu_loading", 34, UIPalette.TextSecondary, TextAnchor.MiddleCenter);
            Anchor(loading.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(680, 60));

            // Level selection
            var selection = Stretch(Create("LevelSelection", safeArea));
            var selectionGroup = selection.gameObject.AddComponent<CanvasGroup>();
            var selectionTitle = CreateLocalizedText("Title", selection, "menu_select_level", 64, UIPalette.TextPrimary, TextAnchor.MiddleCenter, true);
            Anchor(selectionTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(900, 90));

            var listArea = Create("LevelList", selection);
            listArea.anchorMin = new Vector2(0.5f, 0);
            listArea.anchorMax = new Vector2(0.5f, 1);
            listArea.pivot = new Vector2(0.5f, 0.5f);
            listArea.sizeDelta = new Vector2(780, 0);
            listArea.offsetMin = new Vector2(-390, 170);
            listArea.offsetMax = new Vector2(390, -140);
            CreateScrollView(listArea, out var levelsContent);

            var backButton = CreateButton("BackButton", selection, "menu_back", UIPalette.Panel, UIPalette.TextPrimary, 40, out _);
            Anchor((RectTransform)backButton.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(420, 110));

            var controllerGo = new GameObject("MainMenuController");
            var controller = controllerGo.AddComponent<MainMenuController>();
            SetField(controller, "mainPanel", mainGroup);
            SetField(controller, "levelSelectionPanel", selectionGroup);
            SetField(controller, "continueButton", continueButton);
            SetField(controller, "continueLabel", continueLabel);
            SetField(controller, "continueSubLabel", continueSub);
            SetField(controller, "selectLevelButton", selectButton);
            SetField(controller, "backButton", backButton);
            SetField(controller, "levelsContainer", levelsContent);
            SetField(controller, "levelButtonPrefab", prefabs.LevelButton);
            SetField(controller, "loadingText", loading);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), MainMenuPath);
        }

        #endregion

        #region Game

        private static void BuildGameScene(Prefabs prefabs)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var canvas = CreateCanvas();
            var safeArea = CreateSafeArea(canvas);
            var layout = safeArea.gameObject.AddComponent<AdaptiveLayoutController>();

            // Top bar
            var topBar = Create("TopBar", safeArea);
            var menuButton = CreateButton("MenuButton", topBar, "game_menu", UIPalette.Panel, UIPalette.TextPrimary, 34, out _);
            Anchor((RectTransform)menuButton.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(180, 76));
            ((RectTransform)menuButton.transform).pivot = new Vector2(0, 0.5f);
            ((RectTransform)menuButton.transform).anchoredPosition = new Vector2(16, 0);

            var levelTitle = CreateText("LevelTitle", topBar, "", 40, UIPalette.TextPrimary, TextAnchor.MiddleCenter, true);
            Stretch(levelTitle.rectTransform, 210, 0, 210, 0);
            levelTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            levelTitle.resizeTextForBestFit = true;
            levelTitle.resizeTextMinSize = 20;
            levelTitle.resizeTextMaxSize = 40;

            var counterBack = Create("Counter", topBar);
            Anchor(counterBack, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(160, 76));
            counterBack.pivot = new Vector2(1, 0.5f);
            counterBack.anchoredPosition = new Vector2(-16, 0);
            AddImage(counterBack, RoundedLarge, UIPalette.Panel, false);
            var counter = CreateText("Text", counterBack, "0/12", 36, UIPalette.Accent, TextAnchor.MiddleCenter, true);
            Stretch(counter.rectTransform);

            // Crossword area
            var crosswordArea = Create("CrosswordArea", safeArea);
            AddImage(crosswordArea, RoundedLarge, UIPalette.Panel, false);
            var gridRoot = Create("CrosswordGrid", crosswordArea);
            Anchor(gridRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100));
            var gridManager = crosswordArea.gameObject.AddComponent<CrosswordGridManager>();
            SetField(gridManager, "gridRoot", gridRoot);
            SetField(gridManager, "cellPrefab", prefabs.Cell);

            var hint = CreateButton("HintButton", crosswordArea, "game_hint", UIPalette.Warning, Color.white, 32, out _);
            Anchor((RectTransform)hint.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(210, 72));
            var hintButton = hint.gameObject.AddComponent<HintButton>();
            SetField(hintButton, "button", hint);
            SetField(hintButton, "area", crosswordArea);
            hint.gameObject.SetActive(false);

            // Questions
            var questionsArea = Create("QuestionsArea", safeArea);
            AddImage(questionsArea, RoundedLarge, UIPalette.Panel, false);
            var scroll = CreateScrollView(questionsArea, out var questionsContent);
            var acrossHeader = CreateLocalizedText("AcrossHeader", questionsContent, "game_across", 36, UIPalette.TextPrimary, TextAnchor.MiddleLeft, true);
            acrossHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 54;
            var acrossList = CreateVerticalList("AcrossQuestions", questionsContent);
            var downHeader = CreateLocalizedText("DownHeader", questionsContent, "game_down", 36, UIPalette.TextPrimary, TextAnchor.MiddleLeft, true);
            downHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 54;
            var downList = CreateVerticalList("DownQuestions", questionsContent);

            var questionList = questionsArea.gameObject.AddComponent<QuestionListController>();
            SetField(questionList, "scrollRect", scroll);
            SetField(questionList, "acrossContainer", acrossList);
            SetField(questionList, "downContainer", downList);
            SetField(questionList, "itemPrefab", prefabs.QuestionItem);

            // Bottom: answer input + keyboard
            var bottomArea = Create("BottomArea", safeArea);

            var inputArea = Create("AnswerInputArea", bottomArea);
            inputArea.anchorMin = new Vector2(0, 1);
            inputArea.anchorMax = new Vector2(1, 1);
            inputArea.pivot = new Vector2(0.5f, 1);
            inputArea.offsetMin = new Vector2(0, -96);
            inputArea.offsetMax = Vector2.zero;
            var slots = Stretch(Create("Slots", inputArea));
            var slotsLayout = slots.gameObject.AddComponent<HorizontalLayoutGroup>();
            slotsLayout.childAlignment = TextAnchor.MiddleCenter;
            slotsLayout.childControlWidth = true;
            slotsLayout.childControlHeight = true;
            slotsLayout.childForceExpandWidth = false;
            slotsLayout.childForceExpandHeight = false;
            slotsLayout.spacing = 8;
            var prompt = CreateLocalizedText("Prompt", inputArea, "game_select_prompt", 34, UIPalette.TextSecondary, TextAnchor.MiddleCenter);
            Stretch(prompt.rectTransform);
            var answerInput = inputArea.gameObject.AddComponent<AnswerInputController>();
            SetField(answerInput, "slotsRoot", slots);
            SetField(answerInput, "slotPrefab", prefabs.AnswerSlot);
            SetField(answerInput, "promptText", prompt);

            var keyboardRect = Create("Keyboard", bottomArea);
            Stretch(keyboardRect, 0, 0, 0, 108);
            var keyboardGroup = keyboardRect.gameObject.AddComponent<CanvasGroup>();
            var rows = Stretch(Create("Rows", keyboardRect));
            var rowsLayout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            rowsLayout.childAlignment = TextAnchor.MiddleCenter;
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = true;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = false;
            rowsLayout.spacing = 8;
            var keyboard = keyboardRect.gameObject.AddComponent<KeyboardController>();
            SetField(keyboard, "rowsRoot", rows);
            SetField(keyboard, "rowPrefab", prefabs.KeyboardRow);
            SetField(keyboard, "keyPrefab", prefabs.Key);
            SetField(keyboard, "canvasGroup", keyboardGroup);
            SetField(keyboard, "backspaceIcon", BackspaceIcon);

            SetField(layout, "topBar", topBar);
            SetField(layout, "crosswordArea", crosswordArea);
            SetField(layout, "questionsArea", questionsArea);
            SetField(layout, "bottomArea", bottomArea);

            // Overlays
            CreateToast(canvas, out var toast);
            var completePanel = BuildLevelCompletePanel(canvas);

            var controllerGo = new GameObject("CrosswordManager");
            var physical = controllerGo.AddComponent<PhysicalKeyboardInput>();
            var manager = controllerGo.AddComponent<CrosswordManager>();
            SetField(manager, "grid", gridManager);
            SetField(manager, "questionList", questionList);
            SetField(manager, "answerInput", answerInput);
            SetField(manager, "keyboard", keyboard);
            SetField(manager, "physicalKeyboard", physical);
            SetField(manager, "hintButton", hintButton);
            SetField(manager, "levelCompletePanel", completePanel);
            SetField(manager, "toast", toast);
            SetField(manager, "levelTitleText", levelTitle);
            SetField(manager, "counterText", counter);
            SetField(manager, "menuButton", menuButton);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), GamePath);
        }

        private static RectTransform CreateVerticalList(string name, RectTransform parent)
        {
            var list = Create(name, parent);
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 4;
            return list;
        }

        private static LevelCompletePanel BuildLevelCompletePanel(RectTransform canvas)
        {
            var root = Stretch(Create("LevelCompletePanel", canvas));
            AddImage(root, null, new Color(0.06f, 0.09f, 0.16f, 0.55f)); // затемнение и блокировка нажатий
            var group = root.gameObject.AddComponent<CanvasGroup>();

            var card = Create("Card", root);
            Anchor(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 860));
            AddImage(card, RoundedLarge, UIPalette.Panel);

            var title = CreateLocalizedText("Title", card, "complete_title", 64, UIPalette.Success, TextAnchor.MiddleCenter, true);
            Anchor(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(700, 100));
            title.rectTransform.pivot = new Vector2(0.5f, 1);

            var words = CreateText("Words", card, "", 38, UIPalette.TextPrimary, TextAnchor.MiddleCenter);
            Anchor(words.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -190), new Vector2(700, 56));
            var time = CreateText("Time", card, "", 38, UIPalette.TextPrimary, TextAnchor.MiddleCenter);
            Anchor(time.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -250), new Vector2(700, 56));
            var hints = CreateText("Hints", card, "", 38, UIPalette.TextPrimary, TextAnchor.MiddleCenter);
            Anchor(hints.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -310), new Vector2(700, 56));

            var next = CreateButton("NextButton", card, "complete_next", UIPalette.Accent, Color.white, 42, out _);
            Anchor((RectTransform)next.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 340), new Vector2(600, 120));
            var menu = CreateButton("MenuButton", card, "complete_menu", UIPalette.KeySpecial, UIPalette.TextPrimary, 38, out _);
            Anchor((RectTransform)menu.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 200), new Vector2(600, 110));
            var replay = CreateButton("ReplayButton", card, "complete_replay", UIPalette.Panel, UIPalette.Accent, 34, out _);
            Anchor((RectTransform)replay.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(600, 100));

            var panel = root.gameObject.AddComponent<LevelCompletePanel>();
            SetField(panel, "canvasGroup", group);
            SetField(panel, "card", card);
            SetField(panel, "titleText", title);
            SetField(panel, "wordsText", words);
            SetField(panel, "timeText", time);
            SetField(panel, "hintsText", hints);
            SetField(panel, "nextButton", next);
            SetField(panel, "menuButton", menu);
            SetField(panel, "replayButton", replay);
            root.gameObject.SetActive(false);
            return panel;
        }

        #endregion

        #region Project settings

        private static void ConfigureProject()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuPath, true),
                new EditorBuildSettingsScene(GamePath, true),
            };

            PlayerSettings.productName = "Crossword"; // латиница: имя используется в путях сборки и persistentDataPath
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            if (Directory.Exists("Assets/WebGLTemplates/YandexGames"))
                PlayerSettings.WebGL.template = "PROJECT:YandexGames";

            ConfigureYG2();
        }

        /// <summary>
        /// Настройки PluginYourGames: русский язык интерфейса плагина и симуляции,
        /// Game Ready вызывается игрой вручную после загрузки уровней (PlatformBridge.GameReady).
        /// </summary>
        private static void ConfigureYG2()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/PluginYourGames/Resources/SettingsYG2.asset");
            if (settings == null) return;

            var so = new SerializedObject(settings);
            var autoGRA = so.FindProperty("Basic.autoGRA");
            if (autoGRA != null) autoGRA.boolValue = false;
            var language = so.FindProperty("Simulation.language");
            if (language != null) language.stringValue = "ru";
            var showFirstAdv = so.FindProperty("InterstitialAdv.showFirstAdv");
            if (showFirstAdv != null) showFirstAdv.boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);

            foreach (var target in new[] { NamedBuildTarget.WebGL, NamedBuildTarget.Standalone, NamedBuildTarget.Android, NamedBuildTarget.iOS })
            {
                var defines = PlayerSettings.GetScriptingDefineSymbols(target).Split(';').Where(d => d.Length > 0).ToList();
                if (!defines.Contains("PLUGIN_YG_2") || defines.Contains("RU_YG2")) continue;
                defines.Add("RU_YG2");
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
            }
        }

        #endregion
    }
}
