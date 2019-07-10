using System;
using System.Threading;
using CSLib.ProvidedCode;

namespace CSLib
{

    /// <summary>
    /// The <see cref="ConcreteEquityOrder"/> will receive all ticks (price updates for equities)
    /// from an external tick source via the ReceiveTick method
    /// When a (relevant) tick is received whose price is below a threshold level, the component should then:
    /// - Place a buy order via the IOrderService interface
    /// - Signal the Order Placed Event Handler
    /// - Shut down - ignoring all further ticks
    /// Any errors experienced should cause the component to signal the Order Errored Event Handler,
    /// and then shut down - ignoring all further ticks
    /// 
    /// Each instance of <see cref="ConcreteEquityOrder"/> should only ever place one order.
    /// 
    /// There may be several instances active simultaneously
    /// 
    /// </summary>
    public class ConcreteEquityOrder : IEquityOrder
    {
        private readonly decimal _thresholdLevel;
        private readonly IOrderService _orderService;
        private readonly ILoggerService _loggerService;
        private readonly object _syncLock = new object();
        private int _ticksInhibited = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcreteEquityOrder"/>
        /// with the specified parameters. 
        /// </summary>
        /// <param name="thresholdLevel">The threshold at which to place a buy order.</param>
        /// <param name="orderService">The <see cref="IOrderService"/> to use to place the buy order</param>
        /// <param name="loggerService">The <see cref="ILoggerService"/> to use for logging</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="orderService"/> is <see langword="null"/>.
        /// <para>- or -</para>
        /// <paramref name="loggerService"/> is <see langword="null"/>.
        /// </exception>
        public ConcreteEquityOrder(decimal thresholdLevel, IOrderService orderService, ILoggerService loggerService)
        {
            _thresholdLevel = thresholdLevel;
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
        }

        /// <inheritdoc/>
        public event OrderPlacedEventHandler OrderPlaced;

        /// <inheritdoc/>
        public event OrderErroredEventHandler OrderErrored;

        /// <inheritdoc/>
        public void ReceiveTick(string equityCode, decimal price)
        {
            if (IsInhibited())
            {
                return;
            }

            try
            {
                if (price < _thresholdLevel)
                {
                    lock (_syncLock)
                    {
                        if (IsInhibited())
                        {
                            return;
                        }
                        _orderService.Buy(equityCode, 1, price);
                        _loggerService.Info($"Bought equityCode '{equityCode}' at price '{price}'");
                        InhibitFurtherTicks();
                    }
                    OrderPlaced?.Invoke(new OrderPlacedEventArgs(equityCode, price));
                }

            }
            catch(Exception ex)
            {
                _loggerService.Error($"Error seen in ReceiveTick: {ex.Message}", ex);
                InhibitFurtherTicks();
                OrderErrored?.Invoke(new OrderErroredEventArgs(equityCode, price,ex));
            }
        }

        public bool IsInhibited()
        {
            lock (_syncLock)
            {
                return _ticksInhibited == 1;
            }
        }

        /// <summary>
        /// Sets the _ticksInhibited to 1 which indicated that no further ticks will be acted upon by
        /// the current <see cref="ConcreteEquityOrder"/> instance
        /// </summary>
        private void InhibitFurtherTicks()
        {
            Interlocked.Exchange(ref _ticksInhibited, 1);
        }

    }
}
