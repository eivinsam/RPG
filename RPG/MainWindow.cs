using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RPG
{
    class Context
    {
        public Model world;

        public MainWindow ui;
    }

    delegate void NumberChange(int value);

    class MenuItem : System.Windows.Controls.MenuItem
    {
        public MenuItem(string header, RoutedEventHandler click_handler)
        {
            Header = header;
            Click += click_handler;
        }
    }

    class FlatButton : Button
    {
        private static Style style;

        public FlatButton()
        {
            if (style == null)
                style = FindResource(ToolBar.ButtonStyleKey) as Style;
            Style = style;
        }
        public FlatButton(RoutedEventHandler click_handler) : this()
        {
            Click += click_handler;
        }
    }

    class ClickableLabel : Label
    {
        public event RoutedEventHandler Click;
        public event RoutedEventHandler RightClick;

        public ClickableLabel()
        {
            MouseLeftButtonDown += (s, e) => { e.Handled = true; CaptureMouse(); };
            MouseLeftButtonUp += (s, e) =>
            {
                if (!IsMouseCaptured)
                    return;
                ReleaseMouseCapture();
                if (InputHitTest(e.GetPosition(this)) != null)
                    Click?.Invoke(s, e);
                e.Handled = true;
            };
            MouseRightButtonDown += (s, e) => { e.Handled = true; CaptureMouse(); };
            MouseRightButtonUp += (s, e) =>
            {
                if (!IsMouseCaptured)
                    return;
                ReleaseMouseCapture();
                if (InputHitTest(e.GetPosition(this)) != null)
                    RightClick?.Invoke(s, e);
                e.Handled = true;
            };
        }
    }

    class NumberBox : TextBox
    {
        public event NumberChange Change;

        private bool ignore_change = false;
        public int Value
        {
            get { int.TryParse(Text, out int result); return result; }
            set { ignore_change = true;  Text = value == 0 ? "" : value.ToString(); ignore_change = false; }
        }
        public NumberBox()
        {
            Width = 30;
            Height = Double.NaN;
            TextAlignment = TextAlignment.Center;
            TextWrapping = TextWrapping.NoWrap;
            HorizontalAlignment = HorizontalAlignment.Right;
            VerticalAlignment = VerticalAlignment.Center;

            PreviewTextInput += (s, e) => 
            { foreach (char ch in e.Text) if (!Char.IsDigit(ch)) e.Handled = true; };

            TextChanged += (s, e) =>
            {
                if (ignore_change)
                    return;
                Change?.Invoke(Text == "" ? 0 : int.Parse(Text));
            };
        }
    }


    class PlacePanel : TabPanel<Place, CharacterPanel>
    {
        public PlacePanel(List<Place> d, Dock tab_dock) : base(d, tab_dock)
        {
            NewEntryText = "New location";
            NoEntryText = "No location selected";
            NoEntryWidth = 390;
            ((Label)Header).Content = "Location";
        }

        override protected CharacterPanel CreateSub(Tab t)
        {
            var header = new TextBox
            {
                Margin = Header.Margin, Padding = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                FontSize = 18, Text = t.data.name
            };
            header.TextChanged += (s, e) =>
            {
                t.Content = header.Text;
                t.data.name = header.Text;
            };
            return new CharacterPanel(t.data, Dock.Left) { Header = header };
        }

        protected override Place NewEntry(string option)
        {
            return new Place();
        }

        protected override string[] NewEntryOptions()
        {
            return new string[]{ "Blank" };
        }

        protected override void NewEntryCreated()
        {
            ((TextBox)SubPanel.Header).Focus();
        }
    }

    class ItemPanel : StackPanel
    {
        class List : StackPanel
        {
            class Entry : FlatButton
            {
                public string ItemName { get => (string)Content; set => Content = value; }
                public Entry(string name)
                {
                    Margin = new Thickness(10.0, 5.0, 10.0, 5.0);
                    Content = name;
                    Background = new SolidColorBrush(Colors.LightGoldenrodYellow);
                    HorizontalAlignment = HorizontalAlignment.Stretch;
                    HorizontalContentAlignment = HorizontalAlignment.Left;
                    ContextMenu = new ContextMenu();

                    ContextMenu.Items.Add(new MenuItem("Delete", (s, e) => ((List)Parent).Remove(this)));
                }
            }

            private readonly ItemList items;

            public List(ItemList items)
            {
                this.items = items;

                foreach (var item in items)
                    Children.Add(new Entry(item.Key));
            }

            public void Create()
            {
                Children.Add(new Entry(""));
                items.Add("", new ItemProperties());
            }

            private void Remove(Entry entry)
            {
                items.Remove(entry.ItemName);
                Children.Remove(entry);
            }
        }

        class Info : StackPanel
        {

            public Info()
        }

        readonly FlatButton new_item = new FlatButton
        {
            Margin = new Thickness(10),
            Content = "New item",
            Background = new SolidColorBrush(Colors.AliceBlue)
        };
        readonly List items;

        public ItemPanel(ItemList itemdata)
        {
            items = new List(itemdata);

            Children.Add(new Label { Content = "Items", FontSize = 16 });
            Children.Add(items);
            Children.Add(new_item);

            new_item.Click += (s, e) => items.Create();
        }

    }

    class CenterPanel : TabControl
    {
        public CenterPanel(Context c)
        {
            var turns = new TurnPanel(c.world.party, c.world.places[0]);

            Items.Add(new TabItem { Header = "Turns", Content = turns });
            Items.Add(new TabItem { Header = "Items", Content = new ItemPanel(c.world.itemdata) });
        }
    }

    public class MainWindow : Window
    {
        private readonly Context context;

        internal readonly CharacterPanel party;

        public MainWindow()
        {
            context = new Context() { ui = this };

            Closed += (s, e) => context.world.Write("backup.json");
            context.world = Model.Read("backup.json");

            party = new CharacterPanel(context.world.party, Dock.Right);
            var places = new PlacePanel(context.world.places, Dock.Left);

            var center = new CenterPanel(context);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            grid.Children.Add(party); Grid.SetColumn(party, 0);
            grid.Children.Add(center); Grid.SetColumn(center, 1);
            grid.Children.Add(places); Grid.SetColumn(places, 2);

            Content = grid;
            WindowState = WindowState.Maximized;
        }


        [STAThread]
        public static void Main()
        {
            Random rng = new Random();
            var men = new NameDistribution("GutterFornavnFodte.csv");
            for (int i = 0; i < 10; ++i)
                men.Next(rng);
            Application app = new Application();

            app.Run(new MainWindow());
        }

    }
}
