using ElevatorSystem.Domain.Entities;

namespace ElevatorSystem.Domain.Repositories;

/// <summary>
/// Interface for elevator repository
/// </summary>
public interface IElevatorRepository
{
    // Elevator operations
    Task<IEnumerable<Elevator>> GetAllElevatorsAsync();
    Task<Elevator?> GetElevatorByIdAsync(int id);
    Task UpdateElevatorAsync(Elevator elevator);
    
    // Passenger operations
    Task<int> GetNextPassengerIdAsync();
    Task IncrementPassengerIdAsync();
    
    // Floor request operations
    Task<IEnumerable<FloorRequest>> GetAllFloorRequestsAsync();
    Task AddFloorRequestAsync(FloorRequest request);
    Task RemoveFloorRequestAsync(int floor, ElevatorDirection direction);
}
