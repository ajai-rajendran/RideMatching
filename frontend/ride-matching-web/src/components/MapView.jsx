import React, { useEffect } from 'react';
import { MapContainer, TileLayer, Marker, Popup, Polyline, useMap, useMapEvents } from 'react-leaflet';
import L from 'leaflet';
import { useRide } from '../context/RideContext';

// Fix Leaflet default icon issues in React
delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

// Custom Car SVG Icon
const createCarIcon = (status = 'Available', isSelected = false) => {
  const isAvail = status === 'Available';
  const color = isSelected ? '#3b82f6' : isAvail ? '#22c55e' : '#f59e0b';
  const bg = isSelected ? '#1e3a8a' : isAvail ? '#14532d' : '#78350f';

  return L.divIcon({
    className: 'custom-car-marker',
    html: `
      <div style="
        background: ${bg};
        border: 2px solid ${color};
        width: 38px;
        height: 38px;
        border-radius: 50%;
        display: flex;
        align-items: center;
        justify-content: center;
        box-shadow: 0 4px 12px rgba(0,0,0,0.5);
        transform: translate(-50%, -50%);
        transition: all 0.3s ease;
      ">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="${color}" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
          <path d="M19 17h2c.6 0 1-.4 1-1v-3c0-.9-.7-1.7-1.5-1.9C18.7 10.6 16 10 16 10s-1.3-1.4-2.2-2.3c-.5-.4-1.1-.7-1.8-.7H5c-.6 0-1.1.4-1.4.9l-1.4 2.9A3.7 3.7 0 0 0 2 12v4c0 .6.4 1 1 1h2"/>
          <circle cx="7" cy="17" r="2"/>
          <path d="M9 17h6"/>
          <circle cx="17" cy="17" r="2"/>
        </svg>
      </div>
    `,
    iconSize: [38, 38],
    iconAnchor: [19, 19],
  });
};

// Custom Pickup Icon (Green Pin)
const createPickupIcon = () =>
  L.divIcon({
    className: 'custom-pickup-marker',
    html: `
      <div style="
        background: #10b981;
        border: 2px solid #ffffff;
        width: 32px;
        height: 32px;
        border-radius: 50% 50% 50% 0;
        transform: rotate(-45deg) translate(8px, -8px);
        display: flex;
        align-items: center;
        justify-content: center;
        box-shadow: 0 4px 12px rgba(16, 185, 129, 0.6);
      ">
        <div style="transform: rotate(45deg); font-weight: bold; color: white; font-size: 13px;">P</div>
      </div>
    `,
    iconSize: [32, 32],
    iconAnchor: [16, 32],
  });

// Custom Dropoff Icon (Red Pin)
const createDropoffIcon = () =>
  L.divIcon({
    className: 'custom-dropoff-marker',
    html: `
      <div style="
        background: #ef4444;
        border: 2px solid #ffffff;
        width: 32px;
        height: 32px;
        border-radius: 50% 50% 50% 0;
        transform: rotate(-45deg) translate(8px, -8px);
        display: flex;
        align-items: center;
        justify-content: center;
        box-shadow: 0 4px 12px rgba(239, 68, 68, 0.6);
      ">
        <div style="transform: rotate(45deg); font-weight: bold; color: white; font-size: 13px;">D</div>
      </div>
    `,
    iconSize: [32, 32],
    iconAnchor: [16, 32],
  });

// Component to handle map clicks for selecting pickup / dropoff / driver location
function MapClickHandler({ onMapClick, isSelectingLocation }) {
  useMapEvents({
    click: (e) => {
      if (isSelectingLocation && onMapClick) {
        onMapClick(e.latlng.lat, e.latlng.lng);
      }
    },
  });
  return null;
}

// Auto-center map to fit route
function MapBoundsUpdater({ bounds }) {
  const map = useMap();
  useEffect(() => {
    if (bounds && bounds.length > 0) {
      try {
        map.fitBounds(bounds, { padding: [50, 50], maxZoom: 15 });
      } catch (err) {
        // ignore
      }
    }
  }, [bounds, map]);
  return null;
}

