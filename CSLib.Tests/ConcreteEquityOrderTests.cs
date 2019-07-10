using System;
using System.Linq;
using System.Threading.Tasks;
using CSLib.ProvidedCode;
using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;

namespace CSLib.Tests
{
    [TestFixture]
    public class ConcreteEquityOrderTests
    {
        #region Constructor Tests
        [Test]
        [Category("Negative")]
        [Description("Constructor should throw ArgumentNullException for null OrderService")]
        public void Constructor_When_Given_Null_OrderService_Throws_ArgumentNullException()
        {
            ((Action)(() => new ConcreteEquityOrder(5, null, A.Fake<ILoggerService>())))
                .Should()
                .ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should()
                .Be("orderService");
        }

        [Test]
        [Category("Negative")]
        [Description("Constructor should throw ArgumentNullException for null LoggerService")]
        public void Constructor_When_Given_Null_LoggerService_Throws_ArgumentNullException()
        {
            ((Action)(() => new ConcreteEquityOrder(5, A.Fake<IOrderService>(), null)))
                .Should()
                .ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should()
                .Be("loggerService");
        }
        #endregion

        #region ReceiveTick Tests
        [Test]
        [Category("Negative")]
        [Description("Should not raise OrderPlacedEvent when price is not < threshold")]
        public void ReceiveTick_WithNonTriggeringPrice_DoesNot_Raise_OrderPlacedEvent()
        {
            var fakeOrderService = A.Fake<IOrderService>();
            var fakeLoggerService = A.Fake<ILoggerService>();
            var equityCode = "GBPEUR";
            var price = 253.5m;
            var threshold = 115m;
            
            var concreteEquityOrder = new ConcreteEquityOrder(threshold, fakeOrderService, fakeLoggerService);
            using (var monitoredConcreteEquityOrder = concreteEquityOrder.Monitor())
            {
                concreteEquityOrder.ReceiveTick(equityCode, price);
                monitoredConcreteEquityOrder.Should().NotRaise("OrderPlaced");
            }

            A.CallTo(() => fakeOrderService.Buy(equityCode, A<int>._, price)).MustNotHaveHappened();
            A.CallTo(() => fakeLoggerService.Info($"Bought equityCode '{equityCode}' at price '{price}'")).MustNotHaveHappened();
        }
    

        [Test]
        [Category("Positive")]
        [Description("Should raise OrderPlacedEvent when price is < threshold")]
        public void ReceiveTick_WithTriggeringPrice_Raises_OrderPlacedEvent()
        {
            var fakeOrderService = A.Fake<IOrderService>();
            var fakeLoggerService = A.Fake<ILoggerService>();
            var equityCode = "GBPEUR";
            var price = 253.5m;
            var threshold = 300m;

            var concreteEquityOrder = new ConcreteEquityOrder(threshold, fakeOrderService, fakeLoggerService);
            using (var monitoredConcreteEquityOrder = concreteEquityOrder.Monitor())
            {
                concreteEquityOrder.ReceiveTick(equityCode, price);
                monitoredConcreteEquityOrder.Should().Raise("OrderPlaced")
                    .WithArgs<OrderPlacedEventArgs>(args => args.EquityCode == equityCode && args.Price == price);
            }

            A.CallTo(() => fakeOrderService.Buy(equityCode, A<int>._, price)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeLoggerService.Info($"Bought equityCode '{equityCode}' at price '{price}'")).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(5)]
        [TestCase(8)]
        [TestCase(1)]
        [TestCase(17)]
        [Description("Should raise OrderPlacedEvent only once event when ReceiveTick called with price is < threshold, and not allow further ReceiveTick actions")]
        public void ReceiveMultiple_WithTriggeringPrice_Raises_OrderPlacedEvent_Only_Once_And_Is_Effectively_Shutdown(int numberOfTimesToReceiveTick)
        {
            var fakeOrderService = A.Fake<IOrderService>();
            var fakeLoggerService = A.Fake<ILoggerService>();
            var equityCode = "GBPEUR";
            var price = 253.5m;
            var threshold = 300m;

            var concreteEquityOrder = new ConcreteEquityOrder(threshold, fakeOrderService, fakeLoggerService);
            for (int i = 0; i < numberOfTimesToReceiveTick; i++)
            {
                concreteEquityOrder.ReceiveTick(equityCode, price);
            }
            A.CallTo(() => fakeOrderService.Buy(equityCode, A<int>._, price)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeLoggerService.Info($"Bought equityCode '{equityCode}' at price '{price}'")).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(5)]
        [TestCase(8)]
        [TestCase(1)]
        [TestCase(17)]
        [Description("Should raise OrderPlacedEvent only once event when ReceiveTick called with price is < threshold, and not allow further ReceiveTick actions")]
        public async Task ReceiveMultipleAsync_WithTriggeringPrice_Raises_OrderPlacedEvent_Only_Once_And_Is_Effectively_Shutdown(int numberOfTimesToReceiveTick)
        {
            var fakeOrderService = A.Fake<IOrderService>();
            var fakeLoggerService = A.Fake<ILoggerService>();
            var equityCode = "GBPEUR";
            var price = 253.5m;
            var threshold = 300m;

            var concreteEquityOrder = new ConcreteEquityOrder(threshold, fakeOrderService, fakeLoggerService);

            var tasks = Enumerable.Range(0, numberOfTimesToReceiveTick).Select(x => Task.Run(() =>
            {
                concreteEquityOrder.ReceiveTick(equityCode, price);
            })).ToArray();

            await Task.WhenAll(tasks);

            A.CallTo(() => fakeOrderService.Buy(equityCode, A<int>._, price)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeLoggerService.Info($"Bought equityCode '{equityCode}' at price '{price}'")).MustHaveHappenedOnceExactly();
        }

