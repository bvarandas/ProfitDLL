using CSV.Entities;
using CsvHelper;
using ProfitDLL.Factory;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProfitDLL.CSV;

internal class CSVBookEventFactory : CSVFactory, IDisposable
{
    private static ConcurrentDictionary<string, ConcurrentQueue<BookEvent>>
    _BookEvent = new ConcurrentDictionary<string, ConcurrentQueue<BookEvent>>();

    private static Thread _ThreadWriteCsv = null;
    private static CancellationTokenSource cs = new CancellationTokenSource();
    public CSVBookEventFactory(string pathFile)
    {
        PathFile = pathFile ?? throw new ArgumentNullException(nameof(pathFile));
        _ThreadWriteCsv = new Thread(new ThreadStart(() =>
        {
            while (!cs.IsCancellationRequested)
            {
                this.ProcessAsync();

                Thread.Sleep(3000);
            }
        }));

        _ThreadWriteCsv.Name = "ThreadWriteCsvBookEvent";

        _ThreadWriteCsv.Start();
    }

    public override Func<ICsv, Task<bool>> AddAsync => (ICsv csv) =>
    {
        var bookEvent = (BookEvent)csv;
        var key = $"{bookEvent.Ticker}_bookvent";

        if (!_BookEvent.TryGetValue(key, out ConcurrentQueue<BookEvent> bag))
            bag = new ConcurrentQueue<BookEvent>();

        bag.Enqueue(bookEvent);

        var ret = _BookEvent.AddOrUpdate(key, bag, (key, old) => bag);
        return Task.FromResult(ret != null);
    };

    public void Dispose()
    {
        cs.Cancel();
        GC.Collect();
    }

    public override Func<Task> ProcessAsync => async () =>
    {
        foreach (var topBook in _BookEvent?.Keys)
            await WriteAsync(topBook);
    };

    protected override Func<string, Task> WriteAsync => async (string ticker) =>
    {
        var date = DateTime.Now.ToString("yyyyMMdd");

        using (var stream = File.Open(@$"CsvFiles\{date}\{date}_{ticker}.csv", FileMode.Append))
        using (var writer = new StreamWriter(stream))
        using (var csv = new CsvWriter(writer, _config))
        {
            if (_BookEvent.TryGetValue(ticker, out var topBooks))
            {
                await csv.WriteRecordsAsync(topBooks);
                topBooks.Clear();

            }
        }
    };
}
