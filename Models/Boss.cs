using System;

namespace BIMDefender.Models
{
    /// <summary>
    /// Represents the boss enemy - "Corrupt Central Model"
    /// Appears every 5 waves
    /// </summary>
    public class Boss
    {
        private static readonly Random _random = new Random();

        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; } = 120;
        public double Height { get; } = 80;
        public int MaxHealth { get; }
        public int Health { get; set; }
        public int Points { get; } = 500;
        public bool IsAlive => Health > 0;
        public int Direction { get; set; } = 1;
        public double Speed { get; } = 2;
        public DateTime LastShotTime { get; set; }

        public Boss(double canvasWidth, int wave)
        {
            // Boss gets tougher each appearance
            MaxHealth = 10 + (wave / 5) * 5;
            Health = MaxHealth;
            X = (canvasWidth - Width) / 2;
            Y = 50;
        }

        public void Update(double canvasWidth)
        {
            X += Direction * Speed;

            // Bounce off edges
            if (X <= 0)
            {
                X = 0;
                Direction = 1;
            }
            else if (X >= canvasWidth - Width)
            {
                X = canvasWidth - Width;
                Direction = -1;
            }
        }

        public bool TakeHit()
        {
            Health--;
            return !IsAlive;
        }

        /// <summary>
        /// Boss shoots more frequently than regular enemies
        /// </summary>
        public bool ShouldShoot()
        {
            if ((DateTime.Now - LastShotTime).TotalMilliseconds < 800) return false;

            if (_random.NextDouble() < 0.05)
            {
                LastShotTime = DateTime.Now;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get random X positions for multi-shot pattern
        /// </summary>
        public double[] GetShotPositions()
        {
            // Boss shoots 3 projectiles
            return new double[]
            {
                X + Width * 0.25,
                X + Width * 0.5,
                X + Width * 0.75
            };
        }

        public double HealthPercentage => (double)Health / MaxHealth;
    }
}
