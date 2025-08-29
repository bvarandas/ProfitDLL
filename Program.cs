﻿using Microsoft.Extensions.Configuration;
using ProfitDLL.CSV;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace ProfitDLLClient;

#region Struturas para exemplo
public struct CandleTrade
{
    public CandleTrade(double close, double vol, double open, double max, double min, int qtd, string asset, DateTime date)
    {
        Close = close;
        Vol = vol;
        Qtd = qtd;
        Asset = asset;
        Date = date;
        Open = open;
        Max = max;
        Min = min;
    }

    public double Close { get; set; }
    public double Vol { get; set; }
    public double Max { get; set; }
    public double Min { get; set; }
    public double Open { get; set; }
    public int Qtd { get; set; }
    public string Asset { get; set; }
    public DateTime Date { get; set; }
}

public struct Trade
{
    public Trade(double price, double vol, int qtd, string asset, string date)
    {
        Price = price;
        Qtd = qtd;
        Asset = asset;
        Date = date;
        Vol = vol;
    }

    public double Price { get; }
    public double Vol { get; }
    public int Qtd { get; }
    public string Asset { get; }
    public string Date { get; }
}
#endregion

public partial class DLLConnector
{
    private static CSVFacade _csv = new CSVFacade();
    //////////////////////////////////////////////////////////////////////////////
    // Error Codes
    public const int NL_OK = 0x00000000;  // OK

    private static string ReadPassword()
    {
        Console.Write("Senha: ");

        var retVal = "";
        while (true)
        {
            var keyInfo = Console.ReadKey(intercept: true);
            var key = keyInfo.Key;

            if (key == ConsoleKey.Enter)
            {
                break;
            }

            if (key == ConsoleKey.Backspace && retVal.Length > 0)
            {
                retVal = retVal[..^1];

                var (left, top) = Console.GetCursorPosition();
                Console.SetCursorPosition(left - 1, top);

                Console.Write(" ");
                Console.SetCursorPosition(left - 1, top);
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                Console.Write("*");
                retVal += keyInfo.KeyChar;
            }
        }

        Console.WriteLine();

        return retVal;
    }

    #region obj garbage KeepAlive
    public static TAssetListCallback _assetListCallback = new TAssetListCallback(AssetListCallback);
    public static TAssetListInfoCallback _assetListInfoCallback = new TAssetListInfoCallback(AssetListInfoCallback);
    public static TAssetListInfoCallbackV2 _assetListInfoCallbackV2 = new TAssetListInfoCallbackV2(AssetListInfoCallbackV2);
    public static TStateCallback _stateCallback = new TStateCallback(StateCallback);
    public static TNewDailyCallback _newDailyCallback = new TNewDailyCallback(NewDailyCallback);
    public static TPriceBookCallback _priceBookCallback = new TPriceBookCallback(PriceBookCallback);
    public static TOfferBookCallback _offerBookCallbackV2 = new TOfferBookCallback(OfferBookCallbackV2);
    public static TNewTinyBookCallBack _newTinyBookCallBack = new TNewTinyBookCallBack(NewTinyBookCallBack);
    public static TAccountCallback _accountCallback = new TAccountCallback(AccountCallback);


    public static TConnectorBrokerAccountListCallback _brokerAccountListCallback = new TConnectorBrokerAccountListCallback(BrokerAccountListChangedCallback);
    public static TConnectorBrokerSubAccountListCallback _brokerSubAccountListCallback = new TConnectorBrokerSubAccountListCallback(BrokerSubAccountListChangedCallback);
    public static TChangeStateTickerCallback _changeStateTickerCallback = new TChangeStateTickerCallback(ChangeStateTickerCallback);
    public static TTheoreticalPriceCallback _theoreticalPriceCallback = new TTheoreticalPriceCallback(TheoreticalPriceCallback);
    public static TConnectorAssetPositionListCallback _assetPositionListCallback = new TConnectorAssetPositionListCallback(AssetPositionListCallback);
    public static TAdjustHistoryCallbackV2 _adjustHistoryCallbackV2 = new TAdjustHistoryCallbackV2(AdjustHistoryCallbackV2);
    public static TConnectorOrderCallback _orderCallback = new TConnectorOrderCallback(OrderCallback);
    public static TConnectorAccountCallback _orderHistoryCallback = new TConnectorAccountCallback(OrderHistoryCallback);
    private static TTradeCallback _TradeCallback = new TTradeCallback(TradeCallback);
    private static TConnectorTradeCallback _HistoryTradeCallback = new TConnectorTradeCallback(HistoryTradeCallback);
    #endregion

