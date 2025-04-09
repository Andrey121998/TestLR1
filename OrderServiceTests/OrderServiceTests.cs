using NUnit.Framework;
using DiscountApp;
using Moq;
using System.Collections.Generic;

namespace DiscountApp.Tests
{
    [TestFixture]
    public class ExtendedDiscountTests
    {
        private Order _order;

        [SetUp]
        public void Setup()
        {
            // Базовая инициализация заказа для тестов
            _order = new Order
            {
                OrderId = 1,
                TotalAmount = 200m,
                CustomerType = CustomerType.Standard,
                CouponCode = null
            };
        }

        #region StandardDiscountCalculator Tests

        [Test]
        public void StandardDiscountCalculator_ReturnsZero_WhenTotalAmountLessThanOrEqualTo100()
        {
            // Arrange
            _order.TotalAmount = 100m;
            var calculator = new StandardDiscountCalculator();

            // Act
            decimal discount = calculator.CalculateDiscount(_order);

            // Assert
            Assert.AreEqual(0m, discount, "Стандартная скидка должна быть 0, если сумма заказа меньше или равна 100");
        }

        [Test]
        public void StandardDiscountCalculator_ReturnsTenPercentDiscount_WhenTotalAmountGreaterThan100()
        {
            // Arrange
            _order.TotalAmount = 250m;
            var calculator = new StandardDiscountCalculator();
            decimal expectedDiscount = 250m * 0.10m;

            // Act
            decimal discount = calculator.CalculateDiscount(_order);

            // Assert
            Assert.AreEqual(expectedDiscount, discount, "Стандартная скидка должна составлять 10% от суммы заказа");
        }

        #endregion

        #region VIPDiscountCalculator Tests

        [Test]
        public void VIPDiscountCalculator_ReturnsZero_ForStandardCustomer()
        {
            // Arrange
            _order.CustomerType = CustomerType.Standard;
            var calculator = new VIPDiscountCalculator();

            // Act
            decimal discount = calculator.CalculateDiscount(_order);

            // Assert
            Assert.AreEqual(0m, discount, "Для обычного клиента VIP скидка должна быть 0");
        }

        [Test]
        public void VIPDiscountCalculator_ReturnsFivePercentDiscount_ForVIPCustomer()
        {
            // Arrange
            _order.CustomerType = CustomerType.VIP;
            var calculator = new VIPDiscountCalculator();
            decimal expectedDiscount = _order.TotalAmount * 0.05m;

            // Act
            decimal discount = calculator.CalculateDiscount(_order);

            // Assert
            Assert.AreEqual(expectedDiscount, discount, "Для VIP клиента скидка должна составлять 5% от суммы заказа");
        }

        #endregion

        #region CouponDiscountCalculator Tests

        [Test]
        public void CouponDiscountCalculator_ReturnsZero_WhenCouponCodeIsNullOrEmpty()
        {
            // Arrange
            _order.CouponCode = null;
            var calculator = new CouponDiscountCalculator();

            // Act
            decimal discount = calculator.CalculateDiscount(_order);

            // Assert
            Assert.AreEqual(0m, discount, "Если купон отсутствует, скидка должна быть 0");
        }

        [Test]
        public void CouponDiscountCalculator_ReturnsZero_WhenCouponCodeIsInvalid()
        {
            // Arrange
            _order.CouponCode = "INVALID";
            var calculator = new CouponDiscountCalculator();

            // Act
            decimal discount = calculator.CalculateDiscount(_order);

            // Assert
            Assert.AreEqual(0m, discount, "Неверный купон не должен давать скидку");
        }

        [Test]
        public void CouponDiscountCalculator_ReturnsTwentyPercentDiscount_ForValidCoupon_CaseInsensitive()
        {
            // Arrange
            _order.CouponCode = "save20"; // тест на проверку без учета регистра
            var calculator = new CouponDiscountCalculator();
            decimal expectedDiscount = _order.TotalAmount * 0.20m;

            // Act
            decimal discount = calculator.CalculateDiscount(_order);

            // Assert
            Assert.AreEqual(expectedDiscount, discount, "При корректном купоне скидка должна составлять 20% от суммы заказа");
        }

