using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows;

namespace RPG
{
    class Vitals : DockPanel
    {
        private readonly NumberBox body_box = new NumberBox { Value = 0 };
        private readonly NumberBox mind_box = new NumberBox { Value = 0 };
        private readonly Label body_max = new Label { Content = "/ 0" };
        private readonly Label mind_max = new Label { Content = "/ 0" };

        private readonly Character character;

        public Vitals(Character c)
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
            SetDock(value, Dock.Right);
            Children.Add(value);
            Children.Add(new Label() { Content = texts.Keys.Contains(name) ? texts[name] : name, Padding = new Thickness(3) });
            Margin = new Thickness(2.0);
        }
    }
    class Skill : DockPanel
    {
        public readonly string name;
        public NumberBox value = new NumberBox();

        public Skill(string name)
        {
            this.name = name;
            var desc = new FlatButton
            {
                Content = name,
                Margin = new Thickness(0, 0, 4, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            desc.ContextMenu = new ContextMenu();
            desc.ContextMenu.Items.Add(new MenuItem("Remove", (s, e) => (Parent as StatsPanel).RemSkill(name)));
            SetDock(value, Dock.Right);
            Children.Add(value);
            Children.Add(desc);
            Margin = new Thickness(2.0);
        }

    }
    class StatsPanel : StackPanel, IDataPanel<Character>
    {
        internal readonly TextBox name = new TextBox()
        {
            FontSize = 18,
            Margin = new Thickness(10),
            Width = 180,
            Height = Double.NaN
        };
        private readonly Vitals vitals;

        internal readonly Character character;

        public Character Data => character;

        public StatsPanel(Character c)
        {
            character = c;
            name.Text = character.name;
            vitals = new Vitals(character);

            Children.Add(name);
            Children.Add(vitals);
            foreach (var n in Character.stat_names)
            {
                var stat = new Stat(n);
                stat.value.Value = character.stats[n];
                stat.value.Change += v => { character.stats[n] = v; vitals.Update(); };
                Children.Add(stat);
            }
            var new_skill_name = new TextBox { Margin = new Thickness(0, 0, 4.0, 0) };
            var add_skill = new DockPanel { Margin = new Thickness(2.0) };
            var add_skill_button = new Button { Content = "+", Width = 30 };
            DockPanel.SetDock(add_skill_button, Dock.Right);
            add_skill.Children.Add(add_skill_button);
            add_skill.Children.Add(new_skill_name);
            Children.Add(add_skill);

            foreach (var s in character.skills)
                AddSkill(s.Key, s.Value);

            add_skill_button.Click += (s, e) =>
            {
                if (AddSkill(new_skill_name.Text, 0) != null)
                {
                    (Children[Children.Count - 2] as Skill).value.Focus();
                    new_skill_name.Text = "";
                }
            };
            name.TextChanged += (s, e) =>
            {
                if (character == null)
                    return;
                character.name = name.Text;
                (Parent as CharacterPanel).Update(character);
            };
        }
        Skill AddSkill(string name, int value)
        {
            if (Children.OfType<Stat>().Any(s => s.name == name) || Children.OfType<Skill>().Any(s => s.name == name))
                return null;
            var new_skill = new Skill(name);
            new_skill.value.Value = value;
            new_skill.value.Change += v => character.skills[name] = v;
            Children.Insert(Children.Count - 1, new_skill);
            return new_skill;
        }
        internal void RemSkill(string name)
        {
            var match = Children.OfType<Skill>().SingleOrDefault(s => s.name == name);
            if (match != null)
                Children.Remove(match);
        }
    }

    class CharacterPanel : TabPanel<Character, StatsPanel>, IDataPanel<Place>
    {
        private readonly Random rng = new Random();
        readonly Place place;
        public Place Data => place;

        public CharacterPanel(Place p, Dock tab_dock) : base(p.characters, tab_dock)
        {
            place = p;

            NewEntryText = "New character";
            NoEntryText = "No character selected";
            NoEntryWidth = 190;
            ((Label)Header).Content = place.name;
        }

        protected override StatsPanel CreateSub(Tab t) => new StatsPanel(t.data);

        protected override string[]  NewEntryOptions()       => Character.generators.Keys.ToArray();
        protected override Character NewEntry(string option) => Character.generators[option](rng).FillVitals();
    }
}
