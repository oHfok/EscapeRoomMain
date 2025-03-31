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
        private readonly string[] questions = { "Ile to 6 + 3?", "Jakiej litery brakuje: A, B, _, D?", "Znajdź hasło", "Ile jest planet w układzie słonecznym?", "traffic_light", "memory_game" };
        private readonly string[] answers = { "9", "c", "", "8", "", "" };
        private DateTime startTime;
        private string password = string.Empty;
        private double initialBarWidth;

        // Memory game variables
        private List<Button> memoryButtons = new();
        private List<string> emojis = new();
        private Button? firstFlippedTile;
        private Button? secondFlippedTile;
        private bool isProcessing = false;
        private int matchesFound = 0;

        // Traffic light puzzle variables
        private Frame? draggedLight = null;
        private Dictionary<string, Frame> lightSlots = new();
        private Dictionary<Frame, string> slotContents = new();

        public MainPage()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            ResetUI();
            gameTimer = new System.Timers.Timer(1000);
            gameTimer.Elapsed += OnTimerElapsed;
            TimerBar.WidthRequest = 300;

            // Initialize the traffic light slots
            lightSlots = new Dictionary<string, Frame>
            {
                { "Top", TopLightSlot },
                { "Middle", MiddleLightSlot },
                { "Bottom", BottomLightSlot }
            };

            slotContents = new Dictionary<Frame, string>();
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
            TrafficLightGrid.IsVisible = false;
            StartButton.IsVisible = true;

            // Reset traffic light puzzle
            if (lightSlots != null && lightSlots.Count > 0)
            {
                foreach (var slot in lightSlots.Values)
                {
                    slot.BackgroundColor = Colors.LightGray;
                }
            }

            if (slotContents != null)
            {
                slotContents.Clear();
            }

            // Make the colored lights visible again
            if (RedLight != null) RedLight.IsVisible = true;
            if (YellowLight != null) YellowLight.IsVisible = true;
            if (GreenLight != null) GreenLight.IsVisible = true;
        }

        private void StartGame(object sender, EventArgs e)
        {
            // Explicitly hide success and game over layouts first
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

            TimerBorder.IsVisible = true;
            TimerBar.IsVisible = true;
            TimerLabel.IsVisible = true;
            TimerLabel.TextColor = Colors.White; // Reset text color
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

                // Hide all puzzle layouts first
                PuzzleLayout.IsVisible = false;
                MemoryGridLayout.IsVisible = false;
                TrafficLightGrid.IsVisible = false;

                if (questions[currentQuestion] == "memory_game")
                {
                    MemoryGridLayout.IsVisible = true;
                    WarningEmojiButton.IsVisible = false;
                    FindPasswordLabel.IsVisible = false;
                    StartMemoryGame();
                }
                else if (questions[currentQuestion] == "traffic_light")
                {
                    TrafficLightGrid.IsVisible = true;
                    InitializeTrafficLightPuzzle();
                }
                else
                {
                    PuzzleLayout.IsVisible = true;
                    PuzzleQuestion.Text = questions[currentQuestion];
                    PuzzleAnswer.Text = string.Empty;
                    IncorrectAnswerLabel.IsVisible = false;
                    WarningEmojiButton.IsVisible = currentQuestion == 2;
                    FindPasswordLabel.IsVisible = currentQuestion == 2;
                    PuzzleAnswer.IsVisible = true;
                    if (currentQuestion == 2) GenerateEmojiPassword();
                }
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
            if (currentQuestion >= questions.Length || questions[currentQuestion] == "memory_game" || questions[currentQuestion] == "traffic_light")
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
                TrafficLightGrid.IsVisible = false;

                // Calculate time taken
                double secondsTaken = Math.Round((DateTime.Now - startTime).TotalSeconds, 2);

                // Show the appropriate end screen
                if (isTimeout)
                {
                    TimeTakenLabel.Text = $"Time Taken: {secondsTaken} seconds";
                    GameOverLayout.IsVisible = true;
                    SuccessLayout.IsVisible = false;
                }
                else
                {
                    TimeTakenSuccessLabel.Text = $"Time Taken: {secondsTaken} seconds";
                    SuccessLayout.IsVisible = true;
                    GameOverLayout.IsVisible = false;
                }
            });
        }

        #region Memory Game

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

        #endregion

        #region Traffic Light Puzzle

        private void InitializeTrafficLightPuzzle()
        {
            // Reset the traffic light puzzle
            foreach (var slot in lightSlots.Values)
            {
                slot.BackgroundColor = Colors.LightGray;
            }

            slotContents.Clear();

            // Make the colored lights visible again
            RedLight.IsVisible = true;
            YellowLight.IsVisible = true;
            GreenLight.IsVisible = true;
        }

        private void OnLightDragStarting(object sender, DragStartingEventArgs e)
        {
            if (sender is Frame light)
            {
                draggedLight = light;
                e.Data.Properties.Add("color", light.BackgroundColor.ToString());
            }
        }

        private void OnLightSlotDropCompleted(object sender, DropCompletedEventArgs e)
        {
            if (sender is Frame slot && draggedLight != null)
            {
                // If this slot already has a light, return it to visibility
                if (slotContents.ContainsKey(slot))
                {
                    string currentColor = slotContents[slot];
                    if (currentColor == "Red")
                        RedLight.IsVisible = true;
                    else if (currentColor == "Yellow")
                        YellowLight.IsVisible = true;
                    else if (currentColor == "Green")
                        GreenLight.IsVisible = true;
                }

                // Set the slot to the color of the dragged light
                slot.BackgroundColor = draggedLight.BackgroundColor;

                // Hide the dragged light
                draggedLight.IsVisible = false;

                // Update the slot contents
                string draggedColor = "";
                if (draggedLight == RedLight)
                    draggedColor = "Red";
                else if (draggedLight == YellowLight)
                    draggedColor = "Yellow";
                else if (draggedLight == GreenLight)
                    draggedColor = "Green";

                slotContents[slot] = draggedColor;

                // Reset the draggedLight
                draggedLight = null;
            }
        }

        private void CheckTrafficLightSolution(object sender, EventArgs e)
        {
            // Check if all slots have lights
            if (slotContents.Count < 3)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                    await DisplayAlert("Incomplete", "Place all three colors in the traffic light", "OK"));
                return;
            }

            // Check if the order is correct (Red at top, Yellow in middle, Green at bottom)
            bool isCorrect =
                slotContents.ContainsKey(TopLightSlot) && slotContents[TopLightSlot] == "Red" &&
                slotContents.ContainsKey(MiddleLightSlot) && slotContents[MiddleLightSlot] == "Yellow" &&
                slotContents.ContainsKey(BottomLightSlot) && slotContents[BottomLightSlot] == "Green";

            if (isCorrect)
            {
                currentQuestion++;
                ShowPuzzle();
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                    await DisplayAlert("Incorrect", "The traffic light colors are not in the correct order", "Try Again"));
            }
        }

        #endregion
    }
}