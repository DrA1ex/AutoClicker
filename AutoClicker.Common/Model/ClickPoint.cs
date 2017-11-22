using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static System.Math;

namespace AutoClicker.Common.Model
{
    public enum ClickMode
    {
        PushAndRelease,
        Push,
        Release,
        Move
    }

    public class ClickPoint : INotifyPropertyChanged, ICloneable
    {
        private int _selectedClickMode;
        private int _x;
        private int _y;

        public ClickPoint()
        {
            SelectedClickMode = (int)ClickMode.PushAndRelease;
        }

        public int X
        {
            get => _x;
            set
            {
                _x = value;
                OnPropertyChanged();
            }
        }

        public int Y
        {
            get => _y;
            set
            {
                _y = value;
                OnPropertyChanged();
            }
        }

        public int SelectedClickMode
        {
            get => _selectedClickMode;
            set
            {
                _selectedClickMode = value;
                OnPropertyChanged();
            }
        }

        public object Clone()
        {
            return new ClickPoint
            {
                X = X,
                Y = Y,
                SelectedClickMode = SelectedClickMode
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public double Distance(ClickPoint toPoint)
        {
            return Sqrt(Pow(X - toPoint.X, 2) + Pow(Y - toPoint.Y, 2));
        }

        public double Angle(int x, int y)
        {
            double deltaX = x - X;
            double deltaY = y - Y;

            return Atan2(deltaX, deltaY);
        }
    }
}