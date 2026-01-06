using System;

namespace BIMDefender.Models
{
    /// <summary>
    /// Enemy types based on Revit elements
    /// </summary>
    public enum EnemyType
    {
        Clash,      // Red sphere - 10 points
        Warning,    // Yellow triangle - 25 points
        Error       // Orange diamond - 50 points, can shoot back
    }

    /// <summary>
    /// Represents an enemy in the game
    /// </summary>
    public class Enemy
    {
        private static readonly Random _random = new Random();

        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public EnemyType Type { get; }
        public bool IsAlive { get; set; } = true;
        public int Points { get; }
        public double Speed { get; }
        public bool CanShoot { get; }
        public DateTime LastShotTime { get; set; }

        // Direction: 1 = right, -1 = left
        public int Direction { get; set; } = 1;

        public Enemy(EnemyType type, double x, double y)
        {
            Type = type;
            X = x;
            Y = y;

            switch (type)
            {
                case EnemyType.Clash:
                    Width = 35;
                    Height = 35;
                    Points = 10;
                    Speed = 1.0;
                    CanShoot = false;
                    break;
                case EnemyType.Warning:
                    Width = 32;
                    Height = 32;
                    Points = 25;
                    Speed = 1.3;
                    CanShoot = false;
                    break;
                case EnemyType.Error:
                    Width = 30;
                    Height = 30;
                    Points = 50;
                    Speed = 1.5;
                    CanShoot = true;
                    break;
            }
        }

        public void Move(double horizontalSpeed)
        {
            X += Direction * horizontalSpeed * Speed;
        }

        public void MoveDown(double amount)
        {
            Y += amount;
        }

        public void ReverseDirection()
        {
            Direction *= -1;
        }

        public bool ShouldShoot()
        {
            if (!CanShoot) return false;

            // Random chance to shoot (about once every 3-5 seconds on average)
            if ((DateTime.Now - LastShotTime).TotalMilliseconds < 2000) return false;

            if (_random.NextDouble() < 0.01)
            {
                LastShotTime = DateTime.Now;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Random chance to drop a power-up when destroyed
        /// </summary>
        public bool DropsPowerUp()
        {
            // 5% chance to drop power-up
            return _random.NextDouble() < 0.05;
        }
    }
}
