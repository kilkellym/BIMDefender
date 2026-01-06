using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using BIMDefender.Assets;
using BIMDefender.Game;
using BIMDefender.Models;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using Color2 = System.Windows.Media.Color;

namespace BIMDefender.UI
{
    /// <summary>
    /// Main game window - handles rendering and game loop
    /// OPTIMIZED VERSION - uses object pooling and removes expensive effects
    /// </summary>
    public partial class GameWindow : Window
    {
        private GameEngine _game;
        private InputHandler _input;
        private ScoreManager _scoreManager;
        private SoundManager _sound;
        private DispatcherTimer _gameLoop;

        // Cached brushes for performance (frozen for thread safety)
        private static readonly SolidColorBrush PlayerBrush;
        private static readonly SolidColorBrush PlayerShieldBrush;
        private static readonly SolidColorBrush ClashBrush;
        private static readonly SolidColorBrush WarningBrush;
        private static readonly SolidColorBrush ErrorBrush;
        private static readonly SolidColorBrush ProjectileBrush;
        private static readonly SolidColorBrush EnemyProjectileBrush;
        private static readonly SolidColorBrush BossBrush;
        private static readonly SolidColorBrush BossCorruptionBrush;
        private static readonly SolidColorBrush AuditBombBrush;
        private static readonly SolidColorBrush RapidFireBrush;
        private static readonly SolidColorBrush ShieldPowerUpBrush;

        // Object pools to avoid GC pressure
        private readonly List<Shape> _shapePool = new List<Shape>();
        private readonly List<TextBlock> _textPool = new List<TextBlock>();
        private int _shapePoolIndex;
        private int _textPoolIndex;

        // Pre-created point collections for enemy shapes (reused)
        private readonly PointCollection _trianglePoints = new PointCollection(3);
        private readonly PointCollection _diamondPoints = new PointCollection(4);
        private readonly PointCollection _shipPoints = new PointCollection(5);

        // Icon path geometry (parsed once for performance)
        private static readonly Geometry InvaderGeometry;
        private static readonly Geometry SkullGeometry;
        private static readonly Geometry GhostGeometry;
        private static readonly Geometry VirusGeometry;
        private static readonly Geometry RocketGeometry;

