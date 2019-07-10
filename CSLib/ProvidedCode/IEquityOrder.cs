using System;
using System.IO;

// These set of classes/interfaces/event args were pre provided
namespace CSLib.ProvidedCode
{
    public interface IEquityOrder : IOrderPlaced, IOrderErrored
    {
        void ReceiveTick(string equityCode, decimal price);
    }

    public interface IOrderService
    {
        void Buy(string equityCode, int quantity, decimal price);
        void Sell(string equityCode, int quantity, decimal price);
    }

    public interface IOrderPlaced
    {
        event OrderPlacedEventHandler OrderPlaced;
    }

    public delegate void OrderPlacedEventHandler(OrderPlacedEventArgs e);

    public class OrderPlacedEventArgs
    {
        public OrderPlacedEventArgs(string equityCode, decimal price)
        {
            EquityCode = equityCode;
            Price = price;
        }

        public string EquityCode { get; }
        public decimal Price { get; }
    }

    public interface IOrderErrored
    {
        event OrderErroredEventHandler OrderErrored;
    }

    public delegate void OrderErroredEventHandler(OrderErroredEventArgs e);

    public class OrderErroredEventArgs : ErrorEventArgs
    {
        public OrderErroredEventArgs(string equityCode, decimal price, Exception ex) : base(ex)
        {
            EquityCode = equityCode;
            Price = price;
        }

        public string EquityCode { get; }
        public decimal Price { get; }
    }
}
