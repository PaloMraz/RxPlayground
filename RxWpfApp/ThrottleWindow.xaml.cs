using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RxWpfApp
{
  /// <summary>
  /// Interaction logic for ThrottleWindow.xaml
  /// </summary>
  public partial class ThrottleWindow : Window
  {
    public ThrottleWindow()
    {
      InitializeComponent();


      var searchTextChangedEventObservable = Observable.FromEventPattern<TextChangedEventArgs>(this._searchText, nameof(this._searchText.TextChanged));
      searchTextChangedEventObservable
        .Throttle(TimeSpan.FromSeconds(0.7))
        .ObserveOnDispatcher()
        .Subscribe((e) => this._searchTextList.Items.Add(this._searchText.Text));
    }
  }
}