        static GameWindow()
        {
            // Initialize and freeze brushes (frozen brushes are more performant)
            PlayerBrush = new SolidColorBrush(Color2.FromRgb(52, 152, 219));
            PlayerBrush.Freeze();

            PlayerShieldBrush = new SolidColorBrush(Color2.FromRgb(155, 89, 182));
            PlayerShieldBrush.Freeze();

            ClashBrush = new SolidColorBrush(Color2.FromRgb(231, 76, 60));
            ClashBrush.Freeze();

            WarningBrush = new SolidColorBrush(Color2.FromRgb(241, 196, 15));
            WarningBrush.Freeze();

            ErrorBrush = new SolidColorBrush(Color2.FromRgb(0, 188, 212));
            ErrorBrush.Freeze();

            ProjectileBrush = new SolidColorBrush(Color2.FromRgb(0, 255, 0));
            ProjectileBrush.Freeze();

            EnemyProjectileBrush = new SolidColorBrush(Color2.FromRgb(255, 100, 100));
            EnemyProjectileBrush.Freeze();

            BossBrush = new SolidColorBrush(Color2.FromRgb(142, 68, 173));
            BossBrush.Freeze();

            BossCorruptionBrush = new SolidColorBrush(Color2.FromArgb(80, 255, 0, 0));
            BossCorruptionBrush.Freeze();

            AuditBombBrush = new SolidColorBrush(Color2.FromRgb(52, 152, 219));
            AuditBombBrush.Freeze();

            RapidFireBrush = new SolidColorBrush(Color2.FromRgb(46, 204, 113));
            RapidFireBrush.Freeze();

            ShieldPowerUpBrush = new SolidColorBrush(Color2.FromRgb(155, 89, 182));
            ShieldPowerUpBrush.Freeze();

            // Enemy geometry
            InvaderGeometry = Geometry.Parse(
                "M7,6H5V4H7V6M17,6H19V4H17V6M23,12V18H21V14H19V18H17V16H7V18H5V14H3V18H1V12H3V10H5V8H7V6H9V8H15V6H17V8H19V10H21V12H23M15,10V12H17V10H15M7,12H9V10H7V12M11,18H7V20H11V18M17,18H13V20H17V18Z"
            );
            InvaderGeometry.Freeze();

            SkullGeometry = Geometry.Parse(
                "M8,15A2,2 0 0,1 6,13A2,2 0 0,1 8,11A2,2 0 0,1 10,13A2,2 0 0,1 8,15M10.5,17L12,14L13.5,17H10.5M16,15A2,2 0 0,1 14,13A2,2 0 0,1 16,11A2,2 0 0,1 18,13A2,2 0 0,1 16,15M22,11A10,10 0 0,0 12,1A10,10 0 0,0 2,11C2,13.8 3.2,16.3 5,18.1V22H19V18.1C20.8,16.3 22,13.8 22,11M17,20H15V18H13V20H11V18H9V20H7V17.2C5.2,15.7 4,13.5 4,11A8,8 0 0,1 12,3A8,8 0 0,1 20,11C20,13.5 18.8,15.8 17,17.2V20Z"
            );
            SkullGeometry.Freeze();

            GhostGeometry = Geometry.Parse(
                "M12,2A9,9 0 0,0 3,11V22L6,19L9,22L12,19L15,22L18,19L21,22V11A9,9 0 0,0 12,2M9,8A2,2 0 0,1 11,10A2,2 0 0,1 9,12A2,2 0 0,1 7,10A2,2 0 0,1 9,8M15,8A2,2 0 0,1 17,10A2,2 0 0,1 15,12A2,2 0 0,1 13,10A2,2 0 0,1 15,8Z"
            );
            GhostGeometry.Freeze();

            VirusGeometry = Geometry.Parse(
                "M19.82 14C20.13 14.45 20.66 14.75 21.25 14.75C22.22 14.75 23 13.97 23 13S22.22 11.25 21.25 11.25C20.66 11.25 20.13 11.55 19.82 12H19C19 10.43 18.5 9 17.6 7.81L18.94 6.47C19.5 6.57 20.07 6.41 20.5 6C21.17 5.31 21.17 4.2 20.5 3.5C19.81 2.83 18.7 2.83 18 3.5C17.59 3.93 17.43 4.5 17.53 5.06L16.19 6.4C15.27 5.71 14.19 5.25 13 5.08V3.68C13.45 3.37 13.75 2.84 13.75 2.25C13.75 1.28 12.97 .5 12 .5S10.25 1.28 10.25 2.25C10.25 2.84 10.55 3.37 11 3.68V5.08C10.1 5.21 9.26 5.5 8.5 5.94L7.39 4.35C7.58 3.83 7.53 3.23 7.19 2.75C6.63 1.96 5.54 1.76 4.75 2.32C3.96 2.87 3.76 3.96 4.32 4.75C4.66 5.24 5.2 5.5 5.75 5.5L6.93 7.18C6.5 7.61 6.16 8.09 5.87 8.62C5.25 8.38 4.5 8.5 4 9C3.33 9.7 3.33 10.8 4 11.5C4.29 11.77 4.64 11.93 5 12L5 12C5 12.54 5.07 13.06 5.18 13.56L3.87 13.91C3.45 13.56 2.87 13.41 2.29 13.56C1.36 13.81 .808 14.77 1.06 15.71C1.31 16.64 2.28 17.19 3.21 16.94C3.78 16.78 4.21 16.36 4.39 15.84L5.9 15.43C6.35 16.22 6.95 16.92 7.65 17.5L6.55 19.5C6 19.58 5.5 19.89 5.21 20.42C4.75 21.27 5.07 22.33 5.92 22.79C6.77 23.25 7.83 22.93 8.29 22.08C8.57 21.56 8.56 20.96 8.31 20.47L9.38 18.5C10.19 18.82 11.07 19 12 19C12.06 19 12.12 19 12.18 19C12.05 19.26 12 19.56 12 19.88C12.08 20.85 12.92 21.57 13.88 21.5S15.57 20.58 15.5 19.62C15.46 19.12 15.21 18.68 14.85 18.39C15.32 18.18 15.77 17.91 16.19 17.6L18.53 19.94C18.43 20.5 18.59 21.07 19 21.5C19.7 22.17 20.8 22.17 21.5 21.5S22.17 19.7 21.5 19C21.07 18.59 20.5 18.43 19.94 18.53L17.6 16.19C18.09 15.54 18.47 14.8 18.71 14H19.82M10.5 12C9.67 12 9 11.33 9 10.5S9.67 9 10.5 9 12 9.67 12 10.5 11.33 12 10.5 12M14 15C13.45 15 13 14.55 13 14C13 13.45 13.45 13 14 13S15 13.45 15 14C15 14.55 14.55 15 14 15Z"
            );
            VirusGeometry.Freeze();

            RocketGeometry = Geometry.Parse(
                "M20 22L16.14 20.45C16.84 18.92 17.34 17.34 17.65 15.73L20 22M7.86 20.45L4 22L6.35 15.73C6.66 17.34 7.16 18.92 7.86 20.45M12 2C12 2 17 4 17 12C17 15.1 16.25 17.75 15.33 19.83C15 20.55 14.29 21 13.5 21H10.5C9.71 21 9 20.55 8.67 19.83C7.76 17.75 7 12 7 12C7 4 12 2 12 2M12 12C13.1 12 14 11.1 14 10C14 8.9 13.1 8 12 8C10.9 8 10 8.9 10 10C10 11.1 10.9 12 12 12Z"
            );
            RocketGeometry.Freeze();
        }

