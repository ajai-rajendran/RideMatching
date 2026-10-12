import React, { useState, useEffect, useRef } from 'react';
import { useRide } from '../context/RideContext';
import { api } from '../services/api';
import {
  Car,
  Power,
  Navigation,
  MapPin,
  Clock,
  DollarSign,
  User,
  Star,
  CheckCircle,
  XCircle,
  Play,
  Check,
  PlusCircle,
  RotateCcw,
  Zap,
} from 'lucide-react';

const SF_PRESETS = [
  { name: 'Civic Center / City Hall', lat: 37.7792, lon: -122.4191 },
  { name: 'Union Square', lat: 37.7879, lon: -122.4075 },
  { name: 'Financial District', lat: 37.7946, lon: -122.4005 },
  { name: 'Embarcadero / Ferry Bldg', lat: 37.7955, lon: -122.3937 },
  { name: 'Mission District', lat: 37.7599, lon: -122.4148 },
  { name: 'Fisherman’s Wharf', lat: 37.8080, lon: -122.4177 },
  { name: 'SOMA / Tech Hub', lat: 37.7785, lon: -122.3950 },
];

export default function DriverView({
  onSelectMapLocation,
  selectingType,
  isSelectingLocation,
  pendingDriverLocation,
}) {
  const {
    drivers,
    selectedDriver,
    setSelectedDriver,
    driverActiveRide,
    driverOffer,
    toggleDriverOnline,
    acceptRide,
    rejectRide,
    startRide,
    completeRide,
    updateDriverLocation,
    isLoading,
    showToast,
    loadData,
  } = useRide();

  // Auto-drive simulation timer
  const [isSimulatingDrive, setIsSimulatingDrive] = useState(false);
  const simIntervalRef = useRef(null);

  // Offer countdown timer (15s)
  const [offerCountdown, setOfferCountdown] = useState(15);

  // New driver modal
  const [showNewDriverModal, setShowNewDriverModal] = useState(false);
  const [newDriverName, setNewDriverName] = useState('');
  const [newDriverEmail, setNewDriverEmail] = useState('');
  const [newDriverPhone, setNewDriverPhone] = useState('');
  const [newDriverMake, setNewDriverMake] = useState('Tesla');
  const [newDriverModel, setNewDriverModel] = useState('Model Y');
  const [newDriverPlate, setNewDriverPlate] = useState('7XYZ123');
  const [newDriverColor, setNewDriverColor] = useState('Pearl White');
  const [driverLocPreset, setDriverLocPreset] = useState(SF_PRESETS[0]);
  const [driverLat, setDriverLat] = useState(37.7792);
  const [driverLon, setDriverLon] = useState(-122.4191);

  // Update driver coordinates when picked from map
  useEffect(() => {
    if (pendingDriverLocation) {
      setDriverLat(pendingDriverLocation.lat);
      setDriverLon(pendingDriverLocation.lon);
      setDriverLocPreset({ name: pendingDriverLocation.name, lat: pendingDriverLocation.lat, lon: pendingDriverLocation.lon });
    }
  }, [pendingDriverLocation]);

  // Handle offer countdown
  useEffect(() => {
    let timer;
    if (driverOffer) {
      setOfferCountdown(15);
      timer = setInterval(() => {
        setOfferCountdown((prev) => {
          if (prev <= 1) {
            clearInterval(timer);
            if (selectedDriver) {
              rejectRide(driverOffer.id, selectedDriver.id, 'Offer timed out');
            }
            return 0;
          }
          return prev - 1;
        });
      }, 1000);
    }
    return () => clearInterval(timer);
  }, [driverOffer, rejectRide, selectedDriver]);

  // Clean up simulation interval on unmount
  useEffect(() => {
    return () => {
      if (simIntervalRef.current) clearInterval(simIntervalRef.current);
    };
  }, []);

  const handleCreateDriver = async (e) => {
    e.preventDefault();
    try {
      const res = await api.registerDriver({
        name: newDriverName,
        email: newDriverEmail,
        phoneNumber: newDriverPhone || '+1-555-0000',
        make: newDriverMake,
        model: newDriverModel,
        licensePlate: newDriverPlate,
        color: newDriverColor,
        initialLatitude: driverLat,
        initialLongitude: driverLon,
      });
      await loadData();
      setSelectedDriver(res);
      setShowNewDriverModal(false);
      setNewDriverName('');
      setNewDriverEmail('');
      setNewDriverPhone('');
      showToast(`Driver ${res.name} registered at (${driverLat.toFixed(4)}, ${driverLon.toFixed(4)})! Click GO ONLINE to begin.`, 'success');
    } catch (err) {
      showToast(err.message || 'Failed to register driver', 'error');
    }
  };

  const handleAcceptOffer = async () => {
    if (!driverOffer || !selectedDriver) return;
    await acceptRide(driverOffer.id, selectedDriver.id);
  };

  const handleDeclineOffer = async () => {
    if (!driverOffer || !selectedDriver) return;
    await rejectRide(driverOffer.id, selectedDriver.id, 'Driver declined');
  };

  // Simulate driving step-by-step towards target (Pickup or Dropoff)
  const toggleAutoDrive = (targetLat, targetLon) => {
    if (isSimulatingDrive) {
      if (simIntervalRef.current) clearInterval(simIntervalRef.current);
      setIsSimulatingDrive(false);
      showToast('Simulation paused', 'info');
      return;
    }

    if (!selectedDriver) return;
    setIsSimulatingDrive(true);
    showToast('Auto-driving towards destination...', 'info');

    simIntervalRef.current = setInterval(async () => {
      try {
        const res = await api.stepDriver(selectedDriver.id, targetLat, targetLon, 0.25);
        if (res.arrived) {
          clearInterval(simIntervalRef.current);
          setIsSimulatingDrive(false);
          showToast('🚖 Driver has arrived at destination!', 'success');
        }
      } catch (err) {
        clearInterval(simIntervalRef.current);
        setIsSimulatingDrive(false);
      }
    }, 1200);
  };

  return (
    <div className="flex flex-col h-full bg-slate-900/50 backdrop-blur rounded-2xl border border-slate-800 p-4 space-y-4 overflow-y-auto">
      {/* Header & Driver Picker */}
      <div className="flex items-center justify-between border-b border-slate-800 pb-3">
        <div className="flex items-center gap-2">
          <div className="p-2 bg-emerald-500/10 text-emerald-400 rounded-xl border border-emerald-500/20">
            <Car className="w-5 h-5" />
          </div>
          <div>
            <h2 className="text-sm font-bold text-white uppercase tracking-wider">Driver Console</h2>
            <p className="text-xs text-slate-400">Accept Requests & Live GPS</p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <select
            value={selectedDriver?.id || ''}
            onChange={(e) => {
              const d = drivers.find((x) => x.id === e.target.value);
              setSelectedDriver(d || null);
            }}
            className="bg-slate-950 border border-slate-800 text-xs text-slate-200 rounded-lg px-2.5 py-1.5 focus:outline-none focus:border-emerald-500"
          >
            {drivers.map((d) => (
              <option key={d.id} value={d.id}>
                {d.name} ({d.status})
              </option>
            ))}
          </select>

          <button
            onClick={() => setShowNewDriverModal(true)}
            title="Register new driver"
            className="p-1.5 bg-slate-800 hover:bg-slate-700 text-emerald-400 rounded-lg border border-slate-700 transition"
          >
            <PlusCircle className="w-4 h-4" />
          </button>
        </div>
      </div>

      {/* Driver Online/Offline Control & Profile */}
      {selectedDriver && (
        <div className="bg-slate-950 border border-slate-800 rounded-xl p-4 shadow-xl space-y-3">
          <div className="flex items-center justify-between">
            <div>
              <div className="text-sm font-bold text-white flex items-center gap-2">
                {selectedDriver.name}
                <span className="text-xs bg-amber-500/20 text-amber-300 px-1.5 py-0.5 rounded flex items-center gap-1">
                  <Star className="w-3 h-3 fill-amber-300" /> {selectedDriver.rating}
                </span>
              </div>
              <p className="text-xs text-slate-400">
                {selectedDriver.vehicle?.color} {selectedDriver.vehicle?.make} {selectedDriver.vehicle?.model} • {selectedDriver.vehicle?.licensePlate}
              </p>
            </div>

            {/* Online Toggle Switch */}
            <button
              onClick={() => toggleDriverOnline(selectedDriver)}
              disabled={isLoading || selectedDriver.status === 'InRide'}
              className={`flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-bold transition-all shadow-lg ${
                selectedDriver.status === 'Available'
                  ? 'bg-emerald-500 text-slate-950 shadow-emerald-500/20'
                  : selectedDriver.status === 'Offline'
                  ? 'bg-slate-800 text-slate-400 hover:text-white hover:bg-slate-700'
                  : 'bg-amber-500 text-slate-950'
              }`}
            >
              <Power className="w-4 h-4" />
              {selectedDriver.status === 'Available'
                ? 'ONLINE'
                : selectedDriver.status === 'Offline'
                ? 'GO ONLINE'
                : selectedDriver.status}
            </button>
          </div>

          <div className="grid grid-cols-3 gap-2 pt-2 border-t border-slate-800/80 text-center text-xs">
            <div className="bg-slate-900/80 p-2 rounded-lg">
              <span className="text-slate-400 block text-[10px]">Rides</span>
              <span className="font-bold text-white">{selectedDriver.totalRides}</span>
            </div>
            <div className="bg-slate-900/80 p-2 rounded-lg">
              <span className="text-slate-400 block text-[10px]">Type</span>
              <span className="font-bold text-white">{selectedDriver.vehicle?.type}</span>
            </div>
            <div className="bg-slate-900/80 p-2 rounded-lg">
              <span className="text-slate-400 block text-[10px]">Status</span>
              <span
                className={`font-bold ${
                  selectedDriver.status === 'Available'
                    ? 'text-emerald-400'
                    : selectedDriver.status === 'Offline'
                    ? 'text-slate-400'
                    : 'text-amber-400'
                }`}
              >
                {selectedDriver.status}
              </span>
            </div>
          </div>
        </div>
      )}

      {/* INCOMING RIDE OFFER POPUP HUD */}
      {driverOffer && (
        <div className="bg-gradient-to-b from-amber-950/90 to-slate-950 border-2 border-amber-500 rounded-2xl p-4 shadow-2xl space-y-3 animate-pulse">
          <div className="flex items-center justify-between">
            <span className="text-xs uppercase font-black tracking-widest text-amber-400 flex items-center gap-1.5">
              <Zap className="w-4 h-4" /> INCOMING RIDE OFFER
            </span>
            <span className="text-xs font-mono font-bold bg-amber-500/20 text-amber-300 px-2 py-0.5 rounded-full border border-amber-500/40">
              {offerCountdown}s left
            </span>
          </div>

          {/* Progress Countdown Bar */}
          <div className="w-full bg-slate-900 h-1.5 rounded-full overflow-hidden">
            <div
              className="bg-amber-400 h-full transition-all duration-1000"
              style={{ width: `${(offerCountdown / 15) * 100}%` }}
            ></div>
          </div>

          <div className="space-y-1.5">
            <div className="flex items-center justify-between">
              <div className="text-sm font-bold text-white">{driverOffer.rider?.name || 'Rider'}</div>
              <div className="text-xl font-black text-emerald-400">${driverOffer.estimatedFare}</div>
            </div>

            <div className="text-xs text-slate-300 space-y-1 bg-slate-900/80 p-2.5 rounded-xl border border-slate-800">
              <div className="flex items-center gap-1.5 text-emerald-400">
                <MapPin className="w-3.5 h-3.5 shrink-0" />
                <span className="truncate">From: {driverOffer.pickupLocation.address}</span>
              </div>
              <div className="flex items-center gap-1.5 text-red-400">
                <Navigation className="w-3.5 h-3.5 shrink-0" />
                <span className="truncate">To: {driverOffer.dropoffLocation.address}</span>
              </div>
              <div className="text-[11px] text-slate-400 pt-1">
                Trip: {driverOffer.distanceKm} km • ~{driverOffer.estimatedDurationMinutes} mins
              </div>
            </div>
          </div>

          <div className="flex items-center gap-2 pt-1">
            <button
              onClick={handleDeclineOffer}
              className="flex-1 py-2.5 bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-bold rounded-xl border border-slate-700"
            >
              Decline
            </button>
            <button
              onClick={handleAcceptOffer}
              className="flex-2 py-2.5 bg-emerald-500 hover:bg-emerald-400 text-slate-950 text-xs font-black uppercase tracking-wider rounded-xl shadow-lg shadow-emerald-500/20"
            >
              Accept Ride
            </button>
          </div>
        </div>
      )}

      {/* ACTIVE RIDE DRIVER HUD */}
      {driverActiveRide && (
        <div className="bg-slate-950 border border-slate-800 rounded-xl p-4 shadow-xl space-y-3">
          <div className="flex items-center justify-between">
            <span className="text-xs uppercase font-extrabold tracking-widest text-emerald-400">
              {driverActiveRide.status === 'Accepted'
                ? 'Proceed to Pickup'
                : driverActiveRide.status === 'Started'
                ? 'Driving to Destination'
                : `Status: ${driverActiveRide.status}`}
            </span>
            <span className="text-sm font-black text-white">
              ${driverActiveRide.actualFare || driverActiveRide.estimatedFare}
            </span>
          </div>

          <div className="bg-slate-900 p-3 rounded-xl border border-slate-800 space-y-2 text-xs">
            <div className="flex items-center justify-between">
              <span className="text-slate-400">Rider:</span>
              <span className="font-bold text-white">{driverActiveRide.rider?.name || 'Rider'}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-slate-400">Target Address:</span>
              <span className="font-semibold text-slate-200 truncate max-w-[180px]">
                {driverActiveRide.status === 'Accepted'
                  ? driverActiveRide.pickupLocation.address
                  : driverActiveRide.dropoffLocation.address}
              </span>
            </div>
          </div>

          {/* Turn by Turn / Auto Simulation Controls */}
          <div className="space-y-2">
            <button
              onClick={() => {
                const target =
                  driverActiveRide.status === 'Accepted'
                    ? driverActiveRide.pickupLocation
                    : driverActiveRide.dropoffLocation;
                toggleAutoDrive(target.latitude, target.longitude);
              }}
              className={`w-full py-2 px-3 rounded-xl text-xs font-bold transition flex items-center justify-center gap-2 border ${
                isSimulatingDrive
                  ? 'bg-amber-500/20 text-amber-300 border-amber-500/40 animate-pulse'
                  : 'bg-slate-800 hover:bg-slate-700 text-slate-200 border-slate-700'
              }`}
            >
              <Zap className="w-4 h-4" />
              {isSimulatingDrive
                ? 'Simulating GPS Movement (Pause)'
                : driverActiveRide.status === 'Accepted'
                ? 'Simulate GPS Drive to Pickup'
                : 'Simulate GPS Drive to Dropoff'}
            </button>

            {/* Action Buttons based on status */}
            {driverActiveRide.status === 'Accepted' && (
              <button
                onClick={() => startRide(driverActiveRide.id, selectedDriver.id)}
                disabled={isLoading}
                className="w-full py-2.5 bg-blue-600 hover:bg-blue-500 text-white font-bold text-xs uppercase tracking-wider rounded-xl transition flex items-center justify-center gap-2"
              >
                <Play className="w-4 h-4" /> Start Ride (Passenger Picked Up)
              </button>
            )}

            {driverActiveRide.status === 'Started' && (
              <button
                onClick={() => completeRide(driverActiveRide.id, selectedDriver.id)}
                disabled={isLoading}
                className="w-full py-2.5 bg-emerald-500 hover:bg-emerald-400 text-slate-950 font-black text-xs uppercase tracking-wider rounded-xl transition flex items-center justify-center gap-2 shadow-lg shadow-emerald-500/20"
              >
                <Check className="w-4 h-4" /> Complete Ride & Collect Fare
              </button>
            )}
          </div>
        </div>
      )}

      {/* New Driver Registration Modal */}
      {showNewDriverModal && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-[2000] flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-sm w-full p-5 space-y-4 shadow-2xl max-h-[90vh] overflow-y-auto">
            <h3 className="text-sm font-bold text-white uppercase tracking-wider">Register New Driver</h3>
            <form onSubmit={handleCreateDriver} className="space-y-3">
              <div>
                <label className="text-xs text-slate-400 block mb-1">Full Name</label>
                <input
                  type="text"
                  required
                  placeholder="e.g. David Martinez"
                  value={newDriverName}
                  onChange={(e) => setNewDriverName(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-emerald-500"
                />
              </div>

              <div>
                <label className="text-xs text-slate-400 block mb-1">Email</label>
                <input
                  type="email"
                  required
                  placeholder="david@rides.com"
                  value={newDriverEmail}
                  onChange={(e) => setNewDriverEmail(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-emerald-500"
                />
              </div>

              <div className="grid grid-cols-2 gap-2">
                <div>
                  <label className="text-xs text-slate-400 block mb-1">Make</label>
                  <input
                    type="text"
                    required
                    placeholder="Tesla / Toyota"
                    value={newDriverMake}
                    onChange={(e) => setNewDriverMake(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-emerald-500"
                  />
                </div>
                <div>
                  <label className="text-xs text-slate-400 block mb-1">Model</label>
                  <input
                    type="text"
                    required
                    placeholder="Model Y / Camry"
                    value={newDriverModel}
                    onChange={(e) => setNewDriverModel(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-emerald-500"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-2">
                <div>
                  <label className="text-xs text-slate-400 block mb-1">License Plate</label>
                  <input
                    type="text"
                    required
                    placeholder="7ABC123"
                    value={newDriverPlate}
                    onChange={(e) => setNewDriverPlate(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-emerald-500"
                  />
                </div>
                <div>
                  <label className="text-xs text-slate-400 block mb-1">Color</label>
                  <input
                    type="text"
                    required
                    placeholder="White / Black"
                    value={newDriverColor}
                    onChange={(e) => setNewDriverColor(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-emerald-500"
                  />
                </div>
              </div>

              {/* Initial GPS Location Picker */}
              <div className="space-y-1.5 pt-1 border-t border-slate-800">
                <label className="text-xs font-semibold text-slate-400 flex items-center justify-between">
                  <span className="flex items-center gap-1 text-emerald-400">
                    <MapPin className="w-3.5 h-3.5" /> Initial GPS Location
                  </span>
                  <button
                    type="button"
                    onClick={() => {
                      if (onSelectMapLocation) onSelectMapLocation('driver_reg');
                    }}
                    className={`text-[11px] px-2 py-0.5 rounded border transition ${
                      isSelectingLocation && selectingType === 'driver_reg'
                        ? 'bg-emerald-500 text-slate-950 border-emerald-500 font-bold animate-pulse'
                        : 'text-slate-400 border-slate-700 hover:text-emerald-400'
                    }`}
                  >
                    {isSelectingLocation && selectingType === 'driver_reg' ? 'Click map now...' : 'Pick on Map'}
                  </button>
                </label>

                <select
                  value={driverLocPreset.name}
                  onChange={(e) => {
                    const p = SF_PRESETS.find((x) => x.name === e.target.value);
                    if (p) {
                      setDriverLocPreset(p);
                      setDriverLat(p.lat);
                      setDriverLon(p.lon);
                    }
                  }}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-slate-200 focus:outline-none focus:border-emerald-500"
                >
                  {SF_PRESETS.map((p) => (
                    <option key={p.name} value={p.name}>
                      {p.name}
                    </option>
                  ))}
                </select>

                <div className="text-[11px] font-mono text-slate-500 flex justify-between px-1">
                  <span>Lat: {driverLat.toFixed(4)}</span>
                  <span>Lon: {driverLon.toFixed(4)}</span>
                </div>
              </div>

              <div className="flex items-center gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setShowNewDriverModal(false)}
                  className="flex-1 py-2 bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold rounded-xl"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="flex-1 py-2 bg-emerald-500 hover:bg-emerald-400 text-slate-950 text-xs font-bold rounded-xl shadow-lg shadow-emerald-500/20"
                >
                  Save Driver
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
