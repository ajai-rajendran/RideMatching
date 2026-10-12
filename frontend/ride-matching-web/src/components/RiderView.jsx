import React, { useState, useEffect } from 'react';
import { useRide } from '../context/RideContext';
import { api } from '../services/api';
import confetti from 'canvas-confetti';
import {
  MapPin,
  Navigation,
  Compass,
  Clock,
  DollarSign,
  Car,
  User,
  Shield,
  Star,
  CheckCircle2,
  XCircle,
  Phone,
  ArrowRight,
  Sparkles,
  PlusCircle,
  Radio,
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

export default function RiderView({
  onSelectMapLocation,
  selectingType,
  isSelectingLocation,
  pendingRiderLocation,
  customPickup,
  customDropoff,
}) {
  const {
    riders,
    selectedRider,
    setSelectedRider,
    activeRide,
    requestRide,
    cancelRide,
    isLoading,
    showToast,
    loadData,
  } = useRide();

  // Locations state
  const [pickup, setPickup] = useState(SF_PRESETS[0]);
  const [dropoff, setDropoff] = useState(SF_PRESETS[1]);
  const [estimate, setEstimate] = useState(null);
  const [isEstimating, setIsEstimating] = useState(false);

  // New rider modal
  const [showNewRiderModal, setShowNewRiderModal] = useState(false);
  const [newRiderName, setNewRiderName] = useState('');
  const [newRiderEmail, setNewRiderEmail] = useState('');
  const [newRiderPhone, setNewRiderPhone] = useState('');
  const [riderLocPreset, setRiderLocPreset] = useState(SF_PRESETS[0]);
  const [riderLat, setRiderLat] = useState(37.7792);
  const [riderLon, setRiderLon] = useState(-122.4191);

  // Sync custom pickup/dropoff picked from map
  useEffect(() => {
    if (customPickup) {
      setPickup(customPickup);
    }
  }, [customPickup]);

  useEffect(() => {
    if (customDropoff) {
      setDropoff(customDropoff);
    }
  }, [customDropoff]);

  useEffect(() => {
    if (pendingRiderLocation) {
      setRiderLat(pendingRiderLocation.lat);
      setRiderLon(pendingRiderLocation.lon);
      setRiderLocPreset({ name: pendingRiderLocation.name, lat: pendingRiderLocation.lat, lon: pendingRiderLocation.lon });
    }
  }, [pendingRiderLocation]);

  // Fetch estimate whenever pickup or dropoff changes
  useEffect(() => {
    if (pickup && dropoff) {
      setIsEstimating(true);
      api.estimateRide({
        pickupLatitude: pickup.lat,
        pickupLongitude: pickup.lon,
        dropoffLatitude: dropoff.lat,
        dropoffLongitude: dropoff.lon,
      })
        .then((res) => setEstimate(res))
        .catch(() => setEstimate(null))
        .finally(() => setIsEstimating(false));
    }
  }, [pickup, dropoff]);

  // Confetti when ride completes
  useEffect(() => {
    if (activeRide?.status === 'Completed') {
      confetti({
        particleCount: 80,
        spread: 60,
        origin: { y: 0.6 },
      });
    }
  }, [activeRide?.status]);

  const handleCreateRider = async (e) => {
    e.preventDefault();
    try {
      const res = await api.registerRider({
        name: newRiderName,
        email: newRiderEmail,
        phoneNumber: newRiderPhone || '+1-555-0000',
      });
      await loadData();
      setSelectedRider(res);
      setShowNewRiderModal(false);
      setNewRiderName('');
      setNewRiderEmail('');
      setNewRiderPhone('');
      showToast(`Rider ${res.name} registered successfully!`, 'success');
    } catch (err) {
      showToast(err.message || 'Failed to create rider', 'error');
    }
  };

  const handleBookRide = async () => {
    if (!selectedRider) {
      showToast('Please select or register a rider first.', 'error');
      return;
    }

    try {
      await requestRide({
        riderId: selectedRider.id,
        pickupLatitude: pickup.lat,
        pickupLongitude: pickup.lon,
        pickupAddress: pickup.name,
        dropoffLatitude: dropoff.lat,
        dropoffLongitude: dropoff.lon,
        dropoffAddress: dropoff.name,
      });
    } catch (err) {
      // handled in context
    }
  };

  const handleCancel = async () => {
    if (!activeRide) return;
    await cancelRide(activeRide.id, 'Rider requested cancellation', 'Rider');
  };

  return (
    <div className="flex flex-col h-full bg-slate-900/50 backdrop-blur rounded-2xl border border-slate-800 p-4 space-y-4 overflow-y-auto">
      {/* Header & Rider Picker */}
      <div className="flex items-center justify-between border-b border-slate-800 pb-3">
        <div className="flex items-center gap-2">
          <div className="p-2 bg-cyan-500/10 text-cyan-400 rounded-xl border border-cyan-500/20">
            <User className="w-5 h-5" />
          </div>
          <div>
            <h2 className="text-sm font-bold text-white uppercase tracking-wider">Rider Portal</h2>
            <p className="text-xs text-slate-400">Request & Track Live Rides</p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <select
            value={selectedRider?.id || ''}
            onChange={(e) => {
              const r = riders.find((x) => x.id === e.target.value);
              setSelectedRider(r || null);
            }}
            className="bg-slate-950 border border-slate-800 text-xs text-slate-200 rounded-lg px-2.5 py-1.5 focus:outline-none focus:border-cyan-500"
          >
            {riders.map((r) => (
              <option key={r.id} value={r.id}>
                {r.name} (★ {r.rating})
              </option>
            ))}
          </select>

          <button
            onClick={() => setShowNewRiderModal(true)}
            title="Register new rider"
            className="p-1.5 bg-slate-800 hover:bg-slate-700 text-cyan-400 rounded-lg border border-slate-700 transition"
          >
            <PlusCircle className="w-4 h-4" />
          </button>
        </div>
      </div>

      {/* RIDE ACTIVE STATUS CARD */}
      {activeRide && (
        <div className="bg-slate-950 border border-slate-800 rounded-xl p-4 shadow-xl space-y-3 relative overflow-hidden">
          {/* Status Badge */}
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <span
                className={`w-3 h-3 rounded-full ${
                  activeRide.status === 'Completed'
                    ? 'bg-emerald-400'
                    : activeRide.status === 'Cancelled'
                    ? 'bg-red-400'
                    : 'bg-cyan-400 animate-ping'
                }`}
              ></span>
              <span className="text-xs uppercase font-extrabold tracking-widest text-cyan-400">
                {activeRide.status === 'Requested' || activeRide.status === 'Matching'
                  ? 'Searching for Driver'
                  : activeRide.status === 'Matched'
                  ? 'Driver Matched'
                  : activeRide.status === 'Accepted'
                  ? 'Driver En Route'
                  : activeRide.status === 'Started'
                  ? 'Trip in Progress'
                  : `Ride ${activeRide.status}`}
              </span>
            </div>

            <span className="text-xs font-semibold text-slate-400">
              ${activeRide.actualFare || activeRide.estimatedFare}
            </span>
          </div>

          {/* Radar Animation when Matching */}
          {(activeRide.status === 'Requested' || activeRide.status === 'Matching') && (
            <div className="py-6 flex flex-col items-center justify-center space-y-3">
              <div className="relative flex items-center justify-center">
                <div className="w-16 h-16 rounded-full bg-cyan-500/20 border border-cyan-500/40 radar-pulse flex items-center justify-center">
                  <Radio className="w-8 h-8 text-cyan-400 animate-pulse" />
                </div>
              </div>
              <p className="text-xs text-slate-400 text-center">
                Scanning Redis geospatial index for nearest available drivers...
              </p>
              <button
                onClick={handleCancel}
                className="px-3 py-1 bg-red-500/20 hover:bg-red-500/30 text-red-400 border border-red-500/30 rounded-lg text-xs font-semibold transition"
              >
                Cancel Search
              </button>
            </div>
          )}

          {/* Assigned Driver Card */}
          {activeRide.driver && (
            <div className="bg-slate-900 border border-slate-800 rounded-xl p-3 space-y-2">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2.5">
                  <div className="w-9 h-9 rounded-full bg-cyan-500/20 text-cyan-400 font-bold flex items-center justify-center border border-cyan-500/30">
                    {activeRide.driver.name.charAt(0)}
                  </div>
                  <div>
                    <div className="text-xs font-bold text-white flex items-center gap-1.5">
                      {activeRide.driver.name}
                      <span className="text-[10px] bg-amber-500/20 text-amber-300 px-1.5 py-0.2 rounded flex items-center gap-0.5">
                        <Star className="w-2.5 h-2.5 fill-amber-300" /> {activeRide.driver.rating}
                      </span>
                    </div>
                    <p className="text-[11px] text-slate-400">
                      {activeRide.driver.vehicle.color} {activeRide.driver.vehicle.make} {activeRide.driver.vehicle.model}
                    </p>
                  </div>
                </div>

                <div className="text-right">
                  <div className="text-xs font-mono font-bold bg-slate-950 px-2 py-1 rounded border border-slate-800 text-cyan-400">
                    {activeRide.driver.vehicle.licensePlate}
                  </div>
                  <div className="text-[10px] text-slate-500 mt-0.5">{activeRide.driver.vehicle.type}</div>
                </div>
              </div>

              {/* Status Specific Actions */}
              {activeRide.status !== 'Completed' && activeRide.status !== 'Cancelled' && (
                <div className="pt-2 border-t border-slate-800 flex justify-between items-center text-xs">
                  <span className="text-emerald-400 flex items-center gap-1">
                    <Shield className="w-3.5 h-3.5" /> GPS Verified
                  </span>
                  <button
                    onClick={handleCancel}
                    className="text-red-400 hover:text-red-300 text-xs font-medium"
                  >
                    Cancel Ride
                  </button>
                </div>
              )}
            </div>
          )}

          {/* Completed Receipt Card */}
          {activeRide.status === 'Completed' && (
            <div className="bg-emerald-950/40 border border-emerald-500/30 rounded-xl p-3 text-center space-y-2">
              <CheckCircle2 className="w-8 h-8 text-emerald-400 mx-auto" />
              <div className="text-sm font-bold text-emerald-300">Ride Completed Successfully!</div>
              <div className="text-2xl font-black text-white">${activeRide.actualFare || activeRide.estimatedFare}</div>
              <p className="text-xs text-slate-400">
                {activeRide.distanceKm} km • {activeRide.estimatedDurationMinutes} mins
              </p>
              <button
                onClick={() => {
                  window.location.reload();
                }}
                className="mt-2 w-full py-2 bg-emerald-500 hover:bg-emerald-600 text-slate-950 font-bold text-xs rounded-lg transition"
              >
                Book Another Ride
              </button>
            </div>
          )}
        </div>
      )}

      {/* RIDE BOOKING FORM (Only when no active ride or completed/cancelled) */}
      {(!activeRide || activeRide.status === 'Completed' || activeRide.status === 'Cancelled') && (
        <div className="space-y-4">
          {/* Pickup Selector */}
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-slate-400 flex items-center justify-between">
              <span className="flex items-center gap-1 text-emerald-400">
                <MapPin className="w-3.5 h-3.5" /> Pickup Location
              </span>
              <button
                onClick={() => onSelectMapLocation('pickup')}
                className={`text-[11px] px-2 py-0.5 rounded border transition ${
                  isSelectingLocation && selectingType === 'pickup'
                    ? 'bg-emerald-500 text-slate-950 border-emerald-500 font-bold animate-pulse'
                    : 'text-slate-400 border-slate-700 hover:text-emerald-400'
                }`}
              >
                {isSelectingLocation && selectingType === 'pickup' ? 'Click on map now...' : 'Pick on Map'}
              </button>
            </label>

            <select
              value={pickup.name}
              onChange={(e) => {
                const p = SF_PRESETS.find((x) => x.name === e.target.value);
                if (p) setPickup(p);
              }}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-slate-200 focus:outline-none focus:border-cyan-500"
            >
              {SF_PRESETS.map((p) => (
                <option key={p.name} value={p.name}>
                  {p.name}
                </option>
              ))}
              {!SF_PRESETS.some((p) => p.name === pickup.name) && (
                <option value={pickup.name}>{pickup.name}</option>
              )}
            </select>
          </div>

          {/* Dropoff Selector */}
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-slate-400 flex items-center justify-between">
              <span className="flex items-center gap-1 text-red-400">
                <Navigation className="w-3.5 h-3.5" /> Destination
              </span>
              <button
                onClick={() => onSelectMapLocation('dropoff')}
                className={`text-[11px] px-2 py-0.5 rounded border transition ${
                  isSelectingLocation && selectingType === 'dropoff'
                    ? 'bg-red-500 text-slate-950 border-red-500 font-bold animate-pulse'
                    : 'text-slate-400 border-slate-700 hover:text-red-400'
                }`}
              >
                {isSelectingLocation && selectingType === 'dropoff' ? 'Click on map now...' : 'Pick on Map'}
              </button>
            </label>

            <select
              value={dropoff.name}
              onChange={(e) => {
                const d = SF_PRESETS.find((x) => x.name === e.target.value);
                if (d) setDropoff(d);
              }}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-slate-200 focus:outline-none focus:border-cyan-500"
            >
              {SF_PRESETS.map((p) => (
                <option key={p.name} value={p.name}>
                  {p.name}
                </option>
              ))}
              {!SF_PRESETS.some((p) => p.name === dropoff.name) && (
                <option value={dropoff.name}>{dropoff.name}</option>
              )}
            </select>
          </div>

          {/* Fare Estimate Breakdown */}
          {estimate && (
            <div className="bg-slate-950 border border-slate-800 rounded-xl p-3 space-y-2">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div className="p-2 bg-cyan-500/10 text-cyan-400 rounded-lg">
                    <Car className="w-4 h-4" />
                  </div>
                  <div>
                    <div className="text-xs font-bold text-white">UberCore Standard</div>
                    <div className="text-[11px] text-slate-400">
                      {estimate.distanceKm} km • ~{estimate.estimatedDurationMinutes} mins
                    </div>
                  </div>
                </div>

                <div className="text-right">
                  <div className="text-lg font-black text-white">${estimate.estimatedFare}</div>
                  <div className="text-[10px] text-slate-500">Base: ${estimate.baseFare} + ${estimate.perKmRate}/km</div>
                </div>
              </div>
            </div>
          )}

          {/* Request Button */}
          <button
            onClick={handleBookRide}
            disabled={isLoading || !selectedRider || !estimate}
            className="w-full py-3 bg-gradient-to-r from-cyan-500 to-blue-600 hover:from-cyan-400 hover:to-blue-500 text-slate-950 font-black text-sm uppercase tracking-wider rounded-xl shadow-lg shadow-cyan-500/20 transition-all flex items-center justify-center gap-2 disabled:opacity-50"
          >
            <Sparkles className="w-4 h-4" />
            Request Uber Ride (${estimate?.estimatedFare || '--'})
          </button>
        </div>
      )}

      {/* New Rider Registration Modal */}
      {showNewRiderModal && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-[2000] flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-sm w-full p-5 space-y-4 shadow-2xl max-h-[90vh] overflow-y-auto">
            <h3 className="text-sm font-bold text-white uppercase tracking-wider">Register New Rider</h3>
            <form onSubmit={handleCreateRider} className="space-y-3">
              <div>
                <label className="text-xs text-slate-400 block mb-1">Full Name</label>
                <input
                  type="text"
                  required
                  placeholder="e.g. Alex Morgan"
                  value={newRiderName}
                  onChange={(e) => setNewRiderName(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-cyan-500"
                />
              </div>

              <div>
                <label className="text-xs text-slate-400 block mb-1">Email</label>
                <input
                  type="email"
                  required
                  placeholder="alex@example.com"
                  value={newRiderEmail}
                  onChange={(e) => setNewRiderEmail(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-cyan-500"
                />
              </div>

              <div>
                <label className="text-xs text-slate-400 block mb-1">Phone Number</label>
                <input
                  type="tel"
                  placeholder="+1-555-0199"
                  value={newRiderPhone}
                  onChange={(e) => setNewRiderPhone(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-cyan-500"
                />
              </div>

              {/* Rider Default Location Picker */}
              <div className="space-y-1.5 pt-1 border-t border-slate-800">
                <label className="text-xs font-semibold text-slate-400 flex items-center justify-between">
                  <span className="flex items-center gap-1 text-cyan-400">
                    <MapPin className="w-3.5 h-3.5" /> Preferred Starting Location
                  </span>
                  <button
                    type="button"
                    onClick={() => {
                      if (onSelectMapLocation) onSelectMapLocation('rider_reg');
                    }}
                    className={`text-[11px] px-2 py-0.5 rounded border transition ${
                      isSelectingLocation && selectingType === 'rider_reg'
                        ? 'bg-cyan-500 text-slate-950 border-cyan-500 font-bold animate-pulse'
                        : 'text-slate-400 border-slate-700 hover:text-cyan-400'
                    }`}
                  >
                    {isSelectingLocation && selectingType === 'rider_reg' ? 'Click map now...' : 'Pick on Map'}
                  </button>
                </label>

                <select
                  value={riderLocPreset.name}
                  onChange={(e) => {
                    const p = SF_PRESETS.find((x) => x.name === e.target.value);
                    if (p) {
                      setRiderLocPreset(p);
                      setRiderLat(p.lat);
                      setRiderLon(p.lon);
                    }
                  }}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-slate-200 focus:outline-none focus:border-cyan-500"
                >
                  {SF_PRESETS.map((p) => (
                    <option key={p.name} value={p.name}>
                      {p.name}
                    </option>
                  ))}
                </select>

                <div className="text-[11px] font-mono text-slate-500 flex justify-between px-1">
                  <span>Lat: {riderLat.toFixed(4)}</span>
                  <span>Lon: {riderLon.toFixed(4)}</span>
                </div>
              </div>

              <div className="flex items-center gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setShowNewRiderModal(false)}
                  className="flex-1 py-2 bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold rounded-xl"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="flex-1 py-2 bg-cyan-500 hover:bg-cyan-400 text-slate-950 text-xs font-bold rounded-xl shadow-lg shadow-cyan-500/20"
                >
                  Save Rider
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