        #endregion

        #region CompositeDiscountCalculator Tests

        [Test]
        public void CompositeDiscountCalculator_SumsIndividualDiscounts()
        {
            // Arrange
            _order.TotalAmount = 300m;
            _order.CustomerType = CustomerType.VIP;
            _order.CouponCode = "SAVE20";

            // Используем реальные калькуляторы
            var calculators = new List<IDiscountCalculator>
            {
                new StandardDiscountCalculator(),   // 300 * 10% = 30
                new VIPDiscountCalculator(),          // 300 * 5%  = 15
                new CouponDiscountCalculator()        // 300 * 20% = 60
            };
            var composite = new CompositeDiscountCalculator(calculators);
            // Ожидаемая суммарная скидка = 30 + 15 + 60 = 105, но ограничение 50% от 300 = 150, т.е. скидка не ограничивается.
            decimal expectedDiscount = 105m;

            // Act
            decimal discount = composite.CalculateDiscount(_order);

            // Assert
            Assert.AreEqual(expectedDiscount, discount, "Композитный калькулятор должен корректно суммировать скидки, если они не превышают лимит");
        }

        [Test]
        public void CompositeDiscountCalculator_LimitsTotalDiscount_WhenSumExceedsMaximum()
        {
            // Arrange
            _order.TotalAmount = 100m;
            _order.CustomerType = CustomerType.VIP;
            _order.CouponCode = "SAVE20";

            // Реальные скидки: 10 + 5 + 20 = 35, добавим искусственную для превышения лимита
            var extraCalculatorMock = new Mock<IDiscountCalculator>();
            extraCalculatorMock.Setup(c => c.CalculateDiscount(_order)).Returns(30m); // дополнительно 30
            // Итого: 10 + 5 + 20 + 30 = 65, но лимит – 50% от 100 = 50.
            var calculators = new List<IDiscountCalculator>
            {
                new StandardDiscountCalculator(),
                new VIPDiscountCalculator(),
                new CouponDiscountCalculator(),
                extraCalculatorMock.Object
            };
            var composite = new CompositeDiscountCalculator(calculators);

            // Act
            decimal discount = composite.CalculateDiscount(_order);

            // Assert
            Assert.AreEqual(50m, discount, "Композитный калькулятор должен ограничивать суммарную скидку значением 50% от суммы заказа");
        }

        #endregion

        #region OrderService Integration Tests

        [Test]
        public void OrderService_CalculatesFinalAmount_WithMultipleDiscountStrategies()
        {
            // Arrange
            _order.TotalAmount = 400m;
            _order.CustomerType = CustomerType.VIP;
            _order.CouponCode = "SAVE20";
            // Расчёт скидок: 400 * 10% + 400 * 5% + 400 * 20% = 40 + 20 + 80 = 140, лимит для 400 = 200, итог = 400 - 140 = 260.
            var calculators = new List<IDiscountCalculator>
            {
                new StandardDiscountCalculator(),
                new VIPDiscountCalculator(),
                new CouponDiscountCalculator()
            };
            var composite = new CompositeDiscountCalculator(calculators);
            var service = new OrderService(composite);

            // Act
            decimal finalAmount = service.CalculateFinalAmount(_order);

            // Assert
            Assert.AreEqual(260m, finalAmount, "OrderService должен корректно вычислять итоговую сумму с учетом всех скидок");
        }

        [Test]
        public void OrderService_CalculatesFinalAmount_UsingMockedDiscountCalculator()
        {
            // Arrange
            // Демонстрация использования Moq для имитации расчета скидки
            var mockCalculator = new Mock<IDiscountCalculator>();
            mockCalculator.Setup(c => c.CalculateDiscount(_order)).Returns(50m);

            var service = new OrderService(mockCalculator.Object);

            // Act
            decimal finalAmount = service.CalculateFinalAmount(_order);

            // Assert
            Assert.AreEqual(_order.TotalAmount - 50m, finalAmount, "Сервис должен использовать скидку, возвращаемую mock-объектом");
        }

        #endregion
    }
}
