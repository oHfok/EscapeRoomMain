using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Maui.Controls;


namespace Dawidek
{
    public partial class MainPage : ContentPage
    {
        private int timeLeft = 60;
        private readonly System.Timers.Timer gameTimer;
        private bool gameEnded;
        private int currentQuestion;
        private readonly string[] questions = { "Ile to 6 + 3?", "Jakiej litery brakuje: A, B, _, D?", "Znajdź hasło", "Ile jest planet w układzie słonecznym?", "memory_game" };
        private readonly string[] answers = { "9", "c", "", "8", "" };
        private DateTime startTime;
        private string password = string.Empty;
        private double initialBarWidth;

        private List<Button> memoryButtons = new();
        private List<string> emojis = new();
        private Button? firstFlippedTile;
        private Button? secondFlippedTile;
        private bool isProcessing = false;
        private int matchesFound = 0;

        public MainPage()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            ResetUI();
            gameTimer = new System.Timers.Timer(1000);
            gameTimer.Elapsed += OnTimerElapsed;
            // Avoid DeviceDisplay in constructor which can cause issues
            TimerBar.WidthRequest = 300; // Use a fixed default width initially
            System.Diagnostics.Debug.WriteLine("MainPage Constructor Called");
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            // Update initialBarWidth based on actual page width
            initialBarWidth = width * 0.8;
        }

        private void ResetUI()
        {
            SuccessLayout.IsVisible = false;
            GameOverLayout.IsVisible = false;
            PuzzleLayout.IsVisible = false;
            TimerBorder.IsVisible = false;
            TimerLabel.IsVisible = false;
            TimerBar.IsVisible = false;

            WarningEmojiButton.IsVisible = false;
            IncorrectAnswerLabel.IsVisible = false;
            FindPasswordLabel.IsVisible = false;
            PuzzleAnswer.IsVisible = false;
            MemoryGridLayout.IsVisible = false;

            // Make sure start button is visible
            StartButton.IsVisible = true;

            System.Diagnostics.Debug.WriteLine("ResetUI Called");
        }

        private void StartGame(object sender, EventArgs e)
        {
            
            StartButton.IsVisible = false;
            GameOverLayout.IsVisible = false;
            startTime = DateTime.Now;
            timeLeft = 60;
            currentQuestion = 0;
            gameEnded = false;

            // Use platform independent way to get screen width
            initialBarWidth = Width * 0.8; // Use page width instead of DeviceDisplay
            TimerBar.WidthRequest = initialBarWidth;
            TimerBar.BackgroundColor = Colors.Green;

            TimerBorder.IsVisible = true;
            TimerBar.IsVisible = true;
            TimerLabel.IsVisible = true;
            TimerBar.Margin = new Thickness(0, 50, 0, 0);

            ShowPuzzle();
            gameTimer.Start();
            System.Diagnostics.Debug.WriteLine("StartGame Called");
        }

