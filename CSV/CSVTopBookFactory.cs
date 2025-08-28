using CSV;
using CsvHelper;
using ProfitDLL.Factory;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace ProfitDLL.CSV;

internal class CSVTopBookFactory : CSVFactory
{
    private static ConcurrentDictionary<string, TopBook>
    _TopBook = new ConcurrentDictionary<string, TopBook>();

    public override Func<Csv, Task<bool>> AddAsync => (Csv csv) =>
    {
        var topBook = csv as TopBook;
        string key = $"{topBook.ticker}_topbook";
        var ret = _TopBook.AddOrUpdate(key, topBook, (key, old) => topBook);

        return Task.FromResult(ret is not null);
    };

    public override Func<Task> ProcessAsync => async () =>
    {
        foreach (var topBook in _TopBook?.Keys)
            await WriteAsync(topBook);
    };

    protected override Func<string, Task> WriteAsync => async (string ticker) =>
    {
        var date = DateTime.Now.ToString("yyyyMMdd");

        using (var stream = File.Open(@$"CsvFiles\{date}_{ticker}.csv", FileMode.Append))
        using (var writer = new StreamWriter(stream))
        using (var csv = new CsvWriter(writer, _config))
        {
            if (_TopBook.TryGetValue(ticker, out var topBooks))
            {
                csv.WriteRecord(topBooks);
            }
        }
    };
}