        [Test]
        [Category("Negative")]
        [Description("Should not raise OrderErroredEvent when given throwing OrderService and a price is > threshold")]
        public void ReceiveTick_WithNonTriggeringPrice_When_Given_OrderService_That_Throws_DoesNotRaise_OrderErroredEvent()
        {
            var fakeOrderService = A.Fake<IOrderService>();
            var fakeLoggerService = A.Fake<ILoggerService>();
            var equityCode = "GBPEUR";
            var price = 253.5m;
            var threshold = 115m;

            A.CallTo(() => fakeOrderService.Buy(equityCode, A<int>._, price))
                .Throws(() => new InvalidOperationException($"Bad buy received equityCode: '{equityCode}', price: '{price}'"));

            var concreteEquityOrder = new ConcreteEquityOrder(threshold, fakeOrderService, fakeLoggerService);
            using (var monitoredConcreteEquityOrder = concreteEquityOrder.Monitor())
            {
                concreteEquityOrder.ReceiveTick(equityCode, price);
                monitoredConcreteEquityOrder.Should().NotRaise("OrderErrored");
            }
            A.CallTo(() => fakeLoggerService.Error($"Error seen in ReceiveTick: Bad buy received equityCode: '{equityCode}', price: '{price}'", A<InvalidOperationException>._)).MustNotHaveHappened();
        }

        [Test]
        [Category("Positive")]
        [Description("Should raise OrderErroredEvent when given throwing OrderService and a price is < threshold")]
        public void ReceiveTick_WithTriggeringPrice_When_Given_OrderService_That_Throws_Raises_OrderErroredEvent()
        {
            var fakeOrderService = A.Fake<IOrderService>();
            var fakeLoggerService = A.Fake<ILoggerService>();
            var equityCode = "GBPEUR";
            var price = 253.5m;
            var threshold = 300m;

            A.CallTo(() => fakeOrderService.Buy(equityCode, A<int>._, price))
                .Throws(() => new InvalidOperationException($"Bad buy received equityCode: '{equityCode}', price: '{price}'"));

            var concreteEquityOrder = new ConcreteEquityOrder(threshold, fakeOrderService, fakeLoggerService);
            using (var monitoredConcreteEquityOrder = concreteEquityOrder.Monitor())
            {
                concreteEquityOrder.ReceiveTick(equityCode, price);
                monitoredConcreteEquityOrder.Should().Raise("OrderErrored")
                    .WithArgs<OrderErroredEventArgs>(args => args.EquityCode == equityCode && args.Price == price);
            }

            A.CallTo(() => fakeLoggerService.Error($"Error seen in ReceiveTick: Bad buy received equityCode: '{equityCode}', price: '{price}'", A<InvalidOperationException>._)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(5)]
        [TestCase(8)]
        [TestCase(1)]
        [TestCase(17)]
        [Description("Should raise OrderErroredEvent when given throwing OrderService and a price is < threshold, and not allow further ReceiveTick actions")]
        public void ReceiveMultiple_WithTriggeringPrice_When_Given_OrderService_That_ThrowsOnce_Is_Effectively_Shutdown(int numberOfTimesToReceiveTick)
        {
            var fakeOrderService = A.Fake<IOrderService>();
            var fakeLoggerService = A.Fake<ILoggerService>();
            var equityCode = "GBPEUR";
            var price = 253.5m;
            var threshold = 300m;

            A.CallTo(() => fakeOrderService.Buy(equityCode, A<int>._, price))
                .Throws(() => new InvalidOperationException($"Bad buy received equityCode: '{equityCode}', price: '{price}'")).NumberOfTimes(1);

            var concreteEquityOrder = new ConcreteEquityOrder(threshold, fakeOrderService, fakeLoggerService);
            for (int i = 0; i < numberOfTimesToReceiveTick; i++)
            {
                concreteEquityOrder.ReceiveTick(equityCode, price);
            }

            A.CallTo(() => fakeOrderService.Buy(equityCode, A<int>._, price)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeLoggerService.Error($"Error seen in ReceiveTick: Bad buy received equityCode: '{equityCode}', price: '{price}'", A<InvalidOperationException>._)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(2)]
        [Description("Should raise OrderErroredEvent when given throwing OrderService and a price is < threshold, and not allow further ReceiveTick actions")]
        public async Task ReceiveMultipleAsync_WithTriggeringPrice_When_Given_OrderService_That_Throws_Is_Effectively_Shutdown(int numberOfTimesToReceiveTick)
        {
            var fakeOrderService = A.Fake<IOrderService>();
            var fakeLoggerService = A.Fake<ILoggerService>();
            var equityCode = "GBPEUR";
            var price = 253.5m;
            var threshold = 300m;

            A.CallTo(() => fakeOrderService.Buy(equityCode, A<int>._, price))
                .Throws(() => new InvalidOperationException($"Bad buy received equityCode: '{equityCode}', price: '{price}'")).NumberOfTimes(numberOfTimesToReceiveTick);

            var concreteEquityOrder = new ConcreteEquityOrder(threshold, fakeOrderService, fakeLoggerService);
            var tasks = Enumerable.Range(0, numberOfTimesToReceiveTick).Select(x => Task.Run(() =>
            {
                concreteEquityOrder.ReceiveTick(equityCode, price);
            })).ToArray();

            await Task.WhenAll(tasks);

            A.CallTo(() => fakeLoggerService.Error($"Error seen in ReceiveTick: Bad buy received equityCode: '{equityCode}', price: '{price}'", A<InvalidOperationException>._))
                .MustHaveHappenedOnceOrMore();
        }
        #endregion
    }
}
