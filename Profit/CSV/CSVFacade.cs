using CSV;
using System;
using System.IO;
using System.Threading;

namespace ProfitDLL.CSV;

internal class CSVFacade : IDisposable
{

    private static CSVTopBook10Factory _topbook10 = null;
    private static CSVTopBookFactory _topbook = null;
    private static CSVTradesFactory _trades = null;
    private static CSVBookEventFactory _bookevent = null;
    private static Thread _ThreadWriteCsv = null;
    private static CancellationTokenSource cs = new CancellationTokenSource();
    public CSVFacade(string pathFile)
    {
        _topbook10 = new CSVTopBook10Factory(pathFile);
        _topbook = new CSVTopBookFactory(pathFile);
        _trades = new CSVTradesFactory(pathFile);
        _bookevent = new CSVBookEventFactory(pathFile);

        _ThreadWriteCsv = new Thread(new ThreadStart(() =>
        {
            while (!cs.IsCancellationRequested)
            {
                this.CreateDirectory(pathFile);

                _topbook10.ProcessAsync();
                _topbook.ProcessAsync();
                _trades.ProcessAsync();

                Thread.Sleep(10000);
            }
        }));

        _ThreadWriteCsv.Name = "ThreadWriteCsv";
        _ThreadWriteCsv.Start();
    }
    private void CreateDirectory(string pathFile)
    {
        string path = $"{pathFile}\\{DateTime.Now.ToString("yyyyMMdd")}";

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    public async void AddCsvTopBookAsync(Object csv) => await _topbook.AddAsync((ICsv)csv);
    public async void AddCsvTradesAsync(Object csv) => await _trades.AddAsync((ICsv)csv);
    public async void AddCsvBookAsync(Object csv) => await _topbook10.AddAsync((ICsv)csv);
    public async void AddCsvBookEvent(Object csv) => await _bookevent.AddAsync((ICsv)csv);

    //public async Task AddCsvTopBookAsync(Csv csv) => await _topbook.AddAsync(csv);
    //public async Task AddCsvTradesAsync(Csv csv) => await _trades.AddAsync(csv);
    //public async Task AddCsvBookAsync(Csv csv) => await _topbook10.AddAsync(csv);
    //public async Task AddCsvBookEvent(Csv csv) => await _bookevent.AddAsync(csv);
    public void Dispose() => cs.Cancel();

}
