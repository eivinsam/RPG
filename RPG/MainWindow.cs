using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace RPG.UI
{
    static class Extensions
    {
        public static void Add(this UIElementCollection c, UIElement e, Dock d)
        {
            DockPanel.SetDock(e, d);
            c.Add(e);
        }
        public static void RemoveAll<T>(this UIElementCollection c, Func<T,bool> pred)
            where T: UIElement
        {
            foreach (var match in c.OfType<T>().Where(pred).ToArray())
                c.Remove(match);
        }
    }

    class ListPanel<T> : StackPanel
        where T: UIElement
    {
        public void Add(T entry) => Children.Add(entry);
        public void Remove(T entry) => Children.Remove(entry);
        public void RemoveAll(Func<T,bool> pred) => Children.RemoveAll<T>(pred);

        public T Last => Children.Count == 0 ? null : Children[Children.Count - 1] as T;

        public IEnumerable<T> Where(Func<T, bool> pred) => Children.OfType<T>().Where(pred);
        public bool Any(Func<T, bool> pred) => Children.OfType<T>().Any(pred);
        public bool All(Func<T, bool> pred) => Children.OfType<T>().All(pred);
    }

    class Context
    {
        public Model.Model world;

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
        public FlatButton() => Style = (Style)FindResource(ToolBar.ButtonStyleKey);

        public FlatButton(RoutedEventHandler click_handler) : this() => Click += click_handler;
    }

    class HeaderBox : TextBox
    {
        public HeaderBox()
        {
            FontSize = 18;
            Margin = new Thickness(10);
            HorizontalAlignment = HorizontalAlignment.Stretch;
            Height = Double.NaN;
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

    abstract class NamedNumberBox : DockPanel
    {
        public readonly string name;
        public readonly NumberBox value = new NumberBox { Margin = new Thickness(2) };
        private readonly FlatButton desc;

        public NamedNumberBox(string n, int v)
        {
            name = n;
            value.Value = v;
            value.Change += onValueChange;
            desc = new FlatButton
            {
                Content = name,
                Margin = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            desc.ContextMenu = new ContextMenu();
            desc.ContextMenu.Items.Add(new MenuItem("Remove", (s, e) => onRemoval()));

            Children.Add(value, Dock.Right);
            Children.Add(desc);
        }

        public abstract void onValueChange(int v);
        public abstract void onRemoval();
    }

    class Acceptance
    {
        public bool accepted = true;
    }
    delegate void PropertyAdded(string name, Acceptance accept);

    class PropertyAdder : DockPanel
    {
        public event PropertyAdded Added;

        public PropertyAdder()
        {
            var text = new TextBox { Margin = new Thickness(2) };
            var button = new Button { Margin = new Thickness(2), Content = "+", Width = 30 };

            Children.Add(button, Dock.Right);
            Children.Add(text);

            button.Click += (s, e) =>
            {
                var accept = new Acceptance();
                Added?.Invoke(text.Text, accept);
                if (accept.accepted)
                    text.Text = "";
            };
         }
    }


    class PlacePanel : TabPanel<Model.Place, CharacterPanel>
    {
        public PlacePanel(List<Model.Place> d, Dock tab_dock) : base(d, tab_dock)
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

        protected override Model.Place NewEntry(string option) => new Model.Place();

        protected override string[] NewEntryOptions() => new string[]{ "Blank" };
        protected override void NewEntryCreated() => ((TextBox)SubPanel.Header).Focus();
    }

    class CenterPanel : TabControl
    {
        public CenterPanel(Context c)
        {
            var turns = new TurnPanel(c.world.party, c.world.places[0]);

            Items.Add(new TabItem { Header = "Turns", Content = turns });
            Items.Add(new TabItem { Header = "Items", Content = new ItemPanel(c.world.items) });
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
            context.world = Model.Model.Read("backup.json");

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
            new Application().Run(new MainWindow());
        }

    }
}
