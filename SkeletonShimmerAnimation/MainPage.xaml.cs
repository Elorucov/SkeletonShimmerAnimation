using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SkeletonShimmerAnimation {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page {
        public MainPage() {
            this.InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            DataTemplate template1 = Resources["skeletonTemplate1"] as DataTemplate;
            DataTemplate template2 = Resources["skeletonTemplate2"] as DataTemplate;
            DataTemplate template3 = Resources["skeletonTemplate3"] as DataTemplate;
            SolidColorBrush brush = (SolidColorBrush)App.Current.Resources["ApplicationSecondaryForegroundThemeBrush"];
            SkeletonAnimation.FillPanelAndStartAnimation(container, new List<DataTemplate> { template1, template2, template3 }, brush.Color, 7);
        }
    }
}