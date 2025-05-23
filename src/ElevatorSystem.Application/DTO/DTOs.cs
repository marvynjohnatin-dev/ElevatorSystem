using ElevatorSystem.Domain.Entities;

namespace ElevatorSystem.Application.DTOs;

public class ElevatorDto
{
    public int Id { get; set; }
    public int CurrentFloor { get; set; }
    public int? TargetFloor { get; set; }
    public bool DoorsOpen { get; set; }
    public string Direction { get; set; } = "idle";
    public string Status { get; set; } = "stopped";
    public int Passengers { get; set; }
    public bool PendingPickup { get; set; }
    public PassengerDto? PendingPassenger { get; set; }
    public List<PassengerDto> PassengerList { get; set; } = new List<PassengerDto>();
    
    // Multi-passenger support
    public List<PassengerDto> PendingPassengers { get; set; } = new List<PassengerDto>();
    public List<int> PendingPickupFloors { get; set; } = new List<int>();
    
    // Timing information for 5-second actions
    public string? CurrentAction { get; set; }
    public DateTime? ActionStartTime { get; set; }
    public int? ActionRemainingSeconds { get; set; }
    public bool IsActionInProgress { get; set; }
    
    // Conversion methods
    public static ElevatorDto FromDomain(Elevator elevator)
    {
        var dto = new ElevatorDto
        {
            Id = elevator.Id,
            CurrentFloor = elevator.CurrentFloor,
            TargetFloor = elevator.TargetFloor,
            DoorsOpen = elevator.DoorsOpen,
            Direction = elevator.Direction.ToString().ToLower(),
            Status = elevator.Status.ToString().ToLower(),
            Passengers = elevator.Passengers,
            PendingPickup = elevator.PendingPickup,
            PendingPassenger = elevator.PendingPassenger != null 
                ? PassengerDto.FromDomain(elevator.PendingPassenger) 
                : null,
            PassengerList = elevator.PassengerList
                .Select(p => PassengerDto.FromDomain(p))
                .ToList(),
            PendingPassengers = elevator.PendingPassengers
                .Select(p => PassengerDto.FromDomain(p))
                .ToList(),
            PendingPickupFloors = new List<int>(elevator.PendingPickupFloors)
        };
        
        return dto;
    }
}

public class PassengerDto
{
    public int Id { get; set; }
    public int PickupFloor { get; set; }
    public int TargetFloor { get; set; }
    
    public static PassengerDto FromDomain(Passenger passenger)
    {
        return new PassengerDto
        {
            Id = passenger.Id,
            PickupFloor = passenger.PickupFloor,
            TargetFloor = passenger.TargetFloor
        };
    }
    
    public Passenger ToDomain()
    {
        return new Passenger(Id, PickupFloor, TargetFloor);
    }
}

public class FloorRequestDto
{
    public int Floor { get; set; }
    public string Direction { get; set; } = "up";
    
    public FloorRequest ToDomain()
    {
        return new FloorRequest(
            Floor, 
            Direction.ToLower() == "up" ? ElevatorDirection.Up : ElevatorDirection.Down
        );
    }
    
    public static FloorRequestDto FromDomain(FloorRequest request)
    {
        return new FloorRequestDto
        {
            Floor = request.Floor,
            Direction = request.Direction.ToString().ToLower()
        };
    }
}

public class CallElevatorRequest
{
    public int Floor { get; set; }
    public string Direction { get; set; } = "up";
}

public class SendElevatorRequest
{
    public int TargetFloor { get; set; }
}