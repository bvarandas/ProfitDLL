using CSV.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ProfitDLL.Config;
using ProfitDLL.CSV;
using ProfitDLL.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ProfitDLL.Services;

internal class DllConectorFacadeService : BackgroundService
{
    private static CSVFacade _csv = null;
    public const int NL_OK = 0x00000000;  // OK
    private static readonly object writeLock = new object();

    public static TNewTinyBookCallBack _newTinyBookCallBack = new TNewTinyBookCallBack(NewTinyBookCallBack);
    public static TStateCallback _stateCallback = new TStateCallback(StateCallback);
    public static TOfferBookCallback _offerBookCallbackV2 = new TOfferBookCallback(OfferBookCallbackV2);
    private static TTradeCallback _TradeCallback = new TTradeCallback(TradeCallback);
    public static TAccountCallback _accountCallback = new TAccountCallback(AccountCallback);

    public static List<TConnectorOffer> m_lstOfferSell = new List<TConnectorOffer>();
    public static List<TConnectorOffer> m_lstOfferBuy = new List<TConnectorOffer>();

    public static bool bAtivo = false;
    public static bool bMarketConnected = false;

    private readonly ProfitConfig _config;

    public DllConectorFacadeService(IOptions<ProfitConfig> options)
    {
        _config = options.Value;
    }

