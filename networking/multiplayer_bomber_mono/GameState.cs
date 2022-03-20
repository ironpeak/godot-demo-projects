using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GoTiled.Example
{
    public class GameState : Node
    {
        private const int DefaultPort = 10567;
        private const int MaxPeers = 12;

        private NetworkedMultiplayerENet _peer = null;

        [Signal] public delegate void PlayerListChanged();
        [Signal] public delegate void ConnectionFailed();
        [Signal] public delegate void ConnectionSucceeded();
        [Signal] public delegate void GameEnded();
        [Signal] public delegate void GameError(string error);

        private string _playerName = "The Warrior";
        private readonly Dictionary<int, string> _players = new Dictionary<int, string>();
        private readonly List<int> _playersReady = new List<int>();

        public void PlayerConnected(int id)
        {
            RpcId(id, "RegisterPlayer", _playerName);
        }

        public void PlayerDisconnected(int id)
        {
            if (HasNode("/root/World"))
            {
                if (GetTree().IsNetworkServer())
                {
                    EmitSignal("GameError", $"Player {_players[id]} disconnected");
                    EndGame();
                }
            }
            else
            {
                UnregisterPlayer(id);
            }
        }

        public void ConnectedOK()
        {
            EmitSignal("ConnectionSucceeded");
        }

        public void ServerDisconnected()
        {
            EmitSignal("GameError", "Server disconnected");
            EndGame();
        }

        public void ConnectedFail()
        {
            GetTree().NetworkPeer = null;
            EmitSignal("ConnectionFailed");
        }

        [Remote]
        private void RegisterPlayer(string newPlayerName)
        {
            var id = GetTree().GetRpcSenderId();
            _players[id] = newPlayerName;
            EmitSignal("PlayerListChanged");
        }

        private void UnregisterPlayer(int id)
        {
            _players.Remove(id);
            EmitSignal("PlayerListChanged");
        }

        [Remote]
        private void PreStartGame(Dictionary<int, int> spawnPoints)
        {
            var world = ResourceLoader.Load<PackedScene>("res://world.tscn").Instance();
            GetTree().Root.AddChild(world);

            GetTree().Root.GetNode<Control>("Lobby").Hide();

            var playerScene = ResourceLoader.Load<PackedScene>("res://player.tscn");

            foreach (var pid in spawnPoints.Keys)
            {
                var spawnPoint = world.GetNode<Node2D>($"SpawnPoints/{spawnPoints[pid]}").Position;
                var player = playerScene.Instance<Player>();

                player.Name = spawnPoint.ToString();
                player.Position = spawnPoint;
                player.SetNetworkMaster(pid);

                if (pid == GetTree().GetNetworkUniqueId())
                {
                    player.SetPlayerName(_playerName);
                }
                else
                {
                    player.SetPlayerName(_players[pid]);
                }

                world.GetNode<Node2D>("Players").AddChild(player);
            }

            world.GetNode<Score>("Score").AddPlayer(GetTree().GetNetworkUniqueId(), _playerName);
            foreach (var player in _players.Keys)
            {
                world.GetNode<Score>("Score").AddPlayer(player, _players[player]);
            }

            if (!GetTree().IsNetworkServer())
            {
                RpcId(1, "ReadyToStart", GetTree().GetNetworkUniqueId());
            }
            else if (_players.Count == 0)
            {
                PostStartGame();
            }
        }

        [Remote]
        private void PostStartGame()
        {
            GetTree().Paused = false;
        }

        [Remote]
        private void ReadyToStart(int id)
        {
            if (_playersReady.Contains(id) == false)
            {
                _playersReady.Add(id);
            }

            if (_playersReady.Count == _players.Count)
            {
                foreach (var player in _players.Keys)
                {
                    RpcId(player, "PostStartGame");
                }
                PostStartGame();
            }
        }

        public void HostGame(string newPlayerName)
        {
            _playerName = newPlayerName;
            _peer = new NetworkedMultiplayerENet();
            _peer.CreateServer(DefaultPort, MaxPeers);
            GetTree().NetworkPeer = _peer;
        }

        public void JoinGame(string ip, string newPlayerName)
        {
            _playerName = newPlayerName;
            _peer = new NetworkedMultiplayerENet();
            _peer.CreateClient(ip, DefaultPort);
            GetTree().NetworkPeer = _peer;
        }

        public IReadOnlyList<string> GetPlayerList()
        {
            return _players.Values.ToList();
        }

        public string GetPlayerName()
        {
            return _playerName;
        }

        public void BeginGame()
        {
            var spawnPoints = new Dictionary<int, int>
            {
                [1] = 0
            };
            var spawnPointIndex = 1;
            foreach (var player in _players.Keys)
            {
                spawnPoints[player] = spawnPointIndex;
                spawnPointIndex++;
            }

            foreach (var player in _players.Keys)
            {
                RpcId(player, "PreStartGame", spawnPoints);
            }

            PreStartGame(spawnPoints);
        }

        public void EndGame()
        {
            if (HasNode("/root/World"))
            {
                GetNode("/root/World").QueueFree();
            }
            EmitSignal("GameEnded");
            _players.Clear();
        }

        public override void _Ready()
        {
            GetTree().Connect("network_peer_connected", this, "PlayerConnected");
            GetTree().Connect("network_peer_disconnected", this, "PlayerDisconnected");
            GetTree().Connect("connected_to_server", this, "ConnectedOK");
            GetTree().Connect("connection_failed", this, "ConnectedFail");
            GetTree().Connect("server_disconnected", this, "ServerDisconnected");
        }
    }
}