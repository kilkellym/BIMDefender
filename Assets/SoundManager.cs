using System;
using System.Media;
using System.IO;
using System.Threading.Tasks;

namespace BIMDefender.Assets
{
    /// <summary>
    /// Manages retro sound effects for the game.
    /// Uses simple beep tones since we can't bundle audio files easily.
    /// </summary>
    public class SoundManager
    {
        public bool IsMuted { get; set; } = false;

        /// <summary>
        /// Play a short beep for player shooting
        /// </summary>
        public void PlayShoot()
        {
            if (IsMuted) return;
            PlayBeepAsync(800, 50);
        }

        /// <summary>
        /// Play sound for enemy destroyed
        /// </summary>
        public void PlayEnemyDestroyed()
        {
            if (IsMuted) return;
            PlayBeepAsync(400, 100);
        }

        /// <summary>
        /// Play sound for player hit
        /// </summary>
        public void PlayPlayerHit()
        {
            if (IsMuted) return;
            PlayDescendingBeepsAsync();
        }

        /// <summary>
        /// Play sound for power-up collected
        /// </summary>
        public void PlayPowerUp()
        {
            if (IsMuted) return;
            PlayAscendingBeepsAsync();
        }

        /// <summary>
        /// Play sound for boss hit
        /// </summary>
        public void PlayBossHit()
        {
            if (IsMuted) return;
            PlayBeepAsync(200, 80);
        }

        /// <summary>
        /// Play sound for boss defeated
        /// </summary>
        public void PlayBossDefeated()
        {
            if (IsMuted) return;
            PlayVictoryFanfareAsync();
        }

        /// <summary>
        /// Play game over sound
        /// </summary>
        public void PlayGameOver()
        {
            if (IsMuted) return;
            PlayGameOverSoundAsync();
        }

        /// <summary>
        /// Play new high score sound
        /// </summary>
        public void PlayHighScore()
        {
            if (IsMuted) return;
            PlayHighScoreFanfareAsync();
        }

        // Async beep methods to avoid blocking the game loop

        private async void PlayBeepAsync(int frequency, int duration)
        {
            try
            {
                await Task.Run(() => Console.Beep(frequency, duration));
            }
            catch
            {
                // Silently fail if beep not supported
            }
        }

        private async void PlayDescendingBeepsAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    Console.Beep(600, 80);
                    Console.Beep(400, 80);
                    Console.Beep(200, 120);
                });
            }
            catch { }
        }

        private async void PlayAscendingBeepsAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    Console.Beep(400, 60);
                    Console.Beep(600, 60);
                    Console.Beep(800, 100);
                });
            }
            catch { }
        }

        private async void PlayVictoryFanfareAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    Console.Beep(523, 100); // C
                    Console.Beep(659, 100); // E
                    Console.Beep(784, 100); // G
                    Console.Beep(1047, 200); // High C
                });
            }
            catch { }
        }

        private async void PlayGameOverSoundAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    Console.Beep(400, 150);
                    Console.Beep(350, 150);
                    Console.Beep(300, 150);
                    Console.Beep(250, 300);
                });
            }
            catch { }
        }

        private async void PlayHighScoreFanfareAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    Console.Beep(523, 80);
                    Console.Beep(659, 80);
                    Console.Beep(784, 80);
                    Console.Beep(1047, 80);
                    Console.Beep(784, 80);
                    Console.Beep(1047, 150);
                });
            }
            catch { }
        }

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
        }
    }
}
