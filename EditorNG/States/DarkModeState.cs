using System;

namespace EditorNG.States
{
    public class DarkModeState : IDarkModeState
    {
        private bool enabled = false;

        public bool Enabled
        {
            get
            {
                StateHasChanged();
                return enabled;
            }
            set
            {
                StateHasChanged();
                enabled = value;
            }
        }


        public event EventHandler StateChanged;

        private void StateHasChanged()
        {
            // This will update any subscribers
            // that the counter state has changed
            // so they can update themselves
            // and show the current counter value
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
