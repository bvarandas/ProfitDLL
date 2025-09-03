using System;
namespace CSV;

internal interface ICsv;

internal struct Trade : ICsv
{
    public string Ticker { get; set; }
    public DateTime DateTime { get; set; }
    public uint TradeNumber { get; set; }
    public double Price { get; set; }
    public double Vol { get; set; }
    public int Qtd { get; set; }
    public int BuyAgent { get; set; }
    public int SellAgent { get; set; }
    public int TradeType { get; set; }

    public Trade(string ticker, DateTime dateTime, uint tradeNumber, double price, double vol, int qtd, int buyAgent, int sellAgent, int tradeType)
    {
        this.Ticker = ticker;
        this.DateTime = dateTime;
        this.TradeNumber = tradeNumber;
        this.Price = price;
        this.Vol = vol;
        this.Qtd = qtd;
        this.BuyAgent = buyAgent;
        this.SellAgent = sellAgent;
        this.TradeType = tradeType;
    }
}


internal struct TopBook10 : ICsv
{
    public string Ticker { get; set; }
    public string DataHoraRecebeu { get; set; }
    public string DataHoraNegocio { get; set; }
    public long QtdeBid1 { get; set; }
    public double VolBid1 { get; set; }
    public double PriceBid1 { get; set; }
    public long QtdeAsk1 { get; set; }
    public double VolAsk1 { get; set; }
    public double PriceAsk1 { get; set; }
    public long QtdeBid2 { get; set; }
    public double VolBid2 { get; set; }
    public double PriceBid2 { get; set; }
    public long QtdeAsk2 { get; set; }
    public double VolAsk2 { get; set; }
    public double PriceAsk2 { get; set; }
    public long QtdeBid3 { get; set; }
    public double VolBid3 { get; set; }
    public double PriceBid3 { get; set; }
    public long QtdeAsk3 { get; set; }
    public double VolAsk3 { get; set; }
    public double PriceAsk3 { get; set; }
    public long QtdeBid4 { get; set; }
    public double VolBid4 { get; set; }
    public double PriceBid4 { get; set; }
    public long QtdeAsk4 { get; set; }
    public double VolAsk4 { get; set; }
    public double PriceAsk4 { get; set; }
    public long QtdeBid5 { get; set; }
    public double VolBid5 { get; set; }
    public double PriceBid5 { get; set; }
    public long QtdeAsk5 { get; set; }
    public double VolAsk5 { get; set; }
    public double PriceAsk5 { get; set; }
    public long QtdeBid6 { get; set; }
    public double VolBid6 { get; set; }
    public double PriceBid6 { get; set; }
    public long QtdeAsk6 { get; set; }
    public double VolAsk6 { get; set; }
    public double PriceAsk6 { get; set; }
    public long QtdeBid7 { get; set; }
    public double VolBid7 { get; set; }
    public double PriceBid7 { get; set; }
    public long QtdeAsk7 { get; set; }
    public double VolAsk7 { get; set; }
    public double PriceAsk7 { get; set; }
    public long QtdeBid8 { get; set; }
    public double VolBid8 { get; set; }
    public double PriceBid8 { get; set; }
    public long QtdeAsk8 { get; set; }
    public double VolAsk8 { get; set; }
    public double PriceAsk8 { get; set; }
    public long QtdeBid9 { get; set; }
    public double VolBid9 { get; set; }
    public double PriceBid9 { get; set; }
    public long QtdeAsk9 { get; set; }
    public double VolAsk9 { get; set; }
    public double PriceAsk9 { get; set; }
    public long QtdeBid10 { get; set; }
    public double VolBid10 { get; set; }
    public double PriceBid10 { get; set; }
    public long QtdeAsk10 { get; set; }
    public double VolAsk10 { get; set; }
    public double PriceAsk10 { get; set; }

