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
  /// Interaction logic for MouseMoveWindow.xaml
  /// </summary>
  public partial class MouseMoveWindow : Window
  {
    public MouseMoveWindow()
    {
      InitializeComponent();

      var mouseMovesSequence = Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(
        handler => this._panel.MouseMove += handler, 
        handler => this._panel.MouseMove -= handler);
      mouseMovesSequence.Subscribe(e => 
      {
        Point p = e.EventArgs.GetPosition(this._panel);
        this._infoLabel.Content = $"X = {p.X}, Y = {p.Y}";
      });
    }
  }
}