        public GameWindow()
        {
            InitializeComponent();

            // Initialize managers
            string addInFolder = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _scoreManager = new ScoreManager(addInFolder);
            _sound = new SoundManager();
            _input = new InputHandler();

            // Setup game loop (60 FPS)
            _gameLoop = new DispatcherTimer(DispatcherPriority.Render);
            _gameLoop.Interval = TimeSpan.FromMilliseconds(16.67);
            _gameLoop.Tick += GameLoop_Tick;

            // Pre-allocate object pools
            InitializeObjectPools();

            UpdateHighScoreDisplay();
        }

        private void InitializeObjectPools()
        {
            // Pre-create shapes and text blocks to avoid allocations during gameplay
            // Estimate: ~60 enemies + ~20 projectiles + ~5 power-ups + player + boss = ~100 shapes
            for (int i = 0; i < 150; i++)
            {
                _shapePool.Add(new Rectangle());
            }
            for (int i = 0; i < 100; i++)
            {
                _textPool.Add(new TextBlock { FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, Foreground = Brushes.White });
            }
        }

        private Shape GetPooledRectangle()
        {
            if (_shapePoolIndex < _shapePool.Count)
            {
                var shape = _shapePool[_shapePoolIndex++];
                if (shape is Rectangle rect)
                {
                    rect.Effect = null; // Clear any effects
                    return rect;
                }
            }
            // Fallback: create new if pool exhausted
            return new Rectangle();
        }

        private System.Windows.Shapes.Ellipse GetPooledEllipse()
        {
            // For simplicity, create ellipses on demand (fewer of them)
            return new System.Windows.Shapes.Ellipse();
        }

        private Polygon GetPooledPolygon()
        {
            return new Polygon();
        }

        private TextBlock GetPooledTextBlock()
        {
            if (_textPoolIndex < _textPool.Count)
            {
                return _textPool[_textPoolIndex++];
            }
            return new TextBlock { FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, Foreground = Brushes.White };
        }

        private void ResetPools()
        {
            _shapePoolIndex = 0;
            _textPoolIndex = 0;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize game engine with canvas dimensions
            _game = new GameEngine(GameCanvas.ActualWidth, GameCanvas.ActualHeight);
            SubscribeToGameEvents();

            // Focus window for keyboard input
            Focus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _gameLoop.Stop();
        }

        private void SubscribeToGameEvents()
        {
            _game.OnScoreChanged += () => Dispatcher.Invoke(() => ScoreText.Text = _game.Score.ToString());
            _game.OnWaveChanged += () => Dispatcher.Invoke(() => WaveText.Text = _game.Wave.ToString());
            _game.OnLivesChanged += () => Dispatcher.Invoke(UpdateLivesDisplay);
            _game.OnGameOver += () => Dispatcher.Invoke(ShowGameOver);
            _game.OnPowerUpCollected += type => Dispatcher.Invoke(() => OnPowerUpCollected(type));
            _game.OnBossDefeated += () => Dispatcher.Invoke(() =>
            {
                BossHealthBar.Visibility = System.Windows.Visibility.Collapsed;
                _sound.PlayBossDefeated();
            });
            _game.OnEnemyDestroyed += () => _sound.PlayEnemyDestroyed();
            _game.OnPlayerHit += () => Dispatcher.Invoke(() =>
            {
                _sound.PlayPlayerHit();
                PlayDamageEffect();
            });
            _game.OnPlayerShoot += () => _sound.PlayShoot();
            _game.OnBossHit += () => _sound.PlayBossHit();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            _input.KeyDown(e.Key);

            // Handle pause toggle
            if (_input.IsPausePressed && (_game.State == GameState.Playing ||
                                           _game.State == GameState.Paused ||
                                           _game.State == GameState.BossWave))
            {
                _game.TogglePause();
                PauseOverlay.Visibility = _game.State == GameState.Paused ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                _input.ClearPauseKey();
            }
        }

        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            _input.KeyUp(e.Key);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartOverlay.Visibility = System.Windows.Visibility.Collapsed;
            GameOverOverlay.Visibility = System.Windows.Visibility.Collapsed;
            PauseOverlay.Visibility = System.Windows.Visibility.Collapsed;
            BossHealthBar.Visibility = System.Windows.Visibility.Collapsed;
            ShieldIndicator.Visibility = System.Windows.Visibility.Collapsed;
            RapidFireIndicator.Visibility = System.Windows.Visibility.Collapsed;

