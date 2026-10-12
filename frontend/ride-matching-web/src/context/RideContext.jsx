import React, { createContext, useContext, useState, useEffect, useCallback, useRef } from 'react';
import { api } from '../services/api';
import { signalRService } from '../services/signalr';

const RideContext = createContext(null);

export function RideProvider({ children }) {
  const [currentMode, setCurrentMode] = useState('dual'); // 'rider' | 'driver' | 'dual'
  const [riders, setRiders] = useState([]);
  const [drivers, setDrivers] = useState([]);
  const [selectedRider, setSelectedRider] = useState(null);
  const [selectedDriver, setSelectedDriver] = useState(null);

  // Active Rides
  const [activeRide, setActiveRide] = useState(null); // Rider's active ride
  const [driverActiveRide, setDriverActiveRide] = useState(null); // Driver's active ride
  const [driverOffer, setDriverOffer] = useState(null); // Incoming ride offer for driver

  // Real-time map locations for drivers: { [driverId]: { lat, lon, address, timestamp } }
  const [driverLocations, setDriverLocations] = useState({});

  // SignalR & Activity
  const [signalRStatus, setSignalRStatus] = useState('Connecting');
  const [activityLogs, setActivityLogs] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [toastMessage, setToastMessage] = useState(null);

  const selectedDriverRef = useRef(selectedDriver);
  const selectedRiderRef = useRef(selectedRider);
  const activeRideRef = useRef(activeRide);
  const driverActiveRideRef = useRef(driverActiveRide);

  useEffect(() => {
    selectedDriverRef.current = selectedDriver;
  }, [selectedDriver]);

  useEffect(() => {
    selectedRiderRef.current = selectedRider;
  }, [selectedRider]);

  useEffect(() => {
    activeRideRef.current = activeRide;
  }, [activeRide]);

  useEffect(() => {
    driverActiveRideRef.current = driverActiveRide;
  }, [driverActiveRide]);

  const addLog = useCallback((type, message, data = null) => {
    const newLog = {
      id: `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      time: new Date().toLocaleTimeString(),
      type,
      message,
      data,
    };
    setActivityLogs((prev) => [newLog, ...prev.slice(0, 49)]);
  }, []);

  const showToast = useCallback((message, type = 'info') => {
    setToastMessage({ message, type, id: Date.now() });
    setTimeout(() => {
      setToastMessage((prev) => (prev?.id === toastMessage?.id ? null : prev));
    }, 4500);
  }, [toastMessage]);

  // Load initial riders & drivers
  const loadData = useCallback(async () => {
    try {
      setIsLoading(true);
      const [driversData, ridersData] = await Promise.all([
        api.getDrivers().catch(() => []),
        api.getRiders().catch(() => []),
      ]);

      setDrivers(driversData);
      setRiders(ridersData);

      // Populate driver locations
      const locMap = {};
      driversData.forEach((d) => {
        locMap[d.id] = {
          lat: d.currentLatitude,
          lon: d.currentLongitude,
          address: d.currentAddress,
          status: d.status,
          name: d.name,
          vehicle: d.vehicle,
          rating: d.rating,
        };
      });
      setDriverLocations(locMap);

      // Default select first rider & first driver if not selected
      if (!selectedRider && ridersData.length > 0) {
        setSelectedRider(ridersData[0]);
      }
      if (!selectedDriver && driversData.length > 0) {
        setSelectedDriver(driversData[0]);
      }
    } catch (err) {
      console.error('Failed to load initial data:', err);
    } finally {
      setIsLoading(false);
    }
  }, [selectedDriver, selectedRider]);

  // Setup SignalR and attach event handlers
  useEffect(() => {
    signalRService.startConnection();
    const unsubStatus = signalRService.onStatusChange(setSignalRStatus);

    // Event: DriverLocationUpdated
    signalRService.on('DriverLocationUpdated', (data) => {
      setDriverLocations((prev) => ({
        ...prev,
        [data.driverId]: {
          ...prev[data.driverId],
          lat: data.latitude,
          lon: data.longitude,
          address: data.address,
          timestamp: data.timestamp,
        },
      }));

      // Update active ride driver position if tracking
      if (activeRideRef.current && activeRideRef.current.driverId === data.driverId) {
        setActiveRide((prev) =>
          prev ? { ...prev, driver: { ...prev.driver, currentLatitude: data.latitude, currentLongitude: data.longitude } } : null
        );
      }
    });

    // Event: DriverStatusChanged
    signalRService.on('DriverStatusChanged', (data) => {
      setDrivers((prev) =>
        prev.map((d) => (d.id === data.driverId ? { ...d, status: data.status } : d))
      );
      setDriverLocations((prev) => ({
        ...prev,
        [data.driverId]: {
          ...prev[data.driverId],
          status: data.status,
        },
      }));
      if (selectedDriverRef.current && selectedDriverRef.current.id === data.driverId) {
        setSelectedDriver((prev) => (prev ? { ...prev, status: data.status } : null));
      }
      addLog('status', `Driver status changed to ${data.status}`, data);
    });

    // Event: RideRequested
    signalRService.on('RideRequested', (ride) => {
      addLog('requested', `New ride requested by ${ride.rider?.name || 'Rider'}`, ride);
      if (selectedRiderRef.current && selectedRiderRef.current.id === ride.riderId) {
        setActiveRide(ride);
      }
    });

    // Event: RideMatching
    signalRService.on('RideMatching', (data) => {
      addLog('matching', `Finding nearby drivers (Attempt ${data.attempt})...`, data);
      if (activeRideRef.current && activeRideRef.current.id === data.rideId) {
        setActiveRide((prev) => (prev ? { ...prev, status: 'Matching', matchAttempts: data.attempt } : null));
      }
    });

    // Event: RideOffer (for specific driver)
    signalRService.on('RideOffer', (data) => {
      addLog('offer', `Incoming Ride Offer!`, data);
      if (selectedDriverRef.current && selectedDriverRef.current.id === data.driverId) {
        setDriverOffer(data.ride);
        showToast(`🚖 Incoming Ride Request from ${data.ride.rider?.name || 'Rider'}!`, 'info');
      }
    });

    // Event: RideMatched
    signalRService.on('RideMatched', (data) => {
      addLog('matched', `Ride matched with Driver!`, data);
      if (activeRideRef.current && activeRideRef.current.id === data.rideId) {
        setActiveRide(data.ride);
      }
    });

    // Event: RideAccepted
    signalRService.on('RideAccepted', (ride) => {
      addLog('accepted', `Ride accepted by Driver ${ride.driver?.name || ''}`, ride);
      showToast(`Driver ${ride.driver?.name || ''} accepted the ride and is en route!`, 'success');
      setDriverOffer(null);

      if (selectedRiderRef.current && selectedRiderRef.current.id === ride.riderId) {
        setActiveRide(ride);
      }
      if (selectedDriverRef.current && selectedDriverRef.current.id === ride.driverId) {
        setDriverActiveRide(ride);
      }
    });

    // Event: RideRejected
    signalRService.on('RideRejected', (data) => {
      addLog('rejected', `Driver rejected ride. Re-matching...`, data);
      setDriverOffer(null);
    });

    // Event: RideStarted
    signalRService.on('RideStarted', (ride) => {
      addLog('started', `Ride started! Trip in progress.`, ride);
      showToast('Trip has started! On the way to destination.', 'info');

      if (selectedRiderRef.current && selectedRiderRef.current.id === ride.riderId) {
        setActiveRide(ride);
      }
      if (selectedDriverRef.current && selectedDriverRef.current.id === ride.driverId) {
        setDriverActiveRide(ride);
      }
    });

    // Event: RideCompleted
    signalRService.on('RideCompleted', (ride) => {
      addLog('completed', `Ride completed! Total Fare: $${ride.actualFare || ride.estimatedFare}`, ride);
      showToast(`Ride completed! Fare: $${ride.actualFare || ride.estimatedFare}`, 'success');

      if (selectedRiderRef.current && selectedRiderRef.current.id === ride.riderId) {
        setActiveRide(ride);
      }
      if (selectedDriverRef.current && selectedDriverRef.current.id === ride.driverId) {
        setDriverActiveRide(ride);
      }
      loadData();
    });

    // Event: RideCancelled
    signalRService.on('RideCancelled', (data) => {
      const ride = data.ride || data;
      addLog('cancelled', `Ride was cancelled: ${data.reason || 'User cancelled'}`, data);
      showToast(`Ride cancelled: ${data.reason || 'Cancelled'}`, 'warning');

      setDriverOffer(null);
      if (selectedRiderRef.current && selectedRiderRef.current.id === ride.riderId) {
        setActiveRide(ride);
      }
      if (selectedDriverRef.current && selectedDriverRef.current.id === ride.driverId) {
        setDriverActiveRide(null);
      }
      loadData();
    });

    loadData();

    return () => {
      unsubStatus();
    };
  }, [addLog, loadData, showToast]);

  // Register channels whenever selected rider or driver changes
  useEffect(() => {
    if (selectedDriver) {
      signalRService.registerDriver(selectedDriver.id);
      // Check active ride for driver
      api.getDriverActiveRide(selectedDriver.id)
        .then((ride) => setDriverActiveRide(ride))
        .catch(() => setDriverActiveRide(null));
    }
  }, [selectedDriver]);

  useEffect(() => {
    if (selectedRider) {
      signalRService.registerRider(selectedRider.id);
      // Check active ride for rider
      api.getRiderActiveRide(selectedRider.id)
        .then((ride) => setActiveRide(ride))
        .catch(() => setActiveRide(null));
    }
  }, [selectedRider]);

  // Actions
  const handleRequestRide = async (rideData) => {
    try {
      setIsLoading(true);
      const res = await api.requestRide(rideData);
      setActiveRide(res);
      showToast('Ride requested! Searching for nearest drivers...', 'info');
      return res;
    } catch (err) {
      showToast(err.message || 'Failed to request ride', 'error');
      throw err;
    } finally {
      setIsLoading(false);
    }
  };

  const handleAcceptRide = async (rideId, driverId) => {
    try {
      setIsLoading(true);
      const res = await api.acceptRide(rideId, driverId);
      setDriverOffer(null);
      setDriverActiveRide(res);
      showToast('Ride accepted! Proceed to pickup location.', 'success');
      return res;
    } catch (err) {
      showToast(err.message || 'Failed to accept ride', 'error');
      throw err;
    } finally {
      setIsLoading(false);
    }
  };

  const handleRejectRide = async (rideId, driverId, reason = 'Driver declined') => {
    try {
      await api.rejectRide(rideId, driverId, reason);
      setDriverOffer(null);
      showToast('Ride declined.', 'info');
    } catch (err) {
      showToast(err.message || 'Failed to reject ride', 'error');
    }
  };

  const handleStartRide = async (rideId, driverId) => {
    try {
      setIsLoading(true);
      const res = await api.startRide(rideId, driverId);
      setDriverActiveRide(res);
      showToast('Trip started! Driving to destination.', 'info');
      return res;
    } catch (err) {
      showToast(err.message || 'Failed to start ride', 'error');
      throw err;
    } finally {
      setIsLoading(false);
    }
  };

  const handleCompleteRide = async (rideId, driverId, finalFare = null, rating = 5.0) => {
    try {
      setIsLoading(true);
      const res = await api.completeRide(rideId, driverId, finalFare, rating);
      setDriverActiveRide(res);
      showToast(`Ride completed! Fare: $${res.actualFare}`, 'success');
      return res;
    } catch (err) {
      showToast(err.message || 'Failed to complete ride', 'error');
      throw err;
    } finally {
      setIsLoading(false);
    }
  };

  const handleCancelRide = async (rideId, reason = 'Cancelled by user', cancelledBy = 'User') => {
    try {
      setIsLoading(true);
      const res = await api.cancelRide(rideId, reason, cancelledBy);
      setActiveRide(null);
      setDriverActiveRide(null);
      setDriverOffer(null);
      showToast('Ride cancelled.', 'warning');
      return res;
    } catch (err) {
      showToast(err.message || 'Failed to cancel ride', 'error');
      throw err;
    } finally {
      setIsLoading(false);
    }
  };

  const handleToggleDriverOnline = async (driver) => {
    try {
      setIsLoading(true);
      if (driver.status === 'Available') {
        const res = await api.setDriverOffline(driver.id);
        setSelectedDriver(res);
        showToast('You are now OFFLINE', 'info');
      } else {
        const res = await api.setDriverOnline(driver.id);
        setSelectedDriver(res);
        showToast('You are now ONLINE and ready for rides!', 'success');
      }
      await loadData();
    } catch (err) {
      showToast(err.message || 'Failed to change driver status', 'error');
    } finally {
      setIsLoading(false);
    }
  };

  const handleUpdateDriverLocation = async (driverId, lat, lon, address = 'Updated Location') => {
    try {
      const res = await api.updateDriverLocation(driverId, { latitude: lat, longitude: lon, address });
      setDriverLocations((prev) => ({
        ...prev,
        [driverId]: {
          ...prev[driverId],
          lat,
          lon,
          address,
        },
      }));
      if (selectedDriver && selectedDriver.id === driverId) {
        setSelectedDriver(res);
      }
      return res;
    } catch (err) {
      console.error('Failed to update driver location:', err);
    }
  };

  const handleReSeed = async () => {
    try {
      setIsLoading(true);
      await api.reSeedDatabase();
      setActiveRide(null);
      setDriverActiveRide(null);
      setDriverOffer(null);
      await loadData();
      showToast('Database reset and seeded with fresh drivers & riders!', 'success');
    } catch (err) {
      showToast(err.message || 'Failed to re-seed database', 'error');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <RideContext.Provider
      value={{
        currentMode,
        setCurrentMode,
        riders,
        drivers,
        selectedRider,
        setSelectedRider,
        selectedDriver,
        setSelectedDriver,
        activeRide,
        setActiveRide,
        driverActiveRide,
        setDriverActiveRide,
        driverOffer,
        setDriverOffer,
        driverLocations,
        signalRStatus,
        activityLogs,
        isLoading,
        toastMessage,
        showToast,
        loadData,
        requestRide: handleRequestRide,
        acceptRide: handleAcceptRide,
        rejectRide: handleRejectRide,
        startRide: handleStartRide,
        completeRide: handleCompleteRide,
        cancelRide: handleCancelRide,
        toggleDriverOnline: handleToggleDriverOnline,
        updateDriverLocation: handleUpdateDriverLocation,
        reSeedData: handleReSeed,
      }}
    >
      {children}
    </RideContext.Provider>
  );
}

export const useRide = () => {
  const context = useContext(RideContext);
  if (!context) {
    throw new Error('useRide must be used within a RideProvider');
  }
  return context;
};
