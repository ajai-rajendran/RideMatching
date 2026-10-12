import React, { useState } from 'react';
import { useRide } from '../context/RideContext';
import { Car, User, Columns, RefreshCw, Zap, Activity, CheckCircle, AlertTriangle, ShieldCheck } from 'lucide-react';

export default function Navbar() {
  const { currentMode, setCurrentMode, signalRStatus, reSeedData, isLoading } = useRide();
  const [isResetting, setIsResetting] = useState(false);

  const handleReset = async () => {
    setIsResetting(true);
    await reSeedData();
    setIsResetting(false);
  };

  return (
    <header className="bg-slate-900/90 backdrop-blur border-b border-slate-800 sticky top-0 z-50 px-4 py-3">
      <div className="max-w-7xl mx-auto flex flex-wrap items-center justify-between gap-3">
        {/* Brand */}
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-cyan-500 to-blue-600 flex items-center justify-center shadow-lg shadow-cyan-500/20">
            <Zap className="w-6 h-6 text-white" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <span className="text-lg font-black tracking-wider text-white uppercase">UberCore</span>
              <span className="text-[10px] uppercase font-bold tracking-widest bg-cyan-500/20 text-cyan-400 px-2 py-0.5 rounded-full border border-cyan-500/30">
                Real-Time
              </span>
            </div>
            <p className="text-xs text-slate-400">SignalR • Redis GEO • EF Core</p>
          </div>
        </div>

        {/* Mode Switcher */}
        <div className="flex items-center bg-slate-950 p-1 rounded-xl border border-slate-800 shadow-inner">
          <button
            onClick={() => setCurrentMode('rider')}
            className={`flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
              currentMode === 'rider'
                ? 'bg-cyan-500 text-slate-950 shadow-md font-bold'
                : 'text-slate-400 hover:text-white hover:bg-slate-900'
            }`}
          >
            <User className="w-4 h-4" />
            Rider Mode
          </button>

          <button
            onClick={() => setCurrentMode('driver')}
            className={`flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
              currentMode === 'driver'
                ? 'bg-emerald-500 text-slate-950 shadow-md font-bold'
                : 'text-slate-400 hover:text-white hover:bg-slate-900'
            }`}
          >
            <Car className="w-4 h-4" />
            Driver Mode
          </button>

          <button
            onClick={() => setCurrentMode('dual')}
            className={`flex items-center gap-2 px-3.5 py-1.5 rounded-lg text-xs font-semibold transition-all ${
              currentMode === 'dual'
                ? 'bg-gradient-to-r from-cyan-500 to-emerald-500 text-slate-950 shadow-md font-bold'
                : 'text-slate-400 hover:text-white hover:bg-slate-900'
            }`}
          >
            <Columns className="w-4 h-4" />
            Dual Screen (Live Demo)
          </button>
        </div>

        {/* Right Status Badges & Controls */}
        <div className="flex items-center gap-3">
          {/* SignalR Connection Pill */}
          <div
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-medium border ${
              signalRStatus === 'Connected'
                ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-400'
                : signalRStatus === 'Reconnecting'
                ? 'bg-amber-500/10 border-amber-500/30 text-amber-400'
                : 'bg-red-500/10 border-red-500/30 text-red-400'
            }`}
          >
            <span
              className={`w-2 h-2 rounded-full ${
                signalRStatus === 'Connected'
                  ? 'bg-emerald-400 animate-pulse'
                  : signalRStatus === 'Reconnecting'
                  ? 'bg-amber-400 animate-ping'
                  : 'bg-red-400'
              }`}
            ></span>
            <span className="capitalize">{signalRStatus}</span>
          </div>

          {/* Reset / Seed Button */}
          <button
            onClick={handleReset}
            disabled={isResetting || isLoading}
            title="Reset and re-seed sample drivers & riders"
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 transition disabled:opacity-50"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isResetting ? 'animate-spin' : ''}`} />
            Re-seed
          </button>
        </div>
      </div>
    </header>
  );
}
