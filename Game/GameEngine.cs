using System;
using System.Collections.Generic;
using System.Linq;
using BIMDefender.Models;

namespace BIMDefender.Game
{
    /// <summary>
    /// Game states
    /// </summary>
    public enum GameState
    {
        Ready,      // Initial state, waiting to start
        Playing,    // Game in progress
        Paused,     // Game paused
        GameOver,   // Game ended
        BossWave    // Boss fight in progress
    }

    /// <summary>
    /// Main game engine - handles game logic, collision, and state
    /// </summary>
    public class GameEngine
    {
        private static readonly Random _random = new Random();

        // Game dimensions
        public double CanvasWidth { get; }
        public double CanvasHeight { get; }

        // Game state
        public GameState State { get; private set; } = GameState.Ready;
        public int Score { get; private set; }
        public int Wave { get; private set; } = 1;
        public Player Player { get; private set; }
        public Boss CurrentBoss { get; private set; }

        // Game objects
        public List<Enemy> Enemies { get; } = new List<Enemy>();
        public List<Projectile> Projectiles { get; } = new List<Projectile>();
        public List<PowerUp> PowerUps { get; } = new List<PowerUp>();

        // Wave management
        private double _enemyHorizontalSpeed = 1.5;
        private double _enemyDropDistance = 20;
        private bool _enemiesNeedDrop = false;

        // Events for UI updates
        public event Action OnScoreChanged;
        public event Action OnWaveChanged;
        public event Action OnLivesChanged;
        public event Action OnGameOver;
        public event Action<PowerUpType> OnPowerUpCollected;
        public event Action OnBossDefeated;
        public event Action OnEnemyDestroyed;
        public event Action OnPlayerHit;
        public event Action OnPlayerShoot;
        public event Action OnBossHit;

        public GameEngine(double canvasWidth, double canvasHeight)
        {
            CanvasWidth = canvasWidth;
            CanvasHeight = canvasHeight;
            Player = new Player(canvasWidth, canvasHeight);
        }

        /// <summary>
        /// Start or restart the game
        /// </summary>
        public void Start()
        {
            Score = 0;
            Wave = 1;
            State = GameState.Playing;

            Player = new Player(CanvasWidth, CanvasHeight);
            Enemies.Clear();
            Projectiles.Clear();
            PowerUps.Clear();
            CurrentBoss = null;

            _enemyHorizontalSpeed = 1.5;

            SpawnWave();

            OnScoreChanged?.Invoke();
            OnWaveChanged?.Invoke();
            OnLivesChanged?.Invoke();
        }

        /// <summary>
        /// Toggle pause state
        /// </summary>
        public void TogglePause()
        {
            if (State == GameState.Playing || State == GameState.BossWave)
            {
                State = GameState.Paused;
            }
            else if (State == GameState.Paused)
            {
                State = CurrentBoss != null ? GameState.BossWave : GameState.Playing;
            }
        }

        /// <summary>
        /// Spawn a new wave of enemies
        /// </summary>
        private void SpawnWave()
        {
            Enemies.Clear();

            // Check for boss wave (every 5 waves)
            if (Wave % 5 == 0)
            {
                State = GameState.BossWave;
                CurrentBoss = new Boss(CanvasWidth, Wave);
                return;
            }

            State = GameState.Playing;
            CurrentBoss = null;

            // Calculate formation based on wave
            int rows = Math.Min(3 + Wave / 3, 6);
            int cols = Math.Min(6 + Wave / 2, 10);

            double startX = (CanvasWidth - (cols * 50)) / 2;
            double startY = 60;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    EnemyType type;
                    // Top row: Errors (if wave > 2)
                    // Middle rows: Warnings
                    // Bottom rows: Clashes
                    if (row == 0 && Wave > 2)
                        type = EnemyType.Error;
                    else if (row < rows / 2)
                        type = EnemyType.Warning;
                    else
                        type = EnemyType.Clash;

                    double x = startX + col * 50;
                    double y = startY + row * 45;

                    Enemies.Add(new Enemy(type, x, y));
                }
            }

