export interface Passenger {
  id: number;
  pickupFloor: number;
  targetFloor: number;
}

export interface Elevator {
  id: number;
  currentFloor: number;
  targetFloor: number | null;
  doorsOpen: boolean;
  direction: 'up' | 'down' | 'idle';
  status: 'moving' | 'stopped' | 'loading';
  passengers: number;
  pendingPickup: boolean;
  pendingPassenger: Passenger | null;
  passengerList: Passenger[];
  
  //properties for multiple passengers
  pendingPassengers?: Passenger[];
  pendingPickupFloors?: number[];
}

export interface FloorRequest {
  floor: number;
  direction: 'up' | 'down';
}

// API Request types
export interface CallElevatorRequest {
  floor: number;
  direction: string;
}

export interface SendElevatorRequest {
  targetFloor: number;
}

// Error types for better error handling
export interface ApiError {
  message: string;
  code?: string;
  details?: any;
}