    #region variables
    public static Queue<Trade> Traders = new Queue<Trade>();
    private static readonly object TradeLock = new object();

    public static Queue<Trade> HistTraders = new Queue<Trade>();
    private static readonly object HistLock = new object();

    public static List<TGroupPrice> m_lstPriceSell = new List<TGroupPrice>();
    public static List<TGroupPrice> m_lstPriceBuy = new List<TGroupPrice>();

    public static List<TConnectorOffer> m_lstOfferSell = new List<TConnectorOffer>();
    public static List<TConnectorOffer> m_lstOfferBuy = new List<TConnectorOffer>();

    public static bool bAtivo = false;
    public static bool bMarketConnected = false;

    static readonly CultureInfo provider = CultureInfo.InvariantCulture;
    #endregion

    #region consts
    private const string dateFormat = "dd/MM/yyyy HH:mm:ss.fff";
    #endregion

    #region Client Functions

    public static void InvalidTickerCallback(TConnectorAssetIdentifier assetId)
    {
        if (string.IsNullOrWhiteSpace(strAssetListFilter) && strAssetListFilter == assetId.Ticker)
        {
            WriteSync($"InvalidTickerCallback: {assetId.Ticker}");
        }
    }

    ////////////////////////////////////////////////////////////////////////////////
    //Callback de alterãção em ordens
    public static void ChangeCotationCallback(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string date, uint tradeNumber, double sPrice)
    {
        WriteSync("changeCotationCallback: " + assetId.Ticker + " : " + date + " : " + sPrice);
    }

    public static void AssetListCallback(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string strName)
    {
        if (string.IsNullOrWhiteSpace(strAssetListFilter) || strAssetListFilter == assetId.Ticker)
        {
            WriteSync($"AssetListCallback: {assetId.Ticker} : {strName}");
        }
    }

    public static void AssetListInfoCallback(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string strName, [MarshalAs(UnmanagedType.LPWStr)] string strDescription, int nMinOrderQtd, int nMaxOrderQtd, int nLote, int stSecurityType, int ssSecuritySubType, double sMinPriceInc, double sContractMultiplier,
        [MarshalAs(UnmanagedType.LPWStr)] string validityDate, [MarshalAs(UnmanagedType.LPWStr)] string strISIN)
    {
        if ((string.IsNullOrWhiteSpace(strAssetListFilter) && !string.IsNullOrWhiteSpace(strISIN)) || strAssetListFilter == assetId.Ticker)
        {
            WriteSync($"AssetListInfoCallback: {assetId.Ticker} : {strName} - {strDescription} : ISIN: {strISIN}");
        }
    }

    public static void AssetListInfoCallbackV2(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string strName, [MarshalAs(UnmanagedType.LPWStr)] string strDescription, int nMinOrderQtd, int nMaxOrderQtd, int nLote, int stSecurityType, int ssSecuritySubType, double sMinPriceInc, double sContractMultiplier,
        [MarshalAs(UnmanagedType.LPWStr)] string validityDate, [MarshalAs(UnmanagedType.LPWStr)] string strISIN, [MarshalAs(UnmanagedType.LPWStr)] string strSetor, [MarshalAs(UnmanagedType.LPWStr)] string strSubSetor, [MarshalAs(UnmanagedType.LPWStr)] string strSegmento)
    {
        if ((string.IsNullOrWhiteSpace(strAssetListFilter) && !string.IsNullOrWhiteSpace(strISIN)) || strAssetListFilter == assetId.Ticker)
        {
            WriteSync($"AssetListInfoCallback: {assetId.Ticker} : {strName} - {strDescription} : ISIN: {strISIN} - Setor: {strSetor}");
        }
    }

    public static void ChangeStateTickerCallback(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string strDate, int nState)
    {
        WriteSync("changeStateTickerCallback: ticker=" + assetId.Ticker + " Date=" + strDate + " nState=" + nState);
    }

