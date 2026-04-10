using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ShotSkiMahiD.Behaviors
{
    public static class ListBoxAutoScroll
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(ListBoxAutoScroll),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListBox listBox)
            {
                if ((bool)e.NewValue)
                    listBox.Loaded += OnLoaded;
                else
                    listBox.Loaded -= OnLoaded;
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                ScrollToEnd(listBox);
                if (listBox.Items is INotifyCollectionChanged collectionChanged)
                    collectionChanged.CollectionChanged += (s, args) =>
                    {
                        if (args.Action == NotifyCollectionChangedAction.Add)
                            Application.Current.Dispatcher.BeginInvoke(new Action(() => ScrollToEnd(listBox)));
                    };
            }
        }

        private static void ScrollToEnd(ListBox listBox)
        {
            if (listBox.Items.Count > 0)
            {
                listBox.ScrollIntoView(listBox.Items[listBox.Items.Count - 1]);
                var sv = FindScrollViewer(listBox);
                sv?.ScrollToEnd();
            }
        }

        private static ScrollViewer? FindScrollViewer(DependencyObject element)
        {
            if (element is ScrollViewer sv) return sv;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var result = FindScrollViewer(VisualTreeHelper.GetChild(element, i));
                if (result != null) return result;
            }
            return null;
        }
    }
}
