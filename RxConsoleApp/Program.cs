using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Console;
using static System.Math;

using System.Reactive.Linq;
using System.Threading;
using System.Reactive.Subjects;
using System.Reactive.Disposables;
using System.Reactive.Threading.Tasks;
using System.Reactive.Concurrency;
using System.Reactive;

namespace RxConsoleApp
{
  public static class Program
  {
    public static void Main(string[] args)
    {
      SubscribeOn();

      Console.ReadLine();
    }


    private static void SubscribeOn()
    {
      var source = Observable.Create<int>(observer =>
      {
        WriteLine($"Subscribe-Start [{CurrentThreadId}]");
        observer.OnNext(1);
        observer.OnNext(2);
        observer.OnNext(3);
        observer.OnCompleted();
        WriteLine($"Subscribe-End [{CurrentThreadId}]");
        return Disposable.Empty;
      });


      WriteLine($"Creating Subscription [{CurrentThreadId}]");
      source
        .SubscribeOn(NewThreadScheduler.Default)
        .Subscribe(
          i => WriteLine($"OnNext {i} [{CurrentThreadId}]"), 
          () => WriteLine($"OnCompleted [{CurrentThreadId}]"));
      WriteLine($"Subscription Created [{CurrentThreadId}]");
    }


    private static int CurrentThreadId { get { return Thread.CurrentThread.ManagedThreadId; } }


    private static void SchedulingThreading()
    {
      var source = Observable.Interval(TimeSpan.FromSeconds(0.5)).Take(3);
      source.Dump("source");

      var throttle = source.Throttle(TimeSpan.FromSeconds(1));
      throttle.Dump("throttle");
    }


    private static void HotObservables()
    {
      var source = Observable
        .Interval(TimeSpan.FromMilliseconds(200))
        .Publish();
      source.Connect();

      using (var first = source.Subscribe(value => WriteLine($"First {value}")))
      {
        Thread.Sleep(250);
        using (var second = source.Subscribe(value => WriteLine($"Second {value}")))
        {
          Thread.Sleep(2000);
        }
        Thread.Sleep(2000);
      }
    }


    private static void ColdObservables()
    {
      var source = Observable.Interval(TimeSpan.FromMilliseconds(200));
      using (var first = source.Subscribe(value => WriteLine($"First {value}")))
      {
        Thread.Sleep(250);
        using (var second = source.Subscribe(value => WriteLine($"Second {value}")))
        {
          Thread.Sleep(2000);
        }
        Thread.Sleep(2000);
      }
    }


    private static void Enumerables()
    {
      GreedyEnumerable().Take(1);
      LazyEnumerable().Take(1).ToList();
    }


    private static IEnumerable<int> GreedyEnumerable()
    {
      return Enumerable.Range(1, 5).ToList();
    }


    private static IEnumerable<int> LazyEnumerable()
    {
      for(int i = 1; i <= 10; i++)
      {
        WriteLine($"Yield {i}");
        yield return i;
      }
    }


    private static void Zip()
    {
      var sourceX = Observable.Interval(TimeSpan.FromMilliseconds(100)).Take(5);
      sourceX.Dump("x");
      var sourceY = Observable.Interval(TimeSpan.FromMilliseconds(130)).Take(5);
      sourceY.Dump("y");

      var zip = sourceX.Zip(sourceY, (x, y) => $"{x}.{y}");
      zip.Dump("zip");

    }


    private static void Switch()
    {
     // var source = Observable.Interval(TimeSpan.FromSeconds(0.3)).Take(5)
    }


    private static void PushNumbersPeriodically()
    {
      int[] numbers = { 20, 30, 40, 50, 60 };
      TimeSpan period = TimeSpan.FromMilliseconds(500);
      var source = Observable.Create<int>(subscribe: observer =>
      {
        foreach(var number in numbers)
        {
          observer.OnNext(number);
          Thread.Sleep(period);
        }
        observer.OnCompleted();

        return () => { };
      });

      source.Dump("source1");
      source.Dump("source2");
    }

