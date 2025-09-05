using System.Collections.Generic;

namespace ProfitDLL.Config;

//internal record ProfitSettings(string User, string Password, string ActivationCode, List<string> Tickers, string PathFile);

internal record ProfitSettings
{
    public string User { get; set; }
    public string Password { get; set; }
    public string ActivationCode { get; set; }
    public List<string> Tickers { get; set; }
    public string PathFile { get; set; }
}


