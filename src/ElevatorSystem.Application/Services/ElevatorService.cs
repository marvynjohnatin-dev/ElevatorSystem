using ElevatorSystem.Application.DTOs;
using ElevatorSystem.Application.Interfaces;
using ElevatorSystem.Domain.Entities;
using ElevatorSystem.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ElevatorSystem.Application.Services;

public class ElevatorService : IElevatorService
{
    private readonly IElevatorRepository _repository;
    private readonly ILogger<ElevatorService> _logger;
    private readonly Random _random = new Random();
    private readonly int _totalFloors = 10;
    
    public ElevatorService(IElevatorRepository repository, ILogger<ElevatorService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<IEnumerable<ElevatorDto>> GetAllElevatorsAsync()
    {
        var elevators = await _repository.GetAllElevatorsAsync();
        return elevators.Select(e => ElevatorDto.FromDomain(e)).ToList();
    }
    
    public async Task<ElevatorDto?> GetElevatorByIdAsync(int id)
    {
        var elevator = await _repository.GetElevatorByIdAsync(id);
        return elevator != null ? ElevatorDto.FromDomain(elevator) : null;
    }
    
    public async Task<IEnumerable<FloorRequestDto>> GetAllFloorRequestsAsync()
    {
        var requests = await _repository.GetAllFloorRequestsAsync();
        return requests.Select(r => FloorRequestDto.FromDomain(r)).ToList();
    }
    
    // Passenger assignment
    public async Task CallElevatorAsync(int floor, string direction)
    {
        if (floor < 1 || floor > _totalFloors)
            throw new ArgumentException($"Invalid floor number: {floor}");
            
        if (direction != "up" && direction != "down")
            throw new ArgumentException($"Invalid direction: {direction}");
            
        _logger.LogInformation($"Processing elevator call: Floor {floor}, Direction {direction}");
        
        // Add to floor requests for UI feedback
        var elevatorDirection = direction == "up" ? ElevatorDirection.Up : ElevatorDirection.Down;
        var floorRequest = new FloorRequest(floor, elevatorDirection);
        await _repository.AddFloorRequestAsync(floorRequest);
        
        // Generate target floor for passenger
        int targetFloor = GenerateTargetFloor(floor, direction);
        
        // Create passenger
        var nextPassengerId = await _repository.GetNextPassengerIdAsync();
        var newPassenger = new Passenger(nextPassengerId, floor, targetFloor);
        await _repository.IncrementPassengerIdAsync();
        
        _logger.LogInformation($"Created passenger {nextPassengerId}: Floor {floor} → {targetFloor}");
        
        // Find the best elevator for this passenger
        var elevators = await _repository.GetAllElevatorsAsync();
        var bestElevator = FindBestElevatorForPassenger(elevators.ToList(), newPassenger, direction);
        
        if (bestElevator != null)
        {
            await AssignPassengerToElevator(bestElevator, newPassenger);
            _logger.LogInformation($"Assigned passenger {nextPassengerId} to Elevator {bestElevator.Id}");
        }
        else
        {
            _logger.LogWarning($"No suitable elevator found for passenger {nextPassengerId}");
        }
        
        // Clean up floor request after delay
        _ = Task.Delay(1000).ContinueWith(async _ => 
        {
            await _repository.RemoveFloorRequestAsync(floor, elevatorDirection);
        });
    }
    
    private Elevator? FindBestElevatorForPassenger(List<Elevator> elevators, Passenger passenger, string direction)
    {
        var candidates = new List<(Elevator elevator, int priority, int cost)>();
        
        foreach (var elevator in elevators)
        {
            if (elevator.Passengers >= 8) continue; // Skip full elevators
            
            var evaluation = EvaluateElevatorForPassenger(elevator, passenger, direction);
            if (evaluation.CanHandle)
            {
                candidates.Add((elevator, evaluation.Priority, evaluation.Cost));
            }
        }
        
        if (!candidates.Any())
        {
            _logger.LogWarning($"No elevators can handle passenger from floor {passenger.PickupFloor}");
            return null;
        }
        
        // Sort by priority (higher = better), then by cost (lower = better)
        var best = candidates.OrderByDescending(c => c.priority).ThenBy(c => c.cost).First();
        
        _logger.LogInformation($"Best elevator for floor {passenger.PickupFloor}: Elevator {best.elevator.Id} (Priority: {best.priority}, Cost: {best.cost})");
        
        return best.elevator;
    }
    
    private (bool CanHandle, int Priority, int Cost) EvaluateElevatorForPassenger(Elevator elevator, Passenger passenger, string direction)
    {
        int floor = passenger.PickupFloor;
        
        // Priority 1: Elevator already going in same direction and will pass the pickup floor
        if (elevator.Status == ElevatorStatus.Moving && 
            elevator.Direction.ToString().ToLower() == direction)
        {
            bool willPassFloor = direction == "up" ? 
                (elevator.CurrentFloor <= floor && (elevator.TargetFloor == null || elevator.TargetFloor >= floor)) :
                (elevator.CurrentFloor >= floor && (elevator.TargetFloor == null || elevator.TargetFloor <= floor));
                
            if (willPassFloor)
            {
                int cost = Math.Abs(elevator.CurrentFloor - floor);
                return (true, 100, cost); // Highest priority - already on route
            }
        }
        
        // Priority 2: Elevator is idle (no target)
        if (elevator.TargetFloor == null)
        {
            int cost = Math.Abs(elevator.CurrentFloor - floor);
            return (true, 90, cost); // High priority - available immediately
        }
        
        // Priority 3: Elevator going to same direction but different route
        if (elevator.Status != ElevatorStatus.Moving)
        {
            // Calculate detour cost
            int originalDistance = elevator.TargetFloor != null ? 
                Math.Abs(elevator.CurrentFloor - elevator.TargetFloor.Value) : 0;
            int detourDistance = Math.Abs(elevator.CurrentFloor - floor) +
                (elevator.TargetFloor != null ? Math.Abs(floor - elevator.TargetFloor.Value) : 0);
            int detourCost = Math.Max(0, detourDistance - originalDistance);
            
            // Only consider if detour is reasonable (less than 3 floors extra)
            if (detourCost <= 3)
            {
                return (true, 70, detourCost); // Medium priority - reasonable detour
            }
        }
        
        // Priority 4: Any elevator with capacity (last resort)
        if (elevator.Passengers < 6) // Only if not too crowded
        {
            int cost = Math.Abs(elevator.CurrentFloor - floor) + 10; // Add penalty
            return (true, 50, cost); // Low priority - any available elevator
        }
        
        return (false, 0, int.MaxValue); // Cannot handle
    }
    
    private async Task AssignPassengerToElevator(Elevator elevator, Passenger passenger)
    {
        // Add to pending passengers list
        elevator.AddPendingPassenger(passenger);
        
        // Update elevator target to optimize route
        var optimalTarget = CalculateOptimalNextTarget(elevator);
        elevator.SetTargetFloor(optimalTarget);
        
        _logger.LogInformation($"Elevator {elevator.Id} new target: Floor {optimalTarget} (picking up passenger {passenger.Id})");
        
        await _repository.UpdateElevatorAsync(elevator);
    }
    
    private int? CalculateOptimalNextTarget(Elevator elevator)
    {
        var allDestinations = new List<int>();
        
        // Add pending pickup floors
        allDestinations.AddRange(elevator.PendingPickupFloors);
        
        // Add passenger destination floors
        allDestinations.AddRange(elevator.PassengerList.Select(p => p.TargetFloor));
        
        if (!allDestinations.Any())
            return null;
        
        // Remove current floor
        allDestinations = allDestinations.Where(f => f != elevator.CurrentFloor).Distinct().ToList();
        
        if (!allDestinations.Any())
            return null;
        
        // Choose next target based on current direction and efficiency
        if (elevator.Direction == ElevatorDirection.Up || 
            (elevator.Direction == ElevatorDirection.Idle && allDestinations.Average() > elevator.CurrentFloor))
        {
            // Going up: visit lowest floor first
            return allDestinations.Where(f => f > elevator.CurrentFloor).DefaultIfEmpty(allDestinations.Min()).Min();
        }
        else
        {
            // Going down: visit highest floor first  
            return allDestinations.Where(f => f < elevator.CurrentFloor).DefaultIfEmpty(allDestinations.Max()).Max();
        }
    }
    
    // Movement logic
    public async Task MoveElevatorsAsync()
    {
        await Task.Delay(5000);
        var elevators = (await _repository.GetAllElevatorsAsync()).ToList();
        
        foreach (var elevator in elevators)
        {
            bool modified = false;
            
            // Check if elevator should stop at current floor
            if (ShouldStopAtCurrentFloor(elevator))
            {
                _logger.LogInformation($"Elevator {elevator.Id} stopping at floor {elevator.CurrentFloor}");
                
                // Handle passenger pickup
                var pickedUpPassengers = elevator.PickupPassengersAtCurrentFloor();
                if (pickedUpPassengers.Any())
                {
                    _logger.LogInformation($"⬆️ Elevator {elevator.Id} picked up {pickedUpPassengers.Count} passengers at floor {elevator.CurrentFloor}");
                }
                
                // Handle passenger dropoff
                var droppedOffCount = elevator.PassengerList.Count(p => p.TargetFloor == elevator.CurrentFloor);
                if (droppedOffCount > 0)
                {
                    elevator.RemovePassengersByTargetFloor(elevator.CurrentFloor);
                    _logger.LogInformation($"⬇️ Elevator {elevator.Id} dropped off {droppedOffCount} passengers at floor {elevator.CurrentFloor}");
                }
                
                // Recalculate next target
                var nextTarget = CalculateOptimalNextTarget(elevator);
                elevator.SetTargetFloor(nextTarget);
                
                elevator.ArriveAtTargetFloor(); // Open doors
                modified = true;
            }
            else if (elevator.TargetFloor == null || elevator.CurrentFloor == elevator.TargetFloor)
            {
                // No target or reached target - decide what to do next
                var nextTarget = CalculateOptimalNextTarget(elevator);
                elevator.SetTargetFloor(nextTarget);
                
                if (nextTarget == null)
                {
                    elevator.StopAtCurrentFloor();
                }
                else
                {
                    elevator.ArriveAtTargetFloor(); // Brief stop to recalculate
                }
                modified = true;
            }
            else
            {
                // Move towards target
                _logger.LogDebug($"Elevator {elevator.Id} moving from floor {elevator.CurrentFloor} to {elevator.TargetFloor}");
                elevator.MoveTowardsTarget();
                modified = true;
            }
            
            if (modified)
            {
                await _repository.UpdateElevatorAsync(elevator);
            }
        }
    }
    
    private bool ShouldStopAtCurrentFloor(Elevator elevator)
    {
        // Stop if passengers want to get off
        bool hasDropoff = elevator.PassengerList.Any(p => p.TargetFloor == elevator.CurrentFloor);
        
        // Stop if there are pending pickups at this floor
        bool hasPickup = elevator.PendingPickupFloors.Contains(elevator.CurrentFloor);
        
        return hasDropoff || hasPickup;
    }
    
    public async Task SendElevatorToFloorAsync(int elevatorId, int targetFloor)
    {
        if (targetFloor < 1 || targetFloor > _totalFloors)
            throw new ArgumentException($"Invalid floor number: {targetFloor}");
            
        var elevator = await _repository.GetElevatorByIdAsync(elevatorId);
        if (elevator == null)
            throw new ArgumentException($"Elevator with ID {elevatorId} not found");
            
        // Remove passengers going to this floor
        elevator.RemovePassengersByTargetFloor(targetFloor);
        
        // Set new target
        elevator.SetTargetFloor(targetFloor);
        
        _logger.LogInformation($"Manual override: Elevator {elevatorId} sent to floor {targetFloor}");
        
        await _repository.UpdateElevatorAsync(elevator);
    }
    
    public async Task GenerateRandomCallAsync()
    {
        int floor = _random.Next(1, _totalFloors + 1);
        List<string> availableDirections = new List<string>();
        
        if (floor < _totalFloors)
            availableDirections.Add("up");
        if (floor > 1)
            availableDirections.Add("down");
            
        string direction = availableDirections.Count == 1
            ? availableDirections[0]
            : availableDirections[_random.Next(availableDirections.Count)];
            
        await CallElevatorAsync(floor, direction);
    }
    
    private int GenerateTargetFloor(int pickupFloor, string direction)
    {
        int minTargetFloor = direction == "up" ? pickupFloor + 1 : 1;
        int maxTargetFloor = direction == "up" ? _totalFloors : pickupFloor - 1;
        
        if (minTargetFloor <= maxTargetFloor)
        {
            return minTargetFloor + _random.Next(maxTargetFloor - minTargetFloor + 1);
        }
        else
        {
            // Generate any random floor different from pickup
            int targetFloor;
            do
            {
                targetFloor = _random.Next(1, _totalFloors + 1);
            } while (targetFloor == pickupFloor);
            return targetFloor;
        }
    }
}