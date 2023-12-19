﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using LanguageExt;
using ProiectPSSC.Domain.Models;
using static ProiectPSSC.Domain.Models.OrderProducts;

using System.Data;

namespace ProiectPSSC.Domain
{
    public static class OrderProductsOperation
    {

        // validare date comanda: existenta email client, existenta cod produs, disponibilitate stoc pt cantitate produs 
        public static Task<IOrderProducts> ValidateOrder2(Func<ClientEmail, Option<ClientEmail>> checkClientExists, Func<ProductCode, Option<ProductCode>> checkProductExists,
                                                                Func<Quantity, Option<Quantity>> checkStocAvailable, UnvalidatedOrderProducts orderProducts) =>
            orderProducts.ProductList
                        .Select(ValidateOrderClients2(checkClientExists, checkStocAvailable, checkProductExists))
                        .Aggregate(CrateEmptyValidatedOrderProductsList().ToAsync(), ReduceValidProducts)
                        .MatchAsync(
                            Right: validatedOrderProducts => new ValidatedOrderProducts(validatedOrderProducts),
                            LeftAsync: errorMessage => Task.FromResult((IOrderProducts)new InvalidOrderProducts(orderProducts.ProductList, errorMessage))
                        );

        private static Func<UnvalidatedClientOrder, EitherAsync<string, ValidatedClientOrder>> ValidateOrderClients2(Func<ClientEmail, Option<ClientEmail>> checkClientExists,
                                                Func<Quantity, Option<Quantity>> checkStocAvailable, Func<ProductCode, Option<ProductCode>> checkProductExists) =>
           unvalidatedClientProducts => ValidateOrderClients2(checkClientExists, checkStocAvailable, checkProductExists, unvalidatedClientProducts);

        private static EitherAsync<string, ValidatedClientOrder> ValidateOrderClients2(Func<ClientEmail, Option<ClientEmail>> checkClientExists,
                        Func<Quantity, Option<Quantity>> checkStocAvailable, Func<ProductCode, Option<ProductCode>> checkProductExists, UnvalidatedClientOrder unvalidatedClientOrder) =>
            from productCode in ProductCode.TryParseProductCode(unvalidatedClientOrder.ProductCode)
                                            .ToEitherAsync($"Invalid product code ({unvalidatedClientOrder.ClientEmail}, {unvalidatedClientOrder.ProductCode})")
            from productExists in checkProductExists(productCode)
                     .ToEitherAsync($"Product {productCode.Value} does not exist.")

            from quantity in Quantity.TryParseQuantity(unvalidatedClientOrder.Quantity)
                                      .ToEitherAsync($"Invalid quantity ({unvalidatedClientOrder.ClientEmail}, {unvalidatedClientOrder.Quantity})")
            from stocAvailable in checkStocAvailable(quantity)
                                .ToEitherAsync($"Quantity for product {productCode.Value} is too much.")

            from clientEmail in ClientEmail.TryParseClientEmail(unvalidatedClientOrder.ClientEmail)
                                .ToEitherAsync($"Invalid client email ({unvalidatedClientOrder.ClientEmail})")

            from clientExists in checkClientExists(clientEmail)
                                 .ToEitherAsync($"Client {clientEmail.Value} does not exist.")
            select new ValidatedClientOrder(clientEmail, productCode, quantity);

        
        //creare lista order goala comanda nevalidata
        private static Either<string, List<ValidatedClientOrder>> CrateEmptyValidatedOrderProductsList() =>
            Right(new List<ValidatedClientOrder>());

        private static EitherAsync<string, List<ValidatedClientOrder>> ReduceValidProducts(EitherAsync<string, List<ValidatedClientOrder>> acc, EitherAsync<string, ValidatedClientOrder> next) =>
            from list in acc
            from nextProduct in next
            select list.AppendValidProduct(nextProduct);

        private static List<ValidatedClientOrder> AppendValidProduct(this List<ValidatedClientOrder> list, ValidatedClientOrder validProduct)
        {
            list.Add(validProduct);
            return list;
        }

        //calcularea pretului comenzii
        public static IOrderProducts CalculateFinalPrices(IOrderProducts orderProducts, IEnumerable<Products> catalog) => orderProducts.Match(
            whenUnvalidatedOrderProducts: unvalidatedClientOrder => unvalidatedClientOrder,
            whenInvalidOrderProducts: invalidatedClientOrder => invalidatedClientOrder,
            whenPlacedOrderProducts: placedOrder => placedOrder,
            whenCalculatedOrderProducts: calculatedOrderProducts => calculatedOrderProducts,
            whenValidatedOrderProducts: validated => CalculateProductFinalPrice2(validated, catalog)
            );

