using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Media.Effects;

// IMPORTANT: Make sure this namespace matches your project name exactly.
namespace PromodoroTimerGemini
{
    // =============================================================================================
    // TIMER SETUP WINDOW
    // =============================================================================================
    public class SetupWindow : Window
    {
        private TextBox minutesTextBox;

        public SetupWindow()
        {
            this.Title = "Setup Pomodoro Timer";
            this.Width = 320;
            this.Height = 200;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.FontFamily = new FontFamily("Segoe UI");

            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.ResizeMode = ResizeMode.NoResize;

            try
            {
                this.Icon = new BitmapImage(new Uri("pack://application:,,,/Assets/icon.ico"));
            }
            catch (Exception)
            {
                // Icon not found, do nothing.
            }

            string setupXaml = @"
            <Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                <Border Background='#EEF0F0F0' CornerRadius='12'>
                    <Border.Effect>
                        <DropShadowEffect ShadowDepth='0' BlurRadius='10' Color='#AAAAAA' Opacity='0.5'/>
                    </Border.Effect>
                    <Grid>
                        <Button Name='CloseButton' Content='❌' HorizontalAlignment='Right' VerticalAlignment='Top' Margin='10' Width='25' Height='25' Foreground='Gray' Background='Transparent' BorderThickness='0' Cursor='Hand' FontSize='10'/>
                        <StackPanel VerticalAlignment='Center' HorizontalAlignment='Center' Margin='20'>
                            <TextBlock Text='Set Timer Duration' FontSize='18' FontWeight='SemiBold' HorizontalAlignment='Center' Margin='0,0,0,10' Foreground='#333'/>
                            <StackPanel Orientation='Horizontal' HorizontalAlignment='Center'>
                                <TextBox Name='MinutesTextBox' Width='80' FontSize='16' Text='40' Padding='5' VerticalContentAlignment='Center' HorizontalContentAlignment='Center' />
                                <TextBlock Text='minutes' FontSize='16' VerticalAlignment='Center' Margin='10,0,0,0' Foreground='#555'/>
                            </StackPanel>
                            <Button Name='StartButton' Content='Start Timer' FontSize='16' Padding='10,5' Margin='0,20,0,0' Cursor='Hand' Background='#007ACC' Foreground='White' BorderThickness='0'/>
                        </StackPanel>
                    </Grid>
                </Border>
            </Grid>";

            var rootElement = (FrameworkElement)XamlReader.Parse(setupXaml);
            this.Content = rootElement;

            this.minutesTextBox = (TextBox)rootElement.FindName("MinutesTextBox");
            Button startButton = (Button)rootElement.FindName("StartButton");
            Button closeButton = (Button)rootElement.FindName("CloseButton");

            startButton.Click += StartButton_Click;
            closeButton.Click += (s, e) => this.Close();
            this.minutesTextBox.PreviewTextInput += MinutesTextBox_PreviewTextInput;
            this.MouseLeftButtonDown += (s, e) => this.DragMove();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(minutesTextBox.Text, out int minutes) && minutes > 0)
            {
                TimerWindow timerWindow = new TimerWindow(TimeSpan.FromMinutes(minutes), this);
                timerWindow.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Please enter a valid number of minutes.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void MinutesTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }
    }


    // =============================================================================================
    // TRANSPARENT TIMER WINDOW
    // =============================================================================================
    public class TimerWindow : Window
    {
        private TextBlock timerText;
        private Button pausePlayButton;
        private Border timerBackground;
        private Brush originalTimerBackground;

        private DispatcherTimer timer;
        private DispatcherTimer topmostTimer; // New timer to keep window on top
        private TimeSpan totalTime;
        private TimeSpan timeRemaining;
        private bool isPaused = false;
        private bool hasFinished = false; // Flag to track if timer has hit zero
        private Window setupWindow;
        private NotificationWindow notification;