    private static void Create()
    {
      var source = Observable.Create<int>(observer =>
      {
        var interval = Program.Interval(100).Take(5).Subscribe(
          i => observer.OnNext((int)i),
          ex => observer.OnError(ex),
          () => observer.OnCompleted());

        return interval;
      });

      source.Dump("source");
    }


    private static void Concat()
    {
      var sourceX = Program.Interval(100).Skip(3).Take(2);
      sourceX.Dump("x");

      var sourceY = Program.Interval(100).Skip(9).Take(2);
      sourceY.Dump("y");

      var concat = sourceX.Concat(sourceY);
      concat.Dump("concat");
    }


    private static void Using()
    {
      var source = Observable.Using(
        resourceFactory: () => Disposable.Create(() => WriteLine("Resource disposed!")),
        observableFactory: (res) => Observable.Interval(TimeSpan.FromMilliseconds(200)).Take(10));

      source.Dump("source");
    }

    private static void Finally()
    {
      var source = new Subject<int>();
      source.Dump("source");

      var result = source.Finally(() => WriteLine("Finally!"));
      result.Dump("result");
      result.Subscribe(i => { }, () => { });

      source.OnNext(10);
      source.OnNext(20);
      source.OnError(new InvalidOperationException("Oops!"));
    }


    public static IObservable<long> Interval(int periodMilliseconds)
    {
      return Observable.Interval(TimeSpan.FromMilliseconds(periodMilliseconds));
    }


    public static IObservable<T> CorrectFinally<T>(this IObservable<T> source, Action finallyAction)
    {
      return Observable.Create<T>(subscribe: observer =>
      {
        var finallyOnce = Disposable.Create(finallyAction);
        var subscription = source.Subscribe(
          onNext: observer.OnNext, 
          onError: ex => 
          {
            try
            {
              observer.OnError(ex);
            }
            finally
            {
              finallyOnce.Dispose();
            }
          }, 
          onCompleted: () => 
          {
            try
            {
              observer.OnCompleted();
            }
            finally
            {
              finallyOnce.Dispose();
            }

          });
        return new CompositeDisposable(finallyOnce, subscription);
      });
    }


    private static void Catch()
    {
      var source = new Subject<int>();
      source.Dump("source");

      source
        .Catch(Observable.Empty<int>())
        .Dump("catched");

      source.OnNext(10);
      source.OnNext(20);
      source.OnError(new InvalidOperationException("Oops!"));
    }


    private static void ToEvent()
    {
      var source = Observable
        .Interval(TimeSpan.FromMilliseconds(200))
        .Take(10);
      source.Dump("source");

      IEventSource<long> eventSource = source.ToEvent();
      eventSource.OnNext += (i) => WriteLine($"Event {i}");      
    }


    private static void ToTask()
    {
      var source = Observable
        .Interval(TimeSpan.FromMilliseconds(200))
        .Take(10);
      source.Dump("source");

      Task<long> t = source.ToTask();
      long result = t.Result;
    }


    private static void ToLookup()
    {
      var source = Observable
        .Interval(TimeSpan.FromMilliseconds(200))
        .Take(5);
      source.Dump("source");

      var lookup = source
        .ToLookup(keySelector: i => i, elementSelector: i => DateTimeOffset.UtcNow.AddDays(-i))
        .ToEnumerable()
        .First();

    }


    private static void ToDictionary()
    {
      var source = Observable
        .Interval(TimeSpan.FromMilliseconds(500))
        .Take(10);
      source.Dump("source");

      source
        .ToDictionary(keySelector: i => i.ToString(), elementSelector: i => new { Index = i })
        .Subscribe((d) => WriteLine($"{d}"), () => WriteLine("Completed!"));
      source.Dump("dict");
    }


