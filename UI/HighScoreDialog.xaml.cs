using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BIMDefender.UI
{
    /// <summary>
    /// Dialog for entering player initials after achieving a high score
    /// </summary>
    public partial class HighScoreDialog : Window
    {
        public string PlayerInitials { get; private set; }
        public int Score { get; }
        public int Wave { get; }

        public HighScoreDialog(int score, int wave)
        {
            InitializeComponent();
            
            Score = score;
            Wave = wave;
            
            ScoreDisplay.Text = score.ToString();
            WaveDisplay.Text = wave.ToString();
            
            // Focus first initial box
            Loaded += (s, e) => Initial1.Focus();
        }

        private void Initial_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            
            // Auto-advance to next box
            if (textBox.Text.Length == 1)
            {
                if (textBox == Initial1)
                    Initial2.Focus();
                else if (textBox == Initial2)
                    Initial3.Focus();
            }

            // Update submit button state
            UpdateSubmitButton();
        }

        private void Initial_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;

            // Handle backspace to go to previous box
            if (e.Key == Key.Back && textBox.Text.Length == 0)
            {
                if (textBox == Initial3)
                {
                    Initial2.Focus();
                    Initial2.SelectAll();
                }
                else if (textBox == Initial2)
                {
                    Initial1.Focus();
                    Initial1.SelectAll();
                }
                e.Handled = true;
            }

            // Handle Enter to submit
            if (e.Key == Key.Enter && SubmitButton.IsEnabled)
            {
                Submit();
            }

            // Only allow letters
            if (e.Key >= Key.A && e.Key <= Key.Z)
            {
                return; // Allow
            }

            // Allow navigation keys
            if (e.Key == Key.Tab || e.Key == Key.Back || e.Key == Key.Delete ||
                e.Key == Key.Left || e.Key == Key.Right)
            {
                return; // Allow
            }

            e.Handled = true; // Block everything else
        }

        private void UpdateSubmitButton()
        {
            SubmitButton.IsEnabled = 
                Initial1.Text.Length == 1 && 
                Initial2.Text.Length == 1 && 
                Initial3.Text.Length == 1;
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            Submit();
        }

        private void Submit()
        {
            PlayerInitials = $"{Initial1.Text}{Initial2.Text}{Initial3.Text}".ToUpper();
            DialogResult = true;
            Close();
        }
    }
}
