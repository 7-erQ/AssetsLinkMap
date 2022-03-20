using System.Reflection.Emit;
using System.Net.Mime;
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;


namespace AssetsLinkMap
{
    [ExecuteInEditMode]
    public class AssetsLinkMapWindow : EditorWindow
    {
        private static AssetsLinkMapWindow _instance;
        public static AssetsLinkMapWindow Instance
        {
            get { return _instance; }
        }
        public UnityEngine.Object m_targetObj;
        private AssetsLinkMapGraph m_GraphView;
        private AssetsLinkMapGraphDrawer m_graphDrawer;
        private AssetsLinkMapSettings _settings;
        internal AssetsLinkMapSettings m_settings
        {
            get { return _settings; }
            set { _settings = value; }
        }

        private AssetsLinkMapResolver m_resolver;
        private VisualElement m_toolBar;
        private List<Button> m_sceneSearchBtnList = new List<Button>();


        [MenuItem("GameObject/查看引用、依赖关系", priority = 10)]
        private static void ShowAssetsLinkMapWindowFromSceneMenu(MenuCommand menuCommand)
        {
            ShowAssetsLinkMapWindow();
        }

        [MenuItem("Assets/查看引用、依赖关系")]
        public static void ShowAssetsLinkMapWindow()
        {
            AssetsLinkMapWindow window = EditorWindow.GetWindow<AssetsLinkMapWindow>("Assets Link Map");
            window.minSize = new Vector2(800f, 600f);
            window.Init(Selection.activeObject);
        }

        public void Init(UnityEngine.Object targetObject)
        {
            EditorUtility.DisplayProgressBar("初始化视图中...", "", 0);

            m_targetObj = targetObject;
            if (m_GraphView != null)
            {
                m_GraphView.Clear();
                rootVisualElement.Remove(m_GraphView);
            }
            m_GraphView = new AssetsLinkMapGraph(m_targetObj)
            {
                name = "Asset Link Map Graph",
            };
            rootVisualElement.Add(m_GraphView);

            if (_settings == null)
            {
                _settings = new AssetsLinkMapSettings();
                _settings.Load();
            }
            m_graphDrawer = new AssetsLinkMapGraphDrawer(m_GraphView, m_settings, this);
            m_resolver = new AssetsLinkMapResolver(m_GraphView, m_settings);
            if (m_toolBar == null)
                BuildToolBar();
            BuildGraph();

            EditorApplication.delayCall += () =>
            {
                EditorUtility.ClearProgressBar();
            };
        }

        private void OnEnable()
        {
            _instance = this;
        }

        private void OnDisable()
        {
            _settings.Save();
        }

        void BuildGraph()
        {
            m_resolver.BuildGraph();
            EditorUtility.DisplayProgressBar("初始化视图中...", "", 50);
            m_graphDrawer.Draw();
            EditorUtility.DisplayProgressBar("初始化视图中...", "", 80);
            m_toolBar.BringToFront();
        }

        void BuildToolBar()
        {
            var toolBar = new VisualElement
            {
                style =
            {
                flexDirection = FlexDirection.Row,
                flexWrap = Wrap.NoWrap,
                backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.75f)
            }
            };

            var leftSettingbarContainer = new VisualElement
            {
                style =
            {
                flexDirection = FlexDirection.Column,
                flexWrap = Wrap.Wrap,
                flexShrink = 1,
                flexGrow = 1,
                overflow = Overflow.Hidden
            }
            };

            var firstLine = new VisualElement
            {
                style = {
                flexDirection = FlexDirection.Row,
            }
            };
            var searchDepthSettingField = new TextField("Depth", 1, false, false, '*')
            {
                value = _settings.DependenciesDepth.ToString(),
                style = {
                minWidth = 50f
            },
                tooltip = "查找深度"
            };
            searchDepthSettingField.labelElement.style.minWidth = 20f;
            searchDepthSettingField.ElementAt(1).style.minWidth = 30f;
            searchDepthSettingField.RegisterValueChangedCallback<string>(OnSearchDepthSettingTextChange);

