using CSV;
using CsvHelper.Configuration;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace ProfitDLL.Factory;

internal abstract class CSVFactory
{
    protected static string PathFile { get; set; }
    protected static CsvConfiguration _config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = false,

    };
    public abstract Func<Task> ProcessAsync { get; }
    public abstract Func<ICsv, Task<bool>> AddAsync { get; }
    protected abstract Func<string, Task> WriteAsync { get; }
}