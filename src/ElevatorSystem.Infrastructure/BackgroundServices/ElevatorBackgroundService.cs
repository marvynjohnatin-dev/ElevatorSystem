using ElevatorSystem.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ElevatorSystem.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that processes elevator actions every second
/// Actions take 5 seconds to complete, but we check every second for completion
/// </summary>
public class ElevatorBackgroundService : BackgroundService
{
    private readonly IElevatorService _elevatorService;
    private readonly ILogger<ElevatorBackgroundService> _logger;
    
    public ElevatorBackgroundService(
        IElevatorService elevatorService,
        ILogger<ElevatorBackgroundService> logger)
    {
        _elevatorService = elevatorService;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Elevator Background Service started - checking every 1 second, actions take 5 seconds");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Check elevators every second to see if any actions are complete
                await _elevatorService.MoveElevatorsAsync();
                
                // Wait 1 second before next check
                await Task.Delay(1000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in elevator background service");
            }
        }
        
        _logger.LogInformation("🛑 Elevator Background Service stopping");
    }
}