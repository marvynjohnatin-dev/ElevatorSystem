namespace ElevatorSystem.Domain.Entities;
public class Elevator
{
    public int Id { get; private set; }
    public int CurrentFloor { get; private set; }
    public int? TargetFloor { get; private set; }
    public bool DoorsOpen { get; private set; }
    public ElevatorDirection Direction { get; private set; }
    public ElevatorStatus Status { get; private set; }
    public int Passengers { get; private set; }
    public bool PendingPickup { get; private set; }
    public Passenger? PendingPassenger { get; private set; }
    public List<Passenger> PassengerList { get; private set; } = new List<Passenger>();
    
    // Multi-passenger support
    public List<Passenger> PendingPassengers { get; private set; } = new List<Passenger>();
    public List<int> PendingPickupFloors { get; private set; } = new List<int>();
    
    // Constructor for new elevators
    public Elevator(int id, int initialFloor = 1)
    {
        Id = id;
        CurrentFloor = initialFloor;
        TargetFloor = null;
        DoorsOpen = false;
        Direction = ElevatorDirection.Idle;
        Status = ElevatorStatus.Stopped;
        Passengers = 0;
        PendingPickup = false;
        PendingPassenger = null;
    }

    // Add pending passenger to pickup queue
    public void AddPendingPassenger(Passenger passenger)
    {
        if (!PendingPassengers.Any(p => p.Id == passenger.Id))
        {
            PendingPassengers.Add(passenger);
            
            if (!PendingPickupFloors.Contains(passenger.PickupFloor))
            {
                PendingPickupFloors.Add(passenger.PickupFloor);
                PendingPickupFloors.Sort(); // Keep sorted for efficient routing
            }
            
            PendingPickup = true;
            
            // Update the single pending passenger for backwards compatibility
            if (PendingPassenger == null)
            {
                PendingPassenger = passenger;
            }
        }
    }
    
    // Pick up all passengers at current floor
    public List<Passenger> PickupPassengersAtCurrentFloor()
    {
        var passengersToPickup = PendingPassengers
            .Where(p => p.PickupFloor == CurrentFloor)
            .ToList();
            
        foreach (var passenger in passengersToPickup)
        {
            if (Passengers < 8) // Don't exceed maximum capacity
            {
                PassengerList.Add(passenger);
                Passengers++;
                PendingPassengers.Remove(passenger);
            }
        }
        
        // Remove this floor from pending pickup floors if no more passengers
        if (!PendingPassengers.Any(p => p.PickupFloor == CurrentFloor))
        {
            PendingPickupFloors.Remove(CurrentFloor);
        }
        
        // Update pending pickup status
        PendingPickup = PendingPassengers.Any();
        PendingPassenger = PendingPassengers.FirstOrDefault();
        
        return passengersToPickup;
    }
    
    // Get all destinations this elevator needs to visit
    public List<int> GetAllDestinations()
    {
        var destinations = new List<int>();
        
        // Add pickup floors
        destinations.AddRange(PendingPickupFloors);
        
        // Add passenger destination floors
        destinations.AddRange(PassengerList.Select(p => p.TargetFloor));
        
        return destinations.Distinct().Where(f => f != CurrentFloor).OrderBy(f => f).ToList();
    }
    
    // Get next optimal destination based on direction
    public int? GetNextOptimalDestination()
    {
        var destinations = GetAllDestinations();
        
        if (!destinations.Any())
            return null;
            
        if (Direction == ElevatorDirection.Up)
        {
            // Going up: visit floors above current first, then closest below
            var floorsAbove = destinations.Where(f => f > CurrentFloor).ToList();
            if (floorsAbove.Any())
                return floorsAbove.Min(); // Closest floor above
            else
                return destinations.Min(); // If none above, go to lowest floor
        }
        else if (Direction == ElevatorDirection.Down)
        {
            // Going down: visit floors below current first, then closest above
            var floorsBelow = destinations.Where(f => f < CurrentFloor).ToList();
            if (floorsBelow.Any())
                return floorsBelow.Max(); // Closest floor below
            else
                return destinations.Max(); // If none below, go to highest floor
        }
        else
        {
            // Idle: go to closest floor
            return destinations.OrderBy(f => Math.Abs(f - CurrentFloor)).First();
        }
    }
    
    // Check if elevator should stop at current floor
    public bool ShouldStopAtCurrentFloor()
    {
        // Stop if passengers want to get off
        bool hasDropoff = PassengerList.Any(p => p.TargetFloor == CurrentFloor);
        
        // Stop if there are pending pickups at this floor
        bool hasPickup = PendingPickupFloors.Contains(CurrentFloor);
        
        return hasDropoff || hasPickup;
    }
    
    // Methods to modify elevator state
    public void SetTargetFloor(int? floor) => TargetFloor = floor;
    
    public void MoveTowardsTarget()
    {
        if (TargetFloor == null || CurrentFloor == TargetFloor)
            return;
            
        CurrentFloor += TargetFloor > CurrentFloor ? 1 : -1;
        Direction = TargetFloor > CurrentFloor ? ElevatorDirection.Up : ElevatorDirection.Down;
        Status = ElevatorStatus.Moving;
        DoorsOpen = false;
    }
    
    public void ArriveAtTargetFloor()
    {
        Status = ElevatorStatus.Loading;
        DoorsOpen = true;
        Direction = ElevatorDirection.Idle;
    }
    
    public void StopAtCurrentFloor()
    {
        Status = ElevatorStatus.Stopped;
        DoorsOpen = false;
        Direction = ElevatorDirection.Idle;
    }
    
    public void AddPassenger(Passenger passenger)
    {
        if (Passengers < 8)
        {
            PassengerList.Add(passenger);
            Passengers++;
        }
    }
    
    public void RemovePassenger(Passenger passenger)
    {
        var found = PassengerList.FirstOrDefault(p => p.Id == passenger.Id);
        if (found != null)
        {
            PassengerList.Remove(found);
            Passengers = Math.Max(0, Passengers - 1);
        }
    }
    
    public void RemovePassengersByTargetFloor(int floor)
    {
        var passengersToRemove = PassengerList.Where(p => p.TargetFloor == floor).ToList();
        foreach (var passenger in passengersToRemove)
        {
            PassengerList.Remove(passenger);
        }
        
        Passengers = Math.Max(0, Passengers - passengersToRemove.Count);
    }
    
    public void SetPendingPickup(bool isPending, Passenger? passenger = null)
    {
        PendingPickup = isPending;
        PendingPassenger = passenger;
        
        if (!isPending)
        {
            PendingPassengers.Clear();
            PendingPickupFloors.Clear();
        }
    }
    
    // Clone method for returning copies of the elevator
    public Elevator Clone()
    {
        var clone = new Elevator(Id, CurrentFloor)
        {
            TargetFloor = TargetFloor,
            DoorsOpen = DoorsOpen,
            Direction = Direction,
            Status = Status,
            Passengers = Passengers,
            PendingPickup = PendingPickup,
            PendingPassenger = PendingPassenger?.Clone(),
            PassengerList = PassengerList.Select(p => p.Clone()).ToList(),
            PendingPassengers = PendingPassengers.Select(p => p.Clone()).ToList(),
            PendingPickupFloors = new List<int>(PendingPickupFloors)
        };
        
        return clone;
    }
}

public enum ElevatorDirection
{
    Up,
    Down,
    Idle
}

public enum ElevatorStatus
{
    Moving,
    Stopped,
    Loading
}