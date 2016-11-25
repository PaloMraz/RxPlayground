using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adobel.Reporting.Core.Internals
{
  /// <summary>
  /// Allows "quasi-determinate" progress reporting for undeterminate processes (like database or network calls).
  /// The progress is reported for given duration with values from <see cref="MinValue"/> to <see cref="MaxValue"/>
  /// with a frequency specified by <see cref="ProgressReportPeriod"/>.
  /// <para>
  /// The undeterminate process should be implemented inside using block that calls the Start method. The method 
  /// returns an <see cref="IDisposable"/> whose <see cref="IDisposable.Dispose"/> signals the end of the process.
  /// </para>
  /// <para>
  /// The durtation of each process is remembered and an average duration is used the next time the process
  /// starts. This could theoretically make the durations more "determinate".
  /// </para>
  /// </summary>
  internal class IndeterminateProgressReporter
  {
    private TimeSpan _currentDuration;
    private readonly List<TimeSpan> _durations = new List<TimeSpan>(); 

    public IndeterminateProgressReporter(
      TimeSpan initialDuration,
      int min,
      int max,
      TimeSpan progressReportPeriod)
    {
      this.InitialDuration = initialDuration;
      this._currentDuration = initialDuration;
      this.MinValue = min;
      this.MaxValue = max;
      this.ProgressReportPeriod = progressReportPeriod;
    }


    public TimeSpan InitialDuration { get; private set; }


    public int MaxValue { get; private set; }


    public int MinValue { get; private set; }


    public TimeSpan ProgressReportPeriod { get; private set; }


    public IDisposable Start(Action<int> reportProgress)
    {
      int numberOfTimesProgressWillBeReported = (int)(this._currentDuration.Ticks / this.ProgressReportPeriod.Ticks);
      int progressValueIncrementPerReport = (this.MaxValue - this.MinValue) / numberOfTimesProgressWillBeReported;
      DateTime start = DateTime.UtcNow;

      var source = Observable.Generate(
        initialState: this.MinValue, 
        condition: i => i < this.MaxValue, 
        iterate: i => i + progressValueIncrementPerReport, 
        resultSelector: i => i, 
        timeSelector: i => this.ProgressReportPeriod);

      var subscription = source.Subscribe(i => reportProgress(i));

      IDisposable finish = Disposable.Create(() => this.NoteRecentProcessDuration(DateTime.UtcNow - start));

      return new CompositeDisposable(subscription, finish);
    }


    private void NoteRecentProcessDuration(TimeSpan duration)
    {
      this._durations.Add(duration);
      this._currentDuration = TimeSpan.FromTicks(this._durations.Sum((t) => t.Ticks) / this._durations.Count);
      if (this._durations.Count > 10)
      {
        this._durations.RemoveAt(0);
      }
    }
  }
}
