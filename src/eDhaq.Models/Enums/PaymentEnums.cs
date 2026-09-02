namespace eDhaq.Models.Enums;

public enum PaymentMethod
{
    Cash = 1,
    EVCPlus,
    ZAAD,
    EDahab,
    Sahal,
    MyCash,
    Visa,
    MasterCard
}

public enum PaymentStatus
{
    Pending = 1,
    Paid,
    Failed,
    Refunded
}