    private static void ToEnumerable()
    {
      var source = Observable
        .Interval(TimeSpan.FromMilliseconds(500))
        .Take(10)
        .Select(i => (int)i);
      source.Dump("source projected");

      var numbers = source.ToEnumerable();
      foreach (var n in numbers)
      {
        WriteLine($"Enumerated {n}");
      }
      WriteLine("Enumeration complete.");
    }

    private static void SelectMany()
    {
      var source = Observable.Range(1, 3);
      source.Dump("source");
      source.SelectMany(i => Observable.Range(1, i)).Dump("select many");
    }


    private static void Timestamps()
    {
      var source = Observable.Interval(TimeSpan.FromSeconds(0.3));
      source.Dump("source");
      source.Timestamp().Dump("timestamped");
    }


    private static void Select()
    {
      var source = Observable.Range(1, 10);
      source.Dump("source");
      source.Select(i => new { I = i, Name = i.ToString() }).Dump("projection");
    }


    private static void Scans()
    {
      var source = Observable.Range(1, 10);
      source.Dump("source");
      source.Scan((agg, current) => agg + current).Dump("scan");
    }


    private static void FunctionalFolds()
    {
      var subject = new Subject<int>();
      subject.Dump("subject");

      var first = subject.FirstAsync();
      first.Dump("first");

      subject.OnNext(10);
      subject.OnNext(20);
      subject.OnNext(30);

      subject.OnCompleted();
    }

    private static void Aggregates()
    {
      var subject = new Subject<int>();
      subject.Dump("subject");
      subject.Sum().Dump("sum");
      subject.Min().Dump("min");
      subject.Max().Dump("max");
      subject.Average().Dump("average");

      subject
        .Aggregate(0, (aggregated, current) => aggregated + (int)Pow(current, 2))
        .Dump("Sum-Of-Squares");

      subject.OnNext(10);
      subject.OnNext(20);
      subject.OnNext(30);

      subject.OnCompleted();
    }


    private static void Count2()
    {
      var subject = new Subject<int>();
      subject.Dump("subject");
      subject.Count().Dump("count");

      subject.OnNext(1);
      subject.OnNext(2);
      subject.OnCompleted();
    }

    private static void Count()
    {
      var source = Observable.Range(1, 10);
      source.Dump("source");
      var count = source.Count();
      count.Dump("count");
    }


    public static void Dump<T>(this IObservable<T> source, string moniker)
    {
      source.Subscribe(
        value => WriteLine($"{moniker}: OnNext({value}) [{Thread.CurrentThread.ManagedThreadId}]"),
        ex => WriteLine($"{moniker}: OnError({ex.GetBaseException().Message}) [{Thread.CurrentThread.ManagedThreadId}]"),
        () => WriteLine($"{moniker}: OnCompleted [{Thread.CurrentThread.ManagedThreadId}]"));
    }


    private static void SequenceEqual()
    {
      var x = new Subject<int>();
      x.Subscribe(i => WriteLine($"x-{i}"));

      var y = new Subject<int>();
      y.Subscribe(i => WriteLine($"y-{i}"));

      var eq = x.SequenceEqual(y);
      eq.Subscribe(b => WriteLine($"Equal = {b.ToString().ToUpper()}"), () => WriteLine("Equal completed"));

      Enumerable.Range(1, 5).ToList().ForEach(i =>
      {
        x.OnNext(i);
        //y.OnNext(i);
      });
      x.OnCompleted();
      y.OnCompleted();
    }

    private static void DistinctClause()
    {
      var numbers = (new int[] { 1, 2, 3, 3, 5, 1, 2, 3, 10 }).ToObservable();
      numbers.Subscribe(i => WriteLine($"All {i}"));
      numbers.Distinct().Subscribe(i => WriteLine($"Distinct {i}"));
      numbers.DistinctUntilChanged().Subscribe(i => WriteLine($"Distinct Until Changed {i}"));
    }


