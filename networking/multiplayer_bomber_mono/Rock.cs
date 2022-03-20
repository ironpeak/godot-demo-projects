using Godot;

namespace GoTiled.Example
{
    public class Rock : KinematicBody2D
    {
        [Puppet]
        private void DoExplosion()
        {
            GetNode<AnimationPlayer>("AnimationPlayer").Play("Explode");
        }

        [Master]
        private void Exploded(string owner)
        {
            Rpc("DoExplosion");
            GetNode("../../Score").Rpc("IncreaseScore", owner);
            DoExplosion();
        }
    }
}