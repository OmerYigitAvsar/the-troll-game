using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TrickRunLevelAuthoring
{
    const string ScenePath = "Assets/Scenes/OYUNLAR/OMER/SinirBozucu/SinirBozucu.unity";
    const string GroundPath = "Assets/Scenes/OYUNLAR/OMER/SinirBozucu/Prefabs/Ground.png";
    const string FallingPath = "Assets/Scenes/OYUNLAR/OMER/SinirBozucu/Prefabs/Gameplay/FallingPlatform.prefab";
    const string SpikePath = "Assets/Scenes/OYUNLAR/OMER/SinirBozucu/Prefabs/Gameplay/RisingSpike.prefab";
    const string StaticSpikePath = "Assets/Scenes/OYUNLAR/OMER/SinirBozucu/Prefabs/Gameplay/Spike.prefab";

    const string CharacterFolder = "Assets/Scenes/OYUNLAR/OMER/SinirBozucu/Art/Character";

    const string PixelFolder = "Assets/Scenes/OYUNLAR/OMER/SinirBozucu/Art/PixelWorld";

    [MenuItem("TrickRun/Configure Android (Current Scene - Safe)")]
    public static void ConfigureAndroidSafe()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Önce Play modundan çık."); return; }
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) throw new System.InvalidOperationException("SinirBozucu sahnesini aç.");
        string folder = System.IO.Path.GetFullPath("Recovery/BeforeAndroid-" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(folder);
        System.IO.File.Copy("ProjectSettings/ProjectSettings.asset", System.IO.Path.Combine(folder, "ProjectSettings.asset"));
        System.IO.File.Copy("ProjectSettings/EditorBuildSettings.asset", System.IO.Path.Combine(folder, "EditorBuildSettings.asset"));
        if (!EditorSceneManager.SaveScene(scene, System.IO.Path.Combine(folder, "Before.unity"), true))
            throw new System.InvalidOperationException("Sahne yedeği alınamadı.");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.Android.renderOutsideSafeArea = false;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        // Preserve signing credentials, application ID and SDK choices.
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        foreach (var entry in EditorBuildSettings.scenes)
            if (entry.path != ScenePath) scenes.Add(new EditorBuildSettingsScene(entry.path, false));
        EditorBuildSettings.scenes = scenes.ToArray();
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Android canvas scaling");
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name != "UI" && root.name != "Main Menu") continue;
            foreach (var scaler in root.GetComponentsInChildren<UnityEngine.UI.CanvasScaler>(true))
            {
                Undo.RecordObject(scaler, "Responsive canvas");
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720);
                scaler.matchWidthOrHeight = 0.5f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(scaler);
            }
        }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("ANDROID_CONFIGURED: Landscape, ARM64/IL2CPP, çentik güvenli alanı, oyun sahnesi ilk sırada. Canvas ayarlarını Ctrl+S ile kaydet. Proje ayarları uygulandı; Undo yalnızca Canvas değişimini geri alır. Proje yedeği: " + folder + "\nBuild Profiles içinde Android seçip varsa profilin özel Scene List listesini de kontrol et. APK/cihaz testi henüz yapılmadı.");
    }

    [MenuItem("TrickRun/Build Main Menu (Current Scene - Safe)")]
    public static void BuildMainMenuSafe()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Play modundan çık."); return; }
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) throw new System.InvalidOperationException("SinirBozucu sahnesini aç.");
        TrickRun_LevelManager manager = null;
        GameObject gameplayUI = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "Main Menu") { Selection.activeGameObject = root; Debug.Log("Main Menu zaten var; yeniden oluşturulmadı."); return; }
            if (root.name == "UI") gameplayUI = root;
            var candidate = root.GetComponentInChildren<TrickRun_LevelManager>(true);
            if (candidate != null) manager = candidate;
        }
        if (manager == null || gameplayUI == null) throw new System.InvalidOperationException("Level Manager veya UI bulunamadı.");
        var managerSo = new SerializedObject(manager);
        var levels = managerSo.FindProperty("levels");
        if (levels.arraySize != 48 || managerSo.FindProperty("player").objectReferenceValue == null)
            throw new System.InvalidOperationException("48 bölüm ve Player bağlantısı gerekiyor.");
        for (int i = 0; i < levels.arraySize; i++)
            if (levels.GetArrayElementAtIndex(i).objectReferenceValue == null) throw new System.InvalidOperationException("Boş bölüm kaydı var.");
        var signatures = new System.Collections.Generic.Dictionary<Transform, string>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) signatures[t] = PaletteTransformSignature(t);
        string folder = System.IO.Path.GetFullPath("Recovery/BeforeMainMenu-" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(folder);
        if (!EditorSceneManager.SaveScene(scene, System.IO.Path.Combine(folder, "Before.unity"), true))
            throw new System.InvalidOperationException("Yedek alınamadı.");
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Main Menu");
        try
        {
            var root = new GameObject("Main Menu", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(root, "Main Menu root");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            root.layer = 5;
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            var home = MenuPanel(root.transform, "Home Panel");
            var groups = MenuPanel(root.transform, "Groups Panel");
            var selection = MenuPanel(root.transform, "Level Selection Panel");
            var reset = MenuPanel(root.transform, "Reset Confirmation Panel");
            var completed = MenuPanel(root.transform, "Completion Panel");
            MenuText(home.transform, "Title", "SİNİR BOZUCU", new Vector2(0, 190), new Vector2(1000, 100), 56);
            var summary = MenuText(home.transform, "Save Summary", "Her deneme yeni bir başlangıç.", new Vector2(0, 110), new Vector2(1000, 50), 22);
            var play = MenuButton(home.transform, "New Game - Continue", "YENİ OYUN", new Vector2(0, 10), new Vector2(390, 76));
            var choose = MenuButton(home.transform, "Level Selection", "BÖLÜM SEÇİMİ", new Vector2(0, -95), new Vector2(390, 76));
            var resetButton = MenuButton(home.transform, "Reset Progress", "SIFIRLA", Vector2.zero, new Vector2(170, 54));
            var resetRect = resetButton.GetComponent<RectTransform>();
            resetRect.anchorMin = resetRect.anchorMax = new Vector2(0, 0); resetRect.pivot = Vector2.zero; resetRect.anchoredPosition = new Vector2(30, 30);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(play.onClick, manager.ContinueFromMenu);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(choose.onClick, manager.ShowGroups);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(resetButton.onClick, manager.ShowResetConfirmation);
            MenuText(groups.transform, "Title", "BÖLÜM GRUPLARI", new Vector2(0, 260), new Vector2(1000, 70), 40);
            var groupButtons = new UnityEngine.UI.Button[10];
            for (int i = 0; i < 10; i++)
            {
                groupButtons[i] = MenuButton(groups.transform, $"Group_{i + 1:00}", (i == 0 ? TrickRun_LevelManager.GetGroupName(i) : "???") + $"\n{i * 5 + 1}–{Mathf.Min(i * 5 + 5, 48)}",
                    new Vector2(-460 + (i % 5) * 230, 95 - (i / 5) * 175), new Vector2(205, 140));
                UnityEditor.Events.UnityEventTools.AddIntPersistentListener(groupButtons[i].onClick, manager.SelectGroup, i);
            }
            var groupsBack = MenuButton(groups.transform, "Back", "GERİ", new Vector2(0, -275), new Vector2(190, 55));
            UnityEditor.Events.UnityEventTools.AddPersistentListener(groupsBack.onClick, manager.ShowMainMenu);
            var groupTitle = MenuText(selection.transform, "Group Title", TrickRun_LevelManager.GetGroupName(0), new Vector2(0, 235), new Vector2(1000, 70), 40);
            var levelButtons = new UnityEngine.UI.Button[5];
            for (int i = 0; i < 5; i++)
            {
                levelButtons[i] = MenuButton(selection.transform, $"Level Slot_{i + 1:00}", $"BÖLÜM {i + 1}",
                    new Vector2(-460 + i * 230, 0), new Vector2(205, 135));
                UnityEditor.Events.UnityEventTools.AddIntPersistentListener(levelButtons[i].onClick, manager.SelectLevelSlot, i);
            }
            var levelsBack = MenuButton(selection.transform, "Back", "GRUPLARA DÖN", new Vector2(0, -245), new Vector2(310, 60));
            UnityEditor.Events.UnityEventTools.AddPersistentListener(levelsBack.onClick, manager.ShowGroups);
            MenuText(reset.transform, "Title", "BAŞTAN BAŞLAYALIM MI?", new Vector2(0, 170), new Vector2(1100, 80), 40);
            MenuText(reset.transform, "Explanation", "Bölüm 1'e dönülecek ve deneme sayısı sıfırlanacak.\nAçtığın bölümler açık kalacak.", new Vector2(0, 45), new Vector2(1080, 120), 27);
            var confirm = MenuButton(reset.transform, "Confirm Reset", "SIFIRLA VE BAŞLA", new Vector2(-215, -120), new Vector2(370, 70));
            var cancel = MenuButton(reset.transform, "Cancel Reset", "VAZGEÇ", new Vector2(215, -120), new Vector2(370, 70));
            UnityEditor.Events.UnityEventTools.AddPersistentListener(confirm.onClick, manager.ConfirmReset);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(cancel.onClick, manager.ShowMainMenu);
            MenuText(completed.transform, "Title", "TEBRİKLER!", new Vector2(0, 140), new Vector2(1000, 100), 56);
            MenuText(completed.transform, "Message", "48 bölümün sonuna geldin.\nAçtığın bütün bölümleri yeniden oynayabilirsin.", new Vector2(0, 10), new Vector2(1080, 100), 26);
            var finish = MenuButton(completed.transform, "Main Menu", "ANA MENÜ", new Vector2(0, -155), new Vector2(350, 70));
            UnityEditor.Events.UnityEventTools.AddPersistentListener(finish.onClick, manager.ShowMainMenu);
            var returnButton = MenuButton(root.transform, "Return To Menu", "MENÜ", Vector2.zero, new Vector2(120, 46));
            var returnRect = returnButton.GetComponent<RectTransform>(); returnRect.anchorMin = returnRect.anchorMax = new Vector2(0, 1);
            returnRect.pivot = new Vector2(0, 1); returnRect.anchoredPosition = new Vector2(24, -24);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(returnButton.onClick, manager.ShowMainMenu);
            Undo.RecordObject(manager, "Menu references");
            managerSo.Update();
            managerSo.FindProperty("mainMenuRoot").objectReferenceValue = root;
            managerSo.FindProperty("homePanel").objectReferenceValue = home;
            managerSo.FindProperty("groupsPanel").objectReferenceValue = groups;
            managerSo.FindProperty("levelSelectionPanel").objectReferenceValue = selection;
            managerSo.FindProperty("resetPanel").objectReferenceValue = reset;
            managerSo.FindProperty("completionPanel").objectReferenceValue = completed;
            managerSo.FindProperty("gameplayUI").objectReferenceValue = gameplayUI;
            managerSo.FindProperty("menuReturnButton").objectReferenceValue = returnButton.gameObject;
            managerSo.FindProperty("continueLabel").objectReferenceValue = play.GetComponentInChildren<UnityEngine.UI.Text>();
            managerSo.FindProperty("saveSummary").objectReferenceValue = summary;
            managerSo.FindProperty("groupTitle").objectReferenceValue = groupTitle;
            var groupArray = managerSo.FindProperty("groupButtons"); groupArray.arraySize = 10;
            for (int i = 0; i < 10; i++) groupArray.GetArrayElementAtIndex(i).objectReferenceValue = groupButtons[i];
            var levelArray = managerSo.FindProperty("levelButtons"); levelArray.arraySize = 5;
            for (int i = 0; i < 5; i++) levelArray.GetArrayElementAtIndex(i).objectReferenceValue = levelButtons[i];
            managerSo.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
            groups.SetActive(false); selection.SetActive(false); reset.SetActive(false); completed.SetActive(false); returnButton.gameObject.SetActive(false);
            foreach (var pair in signatures)
                if (pair.Key == null || PaletteTransformSignature(pair.Key) != pair.Value) throw new System.InvalidOperationException("Mevcut hiyerarşi değişti.");
            Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root;
            Debug.Log("MAIN_MENU_READY: Aynı sahnede Main Menu oluşturuldu. Mevcut hiyerarşi korundu. Ctrl+S kaydet; Ctrl+Z geri al. Yedek: " + folder);
        }
        catch { Undo.RevertAllDownToGroup(group); throw; }
    }

    static GameObject MenuPanel(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        Undo.RegisterCreatedObjectUndo(go, "Menu panel"); go.layer = 5; go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        ColorUtility.TryParseHtmlString("#151B2D", out Color color); go.GetComponent<UnityEngine.UI.Image>().color = color;
        return go;
    }

    static UnityEngine.UI.Text MenuText(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
        Undo.RegisterCreatedObjectUndo(go, "Menu text"); go.layer = 5; go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = size;
        var text = go.GetComponent<UnityEngine.UI.Text>(); text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize; text.fontStyle = FontStyle.Bold; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
        ColorUtility.TryParseHtmlString("#F3EBDD", out Color color); text.color = color;
        return text;
    }

    static UnityEngine.UI.Button MenuButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        Undo.RegisterCreatedObjectUndo(go, "Menu button"); go.layer = 5; go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = size;
        var image = go.GetComponent<UnityEngine.UI.Image>(); ColorUtility.TryParseHtmlString("#F2A541", out Color orange); image.color = orange;
        var button = go.GetComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.disabledColor = new Color(0.34f, 0.34f, 0.38f, 1); colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1); button.colors = colors;
        var text = MenuText(go.transform, "Label", label, Vector2.zero, size - new Vector2(16, 10), 24);
        ColorUtility.TryParseHtmlString("#151B2D", out Color ink); text.color = ink;
        return button;
    }

    [MenuItem("TrickRun/Level 29 - Enable Mild Return Slipperiness")]
    public static void ConfigureLevel29MildSlipperiness()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Önce Play modundan çık."); return; }
        Transform level = Find("Level_029_FakeDoors");
        if (level == null || level.gameObject.scene.path != ScenePath) throw new System.InvalidOperationException("Level 29 bulunamadı.");
        var trigger = level.Find("Doors/Fake Door Trigger")?.GetComponent<TrickRun_TrapTrigger>();
        Transform controllers = level.Find("Traps/Controllers");
        var player = Find("Player")?.GetComponent<TrickRun_PlayerController>();
        if (trigger == null || controllers == null || player == null || player.gameObject.scene != level.gameObject.scene)
            throw new System.InvalidOperationException("Kapı tetikleyicisi, Controllers veya Player bulunamadı.");
        string folder = System.IO.Path.GetFullPath("Recovery/BeforeLevel29Slippery-" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(folder);
        if (!EditorSceneManager.SaveScene(level.gameObject.scene, System.IO.Path.Combine(folder, "Before.unity"), true))
            throw new System.InvalidOperationException("Yedek alınamadı.");
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Level 29 mild return slipperiness");
        Transform existing = controllers.Find("Return Slippery Controller");
        GameObject go;
        if (existing == null)
        {
            go = new GameObject("Return Slippery Controller");
            Undo.RegisterCreatedObjectUndo(go, "Slippery controller"); go.transform.SetParent(controllers, false);
        }
        else go = existing.gameObject;
        var trap = go.GetComponent<TrickRun_SlipperyPlayerTrap>();
        if (trap == null) trap = Undo.AddComponent<TrickRun_SlipperyPlayerTrap>(go);
        Undo.RecordObject(trap, "Mild tuning");
        var so = new SerializedObject(trap);
        so.FindProperty("playerController").objectReferenceValue = player;
        so.FindProperty("enableSlippery").boolValue = true;
        so.FindProperty("effectDuration").floatValue = 0f;
        so.FindProperty("acceleration").floatValue = 40f;
        so.FindProperty("deceleration").floatValue = 22f;
        so.FindProperty("speedMultiplier").floatValue = 1f;
        so.FindProperty("clearOnDisable").boolValue = true;
        so.ApplyModifiedProperties();
        Undo.RecordObject(trigger, "Connect fake door");
        var triggerSo = new SerializedObject(trigger);
        var links = triggerSo.FindProperty("trapActivations");
        bool linked = false;
        for (int i = 0; i < links.arraySize; i++)
            if (links.GetArrayElementAtIndex(i).FindPropertyRelative("trap").objectReferenceValue == trap) linked = true;
        if (!linked)
        {
            int index = links.arraySize; links.arraySize++;
            var link = links.GetArrayElementAtIndex(index);
            link.FindPropertyRelative("trap").objectReferenceValue = trap;
            link.FindPropertyRelative("activationDelay").floatValue = 0f;
            triggerSo.ApplyModifiedProperties();
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(trigger);
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        Selection.activeGameObject = go;
        Debug.Log("Level 29 artırılmış kayganlık: 40 hızlanma / 22 yavaşlama / 1x hız. Sahte kapıya bağlı, level kapanınca sıfırlanır. Ctrl+S kaydet; Ctrl+Z geri al.");
    }

    [MenuItem("TrickRun/Level 29 - Match Stair Platforms to Pair 01")]
    public static void MatchLevel29StairPlatforms()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { Debug.LogWarning("Önce Play modundan çık. Platformlar değiştirilmedi."); return; }
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) throw new System.InvalidOperationException("SinirBozucu sahnesini aç.");
        Transform level = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Level_029_") || t.name.StartsWith("Seviye_029_")) level = t;
        if (level == null) throw new System.InvalidOperationException("Level 29 bulunamadı.");
        var steps = new System.Collections.Generic.Dictionary<string, Transform>();
        foreach (var t in level.GetComponentsInChildren<Transform>(true))
        {
            string key = t.name.Replace(" ", "").ToLowerInvariant();
            if (System.Text.RegularExpressions.Regex.IsMatch(key, @"^stairplatform_0[1-5]_0[12]$"))
            {
                if (steps.ContainsKey(key)) throw new System.InvalidOperationException("Aynı isimde birden fazla basamak var; işlem durduruldu.");
                steps.Add(key, t);
            }
        }
        if (steps.Count != 10) throw new System.InvalidOperationException("10 merdiven platformu bekleniyordu; işlem yapılmadı.");
        Transform first = steps["stairplatform_01_01"], second = steps["stairplatform_01_02"];
        Transform parent = first.parent;
        foreach (var t in steps.Values)
            if (t.parent != parent || t.GetComponent<BoxCollider2D>() == null || t.GetComponent<SpriteRenderer>() == null)
                throw new System.InvalidOperationException("Basamak yapıları farklı; güvenlik için durduruldu.");
        // Existing alternating corridor has 7-unit storeys and mirrored outer steps.
        float mirrorSum = second.localPosition.x + steps["stairplatform_02_02"].localPosition.x;
        var positions = new System.Collections.Generic.Dictionary<Transform, Vector3>();
        for (int pair = 2; pair <= 5; pair++)
            for (int step = 1; step <= 2; step++)
            {
                Transform reference = step == 1 ? first : second;
                Transform target = steps[$"stairplatform_{pair:00}_{step:00}"];
                Vector3 p = target.localPosition;
                p.x = pair % 2 == 0 ? mirrorSum - reference.localPosition.x : reference.localPosition.x;
                p.y = reference.localPosition.y - (pair - 1) * 7f;
                positions.Add(target, p);
            }
        string folder = System.IO.Path.GetFullPath("Recovery/BeforeLevel29Stairs-" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(folder);
        if (!EditorSceneManager.SaveScene(scene, System.IO.Path.Combine(folder, "Before.unity"), true))
            throw new System.InvalidOperationException("Yedek alınamadı; platformlar değiştirilmedi.");
        Undo.IncrementCurrentGroup(); int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Match Level 29 stairs to pair 01");
        foreach (var entry in positions)
        {
            Transform target = entry.Key;
            Transform reference = target.name.EndsWith("_01") ? first : second;
            var collider = target.GetComponent<BoxCollider2D>();
            var referenceCollider = reference.GetComponent<BoxCollider2D>();
            var renderer = target.GetComponent<SpriteRenderer>();
            var referenceRenderer = reference.GetComponent<SpriteRenderer>();
            Undo.RecordObjects(new Object[] { target, collider, renderer }, "Match stair geometry");
            target.localPosition = entry.Value;
            target.localScale = reference.localScale;
            collider.size = referenceCollider.size;
            collider.offset = referenceCollider.offset;
            renderer.size = referenceRenderer.size;
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeTransform = parent;
        Debug.Log("Level 29: 8 basamak referans çifte göre ayarlandı. Referanslar, parent/sıralama, zeminler, tuzaklar ve Player değişmedi. Ctrl+S kaydet; Ctrl+Z geri al. Yedek: " + folder);
    }

    [MenuItem("TrickRun/Hide Background Squares and Pipes (All Levels)")]
    public static void HideBackgroundSquaresAndPipes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Önce Play modundan çık. Sahne değiştirilmedi."); return;
        }
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) throw new System.InvalidOperationException("SinirBozucu sahnesini aç.");
        var targets = new System.Collections.Generic.List<SpriteRenderer>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var r in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Transform parent = r.transform.parent;
                if (parent == null || (parent.name != "Midnight Decor" && parent.name != "Midnight Palette Decor")) continue;
                Transform level = parent.parent;
                if (level == null || !System.Text.RegularExpressions.Regex.IsMatch(level.name, @"^(Level|Seviye)_\d{3}_")) continue;
                if (!System.Text.RegularExpressions.Regex.IsMatch(r.name, @"^(Distant Wall_|Pipe_|Pipe Vertical_|Pipe Horizontal_)\d{2}$")) continue;
                if (r.enabled) targets.Add(r);
            }
        if (targets.Count == 0) { Debug.Log("Kapatılacak arka plan kare/direk dekoru yok."); return; }
        string folder = System.IO.Path.GetFullPath("Recovery/BeforeHideBackdrop-" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(folder);
        if (!EditorSceneManager.SaveScene(scene, System.IO.Path.Combine(folder, "Before.unity"), true))
            throw new System.InvalidOperationException("Yedek alınamadı; işlem yapılmadı.");
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Hide background squares and pipes");
        foreach (var r in targets)
        {
            Undo.RecordObject(r, "Hide decorative silhouette");
            r.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(r);
        }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"{targets.Count} arka plan kare/direk görseli kapatıldı. Hiyerarşi, lacivert fon ve colliderlar değişmedi. Ctrl+S kaydet; Ctrl+Z geri al. Yedek: {folder}");
    }

    [MenuItem("TrickRun/Apply All Levels - Midnight Palette (Safe)")]
    public static void ApplyAllLevelsMidnightSafe()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Play modundan çıkıp tekrar dene. Sahne değiştirilmedi."); return;
        }
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) throw new System.InvalidOperationException("SinirBozucu sahnesini aç.");
        TrickRun_LevelManager manager = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            manager = root.GetComponentInChildren<TrickRun_LevelManager>(true);
            if (manager != null) break;
        }
        if (manager == null) throw new System.InvalidOperationException("Level Manager bulunamadı.");
        var list = new SerializedObject(manager).FindProperty("levels");
        var levels = new System.Collections.Generic.List<Transform>();
        for (int i = 0; i < list.arraySize; i++)
        {
            var go = list.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (go == null || go.scene != scene || levels.Contains(go.transform))
                throw new System.InvalidOperationException("Level listesinde boş veya tekrarlanan kayıt var. İşlem yapılmadı.");
            levels.Add(go.transform);
        }
        if (levels.Count != 48) throw new System.InvalidOperationException("48 level bekleniyordu; güvenlik için durduruldu.");
        Sprite flat = AssetDatabase.LoadAssetAtPath<Sprite>(GroundPath);
        if (flat == null) throw new System.InvalidOperationException("Düz sprite bulunamadı.");
        // Snapshot every existing transform and gameplay component, including Player and managers.
        var transforms = new System.Collections.Generic.Dictionary<Transform, string>();
        var gameplay = new System.Collections.Generic.Dictionary<Component, string>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                transforms[t] = PaletteTransformSignature(t);
                foreach (var c in t.GetComponents<Component>())
                    if (c is Collider2D || c is Rigidbody2D || c is MonoBehaviour)
                        gameplay[c] = EditorJsonUtility.ToJson(c);
            }
        string folder = System.IO.Path.GetFullPath("Recovery/MidnightPalette-" + System.DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(folder);
        string backup = System.IO.Path.Combine(folder, "BeforePalette.unity");
        // Save the LIVE scene as a copy, including unsaved user edits; never replace the open scene externally.
        if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new System.InvalidOperationException("Yedek alınamadı; durduruldu.");
        Undo.IncrementCurrentGroup(); int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("All Levels Midnight Palette");
        int applied = 0, preserved = 0, recolored = 0;
        var unknown = new System.Collections.Generic.List<string>();
        try
        {
            foreach (var level in levels)
            {
                if (level.Find("Midnight Decor") != null || level.Find("Midnight Palette Decor") != null)
                { preserved++; continue; }
                var candidates = level.GetComponentsInChildren<SpriteRenderer>(true);
                Bounds area = new Bounds(); bool hasArea = false;
                foreach (var r in candidates)
                {
                    if (IsOldPreviewArtwork(r.transform, level) || r.sprite == null) continue;
                    string path = AnimationUtility.CalculateTransformPath(r.transform, level).ToLowerInvariant();
                    bool wall = path.Contains("roombounds");
                    if (wall)
                    {
                        Undo.RecordObject(r, "Hide boundary artwork"); r.enabled = false;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(r); continue;
                    }
                    // Do not expose trigger/target debug sprites or change their state.
                    if (path.Contains("trigger") || path.Contains("target") || path.Contains("controller")) continue;
                    bool spike = r.name.ToLowerInvariant().Contains("spike") || path.Contains("spikes/");
                    bool door = path.Contains("door") || path.Contains("exit") || path.Contains("kapı");
                    bool surface = !spike && !door && (path.Contains("ground") || path.Contains("zemin") || path.Contains("platform"));
                    if (!spike && !door && !surface) { unknown.Add(level.name + "/" + path); continue; }
                    Undo.RecordObject(r, "Midnight renderer");
                    ColorUtility.TryParseHtmlString(spike ? "#DCE3ED" : door ? "#49D6BE" : "#45516A", out Color color);
                    color.a = r.color.a; r.color = color;
                    // Same ordering as the accepted Level 1: opaque ground covers buried spikes.
                    if (surface || spike) { r.sortingLayerID = 0; r.sortingOrder = surface ? 20 : 10; }
                    PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                    recolored++;
                    if (!surface || !r.enabled) continue;
                    Bounds b = r.bounds;
                    if (!hasArea) { area = b; hasArea = true; } else area.Encapsulate(b);
                    if (r.transform.Find("Midnight Edge") == null)
                    {
                        var edge = new GameObject("Midnight Edge", typeof(SpriteRenderer));
                        Undo.RegisterCreatedObjectUndo(edge, "Surface edge");
                        edge.transform.SetParent(r.transform, false);
                        Bounds local = r.localBounds;
                        float thickness = Mathf.Min(local.size.y * 0.1f, 0.05f / Mathf.Abs(r.transform.lossyScale.y));
                        edge.transform.localPosition = local.center + Vector3.up * ((local.size.y - thickness) / 2);
                        edge.transform.localScale = new Vector3(local.size.x / flat.bounds.size.x, thickness / flat.bounds.size.y, 1);
                        var er = edge.GetComponent<SpriteRenderer>(); er.sprite = flat; er.sharedMaterial = r.sharedMaterial;
                        er.sortingLayerID = 0; er.sortingOrder = 21;
                        ColorUtility.TryParseHtmlString("#7A89A3", out Color edgeColor); edgeColor.a = r.color.a; er.color = edgeColor;
                    }
                }
                if (hasArea)
                {
                    var decor = new GameObject("Midnight Palette Decor");
                    Undo.RegisterCreatedObjectUndo(decor, "Background decoration"); decor.transform.SetParent(level, false);
                    MeadowBlock(decor.transform, flat, "Navy Background", area.center,
                        new Vector2(area.size.x + 80, area.size.y + 80), "#151B2D", -100);
                }
                applied++;
            }
            // UI already uses the approved palette; keep it and Player completely untouched.
            foreach (var pair in transforms)
                if (pair.Key == null || PaletteTransformSignature(pair.Key) != pair.Value)
                    throw new System.InvalidOperationException("Mevcut hiyerarşi veya transform değişti.");
            foreach (var pair in gameplay)
                if (pair.Key == null || EditorJsonUtility.ToJson(pair.Key) != pair.Value)
                    throw new System.InvalidOperationException("Fizik veya script ayarı değişti.");
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            string report = $"Uygulanan: {applied}; mevcut tema korunan: {preserved}; renklenen görsel: {recolored}.\nHiyerarşi/konum/aktiflik/collider/rigidbody/script kontrolü: BAŞARILI.\nYedek: {backup}\nSınıflandırılamadığı için korunmuş görseller:\n" + string.Join("\n", unknown);
            System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "PaletteReport.txt"), report);
            Debug.Log(report + "\nSahne otomatik kaydedilmedi. Ctrl+S kaydet; Ctrl+Z bütün uygulamayı geri al.");
        }
        catch (System.Exception ex)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogError("Tema işlemi geri alındı: " + ex.Message + "\nYedek: " + backup);
            throw;
        }
    }

    static string PaletteTransformSignature(Transform t)
    {
        return t.name + "|" + (t.parent == null ? 0 : t.parent.GetInstanceID()) + "|" + t.GetSiblingIndex() + "|" +
            t.localPosition.ToString("R") + "|" + t.localRotation.ToString("R") + "|" + t.localScale.ToString("R") + "|" +
            t.gameObject.activeSelf + "|" + t.gameObject.layer + "|" + t.gameObject.tag;
    }

    [MenuItem("TrickRun/Apply Level 1 - Midnight Palette")]
    public static void ApplyLevelOneMidnightPalette()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Önce Play modundan çıkıp renk temasını Edit modunda uygula.");
            return;
        }
        Transform level = Find("Level_001_RisingSpike");
        if (level == null || level.gameObject.scene.path != ScenePath)
            throw new System.InvalidOperationException("Level 1 bulunamadı.");
        if (level.Find("Midnight Decor") != null) { Selection.activeTransform = level; return; }
        Transform grounds = level.Find("Grounds");
        Sprite flat = AssetDatabase.LoadAssetAtPath<Sprite>(GroundPath);
        if (grounds == null || flat == null) throw new System.InvalidOperationException("Grounds veya düz sprite bulunamadı.");
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Level 1 Midnight Palette");
        // Retain the old preview for recovery; disable only known generated artwork.
        foreach (Transform t in level.GetComponentsInChildren<Transform>(true))
            if (t.name == "Pixel World Decor" || t.name == "Soft Meadow Decor" ||
                t.name == "Pixel Soil" || t.name == "Pixel Grass Edge" || t.name == "Pixel Soil Opaque Backing" ||
                t.name == "Pixel Soil Wall" || t.name == "Pixel Metal Spike" || t.name == "Pixel Wood" ||
                t.name.StartsWith("Grass Edge_") || t.name.StartsWith("Soil Detail_"))
            {
                Undo.RecordObject(t.gameObject, "Disable previous generated artwork");
                t.gameObject.SetActive(false);
            }
        var decor = new GameObject("Midnight Decor");
        Undo.RegisterCreatedObjectUndo(decor, "Midnight decoration");
        decor.transform.SetParent(level, false);
        Bounds area = new Bounds(); bool first = true;
        foreach (var r in grounds.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (IsOldPreviewArtwork(r.transform, level) || r.sprite == null) continue;
            Undo.RecordObject(r, "Restore solid ground"); r.enabled = true;
            MeadowTint(r, "#45516A");
            r.sortingLayerID = 0; r.sortingOrder = 20;
            PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            Bounds b = r.bounds;
            if (first) { area = b; first = false; } else area.Encapsulate(b);
            var edge = MeadowBlock(decor.transform, flat, "Ground Highlight", new Vector2(b.center.x, b.max.y - 0.025f),
                new Vector2(b.size.x, 0.05f), "#7A89A3", 21);
            edge.transform.SetParent(r.transform, true);
        }
        if (first) { Undo.RevertAllDownToGroup(group); throw new System.InvalidOperationException("Zemin görseli bulunamadı."); }
        Transform platforms = level.Find("Platforms");
        if (platforms != null)
            foreach (var r in platforms.GetComponentsInChildren<SpriteRenderer>(true))
                if (!IsOldPreviewArtwork(r.transform, level))
                {
                    Undo.RecordObject(r, "Restore platform visual"); r.enabled = true;
                    MeadowTint(r, "#45516A");
                }
        Transform traps = level.Find("Traps");
        if (traps != null)
            foreach (var r in traps.GetComponentsInChildren<SpriteRenderer>(true))
                if (!IsOldPreviewArtwork(r.transform, level) &&
                    r.name.IndexOf("spike", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                    r.name.IndexOf("trigger", System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    Undo.RecordObject(r, "Restore metal spike"); r.enabled = true;
                    r.sortingLayerID = 0; r.sortingOrder = 10;
                    MeadowTint(r, "#DCE3ED");
                }
        Transform walls = level.Find("RoomBounds");
        if (walls != null)
            foreach (var r in walls.GetComponentsInChildren<Renderer>(true))
            {
                Undo.RecordObject(r, "Keep boundary invisible"); r.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
        Transform exit = level.Find("Exit");
        if (exit != null)
            foreach (var r in exit.GetComponentsInChildren<SpriteRenderer>(true)) MeadowTint(r, "#49D6BE");
        MeadowBlock(decor.transform, flat, "Navy Background", new Vector2(area.center.x, area.max.y + 5),
            new Vector2(area.size.x + 60, 50), "#151B2D", -100);
        Transform ui = Find("UI");
        if (ui != null && ui.gameObject.scene == level.gameObject.scene)
        {
            ColorUtility.TryParseHtmlString("#F3EBDD", out Color textColor);
            ColorUtility.TryParseHtmlString("#F2A541", out Color buttonColor);
            foreach (var text in ui.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                Undo.RecordObject(text, "UI text palette"); text.color = textColor;
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }
            foreach (var text in ui.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                Undo.RecordObject(text, "UI text palette"); text.color = textColor;
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }
            foreach (var button in ui.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                if (button.targetGraphic != null)
                {
                    Undo.RecordObject(button.targetGraphic, "Button palette");
                    button.targetGraphic.color = buttonColor;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(button.targetGraphic);
                }
        }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        Selection.activeTransform = level;
        Debug.Log("MIDNIGHT_PALETTE_READY: Level 1 düz renkler; ortak UI paleti. Fizik ve Player değişmedi. Ctrl+S kaydet, Ctrl+Z geri al.");
    }

    static bool IsOldPreviewArtwork(Transform t, Transform level)
    {
        for (; t != null && t != level; t = t.parent)
            if (t.name.StartsWith("Pixel ") || t.name == "Soft Meadow Decor" ||
                t.name.StartsWith("Grass Edge_") || t.name.StartsWith("Soil Detail_") || t.name == "Midnight Decor") return true;
        return false;
    }

    [MenuItem("TrickRun/Preview Level 1 - Pixel World")]
    public static void PreviewLevelOnePixelWorld()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Transform level = Find("Level_001_RisingSpike");
        if (level == null || level.gameObject.scene.path != ScenePath)
            throw new System.InvalidOperationException("SinirBozucu sahnesindeki Level 1 bulunamadı.");
        if (level.Find("Pixel World Decor") != null) { FixLevelOnePixelWorld(); return; }
        Transform grounds = level.Find("Grounds");
        if (grounds == null) throw new System.InvalidOperationException("Grounds bulunamadı.");
        Sprite soil = ImportPixelAsset("Ground", "Soil");
        Sprite grass = AssetDatabase.LoadAllAssetsAtPath(PixelFolder + "/Ground.png") is Object[] assets
            ? System.Array.Find(assets, a => a is Sprite && a.name == "Grass") as Sprite : null;
        Sprite metal = ImportPixelAsset("Spike");
        Sprite wood = ImportPixelAsset("Wood");
        Sprite cloud = ImportPixelAsset("Cloud");
        if (grass == null) throw new System.InvalidOperationException("Grass sprite bulunamadı.");
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Level 1 Pixel World");
        // Remove only decoration created by our previous preview, never level geometry.
        Transform oldDecor = level.Find("Soft Meadow Decor");
        if (oldDecor != null) Undo.DestroyObjectImmediate(oldDecor.gameObject);
        foreach (Transform t in grounds.GetComponentsInChildren<Transform>(true))
            if (t != null && (t.name.StartsWith("Grass Edge_") || t.name.StartsWith("Soil Detail_")))
                Undo.DestroyObjectImmediate(t.gameObject);
        var root = new GameObject("Pixel World Decor");
        Undo.RegisterCreatedObjectUndo(root, "Pixel world decor");
        root.transform.SetParent(level, false);
        var renderers = grounds.GetComponentsInChildren<SpriteRenderer>(true);
        Bounds area = new Bounds(); bool first = true;
        foreach (var r in renderers)
        {
            if (r.sprite == null) continue;
            Bounds b = r.bounds;
            if (first) { area = b; first = false; } else area.Encapsulate(b);
            AddPixelSkin(r, soil, "Pixel Soil", r.localBounds.size, r.localBounds.center, true);
            Vector3 center = r.localBounds.center;
            float height = Mathf.Min(0.16f / Mathf.Abs(r.transform.lossyScale.y), r.localBounds.size.y * 0.12f);
            center.y += (r.localBounds.size.y - height) * 0.5f;
            AddPixelSkin(r, grass, "Pixel Grass Edge", new Vector2(r.localBounds.size.x, height), center, true);
            Undo.RecordObject(r, "Hide original ground visual"); r.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(r);
        }
        if (first) { Undo.RevertAllDownToGroup(group); throw new System.InvalidOperationException("Zemin görseli bulunamadı."); }
        Transform platforms = level.Find("Platforms");
        if (platforms != null)
            foreach (var r in platforms.GetComponentsInChildren<SpriteRenderer>(true))
                if (r.sprite != null) PixelReplaceVisual(r, wood, "Pixel Wood", true);
        Transform traps = level.Find("Traps");
        if (traps != null)
            foreach (var r in traps.GetComponentsInChildren<SpriteRenderer>(true))
                if (r.sprite != null && r.name.IndexOf("spike", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                    r.name.IndexOf("trigger", System.StringComparison.OrdinalIgnoreCase) < 0)
                    PixelReplaceVisual(r, metal, "Pixel Metal Spike", false);
        Transform walls = level.Find("RoomBounds");
        if (walls != null)
            foreach (var r in walls.GetComponentsInChildren<SpriteRenderer>(true))
                if (r.sprite != null) PixelReplaceVisual(r, soil, "Pixel Soil Wall", true);
        Sprite flat = AssetDatabase.LoadAssetAtPath<Sprite>(GroundPath);
        MeadowBlock(root.transform, flat, "Muted Sky", new Vector2(area.center.x, area.max.y + 5),
            new Vector2(area.size.x + 60, 50), "#596B80", -100);
        for (int i = 0; i < 6; i++)
        {
            float width = 4f + (i % 3) * 0.75f;
            SpriteRenderer r = MeadowBlock(root.transform, cloud, $"Pixel Cloud_{i + 1:00}",
                new Vector2(area.min.x + i * (area.size.x / 5f), area.max.y + 4 + (i % 3) * 1.1f),
                Vector2.one, "#B5BDCA", -90);
            r.drawMode = SpriteDrawMode.Simple;
            Vector3 parentScale = root.transform.lossyScale;
            float scale = width / cloud.bounds.size.x;
            r.transform.localScale = new Vector3(scale / parentScale.x, scale / parentScale.y, 1);
            AddCloudAnimation(r.gameObject, i, 4f / parentScale.x);
        }
        AssetDatabase.SaveAssets();
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        Selection.activeTransform = level;
        Debug.Log("PIXEL_WORLD_READY: Level 1 pixel dokular ve animasyonlu bulutlar. Ctrl+Z geri alır; Ctrl+S kaydeder. Colliderlar ve oyun scriptleri değiştirilmedi.");
        FixLevelOnePixelWorld();
    }

    [MenuItem("TrickRun/Fix Level 1 - Pixel World")]
    public static void FixLevelOnePixelWorld()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Önce Play modundan çık. Görsel düzeltmeleri Edit modunda uygula.");
            return;
        }
        Transform level = Find("Level_001_RisingSpike");
        if (level == null || level.gameObject.scene.path != ScenePath)
            throw new System.InvalidOperationException("Level 1 bulunamadı.");
        Transform decor = level.Find("Pixel World Decor");
        if (decor == null) { PreviewLevelOnePixelWorld(); return; }
        Transform grounds = level.Find("Grounds");
        Sprite flat = AssetDatabase.LoadAssetAtPath<Sprite>(GroundPath);
        if (grounds == null || flat == null) throw new System.InvalidOperationException("Zemin veya dolgu sprite bulunamadı.");
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Level 1 pixel appearance corrections");
        Bounds area = new Bounds(); bool found = false;
        foreach (var soil in grounds.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (soil.name != "Pixel Soil" || soil.sprite == null) continue;
            Bounds bounds = soil.bounds;
            if (!found) { area = bounds; found = true; } else area.Encapsulate(bounds);
            // An opaque brown backing prevents transparent texture pixels revealing sky/hazards.
            Transform existing = soil.transform.parent.Find("Pixel Soil Opaque Backing");
            SpriteRenderer backing;
            if (existing == null)
            {
                backing = MeadowBlock(soil.transform.parent, flat, "Pixel Soil Opaque Backing",
                    bounds.center, bounds.size, "#805333", 20);
            }
            else backing = existing.GetComponent<SpriteRenderer>();
            Undo.RecordObject(backing, "Opaque soil backing");
            backing.enabled = true;
            backing.sortingLayerID = 0;
            backing.sortingOrder = 20;
            backing.drawMode = SpriteDrawMode.Simple;
            Undo.RecordObject(backing.transform, "Fit soil backing");
            Vector3 ps = backing.transform.parent.lossyScale;
            backing.transform.localScale = new Vector3(bounds.size.x / flat.bounds.size.x / Mathf.Abs(ps.x),
                bounds.size.y / flat.bounds.size.y / Mathf.Abs(ps.y), 1);
            MeadowTint(backing, "#805333");
            Undo.RecordObject(soil, "Brown soil texture and draw order");
            Undo.RecordObject(soil.transform, "Larger texture repeats");
            // Size repetition in world units, not source PNG pixels (which the importer downsizes).
            float factor = 4f / soil.sprite.bounds.size.x;
            Vector3 parentScale = soil.transform.parent.lossyScale;
            soil.transform.localScale = new Vector3(factor / Mathf.Abs(parentScale.x), factor / Mathf.Abs(parentScale.y), 1);
            soil.size = new Vector2(bounds.size.x / factor, bounds.size.y / factor);
            soil.drawMode = SpriteDrawMode.Tiled;
            soil.tileMode = SpriteTileMode.Continuous;
            soil.sortingLayerID = 0;
            soil.sortingOrder = 21;
            // Keep generated earth detail subtle, backed by solid brown rather than the blue sky.
            soil.color = new Color(1f, 0.78f, 0.55f, 0.65f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(soil);
        }
        if (!found) { Undo.RevertAllDownToGroup(group); throw new System.InvalidOperationException("Pixel Soil bulunamadı."); }
        foreach (var grass in grounds.GetComponentsInChildren<SpriteRenderer>(true))
            if (grass.name == "Pixel Grass Edge")
            {
                Undo.RecordObject(grass, "Grass foreground");
                grass.sortingLayerID = 0; grass.sortingOrder = 22;
            }
        Transform traps = level.Find("Traps");
        if (traps != null)
            foreach (var r in traps.GetComponentsInChildren<SpriteRenderer>(true))
                if (r.name == "Pixel Metal Spike")
                {
                    Undo.RecordObject(r, "Spikes behind opaque ground");
                    r.sortingLayerID = 0; r.sortingOrder = 10;
                }
        Transform walls = level.Find("RoomBounds");
        if (walls != null)
            foreach (var r in walls.GetComponentsInChildren<Renderer>(true))
            {
                Undo.RecordObject(r, "Invisible room boundary");
                r.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
        int cloudIndex = 0;
        foreach (Transform child in decor)
        {
            if (!child.name.StartsWith("Pixel Cloud_")) continue;
            Undo.RecordObject(child.gameObject, "Fewer clouds");
            bool keep = cloudIndex == 0 || cloudIndex == 2 || cloudIndex == 5;
            child.gameObject.SetActive(keep);
            if (keep)
            {
                Undo.RecordObject(child, "Cloud altitude");
                Vector3 p = child.position;
                p.y = area.max.y + (cloudIndex == 2 ? 7f : 5.8f);
                child.position = p;
            }
            cloudIndex++;
        }
        if (decor.Find("Pixel Sky Bands") == null)
        {
            var bands = new GameObject("Pixel Sky Bands");
            Undo.RegisterCreatedObjectUndo(bands, "Banded pixel sky");
            bands.transform.SetParent(decor, false);
            ColorUtility.TryParseHtmlString("#96AEB0", out Color horizon);
            ColorUtility.TryParseHtmlString("#496E9A", out Color upper);
            for (int i = 0; i < 24; i++)
            {
                Color color = Color.Lerp(horizon, upper, i / 23f);
                MeadowBlock(bands.transform, flat, $"Sky Band_{i:00}",
                    new Vector2(area.center.x, area.max.y - 4f + i * 0.75f),
                    new Vector2(area.size.x + 60, 0.76f), "#" + ColorUtility.ToHtmlStringRGB(color), -95);
            }
        }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        Selection.activeTransform = level;
        Debug.Log("LEVEL_1_VISUAL_FIX: Kahverengi opak toprak, 3 bulut, kademeli gökyüzü; dikenlerin gömülü kısmı kapalı, RoomBounds sadece collider. Ctrl+S kaydeder, Ctrl+Z geri alır.");
    }

    static Sprite ImportPixelAsset(string name, string slice = null)
    {
        string path = PixelFolder + "/" + name + ".png";
        AssetDatabase.ImportAsset(path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new System.InvalidOperationException(path + " bulunamadı.");
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = slice == null ? SpriteImportMode.Single : SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = width / 2f;
        importer.spriteBorder = Vector4.zero;
        importer.filterMode = FilterMode.Point;
        importer.maxTextureSize = 64;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        var textureSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(textureSettings);
        textureSettings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(textureSettings);
        if (slice != null)
        {
#pragma warning disable 0618
            importer.spritesheet = new[] {
                new SpriteMetaData { name = "Soil", rect = new Rect(0, 0, width, Mathf.Floor(height * 0.88f)), alignment = 0, pivot = Vector2.one * 0.5f },
                new SpriteMetaData { name = "Grass", rect = new Rect(0, Mathf.Floor(height * 0.92f), width, height - Mathf.Floor(height * 0.92f)), alignment = 0, pivot = Vector2.one * 0.5f }
            };
#pragma warning restore 0618
        }
        importer.SaveAndReimport();
        foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(path))
            if (obj is Sprite sprite && (slice == null || sprite.name == slice)) return sprite;
        throw new System.InvalidOperationException(path + " sprite içe aktarılamadı.");
    }

    static void PixelReplaceVisual(SpriteRenderer source, Sprite sprite, string name, bool tiled)
    {
        AddPixelSkin(source, sprite, name, source.localBounds.size, source.localBounds.center, tiled);
        Undo.RecordObject(source, "Hide original visual");
        source.enabled = false;
        PrefabUtility.RecordPrefabInstancePropertyModifications(source);
    }

    static void AddPixelSkin(SpriteRenderer source, Sprite sprite, string name, Vector2 size, Vector3 center, bool tiled)
    {
        var go = new GameObject(name, typeof(SpriteRenderer));
        Undo.RegisterCreatedObjectUndo(go, "Pixel visual");
        go.transform.SetParent(source.transform, false);
        go.transform.localPosition = center;
        var r = go.GetComponent<SpriteRenderer>();
        r.sprite = sprite; r.color = Color.white;
        r.sortingLayerID = source.sortingLayerID;
        r.sortingOrder = source.sortingOrder + (name == "Pixel Grass Edge" ? 1 : 0);
        r.sharedMaterial = source.sharedMaterial;
        if (tiled)
        {
            Vector3 worldScale = source.transform.lossyScale;
            go.transform.localScale = new Vector3(1 / Mathf.Abs(worldScale.x), 1 / Mathf.Abs(worldScale.y), 1);
            r.drawMode = SpriteDrawMode.Tiled;
            r.tileMode = SpriteTileMode.Continuous;
            r.size = new Vector2(size.x * Mathf.Abs(worldScale.x), size.y * Mathf.Abs(worldScale.y));
        }
        else go.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
    }

    static void AddCloudAnimation(GameObject cloud, int index, float distance)
    {
        string path = PixelFolder + $"/CloudDrift_{index:00}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = $"CloudDrift_{index:00}", frameRate = 12 };
            float x = cloud.transform.localPosition.x;
            float duration = 45 + index * 5;
            var curve = new AnimationCurve(new Keyframe(0, x), new Keyframe(duration / 2, x + distance), new Keyframe(duration, x));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.x"), curve);
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, path);
        }
        string controllerPath = PixelFolder + $"/CloudDrift_{index:00}.controller";
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var state = controller.layers[0].stateMachine.AddState("Slow Drift"); state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
        }
        var animator = Undo.AddComponent<Animator>(cloud); animator.runtimeAnimatorController = controller;
    }

    [MenuItem("TrickRun/Preview Level 1 - Soft Meadow")]
    public static void PreviewLevelOneMeadow()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Transform level = Find("Level_001_RisingSpike");
        if (level == null || level.gameObject.scene.path != ScenePath)
            throw new System.InvalidOperationException("SinirBozucu sahnesinde Level 1 bulunamadı.");
        if (level.Find("Soft Meadow Decor") != null)
        {
            Selection.activeTransform = level;
            return;
        }
        Sprite tile = AssetDatabase.LoadAssetAtPath<Sprite>(GroundPath);
        if (tile == null) throw new System.InvalidOperationException("Ground sprite bulunamadı.");
        Transform grounds = level.Find("Grounds");
        if (grounds == null) throw new System.InvalidOperationException("Grounds bulunamadı.");
        // Only this scene instance is styled. Never apply changes to shared prefabs.
        SpriteRenderer[] surfaces = grounds.GetComponentsInChildren<SpriteRenderer>(true);
        Bounds area = new Bounds();
        bool hasArea = false;
        foreach (var surface in surfaces)
        {
            if (surface.sprite == null) continue;
            if (!hasArea) { area = surface.bounds; hasArea = true; }
            else area.Encapsulate(surface.bounds);
        }
        if (!hasArea) throw new System.InvalidOperationException("Level 1 zemin görselleri bulunamadı.");
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Level 1 Soft Meadow Preview");
        var decor = new GameObject("Soft Meadow Decor");
        Undo.RegisterCreatedObjectUndo(decor, "Create meadow decoration");
        decor.transform.SetParent(level, false);
        // World-space sizing also works when Levels or Level 1 has been moved.
        MeadowBlock(decor.transform, tile, "Matte Sky", new Vector2(area.center.x, area.max.y + 4),
            new Vector2(area.size.x + 60, 50), "#555F73", -100);
        for (int i = 0; i < 9; i++)
        {
            float x = area.min.x - 8 + i * 7;
            float h = 2.4f + (i % 3) * 0.65f;
            // Stepped silhouettes fit the pixel-art character without visual noise.
            MeadowBlock(decor.transform, tile, $"Distant Hill_{i + 1:00}", new Vector2(x, area.max.y + h / 2),
                new Vector2(9, h), "#606F78", -90);
            MeadowBlock(decor.transform, tile, $"Hill Crown_{i + 1:00}", new Vector2(x, area.max.y + h + 0.35f),
                new Vector2(6, 0.7f), "#606F78", -90);
        }
        int index = 0;
        foreach (var surface in surfaces)
        {
            if (surface.sprite == null) continue;
            MeadowTint(surface, "#78675F");
            Bounds b = surface.bounds;
            index++;
            SpriteRenderer lip = MeadowBlock(decor.transform, tile, $"Grass Edge_{index:00}",
                new Vector2(b.center.x, b.max.y - 0.07f), new Vector2(b.size.x, 0.14f), "#8EAA87", surface.sortingOrder + 1);
            lip.sortingLayerID = surface.sortingLayerID;
            // Follow the original surface if a ground object ever moves or is disabled.
            lip.transform.SetParent(surface.transform, true);
            for (int j = 0; b.size.y > 1.1f && j < Mathf.Min(12, Mathf.FloorToInt(b.size.x / 2)); j++)
            {
                var stone = MeadowBlock(decor.transform, tile, $"Soil Detail_{index:00}_{j:00}",
                    new Vector2(b.min.x + 1 + j * 2, b.max.y - 0.45f - (j % 3) * 0.25f),
                    new Vector2(0.18f, 0.07f), "#8B7970", surface.sortingOrder + 1);
                stone.sortingLayerID = surface.sortingLayerID;
                stone.transform.SetParent(surface.transform, true);
            }
        }
        Transform traps = level.Find("Traps");
        if (traps != null)
            foreach (var r in traps.GetComponentsInChildren<SpriteRenderer>(true))
                if (r.name.IndexOf("spike", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                    r.name.IndexOf("trigger", System.StringComparison.OrdinalIgnoreCase) < 0)
                    MeadowTint(r, "#D0C5B8");
        Transform exit = level.Find("Exit");
        if (exit != null)
            foreach (var r in exit.GetComponentsInChildren<SpriteRenderer>(true)) MeadowTint(r, "#C98F7D");
        Transform walls = level.Find("RoomBounds");
        if (walls != null)
            foreach (var r in walls.GetComponentsInChildren<SpriteRenderer>(true)) MeadowTint(r, "#655E60");
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        Selection.activeTransform = level;
        Debug.Log("Level 1 mat çayır önizlemesi uygulandı. Kaydetmek için Ctrl+S; geri almak için Ctrl+Z. Diğer leveller ve colliderlar değiştirilmedi.");
    }

    static void MeadowTint(SpriteRenderer renderer, string hex)
    {
        Undo.RecordObject(renderer, "Meadow color");
        ColorUtility.TryParseHtmlString(hex, out Color color);
        renderer.color = color;
        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
    }

    static SpriteRenderer MeadowBlock(Transform parent, Sprite sprite, string name, Vector2 position, Vector2 size, string hex, int order)
    {
        var go = new GameObject(name, typeof(SpriteRenderer));
        Undo.RegisterCreatedObjectUndo(go, "Meadow decoration");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(position.x, position.y, parent.position.z);
        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = size;
        // Keep dimensions in world units despite scaled level containers.
        Vector3 scale = parent.lossyScale;
        go.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f);
        ColorUtility.TryParseHtmlString(hex, out Color color);
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }

    [MenuItem("TrickRun/Setup Man Character Animation")]
    public static void SetupCharacterAnimation()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Transform player = Find("Player");
        if (player == null || player.gameObject.scene.path != ScenePath)
            throw new System.InvalidOperationException("SinirBozucu sahnesindeki Player bulunamadı.");
        if (player.Find("Character Visual") != null) return;
        string texturePath = CharacterFolder + "/Man.png";
        AssetDatabase.ImportAsset(texturePath);
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.spritePixelsPerUnit = 36f;
        var slices = new SpriteMetaData[10];
        for (int i = 0; i < 10; i++)
            slices[i] = new SpriteMetaData { name = $"Man_{i + 1:00}", rect = new Rect(i * 64, 0, 64, 64), alignment = 9, pivot = new Vector2(0.5f, 0.5f) };
