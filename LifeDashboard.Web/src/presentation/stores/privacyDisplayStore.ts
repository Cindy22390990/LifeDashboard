import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface PrivacyDisplayState {
  isAmountHidden: boolean;
  toggleVisibility: () => void;
  setAmountHidden: (value: boolean) => void;
}

export const usePrivacyDisplayStore = create<PrivacyDisplayState>()(
  persist(
    (set) => ({
      isAmountHidden: false,
      toggleVisibility: () => set((state) => ({
        isAmountHidden: !state.isAmountHidden,
      })),
      setAmountHidden: (value) => set({ isAmountHidden: value }),
    }),
    {
      name: 'privacy-display-preference',
      partialize: (state) => ({ isAmountHidden: state.isAmountHidden }),
    },
  ),
);
