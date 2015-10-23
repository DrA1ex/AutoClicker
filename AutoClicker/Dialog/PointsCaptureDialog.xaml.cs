using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using AutoClicker.Common.Model;
using AutoClicker.Common.Utils;
using Gma.UserActivityMonitor;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace AutoClicker.Dialog
{
    public partial class PointsCaptureDialog
    {
        private ICommand _exitCommand;
        private List<ClickPoint> _points;

        public PointsCaptureDialog()
        {
            InitializeComponent();

            DataContext = this;

            Left = 0;
            Top = 0;

            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
#if !DEBUG
            Topmost = true;
#endif

            HookManager.MouseUp += HookManagerOnMouseClick;
        }

        private List<ClickPoint> Points => _points ?? (_points = new List<ClickPoint>());
        public ICommand ExitCommand => _exitCommand ?? (_exitCommand = new DelegateCommand(Exit));

        private void HookManagerOnMouseClick(object sender, MouseEventArgs e)
        {
            if(e.Button.HasFlag(MouseButtons.Left))
            {
                var point = new ClickPoint {X = e.X, Y = e.Y};
                Dispatcher.InvokeAsync(() =>
                {
                    Points.Add(point);
                    AddCircleAtPosition(Points.Count, point);
                });
            }
        }

        public ClickPoint[] GetPoints()
        {
            return Points.ToArray();
        }

        private void AddCircleAtPosition(int index, ClickPoint p)
        {
            var border = new Border
            {
                BorderBrush = Brushes.OrangeRed,
                Background = Brushes.DarkOrange,
                Width = 30,
                Height = 30,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(360),
                Opacity = 0.5,
                Child = new Viewbox
                {
                    Child = new TextBlock
                    {
                        Text = index.ToString(),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Padding = new Thickness(3)
                    }
                }
            };

            Canvas.Children.Add(border);

            Canvas.SetLeft(border, p.X - border.Width / 2);
            Canvas.SetTop(border, p.Y - border.Height / 2);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowsServices.SetWindowExTransparent(hwnd);
        }

        private void Exit()
        {
            HookManager.MouseUp -= HookManagerOnMouseClick;
            Close();
        }
    }
}