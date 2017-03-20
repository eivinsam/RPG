using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace RPG.UI
{
    internal interface IDataPanel<DataT>
    {
        DataT Data { get; }
    }

    internal abstract class TabPanel<Data, Sub> : DockPanel
        where Data : Model.NamedData
        where Sub : UIElement, IDataPanel<Data>
    {
        protected class Tab : FlatButton
        {
            public readonly Data data;

            public Tab(TabPanel<Data, Sub> panel, Data d)
            {
                data = d;

                Width = 180;
                Margin = new Thickness(10.0, 5.0, 10.0, 5.0);
                Content = d.name;
                Background = new SolidColorBrush(Colors.LightSkyBlue);
                HorizontalContentAlignment = HorizontalAlignment.Left;
                ContextMenu = new ContextMenu();

                ContextMenu.Items.Add(new MenuItem("Delete", (s, e) => panel.Remove(data)));
            }
        }

        static FlatButton CreateButton(Brush brush, object content, RoutedEventHandler click)
            => new FlatButton(click)
            {
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = brush,
                Content = content
            };
        static FlatButton CreateButton(Color color, object content, RoutedEventHandler click)
            => CreateButton(new SolidColorBrush(color), content, click);

        FrameworkElement header;
        readonly StackPanel tabs = new StackPanel { Width = 200 };
        readonly Label no_entry = new Label { FontSize = 18, Margin = new Thickness(5.0) };
        readonly FlatButton new_entry;
        readonly FlatButton new_cancel;
        readonly List<Data> data;

        protected string NewEntryText { set => new_entry.Content = value; }
        protected string NoEntryText { set => no_entry.Content = value; }
        protected int NoEntryWidth { set => no_entry.Width = value; }

        public FrameworkElement Header { get => header; set { tabs.Children.Remove(header); header = value; tabs.Children.Insert(0, header); } }
        public Sub SubPanel { get => Children[1] as Sub; }

        public TabPanel(List<Data> d, Dock tab_dock)
        {
            data = d;

            new_entry  = CreateButton(Colors.AliceBlue,      "New "+typeof(Data).Name, (s, e) => ShowNewOptions());
            new_cancel = CreateButton(Colors.BlanchedAlmond, "Cancel",                 (s, e) => ShowEntries());

            SetDock(tabs, tab_dock);
            Children.Add(tabs);
            Children.Add(no_entry);

            header = new Label { Content = typeof(Data).Name, FontSize = 18, Margin = new Thickness(5.0) };

            ShowEntries();
        }

        private void ShowEntries()
        {
            tabs.Children.Clear();
            tabs.Children.Add(header);
            tabs.Children.Add(new_entry);
            foreach (var item in data)
                Add(item);
        }
        private void ShowNewOptions()
        {
            tabs.Children.Clear();
            tabs.Children.Add(header);
            foreach (var o in NewEntryOptions())
                tabs.Children.Add(CreateButton(new_entry.Background, o, (s, e) =>
                {
                    ShowEntries();
                    var new_data = NewEntry(o);
                    data.Add(new_data);
                    Select(Add(new_data));
                    NewEntryCreated();
                }));
            tabs.Children.Add(new_cancel);
        }

        protected abstract string[] NewEntryOptions();
        protected abstract Data NewEntry(string option);

        protected abstract Sub CreateSub(Tab t);

        protected virtual void NewEntryCreated() { }

        Tab Add(Data d)
        {
            var new_tab = new Tab(this, d);
            new_tab.Click += (s, e) => Select(new_tab);
            tabs.Children.Insert(tabs.Children.Count - 1, new_tab);
            return new_tab;
        }
        void Remove(Data d)
        {
            tabs.Children.Remove(tabs.Children.OfType<Tab>().Where((t) => t.data == d).Single());
            data.Remove(d);
            if (Children.OfType<Sub>().Any(s => s.Data == d))
                Select(null);
        }
        void Select(Tab t)
        {
            Children.RemoveAt(1);
            Children.Add(t == null ? (UIElement)no_entry : CreateSub(t));
        }
        public void Update(Data d)
        {
            foreach (var tab in tabs.Children.OfType<Tab>().Where(t => t.data == d))
                tab.Content = d.name;
        }
    }
}
