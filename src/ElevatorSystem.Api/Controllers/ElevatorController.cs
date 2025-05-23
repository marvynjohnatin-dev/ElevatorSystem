using Microsoft.AspNetCore.Mvc;
using ElevatorSystem.Application.Interfaces;
using ElevatorSystem.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace ElevatorSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ElevatorController : ControllerBase
{
    private readonly IElevatorService _elevatorService;
    private readonly ILogger<ElevatorController> _logger;
    
    public ElevatorController(IElevatorService elevatorService, ILogger<ElevatorController> logger)
    {
        _elevatorService = elevatorService;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ElevatorDto>>> GetElevators()
    {
        var elevators = await _elevatorService.GetAllElevatorsAsync();
        return Ok(elevators);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<ElevatorDto>> GetElevator(int id)
    {
        var elevator = await _elevatorService.GetElevatorByIdAsync(id);
        if (elevator == null)
            return NotFound();
            
        return Ok(elevator);
    }
    
    [HttpGet("floorRequests")]
    public async Task<ActionResult<IEnumerable<FloorRequestDto>>> GetFloorRequests()
    {
        var requests = await _elevatorService.GetAllFloorRequestsAsync();
        return Ok(requests);
    }
    
    [HttpPost("call")]
    public async Task<ActionResult> CallElevator([FromBody] CallElevatorRequest request)
    {
        _logger.LogInformation($"Call elevator to floor {request.Floor}, direction {request.Direction}");
        
        try
        {
            await _elevatorService.CallElevatorAsync(request.Floor, request.Direction);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost("{elevatorId}/send")]
    public async Task<ActionResult> SendElevatorToFloor(int elevatorId, [FromBody] SendElevatorRequest request)
    {
        _logger.LogInformation($"Send elevator {elevatorId} to floor {request.TargetFloor}");
        
        try
        {
            await _elevatorService.SendElevatorToFloorAsync(elevatorId, request.TargetFloor);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost("random-call")]
    public async Task<ActionResult> GenerateRandomCall()
    {
        await _elevatorService.GenerateRandomCallAsync();
        return Ok();
    }
}
