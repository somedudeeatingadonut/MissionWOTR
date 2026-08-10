import React, { useState } from 'react';
import { useGameStore } from '../../store/useGameStore';
import { Save, Upload, Download, X, CheckCircle2, AlertCircle } from 'lucide-react';
import { soundEngine } from '../../engine/soundEngine';

interface SaveLoadModalProps {
  onClose: () => void;
}

export const SaveLoadModal: React.FC<SaveLoadModalProps> = ({ onClose }) => {
  const { 
    saveGameToLocal, 
    loadGameFromLocal, 
    exportGameJson, 
    importGameJson 
  } = useGameStore();

  const [importText, setImportText] = useState<string>('');
  const [message, setMessage] = useState<{ text: string; type: 'success' | 'error' } | null>(null);

  const handleSaveLocal = () => {
    saveGameToLocal();
    setMessage({ text: 'Game saved successfully to browser local storage!', type: 'success' });
  };

  const handleLoadLocal = () => {
    const ok = loadGameFromLocal();
    if (ok) {
      setMessage({ text: 'Saved game loaded successfully!', type: 'success' });
    } else {
      setMessage({ text: 'No save data found in local storage.', type: 'error' });
    }
  };

  const handleExportJson = () => {
    soundEngine.playClick();
    const json = exportGameJson();
    const blob = new Blob([json], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `devtycoon_save_${Date.now()}.json`;
    a.click();
    URL.revokeObjectURL(url);
    setMessage({ text: 'Save file downloaded to your computer!', type: 'success' });
  };

  const handleImportJson = () => {
    if (!importText.trim()) return;
    const ok = importGameJson(importText.trim());
    if (ok) {
      setMessage({ text: 'Studio data imported successfully!', type: 'success' });
      setImportText('');
    } else {
      setMessage({ text: 'Invalid JSON save format.', type: 'error' });
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-md">
      <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-lg w-full shadow-2xl overflow-hidden flex flex-col">
        {/* Header */}
        <div className="p-5 border-b border-slate-800 bg-slate-950/60 flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="p-2.5 rounded-xl bg-gradient-to-br from-sky-500 to-indigo-600 text-white">
              <Save className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-lg font-bold text-white leading-none">
                Save, Load, & Export Studio Data
              </h3>
              <p className="text-xs text-slate-400 mt-1">
                Persist your tycoon progress or transfer your studio between browsers.
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="text-slate-500 hover:text-white p-1 rounded-lg transition"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content */}
        <div className="p-6 space-y-6">
          {message && (
            <div
              className={`p-4 rounded-xl border flex items-center space-x-3 text-xs font-semibold ${
                message.type === 'success'
                  ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-300'
                  : 'bg-rose-500/10 border-rose-500/30 text-rose-300'
              }`}
            >
              {message.type === 'success' ? (
                <CheckCircle2 className="w-5 h-5 shrink-0 text-emerald-400" />
              ) : (
                <AlertCircle className="w-5 h-5 shrink-0 text-rose-400" />
              )}
              <span>{message.text}</span>
            </div>
          )}

          {/* Local Storage Buttons */}
          <div className="grid grid-cols-2 gap-3">
            <button
              onClick={handleSaveLocal}
              className="p-4 rounded-xl bg-slate-950 hover:bg-slate-800 border border-slate-800 hover:border-sky-500 transition flex flex-col items-center justify-center text-center space-y-1.5"
            >
              <Save className="w-6 h-6 text-sky-400" />
              <span className="font-bold text-sm text-white">Save to Browser</span>
              <span className="text-[10px] text-slate-400">Stores in local storage</span>
            </button>

            <button
              onClick={handleLoadLocal}
              className="p-4 rounded-xl bg-slate-950 hover:bg-slate-800 border border-slate-800 hover:border-sky-500 transition flex flex-col items-center justify-center text-center space-y-1.5"
            >
              <Upload className="w-6 h-6 text-emerald-400" />
              <span className="font-bold text-sm text-white">Load Saved Game</span>
              <span className="text-[10px] text-slate-400">Restore last progress</span>
            </button>
          </div>

          {/* Export JSON Button */}
          <div className="pt-2 border-t border-slate-800">
            <button
              onClick={handleExportJson}
              className="w-full p-3.5 rounded-xl bg-slate-950 hover:bg-slate-800 border border-slate-800 hover:border-indigo-500 transition flex items-center justify-center space-x-2"
            >
              <Download className="w-4 h-4 text-indigo-400" />
              <span className="font-semibold text-xs text-white">Download Save File (.json)</span>
            </button>
          </div>

          {/* Import JSON textarea */}
          <div className="space-y-2 pt-2 border-t border-slate-800">
            <label className="text-xs font-bold uppercase tracking-wider text-slate-300">
              Import Save Data (JSON)
            </label>
            <textarea
              placeholder="Paste saved JSON content here to import your studio..."
              value={importText}
              onChange={(e) => setImportText(e.target.value)}
              className="w-full h-24 bg-slate-950 border border-slate-700 rounded-xl p-3 text-xs font-mono text-white focus:outline-none focus:border-sky-500"
            />
            <button
              onClick={handleImportJson}
              disabled={!importText.trim()}
              className={`w-full py-2.5 rounded-xl font-bold text-xs transition ${
                importText.trim()
                  ? 'bg-sky-600 hover:bg-sky-500 text-white'
                  : 'bg-slate-800 text-slate-500 cursor-not-allowed'
              }`}
            >
              Import JSON Save Data
            </button>
          </div>
        </div>

        {/* Footer */}
        <div className="p-4 bg-slate-950 border-t border-slate-800 flex justify-end">
          <button
            onClick={onClose}
            className="px-6 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
};
