namespace eDhaq.Models.Enums;

public enum OrderStatus
{
    OrderPlaced = 1,
    PickupScheduled,
    DriverAssigned,
    DriverOnTheWay,
    ClothesPickedUp,
    LaundryReceived,
    Sorting,
    Washing,
    DryCleaning,
    Drying,
    Ironing,
    Folding,
    Packaging,
    ReadyForDelivery,
    OutForDelivery,
    Delivered,
    Completed,
    Cancelled,
    CustomerConfirmed
}
