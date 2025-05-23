using ElevatorSystem.Api.Controllers;
using ElevatorSystem.Application.DTOs;
using ElevatorSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElevatorSystem.Api.Tests.Controllers;

public class ElevatorControllerTests
{
    private readonly Mock<IElevatorService> _mockElevatorService;
    private readonly Mock<ILogger<ElevatorController>> _mockLogger;
    private readonly ElevatorController _controller;

    public ElevatorControllerTests()
    {
        _mockElevatorService = new Mock<IElevatorService>();
        _mockLogger = new Mock<ILogger<ElevatorController>>();
        _controller = new ElevatorController(_mockElevatorService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetElevators_Should_Return_Ok_With_Elevators()
    {
        // Arrange
        var elevators = new List<ElevatorDto>
        {
            new ElevatorDto { Id = 0, CurrentFloor = 1, Status = "stopped" },
            new ElevatorDto { Id = 1, CurrentFloor = 3, Status = "moving" },
            new ElevatorDto { Id = 2, CurrentFloor = 5, Status = "loading" }
        };

        _mockElevatorService.Setup(s => s.GetAllElevatorsAsync())
            .ReturnsAsync(elevators);

        // Act
        var result = await _controller.GetElevators();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedElevators = Assert.IsType<List<ElevatorDto>>(okResult.Value);
        Assert.Equal(3, returnedElevators.Count);
        Assert.Equal(0, returnedElevators[0].Id);
        Assert.Equal(1, returnedElevators[0].CurrentFloor);
    }

    [Fact]
    public async Task GetElevator_Should_Return_Ok_When_Elevator_Exists()
    {
        // Arrange
        var elevator = new ElevatorDto 
        { 
            Id = 1, 
            CurrentFloor = 5, 
            TargetFloor = 8,
            Status = "moving",
            Direction = "up"
        };

        _mockElevatorService.Setup(s => s.GetElevatorByIdAsync(1))
            .ReturnsAsync(elevator);

        // Act
        var result = await _controller.GetElevator(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedElevator = Assert.IsType<ElevatorDto>(okResult.Value);
        Assert.Equal(1, returnedElevator.Id);
        Assert.Equal(5, returnedElevator.CurrentFloor);
        Assert.Equal(8, returnedElevator.TargetFloor);
    }

    [Fact]
    public async Task GetElevator_Should_Return_NotFound_When_Elevator_Does_Not_Exist()
    {
        // Arrange
        _mockElevatorService.Setup(s => s.GetElevatorByIdAsync(999))
            .ReturnsAsync((ElevatorDto?)null);

        // Act
        var result = await _controller.GetElevator(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetFloorRequests_Should_Return_Ok_With_Floor_Requests()
    {
        // Arrange
        var floorRequests = new List<FloorRequestDto>
        {
            new FloorRequestDto { Floor = 3, Direction = "up" },
            new FloorRequestDto { Floor = 7, Direction = "down" },
            new FloorRequestDto { Floor = 5, Direction = "up" }
        };

        _mockElevatorService.Setup(s => s.GetAllFloorRequestsAsync())
            .ReturnsAsync(floorRequests);

        // Act
        var result = await _controller.GetFloorRequests();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRequests = Assert.IsType<List<FloorRequestDto>>(okResult.Value);
        Assert.Equal(3, returnedRequests.Count);
        Assert.Contains(returnedRequests, r => r.Floor == 3 && r.Direction == "up");
    }

    [Fact]
    public async Task CallElevator_Should_Return_Ok_When_Request_Is_Valid()
    {
        // Arrange
        var request = new CallElevatorRequest
        {
            Floor = 5,
            Direction = "up"
        };

        _mockElevatorService.Setup(s => s.CallElevatorAsync(5, "up"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.CallElevator(request);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockElevatorService.Verify(s => s.CallElevatorAsync(5, "up"), Times.Once);
    }

    [Fact]
    public async Task CallElevator_Should_Return_BadRequest_When_Service_Throws_ArgumentException()
    {
        // Arrange
        var request = new CallElevatorRequest
        {
            Floor = 0, // Invalid floor
            Direction = "up"
        };

        _mockElevatorService.Setup(s => s.CallElevatorAsync(0, "up"))
            .ThrowsAsync(new ArgumentException("Invalid floor number: 0"));

        // Act
        var result = await _controller.CallElevator(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid floor number: 0", badRequestResult.Value);
    }

    [Fact]
    public async Task SendElevatorToFloor_Should_Return_Ok_When_Request_Is_Valid()
    {
        // Arrange
        var request = new SendElevatorRequest
        {
            TargetFloor = 7
        };

        _mockElevatorService.Setup(s => s.SendElevatorToFloorAsync(1, 7))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SendElevatorToFloor(1, request);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockElevatorService.Verify(s => s.SendElevatorToFloorAsync(1, 7), Times.Once);
    }

    [Fact]
    public async Task SendElevatorToFloor_Should_Return_BadRequest_When_Service_Throws_ArgumentException()
    {
        // Arrange
        var request = new SendElevatorRequest
        {
            TargetFloor = 11 // Invalid floor
        };

        _mockElevatorService.Setup(s => s.SendElevatorToFloorAsync(1, 11))
            .ThrowsAsync(new ArgumentException("Invalid floor number: 11"));

        // Act
        var result = await _controller.SendElevatorToFloor(1, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid floor number: 11", badRequestResult.Value);
    }

    [Fact]
    public async Task SendElevatorToFloor_Should_Return_BadRequest_When_Elevator_Not_Found()
    {
        // Arrange
        var request = new SendElevatorRequest
        {
            TargetFloor = 5
        };

        _mockElevatorService.Setup(s => s.SendElevatorToFloorAsync(999, 5))
            .ThrowsAsync(new ArgumentException("Elevator with ID 999 not found"));

        // Act
        var result = await _controller.SendElevatorToFloor(999, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Elevator with ID 999 not found", badRequestResult.Value);
    }

    [Fact]
    public async Task GenerateRandomCall_Should_Return_Ok()
    {
        // Arrange
        _mockElevatorService.Setup(s => s.GenerateRandomCallAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GenerateRandomCall();

        // Assert
        Assert.IsType<OkResult>(result);
        _mockElevatorService.Verify(s => s.GenerateRandomCallAsync(), Times.Once);
    }

    [Fact]
    public async Task CallElevator_Should_Log_Information()
    {
        // Arrange
        var request = new CallElevatorRequest
        {
            Floor = 5,
            Direction = "up"
        };

        _mockElevatorService.Setup(s => s.CallElevatorAsync(5, "up"))
            .Returns(Task.CompletedTask);

        // Act
        await _controller.CallElevator(request);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Call elevator to floor 5, direction up")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendElevatorToFloor_Should_Log_Information()
    {
        // Arrange
        var request = new SendElevatorRequest
        {
            TargetFloor = 7
        };

        _mockElevatorService.Setup(s => s.SendElevatorToFloorAsync(2, 7))
            .Returns(Task.CompletedTask);

        // Act
        await _controller.SendElevatorToFloor(2, request);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Send elevator 2 to floor 7")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
