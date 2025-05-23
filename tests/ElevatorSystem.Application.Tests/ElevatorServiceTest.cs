using ElevatorSystem.Application.Services;
using ElevatorSystem.Domain.Entities;
using ElevatorSystem.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElevatorSystem.Application.Tests.Services;

public class ElevatorServiceTests
{
    private readonly Mock<IElevatorRepository> _mockRepository;
    private readonly Mock<ILogger<ElevatorService>> _mockLogger;
    private readonly ElevatorService _elevatorService;

    public ElevatorServiceTests()
    {
        _mockRepository = new Mock<IElevatorRepository>();
        _mockLogger = new Mock<ILogger<ElevatorService>>();
        _elevatorService = new ElevatorService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllElevatorsAsync_Should_Return_Elevator_DTOs()
    {
        // Arrange
        var elevators = new List<Elevator>
        {
            new Elevator(0, 1),
            new Elevator(1, 3),
            new Elevator(2, 5)
        };

        _mockRepository.Setup(r => r.GetAllElevatorsAsync())
            .ReturnsAsync(elevators);

        // Act
        var result = await _elevatorService.GetAllElevatorsAsync();

        // Assert
        var elevatorDtos = result.ToList();
        Assert.Equal(3, elevatorDtos.Count);

        Assert.Equal(0, elevatorDtos[0].Id);
        Assert.Equal(1, elevatorDtos[0].CurrentFloor);
        Assert.Equal("idle", elevatorDtos[0].Direction);
        Assert.Equal("stopped", elevatorDtos[0].Status);

        Assert.Equal(1, elevatorDtos[1].Id);
        Assert.Equal(3, elevatorDtos[1].CurrentFloor);
    }

    [Fact]
    public async Task GetElevatorByIdAsync_Should_Return_Correct_Elevator_DTO()
    {
        // Arrange
        var elevator = new Elevator(1, 5);
        elevator.SetTargetFloor(8);

        _mockRepository.Setup(r => r.GetElevatorByIdAsync(1))
            .ReturnsAsync(elevator);

        // Act
        var result = await _elevatorService.GetElevatorByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(5, result.CurrentFloor);
        Assert.Equal(8, result.TargetFloor);
    }

    [Fact]
    public async Task GetElevatorByIdAsync_Should_Return_Null_When_Not_Found()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetElevatorByIdAsync(999))
            .ReturnsAsync((Elevator?)null);

