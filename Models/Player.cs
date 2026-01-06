using System;

namespace BIMDefender.Models
{
    /// <summary>
    /// Represents the player's ship (section box aesthetic)
    /// </summary>
    public class Player
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; } = 50;
        public double Height { get; } = 40;
        public double Speed { get; } = 8;
        public int Lives { get; set; } = 3;
        public bool HasShield { get; set; }
        public bool HasRapidFire { get; set; }
        public DateTime RapidFireEndTime { get; set; }
        public DateTime LastShotTime { get; set; }

        // Normal fire rate: 3 shots per second, Rapid: 8 shots per second
        public double FireCooldown => HasRapidFire ? 125 : 333;

        public Player(double canvasWidth, double canvasHeight)
        {
            X = (canvasWidth - Width) / 2;
            Y = canvasHeight - Height - 20;
        }

        public bool CanShoot()
        {
            return (DateTime.Now - LastShotTime).TotalMilliseconds >= FireCooldown;
        }

        public void Shoot()
        {
            LastShotTime = DateTime.Now;
        }

        public void MoveLeft(double minX = 0)
        {
            X = Math.Max(minX, X - Speed);
        }

        public void MoveRight(double maxX)
        {
            X = Math.Min(maxX - Width, X + Speed);
        }

        public void UpdatePowerUps()
        {
            if (HasRapidFire && DateTime.Now > RapidFireEndTime)
            {
                HasRapidFire = false;
            }
        }

        public void ApplyRapidFire(double durationSeconds = 10)
        {
            HasRapidFire = true;
            RapidFireEndTime = DateTime.Now.AddSeconds(durationSeconds);
        }

        /// <summary>
        /// Returns true if player survives (had shield), false if life lost
        /// </summary>
        public bool TakeHit()
        {
            if (HasShield)
            {
                HasShield = false;
                return true;
            }
            Lives--;
            return false;
        }

        public bool IsAlive => Lives > 0;
    }
}
