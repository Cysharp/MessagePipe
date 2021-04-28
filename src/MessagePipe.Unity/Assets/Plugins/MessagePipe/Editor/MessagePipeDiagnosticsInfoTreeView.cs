#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using UnityEditor.IMGUI.Controls;
using System.Text;
using System.Text.RegularExpressions;

namespace MessagePipe.Editor
{
    public class MessagePipeDiagnosticsInfoTreeViewItem : TreeViewItem
    {
        static Regex removeHref = new Regex("<a href.+>(.+)</a>", RegexOptions.Compiled);

        public int Count { get; set; }
        public string Caller { get; set; }
        public string[] StackTraces { get; set; }

        public MessagePipeDiagnosticsInfoTreeViewItem(int id) : base(id)
        {

        }
    }

    public class MessagePipeDiagnosticsInfoTreeView : TreeView
    {
        const string sortedColumnIndexStateKey = "MessagePipeDiagnosticsInfoTreeView_sortedColumnIndex";

        public IReadOnlyList<TreeViewItem> CurrentBindingItems;
        int trackId = 0;

        public MessagePipeDiagnosticsInfoTreeView()
            : this(new TreeViewState(), new MultiColumnHeader(new MultiColumnHeaderState(new[]
            {
                new MultiColumnHeaderState.Column() { headerContent = new GUIContent("Count"), width = 5},
                new MultiColumnHeaderState.Column() { headerContent = new GUIContent("Caller")},
            })))
        {
        }

        MessagePipeDiagnosticsInfoTreeView(TreeViewState state, MultiColumnHeader header)
            : base(state, header)
        {
            rowHeight = 20;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            header.sortingChanged += Header_sortingChanged;

            header.ResizeToFit();
            Reload();

            header.sortedColumnIndex = SessionState.GetInt(sortedColumnIndexStateKey, 1);
        }

        public void ReloadAndSort()
        {
            var currentSelected = this.state.selectedIDs;
            Reload();
            Header_sortingChanged(this.multiColumnHeader);
            this.state.selectedIDs = currentSelected;
        }

        private void Header_sortingChanged(MultiColumnHeader multiColumnHeader)
        {
            SessionState.SetInt(sortedColumnIndexStateKey, multiColumnHeader.sortedColumnIndex);
            var index = multiColumnHeader.sortedColumnIndex;
            var ascending = multiColumnHeader.IsSortedAscending(multiColumnHeader.sortedColumnIndex);

            var items = rootItem.children.Cast<MessagePipeDiagnosticsInfoTreeViewItem>();

            IOrderedEnumerable<MessagePipeDiagnosticsInfoTreeViewItem> orderedEnumerable;
            switch (index)
            {
                case 0:
                    orderedEnumerable = ascending ? items.OrderBy(item => item.Count) : items.OrderByDescending(item => item.Count);
                    break;
                case 1:
                    orderedEnumerable = ascending ? items.OrderBy(item => item.Caller) : items.OrderByDescending(item => item.Caller);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }

            CurrentBindingItems = rootItem.children = orderedEnumerable.Cast<TreeViewItem>().ToList();
            BuildRows(rootItem);
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { depth = -1 };

            var children = new List<TreeViewItem>();

            if (MessagePipeDiagnosticsInfoWindow.diagnosticsInfo != null)
            {
                var grouped = MessagePipeDiagnosticsInfoWindow.diagnosticsInfo.GroupedByCaller;
                foreach (var item in grouped)
                {
                    var viewItem = new MessagePipeDiagnosticsInfoTreeViewItem(trackId++)
                    {
                        Count = item.Count(),
                        Caller = item.Key,
                        StackTraces = item.ToArray()
                    };
                    children.Add(viewItem);
                }
            }

            CurrentBindingItems = children;
            root.children = CurrentBindingItems as List<TreeViewItem>;
            return root;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as MessagePipeDiagnosticsInfoTreeViewItem;

            for (var visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
            {
                var rect = args.GetCellRect(visibleColumnIndex);
                var columnIndex = args.GetColumn(visibleColumnIndex);

                var labelStyle = args.selected ? EditorStyles.whiteLabel : EditorStyles.label;
                labelStyle.alignment = TextAnchor.MiddleLeft;
                switch (columnIndex)
                {
                    case 0:
                        EditorGUI.LabelField(rect, item.Count.ToString(), labelStyle);
                        break;
                    case 1:
                        EditorGUI.LabelField(rect, item.Caller, labelStyle);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(columnIndex), columnIndex, null);
                }
            }
        }
    }

}

