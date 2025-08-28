using CSV;
using System.Threading;
using System.Threading.Tasks;

namespace ProfitDLL.CSV;

internal class CSVFacade
{

    private static CSVTopBook10Factory _topbook10 = new CSVTopBook10Factory();
    private static CSVTopBookFactory _topbook = new CSVTopBookFactory();
    private static CSVTradesFactory _trades = new CSVTradesFactory();
    private static CSVBookEventFactory _bookevent = new CSVBookEventFactory();
    private static Thread _ThreadWriteCsv = null;
    private static CancellationTokenSource cs = new CancellationTokenSource();
    public CSVFacade()
    {
        _ThreadWriteCsv = new Thread(new ThreadStart(() =>
        {
            while (!cs.IsCancellationRequested)
            {
                _topbook10.ProcessAsync();

                _topbook.ProcessAsync();

                _trades.ProcessAsync();

                Thread.Sleep(10000);
            }
        }));

        _ThreadWriteCsv.Name = "ThreadWriteCsv";

        _ThreadWriteCsv.Start();
    }

    public async Task AddCsvTopBookAsync(Csv csv)
    {
        await _topbook.AddAsync(csv);
    }

    public async Task AddCsvTradesAsync(Csv csv)
    {
        await _trades.AddAsync(csv);
    }

    public async Task AddCsvBookAsync(Csv csv)
    {
        await _topbook10.AddAsync(csv);
    }

    public async Task AddCsvBookEvent(Csv csv)
    {
        await _bookevent.AddAsync(csv);
    }
}
