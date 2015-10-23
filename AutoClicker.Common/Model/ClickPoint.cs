using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
            get { return _x; }
            set
            {
                _x = value;
                OnPropertyChanged();
            }
        }

        public int Y
        {
            get { return _y; }
            set
            {
                _y = value;
                OnPropertyChanged();
            }
        }

        public int SelectedClickMode
        {
            get { return _selectedClickMode; }
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
    }
}