using System;
using System.Collections.Generic;

namespace DiscountApp
{
    /// <summary>
    /// Точка входа для демонстрации работы расширенного OrderService.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Создаём список стратегий скидок
            var discountCalculators = new List<IDiscountCalculator>
            {
                new StandardDiscountCalculator(),
                new VIPDiscountCalculator(),
                new CouponDiscountCalculator()
            };

            // Композитный калькулятор, суммирующий скидки от всех стратегий
            var compositeCalculator = new CompositeDiscountCalculator(discountCalculators);
            var orderService = new OrderService(compositeCalculator);

            // Пример заказа для VIP-клиента с купоном
            var order = new Order
            {
                OrderId = 1,
                TotalAmount = 300m,
                CustomerType = CustomerType.VIP,
                CouponCode = "SAVE20"
            };

            var finalAmount = orderService.CalculateFinalAmount(order);
            Console.WriteLine($"Итоговая сумма заказа: {finalAmount}");
        }
    }
}
