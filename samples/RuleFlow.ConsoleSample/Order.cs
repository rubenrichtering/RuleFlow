namespace RuleFlow.ConsoleSample;

public class Order
{
    public decimal Amount { get; set; }

    /// <summary>
    /// Example ceiling used by dynamic field-to-field conditions in samples.
    /// </summary>
    public decimal MaxOrderValue { get; set; } = 1000m;

    public string Country { get; set; } = "US";
    public bool IsValid { get; set; }
    public bool FreeShipping { get; set; }
    public bool PremiumShipping { get; set; }
    public bool StandardShipping { get; set; }
    public bool RequiresApproval { get; set; }
    public bool LogProcessed { get; set; }
    public Customer? Customer { get; set; }
}

public class Customer
{
    public string Name { get; set; } = "";
    public bool IsPremium { get; set; }
}
