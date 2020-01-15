using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Conekta.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace Conekta.Integration.Tests
{
  /// <summary>
  /// 
  /// </summary>
  public class OrderTests
  {
    #region :: Private Fields ::

    /// <summary>
    /// Order Context.
    /// </summary>
    private readonly OrderContext _orderContext;

    /// <summary>
    /// Valid Order.
    /// </summary>
    private readonly OrderOperationData _validOrder = new OrderOperationData
    {
      Currency = "MXN",
      Metadata = new
      {
        test = true
      },
      LineItems = new List<LineItem>
        {
          new LineItem
          {
            Name = "Box of Cohiba S1s",
            Description = "Imported From Mex.",
            UnitPrice = 35000,
            Quantity = 1,
            Tags = new List<string>
            {
              "food",
              "mexican food"
            }
          }
        }
    };

    /// <summary>
    /// Custormer Info.
    /// </summary>
    private readonly CustomerInfo _customerInfo = new CustomerInfo
    {
      Name = "John Constantine",
      Phone = "+5213353319758",
      Email = "hola@hola.com"
    };

    /// <summary>
    /// Customer Transportation Info.
    /// </summary>
    private readonly CustomerInfo _customerTransportationInfo = new CustomerInfo
    {
      Name = "John Constantine",
      Phone = "+5213353319758",
      Email = "hola@hola.com",
      AntifraudInfo = new
      {
        account_created_at = 1484040996,
        first_paid_at = 1484040996,
        paid_transactions = 9,
        requires_receipt = true
      }
    };

    private readonly ChargeOperationData _validCharge = new ChargeOperationData
    {
      PaymentMethod = new PaymentMethod
      {
        Type = "card",
        TokenId = "tok_test_visa_4242"
      },
      Amount = 35000
    };

    #endregion

    #region :: Constructor ::

    /// <summary>
    /// Order Tests.
    /// </summary>
    public OrderTests()
    {
      IServiceCollection services = new ServiceCollection();

      services.AddHttpClient();

      IHttpClientFactory factory = services
        .BuildServiceProvider()
        .GetRequiredService<IHttpClientFactory>();

      ConektaInfo.SetHttpClientFactory(factory);

      _orderContext = new OrderContext();

      ConektaInfo.APIKey = "key_ZLy4aP2szht1HqzkCezDEA";
    }

    #endregion

    #region :: Methods ::

    /// <summary>
    /// Create OK.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task CreateAsync_OK_Test()
    {
      var orderCreated = await _orderContext.CreateAsync(_validOrder);

      orderCreated.LiveMode.Should().BeFalse();
      orderCreated.Amount.Should().Be(35000);
      orderCreated.Currency.Should().Be(_validOrder.Currency);

      var test = (bool)orderCreated.Metadata.test;

      Console.WriteLine(@$"CreateAsync_OK_Test    [->] { JsonConvert.SerializeObject(orderCreated,
        Formatting.Indented) }");

      test.Should().BeTrue();
    }

    /// <summary>
    /// Update Ok.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task UpdateAsync_OK_Test()
    {
      var orderCreated = await _orderContext.CreateAsync(_validOrder);

      var orderToUpdate = new OrderOperationData(orderCreated.Id)
      {
        Currency = "USD",
        CustomerInfo = orderCreated.CustomerInfo,
        LineItems = orderCreated.LineItemsList.Data
      };

      var orderUpdated = await _orderContext.UpdateAsync(orderToUpdate);

      Console.WriteLine(@$"UpdateAsync_OK_Test    [->] { JsonConvert.SerializeObject(orderUpdated,
        Formatting.Indented) }");

      orderUpdated.Currency.Should().Be(orderToUpdate.Currency);
    }

    /// <summary>
    /// Find Ok
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task FindAsync_OK_Test()
    {
      var orderCreated = await _orderContext.CreateAsync(_validOrder);

      var orderFound = await _orderContext.FindAsync(orderCreated.Id);

      orderFound.LiveMode.Should().BeFalse();
      orderFound.Amount.Should().Be(35000);
      orderFound.Currency.Should().Be(_validOrder.Currency);

      var test = (bool)orderFound.Metadata.test;

      Console.WriteLine(@$"FindAsync_OK_Test    [->] { JsonConvert.SerializeObject(orderFound,
        Formatting.Indented) }");

      test.Should().BeTrue();
    }

    /// <summary>
    /// Where Ok.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task WhereAsync_OK_Test()
    {
      var query = new Dictionary<string, string>
        {
          { "limit", "10" }
        };

      var ordersFound = await _orderContext.WhereAsync(query);

      //Console.WriteLine(@$"WhereAsync_OK_Test    [->] { JsonConvert.SerializeObject(ordersFound,
      //  Formatting.Indented) }");

      ordersFound.Data.Count.Should().Be(10);
      ordersFound.HasMore.Should().BeTrue();
      ordersFound.NextPageUrl.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Capture OK
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task CaptureAsync_OK_Test()
    {
      var orderToCapture = (OrderOperationData)_validOrder.Clone();

      orderToCapture.PreAuthorize = true;
      orderToCapture.CustomerInfo = _customerInfo;
      orderToCapture.Charges = new List<ChargeOperationData>
      {
        _validCharge
      };

      var orderCreated = await _orderContext.CreateAsync(orderToCapture);

      var orderCaptured = await _orderContext.CaptureAsync(orderCreated.Id);

      Console.WriteLine(@$"CaptureAsync_OK_Test    [->] { JsonConvert.SerializeObject(orderCaptured,
        Formatting.Indented) }");

      var charge = orderCaptured.ChargeList.Data.FirstOrDefault();

      charge.Status.Should().Be("paid");
    }

    /// <summary>
    /// Refund OK
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task CreateRefundAsync_OK_Test()
    {
      var orderToRefund = (OrderOperationData)_validOrder.Clone();

      orderToRefund.CustomerInfo = _customerInfo;
      orderToRefund.Charges = new List<ChargeOperationData>
      {
        _validCharge
      };

      var orderCreated = await _orderContext.CreateAsync(orderToRefund);

      var refundInfo = new RefundInfo
      {
        Amount = orderCreated.Amount,
        Reason = "requested_by_client"
      };

      var orderRefunded = await _orderContext.CreateRefundAsync(orderCreated.Id, refundInfo);

      Console.WriteLine(@$"CreateRefundAsync    [->] { JsonConvert.SerializeObject(orderRefunded,
        Formatting.Indented) }");

      orderRefunded.PaymentStatus.Should().Be("refunded");
    }

    /// <summary>
    /// Create charge Ok.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task CreateChargeAsync_OK_Test()
    {
      var order = (OrderOperationData)_validOrder.Clone();

      order.CustomerInfo = _customerInfo;

      var orderCreated = await _orderContext.CreateAsync(order);

      var charge = new ChargeOperationData
      {
        PaymentMethod = new PaymentMethod
        {
          Type = "banorte",
          ExpiresAt = new DateTimeOffset(DateTime.Now.AddDays(1)).ToUnixTimeSeconds()
        },
        Amount = 2300
      };

      var chargeCreated = await _orderContext.CreateChargeAsync(orderCreated.Id, charge);

      Console.WriteLine(@$"CreateChargeAsync_OK_Test    [->] { JsonConvert.SerializeObject(chargeCreated,
        Formatting.Indented, new JsonSerializerSettings
        {
          NullValueHandling = NullValueHandling.Ignore
        }) }");

      chargeCreated.Amount.Should().Be(charge.Amount);
      chargeCreated.PaymentMethod.Type.Should().Be(charge.PaymentMethod.Type);
      chargeCreated.PaymentMethod.ExpiresAt.Should().Be(charge.PaymentMethod.ExpiresAt);
    }

    /// <summary>
    /// Create LineItem Ok.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task CreateLineItemAsync_OK_Test()
    {
      var orderCreated = await _orderContext.CreateAsync(_validOrder);

      var lineItem = new LineItem
      {
        Name = "Box of Cohiba S1s",
        Description = "Imported From Mex.",
        UnitPrice = 20000,
        Quantity = 1,
        Tags = new List<string>
        {
          "food",
          "mexican food"
        }
      };

      var lineItemCreated = await _orderContext.CreateLineItemAsync(orderCreated.Id, lineItem);

      Console.WriteLine(@$"CreateLineItemAsync_OK_Test    [->] { JsonConvert.SerializeObject(lineItemCreated,
        Formatting.Indented, new JsonSerializerSettings
        {
          NullValueHandling = NullValueHandling.Ignore
        }) }");

      lineItemCreated.Name.Should().Be(lineItem.Name);
      lineItemCreated.Description.Should().Be(lineItem.Description);
      lineItemCreated.UnitPrice.Should().Be(lineItem.UnitPrice);
      lineItemCreated.Quantity.Should().Be(lineItem.Quantity);
    }

    /// <summary>
    /// Create taxline OK
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task CreateTaxLineAsync_OK_Test()
    {
      var orderCreated = await _orderContext.CreateAsync(_validOrder);

      var taxLineIVA = new TaxLine
      {
        Description = "IVA",
        Amount = 60
      };

      var taxLineCreated = await _orderContext.CreateTaxLineAsync(orderCreated.Id, taxLineIVA);

      Console.WriteLine(@$"CreateTaxLineAsync_OK_Test    [->] { JsonConvert.SerializeObject(taxLineCreated,
        Formatting.Indented, new JsonSerializerSettings
        {
          NullValueHandling = NullValueHandling.Ignore
        }) }");

      taxLineCreated.Description.Should().Be(taxLineIVA.Description);
      taxLineCreated.Amount.Should().Be(taxLineIVA.Amount);
    }

    /// <summary>
    /// Create ShippingLine OK.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task CreateShippingLineAsync_OK_Test()
    {
      var orderCreated = await _orderContext.CreateAsync(_validOrder);

      var shippingLine = new ShippingLine
      {
        Amount = 0,
        TrackingNumber = "TRACK123",
        Carrier = "USPS",
        Method = "Train",
        Metadata = new
        {
          some_random = "something"
        }
      };

      var shippingLineCreated = await _orderContext.CreateShippingLineAsync(orderCreated.Id, shippingLine);

      Console.WriteLine(@$"CreateShippingLineAsync_OK_Test    [->] { JsonConvert.SerializeObject(shippingLineCreated,
        Formatting.Indented, new JsonSerializerSettings
        {
          NullValueHandling = NullValueHandling.Ignore
        }) }");

      var someRandom = (string)shippingLineCreated.Metadata.some_random;

      shippingLineCreated.Amount.Should().Be(shippingLine.Amount);
      shippingLineCreated.TrackingNumber.Should().Be(shippingLine.TrackingNumber);
      shippingLineCreated.Carrier.Should().Be(shippingLine.Carrier);
      shippingLineCreated.Method.Should().Be(shippingLine.Method);

      someRandom.Should().Be((string)shippingLine.Metadata.some_random);
    }

    /// <summary>
    /// Creates discount line.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task CreateDiscountLineAsync_OK_Test()
    {
      var orderCreated = await _orderContext.CreateAsync(_validOrder);

      var discountLine = new DiscountLine
      {
        Code = "Cupón de descuento",
        Amount = 5,
        Type = "loyalty"
      };

      var discountLineCreated = await _orderContext.CreateDiscountLineAsync(orderCreated.Id, discountLine);

      Console.WriteLine(@$"CreateDiscountLineAsync_OK_Test    [->] { JsonConvert.SerializeObject(discountLineCreated,
        Formatting.Indented, new JsonSerializerSettings
        {
          NullValueHandling = NullValueHandling.Ignore
        }) }");
    }

    #endregion
  }
}