    private static void WhereClause()
    {
      var numbers = Observable.Range(1, 5)
        .Where(i => (i % 2) == 0)
        .Subscribe(i => WriteLine($"{i}"));
    }


    private static void FromTask()
    {
      var voidTask = Task.Run(() =>
      {
        WriteLine("Task starting...");
        Thread.Sleep(1000);
        WriteLine("Task finished");
      });

      var source = voidTask.ToObservable();
      Thread.Sleep(2000);
      source.Subscribe(unit => WriteLine("OnNext"), () => WriteLine("Completed"));
    }


    private static void ObservableGenerate()
    {
      var observable = Observable.Generate(0, (i) => i < 10, (i) => i + 1, (i) => i, (i) => TimeSpan.FromMilliseconds(100));

      Action<string> log = message => WriteLine($"{message} on thread {Thread.CurrentThread.ManagedThreadId}");
      observable.Subscribe(i => log($"A-{i}"));
      Thread.Sleep(2000);
      observable.Subscribe(i => log($"B-{i}"));
    }


    private static IObservable<T> CustomInterval<T>(T value, TimeSpan dueTime) => Observable.Generate(
      initialState: value,
      condition: v => false,
      iterate: v => v,
      resultSelector: v => v,
      timeSelector: v => dueTime);

    private static IObservable<int> CustomRange(int start, int count) => Observable.Generate(
      initialState: start,
      condition: i => i < (start + count),
      iterate: i => i + 1,
      resultSelector: i => i);


    private static void ObservableRange()
    {
      var sequence = Observable.Range(1, 10, System.Reactive.Concurrency.Scheduler.Default);
      sequence.Subscribe((i) => WriteLine(i));
      sequence.Subscribe((i) => WriteLine(i));
    }


    private static void TimerTicks()
    {
      // Každý Subscribe vytvorí vlastný timer a Elapsed stream!
      IObservable<long> observable = Observable.Create<long>((observer) =>
      {
        var timer = new System.Timers.Timer(1000);
        timer.Elapsed += (sender, e) => observer.OnNext(e.SignalTime.Ticks);
        timer.Start();
        return timer;
      });

      using (observable.Subscribe((ticks) => WriteLine($"Signal {new DateTime(ticks)}")))
      {
        Thread.Sleep(10000);
      }
      observable.Subscribe((ticks) => { });
      observable.Subscribe((ticks) => { });
    }


    private static void LazyEvaluation()
    {
      Action<IObserver<string>> blockingPushAction = (observer) =>
      {
        observer.OnNext("a");
        observer.OnNext("b");
        Thread.Sleep(1000);
      };

      Func<ISubject<string>> createBlockingSubject = () =>
      {
        var subject = new ReplaySubject<string>();
        blockingPushAction(subject);
        return subject;
      };

      Func<IObservable<string>> blocking = () => createBlockingSubject();


      Func<IObservable<string>> nonBlocking = () => Observable.Create<string>((observer) =>
      {
        blockingPushAction(observer);
        return () => WriteLine("Unsubscribed");
      });

      blocking().Subscribe(s => WriteLine(s));
      nonBlocking().Subscribe(s => WriteLine(s));
    }

    private static void ObservableReturn()
    {
      IObservable<string> observable = Observable.Return("sole value");
      observable.Subscribe(s => WriteLine(s));
      observable.Subscribe(s => WriteLine(s));
    }



    private static void Step7()
    {
      using (var scope = Disposable.Create(() => WriteLine("Disposed!")))
      {
        WriteLine("Working...");
      }
    }


    private static void Step6()
    {
      var subject = new ReplaySubject<string>();
      subject.Subscribe(
        s => WriteLine($"Subscriber A: {s}"),
        (ex) => WriteLine($"Error: {ex.Message}"),
        () => WriteLine("Subscriber A Completed!"));

      var list = Enumerable
        .Range(1, 5)
        .Select(i => i.ToString())
        .ToList();

      list.ForEach(s => subject.OnNext($"1-{s}"));

      subject.OnError(new InvalidOperationException("Error!"));
    }


