using System;
using System.Collections.Generic;
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
            get { return _repeatCount; }
            set
            {
                _repeatCount = value;
                OnPropertyChanged();
            }
        }

        public int DelayAfterClick
        {
            get { return _delayAfterClick; }
            set
            {
                _delayAfterClick = value;
                OnPropertyChanged();
            }
        }

        public bool IsCancelling
        {
            get { return _isCancelling; }
            set
            {
                _isCancelling = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RunButtonText));
            }
        }

        public int Countdown
        {
            get { return _countdown; }
            set
            {
                _countdown = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RunButtonText));
            }
        }

        public int DelayAfterStory
        {
            get { return _delayAfterStory; }
            set
            {
                _delayAfterStory = value;
                OnPropertyChanged();
            }
        }

        public int ClickDuration
        {
            get { return _clickDuration; }
            set
            {
                _clickDuration = value;
                OnPropertyChanged();
            }
        }

        public double Speed
        {
            get { return _speed; }
            set
            {
                _speed = value;
                OnPropertyChanged();
            }
        }

        public bool IsClicking
        {
            get { return _isClicking; }
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
            get
            {
                return _runCommand ?? (_runCommand = new DelegateCommand(Run, () => Points.Count > 0 || IsClicking));
            }
        }

        public CancellationTokenSource CancellationTokenSource { get; set; }
        public Action MinimizeAction { get; set; }
        public Action RestoreAction { get; set; }
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
            var dlg = new PointsCaptureDialog();
            MinimizeAction?.Invoke();

            dlg.ShowDialog();

            RestoreAction?.Invoke();

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

        private void Run()
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

                var copyOfPoints = new List<ClickPoint>(Points.Count);
                copyOfPoints.AddRange(Points.Select(clickPoint => (ClickPoint)clickPoint.Clone()));
                var dispatcher = Dispatcher.CurrentDispatcher;
                var ct = CancellationTokenSource.Token;

                var delayAfterClick = DelayAfterClick;
                var delayAfterStory = DelayAfterStory;
                var maxClicks = RepeatCount;
                var speed = Speed;
                var clickDuration = ClickDuration;

                Task.Run(() =>
                {
                    dispatcher.Invoke(() => IsClicking = true);

                    var countdown = 3;
                    while(!ct.IsCancellationRequested && countdown > 0)
                    {
                        // ReSharper disable once AccessToModifiedClosure
                        dispatcher.Invoke(() => Countdown = countdown);
                        --countdown;
                        Thread.Sleep(1000);
                    }

                    dispatcher.Invoke(() => Countdown = 0);

                    try
                    {
                        var clicks = 0;
                        while(!ct.IsCancellationRequested && (clicks < maxClicks || maxClicks == 0))
                        {
                            foreach(var point in copyOfPoints)
                            {
                                if(ct.IsCancellationRequested)
                                {
                                    break;
                                }

                                ClickUtils.Click(point.X, point.Y, (ClickMode)point.SelectedClickMode, speed,
                                    clickDuration);

                                if(!ct.IsCancellationRequested && DelayAfterClick > 0)
                                {
                                    Thread.Sleep(delayAfterClick);
                                }
                            }

                            if(!ct.IsCancellationRequested && DelayAfterStory > 0)
                            {
                                Thread.Sleep(delayAfterStory);
                            }
                            ++clicks;
                        }
                    }
                    finally
                    {
                        dispatcher.Invoke(() => IsClicking = false);
                        dispatcher.Invoke(() => IsCancelling = false);
                        dispatcher.Invoke(() => Countdown = 0);

                        if(!ct.IsCancellationRequested)
                        {
                            CancellationTokenSource = null;
                        }
                    }
                }, ct);
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}