            _input.Reset();
            _game.Start();
            _gameLoop.Start();

            Focus();
        }

        private void SoundToggle_Click(object sender, RoutedEventArgs e)
        {
            _sound.ToggleMute();
            SoundToggle.Content = _sound.IsMuted ? "🔇" : "🔊";
        }

        private void GameLoop_Tick(object sender, EventArgs e)
        {
            if (_game.State == GameState.Playing || _game.State == GameState.BossWave)
            {
                _game.Update(_input);
                UpdatePowerUpIndicators();
                UpdateBossHealthBar();
                Render();
            }
        }

        private void Render()
        {
            GameCanvas.Children.Clear();
            ResetPools();

            // Draw player
            DrawPlayer();

            // Draw enemies
            foreach (var enemy in _game.Enemies.Where(en => en.IsAlive))
            {
                DrawEnemy(enemy);
            }

            // Draw boss
            if (_game.CurrentBoss != null && _game.CurrentBoss.IsAlive)
            {
                DrawBoss(_game.CurrentBoss);
            }

            // Draw projectiles
            foreach (var projectile in _game.Projectiles)
            {
                DrawProjectile(projectile);
            }

            // Draw power-ups
            foreach (var powerUp in _game.PowerUps.Where(p => p.IsActive))
            {
                DrawPowerUp(powerUp);
            }
        }

        private void DrawPlayer()
        {
            var player = _game.Player;

            // Draw rocket ship
            var rocket = new System.Windows.Shapes.Path
            {
                Data = RocketGeometry,
                Fill = player.HasShield ? PlayerShieldBrush : PlayerBrush,
                Stroke = Brushes.White,
                StrokeThickness = 1,
                Stretch = Stretch.Uniform,
                Width = player.Width,
                Height = player.Height
            };

            Canvas.SetLeft(rocket, player.X);
            Canvas.SetTop(rocket, player.Y);
            GameCanvas.Children.Add(rocket);

            // Shield glow effect
            if (player.HasShield)
            {
                var shield = GetPooledEllipse();
                shield.Width = player.Width + 20;
                shield.Height = player.Height + 20;
                shield.Stroke = PlayerShieldBrush;
                shield.StrokeThickness = 2;
                shield.Fill = null;

                Canvas.SetLeft(shield, player.X - 10);
                Canvas.SetTop(shield, player.Y - 10);
                GameCanvas.Children.Add(shield);
            }
        }

        private void DrawEnemy(Enemy enemy)
        {
            switch (enemy.Type)
            {
                case EnemyType.Clash:
                    var invader = new System.Windows.Shapes.Path
                    {
                        Data = InvaderGeometry,
                        Fill = ClashBrush,
                        Stroke = Brushes.White,
                        StrokeThickness = 0.5,
                        Stretch = Stretch.Uniform,
                        Width = enemy.Width,
                        Height = enemy.Height
                    };
                    Canvas.SetLeft(invader, enemy.X);
                    Canvas.SetTop(invader, enemy.Y);
                    GameCanvas.Children.Add(invader);
                    break;

                case EnemyType.Warning:
                    var ghost = new System.Windows.Shapes.Path
                    {
                        Data = GhostGeometry,
                        Fill = WarningBrush,
                        Stroke = Brushes.White,
                        StrokeThickness = 0.5,
                        Stretch = Stretch.Uniform,
                        Width = enemy.Width,
                        Height = enemy.Height
                    };
                    Canvas.SetLeft(ghost, enemy.X);
                    Canvas.SetTop(ghost, enemy.Y);
                    GameCanvas.Children.Add(ghost);
                    break;

                case EnemyType.Error:
                    var skull = new System.Windows.Shapes.Path
                    {
                        Data = SkullGeometry,
                        Fill = ErrorBrush,
                        Stroke = Brushes.White,
                        StrokeThickness = 0.5,
                        Stretch = Stretch.Uniform,
                        Width = enemy.Width,
                        Height = enemy.Height
                    };
                    Canvas.SetLeft(skull, enemy.X);
                    Canvas.SetTop(skull, enemy.Y);
                    GameCanvas.Children.Add(skull);
                    break;
            }
        }

