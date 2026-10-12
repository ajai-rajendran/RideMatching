const API_BASE = '/api';

async function request(url, options = {}) {
  const defaultHeaders = {
    'Content-Type': 'application/json',
  };

  const response = await fetch(url, {
    ...options,
    headers: {
      ...defaultHeaders,
      ...options.headers,
    },
  });

  if (!response.ok) {
    let errorDetail = 'An error occurred';
    try {
      const errorJson = await response.json();
      errorDetail = errorJson.message || errorJson.error || JSON.stringify(errorJson);
    } catch {
      errorDetail = await response.text();
    }
    throw new Error(errorDetail || `HTTP error ${response.status}`);
  }

  const contentType = response.headers.get('content-type');
  if (contentType && contentType.includes('application/json')) {
    return response.json();
  }
  return response.text();
}

export const api = {
  // Drivers
  getDrivers: () => request(`${API_BASE}/drivers`),
  getDriverById: (id) => request(`${API_BASE}/drivers/${id}`),
  getAvailableDrivers: (lat, lon, radiusKm = 15) =>
    request(`${API_BASE}/drivers/available?lat=${lat || ''}&lon=${lon || ''}&radiusKm=${radiusKm}`),
  registerDriver: (data) => request(`${API_BASE}/drivers`, { method: 'POST', body: JSON.stringify(data) }),
  setDriverOnline: (id) => request(`${API_BASE}/drivers/${id}/online`, { method: 'POST' }),
  setDriverOffline: (id) => request(`${API_BASE}/drivers/${id}/offline`, { method: 'POST' }),
  updateDriverLocation: (id, location) =>
    request(`${API_BASE}/drivers/${id}/location`, { method: 'POST', body: JSON.stringify(location) }),
  getDriverActiveRide: (id) => request(`${API_BASE}/drivers/${id}/active-ride`),

  // Riders
  getRiders: () => request(`${API_BASE}/riders`),
  getRiderById: (id) => request(`${API_BASE}/riders/${id}`),
  registerRider: (data) => request(`${API_BASE}/riders`, { method: 'POST', body: JSON.stringify(data) }),
  getRiderActiveRide: (id) => request(`${API_BASE}/riders/${id}/active-ride`),

  // Rides
  getRides: (limit = 20) => request(`${API_BASE}/rides?limit=${limit}`),
  getRideById: (id) => request(`${API_BASE}/rides/${id}`),
  requestRide: (data) => request(`${API_BASE}/rides`, { method: 'POST', body: JSON.stringify(data) }),
  estimateRide: (data) => request(`${API_BASE}/rides/estimate`, { method: 'POST', body: JSON.stringify(data) }),
  acceptRide: (rideId, driverId) =>
    request(`${API_BASE}/rides/${rideId}/accept`, { method: 'POST', body: JSON.stringify({ driverId }) }),
  rejectRide: (rideId, driverId, reason = null) =>
    request(`${API_BASE}/rides/${rideId}/reject${reason ? `?reason=${encodeURIComponent(reason)}` : ''}`, {
      method: 'POST',
      body: JSON.stringify({ driverId }),
    }),
  startRide: (rideId, driverId) =>
    request(`${API_BASE}/rides/${rideId}/start`, { method: 'POST', body: JSON.stringify({ driverId }) }),
  completeRide: (rideId, driverId, finalFare = null, driverRating = 5.0) =>
    request(`${API_BASE}/rides/${rideId}/complete?driverId=${driverId}`, {
      method: 'POST',
      body: JSON.stringify({ finalFare, driverRating }),
    }),
  cancelRide: (rideId, reason, cancelledBy = 'User') =>
    request(`${API_BASE}/rides/${rideId}/cancel`, { method: 'POST', body: JSON.stringify({ reason, cancelledBy }) }),

  // Simulation & Health
  reSeedDatabase: () => request(`${API_BASE}/simulation/seed`, { method: 'POST' }),
  stepDriver: (driverId, targetLat, targetLon, stepFraction = 0.15) =>
    request(`${API_BASE}/simulation/driver-step`, {
      method: 'POST',
      body: JSON.stringify({ driverId, targetLatitude: targetLat, targetLongitude: targetLon, stepFraction }),
    }),
  getHealth: () => request('/health'),
};
