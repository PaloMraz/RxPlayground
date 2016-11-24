using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reactive.Linq;

namespace RxPlayground
{
  [TestClass]
  public class MasterTests
  {
    [TestMethod]
    public void ObservableReturn()
    {
      IObservable<string> observable = Observable.Return("sole value");
    }
  }
}
