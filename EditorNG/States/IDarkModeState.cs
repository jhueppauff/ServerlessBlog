using System;

namespace EditorNG.States
{
    public interface IDarkModeState
    {
        bool Enabled { get; set; }

        event EventHandler StateChanged;
    }
}