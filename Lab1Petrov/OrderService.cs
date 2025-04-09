using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscountApp
{
    /// <summary>
    /// Перечисление типов клиентов.
    /// </summary>
    public enum CustomerType
    {
        Standard,
        VIP
    }

    /// <summary>
    /// Класс, представляющий заказ.
    /// </summary>
    public class Order
    {
        public int OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        // Дополнительные поля для усложнения логики скидок:
        public CustomerType CustomerType { get; set; } = CustomerType.Standard;
        public string CouponCode { get; set; }
    }

    /// <summary>
    /// Интерфейс для расчёта скидки.
    /// </summary>
    public interface IDiscountCalculator
    {
        /// <summary>
        /// Метод расчёта скидки для заданного заказа.
        /// </summary>
        /// <param name="order">Заказ, к которому применяется скидка</param>
        /// <returns>Сумма скидки</returns>
        decimal CalculateDiscount(Order order);
    }

    /// <summary>
    /// Стандартная реализация расчёта скидки:
    /// Если сумма заказа больше 100, скидка составляет 10%.
    /// </summary>
    public class StandardDiscountCalculator : IDiscountCalculator
    {
        public decimal CalculateDiscount(Order order)
        {
            if (order.TotalAmount > 100m)
                return order.TotalAmount * 0.10m;
            return 0m;
        }
    }

    /// <summary>
    /// Дополнительная скидка для VIP-клиентов: 5% от суммы заказа.
    /// </summary>
    public class VIPDiscountCalculator : IDiscountCalculator
    {
        public decimal CalculateDiscount(Order order)
        {
            if (order.CustomerType == CustomerType.VIP)
                return order.TotalAmount * 0.05m;
            return 0m;
        }
    }

    /// <summary>
    /// Скидка по купону: если купон равен "SAVE20", применяется скидка 20% от суммы заказа.
    /// </summary>
    public class CouponDiscountCalculator : IDiscountCalculator
    {
        private const string ValidCoupon = "SAVE20";
        private const decimal CouponDiscountRate = 0.20m;

        public decimal CalculateDiscount(Order order)
        {
            if (!string.IsNullOrEmpty(order.CouponCode) &&
                order.CouponCode.Equals(ValidCoupon, StringComparison.InvariantCultureIgnoreCase))
            {
                return order.TotalAmount * CouponDiscountRate;
            }
            return 0m;
        }
    }

    /// <summary>
    /// Композитный калькулятор скидок, суммирующий скидки от всех переданных стратегий.
    /// Итоговая скидка ограничена 50% от суммы заказа.
    /// </summary>
    public class CompositeDiscountCalculator : IDiscountCalculator
    {
        private readonly IEnumerable<IDiscountCalculator> _calculators;
        private const decimal MaximumDiscountRate = 0.50m;

        public CompositeDiscountCalculator(IEnumerable<IDiscountCalculator> calculators)
        {
            _calculators = calculators;
        }

        public decimal CalculateDiscount(Order order)
        {
            // Суммируем скидки от всех стратегий
            decimal totalDiscount = _calculators.Sum(calc => calc.CalculateDiscount(order));

            // Ограничиваем скидку 50% от суммы заказа
            decimal maxDiscount = order.TotalAmount * MaximumDiscountRate;
            return totalDiscount > maxDiscount ? maxDiscount : totalDiscount;
        }
    }

    /// <summary>
    /// Сервис для обработки заказа, рассчитывающий итоговую сумму с учётом скидки.
    /// </summary>
    public class OrderService
    {
        private readonly IDiscountCalculator _discountCalculator;

        public OrderService(IDiscountCalculator discountCalculator)
        {
            _discountCalculator = discountCalculator;
        }

        /// <summary>
        /// Вычисляет окончательную сумму заказа, вычитая рассчитанную скидку.
        /// </summary>
        /// <param name="order">Заказ</param>
        /// <returns>Итоговая сумма</returns>
        public decimal CalculateFinalAmount(Order order)
        {
            var discount = _discountCalculator.CalculateDiscount(order);
            return order.TotalAmount - discount;
        }
    }
}