    ////////////////////////////////////////////////////////////////////////////////
    //Callback com mudança em posição
    public static void AssetPositionListCallback(TConnectorAccountIdentifier AccountID,
         TConnectorAssetIdentifier assetId, int EventID)
    {
        WriteSync($"AssetPositionListCallback: {AccountID.AccountID} - {assetId.Ticker} - {EventID}");
    }

    ////////////////////////////////////////////////////////////////////////////////
    //Callback com a lista de contas
    public static void AccountCallback(int nCorretora,
        [MarshalAs(UnmanagedType.LPWStr)] string CorretoraNomeCompleto,
        [MarshalAs(UnmanagedType.LPWStr)] string AccountID,
        [MarshalAs(UnmanagedType.LPWStr)] string NomeTitular)
    {
        WriteSync($"AccountCallback: {AccountID} - {NomeTitular}");
    }

    public static void BrokerAccountListChangedCallback(int nCorretora, int nChanged)
    {
        int Count;
        Count = ProfitDLL.GetAccountCountByBroker(nCorretora);
        WriteSync($"BrokerAccountListChangedCallback: Corretora: {nCorretora} - Contas: {Count}");
    }

    public static void BrokerSubAccountListChangedCallback(TConnectorAccountIdentifier accountId)
    {
        int Count;
        Count = ProfitDLL.GetSubAccountCount(ref accountId);
        WriteSync($"BrokerSubAccountListChangedCallback: {accountId} - {Count}");
    }

    public static void PriceBookCallback(TAssetID assetId, int nAction, int nPosition, int Side, int nQtd, int nCount, double sPrice, IntPtr pArraySell, IntPtr pArrayBuy)
    {
        List<TGroupPrice> lstBook;

        if (pArraySell != IntPtr.Zero)
        {
            DescriptaPriceArray(pArraySell, m_lstPriceSell);
        }

        if (pArrayBuy != IntPtr.Zero)
        {
            DescriptaPriceArray(pArrayBuy, m_lstPriceBuy);
        }

        if (Side == 0)
            lstBook = m_lstPriceBuy;
        else
            lstBook = m_lstPriceSell;

        TGroupPrice newPrice = new TGroupPrice(sPrice, nCount, nQtd);

        switch (nAction)
        {
            case 0:
                {
                    if (nPosition >= 0 && nPosition < lstBook.Count)
                        lstBook.Insert(lstBook.Count - nPosition, newPrice);
                }
                break;
            case 1:
                {
                    if (nPosition >= 0 && nPosition < lstBook.Count)
                    {
                        TGroupPrice currentPrice = lstBook[lstBook.Count - 1 - nPosition];
                        newPrice.Qtd += currentPrice.Qtd;
                        newPrice.Count += currentPrice.Count;
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
                        DescriptaPriceArray(pArraySell, m_lstPriceSell);
                    }

                    if (pArrayBuy != IntPtr.Zero)
                    {
                        DescriptaPriceArray(pArrayBuy, m_lstPriceBuy);
                    }
                }
                break;
            default: break;
        }
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



            _csv.AddCsvBookEvent(new CSV.BookEvent(assetId.Ticker, Timestamp.ToString(), dataOfertaStamp.ToString(), nQtd, offer.Volume, sPrice, Side));
        }

        if (m_lstOfferBuy.Count > 10 && m_lstOfferSell.Count > 10)
        {
            //WriteSync($"Ticker: {assetId.Ticker} | Bolsa: {assetId.Bolsa} |  quant: {nQtd} | Side: {Side} | Position: {nPosition} | Price: {sPrice} | nAgent: {nAgent} ");

            var lstBuy = m_lstOfferBuy[0..10];

            var lstSell = m_lstOfferSell[0..10];

            var Timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

            var dataOfertaStamp = new DateTimeOffset(date).ToUnixTimeSeconds();

            var topBook = new CSV.TopBook(assetId.Ticker, Timestamp.ToString(), dataOfertaStamp.ToString(),
                lstBuy[0].Qtd, lstBuy[0].Volume, lstBuy[0].Price, lstSell[0].Qtd, lstSell[0].Volume, lstSell[0].Price);

            _csv.AddCsvTopBookAsync(topBook);

            var topBook10 = new CSV.TopBook10(assetId.Ticker, Timestamp.ToString(), dataOfertaStamp.ToString(),
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

            _csv.AddCsvBookAsync(topBook10);

        }





    }

