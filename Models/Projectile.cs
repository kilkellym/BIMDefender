namespace BIMDefender.Models
{
    /// <summary>
    /// Represents a projectile (bullet) in the game
    /// </summary>
    public class Projectile
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; } = 4;
        public double Height { get; } = 12;
        public double Speed { get; }
        public bool IsPlayerProjectile { get; }
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Creates a new projectile
        /// </summary>
        /// <param name="x">Starting X position (center)</param>
        /// <param name="y">Starting Y position</param>
        /// <param name="isPlayerProjectile">True if fired by player, false if by enemy</param>
        public Projectile(double x, double y, bool isPlayerProjectile)
        {
            X = x - Width / 2;
            Y = y;
            IsPlayerProjectile = isPlayerProjectile;
            Speed = isPlayerProjectile ? -10 : 5; // Negative = up, Positive = down
        }

        public void Update()
        {
            Y += Speed;
        }

        public bool IsOffScreen(double canvasHeight)
        {
            return Y < -Height || Y > canvasHeight;
        }
    }
}
