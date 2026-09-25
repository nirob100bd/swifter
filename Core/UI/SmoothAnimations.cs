using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Swifter.Core.UI;

public static class SmoothAnimations
{
    public static void FadeIn(UIElement element, double durationMs = 200)
    {
        element.Opacity = 0;
        element.Visibility = Visibility.Visible;
        var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(durationMs)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        element.BeginAnimation(UIElement.OpacityProperty, anim);
    }

    public static void FadeOut(UIElement element, double durationMs = 200, bool collapse = true)
    {
        var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(durationMs)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
        if (collapse) anim.Completed += (_, _) => element.Visibility = Visibility.Collapsed;
        element.BeginAnimation(UIElement.OpacityProperty, anim);
    }

    public static void SlideIn(FrameworkElement element, SlideDirection direction, double durationMs = 250)
    {
        var transform = new TranslateTransform();
        element.RenderTransform = transform;
        double from = direction switch
        {
            SlideDirection.Left => -element.ActualWidth,
            SlideDirection.Right => element.ActualWidth,
            SlideDirection.Up => -element.ActualHeight,
            SlideDirection.Down => element.ActualHeight,
            _ => 0
        };
        var anim = new DoubleAnimation(from, 0, TimeSpan.FromMilliseconds(durationMs)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        transform.BeginAnimation(TranslateTransform.XProperty, direction is SlideDirection.Left or SlideDirection.Right ? anim : null);
        transform.BeginAnimation(TranslateTransform.YProperty, direction is SlideDirection.Up or SlideDirection.Down ? anim : null);
        FadeIn(element, durationMs);
    }

    public static void SlideOut(FrameworkElement element, SlideDirection direction, double durationMs = 250)
    {
        if (element.RenderTransform is not TranslateTransform transform)
        {
            transform = new TranslateTransform();
            element.RenderTransform = transform;
        }
        double to = direction switch
        {
            SlideDirection.Left => -element.ActualWidth,
            SlideDirection.Right => element.ActualWidth,
            SlideDirection.Up => -element.ActualHeight,
            SlideDirection.Down => element.ActualHeight,
            _ => 0
        };
        var anim = new DoubleAnimation(0, to, TimeSpan.FromMilliseconds(durationMs)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
        anim.Completed += (_, _) => element.Visibility = Visibility.Collapsed;
        transform.BeginAnimation(TranslateTransform.XProperty, direction is SlideDirection.Left or SlideDirection.Right ? anim : null);
        transform.BeginAnimation(TranslateTransform.YProperty, direction is SlideDirection.Up or SlideDirection.Down ? anim : null);
        FadeOut(element, durationMs, false);
    }

    public static void ScaleIn(FrameworkElement element, double durationMs = 200)
    {
        var transform = new ScaleTransform(0.9, 0.9, element.ActualWidth / 2, element.ActualHeight / 2);
        element.RenderTransform = transform;
        element.Opacity = 0;
        element.Visibility = Visibility.Visible;
        var scaleX = new DoubleAnimation(0.9, 1, TimeSpan.FromMilliseconds(durationMs)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        var scaleY = new DoubleAnimation(0.9, 1, TimeSpan.FromMilliseconds(durationMs)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        transform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
        transform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
        FadeIn(element, durationMs);
    }

    public static void ScaleOut(FrameworkElement element, double durationMs = 200)
    {
        if (element.RenderTransform is not ScaleTransform transform)
        {
            transform = new ScaleTransform(1, 1, element.ActualWidth / 2, element.ActualHeight / 2);
            element.RenderTransform = transform;
        }
        var scaleX = new DoubleAnimation(1, 0.9, TimeSpan.FromMilliseconds(durationMs)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
        var scaleY = new DoubleAnimation(1, 0.9, TimeSpan.FromMilliseconds(durationMs)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
        scaleX.Completed += (_, _) => element.Visibility = Visibility.Collapsed;
        transform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
        transform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
        FadeOut(element, durationMs, false);
    }

    public static void ColorFade(DependencyObject target, DependencyProperty property, Color from, Color to, double durationMs = 300)
    {
        var anim = new ColorAnimation(from, to, TimeSpan.FromMilliseconds(durationMs)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } };
        if (target is SolidColorBrush brush)
        {
            brush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
        }
    }

    public static Storyboard CreatePulseAnimation(FrameworkElement element, double scaleAmount = 1.05, double durationMs = 600)
    {
        var storyboard = new Storyboard();
        var scaleXAnim = new DoubleAnimation(1, scaleAmount, TimeSpan.FromMilliseconds(durationMs / 2)) { AutoReverse = true, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } };
        var scaleYAnim = new DoubleAnimation(1, scaleAmount, TimeSpan.FromMilliseconds(durationMs / 2)) { AutoReverse = true, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } };
        var transform = new ScaleTransform(1, 1, element.ActualWidth / 2, element.ActualHeight / 2);
        element.RenderTransform = transform;
        Storyboard.SetTarget(scaleXAnim, transform);
        Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath(ScaleTransform.ScaleXProperty));
        Storyboard.SetTarget(scaleYAnim, transform);
        Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath(ScaleTransform.ScaleYProperty));
        storyboard.Children.Add(scaleXAnim);
        storyboard.Children.Add(scaleYAnim);
        return storyboard;
    }
}

public enum SlideDirection
{
    Left,
    Right,
    Up,
    Down
}