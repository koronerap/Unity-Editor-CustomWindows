using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTabsWindow : EditorWindow
{
    private List<string> openScenePaths = new List<string>();
    private const string PREFS_KEY = "SceneTabs_OpenScenes";
    private Vector2 scrollPosition;
    private const float TAB_HEIGHT = 30f;

    private static GUIStyle _tabStyle;
    private static GUIStyle _activeTabStyle;
    private static GUIStyle _closeButtonStyle;

    [MenuItem("Tape Corps/Scene Tabs")]
    public static void ShowWindow()
    {
        var window = GetWindow<SceneTabsWindow>();
        window.titleContent = new GUIContent("Scene Tabs", EditorGUIUtility.ObjectContent(null, typeof(SceneAsset)).image);
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("Scene Tabs", EditorGUIUtility.ObjectContent(null, typeof(SceneAsset)).image);
        LoadTabs();
        EditorSceneManager.sceneOpened += OnSceneOpened;

        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && !string.IsNullOrEmpty(activeScene.path))
            AddTab(activeScene.path);
    }

    private void OnDisable()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
    }

    private void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        AddTab(scene.path);
    }

    private void AddTab(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (!openScenePaths.Contains(path))
        {
            openScenePaths.Add(path);
            SaveTabs();
            Repaint();
        }
    }

    private void RemoveTab(string path)
    {
        if (openScenePaths.Remove(path))
        {
            SaveTabs();
            Repaint();
        }
    }

    private void LoadTabs()
    {
        string data = EditorPrefs.GetString(PREFS_KEY, "");
        if (!string.IsNullOrEmpty(data))
            openScenePaths = data.Split(';').Where(s => !string.IsNullOrEmpty(s)).ToList();
    }

    private void SaveTabs()
    {
        EditorPrefs.SetString(PREFS_KEY, string.Join(";", openScenePaths));
    }

    private void InitStyles()
    {
        if (_tabStyle != null) return;

        _tabStyle = new GUIStyle(EditorStyles.toolbarButton)
        {
            alignment = TextAnchor.MiddleLeft,
            imagePosition = ImagePosition.ImageLeft,
            fixedHeight = TAB_HEIGHT,
            padding = new RectOffset(8, 22, 0, 0),
            margin = new RectOffset(0, 0, 0, 0),
            fontStyle = FontStyle.Normal,
            fontSize = 11,
        };

        _activeTabStyle = new GUIStyle(_tabStyle)
        {
            fontStyle = FontStyle.Bold
        };
        _activeTabStyle.normal = _activeTabStyle.onNormal;
        _activeTabStyle.normal.textColor = EditorGUIUtility.isProSkin
            ? new Color(0.9f, 0.9f, 0.9f)
            : Color.black;

        _closeButtonStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fixedWidth = 14,
            fixedHeight = 14,
            padding = new RectOffset(0, 0, 0, 2),
            margin = new RectOffset(0, 0, 0, 0),
            fontStyle = FontStyle.Bold
        };
    }

    private void OnGUI()
    {
        InitStyles();
        DrawTabs();
        HandleDragAndDrop();
    }

    private void DrawTabs()
    {
        var paths = new List<string>(openScenePaths);
        string currentScenePath = SceneManager.GetActiveScene().path;
        string sceneToRemove = null;

        // Fill entire window background (handles docked tall windows)
        /*EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height),
            EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.76f, 0.76f, 0.76f));

        // Toolbar background strip at top
        Rect toolbarRect = new Rect(0, 0, position.width, TAB_HEIGHT);
        EditorGUI.DrawRect(toolbarRect,
            EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.8f, 0.8f, 0.8f));*/

        // Draw all tabs using absolute rects (no layout nesting issues)
        float x = 2f;

        foreach (var path in paths)
        {
            string sceneName = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(sceneName)) sceneName = "Untitled";

            bool isActive = (path == currentScenePath);
            Texture icon = AssetDatabase.GetCachedIcon(path);
            GUIContent content = new GUIContent(sceneName, icon);
            GUIStyle style = isActive ? _activeTabStyle : _tabStyle;

            // Calculate compact width: padding(8) + icon(16) + gap(4) + text + gap(4) + closeBtn(14) + padding(6)
            float textW = EditorStyles.label.CalcSize(new GUIContent(sceneName)).x;
            float tabWidth = 8f + 16f + 4f + textW + 4f + 14f + 8f;
            tabWidth = Mathf.Max(tabWidth, 60f);

            float tabY = (position.height - TAB_HEIGHT) * 0.5f;
            tabY = Mathf.Max(tabY, 0f);
            Rect tabRect = new Rect(x, tabY, tabWidth, TAB_HEIGHT);

            // Draw tab
            if (Event.current.type == EventType.Repaint)
            {
                bool hover = tabRect.Contains(Event.current.mousePosition);
                style.Draw(tabRect, content, hover, isActive, isActive, false);
            }

            // Close button (right side, vertically centered)
            float cbSize = 14f;
            Rect closeRect = new Rect(tabRect.xMax - cbSize - 3, tabRect.y + (TAB_HEIGHT - cbSize) / 2, cbSize, cbSize);

            // Show close button with hover visibility
            bool tabHover = tabRect.Contains(Event.current.mousePosition);
            GUI.color = tabHover ? Color.white : new Color(1, 1, 1, 0.35f);
            if (GUI.Button(closeRect, "×", _closeButtonStyle))
            {
                sceneToRemove = path;
                Event.current.Use();
            }
            GUI.color = Color.white;

            // Middle-click to close
            if (Event.current.type == EventType.MouseDown && Event.current.button == 2
                && tabRect.Contains(Event.current.mousePosition))
            {
                sceneToRemove = path;
                Event.current.Use();
            }

            // Left-click to switch scene
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0
                && tabRect.Contains(Event.current.mousePosition)
                && !closeRect.Contains(Event.current.mousePosition))
            {
                if (!isActive && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(path);
                Event.current.Use();
            }

            x += tabWidth + 1f;
        }

        // (+) Add scene button
        GUIStyle plusStyle = new GUIStyle(_tabStyle)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 13,
            padding = new RectOffset(0, 0, 0, 2),
            imagePosition = ImagePosition.TextOnly
        };
        float plusY = (position.height - TAB_HEIGHT) * 0.5f;
        plusY = Mathf.Max(plusY, 0f);
        Rect plusRect = new Rect(x, plusY, 26, TAB_HEIGHT);
        if (GUI.Button(plusRect, "+", plusStyle))
            ShowSceneSelectionMenu();

        if (sceneToRemove != null)
            RemoveTab(sceneToRemove);
    }

    private void ShowSceneSelectionMenu()
    {
        GenericMenu menu = new GenericMenu();
        string[] guids = AssetDatabase.FindAssets("t:Scene");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Packages/")) continue;

            string menuPath = path.Replace("Assets/", "").Replace(".unity", "");
            bool alreadyOpen = openScenePaths.Contains(path);

            menu.AddItem(new GUIContent(menuPath), alreadyOpen, () =>
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(path);
            });
        }

        menu.ShowAsContext();
    }

    private void HandleDragAndDrop()
    {
        Event evt = Event.current;
        Rect dropArea = new Rect(0, 0, position.width, position.height);

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition)) return;
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is SceneAsset)
                            AddTab(AssetDatabase.GetAssetPath(obj));
                    }
                }
                break;
        }
    }
}
