using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using AutoClicker.Common.Model;

namespace AutoClicker.Common.Utils
{
    public static class ClickUtils
    {
        [DllImport("user32.dll")]
        private static extern void mouse_event(int a, int b, int c, int d, int e);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point pt);

        public static void Click(int x, int y, ClickMode mode, double speed, int clickDuration, double scaleX, double scaleY)
        {
            x = (int)(x * 65535 / SystemParameters.PrimaryScreenWidth);
            y = (int)(y * 65535 / SystemParameters.PrimaryScreenHeight);
            MoveTo(x, y, speed, scaleX, scaleY);

            if(mode == ClickMode.PushAndRelease || mode == ClickMode.Push)
            {
                mouse_event(0x8002, x, y, 0, 0);
                if(clickDuration > 0)
                {
                    Thread.Sleep(clickDuration);
                }
            }
            if(mode == ClickMode.PushAndRelease || mode == ClickMode.Release)
            {
                mouse_event(0x8004, x, y, 0, 0);
            }
        }

        private static void MoveTo(int x, int y, double speed, double scaleX, double scaleY)
        {
            GetCursorPos(out var point);

            var curX = (int)(point.X / scaleX * 65535 / SystemParameters.PrimaryScreenWidth);
            var curY = (int)(point.Y / scaleY * 65535 / SystemParameters.PrimaryScreenHeight);

            var steps = (int)(Math.Max(Math.Abs(x - curX), Math.Abs(y - curY)) / (3000 * speed));
            if(steps == 0)
            {
                steps = 1;
            }

            var dx = (x - curX) / steps;
            var dy = (y - curY) / steps;


            for(var i = 0; i < steps; i++)
            {
                curX += dx;
                curY += dy;
                mouse_event(0x8001, curX, curY, 0, 0);
                Thread.Sleep(10);
            }
            mouse_event(0x8001, x, y, 0, 0);
        }

        internal struct Point
        {
            public int X { get; set; }
            public int Y { get; set; }
        }
    }
}