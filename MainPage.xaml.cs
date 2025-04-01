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
        // siema jestem agnieszka
        private int timeLeft = 60;
        private readonly System.Timers.Timer gameTimer;
        private bool gameEnded;
        private int currentQuestion;
        private readonly string[] questions = { "Ojciec i syn mają razem 36 lat. Wiedząc, że ojciec jest o 30 lat starszy od syna, powiedz ile lat ma syn. ",
 "Znajdź hasło",
 "Jaka liczba, podzielona przez siebie samą, będzie wynikiem tego dzielenia?",
 "memory_game" };
        private readonly string[] answers = { "3", "", "1", "" };
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
            TimerBar.WidthRequest = 300;
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
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
            StartButton.IsVisible = true;
        }

        private void StartGame(object sender, EventArgs e)
        {
            SuccessLayout.IsVisible = false;
            GameOverLayout.IsVisible = false;
            StartButton.IsVisible = false;

            startTime = DateTime.Now;
            timeLeft = 60;
            currentQuestion = 0;
            gameEnded = false;

            initialBarWidth = Width * 0.8;
            TimerBar.WidthRequest = initialBarWidth;
            TimerBar.BackgroundColor = Colors.Green;

            TimerBorder.IsVisible = TimerBar.IsVisible = TimerLabel.IsVisible = true;
            TimerLabel.TextColor = Colors.White;
            TimerBar.Margin = new Thickness(0, 50, 0, 0);

            ShowPuzzle();
            gameTimer.Start();
        }


        private void UpdateTimerBar()
        {
            double proportion = (double)timeLeft / 60.0;
            double remainingWidth = proportion * initialBarWidth;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                TimerBar.WidthRequest = remainingWidth;

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
                TimerLabel.Text = $"Pozostały czas: {timeLeft}";

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
        MainThread.BeginInvokeOnMainThread(async () => await DisplayAlert("Hasło", password, "OK"));

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
                    WarningEmojiButton.IsVisible = currentQuestion == 1;
                    FindPasswordLabel.IsVisible = currentQuestion == 1;
                    PuzzleAnswer.IsVisible = true;
                    if (currentQuestion == 1) GenerateEmojiPassword();
                }
            });
        }

        private void GenerateEmojiPassword()
        {
            password = string.Empty;
            Random rand = new Random();
            string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            for (int i = 0; i < 4; i++) password += letters[rand.Next(letters.Length)];
            answers[1] = password;
        }

        private void SubmitAnswer(object sender, EventArgs e)
        {
            if (currentQuestion >= questions.Length || questions[currentQuestion] == "memory_game")
            {
                return;
            }

            string answer = PuzzleAnswer.Text?.Trim() ?? "";
            bool isCorrect = currentQuestion == 1
            ? answer.ToUpper() == answers[1].ToUpper()
            : answer.ToUpper() == answers[currentQuestion].ToUpper();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (isCorrect)
                {
                    PuzzleLayout.IsVisible = false;
                    try
                    {
                        Preferences.Set(questions[currentQuestion], true);
                    }
                    catch { }

                    currentQuestion++;
                    if (currentQuestion >= questions.Length)
                        EndGame(false);
                    else
                        ShowPuzzle();
                }
                else
                    IncorrectAnswerLabel.IsVisible = true;
            });
        }

        private void EndGame(bool isTimeout)
        {
            gameTimer.Stop();
            gameEnded = true;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Hide all game elements first
                WarningEmojiButton.IsVisible = false;
                FindPasswordLabel.IsVisible = false;
                PuzzleAnswer.IsVisible = false;
                TimerLabel.IsVisible = false;
                TimerBorder.IsVisible = false;
                TimerBar.IsVisible = false;
                PuzzleLayout.IsVisible = false;
                MemoryGridLayout.IsVisible = false;

                // Calculate time taken
                double secondsTaken = Math.Round((DateTime.Now - startTime).TotalSeconds, 2);

                // Show the appropriate end screen
                if (isTimeout)
                {
                    TimeTakenLabel.Text = $"Ukończono w: {secondsTaken} sekund";
                    GameOverLayout.IsVisible = true;
                    SuccessLayout.IsVisible = false;
                }
                else
                {
                    TimeTakenSuccessLabel.Text = $"Ukończono w: {secondsTaken} sekund";
                    SuccessLayout.IsVisible = true;
                    GameOverLayout.IsVisible = false;
                }
            });
        }

        private void StartMemoryGame()
        {
            matchesFound = 0;
            firstFlippedTile = null;
            secondFlippedTile = null;
            isProcessing = false;

            emojis = new List<string> {
 "😊", "😊", "😂", "😂", "😍", "😍", "😎", "😎",
 "🤔", "🤔", "🔥", "🔥", "🎉", "🎉", "🌟", "🌟" };

            emojis = emojis.OrderBy(x => Guid.NewGuid()).ToList();

            memoryButtons = new List<Button>();
            MemoryGridLayout.Children.Clear();

            Label headerLabel = new Label
            {
                Text = "Ukończ grę pamięciową, aby przejść dalej",
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
                        BackgroundColor = Colors.LightBlue,
                        TextColor = Colors.Transparent,
                        FontSize = 24,
                        WidthRequest = 70,
                        HeightRequest = 70,
                        CommandParameter = row * 4 + col
                    };
                    button.Clicked += OnMemoryTileClicked;
                    Grid.SetRow(button, row + 1);
                    Grid.SetColumn(button, col);
                    MemoryGridLayout.Children.Add(button);
                    memoryButtons.Add(button);
                }
            }
        }

        private async void OnMemoryTileClicked(object sender, EventArgs e)
        {
            if (isProcessing) return;
            if (sender is not Button button) return;
            if (button.TextColor == Colors.Black) return;

            button.TextColor = Colors.Black;

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

                    if (matchesFound == 8)
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
        }
    }
}
