using CSV.Entities;
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

    public CSVTopBookFactory(string pathFile)
    {
        PathFile = pathFile ?? throw new ArgumentNullException(nameof(pathFile));

    }
    public override Func<ICsv, Task<bool>> AddAsync => (ICsv csv) =>
    {
        var topBook = (TopBook)csv;
        string key = $"{topBook.Ticker}_topbook";
        var ret = _TopBook.AddOrUpdate(key, topBook, (key, old) => topBook);

        return Task.FromResult(ret.Ticker is not null);
    };

    public override Func<Task> ProcessAsync => async () =>
    {
        foreach (var topBook in _TopBook?.Keys)
            await WriteAsync(topBook);
    };

    protected override Func<string, Task> WriteAsync => async (string ticker) =>
    {
        var date = DateTime.Now.ToString("yyyyMMdd");

        using (var stream = File.Open(@$"CsvFiles\{date}\{date}_{ticker}.csv", FileMode.Append))
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
