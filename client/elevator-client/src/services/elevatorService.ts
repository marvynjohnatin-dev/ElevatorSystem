import axios from 'axios';
import { Elevator, FloorRequest } from '../types';

const API_URL = 'https://localhost:7000/api/elevator';

const api = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-type': 'application/json'
  }
});

// Get all elevators
export const getElevators = async (): Promise<Elevator[]> => {
  try {
    const response = await api.get('');
    return response.data;
  } catch (error) {
    console.error('Error fetching elevators:', error);
    throw error;
  }
};

// Get a specific elevator by ID
export const getElevator = async (id: number): Promise<Elevator> => {
  try {
    const response = await api.get(`/${id}`);
    return response.data;
  } catch (error) {
    console.error(`Error fetching elevator ${id}:`, error);
    throw error;
  }
};

// Get all floor requests
export const getFloorRequests = async (): Promise<FloorRequest[]> => {
  try {
    const response = await api.get('/floorRequests');
    return response.data;
  } catch (error) {
    console.error('Error fetching floor requests:', error);
    throw error;
  }
};

// Call an elevator to a specific floor
export const callElevator = async (floor: number, direction: 'up' | 'down'): Promise<void> => {
  try {
    await api.post('/call', { floor, direction });
  } catch (error) {
    console.error('Error calling elevator:', error);
    throw error;
  }
};

// Send an elevator to a specific floor
export const sendElevatorToFloor = async (elevatorId: number, targetFloor: number): Promise<void> => {
  try {
    await api.post(`/${elevatorId}/send`, { targetFloor });
  } catch (error) {
    console.error('Error sending elevator:', error);
    throw error;
  }
};

// Generate a random elevator call
export const generateRandomCall = async (): Promise<void> => {
  try {
    await api.post('/random-call');
  } catch (error) {
    console.error('Error generating random call:', error);
    throw error;
  }
};