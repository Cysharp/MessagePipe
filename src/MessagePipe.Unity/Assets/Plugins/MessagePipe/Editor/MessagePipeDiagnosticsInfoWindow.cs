#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MessagePipe.Editor
{
    public class MessagePipeDiagnosticsInfoWindow : EditorWindow
    {
        static readonly string Splitter = "---" + Environment.NewLine;

        static int interval;

        static MessagePipeDiagnosticsInfoWindow window;

        internal static MessagePipeDiagnosticsInfo diagnosticsInfo;


        [MenuItem("Window/MessagePipe Diagnostics")]
        public static void OpenWindow()
        {
            if (window != null)
            {
                window.Close();
            }

            // will called OnEnable(singleton instance will be set).
            GetWindow<MessagePipeDiagnosticsInfoWindow>("MessagePipe Diagnostics").Show();
        }

        static readonly GUILayoutOption[] EmptyLayoutOption = new GUILayoutOption[0];

        MessagePipeDiagnosticsInfoTreeView treeView;
        object splitterState;

        void OnEnable()
        {
            window = this; // set singleton.
            splitterState = SplitterGUILayout.CreateSplitterState(new float[] { 75f, 25f }, new int[] { 32, 32 }, null);
            treeView = new MessagePipeDiagnosticsInfoTreeView();
            EnableAutoReload = EditorPrefs.GetBool("MessagePipeDiagnosticsInfoWindow.EnableAutoReload", false);
        }

        void OnGUI()
        {
            // Head
            RenderHeadPanel();

            // Splittable
            SplitterGUILayout.BeginVerticalSplit(this.splitterState, EmptyLayoutOption);
            {
                // Column Tabble
                RenderTable();

                // StackTrace details
                RenderDetailsPanel();
            }
            SplitterGUILayout.EndVerticalSplit();
        }

        #region HeadPanel

        static bool EnableAutoReload { get; set; }
        static bool EnableCaptureStackTrace { get; set; }
        internal static bool EnableCollapse { get; set; } = true;
        static readonly GUIContent EnableAutoReloadHeadContent = EditorGUIUtility.TrTextContent("Enable AutoReload", "Reload view automatically.", (Texture)null);
        static readonly GUIContent EnableCaptureStackTraceHeadContent = EditorGUIUtility.TrTextContent("Enable CaptureStackTrace", "CaptureStackTrace on Subscribe.", (Texture)null);
        static readonly GUIContent EnableCollapseHeadContent = EditorGUIUtility.TrTextContent("Collapse", "Collapse StackTraces.", (Texture)null);
        static readonly GUIContent ReloadHeadContent = EditorGUIUtility.TrTextContent("Reload", "Reload View.", (Texture)null);

        // [Enable CaptureStackTrace] | [Enable AutoReload] | .... | Reload
        void RenderHeadPanel()
        {
            EditorGUILayout.BeginVertical(EmptyLayoutOption);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, EmptyLayoutOption);

            // lazy initialize...
            if (diagnosticsInfo == null)
            {
                if (GlobalMessagePipe.IsInitialized)
                {
                    diagnosticsInfo = GlobalMessagePipe.DiagnosticsInfo;
                    EnableCaptureStackTrace = diagnosticsInfo.MessagePipeOptions.EnableCaptureStackTrace;
                }
            }

            if (GUILayout.Toggle(EnableCaptureStackTrace, EnableCaptureStackTraceHeadContent, EditorStyles.toolbarButton, EmptyLayoutOption) != EnableCaptureStackTrace)
            {
                if (CheckInitialized())
                {
                    diagnosticsInfo.MessagePipeOptions.EnableCaptureStackTrace = EnableCaptureStackTrace = !EnableCaptureStackTrace;
                }
            }

            if (GUILayout.Toggle(EnableCollapse, EnableCollapseHeadContent, EditorStyles.toolbarButton, EmptyLayoutOption) != EnableCollapse)
            {
                if (CheckInitialized())
                {
                    EnableCollapse = !EnableCollapse;
                    treeView.ReloadAndSort();
                    Repaint();
                }
            }

            if (GUILayout.Toggle(EnableAutoReload, EnableAutoReloadHeadContent, EditorStyles.toolbarButton, EmptyLayoutOption) != EnableAutoReload)
            {
                if (CheckInitialized())
                {
                    EnableAutoReload = !EnableAutoReload;
                    EditorPrefs.SetBool("MessagePipeDiagnosticsInfoWindow.EnableAutoReload", EnableAutoReload);
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(ReloadHeadContent, EditorStyles.toolbarButton, EmptyLayoutOption))
            {
                if (CheckInitialized())
                {
                    treeView.ReloadAndSort();
                    Repaint();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        bool CheckInitialized()
        {
            if (diagnosticsInfo == null)
            {
                Debug.LogError("MessagePackDiagnosticsInfo is not set. Should call GlobalMessagePipe.SetProvider on startup.");
                return false;
            }
            return true;
        }

        #endregion

        #region TableColumn

        Vector2 tableScroll;
        GUIStyle tableListStyle;

        void RenderTable()
        {
            if (tableListStyle == null)
            {
                tableListStyle = new GUIStyle("CN Box");
                tableListStyle.margin.top = 0;
                tableListStyle.padding.left = 3;
            }

            EditorGUILayout.BeginVertical(tableListStyle, EmptyLayoutOption);

            this.tableScroll = EditorGUILayout.BeginScrollView(this.tableScroll, new GUILayoutOption[]
            {
                GUILayout.ExpandWidth(true),
                GUILayout.MaxWidth(2000f)
            });
            var controlRect = EditorGUILayout.GetControlRect(new GUILayoutOption[]
            {
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true)
            });


            treeView?.OnGUI(controlRect);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void Update()
        {
            if (diagnosticsInfo != null && EnableAutoReload)
            {
                if (interval++ % 120 == 0)
                {
                    if (diagnosticsInfo.CheckAndResetDirty())
                    {
                        treeView.ReloadAndSort();
                        Repaint();
                    }
                }
            }
        }

        #endregion

        #region Details

        static GUIStyle detailsStyle;
        Vector2 detailsScroll;

        void RenderDetailsPanel()
        {
            if (detailsStyle == null)
            {
                detailsStyle = new GUIStyle("CN Message");
                detailsStyle.wordWrap = false;
                detailsStyle.stretchHeight = true;
                detailsStyle.margin.right = 15;
            }

            string message = "";
            var selected = treeView.state.selectedIDs;
            if (selected.Count > 0)
            {
                var first = selected[0];
                var item = treeView.CurrentBindingItems.FirstOrDefault(x => x.id == first) as MessagePipeDiagnosticsInfoTreeViewItem;
                if (item != null)
                {
                    var now = DateTimeOffset.UtcNow;
                    message = string.Join(Splitter, item.StackTraces
                        .Select(x =>
                            "Subscribe at " + x.Timestamp.ToLocalTime().ToString("HH:mm:ss.ff") // + ", Elapsed: " + (now - x.Timestamp).TotalSeconds.ToString("00.00")
                            + Environment.NewLine
                            + (x.formattedStackTrace ?? (x.formattedStackTrace = x.StackTrace.CleanupAsyncStackTrace()))));
                }
            }

            detailsScroll = EditorGUILayout.BeginScrollView(this.detailsScroll, EmptyLayoutOption);
            var vector = detailsStyle.CalcSize(new GUIContent(message));
            EditorGUILayout.SelectableLabel(message, detailsStyle, new GUILayoutOption[]
            {
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true),
                GUILayout.MinWidth(vector.x),
                GUILayout.MinHeight(vector.y)
            });
            EditorGUILayout.EndScrollView();
        }

        #endregion
    }
}

