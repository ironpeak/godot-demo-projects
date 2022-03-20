using Godot;

namespace GoTiled.Example
{
    public class Player : KinematicBody2D
    {
        private const float MOTION_SPEED = 90;

        [Puppet] public Vector2 PuppetPosition = Vector2.Zero;
        [Puppet] public Vector2 PuppetMotion = Vector2.Zero;

        [Export] public bool Stunned = false;

        [RemoteSync]
        private void SetupBomb(string name, Vector2 position, int owner)
        {
            var bomb = ResourceLoader.Load<PackedScene>("res://bomb.tscn").Instance<Bomb>();
            bomb.Name = name;
            bomb.Position = position;
            bomb.FromPlayer = owner;

            GetNode("../..").AddChild(bomb);
        }

        private string _currentAnimation = "";
        private bool _previousBombing = false;
        private int _bombIndex = 0;

        public override void _PhysicsProcess(float delta)
        {
            var motion = Vector2.Zero;

            if (IsNetworkMaster())
            {
                if (Input.IsActionPressed("MoveLeft"))
                {
                    motion += Vector2.Left;
                }
                if (Input.IsActionPressed("MoveRight"))
                {
                    motion += Vector2.Right;
                }
                if (Input.IsActionPressed("MoveUp"))
                {
                    motion += Vector2.Up;
                }
                if (Input.IsActionPressed("MoveDown"))
                {
                    motion += Vector2.Down;
                }

                var bombing = Input.IsActionPressed("Fire");

                if (Stunned)
                {
                    bombing = false;
                    motion = Vector2.Zero;
                }

                if (bombing && !_previousBombing)
                {
                    var bombName = $"{Name}{_bombIndex}";
                    var bombPosition = Position;
                    Rpc("SetupBomb", bombName, bombPosition, GetTree().GetNetworkUniqueId());
                }

                _previousBombing = bombing;

                Rset("PuppetMotion", motion);
                Rset("PuppetPosition", Position);
            }
            else
            {
                Position = PuppetPosition;
                motion = PuppetMotion;
            }

            var newAnimation = "Idle";
            if (motion.y < 0)
            {
                newAnimation = "WalkUp";
            }
            else if (motion.y > 0)
            {
                newAnimation = "WalkDown";
            }
            else if (motion.x < 0)
            {
                newAnimation = "WalkLeft";
            }
            else if (motion.x > 0)
            {
                newAnimation = "WalkRight";
            }

            if (Stunned)
            {
                newAnimation = "Stunned";
            }

            if (newAnimation != _currentAnimation)
            {
                _currentAnimation = newAnimation;
                GetNode<AnimationPlayer>("AnimationPlayer").Play(_currentAnimation);
            }

            MoveAndSlide(motion * MOTION_SPEED);

            if (IsNetworkMaster() == false)
            {
                PuppetPosition = Position;
            }
        }

        [Puppet]
        public void Stun()
        {
            Stunned = true;
        }

        [Master]
        public void Exploded(string owner)
        {
            if (Stunned) return;

            Rpc("Stun");
            Stun();
        }

        public void SetPlayerName(string name)
        {
            GetNode<Label>("label").Text = name;
        }

        public override void _Ready()
        {
            PuppetPosition = Position;
            Stunned = false;
        }
    }
}