        private void DrawBoss(Boss boss)
        {
            // Main virus body
            var virus = new System.Windows.Shapes.Path
            {
                Data = VirusGeometry,
                Fill = BossBrush,
                Stroke = Brushes.White,
                StrokeThickness = 1,
                Stretch = Stretch.Uniform,
                Width = boss.Width,
                Height = boss.Height
            };

            Canvas.SetLeft(virus, boss.X);
            Canvas.SetTop(virus, boss.Y);
            GameCanvas.Children.Add(virus);

            // Boss text
            var text = GetPooledTextBlock();
            text.Text = "CORRUPT MODEL";
            text.FontSize = 14;

            Canvas.SetLeft(text, boss.X + boss.Width / 2 - 35);
            Canvas.SetTop(text, boss.Y + boss.Height + 5);
            GameCanvas.Children.Add(text);
        }

        private void DrawProjectile(Projectile projectile)
        {
            var rect = GetPooledRectangle() as Rectangle;
            rect.Width = projectile.Width;
            rect.Height = projectile.Height;
            rect.Fill = projectile.IsPlayerProjectile ? ProjectileBrush : EnemyProjectileBrush;
            rect.RadiusX = 2;
            rect.RadiusY = 2;
            rect.Stroke = null;
            rect.StrokeThickness = 0;

            // REMOVED: DropShadowEffect - this was the main performance killer

            Canvas.SetLeft(rect, projectile.X);
            Canvas.SetTop(rect, projectile.Y);
            GameCanvas.Children.Add(rect);
        }

        private void DrawPowerUp(PowerUp powerUp)
        {
            var rect = GetPooledRectangle() as Rectangle;
            rect.Width = powerUp.Width;
            rect.Height = powerUp.Height;
            rect.Stroke = Brushes.White;
            rect.StrokeThickness = 2;
            rect.RadiusX = 5;
            rect.RadiusY = 5;

            // Use cached brushes instead of parsing color strings
            switch (powerUp.Type)
            {
                case PowerUpType.PurgeAll:
                    rect.Fill = AuditBombBrush;
                    break;
                case PowerUpType.AdminMode:
                    rect.Fill = RapidFireBrush;
                    break;
                case PowerUpType.BackupSave:
                    rect.Fill = ShieldPowerUpBrush;
                    break;
            }

            // REMOVED: Pulsing opacity effect for performance

            Canvas.SetLeft(rect, powerUp.X);
            Canvas.SetTop(rect, powerUp.Y);
            GameCanvas.Children.Add(rect);

            // Label
            var label = GetPooledTextBlock();
            label.Text = powerUp.GetLabel();
            label.FontSize = 14;

            Canvas.SetLeft(label, powerUp.X + powerUp.Width / 2 - 5);
            Canvas.SetTop(label, powerUp.Y + powerUp.Height / 2 - 9);
            GameCanvas.Children.Add(label);
        }

        private void UpdateLivesDisplay()
        {
            LivesText.Text = _game.Player.Lives.ToString();
        }

        private void UpdatePowerUpIndicators()
        {
            ShieldIndicator.Visibility = _game.Player.HasShield ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            RapidFireIndicator.Visibility = _game.Player.HasRapidFire ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        private void UpdateBossHealthBar()
        {
            if (_game.CurrentBoss != null && _game.CurrentBoss.IsAlive)
            {
                BossHealthBar.Visibility = System.Windows.Visibility.Visible;
                BossHealthFill.Width = BossHealthBar.ActualWidth * _game.CurrentBoss.HealthPercentage;
            }
            else
            {
                BossHealthBar.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void OnPowerUpCollected(PowerUpType type)
        {
            _sound.PlayPowerUp();

            if (type == PowerUpType.PurgeAll)
            {
                PlayAuditBombFlash();
            }
        }

        private void PlayAuditBombFlash()
        {
            // Quick flash animation: fade in fast, hold briefly, fade out
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 0.7,
                Duration = TimeSpan.FromMilliseconds(50)
            };

            var fadeOut = new DoubleAnimation
            {
                From = 0.7,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(150) // Hold for 150ms before fading
            };

            var storyboard = new Storyboard();
            storyboard.Children.Add(fadeIn);
            storyboard.Children.Add(fadeOut);

            Storyboard.SetTarget(fadeIn, AuditBombFlash);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
            Storyboard.SetTarget(fadeOut, AuditBombFlash);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));

            storyboard.Begin();
        }