    public TopBook10(string ticker, string dataHoraRecebeu, string dataHoraNegocio,
    long qtdeBid1, double volBid1, double priceBid1, long qtdeAsk1, double volAsk1, double priceAsk1,
    long qtdeBid2, double volBid2, double priceBid2, long qtdeAsk2, double volAsk2, double priceAsk2,
    long qtdeBid3, double volBid3, double priceBid3, long qtdeAsk3, double volAsk3, double priceAsk3,
    long qtdeBid4, double volBid4, double priceBid4, long qtdeAsk4, double volAsk4, double priceAsk4,
    long qtdeBid5, double volBid5, double priceBid5, long qtdeAsk5, double volAsk5, double priceAsk5,
    long qtdeBid6, double volBid6, double priceBid6, long qtdeAsk6, double volAsk6, double priceAsk6,
    long qtdeBid7, double volBid7, double priceBid7, long qtdeAsk7, double volAsk7, double priceAsk7,
    long qtdeBid8, double volBid8, double priceBid8, long qtdeAsk8, double volAsk8, double priceAsk8,
    long qtdeBid9, double volBid9, double priceBid9, long qtdeAsk9, double volAsk9, double priceAsk9,
    long qtdeBid10, double volBid10, double priceBid10, long qtdeAsk10, double volAsk10, double priceAsk10)
    {
        this.Ticker = ticker;
        this.DataHoraRecebeu = dataHoraRecebeu;
        this.DataHoraNegocio = dataHoraNegocio;
        this.PriceBid1 = priceBid1;
        this.PriceAsk1 = priceAsk1;
        this.PriceAsk2 = priceAsk2;
        this.PriceBid2 = priceBid2;
        this.PriceAsk3 = priceAsk3;
        this.PriceAsk4 = priceAsk4;
        this.PriceBid4 = priceBid4;
        this.PriceAsk5 = priceAsk5;
        this.PriceBid5 = priceBid5;
        this.PriceAsk6 = priceAsk6;
        this.PriceBid6 = priceBid6;
        this.PriceAsk7 = priceAsk7;
        this.PriceBid7 = priceBid7;
        this.PriceAsk8 = priceAsk8;
        this.PriceBid8 = priceBid8;
        this.PriceAsk9 = priceAsk9;
        this.PriceBid9 = priceBid9;
        this.PriceAsk10 = priceAsk10;
        this.PriceBid10 = priceBid10;
        this.QtdeBid1 = qtdeBid1;
        this.QtdeAsk1 = qtdeAsk1;
        this.QtdeBid2 = qtdeBid2;
        this.QtdeAsk2 = qtdeAsk2;
        this.QtdeBid3 = qtdeBid3;
        this.QtdeAsk3 = qtdeAsk3;
        this.QtdeBid4 = qtdeBid4;
        this.QtdeAsk4 = qtdeAsk4;
        this.QtdeBid5 = qtdeBid5;
        this.QtdeAsk5 = qtdeAsk5;
        this.QtdeBid6 = qtdeBid6;
        this.QtdeAsk6 = qtdeAsk6;
        this.QtdeBid7 = qtdeBid7;
        this.QtdeAsk7 = qtdeAsk7;
        this.QtdeBid8 = qtdeBid8;
        this.QtdeAsk8 = qtdeAsk8;
        this.QtdeBid9 = qtdeBid9;
        this.QtdeAsk9 = qtdeAsk9;
        this.QtdeBid10 = qtdeBid10;
        this.QtdeAsk10 = qtdeAsk10;
        this.VolBid1 = volBid1;
        this.VolBid2 = volBid2;
        this.VolBid3 = volBid3;
        this.VolBid4 = volBid4;
        this.VolBid5 = volBid5;
        this.VolBid6 = volBid6;
        this.VolBid7 = volBid7;
        this.VolBid8 = volBid8;
        this.VolBid9 = volBid9;
        this.VolBid10 = volBid10;
        this.VolAsk1 = volAsk1;
        this.VolAsk2 = volAsk2;
        this.VolAsk3 = volAsk3;
        this.VolAsk4 = volAsk4;
        this.VolAsk5 = volAsk5;
        this.VolAsk6 = volAsk6;
        this.VolAsk7 = volAsk7;
        this.VolBid8 = volBid8;
        this.VolBid9 = volBid9;
        this.VolAsk10 = volAsk10;
    }

}

internal struct TopBook : ICsv
{
    public string Ticker { get; set; }
    public string DataHoraRecebeu { get; set; }
    public string DataHoraNegocio { get; set; }
    public long QtdeBid { get; set; }
    public double VolBid { get; set; }
    public double PriceBid { get; set; }
    public double QtdeAsk { get; set; }
    public double VolAsk { get; set; }
    public double PriceAsk { get; set; }
    public TopBook(string ticker, string dataHoraRecebeu, string dataHoraNegocio,
    long qtdeBid, double volBid, double priceBid, double qtdeAsk, double volAsk, double priceAsk)
    {
        this.Ticker = ticker;
        this.DataHoraRecebeu = dataHoraRecebeu;
        this.DataHoraNegocio = dataHoraNegocio;
        this.QtdeAsk = qtdeAsk;
        this.QtdeBid = qtdeBid;
        this.PriceBid = priceBid;
        this.PriceAsk = priceAsk;
        this.VolAsk = volAsk;
        this.VolBid = volBid;
        this.PriceAsk = priceAsk;
        this.PriceBid = priceBid;
    }
}
internal struct BookEvent : ICsv
{
    public string Ticker { get; set; }
    public string DataHoraRecebeu { get; set; }
    public string DataHoraNegocio { get; set; }
    public long Qtde { get; set; }
    public double Vol { get; set; }
    public double Price { get; set; }
    public int Size { get; set; }
    public BookEvent(string ticker, string dataHoraRecebeu, string dataHoraNegocio, long qtde, double vol, double price, int side)
    {
        this.Ticker = ticker;
        this.DataHoraRecebeu = dataHoraRecebeu;
        this.DataHoraNegocio = dataHoraNegocio;
        this.Qtde = qtde;
        this.Vol = vol;
        this.Price = price;
        this.Size = side;
    }
}