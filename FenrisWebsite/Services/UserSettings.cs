namespace FenrisWebsite.Services
{
    public class UserSettings
    {
        private bool _isDarkMode = true;
        public string BackgroundColorDark { get; set; } = "#161a1d";
        public string BackgroundColorLight { get; set; } = "#eaeaea";

        public string CurrentBackgroundColor => _isDarkMode ? BackgroundColorDark : BackgroundColorLight;


        public event EventHandler OnDarkModeChanged;

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    OnDarkModeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
