using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PersonalTaskManagement.ViewModels;

namespace PersonalTaskManagement
{
    public partial class MainWindow : Window
    {
        private const string DragFormat = "AgileFlowTask";

        private Point _dragStart;
        private TaskViewModel? _pendingDrag;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Card_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ContentControl cc && cc.DataContext is TaskViewModel tvm)
            {
                _dragStart = e.GetPosition(this);
                _pendingDrag = tvm;
            }
        }

        private void Card_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _pendingDrag = null;
                return;
            }
            if (_pendingDrag == null) return;

            var pos = e.GetPosition(this);
            if (Math.Abs(pos.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(pos.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            if (sender is not DependencyObject source) return;

            var data = new DataObject(DragFormat, _pendingDrag);
            try
            {
                DragDrop.DoDragDrop(source, data, DragDropEffects.Move);
            }
            finally
            {
                _pendingDrag = null;
            }
        }

        private void Card_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ContentControl cc
                && cc.DataContext is TaskViewModel tvm
                && DataContext is MainViewModel main)
            {
                main.EditTask(tvm);
                e.Handled = true;
            }
        }

        private void Column_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DragFormat)
                ? DragDropEffects.Move
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void Column_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DragFormat)) return;
            if (e.Data.GetData(DragFormat) is not TaskViewModel taskVm) return;
            if (DataContext is not MainViewModel main || main.CurrentBoard == null) return;
            if (sender is not ScrollViewer sv) return;
            if (sv.Tag is not ColumnViewModel toColumn) return;

            var fromColumn = main.CurrentBoard.Columns.FirstOrDefault(c => c.Tasks.Contains(taskVm));
            if (fromColumn == null) return;

            var itemsControl = FindDescendant<ItemsControl>(sv);
            int physicalIndex = toColumn.Tasks.Count;

            if (itemsControl != null)
            {
                var posInItems = e.GetPosition(itemsControl);
                int visibleIndex = ComputeVisibleDropIndex(itemsControl, posInItems);

                if (visibleIndex >= itemsControl.Items.Count)
                {
                    physicalIndex = toColumn.Tasks.Count;
                }
                else if (itemsControl.Items[visibleIndex] is TaskViewModel anchor)
                {
                    physicalIndex = toColumn.Tasks.IndexOf(anchor);
                    if (physicalIndex < 0) physicalIndex = toColumn.Tasks.Count;
                }
            }

            main.MoveTask(taskVm, fromColumn, toColumn, physicalIndex);
            e.Handled = true;
        }

        private static int ComputeVisibleDropIndex(ItemsControl items, Point posInItems)
        {
            for (int i = 0; i < items.Items.Count; i++)
            {
                var container = items.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container == null) continue;
                Point topLeft;
                try
                {
                    topLeft = container.TransformToAncestor(items).Transform(new Point(0, 0));
                }
                catch
                {
                    continue;
                }
                var mid = topLeft.Y + container.ActualHeight / 2;
                if (posInItems.Y < mid) return i;
            }
            return items.Items.Count;
        }

        private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match) return match;
                var deeper = FindDescendant<T>(child);
                if (deeper != null) return deeper;
            }
            return null;
        }
    }
}