        // Act
        var result = await _elevatorService.GetElevatorByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CallElevatorAsync_Should_Throw_Exception_For_Invalid_Floor()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _elevatorService.CallElevatorAsync(0, "up")); // Floor 0 is invalid

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _elevatorService.CallElevatorAsync(11, "up")); // Floor 11 is invalid (max is 10)
    }

    [Fact]
    public async Task CallElevatorAsync_Should_Throw_Exception_For_Invalid_Direction()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _elevatorService.CallElevatorAsync(5, "invalid"));
    }

    [Fact]
    public async Task CallElevatorAsync_Should_Add_Floor_Request_And_Assign_Available_Elevator()
    {
        // Arrange
        var availableElevator = new Elevator(0, 1); // Available (no target)
        var busyElevator = new Elevator(1, 3);
        busyElevator.SetTargetFloor(7); // Busy

        var elevators = new List<Elevator> { availableElevator, busyElevator };

        _mockRepository.Setup(r => r.GetAllElevatorsAsync())
            .ReturnsAsync(elevators);
        _mockRepository.Setup(r => r.GetNextPassengerIdAsync())
            .ReturnsAsync(1);

        // Act
        await _elevatorService.CallElevatorAsync(5, "up");

        // Assert
        _mockRepository.Verify(r => r.AddFloorRequestAsync(
            It.Is<FloorRequest>(fr => fr.Floor == 5 && fr.Direction == ElevatorDirection.Up)),
            Times.Once);

        _mockRepository.Verify(r => r.UpdateElevatorAsync(
            It.Is<Elevator>(e => e.Id == 0 && e.TargetFloor == 5 && e.PendingPickup)),
            Times.Once);

        _mockRepository.Verify(r => r.IncrementPassengerIdAsync(), Times.Once);
    }

    [Fact]
    public async Task CallElevatorAsync_Should_Choose_Closest_Available_Elevator()
    {
        // Arrange
        var elevator1 = new Elevator(0, 1);  // Distance from floor 5: |1-5| = 4
        var elevator2 = new Elevator(1, 3);  // Distance from floor 5: |3-5| = 2 (closest)
        var elevator3 = new Elevator(2, 8);  // Distance from floor 5: |8-5| = 3

        var elevators = new List<Elevator> { elevator1, elevator2, elevator3 };

        _mockRepository.Setup(r => r.GetAllElevatorsAsync())
            .ReturnsAsync(elevators);
        _mockRepository.Setup(r => r.GetNextPassengerIdAsync())
            .ReturnsAsync(1);

        // Act
        await _elevatorService.CallElevatorAsync(5, "up");

        // Assert
        _mockRepository.Verify(r => r.UpdateElevatorAsync(
            It.Is<Elevator>(e => e.Id == 1)), // Elevator 1 should be chosen (closest)
            Times.Once);
    }

    [Fact]
    public async Task SendElevatorToFloorAsync_Should_Throw_Exception_For_Invalid_Floor()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _elevatorService.SendElevatorToFloorAsync(0, 0)); // Floor 0 is invalid

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _elevatorService.SendElevatorToFloorAsync(0, 11)); // Floor 11 is invalid
    }

    [Fact]
    public async Task SendElevatorToFloorAsync_Should_Throw_Exception_For_Invalid_Elevator()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetElevatorByIdAsync(999))
            .ReturnsAsync((Elevator?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _elevatorService.SendElevatorToFloorAsync(999, 5));
    }

    [Fact]
    public async Task SendElevatorToFloorAsync_Should_Set_Target_Floor_And_Update_Elevator()
    {
        // Arrange
        var elevator = new Elevator(0, 3);
        _mockRepository.Setup(r => r.GetElevatorByIdAsync(0))
            .ReturnsAsync(elevator);

        // Act
        await _elevatorService.SendElevatorToFloorAsync(0, 7);

        // Assert
        _mockRepository.Verify(r => r.UpdateElevatorAsync(
            It.Is<Elevator>(e => e.Id == 0 && e.TargetFloor == 7)),
            Times.Once);
    }

    [Fact]
    public async Task MoveElevatorsAsync_Should_Move_Elevators_Towards_Target()
    {
        // Arrange
        var elevator1 = new Elevator(0, 2);
        elevator1.SetTargetFloor(5); // Should move up

        var elevator2 = new Elevator(1, 8);
        elevator2.SetTargetFloor(6); // Should move down

        var elevator3 = new Elevator(2, 4); // No target, should not move

        var elevators = new List<Elevator> { elevator1, elevator2, elevator3 };

        _mockRepository.Setup(r => r.GetAllElevatorsAsync())
            .ReturnsAsync(elevators);

        // Act
        await _elevatorService.MoveElevatorsAsync();

        // Assert
        // Verify elevator1 moved from floor 2 to floor 3
        _mockRepository.Verify(r => r.UpdateElevatorAsync(
            It.Is<Elevator>(e => e.Id == 0 && e.CurrentFloor == 3 && e.Status == ElevatorStatus.Moving)),
            Times.Once);

        // Verify elevator2 moved from floor 8 to floor 7
        _mockRepository.Verify(r => r.UpdateElevatorAsync(
            It.Is<Elevator>(e => e.Id == 1 && e.CurrentFloor == 7 && e.Status == ElevatorStatus.Moving)),
            Times.Once);

        // Verify elevator3 stopped (no target)
        _mockRepository.Verify(r => r.UpdateElevatorAsync(
            It.Is<Elevator>(e => e.Id == 2 && e.CurrentFloor == 4 && e.Status == ElevatorStatus.Stopped)),
            Times.Once);
    }

    [Fact]
    public async Task MoveElevatorsAsync_Should_Handle_Passenger_Pickup()
    {
        // Arrange
        var passenger = new Passenger(1, 3, 7);
        var elevator = new Elevator(0, 3);
        elevator.SetTargetFloor(3);
        elevator.AddPassenger(passenger);

        var elevators = new List<Elevator> { elevator };

        _mockRepository.Setup(r => r.GetAllElevatorsAsync())
            .ReturnsAsync(elevators);

        // Act
        await _elevatorService.MoveElevatorsAsync();

        // Assert
        _mockRepository.Verify(r => r.UpdateElevatorAsync(
            It.Is<Elevator>(e =>
                e.Id == 0 &&
                e.Passengers == 1 &&
                e.PendingPickup == false &&
                e.TargetFloor == 7)), // Should now go to passenger's destination
            Times.Once);
    }

    [Fact]
    public async Task GenerateRandomCallAsync_Should_Call_Elevator_To_Random_Floor()
    {
        // Arrange
        var availableElevator = new Elevator(0, 1);
        var elevators = new List<Elevator> { availableElevator };

        _mockRepository.Setup(r => r.GetAllElevatorsAsync())
            .ReturnsAsync(elevators);
        _mockRepository.Setup(r => r.GetNextPassengerIdAsync())
            .ReturnsAsync(1);

        // Act
        await _elevatorService.GenerateRandomCallAsync();

        // Assert
        _mockRepository.Verify(r => r.AddFloorRequestAsync(It.IsAny<FloorRequest>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateElevatorAsync(It.IsAny<Elevator>()), Times.Once);
    }

    [Fact]
    public async Task GetAllFloorRequestsAsync_Should_Return_Floor_Request_DTOs()
    {
        // Arrange
        var floorRequests = new List<FloorRequest>
        {
            new FloorRequest(3, ElevatorDirection.Up),
            new FloorRequest(7, ElevatorDirection.Down),
            new FloorRequest(5, ElevatorDirection.Up)
        };

        _mockRepository.Setup(r => r.GetAllFloorRequestsAsync())
            .ReturnsAsync(floorRequests);

        // Act
        var result = await _elevatorService.GetAllFloorRequestsAsync();

        // Assert
        var requestDtos = result.ToList();
        Assert.Equal(3, requestDtos.Count);

        Assert.Contains(requestDtos, r => r.Floor == 3 && r.Direction == "up");
        Assert.Contains(requestDtos, r => r.Floor == 7 && r.Direction == "down");
        Assert.Contains(requestDtos, r => r.Floor == 5 && r.Direction == "up");
    }
}
