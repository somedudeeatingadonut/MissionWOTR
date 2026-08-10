import React, { useState, useEffect } from 'react';
import { useGameStore } from './store/useGameStore';
import { 
  Navbar, 
  Sidebar, 
  TabId, 
  StudioFloorView, 
  ProjectListView, 
  EmployeeListView, 
  TechTreeView, 
  MarketTrendsView, 
  FinanceDashboard,
  NotificationToast,
  EventModal,
  NewGameWizard,
  ReviewModal,
  HiringModal,
  EngineCreatorModal,
  ScenarioModal,
  SaveLoadModal
} from './components';

export const App: React.FC = () => {
  const { speed, tickDay, activeEvent, activeReviewModalProject } = useGameStore();

  const [activeTab, setActiveTab] = useState<TabId>('studio');
  const [showNewGameModal, setShowNewGameModal] = useState<boolean>(false);
  const [showHireModal, setShowHireModal] = useState<boolean>(false);
  const [showEngineModal, setShowEngineModal] = useState<boolean>(false);
  const [showScenarioModal, setShowScenarioModal] = useState<boolean>(false);
  const [showSaveLoadModal, setShowSaveLoadModal] = useState<boolean>(false);

  // Daily Tick Engine Loop
  useEffect(() => {
    if (speed === 0) return;

    // Determine interval duration in milliseconds
    let intervalMs = 800;
    if (speed === 2) intervalMs = 400;
    if (speed === 5) intervalMs = 150;

    const interval = setInterval(() => {
      // Pause tick if an interactive dilemma modal or review modal is open
      if (!activeEvent && !activeReviewModalProject) {
        tickDay();
      }
    }, intervalMs);

    return () => clearInterval(interval);
  }, [speed, tickDay, activeEvent, activeReviewModalProject]);

  return (
    <div className="flex flex-col h-screen w-screen overflow-hidden bg-slate-950 text-slate-100 selection:bg-sky-500 selection:text-white">
      {/* Top Navbar */}
      <Navbar
        onOpenNewGame={() => setShowNewGameModal(true)}
        onOpenScenarios={() => setShowScenarioModal(true)}
        onOpenSaveLoad={() => setShowSaveLoadModal(true)}
      />

      {/* Main Container: Sidebar + Active View */}
      <div className="flex flex-1 overflow-hidden relative">
        <Sidebar
          activeTab={activeTab}
          onSelectTab={setActiveTab}
        />

        {/* Dynamic Content Pane */}
        <main className="flex-1 flex flex-col overflow-hidden relative">
          {activeTab === 'studio' && (
            <StudioFloorView
              onOpenHire={() => setShowHireModal(true)}
              onOpenNewGame={() => setShowNewGameModal(true)}
              onNavigateTab={(tab) => setActiveTab(tab)}
            />
          )}

          {activeTab === 'projects' && (
            <ProjectListView
              onOpenNewGame={() => setShowNewGameModal(true)}
            />
          )}

          {activeTab === 'staff' && (
            <EmployeeListView
              onOpenHire={() => setShowHireModal(true)}
            />
          )}

          {activeTab === 'tech' && (
            <TechTreeView
              onOpenEngineCreator={() => setShowEngineModal(true)}
            />
          )}

          {activeTab === 'market' && (
            <MarketTrendsView />
          )}

          {activeTab === 'finance' && (
            <FinanceDashboard />
          )}
        </main>
      </div>

      {/* Toast Notification Stack */}
      <NotificationToast />

      {/* Dynamic Dilemma Modal (Auto-open when event triggers) */}
      <EventModal />

      {/* Review Score Reveal Modal (Auto-open when game is released) */}
      <ReviewModal />

      {/* Wizard & Action Modals */}
      {showNewGameModal && (
        <NewGameWizard onClose={() => setShowNewGameModal(false)} />
      )}

      {showHireModal && (
        <HiringModal onClose={() => setShowHireModal(false)} />
      )}

      {showEngineModal && (
        <EngineCreatorModal onClose={() => setShowEngineModal(false)} />
      )}

      {showScenarioModal && (
        <ScenarioModal onClose={() => setShowScenarioModal(false)} />
      )}

      {showSaveLoadModal && (
        <SaveLoadModal onClose={() => setShowSaveLoadModal(false)} />
      )}
    </div>
  );
};

export default App;
