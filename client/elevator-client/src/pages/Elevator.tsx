import { useState, useEffect } from 'react';
import { 
  getElevators, 
  getFloorRequests, 
  callElevator, 
  sendElevatorToFloor, 
  generateRandomCall,
} from '../services/elevatorService';
import { Elevator as ElevatorType, FloorRequest } from '../types';
import { ArrowUp, ArrowDown } from '../components/Icons';

// Main App Component
export default function ElevatorSimulation() {
  const TOTAL_FLOORS = 10;
  const FLOORS = Array.from({ length: TOTAL_FLOORS }, (_, i) => TOTAL_FLOORS - i);
  
  // State
  const [elevators, setElevators] = useState<ElevatorType[]>([]);
  const [floorRequests, setFloorRequests] = useState<FloorRequest[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [connectionStatus, setConnectionStatus] = useState<'connecting' | 'connected' | 'disconnected'>('connecting');
  const [debugInfo, setDebugInfo] = useState<string>('');

  // fetch data
  const fetchData = async () => {
    try {      
      const elevatorsData = await getElevators();
      const requestsData = await getFloorRequests();
      
      console.log('Fetched elevators:', elevatorsData);
      console.log('Fetched floor requests:', requestsData);
      
      setElevators(elevatorsData);
      setFloorRequests(requestsData);
    } catch (err: any) {
      console.error('Error fetching data:', err);
      setError(err.message || 'Failed to connect to elevator API');
      setConnectionStatus('disconnected');
      setDebugInfo(`Error: ${err.message || 'Unknown error'}`);
    } 
  };
  
  // Call elevator to a specific floor
  const handleCallElevator = async (floor: number, direction: 'up' | 'down') => {
    try {
      console.log(`Calling elevator to floor ${floor}, direction ${direction}`);
      await callElevator(floor, direction);
      
      // Optimistically update UI to give immediate feedback
      setFloorRequests(prev => [...prev, { floor, direction }]);
      
      // But then refresh data from server to ensure consistency
      fetchData();
    } catch (err: any) {
      console.error('Error calling elevator:', err);
      setError(`Failed to call elevator: ${err.message}`);
    }
  };

  // Send elevator to a specific floor (from inside the elevator)
  const handleSendElevatorToFloor = async (elevatorId: number, floor: number) => {
    try {
      console.log(`🔄 Sending elevator ${elevatorId} to floor ${floor}`);
      await sendElevatorToFloor(elevatorId, floor);
      
      // Optimistically update UI
      setElevators(prev => 
        prev.map(e => e.id === elevatorId 
          ? { ...e, targetFloor: floor } 
          : e
        )
      );
      
      // But then refresh data from server
      fetchData();
    } catch (err: any) {
      console.error('Error sending elevator:', err);
      setError(`Failed to send elevator: ${err.message}`);
    }
  };
  
  // Generate a random elevator call
  const handleRandomCall = async () => {
    try {
      console.log('🔄 Generating random elevator call');
      await generateRandomCall();
      
      // Refresh data after call
      fetchData();
    } catch (err: any) {
      console.error('Error generating random call:', err);
      setError(`Failed to generate random call: ${err.message}`);
    }
  };

  // Helper function to get all target floors for an elevator
  const getAllTargetFloors = (elevator: ElevatorType): number[] => {
    const targets = new Set<number>();
    
    // Add main target floor
    if (elevator.targetFloor) {
      targets.add(elevator.targetFloor);
    }
    
    // Add passenger destinations
    if (elevator.passengerList) {
      elevator.passengerList.forEach(passenger => {
        targets.add(passenger.targetFloor);
      });
    }
    
    // Add pending pickup floors if available
    if (elevator.pendingPickupFloors) {
      elevator.pendingPickupFloors.forEach(floor => {
        targets.add(floor);
      });
    }
    
    return Array.from(targets).sort((a, b) => a - b);
  };

  // Helper function to get elevator's route description
  const getElevatorRoute = (elevator: ElevatorType): string => {
    const targets = getAllTargetFloors(elevator);
    
    if (targets.length === 0) {
      return 'No destinations';
    }
    
    if (targets.length === 1) {
      return `Going to floor ${targets[0]}`;
    }
    
    return `Route: ${targets.join(' → ')}`;
  };
  
  // Poll for updates every second
  useEffect(() => {
    // Initial fetch
    fetchData();
    
    // Set up polling
    const interval = setInterval(fetchData, 1000);
    
    // Clean up on unmount
    return () => clearInterval(interval);
  }, []);

  if (error) {
    return (
      <div className="error-container">
        <h2>Connection Error</h2>
        <p className="error-message">{error}</p>
        
        <div className="debug-section">
          <h3>Debug Information</h3>
          <div className="debug-info">
            <p><strong>Status:</strong> {connectionStatus}</p>
            <p><strong>Details:</strong> {debugInfo}</p>
          </div>
          
          <div className="troubleshooting">
            <h4>Troubleshooting Steps:</h4>
            <ol>
              <li>Make sure the .NET API is running: <code>dotnet run</code> in the API project</li>
              <li>Try accessing: <a href="https://localhost:7000/swagger" target="_blank" rel="noopener noreferrer">https://localhost:7000/swagger</a></li>
              <li>If using HTTPS, accept the security certificate warning</li>
              <li>Check browser console for CORS or certificate errors</li>
            </ol>
          </div>
        </div>
        
      </div>
    );
  }

  return (
    <div className="elevator-simulation">
      <h1 className="simulation-title">Elevator Simulation</h1>
      
      {/* Main Simulation Area */}
      <div className="simulation-area">
        {/* Building & Floors */}
        <div className="building">
          <div className="building-header">
            <h2 className="building-title">Building</h2>
          </div>
          <div className="floors-container">
            {FLOORS.map(floor => {
              // Find which elevators are targeting this floor
              const elevatorsTargetingFloor = elevators.filter(e => 
                getAllTargetFloors(e).includes(floor)
              );
              
              return (
                <div 
                  key={floor} 
                  className={`floor ${elevators.some(e => e.currentFloor === floor && e.status === 'moving') ? 'active' : ''}`}
                >
                  <span 
                    className={`floor-number ${elevators.some(e => e.currentFloor === floor && e.status === 'moving') ? 'active' : ''}`}
                  >
                    {floor}
                  </span>
                  
                  {/* Show which elevators are targeting this floor */}
                  {elevatorsTargetingFloor.length > 0 && (
                    <div className="floor-targets">
                      {elevatorsTargetingFloor.map(elevator => (
                        <span 
                          key={elevator.id} 
                          className={`target-indicator elevator-${elevator.id}`}
                          title={`Elevator ${elevator.id + 1} is heading to this floor`}
                        >
                          E{elevator.id + 1}
                        </span>
                      ))}
                    </div>
                  )}
                  
                  <div className="floor-buttons">
                    {floor < TOTAL_FLOORS && (
                      <button 
                        onClick={() => handleCallElevator(floor, 'up')}
                        className={`direction-button ${floorRequests.some(r => r.floor === floor && r.direction === 'up') ? 'active' : ''}`}
                        title={`Call elevator UP from floor ${floor}`}
                      >
                        <ArrowUp size={16} />
                      </button>
                    )}
                    {floor > 1 && (
                      <button 
                        onClick={() => handleCallElevator(floor, 'down')}
                        className={`direction-button ${floorRequests.some(r => r.floor === floor && r.direction === 'down') ? 'active' : ''}`}
                        title={`Call elevator DOWN from floor ${floor}`}
                      >
                        <ArrowDown size={16} />
                      </button>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
        
        {/* Elevators */}
        <div className="elevators-container">
          {
            elevators.map(elevator => {
              const allTargets = getAllTargetFloors(elevator);
              const routeDescription = getElevatorRoute(elevator);
              
              return (
                <div key={elevator.id} className="elevator">
                  <div className="elevator-header">
                    <h2 className="elevator-title">Elevator {elevator.id + 1}</h2>
                  </div>
                  
                  {/* Elevator status */}
                  <div className="elevator-status">
                    <div className="status-row">
                      <div className="elevator-info">
                        <p className={`floor-display ${elevator.status === 'moving' ? 'moving' : ''}`}>
                          Floor: {elevator.currentFloor}
                        </p>
                        <p className="status-text">
                          {elevator.status === 'moving' ? 
                            `Moving ${elevator.direction}` : 
                            elevator.status === 'loading' ? 
                              'Loading/Unloading' : 'Stopped'}
                        </p>
                      </div>
                      <div 
                        className={`status-indicator ${
                          elevator.status === 'moving' ? 'moving' : 
                          elevator.status === 'loading' ? 'loading' : ''
                        }`}
                      />
                    </div>
                    
                    {/* Target Floor Display */}
                    <div className="target-floor-section">
                      <div className="current-target">
                        <p className="detail-text">
                          <strong>Next Target:</strong> {elevator.targetFloor ? `Floor ${elevator.targetFloor}` : 'None'}
                        </p>
                      </div>
                      
                      {/* All Target Floors */}
                      {allTargets.length > 0 && (
                        <div className="all-targets">
                          <p className="targets-label">All Destinations:</p>
                          <div className="targets-list">
                            {allTargets.map((floor, index) => (
                              <span 
                                key={floor}
                                className={`target-floor ${floor === elevator.targetFloor ? 'next-target' : ''}`}
                              >
                                {floor}
                                {index < allTargets.length - 1 && <span className="arrow">→</span>}
                              </span>
                            ))}
                          </div>
                        </div>
                      )}
                      
                      {/* Route Description */}
                      <div className="route-description">
                        <p className="route-text">{routeDescription}</p>
                      </div>
                    </div>
                    
                    <div className="details-container">
                      <p className="detail-text">
                        Doors: {elevator.doorsOpen ? 'Open' : 'Closed'}
                      </p>
                    </div>
                    
                    {/* Passenger Visualization */}
                    <div className="passenger-section">
                      <p className="passenger-count">
                        Passengers: {elevator.passengers}/8
                      </p>
                      <div className="capacity-bar-container">
                        {/* Capacity bar */}
                        <div 
                          className={`capacity-bar ${
                            elevator.passengers === 0 ? '' :  
                            elevator.passengers < 4 ? 'low' :    
                            elevator.passengers < 7 ? 'medium' :    
                            'full'
                          }`}
                          style={{ width: `${(elevator.passengers / 8) * 100}%` }}
                        />
                      </div>
                      
                      {/* Passenger icons */}
                      <div className="passenger-icons">
                        {Array.from({ length: 8 }, (_, i) => (
                          <div 
                            key={i} 
                            className={`passenger-icon ${i < elevator.passengers ? 'filled' : ''}`}
                          />
                        ))}
                      </div>
                      
                      {/* Individual Passenger Destinations */}
                      {elevator.passengerList && elevator.passengerList.length > 0 && (
                        <div className="destinations-container">
                          <p className="destinations-title">
                            Passenger Destinations:
                          </p>
                          <div className="destinations-list">
                            {elevator.passengerList.map(passenger => (
                              <div key={passenger.id} className="destination-tag">
                                <span className="passenger-id">#{passenger.id}</span>
                                <span className="destination-floor">Floor {passenger.targetFloor}</span>
                              </div>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>
                  </div>
                  
                  {/* Floor buttons */}
                  <div className="floor-buttons-container">
                    <div className="floor-button-grid">
                      {FLOORS.map(floor => (
                        <button
                          key={floor}
                          onClick={() => handleSendElevatorToFloor(elevator.id, floor)}
                          className={`floor-button ${
                            floor === elevator.currentFloor && elevator.status !== 'stopped' ? 'current' :
                            floor === elevator.targetFloor ? 'next-target' :
                            allTargets.includes(floor) ? 'in-route' : ''
                          }`}
                          title={`Send elevator ${elevator.id + 1} to floor ${floor}`}
                        >
                          {floor}
                          {allTargets.includes(floor) && (
                            <span className="target-dot">●</span>
                          )}
                        </button>
                      ))}
                    </div>
                  </div>
                </div>
              );
            })
          }
        </div>
      </div>
      
      {/* Random Call Button */}
      <div className="random-call-container">
        <button 
          onClick={handleRandomCall}
          className="random-call-button"
        >
          Generate Random Elevator Call
        </button>

      </div>
    </div>
  );
}