            // Increase difficulty each wave
            _enemyHorizontalSpeed = 1.5 + Wave * 0.2;
        }

        /// <summary>
        /// Main update loop - called every frame
        /// </summary>
        public void Update(InputHandler input)
        {
            if (State != GameState.Playing && State != GameState.BossWave) return;

            // Update player
            UpdatePlayer(input);

            // Update enemies or boss
            if (State == GameState.BossWave)
            {
                UpdateBoss();
            }
            else
            {
                UpdateEnemies();
            }

            // Update projectiles
            UpdateProjectiles();

            // Update power-ups
            UpdatePowerUps();

            // Check collisions
            CheckCollisions();

            // Check wave completion
            CheckWaveComplete();
        }

        private void UpdatePlayer(InputHandler input)
        {
            Player.UpdatePowerUps();

            if (input.IsLeftPressed)
            {
                Player.MoveLeft();
            }
            if (input.IsRightPressed)
            {
                Player.MoveRight(CanvasWidth);
            }
            if (input.IsFirePressed && Player.CanShoot())
            {
                FirePlayerProjectile();
            }
        }

        private void FirePlayerProjectile()
        {
            Player.Shoot();
            double x = Player.X + Player.Width / 2;
            double y = Player.Y;
            Projectiles.Add(new Projectile(x, y, true));
            OnPlayerShoot?.Invoke();
        }

        private void UpdateEnemies()
        {
            if (Enemies.Count == 0) return;

            // Check if any enemy hits the edge
            bool hitEdge = false;
            foreach (var enemy in Enemies.Where(e => e.IsAlive))
            {
                if ((enemy.Direction > 0 && enemy.X + enemy.Width >= CanvasWidth - 10) ||
                    (enemy.Direction < 0 && enemy.X <= 10))
                {
                    hitEdge = true;
                    break;
                }
            }

            // Move enemies
            foreach (var enemy in Enemies.Where(e => e.IsAlive))
            {
                if (hitEdge)
                {
                    enemy.ReverseDirection();
                    enemy.MoveDown(_enemyDropDistance);
                }
                else
                {
                    enemy.Move(_enemyHorizontalSpeed);
                }

                // Enemy shooting
                if (enemy.ShouldShoot())
                {
                    double x = enemy.X + enemy.Width / 2;
                    double y = enemy.Y + enemy.Height;
                    Projectiles.Add(new Projectile(x, y, false));
                }

                // Check if enemies reached the bottom
                if (enemy.Y + enemy.Height >= Player.Y)
                {
                    State = GameState.GameOver;
                    OnGameOver?.Invoke();
                    return;
                }
            }
        }

        private void UpdateBoss()
        {
            if (CurrentBoss == null || !CurrentBoss.IsAlive) return;

            CurrentBoss.Update(CanvasWidth);

            // Boss shooting
            if (CurrentBoss.ShouldShoot())
            {
                foreach (double x in CurrentBoss.GetShotPositions())
                {
                    Projectiles.Add(new Projectile(x, CurrentBoss.Y + CurrentBoss.Height, false));
                }
            }
        }

        private void UpdateProjectiles()
        {
            foreach (var projectile in Projectiles)
            {
                projectile.Update();
                if (projectile.IsOffScreen(CanvasHeight))
                {
                    projectile.IsActive = false;
                }
            }

            // Remove inactive projectiles
            Projectiles.RemoveAll(p => !p.IsActive);
        }

        private void UpdatePowerUps()
        {
            foreach (var powerUp in PowerUps)
            {
                powerUp.Update();
                if (powerUp.IsOffScreen(CanvasHeight))
                {
                    powerUp.IsActive = false;
                }
            }

            // Remove inactive power-ups
            PowerUps.RemoveAll(p => !p.IsActive);
        }

        private void CheckCollisions()
        {
            // Player projectiles vs enemies
            foreach (var projectile in Projectiles.Where(p => p.IsPlayerProjectile && p.IsActive))
            {
                // Check boss collision
                if (CurrentBoss != null && CurrentBoss.IsAlive)
                {
                    if (CheckCollision(projectile, CurrentBoss))
                    {
                        projectile.IsActive = false;
                        bool defeated = CurrentBoss.TakeHit();
                        OnBossHit?.Invoke();

                        if (defeated)
                        {
                            Score += CurrentBoss.Points;
                            OnScoreChanged?.Invoke();
                            OnBossDefeated?.Invoke();
                        }
                        continue;
                    }
                }

                // Check enemy collisions
                foreach (var enemy in Enemies.Where(e => e.IsAlive))
                {
                    if (CheckCollision(projectile, enemy))
                    {
                        projectile.IsActive = false;
                        enemy.IsAlive = false;
                        Score += enemy.Points;
                        OnScoreChanged?.Invoke();
                        OnEnemyDestroyed?.Invoke();

                        // Chance to spawn power-up
                        if (enemy.DropsPowerUp())
                        {
                            SpawnPowerUp(enemy.X + enemy.Width / 2, enemy.Y);
                        }
                        break;
                    }
                }
            }

            // Enemy projectiles vs player
            foreach (var projectile in Projectiles.Where(p => !p.IsPlayerProjectile && p.IsActive))
            {
                if (CheckCollision(projectile, Player))
                {
                    projectile.IsActive = false;
                    bool survived = Player.TakeHit();
                    OnPlayerHit?.Invoke();
                    OnLivesChanged?.Invoke();

                    if (!Player.IsAlive)
                    {
                        State = GameState.GameOver;
                        OnGameOver?.Invoke();
                    }
                }
            }

            // Power-ups vs player
            foreach (var powerUp in PowerUps.Where(p => p.IsActive))
            {
                if (CheckCollision(powerUp, Player))
                {
                    powerUp.IsActive = false;
                    ApplyPowerUp(powerUp.Type);
                    OnPowerUpCollected?.Invoke(powerUp.Type);
                }
            }
        }

        private bool CheckCollision(Projectile p, Enemy e)
        {
            return p.X < e.X + e.Width &&
                   p.X + p.Width > e.X &&
                   p.Y < e.Y + e.Height &&
                   p.Y + p.Height > e.Y;
        }

        private bool CheckCollision(Projectile p, Boss b)
        {
            return p.X < b.X + b.Width &&
                   p.X + p.Width > b.X &&
                   p.Y < b.Y + b.Height &&
                   p.Y + p.Height > b.Y;
        }

        private bool CheckCollision(Projectile p, Player player)
        {
            return p.X < player.X + player.Width &&
                   p.X + p.Width > player.X &&
                   p.Y < player.Y + player.Height &&
                   p.Y + p.Height > player.Y;
        }

        private bool CheckCollision(PowerUp pu, Player player)
        {
            return pu.X < player.X + player.Width &&
                   pu.X + pu.Width > player.X &&
                   pu.Y < player.Y + player.Height &&
                   pu.Y + pu.Height > player.Y;
        }

        private void SpawnPowerUp(double x, double y)
        {
            PowerUpType type = (PowerUpType)_random.Next(3);
            PowerUps.Add(new PowerUp(type, x, y));
        }

        private void ApplyPowerUp(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.PurgeAll:
                    // Clear all enemies and award points
                    foreach (var enemy in Enemies.Where(e => e.IsAlive))
                    {
                        enemy.IsAlive = false;
                        Score += enemy.Points / 2; // Half points for bomb kills
                    }
                    OnScoreChanged?.Invoke();
                    break;

                case PowerUpType.AdminMode:
                    Player.ApplyRapidFire(10);
                    break;

                case PowerUpType.BackupSave:
                    Player.HasShield = true;
                    break;
            }
        }

        private void CheckWaveComplete()
        {
            bool waveComplete = false;

            if (State == GameState.BossWave)
            {
                waveComplete = CurrentBoss != null && !CurrentBoss.IsAlive;
            }
            else
            {
                waveComplete = Enemies.All(e => !e.IsAlive);
            }

            if (waveComplete)
            {
                Wave++;
                OnWaveChanged?.Invoke();
                SpawnWave();
            }
        }
    }
}
