using Microsoft.UI.Xaml.Controls;

namespace FenrisUI.Models
{
    public class WebsiteCategory
    {
        public string Label { get; set; }
        public FontIcon Icon { get; set; }

        public WebsiteCategory(string label, FontIcon icon)
        {
            Label = label;
            Icon = icon;
        }
    }
}