        public TimerWindow(TimeSpan duration, Window parentSetupWindow)
        {
            this.totalTime = duration;
            this.timeRemaining = duration;
            this.setupWindow = parentSetupWindow;

            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.Width = 150;
            this.Height = 50;
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Left = SystemParameters.PrimaryScreenWidth - this.Width - 20;
            this.Top = SystemParameters.PrimaryScreenHeight - this.Height - 50;

            var buttonStyle = new Style(typeof(Button));
            buttonStyle.Setters.Add(new Setter(Button.BackgroundProperty, Brushes.Transparent));
            buttonStyle.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(0)));
            buttonStyle.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));
            buttonStyle.Setters.Add(new Setter(Button.FontSizeProperty, 16.0));
            buttonStyle.Setters.Add(new Setter(Button.WidthProperty, 30.0));
            buttonStyle.Setters.Add(new Setter(Button.CursorProperty, Cursors.Hand));
            var trigger = new Trigger { Property = IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555"))));
            buttonStyle.Triggers.Add(trigger);

            this.Resources.Add("ControlButton", buttonStyle);

            string timerXaml = @"
            <Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width='*' />
                    <ColumnDefinition Width='Auto' />
                </Grid.ColumnDefinitions>
                <Border Grid.Column='0' Name='TimerBackground' Background='#DD222222' CornerRadius='8'>
                    <TextBlock Name='TimerText' Text='40:00'
                               FontSize='24' FontFamily='Consolas' FontWeight='Bold'
                               Foreground='White'
                               HorizontalAlignment='Center' VerticalAlignment='Center'/>
                </Border>
                <Border Grid.Column='1' Name='ControlsPanel' Background='#DD333333' CornerRadius='0,8,8,0' Width='0'>
                   <StackPanel Orientation='Horizontal' VerticalAlignment='Center'>
                        <Button Name='PausePlayButton' Content='⏸'/>
                        <Button Name='RefreshButton' Content='🔄'/>
                        <Button Name='CloseButton' Content='❌'/>
                   </StackPanel>
                </Border>
                <Grid.Triggers>
                    <EventTrigger RoutedEvent='MouseEnter'>
                        <BeginStoryboard>
                            <Storyboard>
                                <DoubleAnimation Storyboard.TargetName='ControlsPanel' Storyboard.TargetProperty='Width' To='100' Duration='0:0:0.2' />
                            </Storyboard>
                        </BeginStoryboard>
                    </EventTrigger>
                    <EventTrigger RoutedEvent='MouseLeave'>
                        <BeginStoryboard>
                            <Storyboard>
                                <DoubleAnimation Storyboard.TargetName='ControlsPanel' Storyboard.TargetProperty='Width' To='0' Duration='0:0:0.3' />
                            </Storyboard>
                        </BeginStoryboard>
                    </EventTrigger>
                </Grid.Triggers>
            </Grid>";

            var parserContext = new ParserContext();
            parserContext.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            parserContext.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");

            var rootElement = (FrameworkElement)XamlReader.Parse(timerXaml, parserContext);
            this.Content = rootElement;

            timerText = (TextBlock)rootElement.FindName("TimerText");
            pausePlayButton = (Button)rootElement.FindName("PausePlayButton");
            var refreshButton = (Button)rootElement.FindName("RefreshButton");
            var closeButton = (Button)rootElement.FindName("CloseButton");

            timerBackground = (Border)rootElement.FindName("TimerBackground");
            originalTimerBackground = timerBackground.Background;

            var loadedStyle = (Style)this.Resources["ControlButton"];
            pausePlayButton.Style = loadedStyle;
            refreshButton.Style = loadedStyle;
            closeButton.Style = loadedStyle;

            this.MouseLeftButtonDown += (s, e) => this.DragMove();
            pausePlayButton.Click += PausePlayButton_Click;
            refreshButton.Click += RefreshButton_Click;
            closeButton.Click += CloseButton_Click;

            // Main timer for counting down/up
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            // FIX: Robust "Always on Top" timer
            topmostTimer = new DispatcherTimer();
            topmostTimer.Interval = TimeSpan.FromSeconds(2);
            topmostTimer.Tick += (s, e) => { this.Topmost = true; };

            UpdateTimerText();
            timer.Start();
            topmostTimer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Always tick time down, even into negatives
            timeRemaining = timeRemaining.Add(TimeSpan.FromSeconds(-1));

            // Check if we just passed zero for the first time
            if (!hasFinished && timeRemaining < TimeSpan.Zero)
            {
                hasFinished = true; // Set flag so this only runs once
                timerBackground.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC990000")); // Dark Red
                pausePlayButton.IsEnabled = false;

                // Show the notification window
                if (notification == null || !notification.IsVisible)
                {
                    notification = new NotificationWindow("Timer Finished!");
                    notification.Show();
                }
            }

            // Check for overtime warning (50% of original time)
            TimeSpan overtimeThreshold = TimeSpan.FromSeconds(totalTime.TotalSeconds * -0.5);
            if (hasFinished && timeRemaining <= overtimeThreshold)
            {
                timerBackground.BorderBrush = Brushes.Orange;
                timerBackground.BorderThickness = new Thickness(2);
            }

            UpdateTimerText();
        }

        private void UpdateTimerText()
        {
            // Handle negative time display
            string sign = timeRemaining.Ticks < 0 ? "-" : "";
            timerText.Text = $"{sign}{timeRemaining.Duration():mm\\:ss}";
        }

        private void PausePlayButton_Click(object sender, RoutedEventArgs e)
        {
            isPaused = !isPaused;
            if (isPaused)
            {
                timer.Stop();
                pausePlayButton.Content = "▶";
            }
            else
            {
                timer.Start();
                pausePlayButton.Content = "⏸";
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (notification != null) notification.Close(); // Close notification on restart

            // Reset visual state
            timerBackground.Background = originalTimerBackground;
            timerBackground.BorderBrush = null;
            timerBackground.BorderThickness = new Thickness(0);
            pausePlayButton.IsEnabled = true;
            hasFinished = false;

            // Reset timer logic
            timeRemaining = totalTime;
            isPaused = false;
            pausePlayButton.Content = "⏸";
            UpdateTimerText();
            timer.Start();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Stop all timers before closing
            timer.Stop();
            topmostTimer.Stop();
            if (notification != null) notification.Close();
            setupWindow.Show();
            this.Close();
        }
    }


    // =============================================================================================
    // NOTIFICATION WINDOW
    // =============================================================================================
    public class NotificationWindow : Window
    {
        public NotificationWindow(string message)
        {
            this.Width = 250;
            this.Height = 80;
            this.ShowInTaskbar = false;
            this.Topmost = true;
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.ResizeMode = ResizeMode.NoResize;

            var screenWidth = SystemParameters.WorkArea.Width;
            var screenHeight = SystemParameters.WorkArea.Height;
            this.Left = screenWidth - this.Width - 10;
            this.Top = screenHeight - this.Height - 10;

            string notifyXaml = @"
            <Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                <Border Background='#DD333333' CornerRadius='8'>
                    <Grid>
                        <Button Name='NotifyCloseButton' Content='X' HorizontalAlignment='Right' VerticalAlignment='Top' Margin='5' Width='20' Height='20' Foreground='White' Background='Transparent' BorderThickness='0' Cursor='Hand' FontSize='10'/>
                        <TextBlock Name='MessageText' HorizontalAlignment='Center' VerticalAlignment='Center' Foreground='White' FontSize='16' />
                    </Grid>
                </Border>
            </Grid>";

            var rootElement = (FrameworkElement)XamlReader.Parse(notifyXaml);
            this.Content = rootElement;

            var messageText = (TextBlock)rootElement.FindName("MessageText");
            var closeButton = (Button)rootElement.FindName("NotifyCloseButton");

            messageText.Text = message;
            closeButton.Click += (s, e) => this.Close();

            // Fade in animation
            this.Opacity = 0;
            var fadeInAnimation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.5));
            this.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
        }
    }
}