    public static void OfferBookCallbackV2(TAssetID assetId, int nAction, int nPosition, int Side, int nQtd, int nAgent, long nOfferID, double sPrice, int bHasPrice, int bHasQtd, int bHasDate, int bHasOfferID, int bHasAgent, [MarshalAs(UnmanagedType.LPWStr)] string date_str, IntPtr pArraySell, IntPtr pArrayBuy)
    {
        List<TConnectorOffer> lstBook;

        if (Side == 0)
            lstBook = m_lstOfferBuy;
        else
            lstBook = m_lstOfferSell;

        if (!DateTime.TryParseExact(date_str, "dd/MM/yyyy HH:mm:ss.fff", null, DateTimeStyles.None, out DateTime date))
        {
            date = DateTime.MinValue;
        }

        var dateNow = DateTime.Now;

        var offer = new TConnectorOffer(assetId.Ticker, sPrice, nQtd, nAgent, nOfferID, date);

        switch (nAction)
        {
            case 0:
                {
                    if (lstBook.Count == 0)
                        lstBook.Add(offer);

                    if (nPosition >= 0 && nPosition < lstBook.Count)
                    {
                        lstBook.Insert(lstBook.Count - nPosition, offer);
                    }
                }
                break;
            case 1:
                {
                    if (nPosition >= 0 && nPosition < lstBook.Count)
                    {
                        TConnectorOffer currentOffer = lstBook[lstBook.Count - 1 - nPosition];
                        if (bHasQtd != 0)
                            currentOffer.Qtd += offer.Qtd;
                        if (bHasPrice != 0)
                            currentOffer.Price = offer.Price;
                        if (bHasOfferID != 0)
                            currentOffer.OfferID = offer.OfferID;
                        if (bHasAgent != 0)
                            currentOffer.Agent = offer.Agent;
                        if (bHasDate != 0)
                            currentOffer.Date = offer.Date;
                        lstBook[lstBook.Count - 1 - nPosition] = currentOffer;
                    }
                }
                break;
            case 2:
                {
                    if (nPosition >= 0 && nPosition < lstBook.Count)
                        lstBook.RemoveAt(lstBook.Count - nPosition - 1);
                }
                break;
            case 3:
                {
                    if (nPosition >= 0 && nPosition < lstBook.Count)
                        lstBook.RemoveRange(lstBook.Count - nPosition - 1, nPosition + 1);
                }
                break;
            case 4:
                {
                    if (pArraySell != IntPtr.Zero)
                    {
                        MarshalOfferBuffer(pArraySell, m_lstOfferSell, assetId.Ticker);
                    }

                    if (pArrayBuy != IntPtr.Zero)
                    {
                        MarshalOfferBuffer(pArrayBuy, m_lstOfferBuy, assetId.Ticker);
                    }
                }
                break;
            default: break;
        }


        if (pArraySell != IntPtr.Zero)
        {
            MarshalOfferBuffer(pArraySell, m_lstOfferSell, assetId.Ticker);
        }

        if (pArrayBuy != IntPtr.Zero)
        {
            MarshalOfferBuffer(pArrayBuy, m_lstOfferBuy, assetId.Ticker);
        }
        if (Side == 0)
            m_lstOfferBuy = lstBook;
        else
            m_lstOfferSell = lstBook;

        if (bHasQtd == 1)
        {
            var Timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

            var dataOfertaStamp = new DateTimeOffset(date).ToUnixTimeSeconds();

            Task.Run(() =>
            {
                _csv.AddCsvBookEventAsync(new BookEvent(assetId.Ticker, Timestamp.ToString(), dataOfertaStamp.ToString(), nQtd, offer.Volume, sPrice, Side));
            });
        }

        if (m_lstOfferBuy.Count > 10 && m_lstOfferSell.Count > 10)
        {
            //WriteSync($"Ticker: {assetId.Ticker} | Bolsa: {assetId.Bolsa} |  quant: {nQtd} | Side: {Side} | Position: {nPosition} | Price: {sPrice} | nAgent: {nAgent} ");

            var lstBuy = m_lstOfferBuy[0..10];

            var lstSell = m_lstOfferSell[0..10];

            var Timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

            var dataOfertaStamp = new DateTimeOffset(date).ToUnixTimeSeconds();

            var topBook = new TopBook(assetId.Ticker, Timestamp.ToString(), dataOfertaStamp.ToString(),
                lstBuy[0].Qtd, lstBuy[0].Volume, lstBuy[0].Price, lstSell[0].Qtd, lstSell[0].Volume, lstSell[0].Price);

            //ThreadPool.QueueUserWorkItem(new WaitCallback(_csv.AddCsvTopBookAsync), topBook);
            Task.Run(() => _csv.AddCsvTopBookAsync(topBook));

            var topBook10 = new TopBook10(assetId.Ticker, Timestamp.ToString(), dataOfertaStamp.ToString(),
                lstBuy[0].Qtd, lstBuy[0].Volume, lstBuy[0].Price, lstSell[0].Qtd, lstSell[0].Volume, lstSell[0].Price,
                lstBuy[1].Qtd, lstBuy[1].Volume, lstBuy[1].Price, lstSell[1].Qtd, lstSell[1].Volume, lstSell[1].Price,
                lstBuy[2].Qtd, lstBuy[2].Volume, lstBuy[2].Price, lstSell[2].Qtd, lstSell[2].Volume, lstSell[2].Price,
                lstBuy[3].Qtd, lstBuy[3].Volume, lstBuy[3].Price, lstSell[3].Qtd, lstSell[3].Volume, lstSell[3].Price,
                lstBuy[4].Qtd, lstBuy[4].Volume, lstBuy[4].Price, lstSell[4].Qtd, lstSell[4].Volume, lstSell[4].Price,
                lstBuy[5].Qtd, lstBuy[5].Volume, lstBuy[5].Price, lstSell[5].Qtd, lstSell[5].Volume, lstSell[5].Price,
                lstBuy[6].Qtd, lstBuy[6].Volume, lstBuy[6].Price, lstSell[6].Qtd, lstSell[6].Volume, lstSell[6].Price,
                lstBuy[7].Qtd, lstBuy[7].Volume, lstBuy[7].Price, lstSell[7].Qtd, lstSell[7].Volume, lstSell[7].Price,
                lstBuy[8].Qtd, lstBuy[8].Volume, lstBuy[8].Price, lstSell[8].Qtd, lstSell[8].Volume, lstSell[8].Price,
                lstBuy[9].Qtd, lstBuy[9].Volume, lstBuy[9].Price, lstSell[9].Qtd, lstSell[9].Volume, lstSell[9].Price);

            //ThreadPool.QueueUserWorkItem(new WaitCallback(_csv.AddCsvBookAsync), topBook10);
            Task.Run(() => _csv.AddCsvBookAsync(topBook10));
        }
    }

    private static void TradeCallback(TAssetID assetId, string date, uint tradeNumber, double price, double vol, int qtd, int buyAgent, int sellAgent, int tradeType, int bIsEdit)
    {
        if (!DateTime.TryParseExact(date, "dd/MM/yyyy HH:mm:ss.fff", null, DateTimeStyles.None, out DateTime dateTime))
        {
            dateTime = DateTime.MinValue;
        }

        Task.Run(() =>
        {
            _csv.AddCsvTradesAsync(new Trade(assetId.Ticker, dateTime, tradeNumber, price, vol, qtd, buyAgent, sellAgent, tradeType));
        });

#if DEBUG
        WriteSync($"TradeCallback: {assetId.Ticker}: {assetId.Bolsa} | price {price} | date: {dateTime}");
#endif
    }

