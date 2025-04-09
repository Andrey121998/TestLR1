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
            // ������� ������������� ������ ��� ������
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
            Assert.AreEqual(0m, discount, "����������� ������ ������ ���� 0, ���� ����� ������ ������ ��� ����� 100");
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
            Assert.AreEqual(expectedDiscount, discount, "����������� ������ ������ ���������� 10% �� ����� ������");
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
            Assert.AreEqual(0m, discount, "��� �������� ������� VIP ������ ������ ���� 0");
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
            Assert.AreEqual(expectedDiscount, discount, "��� VIP ������� ������ ������ ���������� 5% �� ����� ������");
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
            Assert.AreEqual(0m, discount, "���� ����� �����������, ������ ������ ���� 0");
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
            Assert.AreEqual(0m, discount, "�������� ����� �� ������ ������ ������");
        }

        [Test]
        public void CouponDiscountCalculator_ReturnsTwentyPercentDiscount_ForValidCoupon_CaseInsensitive()
        {
            // Arrange
            _order.CouponCode = "save20"; // ���� �� �������� ��� ����� ��������
            var calculator = new CouponDiscountCalculator();
            decimal expectedDiscount = _order.TotalAmount * 0.20m;

            // Act
            decimal discount = calculator.CalculateDiscount(_order);

            // Assert
            Assert.AreEqual(expectedDiscount, discount, "��� ���������� ������ ������ ������ ���������� 20% �� ����� ������");
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

            // ���������� �������� ������������
            var calculators = new List<IDiscountCalculator>
            {
                new StandardDiscountCalculator(),   // 300 * 10% = 30
                new VIPDiscountCalculator(),          // 300 * 5%  = 15
                new CouponDiscountCalculator()        // 300 * 20% = 60
            };
            var composite = new CompositeDiscountCalculator(calculators);
            // ��������� ��������� ������ = 30 + 15 + 60 = 105, �� ����������� 50% �� 300 = 150, �.�. ������ �� ��������������.
            decimal expectedDiscount = 105m;

            // Act
            decimal discount = composite.CalculateDiscount(_order);

            // Assert
            Assert.AreEqual(expectedDiscount, discount, "����������� ����������� ������ ��������� ����������� ������, ���� ��� �� ��������� �����");
        }

        [Test]
        public void CompositeDiscountCalculator_LimitsTotalDiscount_WhenSumExceedsMaximum()
        {
            // Arrange
            _order.TotalAmount = 100m;
            _order.CustomerType = CustomerType.VIP;
            _order.CouponCode = "SAVE20";

            // �������� ������: 10 + 5 + 20 = 35, ������� ������������� ��� ���������� ������
            var extraCalculatorMock = new Mock<IDiscountCalculator>();
            extraCalculatorMock.Setup(c => c.CalculateDiscount(_order)).Returns(30m); // ������������� 30
            // �����: 10 + 5 + 20 + 30 = 65, �� ����� � 50% �� 100 = 50.
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
            Assert.AreEqual(50m, discount, "����������� ����������� ������ ������������ ��������� ������ ��������� 50% �� ����� ������");
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
            // ������ ������: 400 * 10% + 400 * 5% + 400 * 20% = 40 + 20 + 80 = 140, ����� ��� 400 = 200, ���� = 400 - 140 = 260.
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
            Assert.AreEqual(260m, finalAmount, "OrderService ������ ��������� ��������� �������� ����� � ������ ���� ������");
        }

        [Test]
        public void OrderService_CalculatesFinalAmount_UsingMockedDiscountCalculator()
        {
            // Arrange
            // ������������ ������������� Moq ��� �������� ������� ������
            var mockCalculator = new Mock<IDiscountCalculator>();
            mockCalculator.Setup(c => c.CalculateDiscount(_order)).Returns(50m);

            var service = new OrderService(mockCalculator.Object);

            // Act
            decimal finalAmount = service.CalculateFinalAmount(_order);

            // Assert
            Assert.AreEqual(_order.TotalAmount - 50m, finalAmount, "������ ������ ������������ ������, ������������ mock-��������");
        }

        #endregion
    }
}
