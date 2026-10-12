import React, { useState } from 'react';
import { useRide } from '../context/RideContext';
import { Activity, Radio, CheckCircle, XCircle, ChevronDown, ChevronUp, Clock, Zap } from 'lucide-react';

export default function ActivityLog() {
  const { activityLogs } = useRide();
  const [isExpanded, setIsExpanded] = useState(false);
  const [selectedLog, setSelectedLog] = useState(null);

  return (
    <div className="bg-slate-900/60 backdrop-blur rounded-2xl border border-slate-800 overflow-hidden shadow-xl">
      {/* Header */}
      <div
        onClick={() => setIsExpanded(!isExpanded)}
        className="px-4 py-3 flex items-center justify-between cursor-pointer hover:bg-slate-800/40 transition border-b border-slate-800"
      >
        <div className="flex items-center gap-2">
          <div className="w-2 h-2 rounded-full bg-cyan-400 animate-ping"></div>
          <span className="text-xs font-bold uppercase tracking-wider text-slate-300 flex items-center gap-1.5">
            <Activity className="w-4 h-4 text-cyan-400" /> Real-Time SignalR Event Stream
          </span>
          <span className="text-[10px] bg-slate-800 text-slate-400 px-2 py-0.5 rounded-full font-mono">
            {activityLogs.length} events
          </span>
        </div>

        <button className="text-slate-400 hover:text-white">
          {isExpanded ? <ChevronUp className="w-4 h-4" /> : <ChevronDown className="w-4 h-4" />}
        </button>
      </div>

      {/* Event Items */}
      <div className={`divide-y divide-slate-800/50 transition-all ${isExpanded ? 'max-h-72' : 'max-h-24'} overflow-y-auto`}>
        {activityLogs.length === 0 ? (
          <div className="p-4 text-center text-xs text-slate-500 flex items-center justify-center gap-2">
            <Radio className="w-3.5 h-3.5 animate-pulse" /> Listening for real-time WebSocket events...
          </div>
        ) : (
          activityLogs.map((log) => {
            const isSelected = selectedLog?.id === log.id;
            return (
              <div
                key={log.id}
                onClick={() => setSelectedLog(isSelected ? null : log)}
                className={`px-4 py-2 hover:bg-slate-800/30 transition text-xs flex items-center justify-between cursor-pointer ${
                  isSelected ? 'bg-slate-800/50' : ''
                }`}
              >
                <div className="flex items-center gap-2.5 truncate">
                  <span
                    className={`w-1.5 h-1.5 rounded-full shrink-0 ${
                      log.type === 'accepted' || log.type === 'completed'
                        ? 'bg-emerald-400'
                        : log.type === 'rejected' || log.type === 'cancelled'
                        ? 'bg-red-400'
                        : log.type === 'offer'
                        ? 'bg-amber-400 animate-ping'
                        : 'bg-cyan-400'
                    }`}
                  ></span>
                  <span className="font-semibold text-slate-300 truncate">{log.message}</span>
                </div>

                <div className="flex items-center gap-2 shrink-0 text-[10px] text-slate-500 font-mono">
                  <Clock className="w-3 h-3" />
                  {log.time}
                </div>
              </div>
            );
          })
        )}
      </div>

      {/* Expanded JSON Inspector if an event is selected */}
      {selectedLog?.data && (
        <div className="p-3 bg-slate-950 border-t border-slate-800 text-[11px] font-mono text-cyan-300 max-h-40 overflow-auto">
          <div className="text-[10px] uppercase font-bold text-slate-500 mb-1">Payload: {selectedLog.type}</div>
          <pre>{JSON.stringify(selectedLog.data, null, 2)}</pre>
        </div>
      )}
    </div>
  );
}
