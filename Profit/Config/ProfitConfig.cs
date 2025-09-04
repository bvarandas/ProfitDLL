using System.Collections.Generic;

namespace ProfitDLL.Config;

internal record ProfitConfig(string User, string Password, string ActivationCode, List<string> Tickers, string pathFile);


