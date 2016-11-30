using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
  /// Interaction logic for BingSearchWindow.xaml
  /// </summary>
  public partial class BingSearchWindow : Window
  {
    public BingSearchWindow()
    {
      InitializeComponent();

      var textChangedEventStream = Observable.FromEventPattern<TextChangedEventHandler, TextChangedEventArgs>(
        h => this._searchText.TextChanged += h, 
        h => this._searchText.TextChanged -= h);

      textChangedEventStream
        .Select((args) => this._searchText.Text)
        .Throttle(TimeSpan.FromSeconds(0.75))
        .Do(term => this.Dispatcher.Invoke(() => this._statusText.Text = $"Searching for {term}..."))
        .Select(term => BingSearchFacade.Search(term))
        .Switch()
        .ObserveOnDispatcher()
        .Subscribe(results => 
        {
          this._resultsList.Items.Clear();
          this._statusText.Text = $"Showing search results";
          results.ToList().ForEach(s => this._resultsList.Items.Add(s));
        },
        ex => MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error));
    }



    private static class BingSearchFacade
    {
      private const string EndpointUrl = "https://api.cognitive.microsoft.com/bing/v5.0";
      private const string AccountName = "BingSearch";
      private const string ApiKey = "xbb8359595784679386739486798679f";


      public static IObservable<IEnumerable<string>> Search(string term)
      {
        Task<IEnumerable<string>> results = SearchCoreAsync(term);
        return results.ToObservable();
      }


      private static async Task<IEnumerable<string>> SearchCoreAsync(string term)
      {
        var client = new HttpClient();
        var queryString = HttpUtility.ParseQueryString("");

        // Request headers
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ApiKey);

        // Request parameters
        queryString["q"] =term;
        queryString["count"] = "20";
        queryString["offset"] = "0";
        queryString["mkt"] = "en-us";
        queryString["safesearch"] = "Moderate";
        var uri = $"{EndpointUrl}/search?" + queryString;

        var response = await client.GetAsync(uri);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        JObject results = JObject.Parse(json);
        return results["webPages"]["value"].Select(page => $"{page["name"]} - {page["displayUrl"]}");
      }
    }
  }


}