    private static void Step5()
    {
      var subject = new AsyncSubject<string>();
      subject.Subscribe(s => WriteLine($"Subscriber A: {s}"), () => WriteLine("Subscriber A Completed!"));
      subject.Subscribe(s => WriteLine($"Subscriber B: {s}"), () => WriteLine("Subscriber B Completed!"));

      var list = Enumerable
        .Range(1, 5)
        .Select(i => i.ToString())
        .ToList();

      list.ForEach(s => subject.OnNext($"1-{s}"));

      subject.OnCompleted();
      subject.OnNext("Cmpleted?");
    }


    private static void Step4()
    {
      var subject = new BehaviorSubject<string>("x");
      subject.Subscribe(s => WriteLine($"Subscriber A: {s}"), () => WriteLine("Subscriber A Completed!"));
      subject.Subscribe(s => WriteLine($"Subscriber B: {s}"), () => WriteLine("Subscriber B Completed!"));

      var list = Enumerable
        .Range(1, 5)
        .Select(i => i.ToString())
        .ToList();

      WriteLine("\nFirst push\n");
      list.ForEach(s => subject.OnNext($"1-{s}"));

      subject.Subscribe(s => WriteLine($"Subscriber C: {s}"), () => WriteLine("Subscriber C Completed!"));

      WriteLine("\nSecond push\n");
      list.ForEach(s => subject.OnNext($"2-{s}"));
    }


    private static void Step3()
    {
      var subject = new ReplaySubject<string>();
      subject.Subscribe(s => WriteLine($"Subscriber A: {s}"), () => WriteLine("Subscriber A Completed!"));
      subject.Subscribe(s => WriteLine($"Subscriber B: {s}"), () => WriteLine("Subscriber B Completed!"));

      var list = Enumerable
        .Range(1, 5)
        .Select(i => i.ToString())
        .ToList();

      WriteLine("\nFirst push\n");
      list.ForEach(s => subject.OnNext($"1-{s}"));

      subject.Subscribe(s => WriteLine($"Subscriber C: {s}"), () => WriteLine("Subscriber C Completed!"));

      WriteLine("\nSecond push\n");
      list.ForEach(s => subject.OnNext($"2-{s}"));
    }


    private static void Step2()
    {
      var subject = new Subject<string>();
      subject.Subscribe(s => WriteLine($"Subscriber A: {s}"), () => WriteLine("Subscriber A Completed!"));
      subject.Subscribe(s => WriteLine($"Subscriber B: {s}"), () => WriteLine("Subscriber B Completed!"));

      var list = Enumerable
        .Range(1, 5)
        .Select(i => i.ToString())
        .ToList();

      WriteLine("\nFirst push\n");
      list.ForEach(s => subject.OnNext($"1-{s}"));

      subject.Subscribe(s => WriteLine($"Subscriber C: {s}"), () => WriteLine("Subscriber C Completed!"));

      WriteLine("\nSecond push\n");
      list.ForEach(s => subject.OnNext($"2-{s}"));
    }


    private static void Step1()
    {
      var observable = "Reactive Extensions".ToObservable();
      observable.Subscribe(new ConsoleObserver<char>());
      observable.Subscribe(new ConsoleObserver<char>());
    }

    private class ConsoleObserver<T> : IObserver<T>
    {
      private static int _s_counter;
      private int _id;

      public ConsoleObserver()
      {
        this._id = ++_s_counter;
      }
      public void OnCompleted()
      {
        WriteLine("Completed!");
      }

      public void OnError(Exception error)
      {
        WriteLine($"Error: {error.GetBaseException().Message}");
      }

      public void OnNext(T value)
      {
        Thread.Sleep(100);
        WriteLine($"Observer#{this._id}: {value}");
      }
    }
  }
}
