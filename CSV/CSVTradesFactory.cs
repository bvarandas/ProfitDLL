using CSV;
using CsvHelper;
using ProfitDLL.Factory;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace ProfitDLL.CSV;

internal class CSVTradesFactory : CSVFactory
{
    private static ConcurrentDictionary<string, ConcurrentQueue<Trade>>
        _Trades = new ConcurrentDictionary<string, ConcurrentQueue<Trade>>();

    public CSVTradesFactory(string pathFile) : base()
    {
        PathFile = pathFile ?? throw new ArgumentNullException(nameof(pathFile));
        _config.HasHeaderRecord = false;
    }


    public override Func<Csv, Task<bool>> AddAsync => (Csv csv) =>
    {
        var trade = csv as Trade;
        string key = $"{trade.ticker}_trades";

        if (!_Trades.TryGetValue(trade.ticker, out ConcurrentQueue<Trade> bag))
            bag = new ConcurrentQueue<Trade>();

        bag.Enqueue(trade);
        var ret = _Trades.AddOrUpdate(trade.ticker, bag, (key, old) => bag);
        return Task.FromResult(ret != null);
    };

    public override Func<Task> ProcessAsync => async () =>
    {
        foreach (var trade in _Trades?.Keys)
            await WriteAsync(trade);
    };

    protected override Func<string, Task> WriteAsync => async (string ticker) =>
    {
        var date = DateTime.Now.ToString("yyyyMMdd");
        using (var stream = File.Open(@$"{PathFile}\{date}\{date}_{ticker}_trades.csv", FileMode.Append))
        using (var writer = new StreamWriter(stream))
        using (var csv = new CsvWriter(writer, _config))
        {
            if (_Trades.TryGetValue(ticker, out var trades))
            {
                await csv.WriteRecordsAsync(trades);
                trades.Clear();
            }
        }
    };
}
