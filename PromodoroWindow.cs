using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

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
            this.Width = 300;
            this.Height = 180;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.FontFamily = new FontFamily("Segoe UI");
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.ResizeMode = ResizeMode.NoResize;

            string setupXaml = @"
            <Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='#f0f0f0'>
                <StackPanel VerticalAlignment='Center' HorizontalAlignment='Center' Margin='20'>
                    <TextBlock Text='Set Timer Duration' FontSize='18' FontWeight='SemiBold' HorizontalAlignment='Center' Margin='0,0,0,10' Foreground='#333'/>
                    <StackPanel Orientation='Horizontal' HorizontalAlignment='Center'>
                        <TextBox Name='MinutesTextBox' Width='80' FontSize='16' Text='25' Padding='5' VerticalContentAlignment='Center' HorizontalContentAlignment='Center' />
                        <TextBlock Text='minutes' FontSize='16' VerticalAlignment='Center' Margin='10,0,0,0' Foreground='#555'/>
                    </StackPanel>
                    <Button Name='StartButton' Content='Start Timer' FontSize='16' Padding='10,5' Margin='0,20,0,0' Cursor='Hand' Background='#007ACC' Foreground='White' BorderThickness='0'/>
                </StackPanel>
            </Grid>";

            this.Content = (FrameworkElement)XamlReader.Parse(setupXaml);
            this.minutesTextBox = (TextBox)this.FindName("MinutesTextBox");
            Button startButton = (Button)this.FindName("StartButton");

            startButton.Click += StartButton_Click;
            this.minutesTextBox.PreviewTextInput += MinutesTextBox_PreviewTextInput;
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
        private Border controlsPanel;
        private Button pausePlayButton;

        private DispatcherTimer timer;
        private TimeSpan totalTime;
        private TimeSpan timeRemaining;
        private bool isPaused = false;
        private Window setupWindow;

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

            string timerXaml = @"
            <Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width='*' />
                    <ColumnDefinition Width='Auto' />
                </Grid.ColumnDefinitions>
                <Border Grid.Column='0' Name='TimerBackground' Background='#DD222222' CornerRadius='8'>
                    <TextBlock Name='TimerText' Text='25:00'
                               FontSize='24' FontFamily='Consolas' FontWeight='Bold'
                               Foreground='White'
                               HorizontalAlignment='Center' VerticalAlignment='Center'/>
                </Border>
                <Border Grid.Column='1' Name='ControlsPanel' Background='#DD333333' CornerRadius='0,8,8,0' Width='0'>
                   <StackPanel Orientation='Horizontal' VerticalAlignment='Center'>
                        <Button Name='PausePlayButton' Content='⏸' Style='{StaticResource ControlButton}'/>
                        <Button Name='RefreshButton' Content='🔄' Style='{StaticResource ControlButton}'/>
                        <Button Name='CloseButton' Content='❌' Style='{StaticResource ControlButton}'/>
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

            timerText = (TextBlock)rootElement.FindName("TimerText");
            controlsPanel = (Border)rootElement.FindName("ControlsPanel");
            pausePlayButton = (Button)rootElement.FindName("PausePlayButton");
            var refreshButton = (Button)rootElement.FindName("RefreshButton");
            var closeButton = (Button)rootElement.FindName("CloseButton");

            this.MouseLeftButtonDown += (s, e) => this.DragMove();
            pausePlayButton.Click += PausePlayButton_Click;
            refreshButton.Click += RefreshButton_Click;
            closeButton.Click += CloseButton_Click;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            UpdateTimerText();
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (timeRemaining > TimeSpan.Zero)
            {
                timeRemaining = timeRemaining.Add(TimeSpan.FromSeconds(-1));
            }
            else
            {
                timer.Stop();
                System.Media.SystemSounds.Exclamation.Play();
            }
            UpdateTimerText();
        }

        private void UpdateTimerText()
        {
            timerText.Text = $"{timeRemaining:mm\\:ss}";
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
            timeRemaining = totalTime;
            isPaused = false;
            pausePlayButton.Content = "⏸";
            UpdateTimerText();
            timer.Start();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            setupWindow.Show();
            this.Close();
        }
    }
}
