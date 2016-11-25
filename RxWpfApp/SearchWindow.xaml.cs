using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
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
  /// Interaction logic for SearchWindow.xaml
  /// </summary>
  public partial class SearchWindow : Window
  {
    public SearchWindow()
    {
      InitializeComponent();

      var searchTextChangedEventObservable = Observable.FromEventPattern<TextChangedEventArgs>(this._searchText, nameof(this._searchText.TextChanged));
      searchTextChangedEventObservable
        .Select(e => this._searchText.Text)
        .Throttle(TimeSpan.FromSeconds(0.5))
        .Select(term => this.Search(term))
        .Switch()
        .Select(s => s.ToList())
        .Subscribe(list =>
        {
          this.Dispatcher.Invoke(() =>
          {
            this._resultsList.Items.Clear();
            list.ForEach(s => this._resultsList.Items.Add(s));
          });
        });
    }


    private IObservable<IEnumerable<string>> Search(string term)
    {
      Task<IEnumerable<string>> result = SearchEndpoint.SearchAsync(term);
      return result.ToObservable();
    }


    private static class SearchEndpoint
    {
      private readonly static List<string> Words =
@"
Problém
Anasoft má záväzok na konci kampane vytvoriť sadu štatistických reportov o priebehu kampane pre každý zo štyroch zmluvných cukrovarov. Po minulé roky tieto reporty vytvárala manuálne Andrejka, čo jej trvalo cca 2 dni per cukrovar. Tento rok to chceme čiastočne automatizovať aby sme ušetrili jej čas a zamedzili potenciálnym chybám („ľudský faktor“).
Analýza
Príklad manuálne vytváraného reportu aj s komentármi pre zdrojové údaje je v P:\Projects\INE\Agrana_AdOBeL\ADOBEL2016\REPORTY\ADOBEL_Capacity_util_Camp.xlsx.

Údaje pre report sú v princípe tvorené jednoduchými SELECT príkazmi, v určitých prípadoch manuálne „joinované“ v rámci výsledného Excel súboru.
Návrh riešenia
1.	Pre jednotlivé sekcie reportu skonštruujeme SELECT príkazy, ktorých resultset bude tvoriť vstupné údaje príslušnej sekcie.
2.	Vytvoríme šablónu Excel reportu (súbor ReportTemplate.xlsx) v ktorom bude jeden „Overview“ worksheet s koláčovými grafmi, ktorý referencuje údaje z niekoľkých „Data“ worksheetov, ktoré budú obsahovať údaje zo skonštruovaných SELECT-ov.
3.	Vytvoríme jednoduchú GUI utilitu, ktorá umožní používateľovi zadať ConnectionString na cieľovú databázu, následne vykoná SELECT-y z kroku 1., naplní výslednými údajmi „Data“ worksheety v ReportTemplate.xlsx a tak vygeneruje cieľový „standalone“ Excel report.

GUI utilitu vytvoríme ako jednoduchú WPF aplikáciu AdobelReportGenerator.exe s jedným oknom, kde bude možné špecifikovať:

•	ConnectionString na cieľovú Log a Master databázu,
•	názov a cestu pre vygenerovaný .xlsx report.

Následne aplikácia vytvorí kópiu ReportTemplate.xlsx ako cieľový report, vytvorí „Data“ worksheety a spustí potrebné SELECT-y pre ich naplnenie.

Šablóna ReportTemplate.xlsx bude ako embedded resource, pre manipuláciu s .xlsx súbormi použijeme EPPlus.

T-SQL pre jednotlivé SELECT-y budú ako samostatné linkované .sql súbory, ktoré vieme odladiť samostatne cez SSMS.

Štruktúra solution bude nasledujúca (root folder je Agrana_Adobel_2\2016)".Split(' ', '\n', '\r').Distinct().ToList();


      public static async Task<IEnumerable<string>> SearchAsync(string term)
      {
        return await Task.Run(async () => 
        {
          await Task.Delay(700);
          return Words.Where(w => w.Contains(term));
        });
      }
    }
  }
}
