import React, { useState } from 'react';
import { useRide } from './context/RideContext';
import Navbar from './components/Navbar';
import MapView from './components/MapView';
import RiderView from './components/RiderView';
import DriverView from './components/DriverView';
import ActivityLog from './components/ActivityLog';
import { Sparkles, Car, User, Columns, ShieldCheck, CheckCircle2, AlertCircle, Info } from 'lucide-react';

export default function App() {
  const { currentMode, toastMessage, showToast, selectedDriver, activeRide, driverActiveRide } = useRide();

  // Location picking on map
  const [isSelectingLocation, setIsSelectingLocation] = useState(false);
  const [selectingType, setSelectingType] = useState('pickup'); // 'pickup' | 'dropoff' | 'driver_reg' | 'rider_reg'
  const [customPickup, setCustomPickup] = useState(null);
  const [customDropoff, setCustomDropoff] = useState(null);
  const [pendingDriverLocation, setPendingDriverLocation] = useState(null);
  const [pendingRiderLocation, setPendingRiderLocation] = useState(null);

  const handleStartMapSelection = (type) => {
    setSelectingType(type);
    setIsSelectingLocation(true);
    showToast(`Click anywhere on the map to set ${type.replace('_', ' ')} coordinates.`, 'info');
  };

  const handleMapClick = (lat, lon) => {
    const formatted = `Custom Location (${lat.toFixed(4)}, ${lon.toFixed(4)})`;

    if (selectingType === 'pickup') {
      setCustomPickup({ name: formatted, lat, lon });
      showToast(`🟢 Pickup location set: ${lat.toFixed(4)}, ${lon.toFixed(4)}`, 'success');
    } else if (selectingType === 'dropoff') {
      setCustomDropoff({ name: formatted, lat, lon });
      showToast(`🔴 Destination set: ${lat.toFixed(4)}, ${lon.toFixed(4)}`, 'success');
    } else if (selectingType === 'driver_reg') {
      setPendingDriverLocation({ name: formatted, lat, lon });
      showToast(`🚖 Driver starting location picked: ${lat.toFixed(4)}, ${lon.toFixed(4)}`, 'success');
    } else if (selectingType === 'rider_reg') {
      setPendingRiderLocation({ name: formatted, lat, lon });
      showToast(`👤 Rider location picked: ${lat.toFixed(4)}, ${lon.toFixed(4)}`, 'success');
    }

    setIsSelectingLocation(false);
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col font-sans selection:bg-cyan-500 selection:text-slate-950">
      <Navbar />

      {/* Global Toast Alert */}
      {toastMessage && (
        <div className="fixed bottom-6 right-6 z-[3000] max-w-md animate-slide-in">
          <div
            className={`px-4 py-3 rounded-2xl shadow-2xl backdrop-blur-md border flex items-center gap-3 text-xs font-semibold ${
              toastMessage.type === 'success'
                ? 'bg-emerald-950/90 border-emerald-500/40 text-emerald-200'
                : toastMessage.type === 'warning' || toastMessage.type === 'error'
                ? 'bg-red-950/90 border-red-500/40 text-red-200'
                : 'bg-cyan-950/90 border-cyan-500/40 text-cyan-200'
            }`}
          >
            {toastMessage.type === 'success' ? (
              <CheckCircle2 className="w-5 h-5 text-emerald-400 shrink-0" />
            ) : toastMessage.type === 'warning' || toastMessage.type === 'error' ? (
              <AlertCircle className="w-5 h-5 text-red-400 shrink-0" />
            ) : (
              <Info className="w-5 h-5 text-cyan-400 shrink-0" />
            )}
            <span>{toastMessage.message}</span>
          </div>
        </div>
      )}

      {/* Main Content Area */}
      <main className="flex-1 max-w-7xl w-full mx-auto p-4 flex flex-col gap-4">
        {/* RIDER MODE */}
        {currentMode === 'rider' && (
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-4 flex-1 min-h-[620px]">
            <div className="lg:col-span-5 flex flex-col">
              <RiderView
                onSelectMapLocation={handleStartMapSelection}
                selectingType={selectingType}
                isSelectingLocation={isSelectingLocation}
                pendingRiderLocation={pendingRiderLocation}
                customPickup={customPickup}
                customDropoff={customDropoff}
              />
            </div>
            <div className="lg:col-span-7 h-[620px] lg:h-auto">
              <MapView
                pickup={customPickup}
                dropoff={customDropoff}
                onMapClick={handleMapClick}
                isSelectingLocation={isSelectingLocation}
                selectingType={selectingType}
              />
            </div>
          </div>
        )}

        {/* DRIVER MODE */}
        {currentMode === 'driver' && (
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-4 flex-1 min-h-[620px]">
            <div className="lg:col-span-5 flex flex-col">
              <DriverView
                onSelectMapLocation={handleStartMapSelection}
                selectingType={selectingType}
                isSelectingLocation={isSelectingLocation}
                pendingDriverLocation={pendingDriverLocation}
              />
            </div>
            <div className="lg:col-span-7 h-[620px] lg:h-auto">
              <MapView
                highlightDriverId={selectedDriver?.id}
                onMapClick={handleMapClick}
                isSelectingLocation={isSelectingLocation}
                selectingType={selectingType}
              />
            </div>
          </div>
        )}

        {/* DUAL MODE (Rider on Left, Driver on Right, Interactive Central Map) */}
        {currentMode === 'dual' && (
          <div className="flex flex-col gap-4 flex-1">
            <div className="bg-slate-900/40 border border-slate-800 p-3 rounded-2xl flex items-center justify-between">
              <div className="flex items-center gap-2">
                <span className="p-1.5 bg-gradient-to-r from-cyan-500 to-emerald-500 rounded-lg text-slate-950 font-black text-xs">
                  DUAL LIVE
                </span>
                <span className="text-xs font-semibold text-slate-300">
                  Simulate Rider requesting a ride on the Left and Driver receiving & accepting on the Right in real time!
                </span>
              </div>
            </div>

            {/* Split Grid */}
            <div className="grid grid-cols-1 lg:grid-cols-12 gap-4 min-h-[680px]">
              {/* Left Column: Rider Portal */}
              <div className="lg:col-span-4 flex flex-col">
                <RiderView
                  onSelectMapLocation={handleStartMapSelection}
                  selectingType={selectingType}
                  isSelectingLocation={isSelectingLocation}
                  pendingRiderLocation={pendingRiderLocation}
                  customPickup={customPickup}
                  customDropoff={customDropoff}
                />
              </div>

              {/* Center Column: Live Shared Map */}
              <div className="lg:col-span-4 h-[450px] lg:h-auto">
                <MapView
                  pickup={customPickup}
                  dropoff={customDropoff}
                  onMapClick={handleMapClick}
                  isSelectingLocation={isSelectingLocation}
                  selectingType={selectingType}
                  highlightDriverId={selectedDriver?.id}
                />
              </div>

              {/* Right Column: Driver Console */}
              <div className="lg:col-span-4 flex flex-col">
                <DriverView
                  onSelectMapLocation={handleStartMapSelection}
                  selectingType={selectingType}
                  isSelectingLocation={isSelectingLocation}
                  pendingDriverLocation={pendingDriverLocation}
                />
              </div>
            </div>
          </div>
        )}

        {/* Real-Time SignalR Event Stream Ticker */}
        <ActivityLog />
      </main>
    </div>
  );
}