            firstLine.Add(AddToolBarFilterToggle("Find References", "Find References", m_settings.FindReferences, "查找引用"));
            firstLine.Add(AddToolBarFilterToggle("Find Dependencies", "Find Dependencies", m_settings.FindDependencies, "查找依赖"));
            firstLine.Add(searchDepthSettingField);
            firstLine.Add(AddToolBarFilterToggle("Distinct Dependencies MonoScripts", "Distinct Dependencies MonoScripts", m_settings.DistinctDependenceMonoScript, "是否合并显示依赖的MonoScript"));
            firstLine.Add(AddToolBarFilterToggle("Distinct Renferences Objects", "Distinct Renferences Objects", m_settings.DistinctReferenceObjects, "是否合并显示引用该项的对象"));
            leftSettingbarContainer.Add(firstLine);

            var secondLine = new VisualElement
            {
                style = {
                flexDirection = FlexDirection.Row,
            }
            };

            secondLine.Add(AddToolBarFilterToggle("Hide ScriptableObject", "Hide ScriptableObject", (_settings.ObjectTypesFilter & AssetsLinkMapSettings.ObjectType.ScriptableObject) == 0));
            secondLine.Add(AddToolBarFilterToggle("Hide Component", "Hide Component", (_settings.ObjectTypesFilter & AssetsLinkMapSettings.ObjectType.Component) == 0));
            secondLine.Add(AddToolBarFilterToggle("Hide MonoScript", "Hide MonoScript", (_settings.ObjectTypesFilter & AssetsLinkMapSettings.ObjectType.MonoScript) == 0));
            secondLine.Add(AddToolBarFilterToggle("Hide Material", "Hide Material", (_settings.ObjectTypesFilter & AssetsLinkMapSettings.ObjectType.Material) == 0));
            secondLine.Add(AddToolBarFilterToggle("Hide Texture2D", "Hide Texture2D", (_settings.ObjectTypesFilter & AssetsLinkMapSettings.ObjectType.Texture2D) == 0));
            secondLine.Add(AddToolBarFilterToggle("Hide Shader", "Hide Shader", (_settings.ObjectTypesFilter & AssetsLinkMapSettings.ObjectType.Shader) == 0));
            secondLine.Add(AddToolBarFilterToggle("Hide Font", "Hide Font", (_settings.ObjectTypesFilter & AssetsLinkMapSettings.ObjectType.Font) == 0));
            secondLine.Add(AddToolBarFilterToggle("Hide Sprite", "Hide Sprite", (_settings.ObjectTypesFilter & AssetsLinkMapSettings.ObjectType.Sprite) == 0));
            leftSettingbarContainer.Add(secondLine);

            var thirdLine = new VisualElement
            {
                style = {
                flexDirection = FlexDirection.Row,
            }
            };
            var skipFileSettingField = new TextField("Skip Files")
            {
                value = _settings.ExcludeAssetFilters,
                style = {
                minWidth = 120f,
                marginRight = 20f
            },
                tooltip = "忽略的文件类型"
            };
            skipFileSettingField.labelElement.style.minWidth = 20f;
            skipFileSettingField.ElementAt(1).style.minWidth = 100f;
            skipFileSettingField.RegisterValueChangedCallback<string>(OnSkipFileSettingTextChange);
            thirdLine.Add(skipFileSettingField);

            var limitSearchFolder = new TextField("Search In Folder")
            {
                value = _settings.ReferencesAssetDirectories[0],
                // value = "",
                style = {
                minWidth = 120f
            },
                tooltip = "限制搜索目录范围"
            };
            limitSearchFolder.labelElement.style.minWidth = 20f;
            limitSearchFolder.ElementAt(1).style.minWidth = 150f;
            limitSearchFolder.RegisterValueChangedCallback<string>(OnLimitSearchFolderTextChange);
            thirdLine.Add(limitSearchFolder);

            leftSettingbarContainer.Add(thirdLine);

