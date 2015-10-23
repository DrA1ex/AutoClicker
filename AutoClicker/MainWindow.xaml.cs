using System;
using System.Collections.Generic;
using AutoClicker.Common.Model;
using AutoClicker.Properties;
using AutoClicker.ViewModel;

namespace AutoClicker
{
    public partial class MainWindow
    {
        private MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            ViewModel.MinimizeAction = Hide;
            ViewModel.RestoreAction = () =>
            {
                Show();
                Activate();
            };

            DataContext = ViewModel;

            var points = Settings.Default.Points;
            if(points != null)
            {
                foreach(var point in points)
                {
                    ViewModel.Points.Add(point);
                }
            }

            ViewModel.DelayAfterClick = Settings.Default.DelayAfterClick;
            ViewModel.DelayAfterStory = Settings.Default.DelayAfterStory;
            ViewModel.RepeatCount = Settings.Default.RepeatCount;
            ViewModel.Speed = Settings.Default.Speed;
            ViewModel.ClickDuration = Settings.Default.ClickDuration;
            ViewModel.AllowInteractions = Settings.Default.AllowInteractions;
        }

        public MainWindowViewModel ViewModel => _viewModel ?? (_viewModel = new MainWindowViewModel());

        private void OnClosed(object sender, EventArgs e)
        {
            Settings.Default.Points = new List<ClickPoint>(ViewModel.Points);
            Settings.Default.DelayAfterClick = ViewModel.DelayAfterClick;
            Settings.Default.DelayAfterStory = ViewModel.DelayAfterStory;
            Settings.Default.RepeatCount = ViewModel.RepeatCount;
            Settings.Default.Speed = ViewModel.Speed;
            Settings.Default.ClickDuration = ViewModel.ClickDuration;
            Settings.Default.AllowInteractions = ViewModel.AllowInteractions;

            Settings.Default.Save();
        }
    }
}