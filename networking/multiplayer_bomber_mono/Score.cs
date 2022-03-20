using System.Collections.Generic;
using Godot;

namespace GoTiled.Example
{
    public class Score : HBoxContainer
    {
        private readonly Dictionary<int, PlayerScore> _playerLabels = new Dictionary<int, PlayerScore>();

        public override void _Process(float delta)
        {
            var rocksLeft = GetNode("../Rocks").GetChildCount();
            if (rocksLeft == 0)
            {
                var winnerName = "";
                var winnerScore = 0;
                foreach (var playerLabel in _playerLabels.Values)
                {
                    if (playerLabel.Score > winnerScore)
                    {
                        winnerScore = playerLabel.Score;
                        winnerName = playerLabel.Name;
                    }
                }

                var winner = GetNode<Label>("../Winner");
                winner.Text = $"THE WINNER IS:\n{winnerName}";
                winner.Show();
            }
        }

        [RemoteSync]
        public void IncreaseScore(int id)
        {
            var player = _playerLabels[id];
            player.Score++;
            player.Label.Text = $"{player.Name}\n{player.Score}";
        }

        public void AddPlayer(int id, string newPlayerName)
        {
            var label = new Label();
            label.Align = Label.AlignEnum.Center;
            label.Text = $"{newPlayerName}\n0";
            label.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;

            var font = new DynamicFont();
            font.Size = 18;
            font.FontData = ResourceLoader.Load<DynamicFontData>("res://montserrat.otf");
            label.AddFontOverride("font", font);
            AddChild(label);

            _playerLabels[id] = new PlayerScore()
            {
                Name = newPlayerName,
                Label = label,
                Score = 0,
            };
        }

        public override void _Ready()
        {
            GetNode<Label>("../Winner").Hide();
            SetProcess(true);
        }

        private void OnExitGamePressed()
        {
            GetNode<GameState>("/root/GameState").EndGame();
        }
    }

    class PlayerScore
    {
        public string Name { get; set; }
        public Label Label { get; set; }
        public int Score { get; set; }
    }
}