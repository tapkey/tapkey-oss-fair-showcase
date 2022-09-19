using MudBlazor;

namespace Tapkey.FairOssShowcase.WebApp.Theme
{
    public class TapkeyTheme : MudTheme
    {
        public TapkeyTheme()
        {
            Palette = new Palette()
            {
                Primary = "#00a3dd",
                Secondary = "#fca311"
            };

            LayoutProperties = new LayoutProperties()
            {
                DefaultBorderRadius = "3px"
            };

            Typography = new Typography()
            {
                Default = new Default()
                {
                    FontFamily = new[] { "Nunito Sans", "sans-serif" },
                    FontSize = "16px",
                    LetterSpacing = ".15px",
                    FontWeight = 600
                },
                Button = new Button()
                {
                    FontFamily = new[] { "Nunito Sans", "sans-serif" },
                    FontSize = "14px",
                    LetterSpacing = ".3px",
                    FontWeight = 700
                },
                Body1 = new Body1()
                {
                    FontFamily = new[] { "Nunito Sans", "sans-serif" },
                    FontSize = "16px",
                    LetterSpacing = ".15px",
                    FontWeight = 600
                }
            };
            Shadows = new Shadow();
            ZIndex = new ZIndex();
        }
    }
}
