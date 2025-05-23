using ElevatorSystem.Application.DTOs;
using ElevatorSystem.Domain.Entities;

namespace ElevatorSystem.Application.Interfaces;

/// <summary>
/// Interface for the elevator service
/// </summary>
public interface IElevatorService
{
    // Elevator operations
    Task<IEnumerable<ElevatorDto>> GetAllElevatorsAsync();
    Task<ElevatorDto?> GetElevatorByIdAsync(int id);
    
    // Floor request operations
    Task<IEnumerable<FloorRequestDto>> GetAllFloorRequestsAsync();
    
    // User actions
    Task CallElevatorAsync(int floor, string direction);
    Task SendElevatorToFloorAsync(int elevatorId, int targetFloor);
    Task GenerateRandomCallAsync();
    
    // Internal method for elevator movement (used by background service)
    Task MoveElevatorsAsync();
}
