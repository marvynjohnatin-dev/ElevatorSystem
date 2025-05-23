using ElevatorSystem.Domain.Entities;
using ElevatorSystem.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ElevatorSystem.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of the elevator repository
/// </summary>
public class InMemoryElevatorRepository : IElevatorRepository
{
    private readonly List<Elevator> _elevators;
    private readonly List<FloorRequest> _floorRequests;
    private int _nextPassengerId = 1;
    private readonly object _lock = new object();
    private readonly ILogger<InMemoryElevatorRepository> _logger;
    
    public InMemoryElevatorRepository(ILogger<InMemoryElevatorRepository> logger)
    {
        _logger = logger;
        _elevators = new List<Elevator>();
        _floorRequests = new List<FloorRequest>();
        
        // Initialize elevators (usually would be 4)
        for (int i = 0; i < 4; i++)
        {
            _elevators.Add(new Elevator(i, 1));
        }
    }
    
    public Task<IEnumerable<Elevator>> GetAllElevatorsAsync()
    {
        lock (_lock)
        {
            // Return copies to prevent inadvertent modification
            var elevatorCopies = _elevators.Select(e => e.Clone()).ToList();
            return Task.FromResult(elevatorCopies.AsEnumerable());
        }
    }
    
    public Task<Elevator?> GetElevatorByIdAsync(int id)
    {
        lock (_lock)
        {
            var elevator = _elevators.FirstOrDefault(e => e.Id == id);
            return Task.FromResult(elevator?.Clone());
        }
    }
    
    public Task UpdateElevatorAsync(Elevator elevator)
    {
        lock (_lock)
        {
            int index = _elevators.FindIndex(e => e.Id == elevator.Id);
            if (index >= 0)
            {
                _elevators[index] = elevator;
            }
        }
        
        return Task.CompletedTask;
    }
    
    public Task<int> GetNextPassengerIdAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_nextPassengerId);
        }
    }
    
    public Task IncrementPassengerIdAsync()
    {
        lock (_lock)
        {
            _nextPassengerId++;
        }
        
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<FloorRequest>> GetAllFloorRequestsAsync()
    {
        lock (_lock)
        {
            // Return a copy to prevent enumeration issues
            var requestsCopy = _floorRequests.ToList();
            return Task.FromResult(requestsCopy.AsEnumerable());
        }
    }
    
    public Task AddFloorRequestAsync(FloorRequest request)
    {
        lock (_lock)
        {
            _floorRequests.Add(request);
        }
        
        return Task.CompletedTask;
    }
    
    public Task RemoveFloorRequestAsync(int floor, ElevatorDirection direction)
    {
        lock (_lock)
        {
            var request = _floorRequests.FirstOrDefault(r => 
                r.Floor == floor && r.Direction == direction);
                
            if (request != null)
            {
                _floorRequests.Remove(request);
            }
        }
        
        return Task.CompletedTask;
    }
}