            var fourthLine = new VisualElement
            {
                style = {
                flexDirection = FlexDirection.Row,
            }
            };

            fourthLine.Add(CreateSceneSearchButton("No Search Scene", "No Search Scene", _settings.SceneSearchType != AssetsLinkMapSettings.SceneSearchMode.NoSearch));
            fourthLine.Add(CreateSceneSearchButton("Search Current Scene", "Search Current Scene", _settings.SceneSearchType != AssetsLinkMapSettings.SceneSearchMode.SearchOnlyInCurrentScene));
            fourthLine.Add(CreateSceneSearchButton("Search Everywhere", "Search Everywhere", _settings.SceneSearchType != AssetsLinkMapSettings.SceneSearchMode.SearchEverywhere));
            leftSettingbarContainer.Add(fourthLine);

            var rightBtnbarContainer = new VisualElement
            {
                style =
            {
                minWidth = 100f,
                flexDirection = FlexDirection.Column,
                flexWrap = Wrap.Wrap,
                flexGrow = 0,
                flexShrink = 0,
                paddingTop = 10f,
                paddingBottom = 10f,
                backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.7f)
            }
            };

            rightBtnbarContainer.Add(AddToolBarFilterToggle("Search immediately", "Search immediately", m_settings.DoSearchOnceSettingChange, "是否在设置发生变化后立即搜索"));
            rightBtnbarContainer.Add(new Button(() =>
            {
                Init(m_targetObj);
            })
            {
                text = "Apply"
            });

            toolBar.Add(leftSettingbarContainer);
            toolBar.Add(rightBtnbarContainer);