    private static void TradeCallback(TAssetID assetId, string date, uint tradeNumber, double price, double vol, int qtd, int buyAgent, int sellAgent, int tradeType, int bIsEdit)
    {
        if (!DateTime.TryParseExact(date, "dd/MM/yyyy HH:mm:ss.fff", null, DateTimeStyles.None, out DateTime dateTime))
        {
            dateTime = DateTime.MinValue;
        }

        _csv.AddCsvTradesAsync(new CSV.Trade(assetId.Ticker, dateTime, tradeNumber, price, vol, qtd, buyAgent, sellAgent, tradeType));

        WriteSync($"TradeCallback: {assetId.Ticker}: {assetId.Bolsa} | price {price}");
    }



    public static void HistoryTradeCallback(TConnectorAssetIdentifier a_Asset, nint a_pTrade, [MarshalAs(UnmanagedType.U4)] TConnectorTradeCallbackFlags a_nFlags)
    {
        var trade = new TConnectorTrade() { Version = 0 };

        if (ProfitDLL.TranslateTrade(a_pTrade, ref trade) == NL_OK)
        {
            WriteSync($"HistoryTradeCallback: {a_Asset}: {trade} | {a_nFlags}");
        }
    }

    public static void NewDailyCallback(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string date, double sOpen, double sHigh, double sLow,
        double sClose, double sVol, double sAjuste, double sMaxLimit, double sMinLimit, double sVolBuyer,
        double sVolSeller, int nQtd, int nNegocios, int nContratosOpen, int nQtdBuyer, int nQtdSeller, int nNegBuyer, int nNegSeller)
    {
        WriteSync($"NewDailyCallback: {assetId.Ticker}: {date} {sOpen} {sHigh} {sLow} {sClose}");
    }

    public static void ProgressCallBack(TAssetID assetId, int nProgress)
    {
        WriteSync("progressCallBack");
    }

    public static void NewTinyBookCallBack(TAssetID assetId, double price, int qtd, int side)
    {
        var sideName = side == 0 ? "buy" : "sell";
        WriteSync($"NewTinyBookCallBack: {assetId.Ticker}: {sideName} {price} {qtd}");
    }

    public static void TheoreticalPriceCallback(TAssetID assetId, double dTheoreticalPrice, Int64 nTheoreticalQtd)
    {
        WriteSync($"TheoreticalPriceCallback: {assetId.Ticker}: {dTheoreticalPrice}");
    }