        private void UpdateTimerBar()
        {
            // Calculate the proportion of time remaining and apply to width
            double proportion = (double)timeLeft / 60.0;
            double remainingWidth = proportion * initialBarWidth;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                TimerBar.WidthRequest = remainingWidth;

                // Smooth color transition based on time remaining
                if (timeLeft <= 10)
                    TimerBar.BackgroundColor = Colors.Red;
                else if (timeLeft <= 30)
                    TimerBar.BackgroundColor = Colors.Orange;
                else
                    TimerBar.BackgroundColor = Colors.Green;
            });
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (gameEnded) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                timeLeft--;
                TimerLabel.Text = $"Time Left: {timeLeft}";

                if (timeLeft <= 10)
                    TimerLabel.TextColor = Colors.Red;
                else if (timeLeft <= 30)
                    TimerLabel.TextColor = Colors.Orange;
                else
                    TimerLabel.TextColor = Colors.White;

                UpdateTimerBar();

                if (timeLeft <= 0)
                {
                    EndGame(true);
                }
            });
        }

        private void WarningEmojiClicked(object sender, EventArgs e) =>
            MainThread.BeginInvokeOnMainThread(async () => await DisplayAlert("Password", password, "OK"));

        private void ShowPuzzle()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (currentQuestion >= questions.Length)
                {
                    EndGame(false);
                    return;
                }

                if (questions[currentQuestion] == "memory_game")
                {
                    PuzzleLayout.IsVisible = false;
                    MemoryGridLayout.IsVisible = true;
                    WarningEmojiButton.IsVisible = false;
                    FindPasswordLabel.IsVisible = false;
                    StartMemoryGame();
                }
                else
                {
                    PuzzleLayout.IsVisible = true;
                    MemoryGridLayout.IsVisible = false;
                    PuzzleQuestion.Text = questions[currentQuestion];
                    PuzzleAnswer.Text = string.Empty;
                    IncorrectAnswerLabel.IsVisible = false;
                    WarningEmojiButton.IsVisible = currentQuestion == 2;
                    FindPasswordLabel.IsVisible = currentQuestion == 2;
                    PuzzleAnswer.IsVisible = true;
                    if (currentQuestion == 2) GenerateEmojiPassword();
                }
                System.Diagnostics.Debug.WriteLine($"ShowPuzzle Called. currentQuestion = {currentQuestion}");
            });
        }

        private void GenerateEmojiPassword()
        {
            password = string.Empty;
            Random rand = new Random();
            string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            for (int i = 0; i < 4; i++) password += letters[rand.Next(letters.Length)];
            answers[2] = password;
        }

        private void SubmitAnswer(object sender, EventArgs e)
        {
            if (currentQuestion >= questions.Length || questions[currentQuestion] == "memory_game")
            {
                return;
            }

            string answer = PuzzleAnswer.Text?.Trim() ?? "";
            bool isCorrect = currentQuestion == 2
                ? answer.ToUpper() == answers[2].ToUpper()
                : answer.ToUpper() == answers[currentQuestion].ToUpper();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (isCorrect)
                {
                    PuzzleLayout.IsVisible = false;
                    // Save progress safely with try/catch
                    try
                    {
                        Preferences.Set(questions[currentQuestion], true);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error saving preference: {ex.Message}");
                    }

                    currentQuestion++;
                    if (currentQuestion >= questions.Length)
                        EndGame(false);
                    else
                        ShowPuzzle();
                }
                else
                    IncorrectAnswerLabel.IsVisible = true;

                System.Diagnostics.Debug.WriteLine($"SubmitAnswer Called. currentQuestion = {currentQuestion}, isCorrect = {isCorrect}");
            });
        }

        private void EndGame(bool isTimeout)
        {
            gameTimer.Stop();
            gameEnded = true;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (isTimeout)
                    GameOverLayout.IsVisible = true;
                else
                    SuccessLayout.IsVisible = true;

                WarningEmojiButton.IsVisible = false;
                FindPasswordLabel.IsVisible = false;
                PuzzleAnswer.IsVisible = false;
                TimerLabel.IsVisible = false;
                TimerBorder.IsVisible = false;
                TimerBar.IsVisible = false;
                PuzzleLayout.IsVisible = false;
                MemoryGridLayout.IsVisible = false;

                double secondsTaken = Math.Round((DateTime.Now - startTime).TotalSeconds, 2);
                TimeTakenLabel.Text = $"Time Taken: {secondsTaken} seconds";
                TimeTakenSuccessLabel.Text = $"Time Taken: {secondsTaken} seconds";

                System.Diagnostics.Debug.WriteLine($"EndGame Called. isTimeout = {isTimeout}");
            });
        }

        private void StartMemoryGame()
        {
            matchesFound = 0;
            firstFlippedTile = null;
            secondFlippedTile = null;
            isProcessing = false;

            // Initialize emoji list inside the method to avoid potential issues
            emojis = new List<string> {
                "😊", "😊", "😂", "😂", "😍", "😍", "😎", "😎",
                "🤔", "🤔", "🔥", "🔥", "🎉", "🎉", "🌟", "🌟" }; // 16 emojis for 4x4 grid

            // Shuffle the emojis
            emojis = emojis.OrderBy(x => Guid.NewGuid()).ToList();

            // Create and add buttons to the grid
            memoryButtons = new List<Button>();
            MemoryGridLayout.Children.Clear();

            // Add the header label back
            Label headerLabel = new Label
            {
                Text = "Complete the memory game to pass",
                HorizontalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Black,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(headerLabel, 0);
            Grid.SetColumnSpan(headerLabel, 4);
            MemoryGridLayout.Children.Add(headerLabel);

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    var button = new Button
                    {
                        // Define styles inline rather than using resource dictionary
                        BackgroundColor = Colors.LightBlue,
                        TextColor = Colors.Transparent,
                        FontSize = 24,
                        WidthRequest = 70,
                        HeightRequest = 70,
                        CommandParameter = row * 4 + col
                    };
                    button.Clicked += OnMemoryTileClicked;
                    Grid.SetRow(button, row + 1); // +1 to account for the header
                    Grid.SetColumn(button, col);
                    MemoryGridLayout.Children.Add(button);
                    memoryButtons.Add(button);
                }
            }
            System.Diagnostics.Debug.WriteLine("StartMemoryGame Called");
        }

        private async void OnMemoryTileClicked(object sender, EventArgs e)
        {
            if (isProcessing) return;
            if (sender is not Button button) return;

            if (button.TextColor == Colors.Black) return; // Already revealed

            button.TextColor = Colors.Black;

            // Get the index from the command parameter
            if (button.CommandParameter is not int index) return;
            if (index < 0 || index >= emojis.Count) return;

            button.Text = emojis[index];

            if (firstFlippedTile == null)
            {
                firstFlippedTile = button;
            }
            else if (secondFlippedTile == null && button != firstFlippedTile)
            {
                secondFlippedTile = button;
                isProcessing = true;

                if (firstFlippedTile.Text == secondFlippedTile.Text)
                {
                    matchesFound++;
                    firstFlippedTile = null;
                    secondFlippedTile = null;
                    isProcessing = false;

                    if (matchesFound == 8) // 8 matches for 16 tiles
                    {
                        currentQuestion++;
                        ShowPuzzle();
                    }
                }
                else
                {
                    await Task.Delay(1000);
                    firstFlippedTile.TextColor = Colors.Transparent;
                    secondFlippedTile.TextColor = Colors.Transparent;
                    firstFlippedTile = null;
                    secondFlippedTile = null;
                    isProcessing = false;
                }
            }
            System.Diagnostics.Debug.WriteLine($"OnMemoryTileClicked Called. matchesFound = {matchesFound}");
        }
    }
}