    public static void MarshalOfferBuffer(IntPtr buffer, List<TConnectorOffer> lstOffer, string ticker)
    {
        lstOffer.Clear();
        var offset = 0;

        // lê o cabeçalho
        var qtdOffer = Marshal.ReadInt32(buffer, offset);
        offset += 4;

        var pointerSize = Marshal.ReadInt32(buffer, offset);
        offset += 4;

        // lê as ofertas
        for (int i = 0; i < qtdOffer; i++)
        {
            var bufferOffer = new byte[53];
            Marshal.Copy(buffer + offset, bufferOffer, 0, 53);

            var offer = new TConnectorOffer();

            offer.Price = BitConverter.ToDouble(bufferOffer, 0);
            offer.Qtd = BitConverter.ToInt64(bufferOffer, 8);
            offer.Agent = BitConverter.ToInt32(bufferOffer, 16);
            offer.OfferID = BitConverter.ToInt64(bufferOffer, 20);

            //var length = BitConverter.ToUInt16(bufferOffer, 28);

            var strDate = bufferOffer[30..].Select(x => (char)x);
            offer.Ticker = ticker;
            offer.Date = DateTime.ParseExact(strDate.ToArray(), "dd/MM/yyyy HH:mm:ss.fff", null);

            lstOffer.Add(offer);

            offset += 53;
        }

        // lê o rodapé
        var trailer = new byte[pointerSize - offset];
        Marshal.Copy(buffer + offset, trailer, 0, trailer.Length);

        var flags = (OfferBookFlags)BitConverter.ToUInt32(trailer);

        WriteSync($"OfferBook: Qtd {qtdOffer} | Tam {pointerSize} | {flags}");

        ProfitDLL.FreePointer(buffer, pointerSize);
    }

    private static void WriteResult(long result, [CallerMemberName] string callerName = "")
    {
        lock (writeLock)
        {
            if (result <= 0)
            {
                Console.WriteLine($"{callerName}: {(NResult)result}");
            }
            else
            {
                Console.WriteLine($"{callerName}: {result}");
            }
        }
    }

    private static void WriteSync(string text)
    {
        lock (writeLock)
        {
            Console.WriteLine(text);
        }
    }
    private static void SubscribeAssets(List<string> tickers)
    {
        tickers.ForEach(input =>
        {
            var split = input.Split(':');

            var retVal = ProfitDLL.SubscribeTicker(split[0], split[1]);

            if (retVal == NL_OK)
            {
                WriteSync("Subscribe com sucesso");
            }
            else
            {
                WriteSync($"Erro no subscribe: {retVal}");
            }

        });
    }

    private static void DoSubscribeOfferBook(List<string> tickers)
    {
        tickers.ForEach(input =>
        {
            var split = input.Split(':');

            var retVal = ProfitDLL.SubscribeOfferBook(split[0], split[1]);

            WriteResult(retVal);

            Thread.Sleep(1000);
        });
    }

    public static void NewTinyBookCallBack(TAssetID assetId, double price, int qtd, int side)
    {
        var sideName = side == 0 ? "buy" : "sell";
        WriteSync($"NewTinyBookCallBack: {assetId.Ticker}: {sideName} {price} {qtd}");
    }



