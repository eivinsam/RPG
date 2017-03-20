using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows;

namespace RPG.UI
{

    class StatsPanel : StackPanel, IDataPanel<Model.Character>
    {
        class Vitals : DockPanel
        {
            private readonly NumberBox body_box = new NumberBox { Value = 0 };
            private readonly NumberBox mind_box = new NumberBox { Value = 0 };
            private readonly Label body_max = new Label { Content = "/ 0" };
            private readonly Label mind_max = new Label { Content = "/ 0" };

            private readonly Model.Character character;

            public Vitals(Model.Character c)
            {
                character = c;

                LastChildFill = false;
                Children.Add(new Label { Content = "Body" });
                Children.Add(body_box);
                Children.Add(body_max);
                Children.Add(mind_max);
                Children.Add(mind_box);
                Children.Add(new Label { Content = "Mind" });
                for (int i = 0; i < 3; i++) DockPanel.SetDock(Children[i], Dock.Left);
                for (int i = 3; i < 6; i++) DockPanel.SetDock(Children[i], Dock.Right);

                body_box.Change += v => character.body = v;
                mind_box.Change += v => character.mind = v;

                Update();
            }
            public void Update()
            {
                body_box.Value = character.body;
                mind_box.Value = character.mind;
                body_max.Content = "/ " + character.MaxBody;
                mind_max.Content = "/ " + character.MaxMind;
            }
        }
        class Stat : DockPanel
        {
            static readonly Dictionary<string, string> texts = new Dictionary<string, string>
        {
            { "STR", "[STR] Strength" },
            { "DEX", "[DEX] Dexterity" },
            { "NTE", "[NTE] Intelligence" },
            { "EMP", "[EMP] Empathy" },
            { "NTU", "[NTU] Intuition" }
        };

            public readonly string name;
            public readonly NumberBox value = new NumberBox();

            public Stat(string name)
            {
                this.name = name;
                Children.Add(value, Dock.Right);
                Children.Add(new Label() { Content = texts.Keys.Contains(name) ? texts[name] : name, Padding = new Thickness(3) });
                Margin = new Thickness(2.0);
            }
        }

        internal class Skill : NamedNumberBox
        {
            readonly StatsPanel panel;
            public Skill(string name, int value, StatsPanel p)
                : base(name, value) { panel = p; }

            public override void onRemoval() => panel.RemoveSkill(this);
            public override void onValueChange(int v) => panel.UpdateSkill(name, v);
        }

        class Item : StackPanel
        {
            readonly Model.ItemInstance item;
            public Item(Model.ItemInstance i)
            {
                item = i;

                var button = new FlatButton
                {
                    Content = item.name,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };
                Children.Add(button);

                button.Click += (s, e) =>
                {
                    if (Children.Count == 1)
                    {
                        Children.Add(new Label { Content = "[TEST]" });
                    }
                    else
                        Children.RemoveAt(1);
                };
            }
        }


        internal readonly TextBox name = new HeaderBox();
        private readonly Vitals vitals;
        private readonly ListPanel<Stat> stats = new ListPanel<Stat>();
        private readonly ListPanel<Skill> skills = new ListPanel<Skill>();

        private ListPanel<Item> items = new ListPanel<Item>();

        internal readonly Model.Character character;

        public Model.Character Data => character;


        public StatsPanel(Model.Character c)
        {
            character = c;
            name.Text = character.name;
            vitals = new Vitals(character);

            Width = 200;

            foreach (var n in Model.Character.stat_names)
            {
                var stat = new Stat(n);
                stat.value.Value = character.stats[n];
                stat.value.Change += v => { character.stats[n] = v; vitals.Update(); };
                stats.Add(stat);
            }
            foreach (var s in character.skills)
                AddSkill(s.Key, s.Value);
            foreach (var item in character.items)
                items.Add(new Item(item));
            items.Add(new Item(new Model.ItemInstance { name = "Item of ultimate test" }));

            var add_skill = new PropertyAdder();
            var add_item = new PropertyAdder();

            Children.Add(name);
            Children.Add(vitals);
            Children.Add(stats);
            Children.Add(new Label { Content = "Skills", HorizontalAlignment = HorizontalAlignment.Center, FontSize = 14 });
            Children.Add(skills);
            Children.Add(add_skill);
            Children.Add(new Label { Content = "Items", HorizontalAlignment = HorizontalAlignment.Center, FontSize = 14 });
            Children.Add(items);
            Children.Add(add_item);

            add_skill.Added += (name, accept) =>
            {
                var added = AddSkill(name, 0);
                if (added != null)
                    added.value.Focus();
                else
                    accept.accepted = false;
            };
            add_item.Added += (name, accept) =>
            {
                var item = new Item(new Model.ItemInstance { name = name });
                items.Add(item);
            };
            name.TextChanged += (s, e) =>
            {
                if (character == null)
                    return;
                character.name = name.Text;
                ((CharacterPanel)Parent).Update(character);
            };
        }
        NamedNumberBox AddSkill(string name, int value)
        {
            if (stats.Any(s => s.name == name) || skills.Any(s => s.name == name))
                return null;
            var new_skill = new Skill(name, value, this);
            skills.Add(new_skill);
            return new_skill;
        }
        internal void UpdateSkill(string name, int value) => character.skills[name] = value;
        internal void RemoveSkill(Skill skill)
        {
            skills.Remove(skill);
            character.skills.Remove(skill.name);
        }
    }

    class CharacterPanel : TabPanel<Model.Character, StatsPanel>, IDataPanel<Model.Place>
    {
        private readonly Random rng = new Random();
        readonly Model.Place place;
        public Model.Place Data => place;

        public CharacterPanel(Model.Place p, Dock tab_dock) : base(p.characters, tab_dock)
        {
            place = p;

            NewEntryText = "New character";
            NoEntryText = "No character selected";
            NoEntryWidth = 190;
            ((Label)Header).Content = place.name;
        }

        protected override StatsPanel CreateSub(Tab t) => new StatsPanel(t.data);

        protected override string[]        NewEntryOptions()       => Model.Character.generators.Keys.ToArray();
        protected override Model.Character NewEntry(string option) => Model.Character.generators[option](rng).FillVitals();
    }
}
