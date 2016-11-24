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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RxWpfApp
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();

      Func<Type, bool> isConcreteWindowSublassExceptMainWindow = (t) =>
        !t.IsAbstract &&
        typeof(Window).IsAssignableFrom(t) &&
        (t != typeof(MainWindow));

      foreach(var windowType in typeof(App).Assembly.GetExportedTypes().Where(t => isConcreteWindowSublassExceptMainWindow(t)))
      {
        var button = new Button()
        {
          Margin = new Thickness(8),
          Padding = new Thickness(4),
          Content = windowType.Name,
          DataContext = windowType
        };
        button.Click += this.OnWindowButtonClick;
        this._mainPanel.Children.Add(button);
      }
    }

    private void OnWindowButtonClick(object sender, RoutedEventArgs e)
    {
      try
      {
        var windowType = (Type)((Button)sender).DataContext;
        var window = (Window)Activator.CreateInstance(windowType);
        window.ShowDialog();
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.ToString());
      }
    }
  }
}
