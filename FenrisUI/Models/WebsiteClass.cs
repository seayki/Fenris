using Fenris;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
