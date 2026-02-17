using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class QuickSelectWindow : EditorWindow
{
    public List<Object> objects = new List<Object>();
    private Vector2 scrollPosition;
    private static QuickSelectWindow Instance;

    private static GUIStyle _itemStyle;
    private static GUIStyle _itemActiveStyle;
    private static GUIStyle _removeButtonStyle;
    private static GUIStyle _headerStyle;

    [MenuItem("Tape Corps/Quick Select")]
    public static void ShowWindow()
    {
        Instance = GetWindow<QuickSelectWindow>();
        Instance.titleContent = new GUIContent("Quick Select", EditorGUIUtility.IconContent("d_Favorite").image);
        Instance.minSize = new Vector2(200, 100);
    }

    private void OnEnable()
    {
        if (!Instance) Instance = this;
        titleContent = new GUIContent("Quick Select", EditorGUIUtility.IconContent("d_Favorite").image);
        EditorUtility.SetDirty(this);
    }

    private void OnDisable()
    {
        if (Instance) EditorUtility.ClearDirty(Instance);
    }

    private void InitStyles()
    {
        if (_itemStyle != null) return;

        _itemStyle = new GUIStyle("CN Box")
        {
            alignment = TextAnchor.MiddleLeft,
            imagePosition = ImagePosition.ImageLeft,
            fixedHeight = 24,
            padding = new RectOffset(6, 4, 2, 2),
            margin = new RectOffset(0, 0, 0, 0),
            fontSize = 11
        };

        _itemActiveStyle = new GUIStyle(_itemStyle);
        _itemActiveStyle.normal.background = _itemActiveStyle.onNormal.background;

        _removeButtonStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            fixedWidth = 18,
            fixedHeight = 18,
            padding = new RectOffset(0, 0, 0, 2)
        };

        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 11,
            padding = new RectOffset(6, 0, 0, 0)
        };
    }

    private void OnGUI()
    {
        InitStyles();
        DrawToolbar();
        DrawList();
        HandleDragAndDrop();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($"Objects ({objects.Count})", _headerStyle);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(new GUIContent("+", "Add slot"), EditorStyles.toolbarButton, GUILayout.Width(24)))
        {
            objects.Add(null);
        }
        if (GUILayout.Button(new GUIContent("Clear", "Remove all"), EditorStyles.toolbarButton, GUILayout.Width(40)))
        {
            if (objects.Count == 0 || EditorUtility.DisplayDialog("Clear All", "Remove all objects from the list?", "Yes", "No"))
                objects.Clear();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawList()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (objects.Count == 0)
        {
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUIStyle centeredLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 11 };
            GUILayout.Label("Drag & drop objects here\nor click + to add a slot", centeredLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        int indexToRemove = -1;

        for (int i = 0; i < objects.Count; i++)
        {
            Object obj = objects[i];
            bool isSelected = obj != null && Selection.activeObject == obj;

            Rect itemRect = EditorGUILayout.BeginHorizontal(isSelected ? _itemActiveStyle : _itemStyle, GUILayout.Height(24));

            // Object field (main interaction - always visible)
            Object newObj = EditorGUILayout.ObjectField(obj, typeof(Object), true, GUILayout.Height(18));
            if (newObj != obj)
                objects[i] = newObj;

            // Remove button
            if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(20), GUILayout.Height(18)))
                indexToRemove = i;

            EditorGUILayout.EndHorizontal();
        }

        if (indexToRemove >= 0)
            objects.RemoveAt(indexToRemove);

        EditorGUILayout.EndScrollView();
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
                        if (obj != null && !objects.Contains(obj))
                            objects.Add(obj);
                    }
                }
                break;
        }
    }
}