    private static int StartDLL(string key, string user, string password)
    {
        int retVal;
        bool bRoteamento = true;
        if (bRoteamento)
        {
            retVal = ProfitDLL.DLLInitializeLogin(key, user, password, _stateCallback, null, null, _accountCallback, _TradeCallback, null/*_newDailyCallback*/, /*_priceBookCallback*/ null, _offerBookCallbackV2, null, null, null);// _newTinyBookCallBack);
        }
        else
        {
            retVal = ProfitDLL.DLLInitializeMarketLogin(key, user, password, _stateCallback, null, null /*_newDailyCallback*/, null/*_priceBookCallback*/, null, null, null, _newTinyBookCallBack);
        }

        if (retVal != NL_OK)
        {
            WriteSync($"Erro na inicialização: {retVal}");
        }
        else
        {


            //ProfitDLL.SetOrderCallback(_orderCallback);
            //ProfitDLL.SetOrderHistoryCallback(_orderHistoryCallback);
            ProfitDLL.SetOfferBookCallbackV2(_offerBookCallbackV2);
            //ProfitDLL.SetAssetListInfoCallbackV2(_assetListInfoCallbackV2);
            //ProfitDLL.SetAdjustHistoryCallbackV2(_adjustHistoryCallbackV2);
            //ProfitDLL.SetAssetPositionListCallback(_assetPositionListCallback);
        }

        return retVal;
    }
    public static void StateCallback(int nConnStateType, int result)
    {

        if (nConnStateType == 0)
        { // notificacoes de login
            if (result == 0)
            {
                WriteSync("Login: Conectado");
            }
            if (result == 1)
            {
                WriteSync("Login: Invalido");
            }
            if (result == 2)
            {
                WriteSync("Login: Senha invalida");
            }
            if (result == 3)
            {
                WriteSync("Login: Senha bloqueada");
            }
            if (result == 4)
            {
                WriteSync("Login: Senha Expirada");
            }
            if (result == 200)
            {
                WriteSync("Login: Erro Desconhecido");
            }
        }
        if (nConnStateType == 1)
        { // notificacoes de broker
            if (result == 0)
            {
                WriteSync("Broker: Desconectado");
            }
            if (result == 1)
            {
                WriteSync("Broker: Conectando");
            }
            if (result == 2)
            {
                WriteSync("Broker: Conectado");
            }
            if (result == 3)
            {
                WriteSync("Broker: HCS Desconectado");
            }
            if (result == 4)
            {
                WriteSync("Broker: HCS Conectando");
            }
            if (result == 5)
            {
                WriteSync("Broker: HCS Conectado");
            }
        }

        if (nConnStateType == 2)
        { // notificacoes de login no Market
            if (result == 0)
            {
                WriteSync("Market: Desconectado");
            }
            if (result == 1)
            {
                WriteSync("Market: Conectando");
            }
            if (result == 2)
            {
                WriteSync("Market: csConnectedWaiting");
            }
            if (result == 3)
            {
                bMarketConnected = false;
                WriteSync("Market: Não logado");
            }
            if (result == 4)
            {
                bMarketConnected = true;
                WriteSync("Market: Conectado");
            }
        }

        if (nConnStateType == 3)
        { // notificacoes de login no Market
            if (result == 0)
            {
                //Atividade: Valida
                bAtivo = true;
                WriteSync("Profit: Notificação de Atividade Valida");
            }
            else
            {
                //Atividade: Invalida
                bAtivo = false;
                WriteSync("Profit: Notificação de Atividade Invalida");
            }
        }
    }

    public static void AccountCallback(int nCorretora,
        [MarshalAs(UnmanagedType.LPWStr)] string CorretoraNomeCompleto,
        [MarshalAs(UnmanagedType.LPWStr)] string AccountID,
        [MarshalAs(UnmanagedType.LPWStr)] string NomeTitular)
    {
        WriteSync($"AccountCallback: {AccountID} - {NomeTitular}");
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        string user = _config.User;
        string pass = _config.Password;
        string activationCode = _config.ActivationCode;
        var tickers = _config.Tickers;  //configuration.GetSection("ProfitSettings:Tickers").Get<List<string>>();
        string pathFile = _config.pathFile; // configuration["ProfitSettings:PathFile"];

        Console.WriteLine($"activationCode: {activationCode}");
        Console.Write($"Usuário: {user}");
        string password = pass;
        _csv = new CSVFacade(pathFile);

        ThreadPool.SetMinThreads(10, 10);
        ThreadPool.SetMaxThreads(100, 100);

        //if (StartDLL(key, user, password) != NL_OK)
        if (StartDLL(activationCode, user, password) != NL_OK)
        {
            return Task.FromException(new Exception("Não conseguiu efetuar o login com a DLL"));
        }

        Task.Run(() =>
        {
            Thread.Sleep(5000);

            //subscribe Tickers
            SubscribeAssets(tickers);

            //subscribe Offer Tickers
            DoSubscribeOfferBook(tickers);
        });

        return Task.CompletedTask;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
