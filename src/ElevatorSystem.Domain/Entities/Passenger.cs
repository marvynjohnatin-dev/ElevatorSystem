namespace ElevatorSystem.Domain.Entities;

/// <summary>
/// Represents a passenger using the elevator system
/// </summary>
public class Passenger
{
    public int Id { get; private set; }
    public int PickupFloor { get; private set; }
    public int TargetFloor { get; private set; }
    
    public Passenger(int id, int pickupFloor, int targetFloor)
    {
        Id = id;
        PickupFloor = pickupFloor;
        TargetFloor = targetFloor;
    }
    
    public Passenger Clone()
    {
        return new Passenger(Id, PickupFloor, TargetFloor);
    }
}

/// <summary>
/// Represents a request to call an elevator to a specific floor
/// </summary>
public class FloorRequest
{
    public int Floor { get; private set; }
    public ElevatorDirection Direction { get; private set; }
    
    public FloorRequest(int floor, ElevatorDirection direction)
    {
        Floor = floor;
        Direction = direction;
    }
}
