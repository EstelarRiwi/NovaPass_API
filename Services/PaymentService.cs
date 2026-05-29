using MercadoPago.Client.Preference;
using MercadoPago.Resource.Preference;
namespace NovaPass_API.Services;

public class PaymentService
{
    public async Task<string> CreateCheckoutPreferenceAsync(string ticketId, string eventTitle, decimal price,
        string userEmail)
    {
        var request = new PreferenceRequest
        {
            Items = new List<PreferenceItemRequest>
            {
                new PreferenceItemRequest
                {
                    Title = $"Boleta: {eventTitle}",
                    Quantity = 1,
                    CurrencyId = "COP",
                    UnitPrice = price,
                }
            },
            Payer = new PreferencePayerRequest
            {
                Email = userEmail,
            },
            ExternalReference = ticketId
        };

        var client = new PreferenceClient();
        
        Preference preference = await client.CreateAsync(request);

        return preference.Id;

    }
    
}
