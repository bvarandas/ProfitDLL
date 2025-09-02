using System;
namespace CSV;

internal record Csv();

internal record Trade(string ticker, DateTime dateTime, uint tradeNumber, double price, double vol, int qtd, int buyAgent, int sellAgent, int tradeType)
    : Csv;

internal record TopBook10(string ticker, string dataHoraRecebeu, string dataHoraNegocio,
    long qtdeBid1, double volBid1, double priceBid1, long qtdeAsk1, double volAsk1, double priceAsk1,
    long qtdeBid2, double volBid2, double priceBid2, long qtdeAsk2, double volAsk2, double priceAsk2,
    long qtdeBid3, double volBid3, double priceBid3, long qtdeAsk3, double volAsk3, double priceAsk3,
    long qtdeBid4, double volBid4, double priceBid4, long qtdeAsk4, double volAsk4, double priceAsk4,
    long qtdeBid5, double volBid5, double priceBid5, long qtdeAsk5, double volAsk5, double priceAsk5,
    long qtdeBid6, double volBid6, double priceBid6, long qtdeAsk6, double volAsk6, double priceAsk6,
    long qtdeBid7, double volBid7, double priceBid7, long qtdeAsk7, double volAsk7, double priceAsk7,
    long qtdeBid8, double volBid8, double priceBid8, long qtdeAsk8, double volAsk8, double priceAsk8,
    long qtdeBid9, double volBid9, double priceBid9, long qtdeAsk9, double volAsk9, double priceAsk9,
    long qtdeBid10, double volBid10, double priceBid10, long qtdeAsk10, double volAsk10, double priceAsk10) : Csv;

internal record TopBook(string ticker, string dataHoraRecebeu, string dataHoraNegocio,
    long qtdeBid, double volBid, double priceBid, double qtdeAsk, double volAsk, double priceAsk) : Csv;


internal record BookEvent(string ticker, string dataHoraRecebeu, string dataHoraNegocio, long qtde, double vol, double price, int side) : Csv;