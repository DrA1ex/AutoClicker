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
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace AutoClicker.Dialog
{
    internal enum CaptureMode
    {
        None,
        Mouse,
        Keyboard
    }

    public partial class PointsCaptureDialog
    {
        private const double CircleSize = 30;
        private const double BorderSize = 2;
        private const double MinDistance = 10; //px
        private const double DefaultOpacity = 0.5;

        private static readonly SolidColorBrush ClickStrokeBrush = Brushes.OrangeRed;
        private static readonly SolidColorBrush ClickBackgroundBrush = Brushes.DarkOrange;

        private static readonly SolidColorBrush MoveStrokeBrush = Brushes.DarkBlue;
        private static readonly SolidColorBrush MoveBackgroundBrush = Brushes.CornflowerBlue;

        private Line _currentTraectoryLine;

        private ICommand _exitCommand;

        private Point _lastMousePoint;
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

            var dpi = VisualTreeHelper.GetDpi(this);
            Canvas.RenderTransform = new ScaleTransform(1 / dpi.DpiScaleX, 1 / dpi.DpiScaleY);

            if(AllowInteractions)
            {
                HookManager.MouseUp += HookManagerOnMouseUp;
                HookManager.MouseMove += HookManagerOnMouseMove;
                HookManager.MouseDown += HookManagerOnMouseDown;
                HookManager.KeyDown += HookManagerOnKeyDown;
                HookManager.KeyUp += HookManagerOnKeyUp;
            }
            else
            {
                MouseDown += OnMouseDown;
                MouseMove += OnMouseMove;
                MouseUp += OnMouseUp;
                KeyDown += OnKeyDown;
                KeyUp += OnKeyUp;
            }
        }

        private CaptureMode CurrentMode { get; set; } = CaptureMode.None;

        private bool AllowInteractions { get; }

        private ClickPoint StartPoint { get; set; }

        private Line CurrentTraectoryLine
        {
            get => _currentTraectoryLine ?? (_currentTraectoryLine = new Line
            {
                Stroke = CurrentMode  == CaptureMode.Mouse ? ClickStrokeBrush : MoveStrokeBrush,
                StrokeThickness = BorderSize,
                StrokeDashArray = new DoubleCollection(new[] {2.0, 1.0}),
                Opacity = DefaultOpacity
            });
            set => _currentTraectoryLine = value;
        }

        private List<ClickPoint> Points => _points ?? (_points = new List<ClickPoint>());

        public ICommand ExitCommand => _exitCommand ?? (_exitCommand = new DelegateCommand(Exit));

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
                HookManager.KeyDown -= HookManagerOnKeyDown;
                HookManager.KeyUp -= HookManagerOnKeyUp;
            }
            else
            {
                MouseUp -= OnMouseUp;
                MouseMove -= OnMouseMove;
                MouseDown -= OnMouseDown;
                KeyDown -= OnKeyDown;
                KeyUp -= OnKeyUp;
            }

            Close();
        }


        #region Keyboard event handlers

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                Dispatcher.InvokeAsync(() => HandleStopRecording((int)_lastMousePoint.X, (int)_lastMousePoint.Y));
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if(CurrentMode == CaptureMode.None && (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl))
            {
                Dispatcher.InvokeAsync(() => HandleStartRecording((int)_lastMousePoint.X, (int)_lastMousePoint.Y, CaptureMode.Keyboard));
            }
        }

        private void HookManagerOnKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if(CurrentMode == CaptureMode.None && (e.KeyData & (Keys.LControlKey | Keys.RControlKey)) != 0)
            {
                Dispatcher.InvokeAsync(() => HandleStartRecording((int)_lastMousePoint.X, (int)_lastMousePoint.Y, CaptureMode.Keyboard));
            }
        }

        private void HookManagerOnKeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if(CurrentMode == CaptureMode.Keyboard && (e.KeyData & (Keys.LControlKey | Keys.RControlKey)) != 0)
            {
                Dispatcher.InvokeAsync(() => HandleStopRecording((int)_lastMousePoint.X, (int)_lastMousePoint.Y));
            }
        }

        #endregion

        #region Mouse event handlers

        private void HookManagerOnMouseDown(object sender, HookMouseEventArgs e)
        {
            if(CurrentMode == CaptureMode.None && e.Button.HasFlag(MouseButtons.Left))
            {
                Dispatcher.InvokeAsync(() => HandleStartRecording(e.X, e.Y, CaptureMode.Mouse));
            }
        }

        private void HookManagerOnMouseMove(object sender, HookMouseEventArgs e)
        {
            _lastMousePoint.X = e.X;
            _lastMousePoint.Y = e.Y;

            if(StartPoint != null)
            {
                Dispatcher.InvokeAsync(() => HandleTargetMove(e.X, e.Y));
            }
        }

        private void HookManagerOnMouseUp(object sender, HookMouseEventArgs e)
        {
            if(CurrentMode == CaptureMode.Mouse && e.Button.HasFlag(MouseButtons.Left))
            {
                Dispatcher.InvokeAsync(() => HandleStopRecording(e.X, e.Y));
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left)
            {
                var pos = e.GetPosition(this);
                HandleStartRecording((int)pos.X, (int)pos.Y, CaptureMode.Mouse);
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if(StartPoint != null)
            {
                var pos = e.GetPosition(this);
                HandleTargetMove((int)pos.X, (int)pos.Y);
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left)
            {
                var pos = e.GetPosition(this);
                HandleStopRecording((int)pos.X, (int)pos.Y);
            }
        }

        #endregion

        #region Tracking

        private void HandleStartRecording(int x, int y, CaptureMode mode)
        {
            CurrentMode = mode;

            StartPoint = new ClickPoint
            {
                X = x,
                Y = y,
                SelectedClickMode = (int)(CurrentMode == CaptureMode.Mouse ? ClickMode.Push : ClickMode.Move)
            };

            CreatePoint(StartPoint);
        }

        private void HandleTargetMove(int x, int y)
        {
            DrawTraectory(StartPoint, x, y);
        }

        private void HandleStopRecording(int x, int y)
        {
            var point = new ClickPoint
            {
                X = x,
                Y = y
            };

            var isMoved = point.Distance(StartPoint) > MinDistance;
            if(isMoved)
            {
                point.SelectedClickMode = (int)(CurrentMode == CaptureMode.Mouse ? ClickMode.Release : ClickMode.Move);
                CreatePoint(point);
                DrawTraectory(StartPoint, x, y, true);
                CurrentTraectoryLine = null;
            }
            else
            {
                StartPoint.SelectedClickMode = (int)(CurrentMode == CaptureMode.Mouse ? ClickMode.PushAndRelease : ClickMode.Move);
                Canvas.Children.Remove(CurrentTraectoryLine);
            }

            CurrentMode = CaptureMode.None;
            StartPoint = null;
        }

        #endregion

        #region Drawing

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
                BorderBrush = CurrentMode == CaptureMode.Mouse ? ClickStrokeBrush : MoveStrokeBrush,
                Background = CurrentMode == CaptureMode.Mouse ? ClickBackgroundBrush : MoveBackgroundBrush,
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

        #endregion
    }
}