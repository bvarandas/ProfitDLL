using CSV;
using CsvHelper;
using ProfitDLL.Factory;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace ProfitDLL.CSV;

internal class CSVTopBook10Factory : CSVFactory, IDisposable
{
    private static ConcurrentDictionary<string, ConcurrentQueue<TopBook10>>
        _TopBook10 = new ConcurrentDictionary<string, ConcurrentQueue<TopBook10>>();

    public CSVTopBook10Factory(string pathFile) : base()
    {
        PathFile = pathFile ?? throw new ArgumentNullException(nameof(pathFile));
    }

    public override Func<Task> ProcessAsync => async () =>
    {
        foreach (var topBook in _TopBook10?.Keys)
            await WriteAsync(topBook);
    };

    public override Func<Csv, Task<bool>> AddAsync => (Csv csv) =>
    {
        var topBook = csv as TopBook10;
        var bag = new ConcurrentQueue<TopBook10>();

        bag.Enqueue(topBook);

        var ret = _TopBook10.AddOrUpdate($"{topBook.ticker}_book10", bag, (key, old) => bag);
        return Task.FromResult(ret is not null);
    };

    protected override Func<string, Task> WriteAsync => async (string ticker) =>
    {
        var date = DateTime.Now.ToString("yyyyMMdd");
        using (var stream = File.Open(@$"{PathFile}\{date}\{date}_{ticker}.csv", FileMode.Append))
        using (var writer = new StreamWriter(stream))
        using (var csv = new CsvWriter(writer, _config))
        {

            if (_TopBook10.TryGetValue(ticker, out var topBooks))
            {
                await csv.WriteRecordsAsync(topBooks);
                topBooks.Clear();
            }
        }
    };



    public void Dispose()
    {

    }
}