            m_toolBar = toolBar;
            rootVisualElement.Add(toolBar);
        }

        private Toggle AddToolBarFilterToggle(string _label, string _name, bool _initValue = false, string _tip = "")
        {
            var toggle = new Toggle(_label)
            {
                style = {
                flexGrow = 0,
                marginRight = 14f
            }
            };
            toggle.labelElement.style.minWidth = 50f;
            toggle.name = _name;
            toggle.value = _initValue;
            if (!string.IsNullOrWhiteSpace(_tip))
                toggle.tooltip = _tip;
            toggle.RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
            return toggle;
        }

        private Button CreateSceneSearchButton(string _text, string _name, bool _initEnable = false, string _tip = "")
        {
            var button = new Button()
            {
                text = _text
            };
            button.name = _name;
            if (!string.IsNullOrWhiteSpace(_tip))
                button.tooltip = _tip;
            button.SetEnabled(_initEnable);
            button.RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
            m_sceneSearchBtnList.Add(button);
            return button;
        }

        void OnSearchDepthSettingTextChange(ChangeEvent<string> ev)
        {
            if (string.IsNullOrWhiteSpace(ev.newValue) || ev.newValue == ev.previousValue)
            {
                return;
            }
            if (int.TryParse(ev.newValue, out int newDepth))
            {
                _settings.DependenciesDepth = newDepth;
                if (_settings.DoSearchOnceSettingChange)
                    Init(m_targetObj);
            }
        }

        void OnSkipFileSettingTextChange(ChangeEvent<string> ev)
        {
            _settings.ExcludeAssetFilters = ev.newValue;
        }

        void OnLimitSearchFolderTextChange(ChangeEvent<string> ev)
        {
            _settings.ReferencesAssetDirectories[0] = ev.newValue;
        }

        // void OnNodeSearchTextChange(ChangeEvent<string> ev)
        // {
        //     m_GraphView.Query("SCLanguage");
        // }

        void OnMouseUpEvent(MouseEventBase<MouseUpEvent> evt)
        {
            if (evt.target is UnityEngine.UIElements.Toggle)
            {
                OnToggleClick((UnityEngine.UIElements.Toggle)evt.target);
            }
            if (evt.target is UnityEngine.UIElements.Button)
            {
                OnButtonClick((UnityEngine.UIElements.Button)evt.target);
            }
        }

        private void OnToggleClick(Toggle target)
        {
            switch (target.name)
            {
                case "Hide ScriptableObject":
                    _settings.ObjectTypesFilter = target.value == true ? (_settings.ObjectTypesFilter & ~AssetsLinkMapSettings.ObjectType.ScriptableObject) : (_settings.ObjectTypesFilter | AssetsLinkMapSettings.ObjectType.ScriptableObject);
                    break;
                case "Hide Component":
                    _settings.ObjectTypesFilter = target.value == true ? (_settings.ObjectTypesFilter & ~AssetsLinkMapSettings.ObjectType.Component) : (_settings.ObjectTypesFilter | AssetsLinkMapSettings.ObjectType.Component);
                    break;
                case "Hide MonoScript":
                    _settings.ObjectTypesFilter = target.value == true ? (_settings.ObjectTypesFilter & ~AssetsLinkMapSettings.ObjectType.MonoScript) : (_settings.ObjectTypesFilter | AssetsLinkMapSettings.ObjectType.MonoScript);
                    break;
                case "Hide Material":
                    _settings.ObjectTypesFilter = target.value == true ? (_settings.ObjectTypesFilter & ~AssetsLinkMapSettings.ObjectType.Material) : (_settings.ObjectTypesFilter | AssetsLinkMapSettings.ObjectType.Material);
                    break;
                case "Hide Texture2D":
                    _settings.ObjectTypesFilter = target.value == true ? (_settings.ObjectTypesFilter & ~AssetsLinkMapSettings.ObjectType.Texture2D) : (_settings.ObjectTypesFilter | AssetsLinkMapSettings.ObjectType.Texture2D);
                    break;
                case "Hide Shader":
                    _settings.ObjectTypesFilter = target.value == true ? (_settings.ObjectTypesFilter & ~AssetsLinkMapSettings.ObjectType.Shader) : (_settings.ObjectTypesFilter | AssetsLinkMapSettings.ObjectType.Shader);
                    break;
                case "Hide Font":
                    _settings.ObjectTypesFilter = target.value == true ? (_settings.ObjectTypesFilter & ~AssetsLinkMapSettings.ObjectType.Font) : (_settings.ObjectTypesFilter | AssetsLinkMapSettings.ObjectType.Font);
                    break;
                case "Hide Sprite":
                    _settings.ObjectTypesFilter = target.value == true ? (_settings.ObjectTypesFilter & ~AssetsLinkMapSettings.ObjectType.Sprite) : (_settings.ObjectTypesFilter | AssetsLinkMapSettings.ObjectType.Sprite);
                    break;
                case "Find References":
                    _settings.FindReferences = target.value;
                    break;
                case "Find Dependencies":
                    _settings.FindDependencies = target.value;
                    break;
                case "Distinct Dependencies MonoScripts":
                    _settings.DistinctDependenceMonoScript = target.value;
                    break;
                case "Distinct Renferences Objects":
                    _settings.DistinctReferenceObjects = target.value;
                    break;
                case "Search immediately":
                    _settings.DoSearchOnceSettingChange = target.value;
                    return;
                default:
                    return;
            }
            if (_settings.DoSearchOnceSettingChange)
                Init(m_targetObj);
        }

        private void OnButtonClick(Button target)
        {
            switch (target.name)
            {
                case "No Search Scene":
                    _settings.SceneSearchType = AssetsLinkMapSettings.SceneSearchMode.NoSearch;
                    break;
                case "Search Current Scene":
                    _settings.SceneSearchType = AssetsLinkMapSettings.SceneSearchMode.SearchOnlyInCurrentScene;
                    break;
                case "Search Everywhere":
                    _settings.SceneSearchType = AssetsLinkMapSettings.SceneSearchMode.SearchEverywhere;
                    break;
                default:
                    return;
            }
            foreach (var button in m_sceneSearchBtnList)
            {
                if (button.GetHashCode() == target.GetHashCode())
                {
                    button.SetEnabled(false);
                }
                else
                {
                    button.SetEnabled(true);
                }
            }
            if (_settings.DoSearchOnceSettingChange)
                Init(m_targetObj);
        }
    }
}