        //calculare pret pt fiecare produs din comanda
        private static IOrderProducts CalculateProductFinalPrice2(ValidatedOrderProducts validOrder, IEnumerable<Products> catalog)
        {
            var CalcOrderList = new List<CalculatedProductPrice>();
            foreach (ValidatedClientOrder product in validOrder.ProductList)
            {
                var calcOrder = CalculateFinalProductPrice2(product, catalog);
                CalcOrderList.Add(calcOrder);
            }
            return new CalculatedOrderProducts(CalcOrderList, CalcOrderList.First().clientEmail);
        }

        //calculare pret total produs = pret*cantitate
        private static CalculatedProductPrice CalculateFinalProductPrice2(ValidatedClientOrder validatedClientOrder, IEnumerable<Products> catalog)
        {

            var productPrice = catalog.Where(c => validatedClientOrder.productCode == c.code)
                                .Select(c => c.price);

            return new CalculatedProductPrice(validatedClientOrder.clientEmail, validatedClientOrder.productCode,
                 validatedClientOrder.quantity, productPrice.FirstOrDefault(), new ProductPrice(productPrice.FirstOrDefault().Price * validatedClientOrder.quantity.Value));
        }

        //actualizare lista
        public static IOrderProducts MergeProducts(IOrderProducts products, IEnumerable<CalculatedProductPrice> existingProducts) =>
            products.Match(
            whenUnvalidatedOrderProducts: unvalidatedClientOrder => unvalidatedClientOrder,
            whenInvalidOrderProducts: invalidatedClientOrder => invalidatedClientOrder,
            whenPlacedOrderProducts: placedOrder => placedOrder,
            whenValidatedOrderProducts: validatedOrder => validatedOrder,
            whenCalculatedOrderProducts: calculatedOrderProducts => MergeProducts(calculatedOrderProducts.ProductList, existingProducts)
                );

        private static CalculatedOrderProducts MergeProducts(IEnumerable<CalculatedProductPrice> newList, IEnumerable<CalculatedProductPrice> existingList)
        {
            var updatedAndNewProducts = newList.Select(product => product with { ClientId = existingList.FirstOrDefault(g => g.clientEmail == product.clientEmail)?.ClientId ?? 0, IsUpdated = true });
            var oldProducts = existingList.Where(product => !newList.Any(g => g.clientEmail == product.clientEmail));
            var allProducts = updatedAndNewProducts.Union(oldProducts)
                                               .ToList()
                                               .AsReadOnly();
            return new CalculatedOrderProducts(allProducts, newList.First().clientEmail);
        }

        public static IOrderProducts PlaceOrder(IOrderProducts products) => products.Match(
            whenUnvalidatedOrderProducts: unvalidatedClientOrder => unvalidatedClientOrder,
            whenInvalidOrderProducts: invalidatedClientOrder => invalidatedClientOrder,
            whenPlacedOrderProducts: placedOrder => placedOrder,
            whenValidatedOrderProducts: validatedOrder => validatedOrder,
            whenCalculatedOrderProducts:calculated =>  GenerateExport(calculated)
            );

        // generare expost si calculare pret total comanda 
        private static IOrderProducts GenerateExport(CalculatedOrderProducts calculatedOrder)
        {
            decimal totalPrice = 0;
            var lastOne = calculatedOrder.ProductList.LastOrDefault();
            
            foreach(CalculatedProductPrice product in calculatedOrder.ProductList)
            {
                //if (product.clientEmail == lastOne.clientEmail)
                    totalPrice = totalPrice + product.totalPrice.Price;
            }
           return new PlacedOrderProducts(calculatedOrder.ProductList, new ProductPrice(totalPrice),
                calculatedOrder.ProductList.Aggregate(new StringBuilder(), CreateCsvLine).ToString(),
                                    DateTime.Now, calculatedOrder.clientEmail1);
        }

        private static StringBuilder CreateCsvLine(StringBuilder export, CalculatedProductPrice product) =>
           export.AppendLine($"{product.code.Value}, {product.quantity.Value}, {product.ProductId}, {product.totalPrice.ToString}");

    }
}
