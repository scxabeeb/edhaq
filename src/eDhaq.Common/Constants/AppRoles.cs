namespace eDhaq.Common.Constants;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";
    public const string Cashier = "Cashier";
    public const string LaundryStaff = "LaundryStaff";
    public const string PickupDriver = "PickupDriver";
    public const string DeliveryDriver = "DeliveryDriver";
    public const string Customer = "Customer";

    public static readonly string[] All =
    [
        Administrator,
        Manager,
        Cashier,
        LaundryStaff,
        PickupDriver,
        DeliveryDriver,
        Customer
    ];
}
