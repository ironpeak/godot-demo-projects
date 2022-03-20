using System.Collections.Generic;
using Godot;

namespace GoTiled.Example
{
    public class Bomb : Area2D
    {
        public int FromPlayer { get; set; }
        private List<Node> InArea = new List<Node>();

        private void Explode()
        {
            if (!IsNetworkMaster()) return;

            foreach (var node in InArea)
            {
                if (node.HasMethod("Exploded"))
                {
                    node.Rpc("Exploded", FromPlayer);
                }
            }
        }

        private void Done()
        {
            QueueFree();
        }

        private void OnBombBodyEnter(Node body)
        {
            if (InArea.Contains(body) == false)
            {
                InArea.Add(body);
            }
        }

        private void OnBombBodyExit(Node body)
        {
            InArea.Remove(body);
        }
    }
}