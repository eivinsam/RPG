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
            Children.Add(value, Dock.Right);
            Children.Add(new Label() { Content = texts.Keys.Contains(name) ? texts[name] : name, Padding = new Thickness(3) });
            Margin = new Thickness(2.0);
        }
    }

    class Skill : NamedNumberBox
    {
        readonly StatsPanel panel;
        public Skill(string name, int value, StatsPanel p) 
            : base(name, value) { panel = p; }

        public override void onRemoval() => panel.RemSkill(this);
        public override void onValueChange(int v) => panel.updateSkill(name, v);
    }

    class StatsPanel : StackPanel, IDataPanel<Character>
    {
        internal readonly TextBox name = new HeaderBox();
        private readonly Vitals vitals;
        private readonly ListPanel<Stat> stats = new ListPanel<Stat>();
        private readonly ListPanel<Skill> skills = new ListPanel<Skill>();

        internal readonly Character character;

        public Character Data => character;


        public StatsPanel(Character c)
        {
            character = c;
            name.Text = character.name;
            vitals = new Vitals(character);

            Width = 200;

            foreach (var n in Character.stat_names)
            {
                var stat = new Stat(n);
                stat.value.Value = character.stats[n];
                stat.value.Change += v => { character.stats[n] = v; vitals.Update(); };
                stats.Add(stat);
            }
            foreach (var s in character.skills)
                AddSkill(s.Key, s.Value);

            Children.Add(name);
            Children.Add(vitals);
            Children.Add(stats);
            Children.Add(skills);
            var add_skill = new PropertyAdder();
            Children.Add(add_skill);


            add_skill.Added += (name, accept) =>
            {
                var added = AddSkill(name, 0);
                if (added != null)
                    added.value.Focus();
                else
                    accept.accepted = false;
            };
            name.TextChanged += (s, e) =>
            {
                if (character == null)
                    return;
                character.name = name.Text;
                (Parent as CharacterPanel).Update(character);
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
        internal void updateSkill(string name, int value) => character.skills[name] = value;
        internal void RemSkill(Skill skill)
        {
            skills.Remove(skill);
            character.skills.Remove(skill.name);
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
