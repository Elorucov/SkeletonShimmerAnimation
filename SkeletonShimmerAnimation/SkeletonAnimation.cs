using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace SkeletonShimmerAnimation {
    public class SkeletonAnimation {
        public static readonly DependencyProperty IsAnimatableProperty = DependencyProperty.RegisterAttached(
            "IsAnimatable",
            typeof(bool),
            typeof(SkeletonAnimation),
            new PropertyMetadata(false)
        );

        public static bool GetIsAnimatable(Shape shape) {
            return (bool)shape.GetValue(IsAnimatableProperty);
        }

        public static void SetIsAnimatable(Shape shape, bool value) {
            shape.SetValue(IsAnimatableProperty, value);
        }

        public static void FillPanelAndStartAnimation(Panel container, DataTemplate template, Color color, int count) {
            FillPanelAndStartAnimation(container, new List<DataTemplate> { template }, color, count);
        }

        public static void FillPanelAndStartAnimation(Panel container, List<DataTemplate> templates, Color color, int count) {
            if (count < 0) throw new ArgumentException("Value must be 0 or positive!", nameof(count));
            if (templates == null && templates.Count == 0) throw new ArgumentException("Value does not null or empty!", nameof(templates));
            if (container == null) throw new ArgumentNullException(nameof(container));

            List<Shape> animatableElements = new List<Shape>();
            for (int i = 0; i < count; i++) {
                DataTemplate template = templates[i % templates.Count];
                FrameworkElement templateRoot = (FrameworkElement)template.LoadContent();
                container.Children.Add(templateRoot);
                FindShapesWhere(animatableElements, templateRoot, (e) => GetIsAnimatable(e));
            }
            if (animatableElements.Count == 0) throw new ArgumentException($"No one template contains no animatable elements!", nameof(templates));
            Start(container, animatableElements, color);
        }

        public static async void Start(FrameworkElement container, List<Shape> animatableElements, Color color) {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (animatableElements == null && animatableElements.Count == 0) throw new ArgumentException("Value does not null or empty!", nameof(animatableElements));

            bool isGradientSupported = ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5);

            Color transparent = Color.FromArgb(0, color.R, color.G, color.B);
            await System.Threading.Tasks.Task.Delay(10); // needed to getting correct values from TransformPoint.

            foreach (Shape shape in animatableElements) {
                // К сожалению, получить размеры и offset-ы элементов
                // можно только у Rectangle и Ellipse, поэтому надо проверить.
                bool allow = false;
                if (shape is Rectangle || shape is Ellipse) allow = true;
                if (!allow) throw new ArgumentException("Only Rectangle and Ellipse can be animated", nameof(animatableElements));

                GeneralTransform gt = shape.TransformToVisual(container);
                Point p = gt.TransformPoint(new Point(0, 0));
                float x = (float)p.X;
                float cw = (float)container.ActualWidth;
                float ew = (float)shape.ActualWidth;
                float diffs = 1 / ew * x; // for start point
                float diffe = (cw / ew) - diffs; // for end point

                System.Diagnostics.Debug.WriteLine($"X: {p.X}; Y: {p.Y}; S: {shape.Width}x{shape.Height}");
                System.Diagnostics.Debug.WriteLine($"DS: {Math.Round(-diffs, 2)}; DE: {Math.Round(diffe, 2)}");

                Compositor _compositor = ElementCompositionPreview.GetElementVisual(container).Compositor;
                SpriteVisual _maskVisual = _compositor.CreateSpriteVisual();
                CompositionSurfaceBrush _surfBrush = (CompositionSurfaceBrush)shape.GetAlphaMask();
                CompositionMaskBrush _maskBrush = _compositor.CreateMaskBrush();
                _maskBrush.Mask = _surfBrush;

                _maskVisual.Brush = _maskBrush;
                _maskVisual.Size = new Vector2((float)shape.ActualWidth, (float)shape.ActualHeight);
                ElementCompositionPreview.SetElementChildVisual(shape, _maskVisual);
                var animation = _compositor.CreateScalarKeyFrameAnimation();

                if (isGradientSupported) {
                    var gradient = _compositor.CreateLinearGradientBrush();
                    gradient.ColorStops.Add(_compositor.CreateColorGradientStop(0f, transparent));
                    gradient.ColorStops.Add(_compositor.CreateColorGradientStop(0.25f, color));
                    gradient.ColorStops.Add(_compositor.CreateColorGradientStop(0.35f, color));
                    gradient.ColorStops.Add(_compositor.CreateColorGradientStop(0.5f, transparent));

                    gradient.StartPoint = new Vector2(-diffs, 0f);
                    gradient.EndPoint = new Vector2(diffe, 0f);
                    gradient.Offset = new Vector2(-(float)container.ActualWidth / 2);
                    _maskBrush.Source = gradient;

                    animation.InsertKeyFrame(1, (float)container.ActualWidth * 2, _compositor.CreateLinearEasingFunction());
                    animation.Duration = TimeSpan.FromSeconds(1);
                    animation.IterationBehavior = AnimationIterationBehavior.Forever;
                    gradient.StartAnimation("Offset.X", animation);

                    shape.Unloaded += (a, b) => {
                        gradient.Dispose();
                    };
                } else {
                    var colorb = _compositor.CreateColorBrush();
                    colorb.Color = color;
                    _maskBrush.Source = colorb;

                    animation.InsertKeyFrame(0f, 0, _compositor.CreateLinearEasingFunction());
                    animation.InsertKeyFrame(0.5f, 1, _compositor.CreateLinearEasingFunction());
                    animation.InsertKeyFrame(1f, 0, _compositor.CreateLinearEasingFunction());
                    animation.Duration = TimeSpan.FromSeconds(1);
                    animation.IterationBehavior = AnimationIterationBehavior.Forever;
                    _maskVisual.StartAnimation("Opacity", animation);
                }
                shape.Unloaded += (a, b) => {
                    _compositor.Dispose();
                    _surfBrush.Dispose();
                    _maskVisual.Dispose();
                    _maskBrush.Dispose();
                    animation.Dispose();
                };
            }
        }

        private static void FindShapesWhere(List<Shape> results, DependencyObject startNode, Func<Shape, bool> func) {
            int count = VisualTreeHelper.GetChildrenCount(startNode);
            for (int i = 0; i < count; i++) {
                DependencyObject current = VisualTreeHelper.GetChild(startNode, i);
                if (current is Shape el && func(el)) {
                    results.Add(el);
                }
                FindShapesWhere(results, current, func);
            }
        }
    }
}