        private void PlayDamageEffect()
        {
            // Red flash effect
            var flashIn = new DoubleAnimation
            {
                From = 0,
                To = 0.4,
                Duration = TimeSpan.FromMilliseconds(50)
            };

            var flashOut = new DoubleAnimation
            {
                From = 0.4,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(150),
                BeginTime = TimeSpan.FromMilliseconds(50)
            };

            var flashStoryboard = new Storyboard();
            flashStoryboard.Children.Add(flashIn);
            flashStoryboard.Children.Add(flashOut);

            Storyboard.SetTarget(flashIn, DamageFlash);
            Storyboard.SetTargetProperty(flashIn, new PropertyPath(OpacityProperty));
            Storyboard.SetTarget(flashOut, DamageFlash);
            Storyboard.SetTargetProperty(flashOut, new PropertyPath(OpacityProperty));

            flashStoryboard.Begin();

            // Screen shake effect
            var shakeStoryboard = new Storyboard();

            // Shake X axis
            var shakeX = new DoubleAnimationUsingKeyFrames();
            shakeX.KeyFrames.Add(new LinearDoubleKeyFrame(8, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(25))));
            shakeX.KeyFrames.Add(new LinearDoubleKeyFrame(-8, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(50))));
            shakeX.KeyFrames.Add(new LinearDoubleKeyFrame(6, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(75))));
            shakeX.KeyFrames.Add(new LinearDoubleKeyFrame(-4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100))));
            shakeX.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(125))));

            // Shake Y axis
            var shakeY = new DoubleAnimationUsingKeyFrames();
            shakeY.KeyFrames.Add(new LinearDoubleKeyFrame(-4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(25))));
            shakeY.KeyFrames.Add(new LinearDoubleKeyFrame(6, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(50))));
            shakeY.KeyFrames.Add(new LinearDoubleKeyFrame(-4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(75))));
            shakeY.KeyFrames.Add(new LinearDoubleKeyFrame(2, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100))));
            shakeY.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(125))));

            shakeStoryboard.Children.Add(shakeX);
            shakeStoryboard.Children.Add(shakeY);

            Storyboard.SetTarget(shakeX, CanvasShake);
            Storyboard.SetTargetProperty(shakeX, new PropertyPath(TranslateTransform.XProperty));
            Storyboard.SetTarget(shakeY, CanvasShake);
            Storyboard.SetTargetProperty(shakeY, new PropertyPath(TranslateTransform.YProperty));

            shakeStoryboard.Begin();
        }

        private void ShowGameOver()
        {
            _gameLoop.Stop();
            _sound.PlayGameOver();

            FinalScoreText.Text = _game.Score.ToString();
            FinalWaveText.Text = _game.Wave.ToString();

            // Check for high score
            if (_scoreManager.IsHighScore(_game.Score))
            {
                _sound.PlayHighScore();
                var dialog = new HighScoreDialog(_game.Score, _game.Wave);
                dialog.Owner = this;

                if (dialog.ShowDialog() == true)
                {
                    _scoreManager.AddScore(dialog.PlayerInitials, _game.Score, _game.Wave);
                    UpdateHighScoreDisplay();
                }
            }

            GameOverOverlay.Visibility = System.Windows.Visibility.Visible;
        }

        private void UpdateHighScoreDisplay()
        {
            HighScoreText.Text = _scoreManager.GetTopScore().ToString();

            var displayList = new List<object>();
            int rank = 1;
            foreach (var entry in _scoreManager.HighScores)
            {
                displayList.Add(new
                {
                    Rank = $"{rank}.",
                    entry.Initials,
                    entry.Score,
                    WaveDisplay = $"W{entry.Wave}"
                });
                rank++;
            }

            // Pad with empty entries if needed
            while (displayList.Count < 5)
            {
                displayList.Add(new
                {
                    Rank = $"{displayList.Count + 1}.",
                    Initials = "---",
                    Score = 0,
                    WaveDisplay = "W0"
                });
            }

            HighScoresList.ItemsSource = displayList;
        }

        private void About_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }
    }
}