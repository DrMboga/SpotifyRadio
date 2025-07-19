#pragma once

class CapacitanceState {
    public:
        CapacitanceState();

        /// @brief Returns true if the channel changed, false otherwise
        bool updateState();

        /// @brief Keeps the current frequency
        int getCurrentFrequency() const;
    private:
        int _currentFrequency;
        void charge();
        void discharge();
        int32_t getCapacitance();
        int getFrequency(int32_t capacitance);
};