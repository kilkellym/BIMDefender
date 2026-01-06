namespace BIMDefender.Models
{
    /// <summary>
    /// Power-up types
    /// </summary>
    public enum PowerUpType
    {
        PurgeAll,       // Clears all enemies on screen
        AdminMode,      // Faster shooting for 10 seconds
        BackupSave      // Absorbs one hit
    }

    /// <summary>
    /// Represents a falling power-up
    /// </summary>
    public class PowerUp
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; } = 25;
        public double Height { get; } = 25;
        public double Speed { get; } = 3;
        public PowerUpType Type { get; }
        public bool IsActive { get; set; } = true;

        public PowerUp(PowerUpType type, double x, double y)
        {
            Type = type;
            X = x;
            Y = y;
        }

        public void Update()
        {
            Y += Speed;
        }

        public bool IsOffScreen(double canvasHeight)
        {
            return Y > canvasHeight;
        }

        /// <summary>
        /// Get display color based on type
        /// </summary>
        public string GetColorHex()
        {
            switch (Type)
            {
                case PowerUpType.PurgeAll: return "#3498db";  // Blue
                case PowerUpType.AdminMode: return "#2ecc71";  // Green
                case PowerUpType.BackupSave: return "#9b59b6";     // Purple
                default: return "#ffffff";
            }
        }

        /// <summary>
        /// Get display label based on type
        /// </summary>
        public string GetLabel()
        {
            switch (Type)
            {
                case PowerUpType.PurgeAll: return "P";
                case PowerUpType.AdminMode: return "A";
                case PowerUpType.BackupSave: return "B";
                default: return "?";
            }
        }
    }
}
