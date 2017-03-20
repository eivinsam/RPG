using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace RPG.UI
{
    class TurnPanel : StackPanel
    {
        class Turn : IComparable<Turn>
        {
            public int value;
            public Model.Character character;

            static int delay(int speed) => (500 + speed - 1) / speed;

            public Turn(Model.Character c)
            {
                value = delay(c.stats["NTU"]);
                character = c;
            }

            public void Next() => value += delay(character.stats["DEX"]);

            public int CompareTo(Turn other) => value - other.value;
        }
        readonly List<Turn> turns = new List<Turn>();

        public TurnPanel(Model.Place party, Model.Place location)
        {
            foreach (var c in party.characters)
                turns.Add(new Turn(c));
            foreach (var c in location.characters)
                turns.Add(new Turn(c));
            turns.Sort();

            var next_turn = new FlatButton { Content = "Next turn" };
            Children.Add(new Label { Content = "Turns", FontSize = 16 });
            Children.Add(next_turn);

            Update();

            next_turn.Click += (s, e) =>
            {
                var time = turns[0].value;
                foreach (var item in turns)
                    item.value = item.value - time;
                turns[0].Next();
                turns.Sort();
                Update();
            };
        }

        void Update()
        {
            Children.RemoveRange(2, Children.Count - 2);
            foreach (var item in turns)
                Children.Add(new Label { Content = item.character.name + " (" + item.value + ")" });
        }
    }
}
