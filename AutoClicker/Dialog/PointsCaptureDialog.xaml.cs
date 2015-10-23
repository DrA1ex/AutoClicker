using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using AutoClicker.Common.Model;
using AutoClicker.Common.Utils;
using Gma.UserActivityMonitor;
using ClickMode = AutoClicker.Common.Model.ClickMode;
using HookMouseEventArgs = System.Windows.Forms.MouseEventArgs;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace AutoClicker.Dialog
{
    public partial class PointsCaptureDialog
    {
        private const double CircleSize = 30;
        private const double BorderSize = 2;
        private const double MinDistance = 10; //px
        private const double DefaultOpacity = 0.5;

        private static readonly SolidColorBrush StrokeBrush = Brushes.OrangeRed;
        private static readonly SolidColorBrush BackgroundBrush = Brushes.DarkOrange;

        private Line _currentTraectoryLine;

        private ICommand _exitCommand;
        private List<ClickPoint> _points;

        public PointsCaptureDialog(bool allowInteractions = true)
        {
            InitializeComponent();

            AllowInteractions = allowInteractions;
            DataContext = this;

            Topmost = true;

            Left = 0;
            Top = 0;

            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;

            if(AllowInteractions)
            {
                HookManager.MouseUp += HookManagerOnMouseUp;
                HookManager.MouseMove += HookManagerOnMouseMove;
                HookManager.MouseDown += HookManagerOnMouseDown;
            }
            else
            {
                MouseDown += OnMouseDown;
                MouseMove += OnMouseMove;
                MouseUp += OnMouseUp;
            }
        }

        private bool AllowInteractions { get; }

        private ClickPoint StartPoint { get; set; }

        private Line CurrentTraectoryLine
        {
            get
            {
                return _currentTraectoryLine ?? (_currentTraectoryLine = new Line
                {
                    Stroke = StrokeBrush,
                    StrokeThickness = BorderSize,
                    StrokeDashArray = new DoubleCollection(new[] { 2.0, 1.0 }),
                    Opacity = DefaultOpacity
                });
            }
            set { _currentTraectoryLine = value; }
        }

        private List<ClickPoint> Points => _points ?? (_points = new List<ClickPoint>());

        public ICommand ExitCommand => _exitCommand ?? (_exitCommand = new DelegateCommand(Exit));

        private void HookManagerOnMouseDown(object sender, HookMouseEventArgs e)
        {
            if(e.Button.HasFlag(MouseButtons.Left))
            {
                Dispatcher.InvokeAsync(() => HandleMouseDown(e.X, e.Y));
            }
        }

        private void HookManagerOnMouseMove(object sender, HookMouseEventArgs e)
        {
            if(StartPoint != null)
            {
                Dispatcher.InvokeAsync(() => HandleMouseMove(e.X, e.Y));
            }
        }

        private void HookManagerOnMouseUp(object sender, HookMouseEventArgs e)
        {
            if(e.Button.HasFlag(MouseButtons.Left))
            {
                Dispatcher.InvokeAsync(() => HandleMouseUp(e.X, e.Y));
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left)
            {
                var pos = e.GetPosition(this);
                HandleMouseDown((int)pos.X, (int)pos.Y);
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if(StartPoint != null)
            {
                var pos = e.GetPosition(this);
                HandleMouseMove((int)pos.X, (int)pos.Y);
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left)
            {
                var pos = e.GetPosition(this);
                HandleMouseUp((int)pos.X, (int)pos.Y);
            }
        }

        private void HandleMouseDown(int x, int y)
        {
            StartPoint = new ClickPoint
            {
                X = x,
                Y = y,
                SelectedClickMode = (int)ClickMode.Push
            };

            CreatePoint(StartPoint);
        }

        private void HandleMouseMove(int x, int y)
        {
            DrawTraectory(StartPoint, x, y);
        }

        private void HandleMouseUp(int x, int y)
        {
            var point = new ClickPoint
            {
                X = x,
                Y = y
            };

            var isMoved = point.Distance(StartPoint) > MinDistance;
            if(isMoved)
            {
                point.SelectedClickMode = (int)ClickMode.Release;
                CreatePoint(point);
                DrawTraectory(StartPoint, x, y, true);
                CurrentTraectoryLine = null;
            }
            else
            {
                StartPoint.SelectedClickMode = (int)ClickMode.PushAndRelease;
                Canvas.Children.Remove(CurrentTraectoryLine);
            }

            StartPoint = null;
        }

        private void CreatePoint(ClickPoint point)
        {
            Points.Add(point);
            AddCircleAtPosition(Points.Count, point);
        }

        public ClickPoint[] GetPoints()
        {
            return Points.ToArray();
        }

        private void AddCircleAtPosition(int index, ClickPoint p)
        {
            var border = new Border
            {
                BorderBrush = StrokeBrush,
                Background = BackgroundBrush,
                Width = CircleSize,
                Height = CircleSize,
                BorderThickness = new Thickness(BorderSize),
                CornerRadius = new CornerRadius(360),
                Opacity = DefaultOpacity,
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

        private void DrawTraectory(ClickPoint from, int toX, int toY, bool adjustTrarget = false)
        {
            if(!Canvas.Children.Contains(CurrentTraectoryLine))
            {
                Canvas.Children.Add(CurrentTraectoryLine);
            }

            var angle = from.Angle(toX, toY);
            var fullSize = CircleSize - BorderSize;

            var xOffset = fullSize / 2 * Math.Sin(angle);
            var yOffset = fullSize / 2 * Math.Cos(angle);

            CurrentTraectoryLine.X1 = from.X + xOffset;
            CurrentTraectoryLine.Y1 = from.Y + yOffset;
            CurrentTraectoryLine.X2 = toX - (adjustTrarget ? xOffset : 0);
            CurrentTraectoryLine.Y2 = toY - (adjustTrarget ? yOffset : 0);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if(AllowInteractions)
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                WindowsServices.SetWindowExTransparent(hwnd);
            }
        }

        private void Exit()
        {
            if(AllowInteractions)
            {
                HookManager.MouseUp -= HookManagerOnMouseDown;
                HookManager.MouseMove -= HookManagerOnMouseMove;
                HookManager.MouseDown -= HookManagerOnMouseUp;
            }
            else
            {
                MouseUp -= OnMouseUp;
                MouseMove -= OnMouseMove;
                MouseDown -= OnMouseDown;
            }

            Close();
        }
    }
}