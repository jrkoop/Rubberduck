﻿using System;
using System.Windows.Forms;
using Rubberduck.VBEditor.WindowsApi;

namespace Rubberduck.VBEditor.Events
{
    public class KeyPressEventArgs
    {
        public KeyPressEventArgs(IntPtr hwnd, IntPtr wParam, IntPtr lParam, bool keydown = false)
        {
            Hwnd = hwnd;
            WParam = wParam;
            LParam = lParam;

            if (keydown)
            {
                var key = (Keys)wParam;
                ControlDown = key.HasFlag(Keys.Control);
                // Why \r and not \n? Because it really doesn't matter...
                Character = key.HasFlag(Keys.Enter) ? '\r' : default;
            }
            else
            {
                ControlDown = (User32.GetKeyState(VirtualKeyStates.VK_CONTROL) & 0x8000) != 0;
                Character = (char)wParam;
            }
        }

        //public bool IsCharacter { get; }
        public IntPtr Hwnd { get; }
        public IntPtr WParam { get; }
        public IntPtr LParam { get; }

        public bool Handled { get; set; }
        public bool IsDelete => (Keys)WParam == Keys.Delete;
        public char Character { get; }
        public bool ControlDown { get; }
    }
}
