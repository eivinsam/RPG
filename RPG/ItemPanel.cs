using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RPG.UI
{
    class ItemPanel : StackPanel
    {
        class List : StackPanel
        {
            public class Entry : FlatButton
            {
                public readonly Model.Item item;

                public new List Parent { get => (List)base.Parent; }

                public Entry(Model.Item i)
                {
                    item = i;

                    Margin = new Thickness(10.0, 5.0, 10.0, 5.0);
                    Content = item.name;
                    Background = new SolidColorBrush(Colors.LightGoldenrodYellow);
                    HorizontalAlignment = HorizontalAlignment.Stretch;
                    HorizontalContentAlignment = HorizontalAlignment.Left;
                    ContextMenu = new ContextMenu();

                    ContextMenu.Items.Add(new MenuItem("Delete", (s, e) => Parent.Remove(this)));

                    Click += (s, e) => Parent.Parent.Select(this);
                }
            }

            private readonly Model.NamedList<Model.Item> items;

            public new ItemPanel Parent { get => (ItemPanel)base.Parent; }

            public List(Model.NamedList<Model.Item> items)
            {
                this.items = items;

                foreach (var item in items)
                    Children.Add(new Entry(item));
            }

            public void Create()
            {
                var item = new Model.Item();
                Children.Add(new Entry(item));
                items.Add(item);
            }

            private void Remove(Entry entry)
            {
                items.Remove(entry.item);
                Children.Remove(entry);
            }
        }
        class Info : StackPanel
        {
            internal class Property : NamedNumberBox
            {
                readonly Info panel;
                public Property(string name, int value, Info p)
                    : base(name, value) { panel = p; }

                public override void onRemoval() => panel.RemoveProperty(this);
                public override void onValueChange(int v) => panel.UpdateProperty(name, v);
            }
            public readonly List.Entry entry;

            private readonly ListPanel<Property> properties = new ListPanel<Property>();

            public Info(List.Entry e)
            {
                entry = e;

                var name_box = new HeaderBox { Text = entry.item.name };
                var add_prop = new PropertyAdder();

                foreach (var prop in entry.item.properties)
                    properties.Add(new Property(prop.Key, prop.Value, this));

                Children.Add(name_box);
                Children.Add(properties);
                Children.Add(add_prop);

                name_box.TextChanged += (s, ev) =>
                {
                    entry.item.name = name_box.Text;
                    entry.Content = entry.item.name;
                };
                add_prop.Added += (name, accept) =>
                {
                    if (entry.item.properties.ContainsKey(name))
                    {
                        accept.accepted = false;
                        return;
                    }
                    accept.accepted = true;
                    properties.Add(new Property(name, 0, this));
                };
            }
            internal void UpdateProperty(string name, int value) => entry.item.properties[name] = value;
            internal void RemoveProperty(Property prop)
            {
                properties.Remove(prop);
                entry.item.properties.Remove(prop.name);
            }
        }

        readonly FlatButton new_item = new FlatButton
        {
            Margin = new Thickness(10),
            Content = "New item",
            Background = new SolidColorBrush(Colors.AliceBlue)
        };
        readonly List items;

        void Select(List.Entry entry)
        {
            var old_info = Children[1] as Info;
            if (old_info != null)
            {
                if (old_info.entry == entry)
                    return;
                Children.RemoveAt(1);
            }
            Children.Insert(1, new Info(entry));
        }

        public ItemPanel(Model.NamedList<Model.Item> items)
        {
            this.items = new List(items);

            Children.Add(new Label { Content = "Items", FontSize = 16 });
            Children.Add(this.items);
            Children.Add(new_item);

            new_item.Click += (s, e) => this.items.Create();
        }

    }
}
