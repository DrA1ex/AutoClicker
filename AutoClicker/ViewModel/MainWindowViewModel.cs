using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using AutoClicker.Annotations;
using AutoClicker.Common.Model;
using AutoClicker.Common.Utils;
using AutoClicker.Dialog;

namespace AutoClicker.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private ICommand _addNewPointCommand;
        private bool _allowInteractions = true;
        private int _clickDuration;
        private int _countdown;
        private int _delayAfterClick;
        private int _delayAfterStory;
        private bool _isCancelling;
        private bool _isClicking;
        private ObservableCollection<ClickPoint> _points;
        private ICommand _recordPointsCommand;
        private ICommand _removePointCommand;
        private int _repeatCount;
        private ICommand _runCommand;
        private double _speed = 0.6;

        public MainWindowViewModel()
        {
            Points.CollectionChanged += (sender, args) => ((DelegateCommand)RunCommand).RaiseCanExecuteChanged();
        }

        public ObservableCollection<ClickPoint> Points => _points ?? (_points = new ObservableCollection<ClickPoint>());

        public int RepeatCount
        {
            get => _repeatCount;
            set
            {
                _repeatCount = value;
                OnPropertyChanged();
            }
        }

        public int DelayAfterClick
        {
            get => _delayAfterClick;
            set
            {
                _delayAfterClick = value;
                OnPropertyChanged();
            }
        }

        public bool IsCancelling
        {
            get => _isCancelling;
            set
            {
                _isCancelling = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RunButtonText));
            }
        }

        public int Countdown
        {
            get => _countdown;
            set
            {
                _countdown = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RunButtonText));
            }
        }

        public int DelayAfterStory
        {
            get => _delayAfterStory;
            set
            {
                _delayAfterStory = value;
                OnPropertyChanged();
            }
        }

        public int ClickDuration
        {
            get => _clickDuration;
            set
            {
                _clickDuration = value;
                OnPropertyChanged();
            }
        }

        public double Speed
        {
            get => _speed;
            set
            {
                _speed = value;
                OnPropertyChanged();
            }
        }

        public bool AllowInteractions
        {
            get => _allowInteractions;
            set
            {
                _allowInteractions = value;
                OnPropertyChanged(nameof(AllowInteractions));
            }
        }

        public bool IsClicking
        {
            get => _isClicking;
            set
            {
                _isClicking = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RunButtonText));
            }
        }

        public string RunButtonText
        {
            get
            {
                if(!IsCancelling)
                {
                    if(Countdown > 0)
                    {
                        return $"Run in {Countdown}...";
                    }
                    return IsClicking ? "Cancel" : "Run";
                }

                return "Canceling...";
            }
        }

        public ICommand AddNewPointCommand
            => _addNewPointCommand ?? (_addNewPointCommand = new DelegateCommand(AddNewPoint));

        public ICommand RemovePointCommand
            => _removePointCommand ?? (_removePointCommand = new DelegateCommand<ClickPoint>(RemovePoint));

        public ICommand RecordPointsCommand
            => _recordPointsCommand ?? (_recordPointsCommand = new DelegateCommand(RecordPoints));

        public ICommand RunCommand
        {
            get { return _runCommand ?? (_runCommand = new DelegateCommand(Run, () => Points.Count > 0 || IsClicking)); }
        }

        public CancellationTokenSource CancellationTokenSource { get; set; }

        public Action MinimizeAction { get; set; }

        public Action SequenceFinishedAction { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void AddNewPoint()
        {
            Points.Add(new ClickPoint());
        }

        private void RemovePoint(ClickPoint point)
        {
            Points.Remove(point);
        }

        private void RecordPoints()
        {
            var dlg = new PointsCaptureDialog(AllowInteractions);
            MinimizeAction?.Invoke();

            dlg.ShowDialog();

            SequenceFinishedAction?.Invoke();

            var points = dlg.GetPoints();
            if(points != null && points.Length > 0)
            {
                Points.Clear();
                foreach(var point in points)
                {
                    Points.Add(point);
                }
            }
        }

        private async void Run()
        {
            if(IsCancelling)
            {
                return;
            }

            if(CancellationTokenSource != null)
            {
                CancellationTokenSource.Cancel();
                CancellationTokenSource = null;
                IsCancelling = true;
            }
            else if(!IsCancelling && !IsClicking)
            {
                CancellationTokenSource = new CancellationTokenSource();

                var copyOfPoints = Points.Select(clickPoint => (ClickPoint)clickPoint.Clone()).ToArray();
                var dispatcher = Dispatcher.CurrentDispatcher;
                var ct = CancellationTokenSource.Token;

                var delayAfterClick = DelayAfterClick;
                var delayAfterStory = DelayAfterStory;
                var repeatCount = RepeatCount > 0 ? RepeatCount : int.MaxValue;
                var speed = Speed;
                var clickDuration = ClickDuration;

                IsClicking = true;

                try
                {
                    for(var countdown = 3; countdown > 0; --countdown)
                    {
                        // ReSharper disable once AccessToModifiedClosure
                        await dispatcher.InvokeAsync(() => Countdown = countdown, DispatcherPriority.Render, ct);
                        await Task.Delay(1000, ct);
                    }

                    await dispatcher.InvokeAsync(() => Countdown = 0, DispatcherPriority.Render, ct);

                    for(var clicks = 0; !ct.IsCancellationRequested && clicks < repeatCount; ++clicks)
                    {
                        for(var index = 0; !ct.IsCancellationRequested && index < copyOfPoints.Length; ++index)
                        {
                            var point = copyOfPoints[index];

                            ClickUtils.Click(point.X, point.Y, (ClickMode)point.SelectedClickMode, speed,
                                clickDuration);

                            if(DelayAfterClick > 0)
                            {
                                await Task.Delay(delayAfterClick, ct);
                            }
                        }

                        if(DelayAfterStory > 0)
                        {
                            await Task.Delay(delayAfterStory, ct);
                        }
                    }
                }
                catch(OperationCanceledException)
                {
                    //ignore
                }
                finally
                {
                    await dispatcher.InvokeAsync(Reset);

                    if(!ct.IsCancellationRequested)
                    {
                        await dispatcher.InvokeAsync(() => SequenceFinishedAction?.Invoke());
                        CancellationTokenSource = null;
                    }
                }
            }
        }

        private void Reset()
        {
            IsClicking = false;
            IsCancelling = false;
            Countdown = 0;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}