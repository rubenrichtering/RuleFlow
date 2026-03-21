namespace RuleFlow.ConsoleSample;

public class Order
{
    public decimal Amount { get; set; }
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