export default function MapView({
  pickup,
  dropoff,
  onMapClick,
  isSelectingLocation,
  selectingType, // 'pickup' | 'dropoff' | 'driver_reg' | 'rider_reg'
  height = '100%',
  highlightDriverId = null,
}) {
  const { driverLocations, drivers, selectedDriver, activeRide, driverActiveRide } = useRide();

  const currentRide = activeRide || driverActiveRide;
  const effectivePickup = pickup || (currentRide ? currentRide.pickupLocation : null);
  const effectiveDropoff = dropoff || (currentRide ? currentRide.dropoffLocation : null);

  // Compute map bounds if route points exist
  const bounds = [];
  if (effectivePickup?.latitude && effectivePickup?.longitude) {
    bounds.push([effectivePickup.latitude, effectivePickup.longitude]);
  }
  if (effectiveDropoff?.latitude && effectiveDropoff?.longitude) {
    bounds.push([effectiveDropoff.latitude, effectiveDropoff.longitude]);
  }
  if (highlightDriverId && driverLocations[highlightDriverId]) {
    bounds.push([driverLocations[highlightDriverId].lat, driverLocations[highlightDriverId].lon]);
  }

  const defaultCenter = [37.7749, -122.4194]; // San Francisco Civic Center

  return (
    <div className="relative w-full h-full rounded-2xl overflow-hidden shadow-2xl border border-slate-800 bg-slate-950">
      {isSelectingLocation && (
        <div className="absolute top-4 left-1/2 transform -translate-x-1/2 z-[1000] bg-slate-900/95 backdrop-blur border border-cyan-500/50 text-cyan-400 px-4 py-2 rounded-full shadow-lg text-xs font-semibold flex items-center gap-2 animate-bounce">
          <span className="w-2 h-2 rounded-full bg-cyan-400 animate-ping"></span>
          Click anywhere on map to choose{' '}
          {selectingType === 'pickup'
            ? '🟢 Pickup Location'
            : selectingType === 'dropoff'
            ? '🔴 Destination Location'
            : selectingType === 'driver_reg'
            ? '🚖 Driver Starting GPS Location'
            : '👤 Rider Starting GPS Location'}
        </div>
      )}

      <MapContainer
        center={defaultCenter}
        zoom={13}
        style={{ height, width: '100%' }}
        scrollWheelZoom={true}
        className="z-10"
      >
        {/* OpenStreetMap Standard Free Tiles (100% Free, NO API KEY REQUIRED) */}
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />

        <MapClickHandler onMapClick={onMapClick} isSelectingLocation={isSelectingLocation} />
        {bounds.length >= 2 && <MapBoundsUpdater bounds={bounds} />}

        {/* Render Live Drivers - ONLY SHOW ONLINE/AVAILABLE/ASSIGNED/INRIDE DRIVERS */}
        {Object.entries(driverLocations).map(([driverId, loc]) => {
          if (!loc || !loc.lat || !loc.lon) return null;
          const driverInfo = drivers.find((d) => d.id === driverId);
          const status = loc.status || driverInfo?.status || 'Offline';

          // EXCLUDE OFFLINE DRIVERS FROM MAP AS REQUESTED
          if (status === 'Offline') {
            return null;
          }

          const isSelected = selectedDriver?.id === driverId || highlightDriverId === driverId;

          return (
            <Marker
              key={driverId}
              position={[loc.lat, loc.lon]}
              icon={createCarIcon(status, isSelected)}
            >
              <Popup className="custom-popup">
                <div className="text-slate-900 p-1">
                  <div className="font-bold text-sm">{loc.name || driverInfo?.name || 'Driver'}</div>
                  <div className="text-xs text-slate-600">
                    {driverInfo?.vehicle?.make} {driverInfo?.vehicle?.model} ({driverInfo?.vehicle?.licensePlate})
                  </div>
                  <div className="text-xs mt-1 flex items-center gap-1 font-semibold">
                    <span
                      className={`inline-block w-2 h-2 rounded-full ${
                        status === 'Available' ? 'bg-emerald-500' : 'bg-amber-500'
                      }`}
                    ></span>
                    {status}
                  </div>
                </div>
              </Popup>
            </Marker>
          );
        })}

        {/* Pickup Pin */}
        {effectivePickup?.latitude && effectivePickup?.longitude && (
          <Marker
            position={[effectivePickup.latitude, effectivePickup.longitude]}
            icon={createPickupIcon()}
          >
            <Popup>
              <div className="text-slate-900 font-semibold text-xs">
                🟢 Pickup Location
                <div className="text-slate-600 font-normal">{effectivePickup.address || 'Pickup'}</div>
              </div>
            </Popup>
          </Marker>
        )}

        {/* Dropoff Pin */}
        {effectiveDropoff?.latitude && effectiveDropoff?.longitude && (
          <Marker
            position={[effectiveDropoff.latitude, effectiveDropoff.longitude]}
            icon={createDropoffIcon()}
          >
            <Popup>
              <div className="text-slate-900 font-semibold text-xs">
                🔴 Destination
                <div className="text-slate-600 font-normal">{effectiveDropoff.address || 'Dropoff'}</div>
              </div>
            </Popup>
          </Marker>
        )}

        {/* Route Polyline connecting Pickup to Dropoff */}
        {effectivePickup?.latitude &&
          effectivePickup?.longitude &&
          effectiveDropoff?.latitude &&
          effectiveDropoff?.longitude && (
            <Polyline
              positions={[
                [effectivePickup.latitude, effectivePickup.longitude],
                [effectiveDropoff.latitude, effectiveDropoff.longitude],
              ]}
              color="#0284c7"
              weight={5}
              opacity={0.85}
              dashArray="8, 8"
            />
          )}

        {/* En-route driver tracking line to Pickup */}
        {currentRide &&
          currentRide.driverId &&
          driverLocations[currentRide.driverId] &&
          effectivePickup?.latitude && (
            <Polyline
              positions={[
                [driverLocations[currentRide.driverId].lat, driverLocations[currentRide.driverId].lon],
                [effectivePickup.latitude, effectivePickup.longitude],
              ]}
              color="#d97706"
              weight={4}
              opacity={0.9}
            />
          )}
      </MapContainer>
    </div>
  );
}