#pragma warning disable 0618
        importer.spritesheet = slices;
#pragma warning restore 0618
        importer.SaveAndReimport();
        var sprites = new System.Collections.Generic.List<Sprite>();
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(texturePath))
            if (asset is Sprite sprite) sprites.Add(sprite);
        sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        if (sprites.Count != 10) throw new System.InvalidOperationException("10 karakter karesi yüklenemedi.");

        AnimationClip walk = MakeCharacterClip("Man_Walk", sprites.GetRange(0, 8).ToArray(), 12f);
        AnimationClip idle = MakeCharacterClip("Man_Idle", new[] { sprites[8] }, 12f);
        AnimationClip jump = MakeCharacterClip("Man_Jump", new[] { sprites[9] }, 12f);
        string controllerPath = CharacterFolder + "/Man.controller";
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            var machine = controller.layers[0].stateMachine;
            var idleState = machine.AddState("Idle"); idleState.motion = idle;
            var walkState = machine.AddState("Walk"); walkState.motion = walk;
            var jumpState = machine.AddState("Jump"); jumpState.motion = jump;
            machine.defaultState = idleState;
            AddCharacterTransition(idleState, walkState, true, true);
            AddCharacterTransition(walkState, idleState, true, false);
            AddCharacterTransition(idleState, jumpState, false, null);
            AddCharacterTransition(walkState, jumpState, false, null);
            AddCharacterTransition(jumpState, idleState, true, false);
            AddCharacterTransition(jumpState, walkState, true, true);
        }

        var visual = new GameObject("Character Visual", typeof(SpriteRenderer), typeof(Animator));
        visual.transform.SetParent(player, false);
        BoxCollider2D bodyCollider = player.GetComponent<BoxCollider2D>();
        visual.transform.localPosition = bodyCollider.offset;
        visual.transform.localScale = Vector3.one * bodyCollider.size.y;
        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        SpriteRenderer oldRenderer = player.GetComponent<SpriteRenderer>();
        renderer.sprite = sprites[8];
        renderer.color = Color.white;
        if (oldRenderer != null)
        {
            renderer.sharedMaterial = oldRenderer.sharedMaterial;
            renderer.sortingLayerID = oldRenderer.sortingLayerID;
            renderer.sortingOrder = oldRenderer.sortingOrder;
            oldRenderer.enabled = false;
        }
        Animator animator = visual.GetComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        SerializedObject playerSo = new SerializedObject(player.GetComponent<TrickRun_PlayerController>());
        playerSo.FindProperty("characterAnimator").objectReferenceValue = animator;
        playerSo.FindProperty("characterRenderer").objectReferenceValue = renderer;
        playerSo.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        EditorSceneManager.SaveScene(player.gameObject.scene);
        Debug.Log("MAN_CHARACTER_OK: 8 walk + idle + jump, Player physics unchanged.");
    }

    static AnimationClip MakeCharacterClip(string name, Sprite[] frames, float fps)
    {
        string path = CharacterFolder + "/" + name + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null) return clip;
        clip = new AnimationClip { name = name, frameRate = fps };
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[0] };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static void AddCharacterTransition(UnityEditor.Animations.AnimatorState from, UnityEditor.Animations.AnimatorState to, bool grounded, bool? moving)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.AddCondition(grounded ? UnityEditor.Animations.AnimatorConditionMode.If : UnityEditor.Animations.AnimatorConditionMode.IfNot, 0f, "Grounded");
        if (moving.HasValue)
            transition.AddCondition(moving.Value ? UnityEditor.Animations.AnimatorConditionMode.Greater : UnityEditor.Animations.AnimatorConditionMode.Less, 0.1f, "Speed");
    }

    [InitializeOnLoadMethod]
    static void ScheduleAttemptCounter()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Transform ui = Find("UI");
            if (ui == null || ui.Find("Attempt Counter") != null) return;
            BuildAttemptCounter();
        };
    }

    [MenuItem("TrickRun/Build Attempt Counter")]
    public static void BuildAttemptCounter()
    {
        Transform ui = Find("UI");
        TrickRun_PlayerDeath playerDeath = Object.FindFirstObjectByType<TrickRun_PlayerDeath>(FindObjectsInactive.Include);
        if (ui == null || playerDeath == null)
            throw new System.InvalidOperationException("UI veya Player Death bulunamadı.");

        GameObject counterObject = new GameObject("Attempt Counter", typeof(RectTransform));
        counterObject.layer = 5;
        RectTransform counterRect = counterObject.GetComponent<RectTransform>();
        counterRect.SetParent(ui, false);
        counterRect.anchorMin = new Vector2(1f, 1f);
        counterRect.anchorMax = new Vector2(1f, 1f);
        counterRect.pivot = new Vector2(1f, 1f);
        counterRect.anchoredPosition = new Vector2(-24f, -24f);
        counterRect.sizeDelta = new Vector2(260f, 60f);

        GameObject textObject = new GameObject("Attempt Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
        textObject.layer = 5;
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(counterRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        UnityEngine.UI.Text text = textObject.GetComponent<UnityEngine.UI.Text>();
        UnityEngine.UI.Text levelText = Find("Level Counter")?.GetComponentInChildren<UnityEngine.UI.Text>(true);
        text.font = levelText != null ? levelText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = "DENEME: 0";
        text.fontSize = 30;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.UpperRight;
        text.color = Color.white;
        text.raycastTarget = false;

        SerializedObject deathSo = new SerializedObject(playerDeath);
        deathSo.FindProperty("attemptCounterText").objectReferenceValue = text;
        deathSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(ui.gameObject);
        EditorUtility.SetDirty(playerDeath);
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        EditorSceneManager.SaveScene(ui.gameObject.scene);
        Debug.Log("ATTEMPT_COUNTER_OK: Sağ üst deneme sayacı eklendi.");
    }

    [InitializeOnLoadMethod]
    static void ScheduleLevel40ConcretePlatformShift()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Transform level = Find("Level_040_NoJump");
            if (level == null) return;
            Transform existing = level.Find("Mechanics/Controllers/Platform Shift Controller");
            if (existing != null && existing.GetComponent<TrickRun_PlatformShift>() != null) return;
            BuildLevel40PlatformShift();
        };
    }

    [InitializeOnLoadMethod]
    static void ScheduleLevel40JumpEnableTriggers()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Transform level = Find("Level_040_NoJump");
            if (level == null) return;
            Transform triggers = level.Find("Mechanics/Triggers");
            if (triggers == null) return;
            if (triggers.Find("Jump Enable_Left") != null && triggers.Find("Jump Enable_Right") != null) return;
            BuildLevel40JumpEnableTriggers();
        };
    }

    [InitializeOnLoadMethod]
    static void ScheduleLevel45NormalGravityTrigger()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Transform level = Find("Level_045_Gravity");
            if (level == null) return;
            if (level.Find("Mechanics/Triggers/Normal Gravity Trigger_01") != null) return;
            BuildLevel45NormalGravityTrigger();
        };
    }

    [InitializeOnLoadMethod]
    static void ScheduleLevel47DisappearingGroundCleanup()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Transform level = Find("Level_047_FinalChallenge");
            Transform group = level != null ? level.Find("Grounds/Disappearing Grounds") : null;
            if (group == null || CountExactDuplicateChildren(group) == 0) return;
            CleanupLevel47DisappearingGroundDuplicates();
        };
    }

    [InitializeOnLoadMethod]
    static void ScheduleLevel29PlayerChaseGatePatch()
    {
        EditorApplication.delayCall += ApplyLevel29PlayerChaseGatePatch;
    }

    [MenuItem("TrickRun/Lock Level 29 Chase Triggers Until Fake Door")]
    public static void ApplyLevel29PlayerChaseGatePatch()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Transform level = Find("Level_029_FakeDoors");
        if (level == null || level.Find("Traps/Triggers/Level29_PlayerChaseGatePatch_v1") != null) return;

        Transform playerTriggerGroup = level.Find("Traps/Triggers/Player Chase Triggers");
        Transform controllers = level.Find("Traps/Controllers/Chase Controllers");
        Transform fakeDoor = level.Find("Doors/Fake Door Trigger");
        Transform triggerRoot = level.Find("Traps/Triggers");
        if (playerTriggerGroup == null || controllers == null || fakeDoor == null || triggerRoot == null) return;

        TrickRun_TrapTrigger[] playerTriggers = new TrickRun_TrapTrigger[5];
        for (int i = 0; i < playerTriggers.Length; i++)
        {
            Transform triggerTransform = playerTriggerGroup.Find($"Player Chase Trigger_{i + 1:00}");
            if (triggerTransform == null) return;
            playerTriggers[i] = triggerTransform.GetComponent<TrickRun_TrapTrigger>();
            if (playerTriggers[i] == null) return;

            SerializedObject triggerSo = new SerializedObject(playerTriggers[i]);
            triggerSo.FindProperty("startsLocked").boolValue = true;
            triggerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        TrickRun_SetTriggerLockTrap unlockTrap = Controller<TrickRun_SetTriggerLockTrap>(
            "Player Chase Trigger Unlock Controller", controllers);
        SerializedObject unlockSo = new SerializedObject(unlockTrap);
        SerializedProperty targets = unlockSo.FindProperty("targetTriggers");
        targets.arraySize = playerTriggers.Length;
        for (int i = 0; i < playerTriggers.Length; i++)
            targets.GetArrayElementAtIndex(i).objectReferenceValue = playerTriggers[i];
        unlockSo.FindProperty("lockTriggers").boolValue = false;
        unlockSo.ApplyModifiedPropertiesWithoutUndo();

        TrickRun_TrapTrigger fakeDoorTrigger = fakeDoor.GetComponent<TrickRun_TrapTrigger>();
        if (fakeDoorTrigger == null) return;
        SerializedObject fakeDoorSo = new SerializedObject(fakeDoorTrigger);
        SerializedProperty activations = fakeDoorSo.FindProperty("trapActivations");
        int oldSize = activations.arraySize;
        activations.arraySize = oldSize + 1;
        SerializedProperty unlockActivation = activations.GetArrayElementAtIndex(oldSize);
        unlockActivation.FindPropertyRelative("trap").objectReferenceValue = unlockTrap;
        unlockActivation.FindPropertyRelative("activationDelay").floatValue = 0f;
        fakeDoorSo.ApplyModifiedPropertiesWithoutUndo();

        Transform marker = Group("Level29_PlayerChaseGatePatch_v1", triggerRoot);
        marker.gameObject.hideFlags = HideFlags.HideInHierarchy;
        EditorUtility.SetDirty(level.gameObject);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        EditorSceneManager.SaveScene(level.gameObject.scene);
        Debug.Log("LEVEL29_CHASE_GATE_OK: Merdiven triggerlari sahte kapiya kadar kilitlendi.");
    }

    [InitializeOnLoadMethod]
    static void ScheduleLevel29PlayerChasePatch()
    {
        EditorApplication.delayCall += ApplyLevel29PlayerChasePatch;
        EditorApplication.playModeStateChanged += ApplyLevel29PlayerChaseAfterPlayMode;
    }

    static void ApplyLevel29PlayerChaseAfterPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.playModeStateChanged -= ApplyLevel29PlayerChaseAfterPlayMode;
        ApplyLevel29PlayerChasePatch();
    }

    [MenuItem("TrickRun/Apply Level 29 Player Chase Triggers")]
    public static void ApplyLevel29PlayerChasePatch()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Transform level = Find("Level_029_FakeDoors");
        if (level == null) return;
        if (level.Find("Traps/Triggers/Level29_PlayerChasePatch_v1") != null) return;

        Transform chaseWalls = level.Find("Traps/Chasing Spike Walls");
        Transform chaseControllers = level.Find("Traps/Controllers/Chase Controllers");
        Transform triggers = level.Find("Traps/Triggers");
        Transform relayTriggers = level.Find("Traps/Triggers/Wall Relay Triggers");
        GameObject spikePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StaticSpikePath);
        if (chaseWalls == null || chaseControllers == null || triggers == null || spikePrefab == null) return;

        string[] wallNames =
        {
            "Door Chase Spike Wall",
            "Chasing Spike Wall_01",
            "Chasing Spike Wall_02",
            "Chasing Spike Wall_03",
            "Chasing Spike Wall_04",
            "Chasing Spike Wall_05"
        };

        // Yalnizca takip duvarlarinin kendi dikenlerini dokuzlu, duzenli bir duvara cevirir.
        foreach (string wallName in wallNames)
        {
            Transform wall = chaseWalls.Find(wallName);
            if (wall == null) continue;
            System.Collections.Generic.List<Transform> wallSpikes = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < wall.childCount; i++)
            {
                Transform child = wall.GetChild(i);
                if (child.name.Contains("Spike")) wallSpikes.Add(child);
            }
            Quaternion wallSpikeRotation = wallSpikes.Count > 0
                ? wallSpikes[0].localRotation
                : Quaternion.identity;
            while (wallSpikes.Count < 9)
            {
                GameObject spike = (GameObject)PrefabUtility.InstantiatePrefab(spikePrefab, wall);
                wallSpikes.Add(spike.transform);
            }
            for (int i = 0; i < wallSpikes.Count; i++)
            {
                wallSpikes[i].name = $"Wall Spike_{i + 1:00}";
                wallSpikes[i].localPosition = new Vector3(0f, -2.88f + i * 0.72f, 0f);
                wallSpikes[i].localRotation = wallSpikeRotation;
            }
        }

        // Eski duvardan-duvara fiziksel zinciri saklar fakat devre disi birakir.
        if (relayTriggers != null)
        {
            for (int i = 0; i < relayTriggers.childCount; i++)
            {
                Transform relay = relayTriggers.GetChild(i);
                if (relay.name.StartsWith("Wall Relay Trigger_") || relay.name == "Door Wall Relay Trigger")
                    relay.gameObject.SetActive(false);
            }
        }

        Transform playerTriggers = triggers.Find("Player Chase Triggers") ?? Group("Player Chase Triggers", triggers);
        string[] controllerNames =
        {
            "Chasing Wall Controller_01",
            "Chasing Wall Controller_02",
            "Chasing Wall Controller_03",
            "Chasing Wall Controller_04",
            "Chasing Wall Controller_05"
        };
        Vector2[] triggerPositions =
        {
            new Vector2(28f, -29.6f),
            new Vector2(2f, -22.6f),
            new Vector2(28f, -15.6f),
            new Vector2(2f, -8.6f),
            new Vector2(28f, -1.6f)
        };

        for (int i = 0; i < controllerNames.Length; i++)
        {
            TrickRun_MovingObjectTrap wallTrap = chaseControllers.Find(controllerNames[i])?.GetComponent<TrickRun_MovingObjectTrap>();
            if (wallTrap == null) continue;
            string triggerName = $"Player Chase Trigger_{i + 1:00}";
            Transform triggerTransform = playerTriggers.Find(triggerName);
            if (triggerTransform == null) triggerTransform = Group(triggerName, playerTriggers);
            triggerTransform.localPosition = new Vector3(triggerPositions[i].x, triggerPositions[i].y, 0f);
            BoxCollider2D collider = triggerTransform.GetComponent<BoxCollider2D>();
            if (collider == null) collider = triggerTransform.gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(4f, 0.6f);
            TrickRun_TrapTrigger trigger = triggerTransform.GetComponent<TrickRun_TrapTrigger>();
            if (trigger == null) trigger = triggerTransform.gameObject.AddComponent<TrickRun_TrapTrigger>();
            ConfigureTrigger(trigger, new TrickRun_TrapBase[] { wallTrap }, new float[] { 0f });
        }

        Transform patchMarker = Group("Level29_PlayerChasePatch_v1", triggers);
        patchMarker.gameObject.hideFlags = HideFlags.HideInHierarchy;

        EditorUtility.SetDirty(level.gameObject);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        EditorSceneManager.SaveScene(level.gameObject.scene);
        Debug.Log("LEVEL29_PLAYER_CHASE_PATCH_OK: Takip duvarlari oyuncunun yatay merdiven cikis triggerlarina baglandi.");
    }

    [MenuItem("TrickRun/Fix Level 29 Rising Spike Trigger Names")]
    public static void RenameLevel29RisingSpikeTriggers()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Transform level = Find("Level_029_FakeDoors");
        Transform descentTriggers = level != null
            ? level.Find("Traps/Triggers/Descent Triggers")
            : null;
        if (descentTriggers == null) return;

        System.Collections.Generic.List<Transform> risingTriggers =
            new System.Collections.Generic.List<Transform>();
        for (int i = 0; i < descentTriggers.childCount; i++)
        {
            Transform child = descentTriggers.GetChild(i);
            bool isRisingTrigger = child.name.StartsWith("Rising Spike Trigger_") ||
                                   child.name.StartsWith("Rising Spike Extra Trigger_") ||
                                   child.name.StartsWith("RisingSpikeTriggerExtra");
            if (isRisingTrigger && !child.name.Contains("Moving"))
                risingTriggers.Add(child);
        }

        if (risingTriggers.Count == 0) return;
        for (int i = 0; i < risingTriggers.Count; i++)
            risingTriggers[i].name = $"Rising Spike Trigger_{i + 1:00}";

        EditorUtility.SetDirty(level.gameObject);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        EditorSceneManager.SaveScene(level.gameObject.scene);
        Debug.Log($"LEVEL29_TRIGGER_RENAME_OK: {risingTriggers.Count} yükselen diken triggerı sıralı adlandırıldı.");
    }

    [InitializeOnLoadMethod]
    static void ScheduleLevel29DoorWallPatch()
    {
        EditorApplication.delayCall += ApplyLevel29DoorWallPatch;
        EditorApplication.playModeStateChanged += ApplyLevel29DoorWallAfterPlayMode;
    }

    static void ApplyLevel29DoorWallAfterPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.playModeStateChanged -= ApplyLevel29DoorWallAfterPlayMode;
        ApplyLevel29DoorWallPatch();
    }

    static void ApplyLevel29DoorWallPatch()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Transform level = Find("Level_029_FakeDoors");
        if (level == null || level.Find("Traps/Chasing Spike Walls/Door Chase Spike Wall") != null) return;

        Transform doors = level.Find("Doors");
        Transform chaseWalls = level.Find("Traps/Chasing Spike Walls");
        Transform chaseControllers = level.Find("Traps/Controllers/Chase Controllers");
        Transform relayTriggers = level.Find("Traps/Triggers/Wall Relay Triggers");
        if (doors == null || chaseWalls == null || chaseControllers == null || relayTriggers == null) return;

        Transform escapingDoor = doors.Find("Escaping Door");
        Transform fakeDoorTrigger = doors.Find("Fake Door Trigger");
        TrickRun_MovingObjectTrap firstExistingWall = chaseControllers.Find("Chasing Wall Controller_01")?.GetComponent<TrickRun_MovingObjectTrap>();
        GameObject staticSpikePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StaticSpikePath);
        if (escapingDoor == null || fakeDoorTrigger == null || firstExistingWall == null || staticSpikePrefab == null) return;

        // Oyuncunun dokunma alani, kullanicinin elle tasidigi kapinin yeni konumunu izler.
        fakeDoorTrigger.localPosition = escapingDoor.localPosition;

        Transform wall = Group("Door Chase Spike Wall", chaseWalls);
        wall.localPosition = new Vector3(-2.2f, -35.75f, 0f);
        Rigidbody2D wallBody = wall.gameObject.AddComponent<Rigidbody2D>();
        wallBody.bodyType = RigidbodyType2D.Kinematic;
        wallBody.gravityScale = 0f;
        for (int i = 0; i < 6; i++)
        {
            GameObject spike = (GameObject)PrefabUtility.InstantiatePrefab(staticSpikePrefab, wall);
            spike.name = $"Door Wall Spike_{i + 1:00}";
            spike.transform.localPosition = new Vector3(0f, -1.8f + i * 0.72f, 0f);
            spike.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        }

        Transform target = Group("Door Chase Wall Target", chaseWalls);
        target.localPosition = new Vector3(32.2f, -35.75f, 0f);
        TrickRun_MovingObjectTrap doorWallTrap = Controller<TrickRun_MovingObjectTrap>("Door Chase Wall Controller", chaseControllers);
        SetObject(doorWallTrap, "movingObject", wall);
        SetObject(doorWallTrap, "targetPoint", target);
        SetFloat(doorWallTrap, "movementSpeed", 5.2f);

        Transform relay = Group("Door Wall Relay Trigger", relayTriggers);
        relay.localPosition = target.localPosition;
        BoxCollider2D relayCollider = relay.gameObject.AddComponent<BoxCollider2D>();
        relayCollider.isTrigger = true;
        relayCollider.size = new Vector2(1f, 5.5f);
        TrickRun_WallTrapRelayTrigger relayTrigger = relay.gameObject.AddComponent<TrickRun_WallTrapRelayTrigger>();
        SetObject(relayTrigger, "activatingWall", wall);
        SetObject(relayTrigger, "nextWallTrap", firstExistingWall);

        TrickRun_TrapTrigger doorTrigger = fakeDoorTrigger.GetComponent<TrickRun_TrapTrigger>();
        if (doorTrigger != null)
        {
            SerializedObject so = new SerializedObject(doorTrigger);
            SerializedProperty activations = so.FindProperty("trapActivations");
            if (activations.arraySize >= 2)
                activations.GetArrayElementAtIndex(1).FindPropertyRelative("trap").objectReferenceValue = doorWallTrap;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(level.gameObject);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        EditorSceneManager.SaveScene(level.gameObject.scene);
        Debug.Log("LEVEL29_DOOR_WALL_PATCH_OK: Kapinin solundaki ilk diken duvari zincire eklendi.");
    }

    [MenuItem("TrickRun/Rebuild Level 27 Concrete Hierarchy")]
    public static void RebuildFromMenu() => Rebuild();

    public static void RebuildFromCommandLine()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Rebuild();
        EditorApplication.Exit(0);
    }

    [MenuItem("TrickRun/Rebuild Level 28 Two Rooms")]
    public static void RebuildLevel28FromMenu() => RebuildLevel28();

    public static void RebuildLevel28FromCommandLine()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        RebuildLevel28();
        EditorApplication.Exit(0);
    }

    [MenuItem("TrickRun/Rebuild Level 29 Vertical Chase")]
    public static void RebuildLevel29FromMenu() => RebuildLevel29();

    public static void RebuildLevel29FromCommandLine()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        RebuildLevel29();
        EditorApplication.Exit(0);
    }

    [MenuItem("TrickRun/Build Level 40 Concrete Platform Shift")]
    public static void BuildLevel40PlatformShiftFromMenu() => BuildLevel40PlatformShift();

    public static void BuildLevel40PlatformShiftFromCommandLine()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        BuildLevel40PlatformShift();
        EditorApplication.Exit(0);
    }

    static void BuildLevel40PlatformShift()
    {
        Transform level = Find("Level_040_NoJump");
        if (level == null) throw new System.InvalidOperationException("Level_040_NoJump bulunamadı.");

        Transform controllers = level.Find("Mechanics/Controllers");
        Transform triggers = level.Find("Mechanics/Triggers");
        if (controllers == null || triggers == null)
            throw new System.InvalidOperationException("Level 40 Mechanics hiyerarşisi eksik.");

        Transform platform02 = FindChildByName(level, "StaticPlatform_02");
        Transform platform03 = FindChildByName(level, "StaticPlatform_03");
        Transform platform08 = FindChildByName(level, "StaticPlatform_08");
        Transform rightWall = FindChildByName(level, "RightWall");
        if (platform02 == null || platform03 == null || platform08 == null || rightWall == null)
            throw new System.InvalidOperationException("Level 40 platform veya RightWall bağlantısı eksik.");

        Transform controllerObject = Group("Platform Shift Controller", controllers);
        TrickRun_PlatformShift controller = controllerObject.GetComponent<TrickRun_PlatformShift>();
        if (controller == null) controller = controllerObject.gameObject.AddComponent<TrickRun_PlatformShift>();

        Collider2D platform02Collider = platform02.GetComponent<Collider2D>();
        Collider2D platform08Collider = platform08.GetComponent<Collider2D>();
        Collider2D rightWallCollider = rightWall.GetComponent<Collider2D>();
        if (platform02Collider == null || platform08Collider == null || rightWallCollider == null)
            throw new System.InvalidOperationException("Level 40 gerekli collider bağlantıları eksik.");

        float triggerY = Mathf.Max(platform02Collider.bounds.max.y, platform08Collider.bounds.max.y) + 1.25f;
        TrickRun_ShiftTrigger leftTrigger = MakeLevel40ShiftTrigger(
            "Platform Shift Trigger_Left", triggers, platform02Collider.bounds.max.x - 0.45f, triggerY, controller);
        TrickRun_ShiftTrigger rightTrigger = MakeLevel40ShiftTrigger(
            "Platform Shift Trigger_Right", triggers, platform08Collider.bounds.min.x + 0.45f, triggerY, controller);

        SerializedObject controllerSo = new SerializedObject(controller);
        controllerSo.FindProperty("platform02").objectReferenceValue = platform02;
        controllerSo.FindProperty("platform03").objectReferenceValue = platform03;
        controllerSo.FindProperty("platform08").objectReferenceValue = platform08;
        controllerSo.FindProperty("rightWallCollider").objectReferenceValue = rightWallCollider;
        controllerSo.FindProperty("leftTrigger").objectReferenceValue = leftTrigger;
        controllerSo.FindProperty("rightTrigger").objectReferenceValue = rightTrigger;
        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(level.gameObject);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        EditorSceneManager.SaveScene(level.gameObject.scene);
        Debug.Log("LEVEL40_PLATFORM_SHIFT_OK: Controller ve iki trigger somut hiyerarşiye bağlandı.");
    }

    [MenuItem("TrickRun/Build Level 40 Door Jump Enable Triggers")]
    public static void BuildLevel40JumpEnableTriggers()
    {
        Transform level = Find("Level_040_NoJump");
        if (level == null) throw new System.InvalidOperationException("Level_040_NoJump bulunamadı.");

        Transform triggers = level.Find("Mechanics/Triggers");
        Transform jumpControllerObject = level.Find("Mechanics/Controllers/Jump Control_01");
        Transform exit = level.Find("Exit");
        if (triggers == null || jumpControllerObject == null || exit == null)
            throw new System.InvalidOperationException("Level 40 trigger, Jump Control_01 veya Exit bağlantısı eksik.");

        TrickRun_DisableJumpTrap jumpTrap = jumpControllerObject.GetComponent<TrickRun_DisableJumpTrap>();
        Collider2D exitCollider = exit.GetComponentInChildren<Collider2D>(true);
        if (jumpTrap == null || exitCollider == null)
            throw new System.InvalidOperationException("Level 40 Jump Control veya Exit collider bağlantısı eksik.");

        float triggerHalfWidth = 0.3f;
        float triggerHeight = Mathf.Max(3f, exitCollider.bounds.size.y);
        float triggerY = exitCollider.bounds.center.y;
        MakeLevel40JumpBoundary("Jump Enable_Left", triggers,
            exitCollider.bounds.min.x - triggerHalfWidth, triggerY, triggerHeight, jumpTrap);
        MakeLevel40JumpBoundary("Jump Enable_Right", triggers,
            exitCollider.bounds.max.x + triggerHalfWidth, triggerY, triggerHeight, jumpTrap);

        EditorUtility.SetDirty(level.gameObject);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        EditorSceneManager.SaveScene(level.gameObject.scene);
        Debug.Log("LEVEL40_JUMP_ENABLE_OK: Kapının sağına ve soluna zıplama açma triggerları eklendi.");
    }

    static void MakeLevel40JumpBoundary(string name, Transform parent, float worldX, float worldY,
        float height, TrickRun_DisableJumpTrap jumpTrap)
    {
        Transform boundaryTransform = parent.Find(name) ?? Group(name, parent);
        boundaryTransform.position = new Vector3(worldX, worldY, 0f);
        BoxCollider2D collider = boundaryTransform.GetComponent<BoxCollider2D>();
        if (collider == null) collider = boundaryTransform.gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.6f, height);

        TrickRun_JumpControlBoundary boundary = boundaryTransform.GetComponent<TrickRun_JumpControlBoundary>();
        if (boundary == null) boundary = boundaryTransform.gameObject.AddComponent<TrickRun_JumpControlBoundary>();
        SerializedObject boundarySo = new SerializedObject(boundary);
        boundarySo.FindProperty("jumpTrap").objectReferenceValue = jumpTrap;
        boundarySo.FindProperty("enableJumpOnContact").boolValue = true;
        boundarySo.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("TrickRun/Build Level 45 Normal Gravity Trigger")]
    public static void BuildLevel45NormalGravityTrigger()
    {
        Transform level = Find("Level_045_Gravity");
        if (level == null) throw new System.InvalidOperationException("Level_045_Gravity bulunamadı.");

        Transform mechanics = level.Find("Mechanics") ?? Group("Mechanics", level);
        Transform controllers = mechanics.Find("Controllers") ?? Group("Controllers", mechanics);
        Transform triggers = mechanics.Find("Triggers") ?? Group("Triggers", mechanics);

        TrickRun_PlayerController player = Object.FindFirstObjectByType<TrickRun_PlayerController>(FindObjectsInactive.Include);
        if (player == null) throw new System.InvalidOperationException("Player Controller bulunamadı.");

        Transform controllerTransform = controllers.Find("Normal Gravity Controller_01") ??
                                        Group("Normal Gravity Controller_01", controllers);
        TrickRun_GravityChangeTrap gravityTrap = controllerTransform.GetComponent<TrickRun_GravityChangeTrap>();
        if (gravityTrap == null) gravityTrap = controllerTransform.gameObject.AddComponent<TrickRun_GravityChangeTrap>();
        SerializedObject gravitySo = new SerializedObject(gravityTrap);
        gravitySo.FindProperty("playerController").objectReferenceValue = player;
        gravitySo.FindProperty("gravityMultiplier").floatValue = 1f;
        gravitySo.FindProperty("effectDuration").floatValue = 0f;
        gravitySo.ApplyModifiedPropertiesWithoutUndo();

        Transform triggerTransform = triggers.Find("Normal Gravity Trigger_01") ??
                                     Group("Normal Gravity Trigger_01", triggers);
        triggerTransform.localPosition = Vector3.zero;
        BoxCollider2D collider = triggerTransform.GetComponent<BoxCollider2D>();
        if (collider == null) collider = triggerTransform.gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1f, 3f);
        TrickRun_TrapTrigger trigger = triggerTransform.GetComponent<TrickRun_TrapTrigger>();
        if (trigger == null) trigger = triggerTransform.gameObject.AddComponent<TrickRun_TrapTrigger>();
        ConfigureTrigger(trigger, new TrickRun_TrapBase[] { gravityTrap }, new float[] { 0f });

        EditorUtility.SetDirty(level.gameObject);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        EditorSceneManager.SaveScene(level.gameObject.scene);
        Debug.Log("LEVEL45_NORMAL_GRAVITY_OK: Taşınmaya hazır normal gravity triggerı eklendi.");
    }

    [MenuItem("TrickRun/Cleanup Level 47 Disappearing Ground Duplicates")]
    public static void CleanupLevel47DisappearingGroundDuplicates()
    {
        Transform level = Find("Level_047_FinalChallenge");
        Transform group = level != null ? level.Find("Grounds/Disappearing Grounds") : null;
        if (group == null)
            throw new System.InvalidOperationException("Level 47 Disappearing Grounds grubu bulunamadı.");

        var seen = new System.Collections.Generic.HashSet<string>();
        var duplicates = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < group.childCount; i++)
        {
            Transform child = group.GetChild(i);
            string key = Level47DuplicateKey(child);
            if (!seen.Add(key)) duplicates.Add(child.gameObject);
        }

        foreach (GameObject duplicate in duplicates)
            Object.DestroyImmediate(duplicate);

        if (duplicates.Count == 0) return;
        EditorUtility.SetDirty(level.gameObject);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        EditorSceneManager.SaveScene(level.gameObject.scene);
        Debug.Log($"LEVEL47_DUPLICATE_CLEANUP_OK: {duplicates.Count} birebir kopya kaldırıldı, {group.childCount} özgün nesne korundu.");
    }

    static int CountExactDuplicateChildren(Transform group)
    {
        var seen = new System.Collections.Generic.HashSet<string>();
        int duplicateCount = 0;
        for (int i = 0; i < group.childCount; i++)
            if (!seen.Add(Level47DuplicateKey(group.GetChild(i)))) duplicateCount++;
        return duplicateCount;
    }

    static string Level47DuplicateKey(Transform child)
    {
        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
        string prefabPath = source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;
        Vector3 p = child.localPosition;
        Quaternion r = child.localRotation;
        Vector3 s = child.localScale;
        return $"{child.name}|{prefabPath}|{p.x:R},{p.y:R},{p.z:R}|{r.x:R},{r.y:R},{r.z:R},{r.w:R}|{s.x:R},{s.y:R},{s.z:R}";
    }

    static TrickRun_ShiftTrigger MakeLevel40ShiftTrigger(string name, Transform parent, float worldX, float worldY, TrickRun_PlatformShift controller)
    {
        Transform triggerTransform = parent.Find(name) ?? Group(name, parent);
        triggerTransform.position = new Vector3(worldX, worldY, 0f);
        BoxCollider2D collider = triggerTransform.GetComponent<BoxCollider2D>();
        if (collider == null) collider = triggerTransform.gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.9f, 3f);
        TrickRun_ShiftTrigger trigger = triggerTransform.GetComponent<TrickRun_ShiftTrigger>();
        if (trigger == null) trigger = triggerTransform.gameObject.AddComponent<TrickRun_ShiftTrigger>();
        SerializedObject triggerSo = new SerializedObject(trigger);
        triggerSo.FindProperty("platformShift").objectReferenceValue = controller;
        triggerSo.ApplyModifiedPropertiesWithoutUndo();
        return trigger;
    }

    static Transform FindChildByName(Transform root, string name)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
            if (child.name == name) return child;
        return null;
    }

    static void Rebuild()
    {
        Transform level = Find("Level_027_FakeDoors");
        if (level == null) throw new System.InvalidOperationException("Level_027_FakeDoors bulunamadı.");
        Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(GroundPath);
        GameObject fallingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FallingPath);
        GameObject spikePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpikePath);
        if (square == null || fallingPrefab == null || spikePrefab == null) throw new System.InvalidOperationException("Level 27 kaynakları eksik.");

        for (int i = level.childCount - 1; i >= 0; i--) Object.DestroyImmediate(level.GetChild(i).gameObject);
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(level.gameObject);
        TrickRun_LevelCameraBounds bounds = level.GetComponent<TrickRun_LevelCameraBounds>();
        if (bounds == null) bounds = level.gameObject.AddComponent<TrickRun_LevelCameraBounds>();
        bounds.Configure(-1f, 32f, -40f);

        Transform grounds = Group("Grounds", level);
        Transform solidGrounds = Group("Solid Grounds", grounds);
        Transform disappearingGrounds = Group("Disappearing Grounds", grounds);
        Transform fallingGround = Group("FallingGround", grounds);
        Transform platforms = Group("Platforms", level);
        Group("Falling Platforms", platforms);
        Transform doors = Group("Doors", level);
        Transform traps = Group("Traps", level);
        Transform controllers = Group("Controllers", traps);
        Transform triggers = Group("Triggers", traps);
        Transform targets = Group("Targets", traps);
        Group("Static Spikes", traps);
        Transform spikes = Group("Rising Spikes", traps);
        Transform returnPhase = Group("Return Phase", traps);
        Transform returnTriggers = Group("Return Triggers", returnPhase);
        Transform roomBounds = Group("RoomBounds", level);
        Group("SpawnPoint", level).localPosition = new Vector3(1f, -2.05f, 0f);

        Solid("Ground_01", solidGrounds, square, 2.5f, 5f);
        GameObject disappearing1 = Solid("Disappearing Ground_01", disappearingGrounds, square, 6.5f, 2.4f);
        Solid("Ground_02", solidGrounds, square, 9.2f, 2.4f);
        Rigidbody2D falling1 = Falling("Falling Platform_01", fallingGround, fallingPrefab, 12f);
        Solid("Ground_03", solidGrounds, square, 15f, 3.2f);
        GameObject disappearing2 = Solid("Disappearing Ground_02", disappearingGrounds, square, 18.2f, 2.4f);
        Solid("Ground_04", solidGrounds, square, 21f, 2.4f);
        Rigidbody2D falling2 = Falling("Falling Platform_02", fallingGround, fallingPrefab, 23.8f);
        Solid("Ground_05", solidGrounds, square, 28.2f, 6.2f);
        Solid("LeftWall", roomBounds, square, -0.75f, 0.5f, 0f, 8f);
        Solid("RightWall", roomBounds, square, 31.25f, 0.5f, 0f, 8f);
        Solid("Ceiling", roomBounds, square, 15f, 32f, 3.75f, 0.5f);
        Hazard(roomBounds, square);

        GameObject realDoor = Door("Real Door", returnPhase, square, 1.4f);
        TrickRun_LevelExit exit = realDoor.AddComponent<TrickRun_LevelExit>();
        SetObject(exit, "levelManager", Object.FindFirstObjectByType<TrickRun_LevelManager>(FindObjectsInactive.Include));
        GameObject fakeDoor = Door("Escaping Door", doors, square, 29.5f);
        TrickRun_FakeExit fakeExit = fakeDoor.AddComponent<TrickRun_FakeExit>();
        SetObject(fakeExit, "exitToReveal", returnPhase.gameObject);

        TrickRun_FallingPlatformTrap fall1 = Controller<TrickRun_FallingPlatformTrap>("Falling Platform Controller_01", controllers);
        SetObject(fall1, "platformRigidbody", falling1); SetFloat(fall1, "gravityScale", 4.5f);
        TrickRun_FallingPlatformTrap fall2 = Controller<TrickRun_FallingPlatformTrap>("Falling Platform Controller_02", controllers);
        SetObject(fall2, "platformRigidbody", falling2); SetFloat(fall2, "gravityScale", 4.5f);
        TrickRun_DisappearingPlatformTrap hide1 = Controller<TrickRun_DisappearingPlatformTrap>("Disappearing Ground Controller_01", controllers);
        SetArray(hide1, "platforms", disappearing1); SetFloat(hide1, "disappearInterval", 0f);
        TrickRun_DisappearingPlatformTrap hide2 = Controller<TrickRun_DisappearingPlatformTrap>("Disappearing Ground Controller_02", controllers);
        SetArray(hide2, "platforms", disappearing2); SetFloat(hide2, "disappearInterval", 0f);

        Trigger("Disappearing Ground Trigger_01", triggers, 5.2f, hide1, 0.42f);
        Trigger("Falling Platform Trigger_01", triggers, 10.5f, fall1, 0.18f);
        Trigger("Disappearing Ground Trigger_02", triggers, 16.9f, hide2, 0.42f);
        Trigger("Falling Platform Trigger_02", triggers, 22.2f, fall2, 0.18f);
        Group("DoorTarget_01", targets).localPosition = new Vector3(1.4f, -1.7f, 0f);

        float[] xs = { 26.4f, 20.7f, 14.7f, 8.8f };
        for (int i = 0; i < xs.Length; i++)
        {
            GameObject spike = (GameObject)PrefabUtility.InstantiatePrefab(spikePrefab, spikes);
            spike.name = $"Rising Spike_{i + 1:00}";
            spike.transform.localPosition = new Vector3(xs[i], -3.75f, 0f);
            Trigger($"Rising Spike Trigger_{i + 1:00}", returnTriggers, xs[i] + 1.35f, spike.GetComponent<TrickRun_RisingSpikeTrap>(), 0f);
        }

        returnPhase.gameObject.SetActive(false);
        EditorUtility.SetDirty(level.gameObject);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        EditorSceneManager.SaveScene(level.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("LEVEL27_REBUILD_OK: Somut Level 27 hiyerarşisi oluşturuldu.");
    }

    static void RebuildLevel28()
    {
        Transform level = Find("Level_028_FakeDoors");
        if (level == null) throw new System.InvalidOperationException("Level_028_FakeDoors bulunamadı.");

        Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(GroundPath);
        GameObject fallingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FallingPath);
        GameObject risingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpikePath);
        GameObject staticSpikePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StaticSpikePath);
        if (square == null || fallingPrefab == null || risingPrefab == null || staticSpikePrefab == null)
            throw new System.InvalidOperationException("Level 28 kaynakları eksik.");

        for (int i = level.childCount - 1; i >= 0; i--) Object.DestroyImmediate(level.GetChild(i).gameObject);
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(level.gameObject);

        TrickRun_LevelCameraBounds bounds = level.GetComponent<TrickRun_LevelCameraBounds>();
        if (bounds == null) bounds = level.gameObject.AddComponent<TrickRun_LevelCameraBounds>();
        float cameraY = bounds.CameraY;
        bounds.Configure(-1f, 32f, cameraY);

        Group("SpawnPoint", level).localPosition = new Vector3(0.5f, -2.35f, 0f);
        Transform firstRoom = Group("First Room", level);
        Transform firstGrounds = Group("Grounds", firstRoom);
        Transform firstSolid = Group("Solid Grounds", firstGrounds);
        Transform firstDisappear = Group("Disappearing Grounds", firstGrounds);
        Transform firstTraps = Group("Traps", firstRoom);
        Transform firstControllers = Group("Controllers", firstTraps);
        Transform firstTriggers = Group("Triggers", firstTraps);
        Transform firstSpikes = Group("Rising Spikes", firstTraps);
        Transform firstDoors = Group("Doors", firstRoom);

        const float floorY = -5.3f;
        const float floorHeight = 5f;
        Solid("Ground_01", firstSolid, square, 2.5f, 7f, floorY, floorHeight);
        GameObject disappearing1 = Solid("Disappearing Ground_01", firstDisappear, square, 7.5f, 3f, floorY, floorHeight);
        Solid("Ground_02", firstSolid, square, 11f, 4f, floorY, floorHeight);
        Solid("Ground_03", firstSolid, square, 15.5f, 5f, floorY, floorHeight);
        GameObject disappearing2 = Solid("Disappearing Ground_02", firstDisappear, square, 20f, 4f, floorY, floorHeight);
        Solid("Ground_04", firstSolid, square, 25f, 6f, floorY, floorHeight);
        Solid("Door Ground", firstSolid, square, 30f, 4f, floorY, floorHeight);

        TrickRun_DisappearingPlatformTrap hide1 = Controller<TrickRun_DisappearingPlatformTrap>("Disappearing Ground Controller_01", firstControllers);
        SetArray(hide1, "platforms", disappearing1); SetFloat(hide1, "disappearInterval", 0f);
        TrickRun_DisappearingPlatformTrap hide2 = Controller<TrickRun_DisappearingPlatformTrap>("Disappearing Ground Controller_02", firstControllers);
        SetArray(hide2, "platforms", disappearing2); SetFloat(hide2, "disappearInterval", 0f);
        Trigger("Disappearing Ground Trigger_01", firstTriggers, 5.4f, hide1, 0.38f);
        Trigger("Disappearing Ground Trigger_02", firstTriggers, 18.8f, hide2, 0.38f);

        float[] firstSpikeX = { 12.7f, 23.5f };
        for (int i = 0; i < firstSpikeX.Length; i++)
        {
            GameObject spike = (GameObject)PrefabUtility.InstantiatePrefab(risingPrefab, firstSpikes);
            spike.name = $"Rising Spike_{i + 1:00}";
            spike.transform.localPosition = new Vector3(firstSpikeX[i], -3.75f, 0f);
            Trigger($"Rising Spike Trigger_{i + 1:00}", firstTriggers, firstSpikeX[i] - 1.25f, spike.GetComponent<TrickRun_RisingSpikeTrap>(), 0.08f);
        }

        float[] movingSpikeX = { 16.8f, 27.2f };
        for (int i = 0; i < movingSpikeX.Length; i++)
        {
            GameObject spike = (GameObject)PrefabUtility.InstantiatePrefab(risingPrefab, firstSpikes);
            spike.name = $"Rising Moving Spike_{i + 1:00}";
            spike.transform.localPosition = new Vector3(movingSpikeX[i], -3.75f, 0f);

            Transform target = Group($"Moving Spike Target_{i + 1:00}", firstSpikes);
            target.localPosition = new Vector3(movingSpikeX[i] - 3.25f, -2.25f, 0f);
            TrickRun_MovingObjectTrap move = Controller<TrickRun_MovingObjectTrap>($"Moving Spike Controller_{i + 1:00}", firstControllers);
            SetObject(move, "movingObject", spike.transform);
            SetObject(move, "targetPoint", target);
            SetFloat(move, "movementSpeed", 7f);

            MultiTrigger($"Rising Moving Spike Trigger_{i + 1:00}", firstTriggers, movingSpikeX[i] - 1.2f,
                new TrickRun_TrapBase[] { spike.GetComponent<TrickRun_RisingSpikeTrap>(), move },
                new float[] { 0.05f, 0.22f });
        }

        Transform secondRoom = Group("Second Room", level);
        Transform secondGrounds = Group("Grounds", secondRoom);
        Transform secondPlatforms = Group("Platforms", secondRoom);
        Transform staticPlatforms = Group("Static Platforms", secondPlatforms);
        Transform fallingPlatforms = Group("Falling Platforms", secondPlatforms);
        Transform secondTraps = Group("Traps", secondRoom);
        Transform secondControllers = Group("Controllers", secondTraps);
        Transform secondTriggers = Group("Triggers", secondTraps);
        Transform staticSpikes = Group("Static Spikes", secondTraps);
        Transform secondDoors = Group("Doors", secondRoom);

        Solid("Second Room Start Ground", secondGrounds, square, 34.5f, 4f, floorY, floorHeight);
        Solid("Second Room End Ground", secondGrounds, square, 70f, 5f, floorY, floorHeight);

        float[] platformX = { 38.5f, 42f, 45.5f, 49f, 52.5f, 56f, 59.5f, 63f, 66.5f };
        float[] platformY = { -2.5f, -1.9f, -1.3f, -1.9f, -2.5f, -1.8f, -1.2f, -1.8f, -2.5f };
        int[] fallingIndices = { 1, 3, 6 };
        int fallNumber = 0;
        for (int i = 0; i < platformX.Length; i++)
        {
            bool falls = System.Array.IndexOf(fallingIndices, i) >= 0;
            if (falls)
            {
                Rigidbody2D body = Falling($"Falling Platform_{fallNumber + 1:00}", fallingPlatforms, fallingPrefab, platformX[i]);
                body.transform.localPosition = new Vector3(platformX[i], platformY[i], 0f);
                TrickRun_FallingPlatformTrap controller = Controller<TrickRun_FallingPlatformTrap>($"Falling Platform Controller_{fallNumber + 1:00}", secondControllers);
                SetObject(controller, "platformRigidbody", body); SetFloat(controller, "gravityScale", 4.5f);
                Trigger($"Falling Platform Trigger_{fallNumber + 1:00}", secondTriggers, platformX[i] - 1.3f, controller, 0.2f);
                fallNumber++;
            }
            else
            {
                GameObject platform = Solid($"Static Platform_{i + 1:00}", staticPlatforms, square, platformX[i], 2.4f, platformY[i], 0.8f);
                if (i == 2 || i == 5 || i == 8)
                {
                    GameObject spike = (GameObject)PrefabUtility.InstantiatePrefab(staticSpikePrefab, staticSpikes);
                    spike.name = $"Static Spike_{i + 1:00}";
                    spike.transform.localPosition = new Vector3(platformX[i], platformY[i] + 0.65f, 0f);
                }
            }
        }

        GameObject realDoor = Door("Real Door", secondDoors, square, 70.5f);
        TrickRun_LevelExit exit = realDoor.AddComponent<TrickRun_LevelExit>();
        TrickRun_LevelManager manager = Object.FindFirstObjectByType<TrickRun_LevelManager>(FindObjectsInactive.Include);
        SetObject(exit, "levelManager", manager);
        secondRoom.gameObject.SetActive(false);

        GameObject fakeDoor = Door("Fake Door", firstDoors, square, 29.5f);
        TrickRun_FakeExit fakeExit = fakeDoor.AddComponent<TrickRun_FakeExit>();
        SetObject(fakeExit, "exitToReveal", secondRoom.gameObject);

        Transform roomBounds = Group("RoomBounds", level);
        Solid("LeftWall", roomBounds, square, -1.5f, 0.5f, 0.5f, 14f);
        GameObject dividerWall = Solid("RightWall", roomBounds, square, 32f, 0.8f, 0.5f, 14f);
        GameObject finalWall = Solid("FinalRightWall", roomBounds, square, 73f, 0.8f, 0.5f, 14f);
        Solid("Ceiling", roomBounds, square, 35.75f, 75f, 7.5f, 0.5f);
        HazardWide(roomBounds, square, 35.75f, 76f);

        TrickRun_CameraBoundsTrap cameraTrap = Controller<TrickRun_CameraBoundsTrap>("Camera Bounds Controller", firstControllers);
        SetObject(cameraTrap, "levelBounds", bounds);
        SetObject(cameraTrap, "player", Object.FindFirstObjectByType<TrickRun_PlayerController>(FindObjectsInactive.Include).transform);
        SetObject(cameraTrap, "firstRoomRightWall", dividerWall);
        SetObject(cameraTrap, "finalRightWall", finalWall);
        SetFloat(cameraTrap, "initialRightEdge", 32f); SetFloat(cameraTrap, "expandedRightEdge", 73f);

        Trigger("Fake Door Room Expansion Trigger", firstTriggers, 29.1f, cameraTrap, 0f);

        EditorUtility.SetDirty(level.gameObject);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        EditorSceneManager.SaveScene(level.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("LEVEL28_REBUILD_OK: İki odalı uzun koridor ve dinamik kamera sınırı oluşturuldu.");
    }

    static void RebuildLevel29()
    {
        Transform level = Find("Level_029_FakeDoors");
        if (level == null) throw new System.InvalidOperationException("Level_029_FakeDoors bulunamadı.");

        Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(GroundPath);
        GameObject risingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpikePath);
        GameObject staticSpikePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StaticSpikePath);
        if (square == null || risingPrefab == null || staticSpikePrefab == null)
            throw new System.InvalidOperationException("Level 29 kaynakları eksik.");

        for (int i = level.childCount - 1; i >= 0; i--) Object.DestroyImmediate(level.GetChild(i).gameObject);
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(level.gameObject);

        TrickRun_LevelCameraBounds bounds = level.GetComponent<TrickRun_LevelCameraBounds>();
        if (bounds == null) bounds = level.gameObject.AddComponent<TrickRun_LevelCameraBounds>();
        Vector3 levelWorldPosition = level.position;
        bounds.ConfigureVertical(
            levelWorldPosition.x - 2f,
            levelWorldPosition.x + 32f,
            levelWorldPosition.y - 19f,
            levelWorldPosition.y - 44f,
            levelWorldPosition.y + 5f);

        Transform marker = Group("Level29_VerticalZigzag_v5", level);
        marker.gameObject.hideFlags = HideFlags.HideInHierarchy;
        Transform spawn = Group("SpawnPoint", level);
        spawn.localPosition = new Vector3(1.5f, -1.75f, 0f);

        Transform grounds = Group("Grounds", level);
        Transform corridors = Group("Zigzag Corridors", grounds);
        Transform stairPlatforms = Group("Stair Platforms", grounds);
        Transform traps = Group("Traps", level);
        Transform risingSpikes = Group("Rising Spikes", traps);
        Transform movingSpikes = Group("Rising Moving Spikes", traps);
        Transform chaseWalls = Group("Chasing Spike Walls", traps);
        Transform controllers = Group("Controllers", traps);
        Transform descentControllers = Group("Descent Controllers", controllers);
        Transform chaseControllers = Group("Chase Controllers", controllers);
        Transform triggers = Group("Triggers", traps);
        Transform descentTriggers = Group("Descent Triggers", triggers);
        Transform relayTriggers = Group("Wall Relay Triggers", triggers);
        Transform doors = Group("Doors", level);

        float[] floorY = { -3f, -10f, -17f, -24f, -31f };
        for (int i = 0; i < floorY.Length; i++)
        {
            bool openingRight = i % 2 == 0;
            float centerX = openingRight ? 13f : 17f;
            Solid($"Corridor Ground_{i + 1:00}", corridors, square, centerX, 28f, floorY[i], 0.9f);

            bool playerMovesRight = openingRight;
            float riseX = playerMovesRight ? 9f : 21f;
            GameObject rise = (GameObject)PrefabUtility.InstantiatePrefab(risingPrefab, risingSpikes);
            rise.name = $"Rising Spike_{i + 1:00}";
            rise.transform.localPosition = new Vector3(riseX, floorY[i] - 0.5f, 0f);
            float triggerX = playerMovesRight ? riseX - 1.5f : riseX + 1.5f;
            TriggerAt($"Rising Spike Trigger_{i + 1:00}", descentTriggers, triggerX, floorY[i] + 1.4f,
                rise.GetComponent<TrickRun_RisingSpikeTrap>(), 0.05f);

            float extraRiseX = playerMovesRight ? 14.5f : 15.5f;
            GameObject extraRise = (GameObject)PrefabUtility.InstantiatePrefab(risingPrefab, risingSpikes);
            extraRise.name = $"Rising Spike Extra_{i + 1:00}";
            extraRise.transform.localPosition = new Vector3(extraRiseX, floorY[i] - 0.5f, 0f);
            float extraTriggerX = playerMovesRight ? extraRiseX - 1.4f : extraRiseX + 1.4f;
            TriggerAt($"Rising Spike Extra Trigger_{i + 1:00}", descentTriggers, extraTriggerX, floorY[i] + 1.4f,
                extraRise.GetComponent<TrickRun_RisingSpikeTrap>(), 0.04f);

            float movingX = playerMovesRight ? 20f : 10f;
            GameObject moving = (GameObject)PrefabUtility.InstantiatePrefab(risingPrefab, movingSpikes);
            moving.name = $"Rising Moving Spike_{i + 1:00}";
            moving.transform.localPosition = new Vector3(movingX, floorY[i] - 0.5f, 0f);
            Transform movingTarget = Group($"Moving Spike Target_{i + 1:00}", movingSpikes);
            movingTarget.localPosition = new Vector3(movingX + (playerMovesRight ? -3.5f : 3.5f), floorY[i] + 1f, 0f);
            TrickRun_MovingObjectTrap movingTrap = Controller<TrickRun_MovingObjectTrap>($"Moving Spike Controller_{i + 1:00}", descentControllers);
            SetObject(movingTrap, "movingObject", moving.transform);
            SetObject(movingTrap, "targetPoint", movingTarget);
            SetFloat(movingTrap, "movementSpeed", 7f);
            float movingTriggerX = playerMovesRight ? movingX - 1.5f : movingX + 1.5f;
            MultiTriggerAt($"Rising Moving Spike Trigger_{i + 1:00}", descentTriggers, movingTriggerX, floorY[i] + 1.4f,
                new TrickRun_TrapBase[] { moving.GetComponent<TrickRun_RisingSpikeTrap>(), movingTrap },
                new float[] { 0.05f, 0.22f });
        }
        Solid("Bottom Ground", corridors, square, 15f, 32f, -38f, 0.9f);

        // Her koridorun acik ucuna, donus yolunda yukari cikilabilen merdivenler koyar.
        for (int gap = 0; gap < floorY.Length; gap++)
        {
            bool rightShaft = gap % 2 == 0;
            float lowerFloorY = gap == floorY.Length - 1 ? -38f : floorY[gap + 1];
            float upperFloorY = floorY[gap];
            for (int step = 0; step < 3; step++)
            {
                float t = (step + 1f) / 4f;
                float stepY = Mathf.Lerp(lowerFloorY, upperFloorY, t);
                float stepX;
                if (rightShaft)
                    stepX = step % 2 == 0 ? 28.15f : 29.85f;
                else
                    stepX = step % 2 == 0 ? 1.85f : 0.15f;
                Solid($"Stair Platform_{gap + 1:00}_{step + 1:00}", stairPlatforms, square,
                    stepX, 1.9f, stepY, 0.45f);
            }
        }

        GameObject realDoor = Door("Real Door", doors, square, 2f);
        realDoor.transform.localPosition = new Vector3(2f, -1.45f, 0f);
        TrickRun_LevelExit exit = realDoor.AddComponent<TrickRun_LevelExit>();
        TrickRun_LevelManager manager = Object.FindFirstObjectByType<TrickRun_LevelManager>(FindObjectsInactive.Include);
        SetObject(exit, "levelManager", manager);
        if (manager != null) SetInt(manager, "startingLevelIndex", 28);
        TrickRun_AppearingObjectsTrap revealRealDoor = Controller<TrickRun_AppearingObjectsTrap>("Real Door Reveal Controller", controllers);
        SetArray(revealRealDoor, "objectsToAppear", realDoor);
        revealRealDoor.ResetTrap();

        GameObject escapingDoor = Door("Escaping Door", doors, square, 28f);
        escapingDoor.transform.localPosition = new Vector3(28f, -36.45f, 0f);
        Object.DestroyImmediate(escapingDoor.GetComponent<BoxCollider2D>());
        BoxCollider2D escapingCollider = escapingDoor.AddComponent<BoxCollider2D>();
        escapingCollider.isTrigger = true; escapingCollider.size = new Vector2(0.9f, 2.2f);
        Rigidbody2D escapingBody = escapingDoor.AddComponent<Rigidbody2D>();
        escapingBody.bodyType = RigidbodyType2D.Kinematic; escapingBody.gravityScale = 0f;
        Transform doorTarget = Group("Escaping Door Top Target", doors);
        doorTarget.localPosition = realDoor.transform.localPosition;
        TrickRun_MovingObjectTrap doorMove = Controller<TrickRun_MovingObjectTrap>("Escaping Door Controller", controllers);
        SetObject(doorMove, "movingObject", escapingDoor.transform);
        SetObject(doorMove, "targetPoint", doorTarget);
        SetFloat(doorMove, "movementSpeed", 12f);

        GameObject fakeDoorTrigger = new GameObject("Fake Door Trigger");
        fakeDoorTrigger.transform.SetParent(doors, false);
        fakeDoorTrigger.transform.localPosition = new Vector3(28f, -36.45f, 0f);
        BoxCollider2D fakeCollider = fakeDoorTrigger.AddComponent<BoxCollider2D>();
        fakeCollider.isTrigger = true; fakeCollider.size = new Vector2(1.2f, 2.6f);

        TrickRun_MovingObjectTrap[] wallTraps = new TrickRun_MovingObjectTrap[floorY.Length];
        Transform[] wallRoots = new Transform[floorY.Length];
        for (int i = 0; i < floorY.Length; i++)
        {
            int corridorIndex = floorY.Length - 1 - i;
            float y = floorY[corridorIndex] + 2.25f;
            bool movesLeft = corridorIndex % 2 == 0;
            // Duvarlar tetiklenene kadar oda dis duvarlarinin icinde tamamen saklidir.
            float startX = movesLeft ? 32.2f : -2.2f;
            float endX = movesLeft ? -2.2f : 32.2f;

            Transform wall = Group($"Chasing Spike Wall_{i + 1:00}", chaseWalls);
            wall.localPosition = new Vector3(startX, y, 0f);
            Rigidbody2D wallBody = wall.gameObject.AddComponent<Rigidbody2D>();
            wallBody.bodyType = RigidbodyType2D.Kinematic; wallBody.gravityScale = 0f;
            for (int spikeIndex = 0; spikeIndex < 6; spikeIndex++)
            {
                GameObject spike = (GameObject)PrefabUtility.InstantiatePrefab(staticSpikePrefab, wall);
                spike.name = $"Wall Spike_{spikeIndex + 1:00}";
                spike.transform.localPosition = new Vector3(0f, -1.8f + spikeIndex * 0.72f, 0f);
                spike.transform.localRotation = Quaternion.Euler(0f, 0f, movesLeft ? 90f : -90f);
            }

            Transform target = Group($"Chasing Wall Target_{i + 1:00}", chaseWalls);
            target.localPosition = new Vector3(endX, y, 0f);
            TrickRun_MovingObjectTrap wallMove = Controller<TrickRun_MovingObjectTrap>($"Chasing Wall Controller_{i + 1:00}", chaseControllers);
            SetObject(wallMove, "movingObject", wall);
            SetObject(wallMove, "targetPoint", target);
            SetFloat(wallMove, "movementSpeed", 5.2f);
            wallRoots[i] = wall;
            wallTraps[i] = wallMove;
        }

        TrickRun_TrapTrigger fakeTrigger = fakeDoorTrigger.AddComponent<TrickRun_TrapTrigger>();
        ConfigureTrigger(fakeTrigger, new TrickRun_TrapBase[] { doorMove, wallTraps[0] }, new float[] { 0f, 0.1f });

        for (int i = 0; i < wallRoots.Length - 1; i++)
        {
            Transform relay = Group($"Wall Relay Trigger_{i + 1:00}", relayTriggers);
            relay.localPosition = wallRoots[i].parent.Find($"Chasing Wall Target_{i + 1:00}").localPosition;
            BoxCollider2D relayCollider = relay.gameObject.AddComponent<BoxCollider2D>();
            relayCollider.isTrigger = true; relayCollider.size = new Vector2(1f, 5.5f);
            TrickRun_WallTrapRelayTrigger relayTrigger = relay.gameObject.AddComponent<TrickRun_WallTrapRelayTrigger>();
            SetObject(relayTrigger, "activatingWall", wallRoots[i]);
            SetObject(relayTrigger, "nextWallTrap", wallTraps[i + 1]);
        }

        Transform doorRelay = Group("Escaping Door Top Relay", relayTriggers);
        doorRelay.localPosition = doorTarget.localPosition;
        BoxCollider2D doorRelayCollider = doorRelay.gameObject.AddComponent<BoxCollider2D>();
        doorRelayCollider.isTrigger = true; doorRelayCollider.size = new Vector2(1.2f, 2.8f);
        TrickRun_WallTrapRelayTrigger doorRelayTrigger = doorRelay.gameObject.AddComponent<TrickRun_WallTrapRelayTrigger>();
        SetObject(doorRelayTrigger, "activatingWall", escapingDoor.transform);
        SetObject(doorRelayTrigger, "nextWallTrap", revealRealDoor);

        Transform roomBounds = Group("RoomBounds", level);
        Solid("LeftWall", roomBounds, square, -1.5f, 0.8f, -19f, 49f);
        Solid("RightWall", roomBounds, square, 31.5f, 0.8f, -19f, 49f);
        Solid("Ceiling", roomBounds, square, 15f, 34f, 5f, 0.6f);
        GameObject deathFloor = Solid("Death Floor", roomBounds, square, 15f, 34f, -43.5f, 0.6f);
        deathFloor.layer = 0; deathFloor.tag = "Hazard";
        deathFloor.GetComponent<BoxCollider2D>().isTrigger = true;
        deathFloor.GetComponent<SpriteRenderer>().color = new Color(0.55f, 0.08f, 0.08f);

        EditorUtility.SetDirty(level.gameObject);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        bool sceneSaved = EditorSceneManager.SaveScene(level.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"LEVEL29_REBUILD_OK: Dikey zikzak iniş ve duvardan duvara kovalamaca oluşturuldu. Scene={level.gameObject.scene.path}, Saved={sceneSaved}");
    }

    static Transform Find(string name)
    {
        foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>()) if (t.name == name && t.gameObject.scene.IsValid()) return t;
        return null;
    }
    static Transform Group(string name, Transform parent) { GameObject go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform; }
    static GameObject Solid(string name, Transform parent, Sprite sprite, float x, float width, float y = -3.25f, float height = 0.9f)
    {
        GameObject go = new GameObject(name) { layer = 8 }; go.transform.SetParent(parent, false); go.transform.localPosition = new Vector3(x, y, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.drawMode = SpriteDrawMode.Tiled; sr.size = new Vector2(width, height); sr.color = new Color(0.16f, 0.18f, 0.23f); sr.sortingOrder = 1;
        go.AddComponent<BoxCollider2D>().size = new Vector2(width, height); return go;
    }
    static Rigidbody2D Falling(string name, Transform parent, GameObject prefab, float x)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent); go.name = name; go.layer = 8; go.transform.localPosition = new Vector3(x, -3.25f, 0f);
        Rigidbody2D rb = go.GetComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic; rb.gravityScale = 0f; return rb;
    }
    static GameObject Door(string name, Transform parent, Sprite sprite, float x)
    {
        GameObject go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = new Vector3(x, -1.7f, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.drawMode = SpriteDrawMode.Tiled; sr.size = new Vector2(0.9f, 2.2f); sr.color = new Color(1f, 0.45f, 0.08f); sr.sortingOrder = 3;
        BoxCollider2D c = go.AddComponent<BoxCollider2D>(); c.size = sr.size; c.isTrigger = true; return go;
    }
    static void Hazard(Transform parent, Sprite sprite)
    {
        GameObject go = Solid("Death_Floor", parent, sprite, 15f, 34f, -7f, 0.5f); go.layer = 0; go.tag = "Hazard"; go.GetComponent<BoxCollider2D>().isTrigger = true; go.GetComponent<SpriteRenderer>().color = new Color(0.55f, 0.08f, 0.08f);
    }
    static void HazardWide(Transform parent, Sprite sprite, float x, float width)
    {
        GameObject go = Solid("Death Floor", parent, sprite, x, width, -7f, 0.5f);
        go.layer = 0; go.tag = "Hazard"; go.GetComponent<BoxCollider2D>().isTrigger = true;
        go.GetComponent<SpriteRenderer>().color = new Color(0.55f, 0.08f, 0.08f);
    }
    static T Controller<T>(string name, Transform parent) where T : Component { GameObject go = new GameObject(name); go.transform.SetParent(parent, false); return go.AddComponent<T>(); }
    static void Trigger(string name, Transform parent, float x, TrickRun_TrapBase target, float delay)
    {
        GameObject go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = new Vector3(x, -1.7f, 0f);
        BoxCollider2D c = go.AddComponent<BoxCollider2D>(); c.isTrigger = true; c.size = new Vector2(0.7f, 3.1f);
        TrickRun_TrapTrigger trigger = go.AddComponent<TrickRun_TrapTrigger>(); SerializedObject so = new SerializedObject(trigger);
        SerializedProperty a = so.FindProperty("trapActivations"); a.arraySize = 1; SerializedProperty item = a.GetArrayElementAtIndex(0);
        item.FindPropertyRelative("trap").objectReferenceValue = target; item.FindPropertyRelative("activationDelay").floatValue = delay; so.FindProperty("activateOnlyOnce").boolValue = true; so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void MultiTrigger(string name, Transform parent, float x, TrickRun_TrapBase[] targets, float[] delays)
    {
        GameObject go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = new Vector3(x, -1.7f, 0f);
        BoxCollider2D c = go.AddComponent<BoxCollider2D>(); c.isTrigger = true; c.size = new Vector2(0.8f, 3.1f);
        TrickRun_TrapTrigger trigger = go.AddComponent<TrickRun_TrapTrigger>(); SerializedObject so = new SerializedObject(trigger);
        SerializedProperty a = so.FindProperty("trapActivations"); a.arraySize = targets.Length;
        for (int i = 0; i < targets.Length; i++)
        {
            SerializedProperty item = a.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("trap").objectReferenceValue = targets[i];
            item.FindPropertyRelative("activationDelay").floatValue = i < delays.Length ? delays[i] : 0f;
        }
        so.FindProperty("activateOnlyOnce").boolValue = true; so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void TriggerAt(string name, Transform parent, float x, float y, TrickRun_TrapBase target, float delay)
    {
        GameObject go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = new Vector3(x, y, 0f);
        BoxCollider2D c = go.AddComponent<BoxCollider2D>(); c.isTrigger = true; c.size = new Vector2(0.8f, 3.2f);
        TrickRun_TrapTrigger trigger = go.AddComponent<TrickRun_TrapTrigger>();
        ConfigureTrigger(trigger, new TrickRun_TrapBase[] { target }, new float[] { delay });
    }
    static void MultiTriggerAt(string name, Transform parent, float x, float y, TrickRun_TrapBase[] targets, float[] delays)
    {
        GameObject go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = new Vector3(x, y, 0f);
        BoxCollider2D c = go.AddComponent<BoxCollider2D>(); c.isTrigger = true; c.size = new Vector2(0.8f, 3.2f);
        TrickRun_TrapTrigger trigger = go.AddComponent<TrickRun_TrapTrigger>();
        ConfigureTrigger(trigger, targets, delays);
    }
    static void ConfigureTrigger(TrickRun_TrapTrigger trigger, TrickRun_TrapBase[] targets, float[] delays)
    {
        SerializedObject so = new SerializedObject(trigger);
        SerializedProperty a = so.FindProperty("trapActivations"); a.arraySize = targets.Length;
        for (int i = 0; i < targets.Length; i++)
        {
            SerializedProperty item = a.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("trap").objectReferenceValue = targets[i];
            item.FindPropertyRelative("activationDelay").floatValue = i < delays.Length ? delays[i] : 0f;
        }
        so.FindProperty("activateOnlyOnce").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void SetObject(Object target, string field, Object value) { SerializedObject so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void SetFloat(Object target, string field, float value) { SerializedObject so = new SerializedObject(target); so.FindProperty(field).floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void SetInt(Object target, string field, int value) { SerializedObject so = new SerializedObject(target); so.FindProperty(field).intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void SetArray(Object target, string field, Object value) { SerializedObject so = new SerializedObject(target); SerializedProperty p = so.FindProperty(field); p.arraySize = 1; p.GetArrayElementAtIndex(0).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
}
