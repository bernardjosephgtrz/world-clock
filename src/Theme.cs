using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace WorldClock
{
    /// <summary>Dark navy / blue / white palette and shared control styles.</summary>
    static class Theme
    {
        public static readonly FontFamily UiFont = new FontFamily("Segoe UI");
        public static readonly FontFamily IconFont = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");

        public static readonly Color BgTop = Hex("#101D38");
        public static readonly Color BgBottom = Hex("#0A1224");
        public static readonly Color WindowBorder = Hex("#22355C");
        public static readonly Color CardTop = Hex("#16284B");
        public static readonly Color CardBottom = Hex("#101C37");
        public static readonly Color CardBorder = Hex("#22355C");
        public static readonly Color HomeBorder = Hex("#3466C2");
        public static readonly Color Accent = Hex("#3B82F6");
        public static readonly Color AccentSoft = Hex("#60A5FA");
        public static readonly Color Text = Hex("#F1F5FF");
        public static readonly Color TextDim = Hex("#9DB0D3");
        public static readonly Color TextFaint = Hex("#6479A3");
        public static readonly Color Pill = Hex("#1A3566");
        public static readonly Color HomePill = Hex("#1E3F7A");

        public static readonly Brush TextBrush = Solid(Text);
        public static readonly Brush TextDimBrush = Solid(TextDim);
        public static readonly Brush TextFaintBrush = Solid(TextFaint);
        public static readonly Brush AccentBrush = Solid(Accent);
        public static readonly Brush AccentSoftBrush = Solid(AccentSoft);
        public static readonly Brush SunBrush = Solid(Hex("#FBBF24"));
        public static readonly Brush MoonBrush = Solid(Hex("#A5B4FC"));
        public static readonly Brush ErrorBrush = Solid(Hex("#F87171"));

        public static SolidColorBrush Solid(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        public static Brush Vertical(Color top, Color bottom)
        {
            var b = new LinearGradientBrush(top, bottom, 90);
            b.Freeze();
            return b;
        }

        static Color Hex(string s)
        {
            return (Color)ColorConverter.ConvertFromString(s);
        }

        public static ResourceDictionary LoadStyles()
        {
            return (ResourceDictionary)XamlReader.Parse(Styles);
        }

        const string Styles = @"
<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">

  <Style x:Key=""IconButton"" TargetType=""Button"">
    <Setter Property=""Foreground"" Value=""#9DB0D3""/>
    <Setter Property=""Background"" Value=""Transparent""/>
    <Setter Property=""FontFamily"" Value=""Segoe Fluent Icons, Segoe MDL2 Assets""/>
    <Setter Property=""FontSize"" Value=""12""/>
    <Setter Property=""Width"" Value=""30""/>
    <Setter Property=""Height"" Value=""28""/>
    <Setter Property=""Margin"" Value=""2,0,0,0""/>
    <Setter Property=""Cursor"" Value=""Hand""/>
    <Setter Property=""Focusable"" Value=""False""/>
    <Setter Property=""Template"">
      <Setter.Value>
        <ControlTemplate TargetType=""Button"">
          <Border Background=""{TemplateBinding Background}"" CornerRadius=""6"">
            <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property=""IsEnabled"" Value=""False"">
              <Setter Property=""Opacity"" Value=""0.35""/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
    <Style.Triggers>
      <Trigger Property=""IsMouseOver"" Value=""True"">
        <Setter Property=""Background"" Value=""#1F3358""/>
        <Setter Property=""Foreground"" Value=""#F1F5FF""/>
      </Trigger>
      <Trigger Property=""IsPressed"" Value=""True"">
        <Setter Property=""Background"" Value=""#2A4475""/>
      </Trigger>
    </Style.Triggers>
  </Style>

  <Style x:Key=""CloseButton"" TargetType=""Button"" BasedOn=""{StaticResource IconButton}"">
    <Style.Triggers>
      <Trigger Property=""IsMouseOver"" Value=""True"">
        <Setter Property=""Background"" Value=""#C42B1C""/>
        <Setter Property=""Foreground"" Value=""White""/>
      </Trigger>
    </Style.Triggers>
  </Style>

  <Style x:Key=""CityButton"" TargetType=""Button"">
    <Setter Property=""Background"" Value=""Transparent""/>
    <Setter Property=""Foreground"" Value=""#F1F5FF""/>
    <Setter Property=""Padding"" Value=""4,1""/>
    <Setter Property=""Margin"" Value=""-4,-1,0,-1""/>
    <Setter Property=""Cursor"" Value=""Hand""/>
    <Setter Property=""Focusable"" Value=""False""/>
    <Setter Property=""HorizontalAlignment"" Value=""Left""/>
    <Setter Property=""Template"">
      <Setter.Value>
        <ControlTemplate TargetType=""Button"">
          <Border Background=""{TemplateBinding Background}"" CornerRadius=""6"" Padding=""{TemplateBinding Padding}"">
            <ContentPresenter VerticalAlignment=""Center""/>
          </Border>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
    <Style.Triggers>
      <Trigger Property=""IsMouseOver"" Value=""True"">
        <Setter Property=""Background"" Value=""#1F3358""/>
      </Trigger>
    </Style.Triggers>
  </Style>

  <Style x:Key=""PillButton"" TargetType=""Button"">
    <Setter Property=""Foreground"" Value=""#F1F5FF""/>
    <Setter Property=""Background"" Value=""#2563EB""/>
    <Setter Property=""FontFamily"" Value=""Segoe UI""/>
    <Setter Property=""FontSize"" Value=""11""/>
    <Setter Property=""FontWeight"" Value=""SemiBold""/>
    <Setter Property=""Padding"" Value=""10,3""/>
    <Setter Property=""Cursor"" Value=""Hand""/>
    <Setter Property=""Focusable"" Value=""False""/>
    <Setter Property=""Template"">
      <Setter.Value>
        <ControlTemplate TargetType=""Button"">
          <Border Background=""{TemplateBinding Background}"" CornerRadius=""10"" Padding=""{TemplateBinding Padding}"">
            <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
          </Border>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
    <Style.Triggers>
      <Trigger Property=""IsMouseOver"" Value=""True"">
        <Setter Property=""Background"" Value=""#3B82F6""/>
      </Trigger>
    </Style.Triggers>
  </Style>

  <Style x:Key=""SearchBox"" TargetType=""TextBox"">
    <Setter Property=""Foreground"" Value=""#F1F5FF""/>
    <Setter Property=""CaretBrush"" Value=""#60A5FA""/>
    <Setter Property=""SelectionBrush"" Value=""#3B82F6""/>
    <Setter Property=""FontFamily"" Value=""Segoe UI""/>
    <Setter Property=""FontSize"" Value=""13""/>
    <Setter Property=""Padding"" Value=""30,7,8,7""/>
    <Setter Property=""Template"">
      <Setter.Value>
        <ControlTemplate TargetType=""TextBox"">
          <Border x:Name=""bd"" Background=""#0B1428"" BorderBrush=""#2A3F6B"" BorderThickness=""1"" CornerRadius=""8"">
            <ScrollViewer x:Name=""PART_ContentHost"" Margin=""{TemplateBinding Padding}"" VerticalAlignment=""Center""/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property=""IsKeyboardFocused"" Value=""True"">
              <Setter TargetName=""bd"" Property=""BorderBrush"" Value=""#3B82F6""/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key=""ZoneItem"" TargetType=""ListBoxItem"">
    <Setter Property=""Foreground"" Value=""#F1F5FF""/>
    <Setter Property=""FocusVisualStyle"" Value=""{x:Null}""/>
    <Setter Property=""Cursor"" Value=""Hand""/>
    <Setter Property=""Template"">
      <Setter.Value>
        <ControlTemplate TargetType=""ListBoxItem"">
          <Border x:Name=""bd"" Background=""Transparent"" CornerRadius=""6"" Padding=""10,5"" Margin=""0,1"">
            <ContentPresenter/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property=""IsMouseOver"" Value=""True"">
              <Setter TargetName=""bd"" Property=""Background"" Value=""#1C2F52""/>
            </Trigger>
            <Trigger Property=""IsSelected"" Value=""True"">
              <Setter TargetName=""bd"" Property=""Background"" Value=""#2563EB""/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style TargetType=""ScrollBar"">
    <Setter Property=""Width"" Value=""6""/>
    <Setter Property=""MinWidth"" Value=""6""/>
    <Setter Property=""Background"" Value=""Transparent""/>
    <Setter Property=""Template"">
      <Setter.Value>
        <ControlTemplate TargetType=""ScrollBar"">
          <Track x:Name=""PART_Track"" IsDirectionReversed=""True"">
            <Track.Thumb>
              <Thumb>
                <Thumb.Template>
                  <ControlTemplate TargetType=""Thumb"">
                    <Border CornerRadius=""3"" Background=""#2C4270""/>
                  </ControlTemplate>
                </Thumb.Template>
              </Thumb>
            </Track.Thumb>
          </Track>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key=""GripThumb"" TargetType=""Thumb"">
    <Setter Property=""Cursor"" Value=""SizeNWSE""/>
    <Setter Property=""Width"" Value=""16""/>
    <Setter Property=""Height"" Value=""16""/>
    <Setter Property=""ToolTip"" Value=""Drag to resize (or Ctrl + mouse wheel)""/>
    <Setter Property=""Template"">
      <Setter.Value>
        <ControlTemplate TargetType=""Thumb"">
          <Grid Background=""Transparent"">
            <Path x:Name=""p"" Data=""M14,5 L5,14 M14,9 L9,14 M14,13 L13,14"" Stroke=""#3F5A8C"" StrokeThickness=""1.4""
                  StrokeStartLineCap=""Round"" StrokeEndLineCap=""Round""/>
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger Property=""IsMouseOver"" Value=""True"">
              <Setter TargetName=""p"" Property=""Stroke"" Value=""#60A5FA""/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style TargetType=""ToolTip"">
    <Setter Property=""Background"" Value=""#0F1A30""/>
    <Setter Property=""Foreground"" Value=""#E6EDFF""/>
    <Setter Property=""BorderBrush"" Value=""#2A3F6B""/>
    <Setter Property=""FontFamily"" Value=""Segoe UI""/>
    <Setter Property=""FontSize"" Value=""12""/>
    <Setter Property=""Padding"" Value=""8,4""/>
  </Style>
</ResourceDictionary>";
    }
}
