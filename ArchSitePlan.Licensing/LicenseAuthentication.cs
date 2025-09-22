using Stripe;
using Subscription = Stripe.Subscription;

namespace ArchSitePlan.Licensing;

public static class LicenseAuthentication
{
    public record LicenseResult(bool Success, string Message);

    public static LicenseResult PeriodicValidation()
    {
        License? license = License.FromFile(Constants.LicensePath);

        if(license is null)
            return new LicenseResult(
                false,
                $"License not found");

        long previousCheckTime = license.LastActivation;

        // Time to validate?
        long timeNow = GetUnixUTCTime();

        bool previousCheckValid = timeNow > license.LastActivation && 
            timeNow < (license.LastActivation + previousCheckTime);

        if (!previousCheckValid)
            return ValidateAndUpdateStripeSubscription(
                license.CustomerID, 
                license.SubscriptionID);
        else
           return new LicenseResult(
                true, 
                $"Previous activation at {license.LastActivation} is still valid");
    }

    public static LicenseResult ValidateAndUpdateStripeSubscription(
        string customerId, 
        string subscriptionID)
    {
        var mCode = MachineCodeHelpers.GetHashedMacAddress();

        // Set your Stripe secret API key
        // Secret Key
        string apiKey = "x ";
        StripeConfiguration.ApiKey = apiKey;

        // Initialize the Stripe Subscription Service
        var subscriptionService = new SubscriptionService();

        // List all subscriptions for the customer
        var subscriptionOptions = new SubscriptionListOptions();
        subscriptionOptions.Customer = customerId;

        // Retrieve the customer's subscription details
        var subscriptions = subscriptionService.List(subscriptionOptions);

        // Check if any subscription exists for the customer
        string message;
        if (subscriptions.Data.Count == 0)
            return new LicenseResult(false, "Customer not found.");

        // Check if the intended subscription exists
        var subscription = subscriptions.Data.Find(sub => sub.Id.ToLower() == subscriptionID.ToLower());
        if (subscription == null)
            return new LicenseResult(false, "Invalid license key");

        // Check if it is active
        if (subscription.Status != "active" && subscription.Status != "trialing")
            return new LicenseResult(false, "Subscription is not active");

        long timeNow = GetUnixUTCTime();
        
        // Check for existence of machine code and time stamp metadata
        bool timeStampExists = subscription.Metadata.TryGetValue("LastActivation", out string timeStamp);
        bool machineCodeExists = subscription.Metadata.ContainsKey(Constants.MachineCode);
        string machineCode = string.Empty;

        // Verify that the seat is vacant
        if (timeStampExists && machineCodeExists)
        {
            // Check that this machine is valid for the license
            long.TryParse(timeStamp, out long lastActiveHrs);
            double minutesSinceLastActivation = ((double)lastActiveHrs - (double)timeNow) / 60;
            bool seatOccupied = MachineCodeIsSame(subscription) && minutesSinceLastActivation < 110;

            if(seatOccupied)
                return new LicenseResult(false, "Seat is occupied. Please sign out of the other system " +
                    "or wait for its license to go idle" +
                    "(in 2 hours)");
        }

        // Construct new metadata
        var updateOptions = new SubscriptionUpdateOptions()
        {
            Metadata = subscription.Metadata
        };

        updateOptions.AddOrEditSubscriptionMetadata(Constants.LastActivatedMetadata, timeNow.ToString());
        updateOptions.AddOrEditSubscriptionMetadata(Constants.MachineCode, machineCode);

        // Update Subscription
        subscriptionService.Update(subscriptionID, updateOptions);

        // Store this license locally
        var license = new License(customerId, subscriptionID, timeNow);
        license.WriteToFile(Constants.LicensePath);

        // SUCCESS!
        message = $"Status: {subscription.Status}\n" +
                        $"Current Period Start: {subscription.CurrentPeriodStart.Date.ToShortDateString()}\n" +
                        $"Current Period End: {subscription.CurrentPeriodEnd.Date.ToShortDateString()}" +
                        $"mCode";

        return new LicenseResult(true, message);
    }

    private static void AddOrEditSubscriptionMetadata(
        this SubscriptionUpdateOptions subscription, 
        string key, 
        string value)
    {
        if (subscription.Metadata.ContainsKey(key))
                subscription.Metadata[key] = value;
        else
            subscription.Metadata.Add(key, value);
    }

    private static bool MachineCodeIsSame(Subscription subscription)
    {
        return false;
        throw new NotImplementedException();
    }

    private static long GetUnixUTCTime()
    {
        // Unix timestamp (seconds since Jan 1, 1970)
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}