    public static void AdjustHistoryCallbackV2(TAssetID assetId,
        double dValue,
        [MarshalAs(UnmanagedType.LPWStr)] string adjustType,
        [MarshalAs(UnmanagedType.LPWStr)] string strObserv,
        [MarshalAs(UnmanagedType.LPWStr)] string dtAjuste,
        [MarshalAs(UnmanagedType.LPWStr)] string dtDeliber,
        [MarshalAs(UnmanagedType.LPWStr)] string dtPagamento,
        int nFlags,
        double dMult)
    {
        WriteSync($"AdjustHistoryCallbackV2: {assetId.Ticker}: Value={dValue} Type={adjustType}");
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

    public static void OrderCallback(TConnectorOrderIdentifier orderId)
    {
        var order = new TConnectorOrderOut()
        {
            Version = 0,
            OrderID = orderId
        };

        if (ProfitDLL.GetOrderDetails(ref order) != NL_OK) { return; }

        order.AssetID.Ticker = new string(' ', order.AssetID.TickerLength);
        order.AssetID.Exchange = new string(' ', order.AssetID.ExchangeLength);
        order.TextMessage = new string(' ', order.TextMessageLength);

        if (ProfitDLL.GetOrderDetails(ref order) != NL_OK) { return; }

        WriteSync($"OrderCallback: {order.AssetID.Ticker} | {order.TradedQuantity} | {order.OrderSide} | {order.Price} | {order.AccountID.AccountID} | {order.OrderID.ClOrderID} | {order.OrderStatus} | {order.TextMessage}");
    }

    private static void OrderHistoryCallback(TConnectorAccountIdentifier accountId)
    {
        var count = 0;

        lock (writeLock)
        {
            bool CountOrders([In] in TConnectorOrder a_Order, nint a_Param)
            {
                count++;

                return true;
            }
            var result = ProfitDLL.EnumerateAllOrders(ref accountId, 0, 0, CountOrders);

            if (result != NL_OK) { WriteSync($"{nameof(ProfitDLL.EnumerateAllOrders)}: {(NResult)result}"); }

            WriteSync($"OrderHistoryCallback: Total orders: {count}");
        }



    }

    public static void ServerClockPrint()
    {
        double serverClock = 0.0;
        int year = 0, month = 0, day = 0, hour = 0, min = 0, sec = 0, mili = 0;
        ProfitDLL.GetServerClock(ref serverClock, ref year, ref month, ref day, ref hour, ref min, ref sec, ref mili);
        WriteSync($"Server Clock: {hour}:{min}:{sec}.{mili}");
    }

    public static void DescriptaPriceArray(IntPtr pRetorno, List<TGroupPrice> lstPrice)
    {
        lstPrice.Clear();

        byte[] header = new byte[128];
        Marshal.Copy(pRetorno, header, 0, 128);

        var qtd = BitConverter.ToInt32(header, 0);
        var tam = BitConverter.ToInt32(header, 4);
        var pos = 8;

        byte[] pBuffer = new byte[tam];
        Marshal.Copy(pRetorno, pBuffer, 0, tam);

        WriteSync($"PriceBook: Qtd {qtd} Tam {tam}");

        for (int i = 0; i < qtd; i++)
        {
            var group = new TGroupPrice();

            group.Price = BitConverter.ToDouble(pBuffer, pos);
            pos += 8;

            group.Qtd = BitConverter.ToInt32(pBuffer, pos);
            pos += 4;

            group.Count = BitConverter.ToInt32(pBuffer, pos);
            pos += 4;

            //WriteSync($"Price {group.Price} Qtd {group.Qtd} Count {group.Count}");
            lstPrice.Add(group);
        }

        ProfitDLL.FreePointer(pRetorno, pos);
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

    public static string DoGetAccountsByBroker()
    {

        WriteSync("Informe o ID da corretora: ");
        string corretoraInput = Console.ReadLine();

        if (!Int32.TryParse(corretoraInput, out int corretoraId))
        {
            WriteSync("ID da corretora inválido.");
            return "Erro";
        }

        int count = ProfitDLL.GetAccountCountByBroker(corretoraId);

        TConnectorAccountIdentifierOut[] accounts = new TConnectorAccountIdentifierOut[count];

        int size = ProfitDLL.GetAccountsByBroker(corretoraId, 0, 0, count, accounts);

        for (int i = 0; i < size; i++)
        {
            Console.WriteLine($"DoGetAccountsByBroker: Corretora = {accounts[i].BrokerID}, Conta ID = {accounts[i].AccountID}");
        }

        return "Sucesso";
    }

    public static string DoGetAgentName()
    {

        WriteSync("Informe o ID do agente: ");
        string agentInput = Console.ReadLine();

        WriteSync("Informe a Flag: (0 - Normal, 1 - Abreviado): ");
        string flagInput = Console.ReadLine();

        if (!Int32.TryParse(agentInput, out int agentId))
        {
            WriteSync("ID do agente inválido.");
            return "Erro";
        }

        if (!Int32.TryParse(flagInput, out int shortFlag))
        {
            WriteSync("Flag inválida.");
            return "Erro";
        }

        int agentLength = ProfitDLL.GetAgentNameLength(agentId, shortFlag);

        StringBuilder AgentName = new StringBuilder(agentLength);

        int retVal = ProfitDLL.GetAgentName(agentLength, agentId, AgentName, shortFlag);

        if (retVal == NL_OK)
        {
            string result = AgentName.ToString();
            WriteSync("Resultado: " + result);

            return result;
        }
        else
        {
            WriteSync($"Erro no GetAgentName: {retVal}");
            return "Erro";
        }
    }

    public static void getPositionAssets()
    {

        bool EnumAssets([In] in TConnectorAssetIdentifier a_Asset, nint a_Param)
        {

            WriteSync("Asset: " + a_Asset.Ticker);
            WriteSync("Exchange: " + a_Asset.Exchange);
            WriteSync("FeedType: " + a_Asset.FeedType);

            return true;
        }

        var accountId = ReadAccountId();

        WriteSync("broker: " + accountId.BrokerID);
        WriteSync("id: " + accountId.AccountID);
        WriteSync("subid: " + accountId.SubAccountID);

        var ret = ProfitDLL.EnumerateAllPositionAssets(ref accountId, 0, 0, EnumAssets);

    }

    #endregion

    #region Exemplo de execucao

    static string strAssetListFilter = "";

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
    private static void SubscribeAsset()
    {
        //Selecionar ativo para callback

        string input;

        do
        {
            Console.Write("Insira o codigo do ativo e clique enter: ");
            input = Console.ReadLine().ToUpper();
        } while (!Regex.IsMatch(input, "[^:]+:[A-Za-z0-9]"));

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
    }


    private static void DoSubscribeOfferBook(List<string> tickers)
    {
        tickers.ForEach(input =>
        {
            var split = input.Split(':');

            var retVal = ProfitDLL.SubscribeOfferBook(split[0], split[1]);

            WriteResult(retVal);
        });
    }

    private static void DoSubscribeOfferBook()
    {
        //Selecionar ativo para callback

        string input;

        do
        {
            Console.Write("Insira o codigo do ativo e clique enter: ");
            input = Console.ReadLine().ToUpper();
        } while (!Regex.IsMatch(input, "[^:]+:[A-Za-z0-9]"));

        var split = input.Split(':');

        var retVal = ProfitDLL.SubscribeOfferBook(split[0], split[1]);

        WriteResult(retVal);
    }

    private static void UnsubscribeAsset()
    {
        //Selecionar ativo para callback

        string input;

        do
        {
            Console.Write("Insira o codigo do ativo e clique enter: ");
            input = Console.ReadLine().ToUpper();
        } while (!Regex.IsMatch(input, "[^:]+:[A-Za-z0-9]"));

        var split = input.Split(':');

        var retVal = ProfitDLL.UnsubscribeTicker(split[0], split[1]);

        if (retVal == NL_OK)
        {
            WriteSync("Subscribe com sucesso");
        }
        else
        {
            WriteSync($"Erro no subscribe: {retVal}");
        }
    }

    private static void RequestHistory()
    {
        string input;

        do
        {
            Console.Write("Insira o codigo do ativo e clique enter (ex. PETR4:B): ");
            input = Console.ReadLine().ToUpper();
        } while (!Regex.IsMatch(input, "[^:]+:[A-Za-z0-9]"));

        var split = input.Split(':');

        var retVal = ProfitDLL.GetHistoryTrades(split[0], split[1], DateTime.Now.AddDays(-1).ToString(dateFormat), DateTime.Now.ToString(dateFormat));

        if (retVal == NL_OK)
        {
            WriteSync("GetHistoryTrades com sucesso");
        }
        else
        {
            WriteSync($"Erro no GetHistoryTrades: {retVal}");
        }
    }

    public static void RequestOrder()
    {
        WriteSync("Informe um ClOrdId: ");
        var retVal = ProfitDLL.GetOrder(Console.ReadLine());

        if (retVal == NL_OK)
        {
            WriteSync("GetOrder com sucesso");
        }
        else
        {
            WriteSync($"Erro no GetOrder: {retVal}");
        }
    }

    private static void DoGetPosition()
    {
        var assetId = ReadAssetID();
        var accountId = ReadAccountId();

        string input;
        do
        {
            Console.Write("Tipo da posição (1 - day trade, 2 - consolidado): ");
            input = Console.ReadLine();
        } while (input != "1" && input == "2");

        var positionType = (TConnectorPositionType)byte.Parse(input);

        var position = new TConnectorTradingAccountPosition()
        {
            Version = 1,
            AssetID = assetId,
            AccountID = accountId,
            PositionType = positionType
        };

        var retVal = ProfitDLL.GetPositionV2(ref position);

        if (retVal == NL_OK)
        {
            WriteSync($"{position.OpenSide} | {position.OpenAveragePrice} | {position.OpenQuantity}");
            WriteSync($"{position.DailyAverageBuyPrice} | {position.DailyAverageSellPrice} | {position.DailyBuyQuantity} | {position.DailySellQuantity}");
        }
        else
        {
            WriteSync($"Erro no GetPositionV2: {retVal}");
        }
    }

    private static TConnectorAccountIdentifier ReadAccountId()
    {
        string input;

        do
        {
            Console.Write("Código do conta (ex 1171:12345:1): ");
            input = Console.ReadLine();
        } while (!Regex.IsMatch(input, @"\d+:\d+(:\d+)?"));

        var numbers = input.Split(':');

        var retVal = new TConnectorAccountIdentifier()
        {
            Version = 0,
            BrokerID = int.Parse(numbers[0]),
            AccountID = numbers[1],
            SubAccountID = ""
        };

        if (numbers.Length == 3)
        {
            retVal.SubAccountID = numbers[2];
        }

        return retVal;
    }

    private static TConnectorAssetIdentifier ReadAssetID()
    {
        string input;
        Match match;

        do
        {
            Console.Write("Código do ativo (ex PETR4:B): ");
            input = Console.ReadLine().ToUpper();

            match = Regex.Match(input, "([^:]+):([A-Za-z0-9])");
        } while (!match.Success);

        return new TConnectorAssetIdentifier()
        {
            Version = 0,
            Ticker = match.Groups[1].Value,
            Exchange = match.Groups[2].Value
        };
    }

    private static void DoZeroPosition()
    {
        string input;

        do
        {
            Console.Write("Código do ativo (ex PETR4:B): ");
            input = Console.ReadLine().ToUpper();
        } while (!Regex.IsMatch(input, "[^:]+:[A-Za-z0-9]"));

        var assetId = new TConnectorAssetIdentifier()
        {
            Version = 0,
            Ticker = input[..input.IndexOf(':')],
            Exchange = input[(input.IndexOf(':') + 1)..]
        };

        do
        {
            Console.Write("Código do conta (ex 1171:12345:1): ");
            input = Console.ReadLine();
        } while (!Regex.IsMatch(input, @"\d+:\d+(:\d+)?"));

        var numbers = input.Split(':');

        var accountId = new TConnectorAccountIdentifier()
        {
            Version = 0,
            BrokerID = int.Parse(numbers[0]),
            AccountID = numbers[1],
            SubAccountID = ""
        };

        if (numbers.Length == 3)
        {
            accountId.SubAccountID = numbers[2];
        }

        do
        {
            Console.Write("Tipo da posição (1 - day trade, 2 - consolidado): ");
            input = Console.ReadLine();
        } while (input != "1" && input == "2");

        var positionType = (TConnectorPositionType)byte.Parse(input);

        var zeroOrder = new TConnectorZeroPosition()
        {
            Version = 1,
            AssetID = assetId,
            AccountID = accountId,
            PositionType = positionType,
            Password = ReadPassword(),
            Price = -1
        };

        var retVal = ProfitDLL.SendZeroPositionV2(ref zeroOrder);

        if (retVal == NL_OK)
        {
            WriteSync($"Sucesso no SendZeroPositionV2: {retVal}");
        }
        else
        {
            WriteSync($"Erro no SendZeroPositionV2: {retVal}");
        }
    }

    private static void DoGetOrders()
    {
        var count = 0;

        bool EnumOrders([In] in TConnectorOrder a_Order, nint a_Param)
        {
            WriteSync($"{nameof(EnumOrders)}: {a_Order}");

            if (a_Order.Quantity == 100) { count++; }

            return true;
        }

        var accountId = ReadAccountId();

        var ret = ProfitDLL.EnumerateOrdersByInterval(ref accountId, 0, SystemTime.FromDateTime(DateTime.Now.AddHours(-1)), SystemTime.FromDateTime(DateTime.Now.AddMinutes(-1)), 0, EnumOrders);

        if (ret != NL_OK) { WriteSync($"{nameof(ProfitDLL.EnumerateOrdersByInterval)}: {(NResult)ret}"); }

        WriteSync($"{nameof(ProfitDLL.EnumerateOrdersByInterval)}: Orders with 100 quantity: {count}");
    }

    private static int StartDLL(string key, string user, string password)
    {
        int retVal;
        bool bRoteamento = true;
        if (bRoteamento)
        {
            retVal = ProfitDLL.DLLInitializeLogin(key, user, password, _stateCallback, null, null, _accountCallback, _TradeCallback, null/*_newDailyCallback*/, _priceBookCallback, _offerBookCallbackV2, null, null, null);// _newTinyBookCallBack);
        }
        else
        {
            retVal = ProfitDLL.DLLInitializeMarketLogin(key, user, password, _stateCallback, null, _newDailyCallback, _priceBookCallback, null, null, null, _newTinyBookCallBack);
        }

        if (retVal != NL_OK)
        {
            WriteSync($"Erro na inicialização: {retVal}");
        }
        else
        {

            //ProfitDLL.SetTradeCallbackV2(_TradeCallback);
            ProfitDLL.SetHistoryTradeCallbackV2(_HistoryTradeCallback);
            ProfitDLL.SetBrokerAccountListChangedCallback(_brokerAccountListCallback);
            ProfitDLL.SetBrokerSubAccountListChangedCallback(_brokerSubAccountListCallback);


            ProfitDLL.SetOrderCallback(_orderCallback);
            //ProfitDLL.SetOrderHistoryCallback(_orderHistoryCallback);
            ProfitDLL.SetOfferBookCallbackV2(_offerBookCallbackV2);
            ProfitDLL.SetAssetListInfoCallbackV2(_assetListInfoCallbackV2);
            ProfitDLL.SetAdjustHistoryCallbackV2(_adjustHistoryCallbackV2);
            ProfitDLL.SetAssetPositionListCallback(_assetPositionListCallback);
        }

        return retVal;
    }



    public static void Main(string[] args)
    {
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) // Set the base path for configuration files
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true) // Add appsettings.json
            .AddEnvironmentVariables() // Add environment variables
            .AddCommandLine(args); // Add command-line arguments

        IConfigurationRoot configuration = builder.Build();

        // Access configuration values
        string user = configuration["ProfitSettings:User"];
        string pass = configuration["ProfitSettings:Password"];
        string activationCode = configuration["ProfitSettings:ActivationCode"];
        var tickers = configuration.GetSection("ProfitSettings:Tickers").Get<List<string>>();
        Console.WriteLine($"activationCode: {activationCode}");

        //string defaultConnection = configuration.GetConnectionString("DefaultConnection");
        //Console.WriteLine($"Default Connection: {defaultConnection}");


        //Console.Write("Chave de ativação: ");
        //string key = Console.ReadLine();

        Console.Write($"Usuário: {user}");
        // Console.ReadLine();

        string password = pass; //ReadPassword();

        //if (StartDLL(key, user, password) != NL_OK)
        if (StartDLL(activationCode, user, password) != NL_OK)
        {
            return;
        }

        //subscribe Tickers
        SubscribeAssets(tickers);

        //subscribe Offer Tickers
        DoSubscribeOfferBook(tickers);

        var terminate = false;
        while (!terminate)
        {
            try
            {
                if (bMarketConnected && bAtivo)
                {
                    WriteSync("Comando: ");

                    var input = Console.ReadLine();
                    switch (input)
                    {
                        case "subscribe":
                            SubscribeAsset();
                            break;
                        case "unsubscribe":
                            UnsubscribeAsset();
                            break;
                        case "offerbook":
                            DoSubscribeOfferBook();
                            break;
                        case "request history":
                            RequestHistory();
                            break;
                        case "request order":
                            RequestOrder();
                            break;
                        case "get position":
                            DoGetPosition();
                            break;
                        case "zero position":
                            DoZeroPosition();
                            break;
                        case "get orders":
                            DoGetOrders();
                            break;
                        case "get accounts broker":
                            DoGetAccountsByBroker();
                            break;
                        case "get agent name":
                            WriteSync("Nome do agente: " + DoGetAgentName());
                            break;
                        case "get position asset":
                            getPositionAssets();
                            break;

                        case "exit":
                            terminate = true;
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteSync(ex.Message);
            }
        }

    }

    private static readonly object writeLock = new object();

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
    #endregion
}
