using System.Linq;
using System.Net;
using Godot;

namespace GoTiled.Example
{
    public class Lobby : Control
    {
        public override void _Ready()
        {
            var gameState = GetNode<GameState>("GameState");

            gameState.Connect("ConnectionFailed", this, "OnConnectionFailed");
            gameState.Connect("ConnectionSucceeded", this, "OnConnectionSuccess");
            gameState.Connect("PlayerListChanged", this, "RefreshLobby");
            gameState.Connect("GameEnded", this, "OnGameEnded");
            gameState.Connect("GameError", this, "OnGameError");

            var username = System.Environment.GetEnvironmentVariable("USERNAME") ?? "NoName";

            GetNode<LineEdit>("Connect/Name").Text = username;
        }

        public void OnHostPressed()
        {
            if (GetNode<LineEdit>("Connect/Name").Text == "")
            {
                GetNode<Label>("Connect/ErrorLabel").Text = "Invalid name!";
                return;
            }

            GetNode<Control>("Connect").Hide();
            GetNode<Control>("Players").Show();
            GetNode<Label>("Connect/ErrorLabel").Text = "";

            var playerName = GetNode<LineEdit>("Connect/Name").Text;
            GetNode<GameState>("GameState").HostGame(playerName);
            RefreshLobby();
        }

        public void OnJoinPressed()
        {
            if (GetNode<LineEdit>("Connect/Name").Text == "")
            {
                GetNode<Label>("Connect/ErrorLabel").Text = "Invalid name!";
                return;
            }

            var ip = GetNode<LineEdit>("Connect/IPAddress").Text;
            if (IPAddress.TryParse(ip, out var _) == false)
            {
                GetNode<Label>("Connect/ErrorLabel").Text = "Invalid IP address!";
                return;
            }

            GetNode<Label>("Connect/ErrorLabel").Text = "";

            GetNode<Button>("Connect/Host").Disabled = true;
            GetNode<Button>("Connect/Join").Disabled = true;

            var playerName = GetNode<LineEdit>("Connect/Name").Text;
            GetNode<GameState>("GameState").JoinGame(ip, playerName);
        }

        private void OnConnectionSuccess()
        {
            GetNode<Control>("Connect").Hide();
            GetNode<Control>("Players").Show();
        }

        private void OnConnectionFailed()
        {
            GetNode<Button>("Connect/Host").Disabled = false;
            GetNode<Button>("Connect/Join").Disabled = false;
            GetNode<Label>("Connect/ErrorLabel").Text = "Connection failed.";
        }

        private void OnGameEnded()
        {
            this.Show();
            GetNode<Control>("Connect").Show();
            GetNode<Control>("Players").Hide();
            GetNode<Button>("Connect/Host").Disabled = false;
            GetNode<Button>("Connect/Join").Disabled = false;
        }

        private void OnGameError(string error)
        {
            GetNode<AcceptDialog>("ErrorDialog").DialogText = error;
            GetNode<AcceptDialog>("ErrorDialog").PopupCenteredMinsize();
            GetNode<Button>("Connect/Host").Disabled = false;
            GetNode<Button>("Connect/Join").Disabled = false;
        }

        private void RefreshLobby()
        {
            var gameState = GetNode<GameState>("GameState");
            var players = gameState.GetPlayerList().ToList();
            players.Sort();

            var playersList = GetNode<ItemList>("Players/List");
            playersList.Clear();
            playersList.AddItem($"{gameState.GetPlayerName()} (You)");

            foreach (var player in players)
            {
                playersList.AddItem(player);
            }

            GetNode<Button>("Players/Start").Disabled = !GetTree().IsNetworkServer();
        }

        private void OnStartPressed()
        {
            var gameState = GetNode<GameState>("GameState");
            gameState.BeginGame();
        }
    }
}